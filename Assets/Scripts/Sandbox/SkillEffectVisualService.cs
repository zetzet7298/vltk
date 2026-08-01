using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC-accurate skill effect visual renderer for ALL combat skills.
    /// Data-driven: reads missles1.txt + skills.txt -> auto-maps every skill's visual.
    /// Plays PreCastSpr animation, spawns missile projectile sprites, renders impact.
    /// No more per-faction hardcoded switch-cases — everything from PC data.
    /// </summary>
    public class SkillEffectVisualService
    {
        private const float PcMissileTickSeconds = 1f / 18f;
        private const int PcFollowRetargetCounterMax = 8;
        private const int MaxPcMissileTicksPerUpdate = 512;

        private readonly SprRuntimeService _sprService;
        private readonly SkillCatalog _catalog;
        private readonly List<ActiveSkillEffect> _activeEffects = new();
        private readonly Dictionary<CombatStateSourceKey, ActiveSkillEffect> _stateAuraEffects = new();
        private readonly HashSet<CombatStateSourceKey> _stateAuraKeysInUse = new();
        private readonly List<CombatStateSourceKey> _staleStateAuraKeys = new();
        private readonly PcSkillVisualAutoMapper _autoMapper = new();
        private bool _autoMapperReady;
        /// <summary>
        /// Callback fired when a PC skill sound should be played.
        /// Wired by SandboxManager → AudioService.PlaySkillCast.
        /// </summary>
        public Action<string> OnCastSound;

        /// <summary>
        /// Fired once for every missile that collides. Consumers can apply the
        /// corresponding PC collide event without waiting for the aggregate phase.
        /// </summary>
        public Action<ActiveSkillEffect, int, Vector2> OnMissileCollided;

        public SkillEffectVisualService(SprRuntimeService sprService)
            : this(sprService, null) { }

        public SkillEffectVisualService(SprRuntimeService sprService, SkillCatalog catalog)
        {
            _sprService = sprService;
            _catalog = catalog;
        }


        /// <summary>
        /// Ensure the data-driven auto-mapper is initialized.
        /// Lazy-init so it works even when constructed before StreamingAssets is ready.
        /// </summary>
        private void EnsureAutoMapperReady()
        {
            if (_autoMapperReady) return;
            try
            {
                _autoMapper.Initialize(UnityEngine.Application.streamingAssetsPath);
                if (_catalog != null) _autoMapper.PreCacheAll(_catalog);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[SkillVisual] AutoMapper init failed: {ex.Message}");
            }
            _autoMapperReady = true;
        }

        /// <summary>
        /// Data-driven visual configuration: auto-resolves skill visual from PC missles1.txt.
        /// Replaces hardcoded per-faction Configure*Visuals switch-cases.
        /// Flow: skill.childSkillId -> missile -> SPR paths + anim info + light color.
        /// </summary>
          private void ConfigureDataDrivenVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
          {
              EnsureAutoMapperReady();

            if (PcTangMenLuaLevelService.Applies(skill.skillId))
            {
                fx.pcFlyEventEnabled = PcTangMenLuaLevelService.FlyEnabled(skill.skillId, level);
                fx.pcFlyEventIntervalTicks = PcTangMenLuaLevelService.FlyInterval(skill.skillId, level);
                fx.pcFlySkillId = PcTangMenLuaLevelService.FlySkillId(skill.skillId, level);
            }
            else if (skill.skillId == 1073 && skill.flySkillId == 1103 && skill.flyEventTime == 1)
            {
                // PC Skills.txt row 1073 emits child 1103 once per flying tick.
                fx.pcFlyEventEnabled = true;
                fx.pcFlyEventIntervalTicks = skill.flyEventTime;
                fx.pcFlySkillId = skill.flySkillId;
            }

            var config = _autoMapper.GetVisualConfig(skill);
            if (config == null) return;

            int tangMenLifetimeTicks = PcTangMenLuaLevelService.MissileLifetime(skill.skillId, level);
            int tangMenSpeedPerTick = PcTangMenLuaLevelService.MissileSpeed(skill.skillId, level);

            // Apply faction default color
            fx.color = config.lightColor;
            fx.flightSoundPath = config.flightSoundPath;
            fx.impactSoundPath = config.impactSoundPath;

              // A state-only skill may present the attached aura immediately. Skills that
              // also have a child missile must keep their cast/missile presentation; their
              // persistent aura is materialized separately from the receiver's state source.
              if (config.hasStateAura && skill.childSkillId <= 0)
            {
                fx.isAura = true;
                fx.pcPreCastSpriteKey = PcSkillVisualAutoMapper.SprPathToKey(config.stateAuraSprPath);
                fx.pcPreCastTotalFrames = config.stateAuraTotalFrames > 0 ? config.stateAuraTotalFrames : 16;
                fx.pcPreCastDirections = config.stateAuraDirections > 0 ? config.stateAuraDirections : 1;
                fx.pcPreCastIntervalTicks = config.stateAuraIntervalTicks > 0 ? config.stateAuraIntervalTicks : 1;
                  fx.pcAuraFrameStart = config.stateAuraFrameStart;
                  fx.pcAuraFrameEnd = config.stateAuraFrameEnd;
                  fx.stateAuraPos = config.stateAuraPos;
                  fx.auraDuration = ResolveStateAuraDurationSeconds(skill, level);
                  fx.preCastDuration = fx.auraDuration;

                if (_sprService != null && !string.IsNullOrEmpty(config.stateAuraSprPath))
                {
                    fx.preCastSprite = _sprService.ResolveSprite(config.stateAuraSprPath, 64, 64);
                }
                return;
            }

            // PreCast visual
            if (config.hasPreCast && !string.IsNullOrEmpty(config.preCastSprPath))
            {
                var preCastKey = PcSkillVisualAutoMapper.SprPathToKey(config.preCastSprPath);
                if (!string.IsNullOrEmpty(preCastKey))
                {
                    SetupPcPreCast(fx, preCastKey, 16, 1, 1);
                }
            }

            // Stationary area effect (MoveKind=0)
              if (config.isStationary)
              {
              if (config.HasFlightVisual)
              {
                    var key = PcSkillVisualAutoMapper.SprPathToKey(config.flightSprPath);
                    SetupPcStationaryEffect(fx, key,
                        config.flightFrames,
                        System.Math.Max(1, config.flightDirections),
                        System.Math.Max(1, config.flightIntervalTicks),
                        config.lightColor);
                }
                  else if (config.HasExplodeVisual)
                {
                    var key = PcSkillVisualAutoMapper.SprPathToKey(config.explodeSprPath);
                    SetupPcStationaryEffect(fx, key,
                        config.explodeFrames,
                        System.Math.Max(1, config.explodeDirections),
                        System.Math.Max(1, config.explodeIntervalTicks),
                          config.lightColor);
                  }
                  // KMissle lifetime, not SPR frame count, owns stationary lifetime.
                  // Missile 359: LifeTime=31, LoopPlay=0, 19 frames.
                  int stationaryLifetimeTicks = tangMenLifetimeTicks > 0
                      ? tangMenLifetimeTicks
                      : config.missileLifetime;
                  if (stationaryLifetimeTicks > 0)
                      ApplyPcStationaryLifetime(fx, stationaryLifetimeTicks);
                  if (fx.pcFlyEventEnabled)
                      fx.missileFlyEventOrdinals = new int[1];
                  return;
              }

            // Flight missile visual
            if (config.HasFlightVisual)
            {
                var flightKey = PcSkillVisualAutoMapper.SprPathToKey(config.flightSprPath);
                string explodeKey = config.HasExplodeVisual
                    ? PcSkillVisualAutoMapper.SprPathToKey(config.explodeSprPath)
                    : null;

                // PC gaibang.lua overrides: missle_speed_v takes priority over engine missles.txt Speed.
                // Source: jx-source bin/client/script/skill/gaibang.lua per-skill interpolation tables.
                int missileSpeed = config.missileSpeed;
                if (PcCaiBangLuaLevelService.Applies(skill.skillId))
                {
                    int luaSpeed = PcCaiBangLuaLevelService.GetMissileSpeed(skill.skillId, level);
                    if (luaSpeed > 0) missileSpeed = luaSpeed;
                }
                if (tangMenSpeedPerTick > 0) missileSpeed = tangMenSpeedPerTick;
                int missileLifetime = tangMenLifetimeTicks > 0 ? tangMenLifetimeTicks : config.missileLifetime;

                  SetupPcMissile(fx,
                    flightKey,
                    config.flightFrames,
                    System.Math.Max(1, config.flightDirections),
                    System.Math.Max(1, config.flightIntervalTicks),
                    missileSpeed,
                    missileLifetime,
                    explodeKey,
                    config.explodeFrames,
                    System.Math.Max(1, config.explodeDirections),
                    System.Math.Max(1, config.explodeIntervalTicks),
                      config.lightColor);
                  fx.pcMissileMoveKind = config.moveKind;


                // PC gaibang.lua: Single-form or fan-spread skills with skill_misslenum_v > 1 use dynamic spread.
                // E.g. Phi Long (357) L20=4, Kháng Long Hữu Hối stock (128) L20=15.
                bool luaSpreadConfigured = false;
                if (PcCaiBangLuaLevelService.Applies(skill.skillId))
                {
                    int luaCount = PcCaiBangLuaLevelService.GetMissileCount(skill.skillId, level);
                    if (luaCount > 0)
                    {
                        luaSpreadConfigured = true;
                        if (luaCount > 1)
                        {
                            int missileForm = PcCaiBangLuaLevelService.GetMissileForm(skill.skillId, level);
                            if (missileForm == 2)
                            {
                                int angleStep = PcCaiBangLuaLevelService.GetSingleValue(skill.skillId, level, "skill_param1_v", 1);
                                SetupPcKangLongSpread(fx, luaCount, angleStep, 0);
                            }
                            else
                            {
                                // PC gaibang.lua: Phi Long 357 has no skill_param1_v, but PC skills.txt
                                // Param1=32 and missile 166 MoveKind=5. Therefore luaCount>1 still needs
                                // parallel lane offsets for the level-20 four homing dragons. Only a single
                                // missile remains straight-line/no-spread.
                                int rawParam = PcCaiBangLuaLevelService.GetSingleValue(
                                    skill.skillId, level, "skill_param1_v", 32);
                                int stepWu = rawParam > 0 ? rawParam : 32;
                                SetupPcPhiLongSpread(fx, luaCount, stepWu);
                            }
                        }
                    }
                }

                // Multi-missile spread for other fan/surround forms
                  if (!luaSpreadConfigured && (skill.missileForm == SkillMissileForm.Fan || skill.missileForm == SkillMissileForm.Surround || skill.missileForm == SkillMissileForm.Zone))
                {
                    int count = System.Math.Max(1, skill.childSkillNum);
                    if (skill.missileForm == SkillMissileForm.Surround)
                        SetupSurroundMissiles(fx, count);
                    else if (skill.missileForm == SkillMissileForm.Zone)
                        SetupPcZoneMissiles(fx, count, skill.attackRadius);
                    else
                          SetupPcFanMissiles(skill, fx, count);
                  }
                  if (fx.pcFlyEventEnabled && fx.missileCount > 0)
                      fx.missileFlyEventOrdinals = new int[fx.missileCount];
                  return;
            }

            // Explosion-only (no flight) — common for buff/aura skills
            if (config.HasExplodeVisual)
            {
                var key = PcSkillVisualAutoMapper.SprPathToKey(config.explodeSprPath);
                SetupPcStationaryEffect(fx, key,
                    config.explodeFrames,
                    System.Math.Max(1, config.explodeDirections),
                    System.Math.Max(1, config.explodeIntervalTicks),
                    config.lightColor);
                return;
            }

            // Melee: no missile, just show impact at target
            if (config.isMelee)
            {
                fx.preCastDuration = Mathf.Max(0.1f, skill.timePerCast > 0 ? skill.timePerCast * 0.055f : 0.15f);
                fx.impactDuration = 0.3f;
                return;
            }

            // Has missile data but no SPR resolved — use speed/timing from PC data
            if (config.missileSpeed > 0)
            {
                fx.missileSpeed = config.SpeedWorldPerSec;
                fx.missileDuration = config.FlightDurationSeconds;
            }
        }
        public int ActiveEffectCount => _activeEffects.Count;

        /// <summary>Cancel all in-flight local and authoritative presentation for a GM state transition.</summary>
        public int ClearActiveEffects()
        {
            int cleared = _activeEffects.Count;
            _activeEffects.Clear();
            _stateAuraEffects.Clear();
            return cleared;
        }

        public int RemoveStateAurasForActor(int actorId)
        {
            _staleStateAuraKeys.Clear();
            foreach (var pair in _stateAuraEffects)
                if (pair.Key.actorId == actorId)
                    _staleStateAuraKeys.Add(pair.Key);

            int removed = 0;
            foreach (var key in _staleStateAuraKeys)
            {
                if (_stateAuraEffects.TryGetValue(key, out var effect) &&
                    _activeEffects.Remove(effect))
                    removed++;
                _stateAuraEffects.Remove(key);
            }
            _staleStateAuraKeys.Clear();
            return removed;
        }

        /// <summary>Reconcile source-owned local-player combat states with exact mapped PC aura SPRs.</summary>
        public int SynchronizeStateAuras(
            CombatActorState actor,
            Vector2 position,
            Func<Vector2> getCurrentActorPos = null)
        {
            if (actor == null || _catalog == null) return 0;

            actor.SynchronizeCompatibilityStates();
            _stateAuraKeysInUse.Clear();
            foreach (var source in actor.stateSources)
            {
                var key = source.Key;
                if (key.skillId == CombatActorState.CompatibilityStateSourceSkillId) continue;

                SkillDefinition skill = _catalog.Resolve(key.skillId);
                if (skill == null || skill.stateSpecialId <= 0) continue;

                var aura = PcSkillVisualAutoMapper.GetStateAuraData(skill.stateSpecialId);
                if (string.IsNullOrEmpty(aura.sprPath)) continue;

                float remaining = ResolveStateSourceAuraDurationSeconds(source.Value);
                if (remaining <= 0f) continue;

                _stateAuraKeysInUse.Add(key);
                ActiveSkillEffect effect = ResolveStateAuraEffect(
                    key, skill, source.Value, aura, position, remaining,
                    getCurrentActorPos, actor.rideHorse);
                RemoveDuplicateStateAuras(key, effect);
            }

            RemoveStaleStateAuras(actor.actorId);
            int actorAuraCount = 0;
            foreach (var pair in _stateAuraEffects)
                if (pair.Key.actorId == actor.actorId)
                    actorAuraCount++;
            return actorAuraCount;
        }

        private ActiveSkillEffect ResolveStateAuraEffect(
            CombatStateSourceKey key,
            SkillDefinition skill,
            CombatStateSourceNode node,
            PcSkillVisualAutoMapper.PcStateAuraData aura,
            Vector2 position,
            float remainingSeconds,
            Func<Vector2> getCurrentActorPos,
            bool stateOwnerMounted)
        {
            if (!_stateAuraEffects.TryGetValue(key, out var effect) || !_activeEffects.Contains(effect))
            {
                effect = FindUnownedStateAura(skill.skillId, position) ??
                    CreateStateAuraEffect(
                        key, skill, aura, position, getCurrentActorPos, stateOwnerMounted);
                _stateAuraEffects[key] = effect;
            }

            effect.hasStateSourceKey = true;
            effect.stateSourceKey = key;
            effect.skillLevel = Mathf.Max(1, node?.sourceLevel ?? effect.skillLevel);
            effect.casterPos = position;
            effect.targetPos = position;
            effect.currentMissilePos = position;
            effect.getCurrentTargetPos = getCurrentActorPos;
            effect.stateOwnerMounted = stateOwnerMounted;
            effect.phase = SkillEffectPhase.PreCast;
            effect.auraDuration = remainingSeconds;
            effect.preCastDuration = remainingSeconds;
            return effect;
        }

        private ActiveSkillEffect FindUnownedStateAura(int skillId, Vector2 position)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveSkillEffect effect = _activeEffects[i];
                if (effect.isAura && effect.skillId == skillId && !effect.hasStateSourceKey &&
                    (effect.targetPos - position).sqrMagnitude <= 0.0001f)
                    return effect;
            }
            return null;
        }

        private ActiveSkillEffect CreateStateAuraEffect(
            CombatStateSourceKey key,
            SkillDefinition skill,
            PcSkillVisualAutoMapper.PcStateAuraData aura,
            Vector2 position,
            Func<Vector2> getCurrentActorPos,
            bool stateOwnerMounted)
        {
            var effect = new ActiveSkillEffect
            {
                skillId = skill.skillId,
                skillLevel = 1,
                skillName = skill.DisplayName,
                casterPos = position,
                targetPos = position,
                currentMissilePos = position,
                startTime = Time.time,
                elapsed = 0f,
                phase = SkillEffectPhase.PreCast,
                phaseStart = 0f,
                isAura = true,
                hasStateSourceKey = true,
                stateSourceKey = key,
                getCurrentTargetPos = getCurrentActorPos,
                stateOwnerMounted = stateOwnerMounted,
                pcPreCastSpriteKey = PcSkillVisualAutoMapper.SprPathToKey(aura.sprPath),
                pcPreCastTotalFrames = aura.totalFrames > 0 ? aura.totalFrames : 16,
                pcPreCastDirections = aura.directions > 0 ? aura.directions : 1,
                pcPreCastIntervalTicks = aura.intervalTicks > 0 ? aura.intervalTicks : 1,
                pcAuraFrameStart = aura.frameStart,
                pcAuraFrameEnd = aura.frameEnd,
                stateAuraPos = aura.position,
            };
            if (_sprService != null)
                effect.preCastSprite = _sprService.ResolveSprite(aura.sprPath, 64, 64);
            _activeEffects.Add(effect);
            return effect;
        }

        private void RemoveDuplicateStateAuras(CombatStateSourceKey sourceKey, ActiveSkillEffect keep)
        {
            _staleStateAuraKeys.Clear();
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                ActiveSkillEffect effect = _activeEffects[i];
                if (ReferenceEquals(effect, keep) || !effect.isAura || effect.skillId != sourceKey.skillId)
                    continue;
                if (effect.hasStateSourceKey && effect.stateSourceKey.actorId != sourceKey.actorId)
                    continue;
                if (!effect.hasStateSourceKey &&
                    (effect.targetPos - keep.targetPos).sqrMagnitude > 0.0001f)
                    continue;
                _activeEffects.RemoveAt(i);
                foreach (var pair in _stateAuraEffects)
                    if (ReferenceEquals(pair.Value, effect))
                        _staleStateAuraKeys.Add(pair.Key);
            }
            foreach (var key in _staleStateAuraKeys)
                _stateAuraEffects.Remove(key);
            _staleStateAuraKeys.Clear();
        }

        private void RemoveStaleStateAuras(int actorId)
        {
            _staleStateAuraKeys.Clear();
            foreach (var pair in _stateAuraEffects)
                if (pair.Key.actorId == actorId &&
                    (!_stateAuraKeysInUse.Contains(pair.Key) || !_activeEffects.Contains(pair.Value)))
                    _staleStateAuraKeys.Add(pair.Key);

            foreach (var key in _staleStateAuraKeys)
            {
                if (_stateAuraEffects.TryGetValue(key, out var effect))
                    _activeEffects.Remove(effect);
                _stateAuraEffects.Remove(key);
            }
            _staleStateAuraKeys.Clear();
            _stateAuraKeysInUse.Clear();
        }

        internal static float ResolveStateSourceAuraDurationSeconds(CombatStateSourceNode node)
        {
            if (node == null || node.attributes == null || node.attributes.Count == 0) return 0f;
            if (node.isPermanentPassive) return float.MaxValue;

            int maxDurationTicks = 0;
            foreach (var attribute in node.attributes.Values)
            {
                if (attribute == null) continue;
                if (attribute.value2 < 0) return float.MaxValue;
                if (attribute.value2 > maxDurationTicks)
                    maxDurationTicks = attribute.value2;
            }

            return maxDurationTicks > 0 ? maxDurationTicks / 18f : float.MaxValue;
        }

        /// <summary>
        /// Spawn immediate target hit flash. Used by melee hits, missile impacts,
        /// and combat feedback when no PC impact SPR exists yet.
        /// </summary>
        public ActiveSkillEffect PlayHitFlash(Vector2 targetPos, Color color, float durationSeconds = 0.35f)
        {
            var effect = new ActiveSkillEffect
            {
                skillId = -1,
                skillName = "HitFlash",
                targetPos = targetPos,
                casterPos = targetPos,
                startTime = Time.time,
                elapsed = 0f,
                phase = SkillEffectPhase.Impact,
                phaseStart = 0f,
                impactDuration = Mathf.Max(0.05f, durationSeconds),
                color = color,
                isHitFlash = true,
            };
            _activeEffects.Add(effect);
            return effect;
        }

        /// <summary>
        /// Spawn a generic aura lifecycle at a world position. Renderers deliberately
        /// fail closed for aura visuals until exact PC art is attached.
        /// </summary>
        public ActiveSkillEffect PlayBuffAura(Vector2 centerPos, Color color, float durationSeconds = 1.2f, float radius = 48f, string label = "BuffAura")
        {
            var effect = new ActiveSkillEffect
            {
                skillId = -2,
                skillName = label,
                casterPos = centerPos,
                targetPos = centerPos,
                startTime = Time.time,
                elapsed = 0f,
                phase = SkillEffectPhase.PreCast,
                phaseStart = 0f,
                preCastDuration = Mathf.Max(0.05f, durationSeconds),
                impactDuration = 0f,
                color = color,
                isAura = true,
                auraDuration = Mathf.Max(0.05f, durationSeconds),
                auraRadius = Mathf.Max(1f, radius),
            };
            _activeEffects.Add(effect);
            return effect;
        }

        /// <summary>
        /// Play the full visual sequence for a skill cast:
        /// 1) PreCast effect on caster (SPR animation)
        /// 2) Missile/projectile from caster to target
        /// 3) Impact effect on target
        /// </summary>
        public ActiveSkillEffect PlaySkillCast(
            SkillDefinition skill,
            Vector2 casterPos,
            Vector2 targetPos,
            int skillLevel)
        {
            return PlaySkillCast(skill, casterPos, targetPos, skillLevel, null, null);
        }

        /// <summary>
        /// Play the full visual sequence with optional live target tracking.
        /// When <paramref name="getCurrentTargetPos"/> is non-null, missiles chase
        /// the target's current position (PC-style target-tracking behavior).
        /// </summary>
        public ActiveSkillEffect PlaySkillCast(
            SkillDefinition skill,
            Vector2 casterPos,
            Vector2 targetPos,
            int skillLevel,
            Func<Vector2> getCurrentTargetPos,
            Action<ActiveSkillEffect, int, Vector2> onMissileCollided = null,
            bool suppressCastAudio = false)
        {
            if (skill == null) return null;

            // [DEBUG 2026-07-16] KangLong visual: entry log.
            if (skill.skillId == 128)
                SubsystemLog.Info("SkillFx", $"[KangLongEntry] PlaySkillCast skill=128 level={skillLevel} caster={casterPos} target={targetPos} " +
                    $"effectKey={skill.effectSourceId?.ToKey() ?? "<null>"} form={skill.missileForm} childNum={skill.childSkillNum} appliesLua={PcCaiBangLuaLevelService.Applies(128)}");

            var effect = new ActiveSkillEffect
            {
                skillId = skill.skillId,
                skillLevel = skillLevel,
                lifecycleSkillIds = new HashSet<int> { skill.skillId },
                skillName = skill.DisplayName,
                casterPos = casterPos,
                targetPos = targetPos,
                startTime = Time.time,
                phase = SkillEffectPhase.PreCast,
            };
              effect.onMissileCollided = onMissileCollided;

            // Phase durations based on PC skill data
            // PC parity [2026-06-19]: PreCast = PC WaitTime (Skills.txt col 25) / 16f seconds.
            //   Trước fix: timePerCast * 0.055f (~PC ticks * 55ms — sai field).
            //   PC WaitTime là cast anim duration; timePerCast là cooldown. Khác nhau.
            //   Min 0.25s để giữ visual luôn thấy được trên mobile.
            effect.preCastDuration = Mathf.Max(0.25f, skill.waitTime > 0 ? skill.waitTime / 16f : 0.25f);
            effect.missileSpeed = 324f; // PC missile 48: Speed=18 game units/tick × 18 ticks/sec
            effect.missileForm = skill.missileForm;

            // Resolve effect SPR from PC source
            if (skill.effectSourceId != null)
            {
                effect.preCastSprite = _sprService?.ResolveSprite(
                    skill.effectSourceId.sourcePath, 64, 64);
            }

            // Resolve missile child skill SPR (PC childSkillId references missle SPR)
            if (skill.HasMissile && skill.childSkillId > 0)
            {
                effect.missileSprite = ResolveMissileSprite(skill);
            }

            // Calculate projectile path
            effect.missileDistance = Vector2.Distance(casterPos, targetPos);
            effect.missileDuration = effect.missileDistance / Mathf.Max(0.1f, effect.missileSpeed);
            effect.currentMissilePos = casterPos;
            // PC data-driven visual: auto-resolve from missles1.txt
              ConfigureDataDrivenVisuals(skill, effect, skillLevel);

            if (!effect.isAura &&
                !effect.HasPcPreCastSprite &&
                !effect.HasPcMissileSprite &&
                !effect.HasPcImpactSprite)
            {
                // ponytail: no canonical PC art. Fail closed, not fake a timeline.
                effect.preCastDuration = 0f;
                effect.impactDuration = 0f;
                effect.missileDuration = 0f;
                effect.missileCount = 0;
                effect.phase = SkillEffectPhase.Finished;
            }

            // [CaiBang-SoundParity 2026-06-18] PC KSkill::Cast fires the SKILL cast sound
            // (skills.txt col 7 ManCastSnd / col 8 FMCastSnd) at the cast frame, BEFORE
            // the missile spawns. This is distinct from the missile status sounds
            // (PC missles.txt SndFile2/SndFile4) fired during flight/collision.
            //
            // PC skill cast sound fires at the cast frame. Missile status sounds
            // are dispatched later at their own flight/collision phases.
            if (!string.IsNullOrEmpty(skill.manCastSndPath))
                effect.castSoundPath = skill.manCastSndPath;
            // Trigger the PC Skills.txt ManCastSnd only at the cast frame.
            // Lifecycle sub-effects (collide/vanish/fly fallback) suppress it: PC KSkill::OnMissleEvent
            // dispatches via CastMissles, not the full KSkill::Cast, so the sub-skill's ManCastSnd
            // is NOT replayed. The runtime combat layer owns the sub-skill's audio as a separate event.
            if (!suppressCastAudio && !string.IsNullOrEmpty(effect.castSoundPath))
                OnCastSound?.Invoke(effect.castSoundPath);

            // (Legacy per-faction visual overrides removed: skill visuals are now
            //  data-driven entirely from PC missles1.txt via ConfigureDataDrivenVisuals)

            // (All per-faction visual overrides removed; data-driven from PC missles1.txt only)





            // If casting a permanent aura, remove any existing aura for the same skill first to avoid duplication
            if (effect.isAura)
            {
                for (int i = _activeEffects.Count - 1; i >= 0; i--)
                {
                    if (_activeEffects[i].skillId == effect.skillId &&
                        (_activeEffects[i].targetPos - effect.targetPos).sqrMagnitude <= 0.0001f)
                    {
                        _activeEffects.RemoveAt(i);
                    }
                }
            }

            _activeEffects.Add(effect);
            effect.getCurrentTargetPos = getCurrentTargetPos;

              if (effect.missileCount > 0)
              {
                  effect.missileExplodeStartTime = new float[effect.missileCount];
                  effect.missileVanishEventFired = new bool[effect.missileCount];
                for (int midx = 0; midx < effect.missileCount; midx++)
                    effect.missileExplodeStartTime[midx] = -1f;

                if (effect.missileArrived == null)
                    effect.missileArrived = new bool[effect.missileCount];
            }

            return effect;
        }

        /// <summary>
        /// Creates a server-owned missile visual without enabling the local
        /// fly/collision/vanish simulation or its damage callbacks.
        /// </summary>
        public ActiveSkillEffect SpawnAuthoritativeMissile(
            string missileInstanceId,
            SkillDefinition skill,
            Vector2 casterPos,
            Vector2 targetPos,
            int skillLevel)
        {
            if (string.IsNullOrEmpty(missileInstanceId) || skill == null ||
                FindAuthoritativeMissile(missileInstanceId) != null)
                return null;

            ActiveSkillEffect effect = PlaySkillCast(
                skill,
                casterPos,
                targetPos,
                Mathf.Max(1, skillLevel),
                null,
                null,
                suppressCastAudio: true);
            if (effect == null || effect.phase == SkillEffectPhase.Finished || !effect.HasMissile)
            {
                if (effect != null)
                    _activeEffects.Remove(effect);
                return null;
            }

            effect.authoritativeLifecycle = true;
            effect.authoritativeMissileInstanceId = missileInstanceId;
            effect.phase = SkillEffectPhase.Missile;
            effect.phaseStart = effect.elapsed;
            effect.currentMissilePos = casterPos;
            if (effect.missilePositions != null)
            {
                for (int i = 0; i < effect.missilePositions.Length; i++)
                    effect.missilePositions[i] = casterPos;
            }
            return effect;
        }

        public bool UpdateAuthoritativeMissile(
            string missileInstanceId,
            Vector2 worldPosition,
            bool playFlightSound)
        {
            ActiveSkillEffect effect = FindAuthoritativeMissile(missileInstanceId);
            if (effect == null)
                return false;

            effect.phase = SkillEffectPhase.Missile;
            effect.currentMissilePos = worldPosition;
            if (effect.missilePositions != null)
            {
                for (int i = 0; i < effect.missilePositions.Length; i++)
                    effect.missilePositions[i] = worldPosition;
            }
            if (playFlightSound && !string.IsNullOrEmpty(effect.flightSoundPath))
                OnCastSound?.Invoke(effect.flightSoundPath);
            return true;
        }

        public bool CollideAuthoritativeMissile(
            string missileInstanceId,
            Vector2 worldPosition,
            bool playConfiguredImpactSound = true)
        {
            ActiveSkillEffect effect = FindAuthoritativeMissile(missileInstanceId);
            if (effect == null)
                return false;

            effect.currentMissilePos = worldPosition;
            effect.targetPos = worldPosition;
            effect.phase = SkillEffectPhase.Impact;
            effect.phaseStart = effect.elapsed;
            if (playConfiguredImpactSound && !string.IsNullOrEmpty(effect.impactSoundPath))
                OnCastSound?.Invoke(effect.impactSoundPath);
            return true;
        }

        public bool VanishAuthoritativeMissile(string missileInstanceId)
        {
            ActiveSkillEffect effect = FindAuthoritativeMissile(missileInstanceId);
            if (effect == null)
                return false;

            effect.phase = SkillEffectPhase.Finished;
            return _activeEffects.Remove(effect);
        }

        private ActiveSkillEffect FindAuthoritativeMissile(string missileInstanceId)
        {
            if (string.IsNullOrEmpty(missileInstanceId))
                return null;
            for (int i = 0; i < _activeEffects.Count; i++)
            {
                ActiveSkillEffect effect = _activeEffects[i];
                if (effect.authoritativeLifecycle &&
                    string.Equals(effect.authoritativeMissileInstanceId, missileInstanceId, StringComparison.Ordinal))
                    return effect;
            }
            return null;
        }


        /// <summary>
        /// Update all active effects. Returns finished effects for cleanup.
        /// </summary>
        public void Update(float dt)
        {
            for (int i = _activeEffects.Count - 1; i >= 0; i--)
            {
                var fx = _activeEffects[i];
                fx.elapsed += dt;

                // Server lifecycle events exclusively advance these effects.
                // Never run local fly/collision/vanish callbacks for them.
                if (fx.authoritativeLifecycle)
                {
                    if (fx.phase == SkillEffectPhase.Finished)
                        _activeEffects.RemoveAt(i);
                    continue;
                }

                if (fx.isAura)
                {
                    // Source-owned state auras are removed by SynchronizeStateAuras when
                    // their exact receiver/source node expires. Let elapsed advance so the
                    // PC SPR animation keeps looping instead of resetting every sync tick.
                    if (!fx.hasStateSourceKey && fx.elapsed >= fx.auraDuration)
                        fx.phase = SkillEffectPhase.Finished;
                    if (fx.phase == SkillEffectPhase.Finished)
                    {
                        _activeEffects.RemoveAt(i);
                    }
                    continue;
                }

                switch (fx.phase)
                {
                    case SkillEffectPhase.PreCast:
                        if (fx.elapsed >= fx.preCastDuration)
                        {
                            fx.phase = fx.HasMissile ? SkillEffectPhase.Missile : SkillEffectPhase.Impact;
                            fx.phaseStart = fx.elapsed;
                            if (!string.IsNullOrEmpty(fx.flightSoundPath))
                            {
                                // PC KMissle::Activate calls MS_DoFly per spawned missile.
                                // Stationary effects enter Impact directly but still activate once.
                                int missileInstances = fx.phase == SkillEffectPhase.Missile
                                    ? Mathf.Max(1, fx.missileCount)
                                    : 1;
                                for (int mi = 0; mi < missileInstances; mi++)
                                    OnCastSound?.Invoke(fx.flightSoundPath);
                            }
                        }
                        break;

                    case SkillEffectPhase.Missile:
                        bool collisionCheckedPerTick = UpdateMultiMissile(fx, dt);
                        if (!collisionCheckedPerTick)
                            ResolveMissileCollisions(fx);

                        bool allArrived;
                        if (fx.missilePositions != null && fx.missilePositions.Length > 0)
                        {
                            allArrived = true;
                            for (int mi = 0; mi < fx.missilePositions.Length; mi++)
                            {
                                bool arrived = fx.missileArrived != null && mi < fx.missileArrived.Length && fx.missileArrived[mi];
                                float explodeTime = arrived && fx.missileExplodeStartTime != null && mi < fx.missileExplodeStartTime.Length
                                    ? fx.elapsed - fx.missileExplodeStartTime[mi]
                                    : 0f;
                                if (!arrived || explodeTime < fx.impactDuration)
                                    allArrived = false;
                            }
                        }
                        else
                        {
                            // Single missile: keep flying until it reaches the target.
                            allArrived = Vector2.Distance(fx.currentMissilePos, fx.ResolveMissileTarget(-1)) <= fx.arrivalRadius;
                        }

                          bool timeout = (fx.elapsed - fx.phaseStart) >= fx.missileDuration * 1.5f;
                          if (allArrived || timeout)
                          {
                              TriggerVanishEvents(fx);
                              fx.phase = SkillEffectPhase.Impact;
                            fx.phaseStart = fx.elapsed;
                        }
                        break;


                      case SkillEffectPhase.Impact:
                      {
                          // KMissle checks CurrentLife >= LifeTime before OnFly.
                          // Stationary TangMen missiles use Impact phase as flight lifecycle.
                          int stationaryLifeTick = Mathf.FloorToInt((fx.elapsed - fx.phaseStart) * 18f + 0.0001f);
                          if (fx.pcStationaryLifetimeOverride && stationaryLifeTick >= fx.pcMissileLifeTicks)
                          {
                              TriggerStationaryCollision(fx);
                              fx.phase = SkillEffectPhase.Finished;
                              break;
                          }
                          TriggerFlyEvents(fx);
                          if (fx.elapsed - fx.phaseStart >= fx.impactDuration)
                          {
                              // Flight missiles already dispatched collision on arrival.
                              // Only stationary KMissle lifetime owns a terminal collision.
                              if (fx.pcStationaryLifetimeOverride)
                                  TriggerStationaryCollision(fx);
                              fx.phase = SkillEffectPhase.Finished;
                          }
                          break;
                      }
                }

                if (fx.phase == SkillEffectPhase.Finished)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
        }

          public List<ActiveSkillEffect> GetActiveEffects() => new(_activeEffects);

          /// <summary>
          /// PC state durations are stored in 18 Hz ticks in state attribute value2.
          /// Active aura visuals expire with their longest finite state; learned passive
          /// states use -1 and therefore remain visible until the passive is removed.
          /// </summary>
          internal static float ResolveStateAuraDurationSeconds(SkillDefinition skill, int level)
          {
              var stateAttributes = skill?.GetPcLevelData(level)?.state;
              if (stateAttributes == null || stateAttributes.Count == 0)
                  return float.MaxValue;

              int maxDurationTicks = 0;
              foreach (var attribute in stateAttributes)
              {
                  if (attribute == null) continue;
                  if (attribute.value2 < 0) return float.MaxValue;
                  if (attribute.value2 > maxDurationTicks)
                      maxDurationTicks = attribute.value2;
              }

              return maxDurationTicks > 0
                  ? maxDurationTicks / 18f
                  : float.MaxValue;
          }

            private bool UpdateMultiMissile(ActiveSkillEffect fx, float dt)
            {
                TriggerFlyEvents(fx);
                if (fx.missilePositions == null)
              {
                  // Single missile: velocity-based toward target
                  Vector2 liveTarget = fx.getCurrentTargetPos != null ? fx.getCurrentTargetPos() : fx.targetPos;
                  Vector2 dir = liveTarget - fx.currentMissilePos;
                  float dist = dir.magnitude;
                  if (dist > fx.arrivalRadius)
                  {
                      float step = fx.missileSpeed * dt;
                      fx.currentMissilePos = step >= dist ? liveTarget : fx.currentMissilePos + (dir / dist) * step;
                  }
                  else
                  {
                      fx.currentMissilePos = fx.ResolveMissileTarget(-1);
                  }
                return false;
            }

            if (UsesPcFollowTickSimulation(fx))
            {
                fx.missileTickAccumulator += Mathf.Max(0f, dt);
                int simulatedTicks = 0;
                while (fx.missileTickAccumulator + 0.000001f >= PcMissileTickSeconds &&
                       simulatedTicks < MaxPcMissileTicksPerUpdate)
                {
                    fx.missileTickAccumulator -= PcMissileTickSeconds;
                    if (fx.missileTickAccumulator < 0f)
                        fx.missileTickAccumulator = 0f;

                    AdvancePcFollowMissilesOneTick(fx);
                    ResolveMissileCollisions(fx);
                    simulatedTicks++;
                }
                return true;
            }

            for (int i = 0; i < fx.missilePositions.Length; i++)
            {
                if (fx.missileArrived != null && i < fx.missileArrived.Length && fx.missileArrived[i])
                    continue;

                Vector2 pos = fx.missilePositions[i];
                Vector2 target = fx.ResolveMissileTarget(i);
                Vector2 dir = target - pos;
                float dist = dir.magnitude;

                if (dist <= fx.arrivalRadius)
                {
                    fx.missilePositions[i] = target;
                }
                else
                {
                    float step = fx.missileSpeed * dt;
                    fx.missilePositions[i] = step >= dist ? target : pos + (dir / dist) * step;
                }
            }
            return false;
        }

          private static void TriggerFlyEvents(ActiveSkillEffect fx)
        {
            if (!fx.pcFlyEventEnabled || fx.pcFlyEventIntervalTicks <= 0)
                return;
            int tick = Mathf.FloorToInt((fx.elapsed - fx.phaseStart) * 18f + 0.0001f);
            int ordinal = tick / fx.pcFlyEventIntervalTicks;
            if (ordinal <= 0 || fx.missileFlyEventOrdinals == null) return;
            for (int i = 0; i < fx.missileFlyEventOrdinals.Length; i++)
            {
                if ((fx.missileArrived != null && i < fx.missileArrived.Length && fx.missileArrived[i]) ||
                    ordinal <= fx.missileFlyEventOrdinals[i]) continue;
                fx.missileFlyEventOrdinals[i] = ordinal;
                  fx.onMissileFlyEvent?.Invoke(fx, i, fx.missilePositions != null && i < fx.missilePositions.Length
                      ? fx.missilePositions[i] : fx.targetPos);
              }
          }

          private void TriggerVanishEvents(ActiveSkillEffect fx)
          {
              if (fx.missileVanishEventFired == null) return;
              for (int i = 0; i < fx.missileVanishEventFired.Length; i++)
              {
                  if (fx.missileVanishEventFired[i]) continue;
                  fx.missileVanishEventFired[i] = true;
                  Vector2 point = fx.missilePositions != null && i < fx.missilePositions.Length
                      ? fx.missilePositions[i]
                      : fx.ResolveMissileTarget(i);
                  // Runtime callback owns nested visuals when present; service is fallback.
                  fx.onMissileVanishEvent?.Invoke(fx, i, point);
                  if (fx.onMissileVanishEvent == null)
                      SpawnVanishSubEffect(fx, point);
              }
          }

        private static bool UsesPcFollowTickSimulation(ActiveSkillEffect fx)
        {
            int count = fx.missilePositions?.Length ?? 0;
            return fx.pcMissileMoveKind == 5 &&
                   count > 0 &&
                   fx.missileDirections != null &&
                   fx.missileDirections.Length == count &&
                   fx.missileFollowTickCounters != null &&
                   fx.missileFollowTickCounters.Length == count;
        }

        private static void AdvancePcFollowMissilesOneTick(ActiveSkillEffect fx)
        {
            float step = fx.pcMissileSpeedPerTick > 0
                ? fx.pcMissileSpeedPerTick
                : fx.missileSpeed * PcMissileTickSeconds;

            for (int i = 0; i < fx.missilePositions.Length; i++)
            {
                if (fx.missileArrived != null && i < fx.missileArrived.Length && fx.missileArrived[i])
                    continue;

                Vector2 pos = fx.missilePositions[i];
                if (fx.getCurrentTargetPos != null)
                {
                    // PC KMissle.cpp: if (m_nTempParam1++ >= 8), retarget before moving tick 9.
                    if (fx.missileFollowTickCounters[i] >= PcFollowRetargetCounterMax)
                    {
                        Vector2 toTarget = fx.ResolveMissileTarget(i) - pos;
                        if (toTarget.sqrMagnitude > 0.000001f)
                            fx.missileDirections[i] = toTarget.normalized;
                        fx.missileFollowTickCounters[i] = 0;
                    }
                    else
                    {
                        fx.missileFollowTickCounters[i]++;
                    }
                }

                Vector2 direction = fx.missileDirections[i];
                if (direction.sqrMagnitude <= 0.000001f)
                    continue;
                direction.Normalize();
                fx.missileDirections[i] = direction;

                Vector2 next = pos + direction * step;
                Vector2 target = fx.ResolveMissileTarget(i);
                if (PassesWithinRadius(pos, next, target, fx.rendRadius))
                    next = target;
                fx.missilePositions[i] = next;
            }
        }

        private static bool PassesWithinRadius(Vector2 from, Vector2 to, Vector2 point, float radius)
        {
            Vector2 segment = to - from;
            float segmentLengthSq = segment.sqrMagnitude;
            if (segmentLengthSq <= 0.000001f)
                return Vector2.Distance(from, point) <= radius;

            float t = Mathf.Clamp01(Vector2.Dot(point - from, segment) / segmentLengthSq);
            Vector2 closest = from + segment * t;
            return (closest - point).sqrMagnitude <= radius * radius;
        }

        private void ResolveMissileCollisions(ActiveSkillEffect fx)
        {
            for (int si = 0; si < (fx.missileArrived?.Length ?? 0); si++)
            {
                if (fx.missileArrived[si]) continue;
                Vector2 targetPos = fx.ResolveMissileTarget(si);
                Vector2 mp = fx.missilePositions != null && si < fx.missilePositions.Length
                    ? fx.missilePositions[si]
                    : fx.currentMissilePos;
                Vector2 origin = fx.missileOrigins != null && si < fx.missileOrigins.Length
                    ? fx.missileOrigins[si]
                    : fx.casterPos;

                bool isHomingMissile = fx.getCurrentTargetPos != null && fx.pcMissileMoveKind == 5;
                bool collided;
                if (!isHomingMissile)
                {
                    float targetDist = Vector2.Distance(fx.targetPos, origin);
                    float traveled = Vector2.Distance(mp, origin);
                    collided = traveled >= targetDist - fx.rendRadius;
                }
                else
                {
                    collided = Vector2.Distance(mp, targetPos) <= fx.rendRadius;
                }

                if (!collided) continue;

                fx.missileArrived[si] = true;
                if (fx.missileExplodeStartTime != null && si < fx.missileExplodeStartTime.Length)
                    fx.missileExplodeStartTime[si] = fx.elapsed;
                Vector2 collidePos = !isHomingMissile
                    ? origin + (targetPos - origin).normalized * Vector2.Distance(fx.targetPos, origin)
                    : mp;
                TriggerSauXe(fx, collidePos);
                fx.onMissileCollided?.Invoke(fx, si, collidePos);
                OnMissileCollided?.Invoke(fx, si, collidePos);
                if (!string.IsNullOrEmpty(fx.impactSoundPath))
                    OnCastSound?.Invoke(fx.impactSoundPath);
                // Runtime callback owns nested visuals when present; service is fallback.
                if (fx.onMissileCollided == null)
                    SpawnCollideSubEffect(fx, collidePos);
            }
        }



        private void TriggerSauXe(ActiveSkillEffect fx, Vector2 position)
        {
            // Sâu xé: proximity rend visual — a small impact flash at the missile position.
            // PC: each dragon independently triggers CollideEvent (skill 389) upon proximity.
            // This can be extended later to queue per-dragon damage in CombatRuntimeService.
            fx.rendPositions ??= new List<Vector2>();
            fx.rendPositions.Add(position);
        }

        private void TriggerStationaryCollision(ActiveSkillEffect fx)
        {
            if (fx.stationaryCollisionFired) return;
            fx.stationaryCollisionFired = true;
            var skill = _catalog?.Resolve(fx.skillId);
            if (skill == null || skill.collideSkillId <= 0) return;
            Vector2 position = fx.casterPos;
            fx.onMissileCollided?.Invoke(fx, 0, position);
            OnMissileCollided?.Invoke(fx, 0, position);
            if (fx.onMissileCollided == null)
                SpawnCollideSubEffect(fx, position);
        }

        private void SpawnVanishSubEffect(ActiveSkillEffect parentFx, Vector2 position)
        {
            var parentSkill = _catalog?.Resolve(parentFx.skillId);
            SpawnLifecycleSubEffect(parentSkill?.vanishSkillId ?? 0, parentFx, position);
        }

        private void SpawnCollideSubEffect(ActiveSkillEffect parentFx, Vector2 position)
        {
            var parentSkill = _catalog?.Resolve(parentFx.skillId);
            SpawnLifecycleSubEffect(parentSkill?.collideSkillId ?? 0, parentFx, position);
        }

        private void SpawnLifecycleSubEffect(int subSkillId, ActiveSkillEffect parentFx, Vector2 position)
        {
            if (subSkillId <= 0) return;
            var subSkill = _catalog?.Resolve(subSkillId);
            if (subSkill == null) return;
            CreateSubEffect(subSkill, parentFx, position);
        }

        private ActiveSkillEffect CreateSubEffect(SkillDefinition subSkill, ActiveSkillEffect parentFx, Vector2 position)
        {
            const int maxLifecycleDepth = 8;
            if (parentFx.lifecycleDepth >= maxLifecycleDepth ||
                parentFx.lifecycleSkillIds != null && parentFx.lifecycleSkillIds.Contains(subSkill.skillId))
                return null;

            // KSkills::OnMissleEvent casts at missile position; it does not replay cast animation.
            // suppressCastAudio=true: PC OnMissleEvent uses CastMissles, not KSkill::Cast, so the
            // sub-skill's ManCastSnd is NOT replayed. Flight/impact status audio is also nulled
            // below: the runtime combat layer owns the collide/vanish/fly event sub-skill audio as
            // a distinct temporal event, so the visual fallback must not double-count the parent's
            // status sounds in the same frame.
            var subFx = PlaySkillCast(subSkill, position, position, parentFx.skillLevel, null, null, suppressCastAudio: true);
            if (subFx == null) return null;
            subFx.flightSoundPath = null;
            subFx.impactSoundPath = null;
            subFx.color = parentFx.color;
            subFx.lifecycleDepth = parentFx.lifecycleDepth + 1;
            if (subFx.phase == SkillEffectPhase.Finished)
                return subFx;
            subFx.lifecycleSkillIds = parentFx.lifecycleSkillIds != null
                ? new HashSet<int>(parentFx.lifecycleSkillIds)
                : new HashSet<int> { parentFx.skillId };
            subFx.lifecycleSkillIds.Add(subSkill.skillId);
            subFx.preCastDuration = 0f;
            subFx.phase = subFx.HasMissile ? SkillEffectPhase.Missile : SkillEffectPhase.Impact;
            subFx.phaseStart = subFx.elapsed;
            return subFx;
        }

        private Sprite ResolveMissileSprite(SkillDefinition skill)
        {
            // PC missile SPR is identified by childSkillId.
            // These are stored in spr.pak as hashed filenames.
            // Fallback to a generic projectile sprite.
            string missileKey = $"missile_{skill.childSkillId}";
            return _sprService?.ResolveSprite(missileKey, 32, 32);
        }

        private static void SetupPcMissile(ActiveSkillEffect fx, string missileKey, int missileFrames, int missileDirs, int missileIntervalTicks, int speedPerTick, int lifeTicks, string impactKey, int impactFrames, int impactDirs, int impactIntervalTicks, Color color)
        {
            fx.color = color;
            fx.trailEnabled = false;
            fx.pcMissileSpriteKey = missileKey;
            fx.pcMissileTotalFrames = missileFrames;
            fx.pcMissileDirections = missileDirs;
            fx.pcMissileIntervalTicks = missileIntervalTicks;
            fx.pcMissileSpeedPerTick = speedPerTick;
            fx.pcMissileLifeTicks = lifeTicks;
            fx.pcImpactSpriteKey = impactKey;
            fx.pcImpactTotalFrames = impactFrames;
            fx.pcImpactDirections = impactDirs;
            fx.pcImpactIntervalTicks = impactIntervalTicks;
            fx.impactDuration = impactFrames > 0
                ? (impactFrames * Mathf.Max(1, impactIntervalTicks)) / 18f
                : fx.impactDuration;
            // PC KMissle: m_nXFactor is Q10 (≈±1024) direction cosine.
            // nDOffsetX = m_nSpeed * m_nXFactor → actual pixel step per tick = m_nSpeed.
            // All positions (casterPos/targetPos) are in raw PC pixel coords (PPU=1f),
            // so speed in pixels/s = speedPerTick × 18fps — no PPU conversion needed.
            fx.missileSpeed = speedPerTick * 18f; // PC pixels per second (world units = PC pixels at PPU=1f)
            fx.missileDuration = lifeTicks / 18f;
        }

        private static void SetupPcPreCast(ActiveSkillEffect fx, string key, int frames, int dirs, int intervalTicks)
        {
            fx.pcPreCastSpriteKey = key;
            fx.pcPreCastTotalFrames = frames;
            fx.pcPreCastDirections = dirs;
            fx.pcPreCastIntervalTicks = intervalTicks;
        }

        private static void SetupPcStationaryEffect(ActiveSkillEffect fx, string key, int frames, int dirs, int intervalTicks, Color color)
        {
            fx.color = color;
            fx.trailEnabled = false;
            fx.pcMissileSpriteKey = null;
            fx.pcMissileTotalFrames = 0;
            fx.pcMissileDirections = 0;
            fx.pcImpactSpriteKey = key;
            fx.pcImpactTotalFrames = frames;
            fx.pcImpactDirections = dirs;
            fx.pcImpactIntervalTicks = intervalTicks;
            fx.missileCount = 0;
            fx.missileDuration = 0.01f;
            fx.impactDuration = (frames * Mathf.Max(1, intervalTicks)) / 18f;
        }

        private static void ApplyPcStationaryLifetime(ActiveSkillEffect fx, int lifeTicks)
        {
            fx.pcMissileLifeTicks = lifeTicks;
            fx.missileDuration = lifeTicks / 18f;
            fx.impactDuration = fx.missileDuration;
            fx.pcStationaryLifetimeOverride = true;
        }

        private void SetupPcKangLongSpread(ActiveSkillEffect fx, int count, int angleStep64, int firstStep)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileOrigins = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            fx.missileArrived = new bool[count];
            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float targetDist = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= targetDist;

            // PC parity: non-homing missiles fly their full lifetime range (speed * duration)
            float distance = fx.missileSpeed * fx.missileDuration;
            if (distance < targetDist) distance = targetDist;

            // [DEBUG 2026-07-16] KangLong visual: chỉ render 1 quả cầu thay vì 15. Log setup.
            SubsystemLog.Info("SkillFx", $"[KangLongSetup] skill={fx.skillId} count={count} angleStep64={angleStep64} firstStep={firstStep} " +
                $"speed={fx.missileSpeed} duration={fx.missileDuration} pcSpeedPerTick={fx.pcMissileSpeedPerTick} pcLifeTicks={fx.pcMissileLifeTicks} " +
                $"targetDist={targetDist} distance={distance} caster={fx.casterPos} target={fx.targetPos}");

            for (int i = 0; i < count; i++)
            {
                float offset = (count - 1) / 2f - i;
                float dSubDir = angleStep64 * offset;
                float angleDeg = dSubDir * 360f / 64f;
                Vector2 dir = Rotate(baseDir, angleDeg);
                Vector2 startPos = fx.casterPos + dir * Mathf.Max(0f, firstStep);
                fx.missileOrigins[i] = startPos;
                fx.missilePositions[i] = startPos;
                fx.missileTargets[i] = fx.casterPos + dir * distance;
            }
        }


        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private void SetupPcPhiLongSpread(ActiveSkillEffect fx, int count, int spacing)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileOrigins = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            fx.missileDirections = new Vector2[count];
            fx.missileFollowTickCounters = new int[count];
            fx.missileArrived = new bool[count];
            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float targetDist = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= targetDist;

            // PC parity: non-homing/fallback uses full range
            float distance = fx.missileSpeed * fx.missileDuration;
            if (distance < targetDist) distance = targetDist;

            Vector2 perpDir = new Vector2(-baseDir.y, baseDir.x);
            // PC KSkill::CastWall starts at -Param1 * count / 2, then adds Param1.
            float currentOffset = -spacing * count / 2f;
            for (int i = 0; i < count; i++)
            {
                Vector2 perp = perpDir * currentOffset;
                fx.missileOrigins[i] = fx.casterPos + perp;
                fx.missilePositions[i] = fx.casterPos + perp;
                fx.missileTargets[i] = fx.casterPos + baseDir * distance + perp;
                fx.missileDirections[i] = baseDir;
                currentOffset += spacing;
            }
        }

        // PC KSkills.cpp CastSpread (SKILL_MF_Spread, e.g. 165 "Vô Ngã Vô Kiếm" wudang.lua):
        // missiles fan around castDir (caster->target). nCurMSRadius = childNum/2,
        // dir_i = nDir + Value1*(i - half) in MaxMissleDir=64 dir units; spawn offset
        // nFirstStep = Value2 px along dir_i (0 = at caster). Trước fix: full 360° xoay
        // quanh caster không theo castDir (tia bay lung tung, "quạt quay" sai PC).
        private void SetupPcFanMissiles(SkillDefinition skill, ActiveSkillEffect fx, int count)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            fx.missileDirections = new Vector2[count];
            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float targetDist = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= targetDist;
            float stepRad = Mathf.Deg2Rad * (360f / 64f) * (skill != null && skill.missileDirStep > 0 ? skill.missileDirStep : 1);
            int half = count / 2;
            float firstStep = skill != null ? Mathf.Max(0, skill.missileFirstStep) : 0f;
            float distance = Mathf.Max(1f, fx.pcMissileSpeedPerTick * fx.pcMissileLifeTicks);
            if (distance < targetDist) distance = targetDist;
            fx.missileDuration = fx.pcMissileLifeTicks / 18f;
            for (int i = 0; i < count; i++)
            {
                float angle = (i - half) * stepRad;
                float c = Mathf.Cos(angle), sn = Mathf.Sin(angle);
                var dir = new Vector2(baseDir.x * c - baseDir.y * sn,
                                      baseDir.x * sn + baseDir.y * c);
                fx.missilePositions[i] = fx.casterPos + dir * firstStep;
                fx.missileTargets[i] = fx.casterPos + dir * (firstStep + distance);
                fx.missileDirections[i] = dir;
            }
        }

        // PC KSkills.cpp CastCircle (SKILL_MF_Circle, e.g. gaibang.lua bangda_egou 125 "Bổng Đả Ác Cẩu"):
        // m_nChildSkillNum missiles evenly spaced over full 360° (nDirPerNum = MaxMissleDir/ChildSkillNum),
        // missile 0 along caster→target dir, spawn at caster (nFirstStep=Value2=0), fly full lifetime range.
        // Trước fix (2026-07-17): radius cố định 1.5 → 16 tia bổng chụm quanh player thay vì tỏa ra.
        private void SetupSurroundMissiles(ActiveSkillEffect fx, int count)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileOrigins = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float targetDist = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= targetDist;

            // PC parity: non-homing missiles fly their full lifetime range (speed * duration).
            float distance = fx.missileSpeed * fx.missileDuration;
            if (distance < targetDist) distance = targetDist;

            float angleStep = 360f / Mathf.Max(1, count);
            for (int i = 0; i < count; i++)
            {
                Vector2 dir = Rotate(baseDir, i * angleStep);
                fx.missileOrigins[i] = fx.casterPos;
                fx.missilePositions[i] = fx.casterPos;
                fx.missileTargets[i] = fx.casterPos + dir * distance;
            }
        }

        // PC SKILL_MF_Zone (form 5): missiles phân bố đều quanh caster tại góc cố định,
        // bay trong bán kính attackRadius. Dùng cho Cái Bang skill 125 (天下无狗) + NPC variant 1539.
        // PC gaibang.lua::tianxia_wugou: skill_misslenum_v L1=1, L20=3 (Unity base 3), attackradius L20=512.
        private void SetupPcZoneMissiles(ActiveSkillEffect fx, int count, int radiusWu)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            fx.missileOrigins = new Vector2[count];
            float angleStep = 360f / Mathf.Max(1, count);
            float radius = Mathf.Max(1f, radiusWu);
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Deg2Rad * (i * angleStep);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                fx.missilePositions[i] = fx.casterPos;
                fx.missileOrigins[i] = fx.casterPos;
                fx.missileTargets[i] = fx.casterPos + dir * radius;
            }
        }

    }

    public enum SkillEffectPhase
    {
        PreCast,
        Missile,
        Impact,
        Finished,
    }

      public class ActiveSkillEffect
      {
          public bool authoritativeLifecycle;
          public string authoritativeMissileInstanceId;
          public int skillId;
        public int skillLevel;
        public int lifecycleDepth;
        public HashSet<int> lifecycleSkillIds;
        public bool stationaryCollisionFired;
        public string skillName;
        public Vector2 casterPos;
        public Vector2 targetPos;
        public float startTime;
        public float elapsed;
        public SkillEffectPhase phase;
        public float phaseStart;

        // PreCast
        public float preCastDuration = 0.15f;
        public Sprite preCastSprite;

        // Missile
        public SkillMissileForm missileForm;
        public Sprite missileSprite;
        public float missileSpeed = 6f;
        public float missileDistance;
        public float missileDuration;
        public Vector2 currentMissilePos;
        public int missileCount = 1;
        public Vector2[] missilePositions;
        public Vector2[] missileOrigins;
        public Vector2[] missileTargets;
        public Vector2[] missileTargetOffsets;
        public Vector2[] missileDirections;
        public int[] missileFollowTickCounters;
        public float missileTickAccumulator;
        public bool[] missileArrived;
        public float[] missileExplodeStartTime;
        public float arrivalRadius = 1f;

        public float rendRadius = 4f;
        public List<Vector2> rendPositions;
        // Multiplier for PC missile/impact/precast SpriteRenderer.localScale.
        // PC visual rows provide no scale field; native SPR size is canonical.
        public float pcSpriteRenderScale = 1f;

        /// <summary>
        /// Optional live target position getter for homing missiles.
        /// PC: missiles track the enemy NPC's current position each tick.
        /// PC missles.txt column MoveKind=5 = target-tracking ("dí") — applied when
        /// the skill child missile is configured with MoveKind=5 (e.g. Phi Long 166,
        /// Thiên Hạ Vô Cẩu 168, etc.). See <c>Assets/StreamingAssets/Reference/PcMissles.txt</c>.
        /// When null, missiles fly toward the cast-time targetPos (MoveKind=1 straight line).
        /// </summary>
        public Func<Vector2> getCurrentTargetPos;

        // Impact
        public float impactDuration = 0.6f;

        // Visual
        public Color color = Color.white;
        public bool trailEnabled;
        public bool isAura;
          public bool hasStateSourceKey;
          public CombatStateSourceKey stateSourceKey;
          public bool stateOwnerMounted;
        public bool isHitFlash;
        public float auraDuration = 1.2f;
        public float auraRadius = 48f;
        public float auraPulseRate = 4f;

        // PC missile SPR metadata from Missles.txt. Used for exact JXWin sprite playback.
        public string pcMissileSpriteKey;
        public string pcImpactSpriteKey;
        public string pcPreCastSpriteKey;
        public int pcPreCastTotalFrames;
        public int pcPreCastDirections;
        public int pcPreCastIntervalTicks = 1;
        public int pcMissileTotalFrames;
        public int pcMissileDirections;
        public int pcMissileIntervalTicks = 1;
        public int pcMissileSpeedPerTick;
        public int pcMissileLifeTicks;
        public int pcImpactTotalFrames;
        public int pcImpactDirections;
        public int pcImpactIntervalTicks = 1;
        public int pcMissileMoveKind = 1;
        public bool pcStationaryLifetimeOverride;


        // (pcAuraFrameStart/End kept as no-op fields for backward compat with SkillEffectWorldOverlay; not used in default data-driven visuals)
        public int pcAuraFrameStart;
        public int pcAuraFrameEnd;
        public int stateAuraPos;
        public bool HasPcMissileSprite => !string.IsNullOrEmpty(pcMissileSpriteKey) && pcMissileTotalFrames > 0 && pcMissileDirections > 0;
        public bool HasPcImpactSprite => !string.IsNullOrEmpty(pcImpactSpriteKey) && pcImpactTotalFrames > 0;
        public bool HasPcPreCastSprite => !string.IsNullOrEmpty(pcPreCastSpriteKey) && pcPreCastTotalFrames > 0 && pcPreCastDirections > 0;
        public bool HasMissile => missileForm != SkillMissileForm.None && missileCount > 0;
        public string castSoundPath;  // PC skills.txt ManCastSnd/FMCastSnd.
        public string flightSoundPath; // PC SndFile2/MS_DoFly, played per missile instance.
        public string impactSoundPath; // PC SndFile4/MS_DoCollision, played per collision.
            public Action<ActiveSkillEffect, int, Vector2> onMissileCollided;
            public Action<ActiveSkillEffect, int, Vector2> onMissileFlyEvent;
            public Action<ActiveSkillEffect, int, Vector2> onMissileVanishEvent;
          public bool pcFlyEventEnabled;
          public int pcFlyEventIntervalTicks;
          public int pcFlySkillId;
            public int[] missileFlyEventOrdinals;
            public bool[] missileVanishEventFired;

        public static Vector2 ResolveMissileTarget(ActiveSkillEffect fx, int index)
        {
            return fx.ResolveMissileTarget(index);
        }

        public Vector2 ResolveMissileTarget(int index)
        {
            bool isHomingMissile = getCurrentTargetPos != null && pcMissileMoveKind == 5;
            Vector2 target = isHomingMissile ? getCurrentTargetPos() : targetPos;

            if (index >= 0)
            {
                if (missileTargets != null && index < missileTargets.Length)
                    return isHomingMissile ? target : missileTargets[index];
            }

            return target;
        }

        public Vector2 ResolveMissileDirection(int index)
        {
            if (index >= 0 &&
                missileDirections != null &&
                index < missileDirections.Length &&
                missileDirections[index].sqrMagnitude > 0.000001f)
            {
                return missileDirections[index].normalized;
            }

            Vector2 from = index >= 0 && missilePositions != null && index < missilePositions.Length
                ? missilePositions[index]
                : currentMissilePos;
            Vector2 direction = ResolveMissileTarget(index) - from;
            return direction.sqrMagnitude > 0.000001f ? direction.normalized : Vector2.zero;
        }

    }
}
// recompile
