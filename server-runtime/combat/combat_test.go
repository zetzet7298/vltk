package combat

import (
	"bytes"
	"encoding/json"
	"fmt"
	"testing"

	"vltk.dev/server-runtime/catalog"
)

const hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"

func testRelease() catalog.Release {
	return catalog.Release{ID: "rel-1", Hash: hash, Policy: catalog.FeaturePolicy{Global: catalog.PolicyEnabled, Skill: catalog.PolicyEnabled, Faction: catalog.PolicyEnabled}, Skills: map[catalog.SkillKey]catalog.Skill{
		{ID: 10, Level: 1}: {
			ID: 10, Level: 1, ManaCost: 10, CastTicks: 2, RecoveryTicks: 2, CooldownTicks: 5, RangeMilli: 1000,
			MissileID: 77, MissileSpeedPerTick: 90, AnimationID: 3, VisualEffectID: 4,
			Effects: []catalog.Effect{
				{Kind: catalog.EffectDamage, Value: 25, School: catalog.SchoolFire, ChancePermille: 1000},
				{Kind: catalog.EffectStatus, Status: catalog.Status{ID: 33, DurationTicks: 3, MaxStacks: 2}},
			},
		},
	}}
}

func testActors() []Actor {
	return []Actor{
		{ID: "caster", Faction: FactionPlayer, Pos: Vec2{X: 0, Y: 0}, HP: 100, MaxHP: 100, Mana: 100, MaxMana: 100},
		{ID: "target", Faction: FactionMonster, Pos: Vec2{X: 180, Y: 0}, HP: 100, MaxHP: 100, Mana: 20, MaxMana: 20},
	}
}

func testIntent(commandID string) CastIntent {
	return CastIntent{CommandID: commandID, SessionEpoch: 1, ClientSeq: 1, CasterID: "caster", SkillID: 10, SkillLevel: 1, TargetID: "target", ContentReleaseID: "rel-1", ContentHash: hash}
}

func newTestInstance(t *testing.T) *Instance {
	t.Helper()
	i, err := NewInstance("inst", testRelease(), 123, WithAuditKey([]byte("audit-key")))
	if err != nil {
		t.Fatal(err)
	}
	for _, actor := range testActors() {
		if err := i.AddActor(actor); err != nil {
			t.Fatal(err)
		}
	}
	return i
}

func TestCastCostCooldownMissileStatusLifecycle(t *testing.T) {
	i := newTestInstance(t)
	result := i.ProcessCast(testIntent("cmd-1"))
	if result.Outcome != OutcomeScheduled {
		t.Fatalf("result=%+v", result)
	}
	start := i.DrainEvents()
	if len(start) != 1 || start[0].Kind != EventCastStarted || len(start[0].RNGAuditToken) != 16 {
		t.Fatalf("start=%+v", start)
	}
	caster, _ := i.Actor("caster")
	if caster.Mana != 90 {
		t.Fatalf("mana=%d", caster.Mana)
	}
	if dup := i.ProcessCast(testIntent("cmd-1")); dup != result {
		t.Fatalf("duplicate changed result: %+v", dup)
	}
	busy := testIntent("cmd-busy")
	busy.ClientSeq = 2
	if got := i.ProcessCast(busy); got.Code != "COOLDOWN" {
		t.Fatalf("want cooldown, got %+v", got)
	}
	events := i.Advance(4)
	want := []EventKind{EventMissileSpawned, EventRecoveryEnded, EventMissileCollided, EventHit, EventStatusApplied, EventMissileVanished}
	if kinds(events).String() != eventKinds(want).String() {
		t.Fatalf("events=%v", kinds(events))
	}
	target, _ := i.Actor("target")
	if target.HP != 75 || target.Statuses[33].Stacks != 1 {
		t.Fatalf("target=%+v", target)
	}
	expired := i.Advance(3)
	if len(expired) != 1 || expired[0].Kind != EventStatusExpired {
		t.Fatalf("expired=%+v", expired)
	}
}

func TestDeterministicReplay(t *testing.T) {
	commands := []ReplayCommand{{At: 0, Intent: testIntent("cmd-1")}}
	a, err := Replay(testRelease(), "inst", 123, testActors(), commands, 7, WithAuditKey([]byte("audit-key")))
	if err != nil {
		t.Fatal(err)
	}
	b, err := Replay(testRelease(), "inst", 123, testActors(), commands, 7, WithAuditKey([]byte("audit-key")))
	if err != nil {
		t.Fatal(err)
	}
	aj, _ := json.Marshal(a)
	bj, _ := json.Marshal(b)
	if !bytes.Equal(aj, bj) {
		t.Fatalf("replay diverged\n%s\n%s", aj, bj)
	}
}

func TestCheckpointRestoreAndCommandReplaySeam(t *testing.T) {
	i := newTestInstance(t)
	_ = i.ProcessCast(testIntent("cmd-1"))
	_ = i.DrainEvents()
	_ = i.Advance(2)
	_, blob, err := i.Checkpoint()
	if err != nil {
		t.Fatal(err)
	}
	restored, err := RestoreCheckpoint(testRelease(), blob, WithAuditKey([]byte("audit-key")))
	if err != nil {
		t.Fatal(err)
	}
	origEvents := i.Advance(5)
	restoredEvents := restored.Advance(5)
	aj, _ := json.Marshal(origEvents)
	bj, _ := json.Marshal(restoredEvents)
	if !bytes.Equal(aj, bj) {
		t.Fatalf("restore diverged\n%s\n%s", aj, bj)
	}
	if dup := restored.ProcessCast(testIntent("cmd-1")); dup.Outcome != OutcomeScheduled {
		t.Fatalf("restored idempotency lost: %+v", dup)
	}
}

func TestCapacityAndContentMismatchFailClosed(t *testing.T) {
	i := newTestInstance(t)
	for n := 0; n < MaxActors-2; n++ {
		if err := i.AddActor(Actor{ID: EntityID(fmt.Sprintf("a%d", n+1)), HP: 1, MaxHP: 1}); err != nil {
			t.Fatal(err)
		}
	}
	if err := i.AddActor(Actor{ID: "too-many", HP: 1, MaxHP: 1}); err != ErrCapacity {
		t.Fatalf("want capacity, got %v", err)
	}
	bad := testIntent("bad")
	bad.ContentHash = "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff"
	if got := i.ProcessCast(bad); got.Code != "CONTENT_MISMATCH" {
		t.Fatalf("got %+v", got)
	}
}

type eventKinds []EventKind

func kinds(events []Event) eventKinds {
	out := make(eventKinds, len(events))
	for idx, event := range events {
		out[idx] = event.Kind
	}
	return out
}

func (k eventKinds) String() string {
	b, _ := json.Marshal(k)
	return string(b)
}
