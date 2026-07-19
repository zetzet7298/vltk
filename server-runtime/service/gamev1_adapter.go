package service

import (
	"errors"

	"vltk.dev/server-runtime/catalog"
	"vltk.dev/server-runtime/combat"
	gamev1 "vltk.dev/server-runtime/gen/game/v1"
	"vltk.dev/server-runtime/session"
)

var ErrUnsupportedGameV1Payload = errors.New("service: unsupported game.v1 payload")

func ContentDigestToProto(d catalog.ContentDigest) *gamev1.ContentDigest {
	return &gamev1.ContentDigest{ContentReleaseId: d.ContentReleaseID, ManifestSha256: d.ManifestSHA256, SourceSnapshotId: d.SourceSnapshotID, CatalogUnionSize: d.CatalogUnionSize, CatalogUnionSha256: d.CatalogUnionSHA256, RuntimeSkillPolicyId: d.RuntimeSkillPolicyID, ClientProjectionSha256: d.ClientProjectionSHA256}
}

func ContentDigestFromProto(d *gamev1.ContentDigest) catalog.ContentDigest {
	if d == nil {
		return catalog.ContentDigest{}
	}
	return catalog.ContentDigest{ContentReleaseID: d.GetContentReleaseId(), ManifestSHA256: d.GetManifestSha256(), SourceSnapshotID: d.GetSourceSnapshotId(), CatalogUnionSize: d.GetCatalogUnionSize(), CatalogUnionSHA256: d.GetCatalogUnionSha256(), RuntimeSkillPolicyID: d.GetRuntimeSkillPolicyId(), ClientProjectionSHA256: d.GetClientProjectionSha256()}
}

func RuntimeSkillPolicyToProto(p catalog.RuntimeSkillPolicy) *gamev1.RuntimeSkillPolicy {
	return &gamev1.RuntimeSkillPolicy{PolicyId: p.PolicyID, CatalogUnionSize: p.CatalogUnionSize, CatalogUnionSha256: p.CatalogUnionSHA256, FilesystemFallbackAllowed: p.FilesystemFallbackAllowed, RuntimeParityClaimed: p.RuntimeParityClaimed, PcRuntimeEvidenceStatus: p.PCRuntimeEvidenceStatus, AndroidPhysicalEvidenceStatus: p.AndroidPhysicalEvidenceStatus}
}

func ProtoInputToClientEnvelope(in *gamev1.ClientEnvelope, active catalog.ContentDigest) (ClientEnvelope, error) {
	out := ClientEnvelope{RequestID: in.GetRequestId(), SessionEpoch: in.GetSessionEpoch(), ClientSeq: in.GetClientSeq(), AckServerSeq: in.GetAckServerSeq()}
	if resync := in.GetResync(); resync != nil {
		out.Reconnect = true
		out.LastAppliedSeq = in.GetAckServerSeq()
		return out, nil
	}
	batch := in.GetInputBatch()
	if batch == nil || len(batch.GetInputs()) != 1 {
		return out, ErrUnsupportedGameV1Payload
	}
	input := batch.GetInputs()[0]
	cast := input.GetCastSkill()
	if cast == nil || input.GetCommandId() == "" {
		return out, ErrUnsupportedGameV1Payload
	}
	out.Cast = &combat.CastIntent{CommandID: input.GetCommandId(), CasterID: "caster", SkillID: cast.GetSkillId(), SkillLevel: cast.GetSkillLevel(), TargetID: combat.EntityID(cast.GetTargetEntityId()), Aim: combat.Vec2{X: int64(cast.GetAimXQ()), Y: int64(cast.GetAimYQ())}, TargetTick: combat.Tick(input.GetTargetTick()), ContentReleaseID: active.ContentReleaseID, ContentHash: active.ManifestSHA256}
	if out.Cast.SkillLevel == 0 {
		out.Cast.SkillLevel = 1
	}
	return out, nil
}

func ServerEnvelopeToProto(out ServerEnvelope) *gamev1.ServerEnvelope {
	base := &gamev1.ServerEnvelope{RequestId: out.RequestID, SessionEpoch: out.SessionEpoch, ServerSeq: out.ServerSeq, LastProcessedClientSeq: out.LastProcessedClientSeq, ServerTick: out.ServerTick}
	if out.Error != "" {
		base.Payload = &gamev1.ServerEnvelope_Error{Error: &gamev1.Error{Code: out.Error, Detail: out.Error, Retryable: false}}
		return base
	}
	if out.CommandResult != nil {
		base.Payload = &gamev1.ServerEnvelope_CommandResult{CommandResult: CommandResultToProto(*out.CommandResult)}
		return base
	}
	if out.ResumeOutcome != 0 {
		base.Payload = &gamev1.ServerEnvelope_Snapshot{Snapshot: &gamev1.WorldSnapshot{BaselineTick: out.ServerTick, Full: out.ResumeOutcome == session.ResumeFullSnapshot}}
		return base
	}
	return base
}

func CommandResultToProto(r combat.CommandResult) *gamev1.CommandResult {
	return &gamev1.CommandResult{CommandId: r.CommandID, InputSeq: r.ClientSeq, Accepted: r.Outcome != combat.OutcomeRejected, Code: r.Code, Detail: r.Detail, Outcome: commandOutcomeToProto(r.Outcome), CommittedServerTick: uint64(r.Tick)}
}

