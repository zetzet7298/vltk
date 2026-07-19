package service

import (
	"context"
	"encoding/binary"
	"errors"
	"fmt"
	"net/http"
	"time"

	"google.golang.org/protobuf/proto"
	"nhooyr.io/websocket"

	"vltk.dev/server-runtime/catalog"
	"vltk.dev/server-runtime/combat"
	gamev1 "vltk.dev/server-runtime/gen/game/v1"
	"vltk.dev/server-runtime/session"
)

const (
	GameV1Subprotocol     = "game.v1"
	DefaultMaxFrameBytes  = 64 << 10
	DefaultMaxInputBatch  = 16
	ReconnectGraceSeconds = 15
)

var (
	ErrAuthRejected    = errors.New("service: auth rejected")
	ErrContentRejected = errors.New("service: active content mismatch")
)

type TicketVerifier interface {
	VerifyTicket(context.Context, string) (AuthenticatedSession, error)
}

type AuthenticatedSession struct {
	RealmID      string
	CharacterID  string
	SessionEpoch uint64
	Runtime      Runtime
	Session      Session
}

type RejectingTicketVerifier struct{}

func (RejectingTicketVerifier) VerifyTicket(context.Context, string) (AuthenticatedSession, error) {
	return AuthenticatedSession{}, ErrAuthRejected
}

type GameV1Handler struct {
	ActiveContent catalog.ContentDigest
	SkillPolicy   catalog.RuntimeSkillPolicy
	MaxFrameBytes uint32
	MaxInputBatch uint32
	Verifier      TicketVerifier
}

