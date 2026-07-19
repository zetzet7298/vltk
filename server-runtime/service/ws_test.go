package service

import (
	"context"
	"errors"
	"net/http/httptest"
	"testing"
	"time"

	"nhooyr.io/websocket"

	"vltk.dev/server-runtime/catalog"
	"vltk.dev/server-runtime/combat"
	gamev1 "vltk.dev/server-runtime/gen/game/v1"
	"vltk.dev/server-runtime/session"
)

const testHash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"

func TestGameV1WebSocketCastThroughCore(t *testing.T) {
	active := testDigest()
	runtime := newRuntime(t, active)
	sess := session.New(7, 8)
	server := httptest.NewServer(GameV1Handler{ActiveContent: active, SkillPolicy: testPolicy(active), Verifier: staticVerifier{runtime: runtime, session: sess}})
	defer server.Close()

	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()
	conn, _, err := websocket.Dial(ctx, "ws"+server.URL[len("http"):], &websocket.DialOptions{Subprotocols: []string{GameV1Subprotocol}})
	if err != nil {
		t.Fatal(err)
	}
	defer conn.CloseNow()

	writeClient(t, ctx, conn, &gamev1.ClientEnvelope{RequestId: "hello", Payload: &gamev1.ClientEnvelope_Hello{Hello: &gamev1.ClientHello{Protocol: GameV1Subprotocol, Ticket: "ticket", ContentReleaseId: active.ContentReleaseID, AcceptedContent: ContentDigestToProto(active), SupportedReconnectGraceSeconds: ReconnectGraceSeconds}}})
	hello := readServer(t, ctx, conn)
	if got := hello.GetHello(); got == nil || got.GetProtocol() != GameV1Subprotocol || got.GetActiveContent().GetManifestSha256() != active.ManifestSHA256 || got.GetSessionEpoch() != 7 {
		t.Fatalf("bad hello: %+v", hello)
	}

	writeClient(t, ctx, conn, &gamev1.ClientEnvelope{RequestId: "cast", SessionEpoch: 7, ClientSeq: 1, Payload: &gamev1.ClientEnvelope_InputBatch{InputBatch: &gamev1.InputBatch{Inputs: []*gamev1.PlayerInput{{InputSeq: 1, TargetTick: 0, CommandId: "cmd-1", Command: &gamev1.PlayerInput_CastSkill{CastSkill: &gamev1.CastSkillInput{SkillId: 10, SkillLevel: 1, TargetEntityId: "target"}}}}}}})
	result := readServer(t, ctx, conn)
	if got := result.GetCommandResult(); got == nil || got.GetOutcome() != gamev1.CommandOutcome_COMMAND_OUTCOME_SCHEDULED || got.GetInputSeq() != 1 || result.GetLastProcessedClientSeq() != 1 {
		t.Fatalf("bad result: %+v", result)
	}
	event := readServer(t, ctx, conn)
	if got := event.GetCombat(); got == nil || got.GetKind() != gamev1.CombatEventKind_COMBAT_EVENT_KIND_CAST_STARTED || got.GetSkillId() != 10 {
		t.Fatalf("bad combat event: %+v", event)
	}
}

func TestGameV1WebSocketContentMismatchFailsClosed(t *testing.T) {
	active := testDigest()
	server := httptest.NewServer(GameV1Handler{ActiveContent: active, SkillPolicy: testPolicy(active), Verifier: staticVerifier{}})
	defer server.Close()
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()
	conn, _, err := websocket.Dial(ctx, "ws"+server.URL[len("http"):], &websocket.DialOptions{Subprotocols: []string{GameV1Subprotocol}})
	if err != nil {
		t.Fatal(err)
	}
	defer conn.CloseNow()
	bad := active
	bad.ManifestSHA256 = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
	writeClient(t, ctx, conn, &gamev1.ClientEnvelope{RequestId: "hello", Payload: &gamev1.ClientEnvelope_Hello{Hello: &gamev1.ClientHello{Protocol: GameV1Subprotocol, Ticket: "ticket", ContentReleaseId: active.ContentReleaseID, AcceptedContent: ContentDigestToProto(bad), SupportedReconnectGraceSeconds: ReconnectGraceSeconds}}})
	assertContentMismatchClose(t, ctx, conn)
}

func TestGameV1WebSocketRequiresClientProjectionDigestTag7(t *testing.T) {
	active := testDigest()
	server := httptest.NewServer(GameV1Handler{ActiveContent: active, SkillPolicy: testPolicy(active), Verifier: staticVerifier{}})
	defer server.Close()
	ctx, cancel := context.WithTimeout(context.Background(), 3*time.Second)
	defer cancel()
	conn, _, err := websocket.Dial(ctx, "ws"+server.URL[len("http"):], &websocket.DialOptions{Subprotocols: []string{GameV1Subprotocol}})
	if err != nil {
		t.Fatal(err)
	}
	defer conn.CloseNow()
	bad := active
	bad.ClientProjectionSHA256 = ""
	writeClient(t, ctx, conn, &gamev1.ClientEnvelope{RequestId: "hello", Payload: &gamev1.ClientEnvelope_Hello{Hello: &gamev1.ClientHello{Protocol: GameV1Subprotocol, Ticket: "ticket", ContentReleaseId: active.ContentReleaseID, AcceptedContent: ContentDigestToProto(bad), SupportedReconnectGraceSeconds: ReconnectGraceSeconds}}})
	assertContentMismatchClose(t, ctx, conn)
}

