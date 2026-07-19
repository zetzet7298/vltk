package persistence

import (
	"context"
	"errors"
	"fmt"
	"sort"
	"sync"
)

var ErrNotFound = errors.New("persistence: not found")

type CommandRecord struct {
	InstanceID string
	Epoch      uint64
	ClientSeq  uint64
	CommandID  string
	AtTick     uint64
	Payload    []byte
}

type OutcomeRecord struct {
	InstanceID string
	CommandID  string
	ClientSeq  uint64
	Code       string
	Outcome    string
	AtTick     uint64
	Payload    []byte
}

type CheckpointRecord struct {
	InstanceID  string
	ReleaseID   string
	ReleaseHash string
	Tick        uint64
	Checksum    string
	Payload     []byte
}

type TraceMetadata struct {
	InstanceID     string
	Tick           uint64
	SampleRatePPM  uint32
	RNGAuditToken  []byte
	ContentRelease string
	ContentHash    string
}

type CommandLog interface {
	AppendCommand(context.Context, CommandRecord) error
	LoadCommandsAfter(context.Context, string, uint64) ([]CommandRecord, error)
}

type CheckpointStore interface {
	SaveCheckpoint(context.Context, CheckpointRecord) error
	LoadLatestCheckpoint(context.Context, string) (CheckpointRecord, error)
}

type OutcomeStore interface {
	UpsertOutcome(context.Context, OutcomeRecord) error
	GetOutcome(context.Context, string, string) (OutcomeRecord, error)
}

type TraceStore interface {
	StoreTraceMetadata(context.Context, TraceMetadata) error
}

type Store interface {
	CommandLog
	CheckpointStore
	OutcomeStore
	TraceStore
}

type MemoryStore struct {
	mu          sync.Mutex
	commands    map[string]map[string]CommandRecord
	checkpoints map[string][]CheckpointRecord
	outcomes    map[string]map[string]OutcomeRecord
	traces      []TraceMetadata
}

func NewMemoryStore() *MemoryStore {
	return &MemoryStore{commands: map[string]map[string]CommandRecord{}, checkpoints: map[string][]CheckpointRecord{}, outcomes: map[string]map[string]OutcomeRecord{}}
}

func (s *MemoryStore) AppendCommand(_ context.Context, r CommandRecord) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	if s.commands[r.InstanceID] == nil {
		s.commands[r.InstanceID] = map[string]CommandRecord{}
	}
	key := commandKey(r)
	if existing, ok := s.commands[r.InstanceID][key]; ok {
		if existing.CommandID == r.CommandID && existing.ClientSeq == r.ClientSeq && string(existing.Payload) == string(r.Payload) {
			return nil
		}
		return errors.New("persistence: conflicting command idempotency key")
	}
	r.Payload = append([]byte(nil), r.Payload...)
	s.commands[r.InstanceID][key] = r
	return nil
}

func (s *MemoryStore) LoadCommandsAfter(_ context.Context, instanceID string, tick uint64) ([]CommandRecord, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	var out []CommandRecord
	for _, r := range s.commands[instanceID] {
		if r.AtTick > tick {
			r.Payload = append([]byte(nil), r.Payload...)
			out = append(out, r)
		}
	}
	sort.Slice(out, func(a, b int) bool {
		if out[a].AtTick == out[b].AtTick {
			return out[a].ClientSeq < out[b].ClientSeq
		}
		return out[a].AtTick < out[b].AtTick
	})
	return out, nil
}

func (s *MemoryStore) SaveCheckpoint(_ context.Context, r CheckpointRecord) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	r.Payload = append([]byte(nil), r.Payload...)
	s.checkpoints[r.InstanceID] = append(s.checkpoints[r.InstanceID], r)
	return nil
}

func (s *MemoryStore) LoadLatestCheckpoint(_ context.Context, instanceID string) (CheckpointRecord, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	items := s.checkpoints[instanceID]
	if len(items) == 0 {
		return CheckpointRecord{}, ErrNotFound
	}
	latest := items[0]
	for _, item := range items[1:] {
		if item.Tick > latest.Tick {
			latest = item
		}
	}
	latest.Payload = append([]byte(nil), latest.Payload...)
	return latest, nil
}

func (s *MemoryStore) UpsertOutcome(_ context.Context, r OutcomeRecord) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	if s.outcomes[r.InstanceID] == nil {
		s.outcomes[r.InstanceID] = map[string]OutcomeRecord{}
	}
	r.Payload = append([]byte(nil), r.Payload...)
	s.outcomes[r.InstanceID][r.CommandID] = r
	return nil
}

func (s *MemoryStore) GetOutcome(_ context.Context, instanceID, commandID string) (OutcomeRecord, error) {
	s.mu.Lock()
	defer s.mu.Unlock()
	r, ok := s.outcomes[instanceID][commandID]
	if !ok {
		return OutcomeRecord{}, ErrNotFound
	}
	r.Payload = append([]byte(nil), r.Payload...)
	return r, nil
}

func (s *MemoryStore) StoreTraceMetadata(_ context.Context, r TraceMetadata) error {
	s.mu.Lock()
	defer s.mu.Unlock()
	r.RNGAuditToken = append([]byte(nil), r.RNGAuditToken...)
	s.traces = append(s.traces, r)
	return nil
}

func commandKey(r CommandRecord) string {
	if r.CommandID != "" {
		return r.CommandID
	}
	return fmt.Sprintf("%d:%d", r.Epoch, r.ClientSeq)
}
