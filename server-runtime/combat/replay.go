package combat

import "vltk.dev/server-runtime/catalog"

type ReplayCommand struct {
	At     Tick
	Intent CastIntent
}

type ReplayResult struct {
	FinalTick Tick
	Events    []Event
	Results   []CommandResult
}

func Replay(release catalog.Release, instanceID string, seed uint64, actors []Actor, commands []ReplayCommand, until Tick, opts ...Option) (ReplayResult, error) {
	i, err := NewInstance(instanceID, release, seed, opts...)
	if err != nil {
		return ReplayResult{}, err
	}
	for _, actor := range actors {
		if err := i.AddActor(actor); err != nil {
			return ReplayResult{}, err
		}
	}
	var out ReplayResult
	for _, command := range commands {
		if command.At > until {
			break
		}
		if command.At > i.tick {
			out.Events = append(out.Events, i.Advance(uint32(command.At-i.tick))...)
		}
		result := i.ProcessCast(command.Intent)
		out.Results = append(out.Results, result)
		out.Events = append(out.Events, i.DrainEvents()...)
	}
	if until > i.tick {
		out.Events = append(out.Events, i.Advance(uint32(until-i.tick))...)
	}
	out.FinalTick = i.tick
	return out, nil
}
