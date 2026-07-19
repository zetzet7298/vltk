package service

import (
	"context"
	"encoding/json"
	"errors"
	"net/http"

	"vltk.dev/server-runtime/combat"
	"vltk.dev/server-runtime/persistence"
	"vltk.dev/server-runtime/session"
)

var ErrUnsupportedPayload = errors.New("service: unsupported payload")

type ClientEnvelope struct {
	RequestID      string             `json:"request_id"`
	SessionEpoch   uint64             `json:"session_epoch"`
	ClientSeq      uint64             `json:"client_seq"`
	AckServerSeq   uint64             `json:"ack_server_seq"`
	Cast           *combat.CastIntent `json:"cast,omitempty"`
	LastAppliedSeq uint64             `json:"last_applied_seq,omitempty"`
	Reconnect      bool               `json:"reconnect,omitempty"`
}

type ServerEnvelope struct {
	RequestID              string                `json:"request_id"`
	SessionEpoch           uint64                `json:"session_epoch"`
	ServerSeq              uint64                `json:"server_seq"`
	LastProcessedClientSeq uint64                `json:"last_processed_client_seq"`
	ServerTick             uint64                `json:"server_tick"`
	CommandResult          *combat.CommandResult `json:"command_result,omitempty"`
	CombatEvents           []combat.Event        `json:"combat_events,omitempty"`
	ResumeOutcome          session.ResumeOutcome `json:"resume_outcome,omitempty"`
	Replay                 []session.ServerEvent `json:"replay,omitempty"`
	Error                  string                `json:"error,omitempty"`
}

type Runtime interface {
	Tick() combat.Tick
	ProcessCast(combat.CastIntent) combat.CommandResult
	DrainEvents() []combat.Event
}

type Session interface {
	Accept(epoch, clientSeq uint64, commandID string) session.CommandReceipt
	AppendServerEvent(tick uint64, data []byte) session.ServerEvent
	Reconnect(epoch, lastAppliedServerSeq, nowTick uint64) (session.ResumeOutcome, []session.ServerEvent, error)
}

type Service struct {
	Runtime Runtime
	Session Session
	Store   persistence.Store
}

func (s Service) Handle(ctx context.Context, in ClientEnvelope) ServerEnvelope {
	return s.append(s.HandleCore(ctx, in))
}

func (s Service) HandleCore(_ context.Context, in ClientEnvelope) ServerEnvelope {
	out := ServerEnvelope{RequestID: in.RequestID, SessionEpoch: in.SessionEpoch, ServerTick: uint64(s.Runtime.Tick())}
	if in.Reconnect {
		outcome, replay, err := s.Session.Reconnect(in.SessionEpoch, in.LastAppliedSeq, uint64(s.Runtime.Tick()))
		out.ResumeOutcome = outcome
		out.Replay = replay
		if err != nil {
			out.Error = err.Error()
		}
		return out
	}
	if in.Cast == nil {
		out.Error = ErrUnsupportedPayload.Error()
		return out
	}
	receipt := s.Session.Accept(in.SessionEpoch, in.ClientSeq, in.Cast.CommandID)
	out.LastProcessedClientSeq = receipt.LastSeq
	if receipt.Disposition == session.CommandOutOfOrder || receipt.Disposition == session.CommandEpochMismatch {
		out.Error = receipt.Disposition.String()
		return out
	}
	in.Cast.SessionEpoch = in.SessionEpoch
	in.Cast.ClientSeq = in.ClientSeq
	result := s.Runtime.ProcessCast(*in.Cast)
	out.CommandResult = &result
	out.CombatEvents = append(out.CombatEvents, s.Runtime.DrainEvents()...)
	return out
}

func (s Service) append(out ServerEnvelope) ServerEnvelope {
	blob, _ := json.Marshal(out)
	event := s.Session.AppendServerEvent(out.ServerTick, blob)
	out.ServerSeq = event.Seq
	return out
}

func HealthHandler(w http.ResponseWriter, _ *http.Request) {
	w.Header().Set("content-type", "application/json")
	_, _ = w.Write([]byte(`{"ok":true,"tick_rate_hz":18}`))
}
