using System;
using System.Collections.Generic;
using UnityEngine;
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
        private readonly SprRuntimeService _sprService;
        private readonly SkillCatalog _catalog;
        private readonly List<ActiveSkillEffect> _activeEffects = new();
        private readonly PcSkillVisualAutoMapper _autoMapper = new();
        private bool _autoMapperReady;
        /// <summary>
        /// Callback fired when a skill cast sound should be played.
        /// Wired by SandboxManager → AudioService.PlaySkillCast.
        /// </summary>
        public Action<string> OnCastSound;

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

            var config = _autoMapper.GetVisualConfig(skill);
            if (config == null) return;

            // Apply faction default color
            fx.color = config.lightColor;
            fx.castSoundPath = config.castSoundPath;

            // Cái Bang dragon SPRs (Tinh Kiem PAK variants) are very small relative
            // to the mobile viewport (PC tile scale ~32-64px, mobile chars ~128-256px).
            // Scale 2.5x so the dragon body is unmistakably visible. Affects flight
            // missile + impact + precast SpriteRenderers.
            if (PcCaiBangLuaLevelService.Applies(skill.skillId))
            {
                fx.pcSpriteRenderScale = 2.5f;
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

                SetupPcMissile(fx,
                    flightKey,
                    config.flightFrames,
                    System.Math.Max(1, config.flightDirections),
                    System.Math.Max(1, config.flightIntervalTicks),
                    missileSpeed,
                    config.missileLifetime,
                    explodeKey,
                    config.explodeFrames,
                    System.Math.Max(1, config.explodeDirections),
                    System.Math.Max(1, config.explodeIntervalTicks),
                    config.lightColor);

                // Multi-missile spread for fan/surround forms
                if (skill.missileForm == SkillMissileForm.Fan || skill.missileForm == SkillMissileForm.Surround)
                {
                    int count = System.Math.Max(1, skill.childSkillNum);
                    if (skill.missileForm == SkillMissileForm.Surround)
                        SetupSurroundMissiles(fx, count);
                    else
                        SetupPcCircleOutwardMissiles(fx, count);
                }
                // PC gaibang.lua: Single-form skills with skill_misslenum_v > 1 use homing spread.
                // E.g. Phi Long (357) L20=4, Thiên Hạ Vô Cẩu (359) L20=3, Càn Khôn (1074) L20=5.
                // [CaiBang-LuaPort 2026-06-17] PcCaiBangSkillTuning.MissileCountAtLevel replaced
                // by Lua-driven GetMissileCount from gaibang.lua skill_misslenum_v table.
                else if (PcCaiBangLuaLevelService.Applies(skill.skillId))
                {
                    int luaCount = PcCaiBangLuaLevelService.GetMissileCount(skill.skillId, level);
                    if (luaCount > 1)
                        SetupPcPhiLongSpread(fx, luaCount, 8);
                }
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
        /// Spawn buff/aura pulse at world position. Intended for passive buffs,
        /// stance skills, and temporary self effects. No fake art: clean fallback ring.
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
            return PlaySkillCast(skill, casterPos, targetPos, skillLevel, null);
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
            Func<Vector2> getCurrentTargetPos)
        {
            if (skill == null) return null;

            var effect = new ActiveSkillEffect
            {
                skillId = skill.skillId,
                skillName = skill.DisplayName,
                casterPos = casterPos,
                targetPos = targetPos,
                startTime = Time.time,
                phase = SkillEffectPhase.PreCast,
            };

            // Phase durations based on PC skill data
            // PreCast: ensure visible (min 0.25s); PC timePerCast is in ticks (~55ms each)
            effect.preCastDuration = Mathf.Max(0.25f, skill.timePerCast > 0 ? skill.timePerCast * 0.055f : 0.25f);
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
            // Trigger cast sound (PC missles.txt SoundPath)
            if (!string.IsNullOrEmpty(effect.castSoundPath))
                OnCastSound?.Invoke(effect.castSoundPath);

            // (Legacy per-faction visual overrides removed: skill visuals are now
            //  data-driven entirely from PC missles1.txt via ConfigureDataDrivenVisuals)

            // (All per-faction visual overrides removed; data-driven from PC missles1.txt only)





            _activeEffects.Add(effect);
            effect.getCurrentTargetPos = getCurrentTargetPos;
            return effect;
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

                if (fx.isAura)
                {
                    if (fx.elapsed >= fx.auraDuration)
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
                        }
                        break;

                    case SkillEffectPhase.Missile:
                        UpdateMultiMissile(fx, dt);

                        bool allArrived;
                        if (fx.missilePositions != null && fx.missilePositions.Length > 0)
                        {
                            allArrived = true;
                            for (int mi = 0; mi < fx.missilePositions.Length; mi++)
                            {
                                Vector2 targetPos = ResolveMissileTarget(fx, mi);
                                if (Vector2.Distance(fx.missilePositions[mi], targetPos) > fx.arrivalRadius)
                                    allArrived = false;
                            }
                        }
                        else
                        {
                            // Single missile: keep flying until it actually reaches the target.
                            allArrived = Vector2.Distance(fx.currentMissilePos, ResolveMissileTarget(fx, -1)) <= fx.arrivalRadius;
                        }

                        bool timeout = (fx.elapsed - fx.phaseStart) >= fx.missileDuration * 1.5f;
                        if (allArrived || timeout)
                        {
                            fx.phase = SkillEffectPhase.Impact;
                            fx.phaseStart = fx.elapsed;
                        }

                        for (int si = 0; si < (fx.missileArrived?.Length ?? 0); si++)
                        {
                            if (fx.missileArrived[si]) continue;
                            Vector2 targetPos = ResolveMissileTarget(fx, si);
                            Vector2 mp = si < fx.missilePositions.Length ? fx.missilePositions[si] : fx.currentMissilePos;
                            if (Vector2.Distance(mp, targetPos) <= fx.rendRadius)
                            {
                                fx.missileArrived[si] = true;
                                TriggerSauXe(fx, mp);
                                SpawnCollideSubEffect(fx, mp);
                            }
                        }
                        break;

                    case SkillEffectPhase.Impact:
                        if (fx.elapsed - fx.phaseStart >= fx.impactDuration)
                        {
                            fx.phase = SkillEffectPhase.Finished;
                        }
                        break;
                }

                if (fx.phase == SkillEffectPhase.Finished)
                {
                    _activeEffects.RemoveAt(i);
                }
            }
        }

        public List<ActiveSkillEffect> GetActiveEffects() => new(_activeEffects);

        private void UpdateMultiMissile(ActiveSkillEffect fx, float dt)
        {
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
                    fx.currentMissilePos = ResolveMissileTarget(fx, -1);
                }
                return;
            }

            for (int i = 0; i < fx.missilePositions.Length; i++)
            {
                Vector2 pos = fx.missilePositions[i];
                Vector2 target = ResolveMissileTarget(fx, i);
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
        }

        private static Vector2 ResolveMissileTarget(ActiveSkillEffect fx, int index)
        {
            bool hasLiveTarget = fx.getCurrentTargetPos != null;
            Vector2 target = hasLiveTarget ? fx.getCurrentTargetPos() : fx.targetPos;

            if (index >= 0)
            {
                if (hasLiveTarget && fx.missileTargetOffsets != null && index < fx.missileTargetOffsets.Length)
                    return target + fx.missileTargetOffsets[index];

                if (!hasLiveTarget && fx.missileTargets != null && index < fx.missileTargets.Length)
                    return fx.missileTargets[index];
            }

            return target;
        }

        private void TriggerSauXe(ActiveSkillEffect fx, Vector2 position)
        {
            // Sâu xé: proximity rend visual — a small impact flash at the missile position.
            // PC: each dragon independently triggers CollideEvent (skill 389) upon proximity.
            // This can be extended later to queue per-dragon damage in CombatRuntimeService.
            fx.rendPositions ??= new List<Vector2>();
            fx.rendPositions.Add(position);
        }

        private void SpawnCollideSubEffect(ActiveSkillEffect parentFx, Vector2 position)
        {
            // PC gaibang.lua skill_collideevent[3] sub-skills: each skill declares which
            // sub-skill to cast when the main missile arrives at the target.
            // 357 Phi Long → 389 Long Chiến Ư Dã (already in catalog, runtime handles damage).
            // 1073 Thời Thặng Lục Long → 1072 Ngũ Diệu Càn Khôn (visual stationary flash).
            int subSkillId = parentFx.skillId switch
            {
                1073 => 1072,
                _    => 0,
            };
            if (subSkillId == 0) return;

            var subSkill = _catalog?.Resolve(subSkillId);
            if (subSkill == null) return;

            var subFx = CreateSubEffect(subSkill, parentFx, position);
            if (subFx != null) _activeEffects.Add(subFx);
        }

        private ActiveSkillEffect CreateSubEffect(SkillDefinition subSkill, ActiveSkillEffect parentFx, Vector2 position)
        {
            var subFx = new ActiveSkillEffect
            {
                skillId        = subSkill.skillId,
                skillName      = subSkill.DisplayName,
                casterPos      = position,
                targetPos      = position,
                startTime      = Time.time,
                phase          = SkillEffectPhase.PreCast,
                color          = parentFx.color,
                impactDuration = 0.6f,
            };
            // (Per-faction ConfigureCaiBangVisuals removed - visuals data-driven only)
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
            fx.missileSpeed = speedPerTick * 18f; // PC ticks/sec ≈ 18
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

        private void SetupPcKangLongSpread(ActiveSkillEffect fx, int count, int angleStep64, int firstStep)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float distance = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= distance;
            int radius = count / 2;
            for (int i = 0; i < count; i++)
            {
                int dSubDir = angleStep64 * radius;
                float angleDeg = dSubDir * 360f / 64f;
                Vector2 dir = Rotate(baseDir, angleDeg);
                fx.missilePositions[i] = fx.casterPos + dir * Mathf.Max(0f, firstStep);
                fx.missileTargets[i] = fx.casterPos + dir * distance;
                radius--;
            }
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r);
            float s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        private void SetupPcPhiLongSpread(ActiveSkillEffect fx, int count, int param64)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileOrigins = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            fx.missileTargetOffsets = new Vector2[count];
            fx.missileArrived = new bool[count];
            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float distance = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= distance;
            Vector2 perpDir = new Vector2(-baseDir.y, baseDir.x);
            float halfSpan = count > 1 ? (count - 1) * param64 * 0.5f : 0f;
            for (int i = 0; i < count; i++)
            {
                float offset = count > 1 ? Mathf.Lerp(-halfSpan, halfSpan, i / (count - 1f)) : 0f;
                Vector2 perp = perpDir * offset;
                fx.missileOrigins[i] = fx.casterPos + perp;
                fx.missilePositions[i] = fx.casterPos + perp;
                fx.missileTargetOffsets[i] = perp;
                fx.missileTargets[i] = fx.casterPos + baseDir * distance + perp;
            }
        }

        private void SetupPcCircleOutwardMissiles(ActiveSkillEffect fx, int count)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            float angleStep = 360f / count;
            float distance = Mathf.Max(1f, fx.pcMissileSpeedPerTick * fx.pcMissileLifeTicks);
            fx.missileDuration = fx.pcMissileLifeTicks / 18f;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Deg2Rad * (i * angleStep);
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                fx.missilePositions[i] = fx.casterPos;
                fx.missileTargets[i] = fx.casterPos + dir * distance;
            }
        }

        private void SetupSurroundMissiles(ActiveSkillEffect fx, int count)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];
            float angleStep = 360f / count;
            float radius = 1.5f;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Deg2Rad * (i * angleStep);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                fx.missilePositions[i] = fx.casterPos;
                fx.missileTargets[i] = fx.casterPos + offset;
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
        public int skillId;
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
        public bool[] missileArrived;
        public float arrivalRadius = 1f;
        public float rendRadius = 4f;
        public List<Vector2> rendPositions;
        // Multiplier for PC missile/impact/precast SpriteRenderer.localScale.
        // Default 1x (native pixel size). Cái Bang dragon skills (Phi Long 357,
        // Khang Long 358, Thien Ha Vo Cau 359) override to 2.5x because their
        // Tinh Kiem PAK SPRs are sized for tile-based PC clients and look
        // tiny in the mobile viewport.
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

        // (pcAuraFrameStart/End kept as no-op fields for backward compat with SkillEffectWorldOverlay; not used in default data-driven visuals)
        public int pcAuraFrameStart;
        public int pcAuraFrameEnd;
        public bool HasPcMissileSprite => !string.IsNullOrEmpty(pcMissileSpriteKey) && pcMissileTotalFrames > 0 && pcMissileDirections > 0;
        public bool HasPcImpactSprite => !string.IsNullOrEmpty(pcImpactSpriteKey) && pcImpactTotalFrames > 0;
        public bool HasPcPreCastSprite => !string.IsNullOrEmpty(pcPreCastSpriteKey) && pcPreCastTotalFrames > 0 && pcPreCastDirections > 0;
        public bool HasMissile => missileForm != SkillMissileForm.None && missileCount > 0;
        public string castSoundPath;  // PC missles.txt SoundPath: \sound\skill\sound_k0XX.wav
    }
}
