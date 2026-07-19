using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;
using VLTK.Model;
using VLTK.SkillPort;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Adapts the pure authoritative bridge to existing Sandbox presentation.
    /// It never invokes CombatRuntime.Cast or any local damage callback.
    /// </summary>
    public sealed class SandboxAuthoritativeCombatPresentationHost :
        IAuthoritativeCombatPresentationContext,
        IAuthoritativeCombatPresentationPreloadGate,
        IAuthoritativeCombatPresentationSink
    {
        private readonly SandboxManager _manager;
        private readonly SkillPortClientProjection _projection;
        private readonly AuthoritativeCombatPresentationBridge _bridge;
        private readonly Dictionary<int, EncounterPreloadGate> _preloadBySkill =
            new Dictionary<int, EncounterPreloadGate>();

        public string localEntityId { get; private set; } = "player-1";
        public AuthoritativeCombatPresentationBridge bridge => _bridge;

        public SandboxAuthoritativeCombatPresentationHost(
            SandboxManager manager,
            SkillPortClientProjection projection,
            ulong policyRevision = 1)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _projection = projection ?? throw new ArgumentNullException(nameof(projection));
            _bridge = new AuthoritativeCombatPresentationBridge(this, this, this);
            _bridge.SetRuntimePolicy(
                SkillPortClientProjectionLoader.BuildRuntimePolicy(projection, policyRevision));
        }

        public void BeginSession(
            ulong sessionEpoch,
            ulong initialServerSequence,
            ulong initialServerTick,
            string localEntityId)
        {
            if (string.IsNullOrEmpty(localEntityId))
                throw new ArgumentException("local entity id is required", nameof(localEntityId));
            this.localEntityId = localEntityId;
            _bridge.BeginSession(sessionEpoch, initialServerSequence, initialServerTick);
        }

        public void SetRuntimePolicy(RuntimePolicySnapshot policy)
        {
            _bridge.SetRuntimePolicy(policy);
        }

        public void ClearTransientPresentationState()
        {
            _preloadBySkill.Clear();
            _bridge.ClearTransientPresentationState();
        }

        public bool RequiresAuthoritativeInput(int skillId)
        {
            return _bridge.RequiresAuthoritativeInput(skillId, CurrentFactionKey());
        }

        public AuthoritativePresentationDispatchResult ApplyServerEnvelope(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return AuthoritativePresentationDispatchResult.EnvelopeRejected;
            try
            {
                return _bridge.ApplyServerEnvelope(GameV1SkillPortAdapters.ParseServerEnvelope(bytes));
            }
            catch (InvalidProtocolBufferException)
            {
                return AuthoritativePresentationDispatchResult.EnvelopeRejected;
            }
        }

        public void StartSkillPreload(
            int skillId,
            IEnumerable<VLTK.SkillPort.AssetDependency> dependencies,
            long nowMilliseconds,
            long assetBudgetBytes,
            long activePinnedBytes)
        {
            if (skillId <= 0)
                throw new ArgumentOutOfRangeException(nameof(skillId));
            var gate = new EncounterPreloadGate();
            gate.Start(
                "skill:" + skillId,
                dependencies,
                nowMilliseconds,
                assetBudgetBytes,
                activePinnedBytes);
            _preloadBySkill[skillId] = gate;
        }

        public EncounterPreloadState EvaluateSkillPreload(
            int skillId,
            ISet<string> residentHashes,
            long nowMilliseconds)
        {
            return _preloadBySkill.TryGetValue(skillId, out EncounterPreloadGate gate)
                ? gate.Evaluate(residentHashes, nowMilliseconds)
                : EncounterPreloadState.Idle;
        }

        public bool TryResolve(
            string sourceEntityId,
            out string factionKey,
            out PlayerVisualTuple visualTuple)
        {
            factionKey = CurrentFactionKey();
            visualTuple = null;
            if (!string.Equals(sourceEntityId, localEntityId, StringComparison.Ordinal))
                return false;

            SandboxPlayerController player = _manager.PlayerController;
            IPlayerVisual visual = player != null ? player.visual : null;
            if (player == null || visual == null || string.IsNullOrEmpty(factionKey))
                return false;

            PcWeaponType weapon = visual.currentWeapon;
            int weaponVariant = ResolveWeaponVariant(visual, weapon);
            WeaponVisibility visibility;
            int weaponVisualId;
            if (weapon == PcWeaponType.EmptyHand)
            {
                visibility = WeaponVisibility.Empty;
                weaponVisualId = 0;
            }
            else if (weapon == PcWeaponType.HiddenWeapon)
            {
                visibility = WeaponVisibility.Hidden;
                weaponVisualId = 0;
            }
            else
            {
                visibility = WeaponVisibility.Equipped;
                weaponVisualId = weaponVariant;
            }

            bool mounted = player.Mount.IsMounted;
            visualTuple = new PlayerVisualTuple
            {
                gender = player.isFemale ? PlayerVisualGender.Female : PlayerVisualGender.Male,
                mounted = mounted,
                mountVisualId = mounted ? player.Mount.HorseType : 0,
                weaponVisibility = visibility,
                weaponVisualId = weaponVisualId,
            };
            return visualTuple.IsCanonical();
        }

        public bool CanReveal(int skillId, CombatLifecycleEvent evt)
        {
            if (!_projection.TryGetRow(skillId, out SkillPortClientSkillRow row) || row.blocked)
                return false;
            return _preloadBySkill.TryGetValue(skillId, out EncounterPreloadGate gate) && gate.canReveal;
        }

        public bool TryPresent(CombatLifecycleEvent evt, PlayerVisualTuple visualTuple)
        {
            if (evt == null || visualTuple == null || !visualTuple.IsCanonical())
                return false;

            SandboxPlayerController player = _manager.PlayerController;
            SkillEffectVisualService visualService = _manager.SkillEffectVisual;
            SkillDefinition skill = _manager.CombatSkillCatalog?.Resolve(evt.skillId);
            if (player == null || visualService == null || skill == null)
                return false;

            if (!string.IsNullOrEmpty(evt.audioCueId))
                _manager.AudioService?.PlaySkillCast(evt.audioCueId);

            Vector2 source = player.transform.position;
            Vector2 eventPosition = new Vector2(evt.impactX, evt.impactY);
            if (eventPosition == Vector2.zero)
                eventPosition = source;

            switch (evt.kind)
            {
                case CombatLifecycleKind.CastStarted:
                    player.PlayPcSkillAction(
                        evt.animationId > 0 ? evt.animationId : skill.charAnimId,
                        0f,
                        skill.horseLimit);
                    return true;

                case CombatLifecycleKind.MissileSpawned:
                    return visualService.SpawnAuthoritativeMissile(
                        evt.missileInstanceId,
                        skill,
                        source,
                        eventPosition,
                        evt.skillLevel) != null;

                case CombatLifecycleKind.MissileFlyTriggered:
                    return visualService.UpdateAuthoritativeMissile(
                        evt.missileInstanceId,
                        eventPosition,
                        playFlightSound: string.IsNullOrEmpty(evt.audioCueId));

                case CombatLifecycleKind.MissileCollided:
                    return visualService.CollideAuthoritativeMissile(
                        evt.missileInstanceId,
                        eventPosition,
                        playConfiguredImpactSound: string.IsNullOrEmpty(evt.audioCueId));

                case CombatLifecycleKind.MissileVanished:
                    return visualService.VanishAuthoritativeMissile(evt.missileInstanceId);

                case CombatLifecycleKind.CastCancelled:
                case CombatLifecycleKind.CastRecoveryStarted:
                case CombatLifecycleKind.CastRecoveryEnded:
                    return true;

                default:
                    // No canonical status/hit/death visual mapping exists yet.
                    return false;
            }
        }

        private string CurrentFactionKey()
        {
            CombatFaction faction = _manager.PlayerProgression != null
                ? _manager.PlayerProgression.faction
                : CombatFaction.None;
            return faction == CombatFaction.None ? string.Empty : faction.ToString();
        }

        private static int ResolveWeaponVariant(IPlayerVisual visual, PcWeaponType weapon)
        {
            if (visual is MalePlayerVisual male)
                return male.weaponVariant;
            if (visual is FemalePlayerVisual female)
                return female.weaponVariant;
            return weapon == PcWeaponType.EmptyHand || weapon == PcWeaponType.HiddenWeapon
                ? 0
                : -1;
        }
    }
}
