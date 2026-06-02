using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC-accurate skill effect visual renderer for Cái Bang combat skills.
    /// Plays PreCastSpr animation on caster, spawns missile projectile sprites,
    /// and renders impact effects. Visuals sourced exclusively from JXWin PC data.
    /// </summary>
    public class SkillEffectVisualService
    {
        private readonly SprRuntimeService _sprService;
        private readonly List<ActiveSkillEffect> _activeEffects = new();

        public SkillEffectVisualService(SprRuntimeService sprService)
        {
            _sprService = sprService;
        }

        public int ActiveEffectCount => _activeEffects.Count;

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

            // CaiBang-specific visual parameters from PC Skills.txt/Missles.txt
            ConfigureCaiBangVisuals(skill, effect, skillLevel);

            _activeEffects.Add(effect);
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
                        float missileT = (fx.elapsed - fx.phaseStart) / Mathf.Max(0.01f, fx.missileDuration);
                        if (missileT >= 1f)
                        {
                            fx.phase = SkillEffectPhase.Impact;
                            fx.phaseStart = fx.elapsed;
                        }
                        else
                        {
                            fx.currentMissilePos = Vector2.Lerp(fx.casterPos, fx.targetPos, missileT);
                            // Fan/Surround skills spawn multiple missiles
                            UpdateMultiMissile(fx, missileT);
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

        private void UpdateMultiMissile(ActiveSkillEffect fx, float t)
        {
            if (fx.missilePositions != null)
            {
                for (int i = 0; i < fx.missilePositions.Length; i++)
                {
                    fx.missilePositions[i] = Vector2.Lerp(
                        fx.casterPos, fx.missileTargets[i], t);
                }
            }
        }

        private Sprite ResolveMissileSprite(SkillDefinition skill)
        {
            // PC missile SPR is identified by childSkillId.
            // These are stored in spr.pak as hashed filenames.
            // Fallback to a generic projectile sprite.
            string missileKey = $"missile_{skill.childSkillId}";
            return _sprService?.ResolveSprite(missileKey, 32, 32);
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

                case 119: // 沿门托钵 - missile 45
                    SetupPcMissile(fx, "c723e35a", 64, 16, 1, 16, 15, "8a1df06d", 8, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    break;

                case 122: // 见人伸手 - missile 46
                    SetupPcMissile(fx, "afb1607e", 64, 16, 1, 20, 15, "8a1df06d", 8, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    break;

                case 125: // 天下无狗 - missile 47, Circle, 16 missiles, MslsGenerateData=5
                    SetupPcMissile(fx, "04e27976", 64, 16, 1, 12, 34, "b91ab706", 18, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    SetupPcCircleOutwardMissiles(fx, 16); // PC CastCircle: 16 line missiles fly outward around caster.
                    break;

                case 128: // Kháng Long Hữu Hối (亢龙有悔) - missile 48, PC dragon SPR
                    SetupPcMissile(fx, "a31b9f04", 80, 16, 1, 18, 20, "c33e96c2", 6, 1, 2, new Color(1f, 174f/255f, 60f/255f));
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
                // CollideEvent triggers skill 389 (Long Chiến Ư Dã)
                // ChildSkillId=166: same SPR as Kháng Long (mag_gb_05_亢龙有悔.spr)
                case 357:
                    SetupPcMissile(fx, "a31b9f04", 80, 16, 1, 20, 20, "c33e96c2", 6, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    {
                        int count = level >= 20 ? 4 : (level >= 16 ? 3 : (level >= 12 ? 2 : 1));
                        int luaForm = level >= 11 ? 0 : 1;
                        fx.missileForm = (SkillMissileForm)luaForm;
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
                    int thvcCount = level >= 20 ? 3 : 1;
                    SetupPcMissile(fx, "04e27976", 64, 16, 1, 20, 24, "0eb30d6c", 18, 1, 2, new Color(1f, 174f/255f, 60f/255f));
                    if (thvcCount > 1)
                    {
                        fx.missileCount = thvcCount;
                        SetupPcKangLongSpread(fx, thvcCount, 2, 1);
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
                    SetupPcMissile(fx, "377228dc", 16, 16, 1, 24, 24, "ffb0b7f7", 11, 1, 1, new Color(1f, 174f/255f, 60f/255f));
                    fx.missileForm = SkillMissileForm.Single;
                    fx.pcMissileSpeedPerTick = 24;
                    fx.missileSpeed = 24 * 18f;
                    fx.missileDuration = fx.missileDistance / Mathf.Max(0.1f, fx.missileSpeed);
                    fx.missileCount = 1;
                    break;

                // 1074 Bổng Hoành Lược Mã (MOD Bổng Hoành Lược Địa 150-tier):
                // PC gaibang.lua::gungaibang150 skill_misslenum_v: L1=1, L20=5.
                // ChildSkillId=336: \spr\skill\1502\gb\gb_150_gungai_zd.spr (e46d8c0d, 170x170, 16,16,1)
                // Impact: \spr\skill\1502\gb\gb_150_gungai_bz.spr (8d06da90, 150x140, 15,1,40)
                // Missiles are target-seeking guided (MisslesForm=1), NOT surround.
                case 1074:
                    int bhCount = level >= 20 ? 5 : (level >= 16 ? 4 : (level >= 12 ? 3 : (level >= 6 ? 2 : 1)));
                    SetupPcMissile(fx, "e46d8c0d", 16, 16, 1, 24, 24, "8d06da90", 15, 1, 1, new Color(1f, 174f/255f, 60f/255f));
                    fx.pcMissileSpeedPerTick = 24;
                    fx.missileSpeed = 24 * 18f;
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

                // === DEFAULT (any unconfigured active skill) ===
                // Use a neutral golden visual so the user always sees feedback even for
                // skills we haven't fully tuned. PC skill with missile form gets a basic
                // outward missile; non-missile (None) gets no visual.
                default:
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
        /// PC feilong_zaitian parallel missile spread (L11+ form=0, misslenum>1).
        /// skill_param1_v(L11+)=32 → 32/64*360 = 180° total spread.
        /// Missiles spread evenly around target direction (same as KangLong spread logic).
        /// </summary>
        private void SetupPcPhiLongSpread(ActiveSkillEffect fx, int count, int param64)
        {
            fx.missileCount = count;
            fx.missilePositions = new Vector2[count];
            fx.missileTargets = new Vector2[count];

            Vector2 baseDir = fx.targetPos - fx.casterPos;
            float distance = Mathf.Max(1f, baseDir.magnitude);
            baseDir /= distance;

            // PC param1: spread angle in 64th units. 32/64*360=180°.
            // Perpendicular spread: rotate baseDir by ±90°, then distribute missiles.
            Vector2 perpDir = new Vector2(-baseDir.y, baseDir.x);
            float totalDeg = param64 * 360f / 64f;
            float halfDeg = totalDeg * 0.5f;
            float step = count > 1 ? totalDeg / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = -halfDeg + i * step;
                Vector2 dir = Rotate(baseDir, angle);
                fx.missilePositions[i] = fx.casterPos + dir * 1f;
                fx.missileTargets[i] = fx.casterPos + dir * distance;
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
        public Vector2[] missileTargets;

        // Impact
        public float impactDuration = 0.6f;

        // Visual
        public Color color = Color.white;
        public bool trailEnabled;
        public bool isAura;

        // PC missile SPR metadata from Missles.txt. Used for exact JXWin sprite playback.
        public string pcMissileSpriteKey;
        public string pcImpactSpriteKey;
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
        public bool HasMissile => missileForm != SkillMissileForm.None && missileCount > 0;
    }
}