func (h GameV1Handler) ServeHTTP(w http.ResponseWriter, r *http.Request) {
	maxFrame := h.MaxFrameBytes
	if maxFrame == 0 {
		maxFrame = DefaultMaxFrameBytes
	}
	maxBatch := h.MaxInputBatch
	if maxBatch == 0 {
		maxBatch = DefaultMaxInputBatch
	}
	conn, err := websocket.Accept(w, r, &websocket.AcceptOptions{Subprotocols: []string{GameV1Subprotocol}, CompressionMode: websocket.CompressionDisabled})
	if err != nil {
		return
	}
	defer conn.CloseNow()
	conn.SetReadLimit(int64(maxFrame))
	if conn.Subprotocol() != GameV1Subprotocol {
		_ = conn.Close(websocket.StatusPolicyViolation, "subprotocol required")
		return
	}
	if h.Verifier == nil {
		h.Verifier = RejectingTicketVerifier{}
	}

	ctx := r.Context()
	client, err := readClient(ctx, conn)
	if err != nil {
		_ = conn.Close(websocket.StatusUnsupportedData, "bad frame")
		return
	}
	hello := client.GetHello()
	if hello == nil || hello.GetProtocol() != GameV1Subprotocol || hello.GetSupportedReconnectGraceSeconds() != ReconnectGraceSeconds || hello.GetContentReleaseId() != h.ActiveContent.ContentReleaseID || ContentDigestFromProto(hello.GetAcceptedContent()) != h.ActiveContent || !activeRuntimePolicyOK(h.ActiveContent, h.SkillPolicy) {
		_ = h.writeError(ctx, conn, nil, client, "CONTENT_MISMATCH", ErrContentRejected.Error(), false)
		_ = conn.Close(websocket.StatusPolicyViolation, "content mismatch")
		return
	}
	auth, err := h.Verifier.VerifyTicket(ctx, hello.GetTicket())
	if err != nil || auth.Runtime == nil || auth.Session == nil {
		_ = h.writeError(ctx, conn, nil, client, "AUTH_REJECTED", ErrAuthRejected.Error(), false)
		_ = conn.Close(websocket.StatusPolicyViolation, "auth rejected")
		return
	}

	epoch := auth.SessionEpoch
	if epoch == 0 {
		epoch = hello.GetResumeSessionEpoch()
	}
	resume := session.ResumeNewSession
	var replay []session.ServerEvent
	if hello.GetResumeSessionEpoch() != 0 {
		var recErr error
		resume, replay, recErr = auth.Session.Reconnect(hello.GetResumeSessionEpoch(), hello.GetLastAppliedServerSeq(), uint64(auth.Runtime.Tick()))
		if recErr != nil && resume != session.ResumeGraceExpired {
			_ = h.writeError(ctx, conn, auth.Session, client, "RECONNECT_FAILED", recErr.Error(), false)
			return
		}
	}
	serverHello := &gamev1.ServerEnvelope{RequestId: client.GetRequestId(), SessionEpoch: epoch, ServerTick: uint64(auth.Runtime.Tick()), Payload: &gamev1.ServerEnvelope_Hello{Hello: &gamev1.ServerHello{Protocol: GameV1Subprotocol, RealmId: auth.RealmID, CharacterId: auth.CharacterID, ContentReleaseId: h.ActiveContent.ContentReleaseID, TickRateHz: combat.TickRateHz, InitialTick: uint64(auth.Runtime.Tick()), SessionEpoch: epoch, MaxInputBatch: maxBatch, MaxFrameBytes: maxFrame, Resumed: resume == session.ResumeDeltaReplay, ResumeOutcome: ResumeOutcomeToProto(resume), ActiveContent: ContentDigestToProto(h.ActiveContent), ReconnectGraceSeconds: ReconnectGraceSeconds, SkillPolicy: RuntimeSkillPolicyToProto(h.SkillPolicy)}}}
	if err := h.writeServer(ctx, conn, auth.Session, serverHello); err != nil {
		return
	}
	for _, event := range replay {
		if len(event.Data) != 0 {
			if err := conn.Write(ctx, websocket.MessageBinary, event.Data); err != nil {
				return
			}
		}
	}
	if resume == session.ResumeFullSnapshot || resume == session.ResumeGraceExpired {
		snap := &gamev1.ServerEnvelope{RequestId: client.GetRequestId(), SessionEpoch: epoch, ServerTick: uint64(auth.Runtime.Tick()), Payload: &gamev1.ServerEnvelope_Snapshot{Snapshot: &gamev1.WorldSnapshot{BaselineTick: uint64(auth.Runtime.Tick()), Full: true}}}
		if err := h.writeServer(ctx, conn, auth.Session, snap); err != nil {
			return
		}
	}

	svc := Service{Runtime: auth.Runtime, Session: auth.Session}
	for {
		client, err = readClient(ctx, conn)
		if err != nil {
			if websocket.CloseStatus(err) == websocket.StatusNormalClosure {
				return
			}
			_ = conn.Close(websocket.StatusUnsupportedData, "bad frame")
			return
		}
		if ping := client.GetPing(); ping != nil {
			pong := &gamev1.ServerEnvelope{RequestId: client.GetRequestId(), SessionEpoch: client.GetSessionEpoch(), ServerTick: uint64(auth.Runtime.Tick()), Payload: &gamev1.ServerEnvelope_Pong{Pong: &gamev1.Pong{ClientTimeMs: ping.GetClientTimeMs(), ServerTimeMs: uint64(time.Now().UnixMilli())}}}
			if err := h.writeServer(ctx, conn, auth.Session, pong); err != nil {
				return
			}
			continue
		}
		if resync := client.GetResync(); resync != nil {
			if resync.GetIncludeActiveCombat() {
				state := combat.ActiveCombatResyncState{BaselineTick: auth.Runtime.Tick(), Full: true}
				if ar, ok := auth.Runtime.(interface {
					ActiveResyncState() combat.ActiveCombatResyncState
				}); ok {
					state = ar.ActiveResyncState()
				}
				msg := &gamev1.ServerEnvelope{RequestId: client.GetRequestId(), SessionEpoch: client.GetSessionEpoch(), ServerTick: uint64(auth.Runtime.Tick()), Payload: &gamev1.ServerEnvelope_ActiveCombatResync{ActiveCombatResync: ActiveResyncToProto(state)}}
				if err := h.writeServer(ctx, conn, auth.Session, msg); err != nil {
					return
				}
			}
			continue
		}
		if err := validateBatch(client.GetInputBatch(), maxBatch, uint64(auth.Runtime.Tick())); err != nil {
			_ = h.writeError(ctx, conn, auth.Session, client, "BAD_INPUT", err.Error(), false)
			continue
		}
		for _, input := range client.GetInputBatch().GetInputs() {
			single := proto.Clone(client).(*gamev1.ClientEnvelope)
			single.Payload = &gamev1.ClientEnvelope_InputBatch{InputBatch: &gamev1.InputBatch{Inputs: []*gamev1.PlayerInput{input}}}
			domainIn, err := ProtoInputToClientEnvelope(single, h.ActiveContent)
			if err != nil {
				_ = h.writeError(ctx, conn, auth.Session, client, "UNSUPPORTED_PAYLOAD", err.Error(), false)
				continue
			}
			out := svc.HandleCore(ctx, domainIn)
			msg := ServerEnvelopeToProto(out)
			if err := h.writeServer(ctx, conn, auth.Session, msg); err != nil {
				return
			}
			for _, event := range out.CombatEvents {
				e := &gamev1.ServerEnvelope{RequestId: out.RequestID, SessionEpoch: out.SessionEpoch, ServerTick: uint64(event.Tick), Payload: &gamev1.ServerEnvelope_Combat{Combat: CombatEventToProto(event)}}
				if err := h.writeServer(ctx, conn, auth.Session, e); err != nil {
					return
				}
			}
		}
	}
}

