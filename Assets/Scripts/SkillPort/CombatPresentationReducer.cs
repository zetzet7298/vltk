using System;
using System.Collections.Generic;

namespace VLTK.SkillPort
{
    public enum CombatLifecycleKind
    {
        Unspecified = 0,
        CastStarted = 1,
        CastCancelled = 2,
        CastRecoveryStarted = 3,
        CastRecoveryEnded = 4,
        MissileSpawned = 5,
        MissileFlyTriggered = 6,
        MissileCollided = 7,
        MissileVanished = 8,
        Hit = 9,
        Heal = 10,
        ResourceChanged = 11,
        StatusApplied = 12,
        StatusRefreshed = 13,
        StatusExpired = 14,
        StatusRemoved = 15,
        Death = 16,
        Revive = 17,
        ChildSkillTriggered = 18,
    }

    public enum SkillTriggerPhase
    {
        Unspecified = 0,
        CastStart = 1,
        MissileFly = 2,
        MissileCollide = 3,
        MissileVanish = 4,
        StateTick = 5,
        CastEnd = 6,
    }

    public enum PresentationApplyResult
    {
        Applied = 0,
        Duplicate = 1,
        InvalidEvent = 2,
        SequenceGap = 3,
        StateMismatch = 4,
    }

    [Serializable]
    public sealed class CombatLifecycleEvent
    {
        public string eventId;
        public ulong serverSequence;
        public ulong serverTick;
        public CombatLifecycleKind kind;
        public SkillTriggerPhase triggerPhase;
        public string sourceEntityId;
        public string targetEntityId;
        public int skillId;
        public int parentSkillId;
        public string parentEventId;
        public string castId;
        public string missileInstanceId;
        public int missileContentId;
        public string statusInstanceId;
        public int statusEffectId;
        public ulong statusRevision;
        public ulong expiresAtTick;
        public ulong appearanceRevision;
        public ulong policyRevision;
        public int impactX;
        public int impactY;
        public int skillLevel;
        public int animationId;
        public int visualEffectId;
        public string audioCueId;
        public string stateCode;
        public long value;
        public uint resultFlags;
    }

    [Serializable]
    public sealed class ActiveCastPresentation
    {
        public string castId;
        public int skillId;
        public string sourceEntityId;
        public ulong startedAtTick;
        public ulong recoveryStartedAtTick;
        public bool recovering;
        public ulong appearanceRevision;
        public ulong policyRevision;

