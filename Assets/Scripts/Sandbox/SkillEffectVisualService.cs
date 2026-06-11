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

                SetupPcMissile(fx,
                    flightKey,
                    config.flightFrames,
                    System.Math.Max(1, config.flightDirections),
                    System.Math.Max(1, config.flightIntervalTicks),
                    config.missileSpeed,
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

            // Legacy per-faction overrides (kept for specific CaiBang multi-missile spread)
            // These will only override if the data-driven setup didn't already configure visuals
            ConfigureCaiBangVisuals(skill, effect, skillLevel);





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
                skillId       = subSkill.skillId,
                skillName     = subSkill.DisplayName,
                casterPos     = position,
                targetPos     = position,
                startTime     = Time.time,
                phase         = SkillEffectPhase.PreCast,
                color         = parentFx.color,
                impactDuration = 0.6f,
            };
            ConfigureCaiBangVisuals(subSkill, subFx, 20);
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

        private void ConfigureWuDangVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            // Source: /var/www/vltksource_new/vl_update_27/Client 6.0/settings/missles.txt
            // Source: /var/www/vltksource_new/vl_update_27/Client 6.0/settings/skills.txt
            // Keys are PC path hashes used by StreamingAssets/Sprites/{uid}.spr when extracted.
            switch (skill.skillId)
            {
                case 153: // 怒雷指, missile 24: Speed=20, LifeTime=16, AnimFile2 wd_01_怒雷指
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "5698379e", 64, 16, 1, 20, 16, "55542141", 6, 1, 2, new Color(156f/255f, 211f/255f, 255f/255f));
                    break;
                case 155: // 沧海明月, missile 25: Speed=20, LifeTime=16, AnimFile2 wd_02_沧海明月
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "55542141", 64, 16, 1, 20, 16, "8de48699", 6, 1, 2, new Color(156f/255f, 211f/255f, 255f/255f));
                    break;
                case 158: // 剑飞惊天, missile 26: Speed=0, LifeTime=16, stationary area thunder
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcStationaryEffect(fx, "7bcefae7", 16, 1, 1, new Color(156f/255f, 211f/255f, 255f/255f));
                    break;
                case 159: // 七星阵, child missile 211: Speed=20, LifeTime=6
                    SetupPcMissile(fx, "8de48699", 8, 1, 2, 20, 6, "8de48699", 8, 1, 2, new Color(156f/255f, 211f/255f, 255f/255f));
                    break;
                case 164: // 搏击二复, missile 28: Speed=0, LifeTime=12, stationary range damage
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, new Color(156f/255f, 211f/255f, 255f/255f));
                    break;
                case 165: // 无我无剑, missile 29: Speed=20, LifeTime=16, ChildSkillNum=16 fan/surround burst
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "7bcefae7", 64, 16, 1, 20, 16, "8de48699", 6, 1, 2, new Color(156f/255f, 211f/255f, 255f/255f));
                    SetupPcCircleOutwardMissiles(fx, Math.Max(1, skill.childSkillNum));
                    break;
            }
        }

        private void ConfigureShaolinVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            if (!PcCombatCatalogFactory.IsShaolinSkill(skill.skillId)) return;

            switch (skill.skillId)
            {
                case 10: // Kim Cang Phục Ma
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 18, 20, "2ed0ae8f", 12, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 11: // Hoành Tảo Lục Hợp
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 13: // Lập Địa Thành Phật
                    SetupPcStationaryEffect(fx, "9ba1b99d", 13, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 14: // Hàng Long Bất Vũ
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 16, 30, "2ed0ae8f", 12, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 15: // Bất Động Minh Vương
                    SetupPcStationaryEffect(fx, "7770c465", 20, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 16: // La Hán Trận
                    fx.color = new Color(255f/255f, 215f/255f, 0f);
                    fx.isAura = true;
                    break;
                case 17: // Long Trảo Hổ Trảo
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "afb1607e", 64, 16, 1, 26, 20, "8a1df06d", 8, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 18: // Huệ Nhãn Chú
                    SetupPcStationaryEffect(fx, "ea9d621d", 15, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 19: // Ma Ha Vô Lượng
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "a31b9f04", 80, 16, 1, 28, 20, "c33e96c2", 6, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    SetupPcCircleOutwardMissiles(fx, 2);
                    break;
                case 20: // Sư Tử Hống
                    SetupPcStationaryEffect(fx, "8de48699", 15, 1, 1, new Color(255f/255f, 215f/255f, 0f));
                    break;
                default:
                    // Data-driven visual handled by ConfigureDataDrivenVisuals above.
                    // Legacy hardcode does not override for this skill.
                    break;
            }
        }

        private void ConfigureTangMenVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            if (!PcCombatCatalogFactory.IsTangMenSkill(skill.skillId)) return;

            switch (skill.skillId)
            {
                case 45: // Tích Lịch Đơn
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 14, 30, "2ed0ae8f", 12, 1, 2, new Color(133f/255f, 222f/255f, 96f/255f));
                    break;
                case 47: // Đoạt Hồn Tiêu
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 24, 30, "2ed0ae8f", 12, 1, 2, new Color(133f/255f, 222f/255f, 96f/255f));
                    break;
                case 50: // Truy Tâm Tiễn
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 24, 30, "2ed0ae8f", 12, 1, 2, new Color(133f/255f, 222f/255f, 96f/255f));
                    if (skill.childSkillNum > 1)
                        SetupPcCircleOutwardMissiles(fx, skill.childSkillNum);
                    break;
                case 54: // Mạn Thiên Hoa Vũ
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 18, 30, "2ed0ae8f", 12, 1, 2, new Color(133f/255f, 222f/255f, 96f/255f));
                    break;
                case 55: // Thối Độc Thuật
                case 57: // Băng Phách Hàn Quang
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, new Color(133f/255f, 222f/255f, 96f/255f));
                    break;
                case 58: // Thiên La Địa Võng
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 26, 30, "2ed0ae8f", 12, 1, 2, new Color(133f/255f, 222f/255f, 96f/255f));
                    break;
                default:
                    // Data-driven visual handled by ConfigureDataDrivenVisuals above.
                    // Legacy hardcode does not override for this skill.
                    break;
            }
        }


        private void ConfigureCaiBangVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            // Each CaiBang skill has unique visual from PC source.
            // SkillId mappings from PC Skills.txt:
            switch (skill.skillId)
            {
                // === ACTIVE COMBAT SKILLS (damage dealers) ===
                case 117: // 投石问路 - missile 44
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 14, 40, "2ed0ae8f", 16, 1, 2, new Color(123f/255f, 113f/255f, 107f/255f));
                    break;

                case 119: // 沿门托钵 - missile 45 (PC: Speed=31, LifeTime=16)
                    SetupPcMissile(fx, "c723e35a", 64, 16, 1, 31, 16, "8a1df06d", 8, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    break;

                case 122: // 见人伸手 - missile 46 (PC: Speed=31, LifeTime=16)
                    SetupPcMissile(fx, "afb1607e", 64, 16, 1, 31, 16, "8a1df06d", 8, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    break;

                case 125: // 天下无狗 - missile 47, Circle, 16 missiles, MslsGenerateData=5 (PC: Speed=31, LifeTime=16)
                    SetupPcMissile(fx, "04e27976", 64, 16, 1, 31, 16, "b91ab706", 18, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    SetupPcCircleOutwardMissiles(fx, 16); // PC CastCircle: 16 line missiles fly outward around caster.
                    break;

                case 1539: // Thiên Hạ Vô Cẩu (NPC variant) - missile 47, Surround, 16 missiles, MslsGenerateData=5
                    SetupPcMissile(fx, "04e27976", 64, 16, 1, 31, 16, "b91ab706", 18, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    SetupPcCircleOutwardMissiles(fx, 16);
                    break;

                case 128: // Kháng Long Hữu Hối (亢龙有悔) - missile 48, PC dragon SPR (PC: Speed=10, LifeTime=16)
                    SetupPcMissile(fx, "a31b9f04", 80, 16, 1, 10, 16, "c33e96c2", 6, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    var kangLong = PcKangLongYouHuiTuning.AtLevel(level);
                    fx.missileForm = kangLong.missileForm;
                    fx.pcMissileSpeedPerTick = kangLong.missileSpeed;
                    fx.missileSpeed = kangLong.missileSpeed * 18f; // VM gaibang.lua missle_speed_v is PC units/tick.
                    fx.missileDuration = fx.missileDistance / Mathf.Max(0.1f, fx.missileSpeed);
                    if (kangLong.missileForm == SkillMissileForm.Fan)
                        SetupPcKangLongSpread(fx, kangLong.missileCount, kangLong.param1, 1);
                    else
                        fx.missileCount = 1;
                    break;

                // === RESISTANCE BUFFS (PreCastSpr: mag_tr_16) ===
                case 118: // 孤木遁雷 - missile 49 stationary buff effect
                    SetupPcStationaryEffect(fx, "9ba1b99d", 13, 1, 2, new Color(1f, 1f, 214f/255f));
                    break;

                case 120: // 奔流到海 - missile 50 stationary buff effect
                    SetupPcStationaryEffect(fx, "3ab94121", 15, 1, 2, new Color(165f/255f, 170f/255f, 1f));
                    break;

                case 123: // 奎木星照 - missile 51 stationary buff effect
                    SetupPcStationaryEffect(fx, "ea9d621d", 15, 1, 2, new Color(123f/255f, 1f, 189f/255f));
                    break;

                case 126: // 金乌映雪 - missile 52 stationary buff effect
                    SetupPcStationaryEffect(fx, "7770c465", 20, 1, 2, new Color(247f/255f, 154f/255f, 41f/255f));
                    break;

                case 129: // 化险为夷 - missile 53 stationary buff effect
                    SetupPcStationaryEffect(fx, "82fe32c1", 15, 1, 2, new Color(247f/255f, 154f/255f, 41f/255f));
                    break;

                // === UTILITY SKILLS ===
                case 121: // Diệu Thủ Không Không (妙手空空) - Surround
                    fx.color = new Color(0.6f, 0.3f, 0.8f);
                    SetupSurroundMissiles(fx, 4);
                    break;

                case 124: // Đả Cẩu Trận (打狗阵) - Aura
                    fx.color = new Color(0.9f, 0.7f, 0.2f);
                    fx.isAura = true;
                    break;

                case 130: // Túy Điệp Cuồng Vũ (醉蝶狂舞) - Self buff
                    fx.color = new Color(0.8f, 0.4f, 0.9f);
                    fx.isAura = true;
                    break;

                // === PASSIVES (no cast visual) ===
                case 115: // Cái Bang Bổng Pháp
                case 116: // Cái Bang Chưởng Pháp
                case 127: // Hoạt Bất Lưu Thủ
                // MOD passives
                case 274: // Giương Long Chưởng (MOD passive combat mastery)
                case 360: // Tiêu Dao Công (MOD passive combat mastery)
                case 714: // Hỗn Thiên Khí Công 120 (passive)
                    fx.preCastDuration = 0;
                    fx.phase = SkillEffectPhase.Finished;
                    break;

                // === MOD active skills (StreamingAssets/Reference/) ===
                // 277 Hoành Bách Lộ Thiên (MOD 40-level speed buff, same PC skill as 127).
                // PC Skills.txt: MisslesForm=6 (stationary), ChildSkillId=114.
                // Missile 114: mag_gb_07_金乌映雪.spr (20,1,1) color (255,219,99) — same SPR as skill 126.
                case 277:
                    SetupPcStationaryEffect(fx, "7770c465", 20, 1, 1, new Color(1f, 219f/255f, 99f/255f));
                    break;

                // 357 Phi Long Tại Thiên (MOD feilong_zaitian).
                // PC gaibang.lua::feilong_zaitian:
                //   skill_misslesform_v: L1-10=1(Single), L11+=0(Single/parallel spread)
                //   skill_misslenum_v: L1-11=1, L12-15=2, L16-19=3, L20+=4
                //   skill_param1_v: L1-10=0, L11+=32 (180° spread between parallel missiles)
                //   missle_speed_v: 20 (PC units/tick)
                // PC MisslesForm=0 = Single/parallel. The "LINE" visual comes from
                // param1 spread with misslenum>1, NOT from a separate form value.
                // PC missles.txt missile 166: MoveKind=5 → target-tracking (dí).
                //   Each dragon missile updates its direction toward the live target each tick.
                // CollideEvent triggers skill 389 (Long Chiến Ư Dã)
                // ChildSkillId=166: same SPR as Kháng Long (mag_gb_05_亢龙有悔.spr)
                case 357:
                    // PC missles.txt missile 166: Speed=30, LifeTime=24, MoveKind=5 (homing).
                    SetupPcMissile(fx, "a31b9f04", 80, 16, 1, 30, 24, "c33e96c2", 6, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    {
                        int count = level >= 20 ? 4 : (level >= 16 ? 3 : (level >= 12 ? 2 : 1));
                        int luaForm = level >= 11 ? 0 : 1;
                        fx.missileForm = SkillMissileForm.Single;
                        fx.arrivalRadius = 2f;
                        fx.rendRadius = 5f;
                        if (luaForm == 0 && count > 1)
                        {
                            SetupPcPhiLongSpread(fx, count, 32);
                        }
                        else
                        {
                            fx.missileCount = count;
                        }
                    }
                    break;

                // 359 Thiên Hạ Vô Cẩu (player MOD): 1→3 target-seeking missiles.
                // PC gaibang.lua::tianxia_wugou skill_misslenum_v: L1=1, L20=3.
                // NOT 16 circle outward (that's NPC 125/1539 with ChildSkillNum=16).
                // ChildSkillId=168: mag_gb_04_天下无狗.spr (same as NPC 125).
                case 359:
                    // PC tianxia_wugou (gaibang.lua): skill_misslenum_v={{{1,1},{20,3}}}.
                    // PC missles.txt missile 168: Speed=24, LifeTime=32, MoveKind=5 (homing).
                    // PC has no skill_misslesform_v and no skill_param1_v — defaults to Form=0 (parallel).
                    // Use PhiLong parallel spread with same param=32 as Phi Long so 3 missiles stay
                    // visible instead of collapsing onto the homing target point.
                    int thvcCount = level >= 20 ? 3 : 1;
                    SetupPcMissile(fx, "04e27976", 64, 16, 1, 24, 32, "b91ab706", 18, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    fx.arrivalRadius = 2f;
                    if (thvcCount > 1)
                    {
                        SetupPcPhiLongSpread(fx, thvcCount, 32);
                    }
                    else
                    {
                        fx.missileCount = 1;
                    }
                    break;

                // 1073 Thần Thủ Lệnh Long (MOD Thời Thừa Lục Long 150-tier):
                // PC Skills.txt: MisslesForm=1 (single guided), ChildSkillId=335.
                //   3-phase event chain: StartEvent→1101(z-Thời Thừa Lục Long, missle 363),
                //   FlyEvent→1103(z-Thời Thống Lục Long Hỏa, missle 344),
                //   CollideEvent→1072(Ngũ Diệu Càn Khôn, missle 334).
                // PreCast: \spr\skill\150\gb\gb_150_shichengjiulong_a.spr (70d46004, 150x160, 26,1,35)
                // FlyEvent: \spr\skill\1502\gb\gb_150_zhanggai_huo.spr (0b96acfa, 120x130, 6,1,100)
                // Missile 335: \spr\skill\1502\gb\gb_150_zhanggai_zd.spr (377228dc, 200x200, 16,16,1)
                case 1073:
                    // PC missles.txt missile 335: Speed=30, LifeTime=16, MoveKind=1 (straight, NOT homing).
                    SetupPcMissile(fx, "377228dc", 16, 16, 1, 30, 16, "ffb0b7f7", 11, 1, 1, new Color(1f, 174f/255f, 60f/255f));
                    SetupPcPreCast(fx, "70d46004", 26, 1, 35);
                    fx.missileForm = SkillMissileForm.Single;
                    fx.pcMissileSpeedPerTick = 30;
                    fx.missileSpeed = 30 * 18f;
                    fx.missileDuration = fx.missileDistance / Mathf.Max(0.1f, fx.missileSpeed);
                    fx.missileCount = 1;
                    break;

                // 1074 Bổng Hoành Lược Mã (MOD Bổng Hoành Lược Địa 150-tier):
                // PC gaibang.lua::gungaibang150 skill_misslenum_v: L1=1, L20=5.
                // ChildSkillId=336: \spr\skill\1502\gb\gb_150_gungai_zd.spr (e46d8c0d, 170x170, 16,16,1)
                // Impact: \spr\skill\1502\gb\gb_150_gungai_bz.spr (8d06da90, 150x140, 15,1,40)
                // Missiles are target-seeking guided (MisslesForm=1), NOT surround.
                case 1074:
                    // PC missles.txt missile 336: Speed=28, LifeTime=24, MoveKind=5 (homing).
                    // PC gaibang.lua gungaibang150: skill_misslenum_v={{{1,1},{20,5},{21,5}}}.
                    int bhCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(1f, 5f, (level - 1) / 19f)), 1, 5);
                    SetupPcMissile(fx, "e46d8c0d", 16, 16, 1, 28, 24, "8d06da90", 15, 1, 1, new Color(1f, 174f/255f, 60f/255f));
                    SetupPcPreCast(fx, "3cae8f47", 16, 1, 2);
                    fx.pcMissileSpeedPerTick = 28;
                    fx.missileSpeed = 28 * 18f;
                    fx.missileDuration = fx.missileDistance / Mathf.Max(0.1f, fx.missileSpeed);
                    if (bhCount > 1)
                    {
                        fx.missileCount = bhCount;
                        SetupPcKangLongSpread(fx, bhCount, 2, 1);
                    }
                    else
                    {
                        fx.missileCount = 1;
                    }
                    break;

                case 389: // Long Chiến Ư Dã (Collide sub-skill for Phi Long lvl >= 11)
                    SetupPcStationaryEffect(fx, "b91ab706", 6, 1, 1, new Color(239f/255f, 146f/255f, 82f/255f));
                    fx.preCastDuration = 0f;
                    break;

                case 1072: // Ngũ Diệu Càn Khôn (CollideEvent[3] sub-skill for Thời Thặng Lục Long 1073)
                    // PC missles.txt missile 334: MoveKind=0, LifeTime=10, Speed=0, DmgInterval=5.
                    // 1 frame, 1 dir, 1 tick (AnimFileInfo 11,1,1). Stationary flash at 335 impact.
                    SetupPcStationaryEffect(fx, "ffb0b7f7", 11, 1, 1, new Color(239f/255f, 146f/255f, 82f/255f, 90f/255f));
                    fx.preCastDuration = 0f;
                    break;

                case 720: // Hỗn Thiên Khí Công nguyền rủa
                    SetupPcMissile(fx, null, 1, 1, 1, 0, 5, null, 0, 1, 1, new Color(255f/255f, 219f/255f, 99f/255f));
                    break;

                // === DEFAULT (any unconfigured active skill) ===
                // Use a neutral golden visual so the user always sees feedback even for
                // skills we haven't fully tuned. PC skill with missile form gets a basic
                // outward missile; non-missile (None) gets no visual.
                default:
                    if (skill.missileForm != SkillMissileForm.None && PcMissileRegistry.TryGet(skill.childSkillId, out var mEntry))
                    {
                        string sprHash = SprRuntimeService.ComputePathUidHex(mEntry.sprFile);
                        if (string.IsNullOrEmpty(sprHash))
                        {
                            sprHash = skill.missileForm switch
                            {
                                SkillMissileForm.Surround => "04e27976",
                                SkillMissileForm.Fan => "a31b9f04",
                                _ => "883bff8c"
                            };
                        }
                        SetupPcMissile(fx, sprHash, 1, 1, 1, mEntry.speed, mEntry.lifetime, "2ed0ae8f", 12, 1, 2, new Color(220f/255f, 180f/255f, 80f/255f));
                        if (skill.missileForm == SkillMissileForm.Surround)
                        {
                            SetupPcCircleOutwardMissiles(fx, System.Math.Max(1, skill.childSkillNum));
                        }
                        else if (skill.missileForm == SkillMissileForm.Fan)
                        {
                            SetupPcKangLongSpread(fx, System.Math.Max(1, skill.childSkillNum), 2, 1);
                        }
                    }
                    else
                    {
                        if (skill.missileForm == SkillMissileForm.Surround)
                        {
                            SetupPcMissile(fx, "04e27976", 64, 16, 1, 12, 30, "b91ab706", 16, 1, 2, new Color(220f/255f, 180f/255f, 80f/255f));
                            SetupSurroundMissiles(fx, System.Math.Max(1, skill.childSkillNum));
                        }
                        else if (skill.missileForm == SkillMissileForm.Fan)
                        {
                            SetupPcMissile(fx, "a31b9f04", 80, 16, 1, 16, 22, "c33e96c2", 7, 1, 2, new Color(220f/255f, 180f/255f, 80f/255f));
                            SetupPcKangLongSpread(fx, System.Math.Max(1, skill.childSkillNum), 2, 1);
                        }
                        else if (skill.missileForm == SkillMissileForm.Single)
                        {
                            SetupPcMissile(fx, "883bff8c", 1, 1, 1, 14, 30, "2ed0ae8f", 12, 1, 2, new Color(220f/255f, 180f/255f, 80f/255f));
                        }
                    }
                    break;
            }
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
            fx.missileSpeed = speedPerTick * 18f;
            fx.missileDuration = fx.missileDistance / Mathf.Max(0.1f, fx.missileSpeed);
            fx.impactDuration = impactFrames > 0 ? (impactFrames * Mathf.Max(1, impactIntervalTicks)) / 18f : 0.25f;
        }

        private static void SetupPcPreCast(ActiveSkillEffect fx, string key, int frames, int dirs, int intervalTicks)
        {
            fx.pcPreCastSpriteKey = key;
            fx.pcPreCastTotalFrames = frames;
            fx.pcPreCastDirections = Mathf.Max(1, dirs);
            fx.pcPreCastIntervalTicks = Mathf.Max(1, intervalTicks);
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

            // PC KSkill::CastSpread: nCurMSRadius starts ChildSkillNum/2 and decrements.
            // nDSubDir = Param1 * radius, then +48 in 64-dir space for target-guided spread.
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

        /// <summary>
        /// PC feilong_zaitian parallel missile spread (L11+ MissilesForm=0, misslenum>1).
        /// PC gaibang_server.lua: skill_param1_v(L11+)=32 -- "khoang cach 2 tia" (spacing between missiles).
        /// Form=0 (Line/Parallel): missiles fly parallel toward target, spaced perpendicularly.
        /// </summary>
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

            // Perpendicular (horizontal) direction relative to flight path
            Vector2 perpDir = new Vector2(-baseDir.y, baseDir.x);

            // PC param1=32 = perpendicular spacing in PC world units between missiles.
            // 4 missiles at level 20: halfSpan = (4-1)*32/2 = 48 units from center.
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
            // PC missile travel: MoveKind line advances Speed each game tick until LifeTime expires.
            float distance = Mathf.Max(1f, fx.pcMissileSpeedPerTick * fx.pcMissileLifeTicks);
            fx.missileDuration = fx.pcMissileLifeTicks / 18f; // stable PC tick lifetime, independent of auto-target distance.

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
            float radius = 1.5f; // Non-PC utility fallback only.

            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.Deg2Rad * (i * angleStep);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                fx.missilePositions[i] = fx.casterPos;
                fx.missileTargets[i] = fx.casterPos + offset;
            }
        }

        private void ConfigureEMeiVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            if (!PcCombatCatalogFactory.IsEMeiSkill(skill.skillId)) return;

            switch (skill.skillId)
            {
                case 80: // Phiêu Tuyết Xuyên Vân (峨嵋-飞雪) - single guided water missile
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 14, 30, "2ed0ae8f", 12, 1, 2, new Color(100f/255f, 180f/255f, 255f/255f));
                    break;
                case 82: // Tứ Tượng Đồng Quy - single water missile
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 16, 30, "2ed0ae8f", 12, 1, 2, new Color(100f/255f, 180f/255f, 255f/255f));
                    break;
                case 85: // Nhất Diệp Tri Thu
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 18, 30, "2ed0ae8f", 12, 1, 2, new Color(100f/255f, 180f/255f, 255f/255f));
                    break;
                case 88: // Bất Diệt Bất Tuyệt
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 20, 30, "2ed0ae8f", 12, 1, 2, new Color(100f/255f, 180f/255f, 255f/255f));
                    break;
                case 91: // Phật Quang Phổ Chiếu
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 22, 30, "2ed0ae8f", 12, 1, 2, new Color(100f/255f, 180f/255f, 255f/255f));
                    break;
                case 81: // Thu Phong Diệp (buff/aura)
                case 83: // Vọng Nguyệt (buff/aura)
                case 84: // Phong Vũ Phiêu Hương (buff/aura)
                case 86: // Lưu Thủy (buff/aura)
                case 89: // Mộng Điệp (buff/aura)
                case 90: // Mê Tung Ảo Ảnh (buff/aura)
                case 92: // Phật Tâm Từ Hữu (buff/aura)
                case 93: // Từ Hàng Phổ Độ (buff/aura)
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, new Color(100f/255f, 180f/255f, 255f/255f));
                    break;
                default:
                    // Data-driven visual handled by ConfigureDataDrivenVisuals above.
                    // Legacy hardcode does not override for this skill.
                    break;
            }
        }

        private void ConfigureTianWangVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            if (!PcCombatCatalogFactory.IsTianWangSkill(skill.skillId)) return;

            switch (skill.skillId)
            {
                case 32: // Vô Tâm Trảm
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 16, 30, "2ed0ae8f", 12, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 34: // Kinh Lôi Trảm
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 18, 30, "2ed0ae8f", 12, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 40: // Đoạn Hồn Thích
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "afb1607e", 64, 16, 1, 28, 20, "8a1df06d", 8, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 30: // Hồi Phong Lạc Nhạn
                case 37: // Bát Phong Trảm
                case 41: // Huyết Chiến Bát Phương
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcStationaryEffect(fx, "8de48699", 15, 1, 1, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 29: // Trảm Long Quyết
                case 35: // Dương Quan Tam Điệp
                    SetupPcStationaryEffect(fx, "9ba1b99d", 13, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                case 42: // Kim Chung Tráo
                    SetupPcStationaryEffect(fx, "7770c465", 20, 1, 2, new Color(255f/255f, 215f/255f, 0f));
                    break;
                default:
                    // Data-driven visual handled by ConfigureDataDrivenVisuals above.
                    // Legacy hardcode does not override for this skill.
                    break;
            }
        }

        private void ConfigureWuDuVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            if (!PcCombatCatalogFactory.IsWuDuSkill(skill.skillId)) return;

            switch (skill.skillId)
            {
                case 63: // Độc Sa Chưởng
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 14, 30, "2ed0ae8f", 12, 1, 2, new Color(100f/255f, 220f/255f, 80f/255f));
                    break;
                case 65: // Huyết Đao Độc Sát
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 16, 30, "2ed0ae8f", 12, 1, 2, new Color(100f/255f, 220f/255f, 80f/255f));
                    break;
                case 68: // U Minh Khô Lâu
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "afb1607e", 64, 16, 1, 18, 30, "8a1df06d", 8, 1, 2, new Color(100f/255f, 220f/255f, 80f/255f));
                    break;
                case 69: // Vô Hình Độc
                case 71: // Thiên Cương Địa Sát
                case 74: // Chu Cáp Thanh Minh
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcStationaryEffect(fx, "8de48699", 15, 1, 1, new Color(100f/255f, 220f/255f, 80f/255f));
                    break;
                case 64: // Băng Lam Huyền Tinh
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, new Color(100f/255f, 180f/255f, 255f/255f));
                    break;
                case 67: // Cửu Thiên Cuồng Lôi
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, new Color(200f/255f, 100f/255f, 255f/255f));
                    break;
                case 70: // Chích Dương Thệ Thiên
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, new Color(255f/255f, 100f/255f, 50f/255f));
                    break;
                case 72: // Xuyên Tâm Độc Thích
                case 73: // Vạn Độc Thực Tâm
                case 76: // Di Hoa Tiếp Ngọc
                    SetupPcStationaryEffect(fx, "9ba1b99d", 13, 1, 2, new Color(100f/255f, 220f/255f, 80f/255f));
                    break;
                default:
                    // Data-driven visual handled by ConfigureDataDrivenVisuals above.
                    // Legacy hardcode does not override for this skill.
                    break;
            }
        }

        private void ConfigureCuiYanVisuals(SkillDefinition skill, ActiveSkillEffect fx, int level)
        {
            if (!PcCombatCatalogFactory.IsCuiYanSkill(skill.skillId)) return;

            var waterColor = new Color(100f/255f, 180f/255f, 255f/255f);
            switch (skill.skillId)
            {
                case 99: // Phong Hoa Tuyết Nguyệt
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 14, 30, "2ed0ae8f", 12, 1, 2, waterColor);
                    break;
                case 102: // Phong Quyển Tàn Tuyết
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "883bff8c", 1, 1, 1, 16, 30, "2ed0ae8f", 12, 1, 2, waterColor);
                    break;
                case 105: // Vũ Đả Lê Hoa
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcMissile(fx, "afb1607e", 64, 16, 1, 18, 30, "8a1df06d", 8, 1, 2, waterColor);
                    break;
                case 108: // Mục Dã Lưu Tinh
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcStationaryEffect(fx, "8de48699", 15, 1, 1, waterColor);
                    break;
                case 111: // Bích Hải Triều Sinh
                case 113: // Phù Vân Tán Tuyết
                    SetupPcPreCast(fx, "42ed0184", 16, 1, 1);
                    SetupPcStationaryEffect(fx, "8de48699", 12, 1, 1, waterColor);
                    break;
                case 100: // Hộ Thể Hàn Băng
                case 101: // Trị Liệu Thuật
                case 103: // Thiên Lý Băng Phong
                case 109: // Tuyết Ảnh
                    SetupPcStationaryEffect(fx, "9ba1b99d", 13, 1, 2, waterColor);
                    break;
                default:
                    // Data-driven visual handled by ConfigureDataDrivenVisuals above.
                    // Legacy hardcode does not override for this skill.
                    break;
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

        public bool HasPcMissileSprite => !string.IsNullOrEmpty(pcMissileSpriteKey) && pcMissileTotalFrames > 0 && pcMissileDirections > 0;
        public bool HasPcImpactSprite => !string.IsNullOrEmpty(pcImpactSpriteKey) && pcImpactTotalFrames > 0;
        public bool HasPcPreCastSprite => !string.IsNullOrEmpty(pcPreCastSpriteKey) && pcPreCastTotalFrames > 0 && pcPreCastDirections > 0;
        public bool HasMissile => missileForm != SkillMissileForm.None && missileCount > 0;
    }
}