func readClient(ctx context.Context, conn *websocket.Conn) (*gamev1.ClientEnvelope, error) {
	typ, data, err := conn.Read(ctx)
	if err != nil {
		return nil, err
	}
	if typ != websocket.MessageBinary {
		return nil, errors.New("want binary")
	}
	var msg gamev1.ClientEnvelope
	if err := decodeDelimited(data, &msg); err != nil {
		return nil, err
	}
	return &msg, nil
}

func (h GameV1Handler) writeError(ctx context.Context, conn *websocket.Conn, s Session, in *gamev1.ClientEnvelope, code, detail string, retryable bool) error {
	return h.writeServer(ctx, conn, s, &gamev1.ServerEnvelope{RequestId: in.GetRequestId(), SessionEpoch: in.GetSessionEpoch(), ServerTick: 0, Payload: &gamev1.ServerEnvelope_Error{Error: &gamev1.Error{Code: code, Detail: detail, Retryable: retryable}}})
}

func (h GameV1Handler) writeServer(ctx context.Context, conn *websocket.Conn, s Session, msg *gamev1.ServerEnvelope) error {
	build := func(seq uint64) ([]byte, error) {
		msg.ServerSeq = seq
		return encodeDelimited(msg)
	}
	var data []byte
	if ss, ok := s.(interface {
		AppendServerEventWithSeq(uint64, func(uint64) ([]byte, error)) (session.ServerEvent, error)
	}); ok {
		event, err := ss.AppendServerEventWithSeq(msg.GetServerTick(), build)
		if err != nil {
			return err
		}
		data = event.Data
	} else {
		var err error
		data, err = build(msg.GetServerSeq())
		if err != nil {
			return err
		}
		if s != nil {
			s.AppendServerEvent(msg.GetServerTick(), data)
		}
	}
	return conn.Write(ctx, websocket.MessageBinary, data)
}

func encodeDelimited(m proto.Message) ([]byte, error) {
	payload, err := proto.Marshal(m)
	if err != nil {
		return nil, err
	}
	buf := make([]byte, binary.MaxVarintLen64+len(payload))
	n := binary.PutUvarint(buf, uint64(len(payload)))
	return append(buf[:n], payload...), nil
}

func decodeDelimited(data []byte, m proto.Message) error {
	n, used := binary.Uvarint(data)
	if used <= 0 || uint64(len(data)-used) != n {
		return fmt.Errorf("bad delimited frame")
	}
	return proto.Unmarshal(data[used:], m)
}

func activeRuntimePolicyOK(d catalog.ContentDigest, p catalog.RuntimeSkillPolicy) bool {
	return d.ClientProjectionSHA256 != "" && p.PolicyID == d.RuntimeSkillPolicyID && p.CatalogUnionSize == d.CatalogUnionSize && p.CatalogUnionSHA256 == d.CatalogUnionSHA256 && !p.FilesystemFallbackAllowed && !p.RuntimeParityClaimed
}

func validateBatch(batch *gamev1.InputBatch, max uint32, nowTick uint64) error {
	if batch == nil || len(batch.GetInputs()) == 0 || uint32(len(batch.GetInputs())) > max {
		return fmt.Errorf("bad input batch size")
	}
	seen := map[string]struct{}{}
	var prev uint64
	for idx, input := range batch.GetInputs() {
		if idx > 0 && input.GetInputSeq() <= prev {
			return fmt.Errorf("inputs not strictly ordered")
		}
		prev = input.GetInputSeq()
		if input.GetCommandId() == "" {
			return fmt.Errorf("empty command_id")
		}
		if _, ok := seen[input.GetCommandId()]; ok {
			return fmt.Errorf("duplicate command_id")
		}
		seen[input.GetCommandId()] = struct{}{}
		if input.GetTargetTick() > nowTick+6 {
			return fmt.Errorf("target_tick too far")
		}
	}
	return nil
}