        public ActiveCastPresentation Clone()
        {
            return (ActiveCastPresentation)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class ActiveMissilePresentation
    {
        public string missileInstanceId;
        public string castId;
        public int skillId;
        public int missileContentId;
        public ulong spawnedAtTick;
        public ulong lastLifecycleTick;
        public CombatLifecycleKind phase;
        public int lastX;
        public int lastY;

        public ActiveMissilePresentation Clone()
        {
            return (ActiveMissilePresentation)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class ActiveStatusPresentation
    {
        public string statusInstanceId;
        public int statusEffectId;
        public int sourceSkillId;
        public string sourceEntityId;
        public string targetEntityId;
        public ulong revision;
        public ulong expiresAtTick;

        public ActiveStatusPresentation Clone()
        {
            return (ActiveStatusPresentation)MemberwiseClone();
        }
    }

    [Serializable]
    public sealed class CombatPresentationSnapshot
    {
        public ulong serverSequence;
        public ulong baselineTick;
        public List<ActiveCastPresentation> casts = new List<ActiveCastPresentation>();
        public List<ActiveMissilePresentation> missiles = new List<ActiveMissilePresentation>();
        public List<ActiveStatusPresentation> statuses = new List<ActiveStatusPresentation>();
    }

    /// <summary>
    /// Pure semantic reducer. It never invents a missing lifecycle frame: a gap or
    /// state mismatch requires a server resync snapshot.
    /// </summary>
    public sealed class CombatPresentationReducer
    {
        private const int MaxRememberedEventIds = 4096;

        private readonly Dictionary<string, ActiveCastPresentation> _casts =
            new Dictionary<string, ActiveCastPresentation>(StringComparer.Ordinal);
        private readonly Dictionary<string, ActiveMissilePresentation> _missiles =
            new Dictionary<string, ActiveMissilePresentation>(StringComparer.Ordinal);
        private readonly Dictionary<string, ActiveStatusPresentation> _statuses =
            new Dictionary<string, ActiveStatusPresentation>(StringComparer.Ordinal);
        private readonly HashSet<string> _eventIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Queue<string> _eventOrder = new Queue<string>();

        public ulong lastServerSequence { get; private set; }
        public ulong baselineTick { get; private set; }
        public bool hasBaseline { get; private set; }

        public IReadOnlyDictionary<string, ActiveCastPresentation> casts => _casts;
        public IReadOnlyDictionary<string, ActiveMissilePresentation> missiles => _missiles;
        public IReadOnlyDictionary<string, ActiveStatusPresentation> statuses => _statuses;

        /// <summary>
        /// Drop encounter presentation owned by the previous GM faction while preserving
        /// the accepted server sequence/baseline for the current realtime session.
        /// </summary>
        public void ClearTransientState()
        {
            _casts.Clear();
            _missiles.Clear();
            _statuses.Clear();
            _eventIds.Clear();
            _eventOrder.Clear();
        }

        public bool ApplySnapshot(CombatPresentationSnapshot snapshot)
        {
            if (snapshot == null || snapshot.serverSequence == 0)
                return false;

            var casts = new Dictionary<string, ActiveCastPresentation>(StringComparer.Ordinal);
            var missiles = new Dictionary<string, ActiveMissilePresentation>(StringComparer.Ordinal);
            var statuses = new Dictionary<string, ActiveStatusPresentation>(StringComparer.Ordinal);

            if (snapshot.casts != null)
            {
                foreach (ActiveCastPresentation cast in snapshot.casts)
                {
                    if (cast == null || string.IsNullOrEmpty(cast.castId) || cast.skillId <= 0 ||
                        casts.ContainsKey(cast.castId))
                        return false;
                    casts.Add(cast.castId, cast.Clone());
                }
            }
            if (snapshot.missiles != null)
            {
                foreach (ActiveMissilePresentation missile in snapshot.missiles)
                {
                    if (missile == null || string.IsNullOrEmpty(missile.missileInstanceId) ||
                        missile.missileContentId <= 0 || missiles.ContainsKey(missile.missileInstanceId))
                        return false;
                    missiles.Add(missile.missileInstanceId, missile.Clone());
                }
            }
            if (snapshot.statuses != null)
            {
                foreach (ActiveStatusPresentation status in snapshot.statuses)
                {
                    if (status == null || string.IsNullOrEmpty(status.statusInstanceId) ||
                        status.statusEffectId <= 0 || status.revision == 0 ||
                        statuses.ContainsKey(status.statusInstanceId))
                        return false;
                    statuses.Add(status.statusInstanceId, status.Clone());
                }
            }

            _casts.Clear();
            _missiles.Clear();
            _statuses.Clear();
            foreach (KeyValuePair<string, ActiveCastPresentation> item in casts)
                _casts.Add(item.Key, item.Value);
            foreach (KeyValuePair<string, ActiveMissilePresentation> item in missiles)
                _missiles.Add(item.Key, item.Value);
            foreach (KeyValuePair<string, ActiveStatusPresentation> item in statuses)
                _statuses.Add(item.Key, item.Value);

            _eventIds.Clear();
            _eventOrder.Clear();
            lastServerSequence = snapshot.serverSequence;
            baselineTick = snapshot.baselineTick;
            hasBaseline = true;
            return true;
        }

        public PresentationApplyResult Apply(CombatLifecycleEvent evt)
        {
            if (evt == null || string.IsNullOrEmpty(evt.eventId) || evt.serverSequence == 0 ||
                evt.serverTick == 0 || evt.kind == CombatLifecycleKind.Unspecified)
                return PresentationApplyResult.InvalidEvent;

            if (_eventIds.Contains(evt.eventId))
                return PresentationApplyResult.Duplicate;

            if (!hasBaseline || evt.serverSequence != lastServerSequence + 1)
                return PresentationApplyResult.SequenceGap;

            PresentationApplyResult result = ApplyStateMutation(evt);
            if (result != PresentationApplyResult.Applied)
                return result;

            lastServerSequence = evt.serverSequence;
            RememberEvent(evt.eventId);
            return PresentationApplyResult.Applied;
        }

        /// <summary>
        /// Advances the envelope sequence for an already accepted non-combat
        /// game.v1 frame. The session cursor proves no transport frame was lost;
        /// without this observation, ordinary inventory/world frames would create
        /// false combat sequence gaps.
        /// </summary>
        public bool ObserveAcceptedServerEnvelope(ulong serverSequence, ulong serverTick)
        {
            if (!hasBaseline)
                return true;
            if (serverSequence != lastServerSequence + 1 || serverTick < baselineTick)
                return false;

            lastServerSequence = serverSequence;
            baselineTick = serverTick;
            return true;
        }

        private PresentationApplyResult ApplyStateMutation(CombatLifecycleEvent evt)
        {
            switch (evt.kind)
            {
                case CombatLifecycleKind.CastStarted:
                    if (string.IsNullOrEmpty(evt.castId) || evt.skillId <= 0 ||
                        string.IsNullOrEmpty(evt.sourceEntityId) || _casts.ContainsKey(evt.castId))
                        return PresentationApplyResult.StateMismatch;
                    _casts.Add(evt.castId, new ActiveCastPresentation
                    {
                        castId = evt.castId,
                        skillId = evt.skillId,
                        sourceEntityId = evt.sourceEntityId,
                        startedAtTick = evt.serverTick,
                        appearanceRevision = evt.appearanceRevision,
                        policyRevision = evt.policyRevision,
                    });
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.CastRecoveryStarted:
                    if (!_casts.TryGetValue(evt.castId ?? string.Empty, out ActiveCastPresentation cast))
                        return PresentationApplyResult.StateMismatch;
                    cast.recovering = true;
                    cast.recoveryStartedAtTick = evt.serverTick;
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.CastCancelled:
                case CombatLifecycleKind.CastRecoveryEnded:
                    if (string.IsNullOrEmpty(evt.castId) || !_casts.Remove(evt.castId))
                        return PresentationApplyResult.StateMismatch;
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.MissileSpawned:
                    if (string.IsNullOrEmpty(evt.missileInstanceId) || string.IsNullOrEmpty(evt.castId) ||
                        evt.skillId <= 0 || evt.missileContentId <= 0 ||
                        !_casts.ContainsKey(evt.castId) || _missiles.ContainsKey(evt.missileInstanceId))
                        return PresentationApplyResult.StateMismatch;
                    _missiles.Add(evt.missileInstanceId, new ActiveMissilePresentation
                    {
                        missileInstanceId = evt.missileInstanceId,
                        castId = evt.castId,
                        skillId = evt.skillId,
                        missileContentId = evt.missileContentId,
                        spawnedAtTick = evt.serverTick,
                        lastLifecycleTick = evt.serverTick,
                        phase = evt.kind,
                        lastX = evt.impactX,
                        lastY = evt.impactY,
                    });
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.MissileFlyTriggered:
                case CombatLifecycleKind.MissileCollided:
                    if (!_missiles.TryGetValue(evt.missileInstanceId ?? string.Empty, out ActiveMissilePresentation missile))
                        return PresentationApplyResult.StateMismatch;
                    missile.phase = evt.kind;
                    missile.lastLifecycleTick = evt.serverTick;
                    missile.lastX = evt.impactX;
                    missile.lastY = evt.impactY;
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.MissileVanished:
                    if (string.IsNullOrEmpty(evt.missileInstanceId) || !_missiles.Remove(evt.missileInstanceId))
                        return PresentationApplyResult.StateMismatch;
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.StatusApplied:
                    if (string.IsNullOrEmpty(evt.statusInstanceId) || evt.statusEffectId <= 0 ||
                        evt.statusRevision == 0 || _statuses.ContainsKey(evt.statusInstanceId))
                        return PresentationApplyResult.StateMismatch;
                    _statuses.Add(evt.statusInstanceId, new ActiveStatusPresentation
                    {
                        statusInstanceId = evt.statusInstanceId,
                        statusEffectId = evt.statusEffectId,
                        sourceSkillId = evt.skillId,
                        sourceEntityId = evt.sourceEntityId,
                        targetEntityId = evt.targetEntityId,
                        revision = evt.statusRevision,
                        expiresAtTick = evt.expiresAtTick,
                    });
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.StatusRefreshed:
                    if (!_statuses.TryGetValue(evt.statusInstanceId ?? string.Empty, out ActiveStatusPresentation status) ||
                        evt.statusRevision <= status.revision)
                        return PresentationApplyResult.StateMismatch;
                    status.revision = evt.statusRevision;
                    status.expiresAtTick = evt.expiresAtTick;
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.StatusExpired:
                case CombatLifecycleKind.StatusRemoved:
                    if (string.IsNullOrEmpty(evt.statusInstanceId) || !_statuses.Remove(evt.statusInstanceId))
                        return PresentationApplyResult.StateMismatch;
                    return PresentationApplyResult.Applied;

                case CombatLifecycleKind.Hit:
                case CombatLifecycleKind.Heal:
                case CombatLifecycleKind.ResourceChanged:
                case CombatLifecycleKind.Death:
                case CombatLifecycleKind.Revive:
                case CombatLifecycleKind.ChildSkillTriggered:
                    return evt.skillId > 0
                        ? PresentationApplyResult.Applied
                        : PresentationApplyResult.InvalidEvent;

                default:
                    return PresentationApplyResult.InvalidEvent;
            }
        }

        private void RememberEvent(string eventId)
        {
            _eventIds.Add(eventId);
            _eventOrder.Enqueue(eventId);
            while (_eventOrder.Count > MaxRememberedEventIds)
                _eventIds.Remove(_eventOrder.Dequeue());
        }
    }
}
