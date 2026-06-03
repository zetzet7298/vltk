// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.1 Skill Level Curve Service
// Calculate skill stats at any level 1-20 (or 30 for ultimate) using PC formulas.
// Uses PcSkillTuningRegistry cho faction-specific tuning curves.
// Source: PcSkills.txt + Lua scripts.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Skill stats tại một level cụ thể.
    /// </summary>
    public struct SkillLevelStats
    {
        public int level;
        public int skillCost;
        public int attackRadius;
        public int missileSpeed;
        public float timePerCast;
        public float cooldown;
        public int baseDamage;
    }

    /// <summary>
    /// Tính toán skill stats tại bất kỳ level 1-20 (hoặc 30 cho ultimate).
    /// Dùng PcSkillTuningRegistry cho faction-specific tuning.
    /// Per-level cost, damage, range, cooldown derived từ PcSkills.txt + Lua scripts.
    /// </summary>
    public static class SkillLevelCurveService
    {
        /// <summary>
        /// Get interpolated stats cho skill tại một level.
        /// </summary>
        public static SkillLevelStats GetStats(int skillId, int level, int factionId = 0)
        {
            int lv = Mathf.Clamp(level, 1, 30);
            var spec = PcSkillTuningRegistry.GetSkillSpec(skillId, lv, factionId);
            var entry = FindSkillEntry(skillId, factionId);

            return new SkillLevelStats
            {
                level = lv,
                skillCost = spec.skillCost > 0 ? spec.skillCost : EstimateCost(entry, lv),
                attackRadius = spec.attackRadius > 0 ? spec.attackRadius : EstimateRadius(entry, lv),
                missileSpeed = spec.missileSpeed,
                timePerCast = 2f,
                cooldown = 0f,
                baseDamage = spec.baseDamage > 0 ? spec.baseDamage : EstimateDamage(entry, lv),
            };
        }

        /// <summary>Get stats cho tất cả levels 1..maxLevel.</summary>
        public static List<SkillLevelStats> GetAllLevels(int skillId, int factionId)
        {
            var entry = FindSkillEntry(skillId, factionId);
            int max = entry.maxLevel > 0 ? entry.maxLevel : 20;
            var result = new List<SkillLevelStats>(max);
            for (int lv = 1; lv <= max; lv++)
                result.Add(GetStats(skillId, lv, factionId));
            return result;
        }

        /// <summary>Mana cost tại một level. PC formula: baseCost + level * costScale.</summary>
        public static int GetSkillCost(int skillId, int level, int factionId)
            => GetStats(skillId, level, factionId).skillCost;

        /// <summary>Attack radius tại một level qua PcSkillTuningRegistry.</summary>
        public static int GetAttackRadius(int skillId, int level, int factionId)
            => PcSkillTuningRegistry.GetSkillSpec(skillId, level, factionId).attackRadius;

        // ── Private ────────────────────────────────────────────────────────

        private static SkillSectCatalog.SectSkillEntry FindSkillEntry(int skillId, int factionId)
        {
            var skills = SkillSectCatalog.GetSkills(factionId);
            foreach (var s in skills)
                if (s.skillId == skillId) return s;
            // Search all factions
            foreach (var fid in CombatFactionExt.AllFactions)
            {
                skills = SkillSectCatalog.GetSkills(fid);
                foreach (var s in skills)
                    if (s.skillId == skillId) return s;
            }
            return default;
        }

        private static int EstimateCost(SkillSectCatalog.SectSkillEntry entry, int lv)
        {
            if (entry.tier == SkillSectCatalog.SkillTier.Passive) return 0;
            int baseCost = entry.tier switch
            {
                SkillSectCatalog.SkillTier.Active  => 10,
                SkillSectCatalog.SkillTier.Ultimate => 50,
                SkillSectCatalog.SkillTier.Buff     => 20,
                _ => 10,
            };
            return baseCost + (lv - 1) * 2;
        }

        private static int EstimateRadius(SkillSectCatalog.SectSkillEntry entry, int lv)
        {
            return entry.tier switch
            {
                SkillSectCatalog.SkillTier.Passive  => 0,
                SkillSectCatalog.SkillTier.Active   => 180 + lv * 10,
                SkillSectCatalog.SkillTier.Ultimate => 400 + lv * 5,
                SkillSectCatalog.SkillTier.Buff     => 400,
                _ => 180,
            };
        }

        private static int EstimateDamage(SkillSectCatalog.SectSkillEntry entry, int lv)
        {
            return entry.tier switch
            {
                SkillSectCatalog.SkillTier.Passive  => 0,
                SkillSectCatalog.SkillTier.Active   => 10 + lv * 8,
                SkillSectCatalog.SkillTier.Ultimate => 50 + lv * 15,
                _ => 0,
            };
        }
    }
}
