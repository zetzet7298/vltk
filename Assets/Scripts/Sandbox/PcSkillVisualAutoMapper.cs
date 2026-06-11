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

        /// <summary>Has any visual data (flight or explosion)?</summary>
        public bool HasAnyVisual => HasFlightVisual || HasExplodeVisual || hasPreCast || isMelee;
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
                // Melee or instant skill — still may have PreCast visual
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
        /// Default light color per faction (from PC data patterns).
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
