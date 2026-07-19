package combat

import (
	"crypto/sha256"
	"encoding/hex"
	"encoding/json"
	"sort"

	"vltk.dev/server-runtime/catalog"
)

type Checkpoint struct {
	InstanceID    string                   `json:"instance_id"`
	ReleaseID     string                   `json:"release_id"`
	ReleaseHash   string                   `json:"release_hash"`
	Tick          Tick                     `json:"tick"`
	Actors        []Actor                  `json:"actors"`
	LightEntities int                      `json:"light_entities"`
	Scheduled     []scheduled              `json:"scheduled"`
	NextOrder     uint64                   `json:"next_order"`
	NextEventSeq  uint64                   `json:"next_event_seq"`
	Processed     map[string]CommandResult `json:"processed"`
	RNGState      uint64                   `json:"rng_state"`
	Checksum      string                   `json:"checksum"`
}

func (i *Instance) Checkpoint() (Checkpoint, []byte, error) {
	cp := Checkpoint{
		InstanceID:    i.id,
		ReleaseID:     i.release.ID,
		ReleaseHash:   i.release.Hash,
		Tick:          i.tick,
		LightEntities: i.lightEntities,
		Scheduled:     append([]scheduled(nil), i.scheduled...),
		NextOrder:     i.nextOrder,
		NextEventSeq:  i.nextEventSeq,
		RNGState:      i.rng.state,
		Processed:     map[string]CommandResult{},
	}
	for _, actor := range i.actors {
		cp.Actors = append(cp.Actors, cloneActor(*actor))
	}
	sort.Slice(cp.Actors, func(a, b int) bool { return cp.Actors[a].ID < cp.Actors[b].ID })
	for key, result := range i.processed {
		cp.Processed[key] = result
	}
	sort.Slice(cp.Scheduled, func(a, b int) bool {
		if cp.Scheduled[a].At == cp.Scheduled[b].At {
			return cp.Scheduled[a].Order < cp.Scheduled[b].Order
		}
		return cp.Scheduled[a].At < cp.Scheduled[b].At
	})
	blob, err := json.Marshal(cp.withoutChecksum())
	if err != nil {
		return Checkpoint{}, nil, err
	}
	sum := sha256.Sum256(blob)
	cp.Checksum = hex.EncodeToString(sum[:])
	blob, err = json.Marshal(cp)
	return cp, blob, err
}

func RestoreCheckpoint(release catalog.Release, blob []byte, opts ...Option) (*Instance, error) {
	var cp Checkpoint
	if err := json.Unmarshal(blob, &cp); err != nil {
		return nil, err
	}
	checksum := cp.Checksum
	cp.Checksum = ""
	checkBlob, err := json.Marshal(cp.withoutChecksum())
	if err != nil {
		return nil, err
	}
	sum := sha256.Sum256(checkBlob)
	if checksum != hex.EncodeToString(sum[:]) {
		return nil, ErrBadCheckpoint
	}
	if cp.ReleaseID != release.ID || cp.ReleaseHash != release.Hash {
		return nil, ErrContentMismatch
	}
	i, err := NewInstance(cp.InstanceID, release, cp.RNGState, opts...)
	if err != nil {
		return nil, err
	}
	i.tick = cp.Tick
	i.lightEntities = cp.LightEntities
	i.scheduled = append([]scheduled(nil), cp.Scheduled...)
	i.nextOrder = cp.NextOrder
	i.nextEventSeq = cp.NextEventSeq
	i.rng.state = cp.RNGState
	for _, actor := range cp.Actors {
		copyActor := cloneActor(actor)
		if copyActor.Cooldowns == nil {
			copyActor.Cooldowns = make(map[uint32]Tick)
		}
		if copyActor.Statuses == nil {
			copyActor.Statuses = make(map[uint32]Status)
		}
		i.actors[actor.ID] = &copyActor
	}
	for key, result := range cp.Processed {
		i.processed[key] = result
	}
	return i, nil
}

func (cp Checkpoint) withoutChecksum() Checkpoint {
	cp.Checksum = ""
	return cp
}
