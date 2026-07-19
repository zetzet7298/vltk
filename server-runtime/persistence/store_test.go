package persistence

import (
	"context"
	"testing"
)

func TestMemoryStoreCheckpointAndIdempotentCommandLog(t *testing.T) {
	ctx := context.Background()
	s := NewMemoryStore()
	cmd := CommandRecord{InstanceID: "i", Epoch: 1, ClientSeq: 1, CommandID: "c1", AtTick: 2, Payload: []byte("cast")}
	if err := s.AppendCommand(ctx, cmd); err != nil {
		t.Fatal(err)
	}
	if err := s.AppendCommand(ctx, cmd); err != nil {
		t.Fatalf("duplicate must be idempotent: %v", err)
	}
	cmd.Payload = []byte("other")
	if err := s.AppendCommand(ctx, cmd); err == nil {
		t.Fatalf("conflict must fail")
	}
	if err := s.SaveCheckpoint(ctx, CheckpointRecord{InstanceID: "i", Tick: 90, Checksum: "a", Payload: []byte("old")}); err != nil {
		t.Fatal(err)
	}
	if err := s.SaveCheckpoint(ctx, CheckpointRecord{InstanceID: "i", Tick: 180, Checksum: "b", Payload: []byte("new")}); err != nil {
		t.Fatal(err)
	}
	latest, err := s.LoadLatestCheckpoint(ctx, "i")
	if err != nil || latest.Tick != 180 || string(latest.Payload) != "new" {
		t.Fatalf("latest=%+v err=%v", latest, err)
	}
}
