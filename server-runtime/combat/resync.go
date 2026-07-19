package combat

import "sort"

type ActiveCombatResyncState struct {
	BaselineTick   Tick
	ActiveStatuses []ActiveStatusState
	Full           bool
}

type ActiveStatusState struct {
	TargetID EntityID
	Status   StatusDelta
}

func (i *Instance) ActiveResyncState() ActiveCombatResyncState {
	out := ActiveCombatResyncState{BaselineTick: i.tick, Full: true}
	ids := make([]string, 0, len(i.actors))
	for id := range i.actors {
		ids = append(ids, string(id))
	}
	sort.Strings(ids)
	for _, id := range ids {
		actor := i.actors[EntityID(id)]
		statusIDs := make([]int, 0, len(actor.Statuses))
		for statusID := range actor.Statuses {
			statusIDs = append(statusIDs, int(statusID))
		}
		sort.Ints(statusIDs)
		for _, statusID := range statusIDs {
			status := actor.Statuses[uint32(statusID)]
			out.ActiveStatuses = append(out.ActiveStatuses, ActiveStatusState{TargetID: actor.ID, Status: StatusDelta{EffectID: status.ID, Stacks: status.Stacks, ExpiresAtTick: status.ExpiresAt}})
		}
	}
	return out
}