func assertContentMismatchClose(t *testing.T, ctx context.Context, conn *websocket.Conn) {
	t.Helper()
	msg := readServer(t, ctx, conn)
	if got := msg.GetError(); got == nil || got.GetCode() != "CONTENT_MISMATCH" {
		t.Fatalf("bad error: %+v", msg)
	}
	_, _, err := conn.Read(ctx)
	if websocket.CloseStatus(err) != websocket.StatusPolicyViolation {
		t.Fatalf("want policy close, got %v", err)
	}
}

func FuzzDecodeDelimitedRejectsGarbage(f *testing.F) {
	good, _ := encodeDelimited(&gamev1.ClientEnvelope{RequestId: "x"})
	f.Add(good)
	f.Add([]byte{5, 1, 2})
	f.Fuzz(func(t *testing.T, data []byte) {
		var msg gamev1.ClientEnvelope
		err := decodeDelimited(data, &msg)
		if err == nil {
			out, encErr := encodeDelimited(&msg)
			if encErr != nil || len(out) == 0 {
				t.Fatalf("round trip encode failed: %v", encErr)
			}
		}
	})
}

type staticVerifier struct {
	runtime *combat.Instance
	session *session.Session
}

func (v staticVerifier) VerifyTicket(context.Context, string) (AuthenticatedSession, error) {
	if v.runtime == nil || v.session == nil {
		return AuthenticatedSession{}, errors.New("no auth")
	}
	return AuthenticatedSession{RealmID: "realm", CharacterID: "char", SessionEpoch: v.session.Epoch, Runtime: v.runtime, Session: v.session}, nil
}

func testDigest() catalog.ContentDigest {
	return catalog.ContentDigest{ContentReleaseID: "rel-1", ManifestSHA256: testHash, CatalogUnionSize: catalog.SkillPortRows, CatalogUnionSHA256: testHash, RuntimeSkillPolicyID: "policy", ClientProjectionSHA256: testHash}
}

func testPolicy(d catalog.ContentDigest) catalog.RuntimeSkillPolicy {
	return catalog.RuntimeSkillPolicy{PolicyID: d.RuntimeSkillPolicyID, CatalogUnionSize: d.CatalogUnionSize, CatalogUnionSHA256: d.CatalogUnionSHA256, PCRuntimeEvidenceStatus: "BLOCKED", AndroidPhysicalEvidenceStatus: "BLOCKED"}
}

func newRuntime(t *testing.T, active catalog.ContentDigest) *combat.Instance {
	t.Helper()
	release := catalog.Release{ID: active.ContentReleaseID, Hash: active.ManifestSHA256, Policy: catalog.FeaturePolicy{Global: catalog.PolicyEnabled, Skill: catalog.PolicyEnabled, Faction: catalog.PolicyEnabled}, Skills: map[catalog.SkillKey]catalog.Skill{{ID: 10, Level: 1}: {ID: 10, Level: 1, ManaCost: 1, CastTicks: 1, RecoveryTicks: 1, CooldownTicks: 5, RangeMilli: 1000, Effects: []catalog.Effect{{Kind: catalog.EffectDamage, Value: 1, School: catalog.SchoolFire, ChancePermille: 1000}}}}}
	runtime, err := combat.NewInstance("inst", release, 1, combat.WithAuditKey([]byte("audit-key")))
	if err != nil {
		t.Fatal(err)
	}
	for _, actor := range []combat.Actor{{ID: "caster", Faction: combat.FactionPlayer, HP: 10, MaxHP: 10, Mana: 10, MaxMana: 10}, {ID: "target", Faction: combat.FactionMonster, Pos: combat.Vec2{X: 10}, HP: 10, MaxHP: 10}} {
		if err := runtime.AddActor(actor); err != nil {
			t.Fatal(err)
		}
	}
	return runtime
}

func writeClient(t *testing.T, ctx context.Context, conn *websocket.Conn, msg *gamev1.ClientEnvelope) {
	t.Helper()
	data, err := encodeDelimited(msg)
	if err != nil {
		t.Fatal(err)
	}
	if err := conn.Write(ctx, websocket.MessageBinary, data); err != nil {
		t.Fatal(err)
	}
}

func readServer(t *testing.T, ctx context.Context, conn *websocket.Conn) *gamev1.ServerEnvelope {
	t.Helper()
	typ, data, err := conn.Read(ctx)
	if err != nil {
		t.Fatal(err)
	}
	if typ != websocket.MessageBinary {
		t.Fatalf("want binary, got %v", typ)
	}
	var msg gamev1.ServerEnvelope
	if err := decodeDelimited(data, &msg); err != nil {
		t.Fatal(err)
	}
	return &msg
}