func CombatEventToProto(e combat.Event) *gamev1.CombatEvent {
	out := &gamev1.CombatEvent{EventId: e.ID, ServerTick: uint64(e.Tick), SourceEntityId: string(e.SourceID), TargetEntityId: string(e.TargetID), SkillId: e.SkillID, SkillLevel: e.SkillLevel, HitIndex: e.HitIndex, Value: e.Value, DamageSchool: damageSchoolToProto(e.School), TargetHpAfter: e.TargetHPAfter, TargetManaAfter: e.TargetManaAfter, ImpactX: int32(e.Impact.X), ImpactY: int32(e.Impact.Y), MissileId: e.MissileID, AnimationId: e.AnimationID, VisualEffectId: e.VisualEffectID, CastId: e.CastID, RngAuditToken: append([]byte(nil), e.RNGAuditToken...), Kind: combatEventKindToProto(e.Kind)}
	for _, d := range e.StatusEffects {
		out.StatusEffects = append(out.StatusEffects, &gamev1.StatusEffectDelta{EffectId: d.EffectID, Stacks: d.Stacks, ExpiresAtTick: uint64(d.ExpiresAtTick), Removed: d.Removed})
	}
	for _, f := range e.Results {
		out.Results = append(out.Results, resultFlagToProto(f))
	}
	return out
}

func ActiveResyncToProto(s combat.ActiveCombatResyncState) *gamev1.ActiveCombatResyncState {
	out := &gamev1.ActiveCombatResyncState{BaselineTick: uint64(s.BaselineTick), Full: s.Full}
	for _, st := range s.ActiveStatuses {
		out.ActiveStatuses = append(out.ActiveStatuses, &gamev1.ActiveStatusState{TargetEntityId: string(st.TargetID), Status: &gamev1.StatusEffectDelta{EffectId: st.Status.EffectID, Stacks: st.Status.Stacks, ExpiresAtTick: uint64(st.Status.ExpiresAtTick), Removed: st.Status.Removed}})
	}
	return out
}

func ResumeOutcomeToProto(o session.ResumeOutcome) gamev1.ResumeOutcome {
	switch o {
	case session.ResumeNewSession:
		return gamev1.ResumeOutcome_RESUME_OUTCOME_NEW_SESSION
	case session.ResumeDeltaReplay:
		return gamev1.ResumeOutcome_RESUME_OUTCOME_DELTA_REPLAY
	case session.ResumeFullSnapshot:
		return gamev1.ResumeOutcome_RESUME_OUTCOME_FULL_SNAPSHOT
	case session.ResumeGraceExpired:
		return gamev1.ResumeOutcome_RESUME_OUTCOME_GRACE_EXPIRED
	case session.ResumeBaselineMismatch:
		return gamev1.ResumeOutcome_RESUME_OUTCOME_BASELINE_MISMATCH
	case session.ResumeSessionReplaced:
		return gamev1.ResumeOutcome_RESUME_OUTCOME_SESSION_REPLACED
	default:
		return gamev1.ResumeOutcome_RESUME_OUTCOME_UNSPECIFIED
	}
}

func commandOutcomeToProto(o combat.CommandOutcome) gamev1.CommandOutcome {
	switch o {
	case combat.OutcomeCommitted:
		return gamev1.CommandOutcome_COMMAND_OUTCOME_COMMITTED
	case combat.OutcomeRejected:
		return gamev1.CommandOutcome_COMMAND_OUTCOME_REJECTED
	case combat.OutcomeScheduled:
		return gamev1.CommandOutcome_COMMAND_OUTCOME_SCHEDULED
	default:
		return gamev1.CommandOutcome_COMMAND_OUTCOME_UNSPECIFIED
	}
}

func combatEventKindToProto(k combat.EventKind) gamev1.CombatEventKind {
	switch k {
	case combat.EventCastStarted:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_CAST_STARTED
	case combat.EventCastCancelled:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_CAST_CANCELLED
	case combat.EventMissileSpawned:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_MISSILE_SPAWNED
	case combat.EventMissileCollided:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_MISSILE_COLLIDED
	case combat.EventMissileVanished:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_MISSILE_VANISHED
	case combat.EventHit:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_HIT
	case combat.EventHeal:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_HEAL
	case combat.EventResourceChanged:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_RESOURCE_CHANGED
	case combat.EventStatusApplied:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_STATUS_APPLIED
	case combat.EventStatusRefreshed:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_STATUS_REFRESHED
	case combat.EventStatusExpired:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_STATUS_EXPIRED
	case combat.EventStatusRemoved:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_STATUS_REMOVED
	case combat.EventRecoveryEnded:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_CAST_RECOVERY_ENDED
	case combat.EventDeath:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_DEATH
	default:
		return gamev1.CombatEventKind_COMBAT_EVENT_KIND_UNSPECIFIED
	}
}

func damageSchoolToProto(s catalog.DamageSchool) gamev1.DamageSchool {
	return gamev1.DamageSchool(s)
}

func resultFlagToProto(f combat.ResultFlag) gamev1.CombatResultFlag {
	switch f {
	case combat.ResultCritical:
		return gamev1.CombatResultFlag_COMBAT_RESULT_FLAG_CRITICAL
	case combat.ResultDodged:
		return gamev1.CombatResultFlag_COMBAT_RESULT_FLAG_DODGED
	case combat.ResultKillingBlow:
		return gamev1.CombatResultFlag_COMBAT_RESULT_FLAG_KILLING_BLOW
	default:
		return gamev1.CombatResultFlag_COMBAT_RESULT_FLAG_UNSPECIFIED
	}
}
