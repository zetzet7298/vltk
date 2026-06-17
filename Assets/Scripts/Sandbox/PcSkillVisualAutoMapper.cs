using VLTK.Model;
// -----------------------------------------------------------------------------
// VLTK Mobile — PC Skill Visual Auto-Mapper
// Purpose: Automatically map every skill to its correct visual from PC data.
// Reads skills.txt → childSkillId → missles1.txt → SPR paths + anim info.
// No more hardcoded per-skill switch-cases. Data-driven for ALL factions.
//
// PC flow: KSkill::Cast → KMissle::Activate with AnimFile SPR rendering.
// Mobile flow: SkillDefinition.childSkillId → PcMissileFullVisual → SPR render.
//
// Source: Assets/StreamingAssets/Reference/PcAttrib/missles1.txt (467 missiles)
//         Assets/StreamingAssets/Reference/PcSkill/skills.txt (1216 skills)
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Resolved visual configuration for a skill, auto-generated from PC data.
    /// Used by SkillEffectVisualService to render correct animation/effects.
    /// </summary>
    [Serializable]
    public class PcSkillVisualConfig
    {
        public int skillId;
        public int missileId;           // ChildSkillId → missile id

        // PreCast visual
        public string preCastSprPath;   // PreCastSpr from skills.txt
        public bool hasPreCast;

        // Flight/missile visual
        public string flightSprPath;    // Primary flight SPR from missles1.txt
        public int flightFrames;        // Total animation frames
        public int flightDirections;    // Direction count (1, 8, 16)
        public int flightIntervalTicks; // Animation speed
        public int missileSpeed;        // Speed in ticks
        public int missileLifetime;     // Lifetime in ticks
        public bool isStationary;       // MoveKind=0 (no flight, area effect)

        // Impact/explosion visual
        public string explodeSprPath;   // Primary explosion SPR from missles1.txt
        public int explodeFrames;
        public int explodeDirections;
        public int explodeIntervalTicks;

        // Light color from missles1.txt
        public Color lightColor;
        public int lightRadius;

        // Skill behavior flags
        public bool isMelee;            // IsMelee from skills.txt
        public bool hasMissile;         // Has any missile/effect visual
        public bool isRangeDmg;         // AOE damage
        public int dmgRange;            // AOE radius

        // State aura visual (PC: 状态与光效图形对照表.txt → stateSpecialId → SPR + anim)
        // Used by state self-buff skills (Túy Điệp Cuồng Vũ 130, Đả Cẩu Trận 277, etc.)
        // 0 = no aura; loop SPR attached to body while state active.
        public string stateAuraSprPath;     // e.g. \spr\skill\丐帮\mag_gb_11_醉蝶狂舞.spr
        public int stateAuraTotalFrames;    // total animation frames (PC: 16 for Túy Điệp)
        public int stateAuraIntervalTicks;  // animation speed (PC: 1 tick = 1 frame change)
        public int stateAuraFrameStart;     // start frame (PC: 4 for Túy Điệp, 0 default)
        public int stateAuraFrameEnd;       // end frame (PC: 12 for Túy Điệp, 0 = play all)
        public int stateAuraDirections;     // 1 (most state auras are direction-free)
        public int stateAuraPos;            // 1=head(头顶) 2=feet(脚底) 3=body(身上)
        public bool hasStateAura;

        /// <summary>Flight duration in seconds (at 18 ticks/sec).</summary>
        public float FlightDurationSeconds => missileLifetime > 0 ? missileLifetime / 18f : 1.5f;

        /// <summary>Speed in world units/sec (PC speed × 18 ticks/sec).</summary>
        public float SpeedWorldPerSec => missileSpeed * 18f;

        /// <summary>Explode duration in seconds.</summary>
        public float ExplodeDurationSeconds => explodeFrames > 0 && explodeIntervalTicks > 0
            ? (explodeFrames * explodeIntervalTicks) / 18f : 0.6f;

        /// <summary>Has complete flight SPR data?</summary>
        public bool HasFlightVisual => !string.IsNullOrEmpty(flightSprPath) && flightFrames > 0;

        /// <summary>Has complete explosion SPR data?</summary>
        public bool HasExplodeVisual => !string.IsNullOrEmpty(explodeSprPath) && explodeFrames > 0;

        public bool HasAnyVisual => HasFlightVisual || HasExplodeVisual || hasPreCast || isMelee || hasStateAura;
    }

    /// <summary>
    /// Data-driven auto-mapper: resolves every skill's visual from PC source data.
    /// Replaces all hardcoded Configure*Visuals switch-cases.
    /// </summary>
    public sealed class PcSkillVisualAutoMapper
    {
        public const string LogTag = "SkillVisual";

        private PcMissileFullVisualRegistry _missileVisuals;
        private readonly Dictionary<int, PcSkillVisualConfig> _cache = new();
        private bool _initialized;
        private int _skillsProcessed;
        private int _visualsFound;
        private int _visualsMissing;

        public int SkillsProcessed => _skillsProcessed;
        public int VisualsFound => _visualsFound;
        public int VisualsMissing => _visualsMissing;
        public int CacheCount => _cache.Count;
        public int MissileVisualCount => _missileVisuals?.Count ?? 0;

        /// <summary>
        /// Initialize by parsing missles1.txt from StreamingAssets.
        /// </summary>
        public void Initialize(string streamingAssetsPath)
        {
            if (_initialized) return;

            var refPath = Path.Combine(streamingAssetsPath, "Reference", "PcAttrib", "missles1.txt");
            if (!File.Exists(refPath))
            {
                // Fallback: try alternate paths
                var alt = Path.Combine(streamingAssetsPath, "Reference", "PcMissles.txt");
                if (!File.Exists(refPath))
                {
                    SubsystemLog.Warn(LogTag, $"missles1.txt not found, skill visuals will use fallbacks");
                    _missileVisuals = new PcMissileFullVisualRegistry();
                    _initialized = true;
                    return;
                }
                refPath = alt;
            }

            _missileVisuals = PcMissileFullVisualRegistry.ParseFromFile(refPath);
            _initialized = true;
            SubsystemLog.Info(LogTag, $"AutoMapper init: {_missileVisuals.Count} missile visuals loaded");
        }

        /// <summary>
        /// Get or create visual config for a skill. Uses PC data-driven mapping:
        ///   skill.childSkillId → missile → SPR paths + anim data + light color
        /// </summary>
        public PcSkillVisualConfig GetVisualConfig(SkillDefinition skill)
        {
            if (skill == null) return null;

            if (_cache.TryGetValue(skill.skillId, out var cached))
                return cached;

            var config = BuildConfig(skill);
            _cache[skill.skillId] = config;
            return config;
        }

        /// <summary>
        /// Batch-resolve all skills in a catalog. Call once at startup for pre-cache.
        /// </summary>
        public void PreCacheAll(SkillCatalog catalog)
        {
            if (catalog == null) return;
            _skillsProcessed = 0;
            _visualsFound = 0;
            _visualsMissing = 0;

            foreach (var skill in catalog.All)
            {
                _skillsProcessed++;
                var config = GetVisualConfig(skill);
                if (config != null && config.HasAnyVisual)
                    _visualsFound++;
                else
                    _visualsMissing++;
            }

            SubsystemLog.Info(LogTag,
                $"PreCache: {_skillsProcessed} skills, {_visualsFound} with visuals, {_visualsMissing} fallbacks");
        }

        private PcSkillVisualConfig BuildConfig(SkillDefinition skill)
        {
            var config = new PcSkillVisualConfig
            {
                skillId = skill.skillId,
                isMelee = skill.isMelee,
                hasMissile = skill.HasMissile,
                preCastSprPath = skill.effectSourceId?.sourcePath,
                hasPreCast = !string.IsNullOrEmpty(skill.effectSourceId?.sourcePath),
            };

            // Default light color from faction
            config.lightColor = GetFactionDefaultColor(skill.faction);

            // Core mapping: skill.childSkillId → missile visual data
            int missileId = skill.childSkillId;
            if (missileId <= 0)
            {
                // No missile. State self-buff skill may still have a state aura SPR.
                ApplyStateAura(skill, config);
                return config;
            }

            config.missileId = missileId;

            if (_missileVisuals == null || !_missileVisuals.TryGet(missileId, out var mv))
            {
                // Try to get basic info from PcMissileRegistry as fallback
                if (PcMissileRegistry.TryGet(missileId, out var basic))
                {
                    config.missileSpeed = basic.speed;
                    config.missileLifetime = basic.lifetime;
                }
                return config;
            }

            // Fill from full missile visual data
            config.missileSpeed = mv.speed;
            config.missileLifetime = mv.lifetime;
            config.isStationary = mv.IsStationary;
            config.isRangeDmg = mv.isRangeDmg != 0;
            config.dmgRange = mv.dmgRange;
            config.lightColor = mv.LightColor;
            config.lightRadius = mv.lightRadius;

            // Flight SPR
            var flight = mv.PrimaryFlight;
            if (flight != null && flight.HasSpr)
            {
                config.flightSprPath = flight.sprPath;
                config.flightFrames = flight.totalFrames;
                config.flightDirections = flight.directions;
                config.flightIntervalTicks = flight.intervalTicks;
            }

            // Explosion SPR
            var explode = mv.PrimaryExplode;
            if (explode != null && explode.HasSpr)
            {
                config.explodeSprPath = explode.sprPath;
                config.explodeFrames = explode.totalFrames;
                config.explodeDirections = explode.directions;
                config.explodeIntervalTicks = explode.intervalTicks;
            }
            return config;
        }

        /// <summary>
        /// Apply state aura SPR (PC 状态与光效图形对照表.txt) if skill has stateSpecialId > 0.
        /// Used by Túy Điệp Cuồng Vũ (130), Đả Cẩu Trận (277), and other state self-buff skills.
        /// </summary>
        private void ApplyStateAura(SkillDefinition skill, PcSkillVisualConfig config)
        {
            if (skill.stateSpecialId <= 0) return;
            var aura = GetStateAuraData(skill.stateSpecialId);
            if (string.IsNullOrEmpty(aura.sprPath)) return;
            config.stateAuraSprPath = aura.sprPath;
            config.stateAuraIntervalTicks = aura.intervalTicks;
            config.stateAuraFrameStart = aura.frameStart;
            config.stateAuraFrameEnd = aura.frameEnd;
            config.stateAuraDirections = aura.directions;
            config.stateAuraPos = aura.position;
            config.hasStateAura = true;
        }

        /// <summary>
        /// Resolve PC SPR path to mobile SPR UID key.
        /// PC paths like "\spr\skill\xxx\yyy.spr" need to be resolved to
        /// the UID hash used in StreamingAssets/Sprites/{uid}.spr.
        /// The SprRuntimeService handles this resolution at render time.
        /// </summary>
public static string SprPathToKey(string pcPath)
        {
            if (string.IsNullOrEmpty(pcPath)) return null;

            // Mobile StreamingAssets/Sprites stores PC SPRs by PAK UID hex.
            // Use the same signed-byte JX FileNameHash as SprRuntimeService;
            // returning a normalized path such as "spr/skill/..." makes
            // SkillEffectWorldOverlay.LoadPcSprites miss the file and fall back
            // to procedural dots/rings.
            return VLTK.Sprites.SprRuntimeService.ComputePathUidHex(pcPath, signedBytes: true);
        }

        /// <summary>
        /// State aura visual data from PC source: 状态与光效图形对照表.txt.
        /// PC source: Utility/Run/Settings/状态与光效图形对照表.txt (Tinh Kiem mod 2023).
        /// State ID 0 = no aura. Position: 1=头顶(head) 2=脚底(feet) 3=身上(body).
        /// Anim: total frames, frame start (e.g. 4-12 for Túy Điệp 43), 1 direction, loop.
        /// </summary>
        /// <summary>
        /// State aura visual data from PC source: 状态与光效图形对照表.txt.
        /// PC source: jx-source Utility/Run/Settings/状态与光效图形对照表.txt.
        /// State ID 0 = no aura. Position: 1=头顶(head) 2=脚底(feet) 3=身上(body).
        /// Columns: stateId, sprPath, position, playMode, frameStart, frameEnd,
        /// totalFrames, directions, frameInterval, name.
        /// </summary>
        public struct PcStateAuraData
        {
            public string sprPath;
            public int totalFrames;
            public int frameStart;
            public int frameEnd;
            public int intervalTicks;
            public int directions;
            public int position;  // 1=head 2=feet 3=body
        }

        /// <summary>
        /// Returns PC state aura visual config for a given state ID.
        /// Source: jx-source 状态与光效图形对照表.txt — all 44 visual states (6-49).
        /// States 1-5 are built-in (stun/poison/freeze/burn/confuse) with no SPR.
        /// </summary>
        public static PcStateAuraData GetStateAuraData(int stateId) => stateId switch
        {
            // ── WuDu (五毒教) states 6-12 ──
            6  => Aura("\\spr\\skill\\五毒教\\wdu_06_无形蛊.spr",   pos:2, fs:0,  fe:0,  tf:30, dir:1, iv:1),
            7  => Aura("\\spr\\skill\\五毒教\\wdu_07_毒盾.spr",     pos:3, fs:5,  fe:15, tf:20, dir:1, iv:1),
            8  => Aura("\\spr\\skill\\五毒教\\wdu_08_冰蓝玄晶.spr", pos:1, fs:0,  fe:0,  tf:30, dir:1, iv:1),
            9  => Aura("\\spr\\skill\\五毒教\\wdu_09_雷动九天.spr", pos:1, fs:0,  fe:0,  tf:30, dir:1, iv:1),
            10 => Aura("\\spr\\skill\\五毒教\\wdu_10_赤焰蚀天.spr", pos:1, fs:0,  fe:0,  tf:30, dir:1, iv:1),
            11 => Aura("\\spr\\skill\\五毒教\\wdu_11_万蛊蚀心.spr", pos:1, fs:0,  fe:0,  tf:30, dir:1, iv:1),
            12 => Aura("\\spr\\skill\\五毒教\\wdu_12_移花接玉.spr", pos:1, fs:0,  fe:0,  tf:20, dir:1, iv:1),
            // ── KunLun (昆仑) states 13-17 ──
            13 => Aura("\\spr\\skill\\昆仑\\kl_06_大浪蚀空.spr",   pos:3, fs:5,  fe:15, tf:20, dir:1, iv:2),
            14 => Aura("\\spr\\skill\\昆仑\\kl_07_引雷遁地.spr",   pos:3, fs:7,  fe:22, tf:30, dir:1, iv:2),
            15 => Aura("\\spr\\skill\\昆仑\\kl_08_烈火红尘.spr",   pos:3, fs:7,  fe:22, tf:30, dir:1, iv:2),
            16 => Aura("\\spr\\skill\\昆仑\\kl_09_木珠兵解.spr",   pos:3, fs:40, fe:40, tf:40, dir:1, iv:2),
            17 => Aura("\\spr\\skill\\昆仑\\kl_10_滑不留手.spr",   pos:3, fs:4,  fe:12, tf:15, dir:1, iv:2),
            // ── WuDang (武当) states 18-20 ──
            18 => Aura("\\spr\\skill\\武当\\wd_08_人剑合一.spr",   pos:3, fs:7,  fe:22, tf:30, dir:1, iv:1),
            19 => Aura("\\spr\\skill\\武当\\wd_06_坐忘无我.spr",   pos:1, fs:0,  fe:0,  tf:36, dir:1, iv:1),
            20 => Aura("\\spr\\skill\\武当\\wd_07_七星阵.spr",     pos:2, fs:0,  fe:0,  tf:30, dir:1, iv:1),
            // ── TianRen (天忍) states 21-30 ──
            21 => Aura("\\spr\\skill\\天忍\\mag_tr_06_火盾.spr",       pos:3, fs:4,  fe:12, tf:15, dir:1, iv:1),
            22 => Aura("\\spr\\skill\\天忍\\mag_tr_07_偷天换日.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            23 => Aura("\\spr\\skill\\天忍\\mag_tr_08_吸星大法.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            24 => Aura("\\spr\\skill\\天忍\\mag_tr_09_借力打力.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            25 => Aura("\\spr\\skill\\天忍\\mag_tr_10_蚀骨血仞.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            26 => Aura("\\spr\\skill\\天忍\\mag_tr_11_幻影飞狐.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            27 => Aura("\\spr\\skill\\天忍\\mag_tr_12_飞鸿无迹.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            28 => Aura("\\spr\\skill\\天忍\\mag_tr_13_厉魔夺魂.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            29 => Aura("\\spr\\skill\\天忍\\mag_tr_14_五行阵.spr",     pos:2, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            30 => Aura("\\spr\\skill\\天忍\\mag_tr_15_天魔解体.spr",   pos:1, fs:0,  fe:0,  tf:15, dir:1, iv:1),
            // ── CuiYan (翠烟) states 31-34 ──
            31 => Aura("\\spr\\skill\\翠烟\\mag_cy_06_雪盾.spr",   pos:3, fs:4,  fe:12, tf:16, dir:1, iv:2),
            32 => Aura("\\spr\\skill\\翠烟\\mag_cy_07_冰盾.spr",   pos:3, fs:4,  fe:12, tf:16, dir:1, iv:2),
            33 => Aura("\\spr\\skill\\翠烟\\mag_cy_09_雪影.spr",   pos:3, fs:16, fe:16, tf:16, dir:1, iv:2),
            34 => Aura("\\spr\\skill\\翠烟\\mag_cy_12_摄心术.spr", pos:1, fs:0,  fe:0,  tf:18, dir:1, iv:4),
            // ── TangMen (唐门) states 35-36 ──
            35 => Aura("\\spr\\skill\\唐门\\tm_01_毒附加.spr", pos:1, fs:0, fe:0, tf:30, dir:1, iv:1),
            36 => Aura("\\spr\\skill\\唐门\\tm_02_冰附加.spr", pos:1, fs:0, fe:0, tf:30, dir:1, iv:1),
            // ── EMei (峨嵋) states 37-42 ──
            37 => Aura("\\spr\\skill\\峨嵋\\mag_em_06_秋风叶.spr",       pos:2, fs:0, fe:0, tf:16, dir:1, iv:1),
            38 => Aura("\\spr\\skill\\峨嵋\\mag_em_07_醉仙望月.spr",     pos:2, fs:0, fe:0, tf:16, dir:1, iv:1),
            39 => Aura("\\spr\\skill\\峨嵋\\mag_em_08_流水.spr",         pos:2, fs:0, fe:0, tf:16, dir:1, iv:1),
            40 => Aura("\\spr\\skill\\峨嵋\\mag_em_09_梦蝶.spr",         pos:2, fs:0, fe:0, tf:16, dir:1, iv:1),
            41 => Aura("\\spr\\skill\\峨嵋\\mag_em_10_佛心慈佑.spr",     pos:2, fs:0, fe:0, tf:15, dir:1, iv:1),
            42 => Aura("\\spr\\skill\\峨嵋\\mag_em_11_风雨飘香.spr",     pos:1, fs:0, fe:0, tf:4,  dir:1, iv:1),
            // ── CaiBang (丐帮) states 43-44 ──
            43 => Aura("\\spr\\skill\\丐帮\\mag_gb_11_醉蝶狂舞.spr", pos:3, fs:4, fe:12, tf:16, dir:1, iv:1),
            44 => Aura("\\spr\\skill\\丐帮\\mag_gb_12_打狗阵.spr",   pos:2, fs:0, fe:0,  tf:8,  dir:1, iv:1),
            // ── Shaolin (少林) state 45 ──
            45 => Aura("\\spr\\skill\\少林\\sl_07_罗汉阵.spr", pos:2, fs:0, fe:0, tf:10, dir:1, iv:1),
            // ── TianWang (天王) states 46-49 ──
            46 => Aura("\\spr\\skill\\天王\\tw_01_火眼金睛.spr",   pos:1, fs:0,  fe:0,  tf:10, dir:1, iv:2),
            47 => Aura("\\spr\\skill\\天王\\tw_02_天王战意.spr",   pos:3, fs:20, fe:20, tf:20, dir:1, iv:2),
            48 => Aura("\\spr\\skill\\天王\\tw_03_沾衣十八跌.spr", pos:3, fs:20, fe:15, tf:20, dir:1, iv:2),
            49 => Aura("\\spr\\skill\\天王\\tw_04_金钟罩.spr",     pos:3, fs:5,  fe:15, tf:20, dir:1, iv:1),
            // States 1-5 are built-in (stun/poison/freeze/burn/confuse) with no SPR.
            _ => default,
        };

        /// <summary>Compact constructor for PcStateAuraData.</summary>
        private static PcStateAuraData Aura(string sprPath, int pos, int fs, int fe, int tf, int dir, int iv) => new()
        {
            sprPath = sprPath,
            position = pos,
            frameStart = fs,
            frameEnd = fe,
            totalFrames = tf,
            directions = dir,
            intervalTicks = iv,
        };

        /// <summary>Default light color per faction (from PC data patterns).
        /// Each faction has a characteristic color for their skill effects.
        /// </summary>
        public static Color GetFactionDefaultColor(CombatFaction faction)
        {
            return faction switch
            {
                CombatFaction.Shaolin => new Color(1f, 0.84f, 0f),      // Gold
                CombatFaction.TianWang => new Color(1f, 0.84f, 0f),     // Gold
                CombatFaction.TangMen => new Color(0.52f, 0.87f, 0.38f), // Green
                CombatFaction.EMei => new Color(0.39f, 0.71f, 1f),      // Sky blue
                CombatFaction.CuiYan => new Color(0.39f, 0.71f, 1f),    // Water blue
                CombatFaction.WuDu => new Color(0.39f, 0.86f, 0.31f),   // Poison green
                CombatFaction.CaiBang => new Color(1f, 0.68f, 0.24f),   // Orange
                CombatFaction.WuDang => new Color(0.61f, 0.83f, 1f),    // Blue-white
                CombatFaction.TianRen => new Color(1f, 0.4f, 0.4f),     // Red
                CombatFaction.KunLun => new Color(0.8f, 0.6f, 1f),      // Purple
                _ => new Color(0.8f, 0.8f, 0.8f),                        // Gray default
            };
        }

        /// <summary>Clear cache for re-initialization.</summary>
        public void Reset()
        {
            _cache.Clear();
            _initialized = false;
            _skillsProcessed = 0;
            _visualsFound = 0;
            _visualsMissing = 0;
        }
    }
}
