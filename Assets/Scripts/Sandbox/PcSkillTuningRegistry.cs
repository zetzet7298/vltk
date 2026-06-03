// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.1 Skill Tuning Registry
// Generalized per-faction skill tuning: attackRadius, missle_speed_v, damage curves.
// Extends PcCaiBangSkillTuning pattern to all 10 phái.
// Source: PcSkills.txt LvlSetting/LvlData columns + Lua scripts.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Per-skill tuning spec tại một level cụ thể.
    /// </summary>
    public struct SkillTuningSpec
    {
        public int attackRadius;
        public int missileSpeed;
        public int baseDamage;
        public int skillCost;
    }

    /// <summary>
    /// Generalized per-faction skill tuning. Mở rộng PcCaiBangSkillTuning
    /// cho tất cả 10 phái. Values từ PcSkills.txt AttackRadius + Lua scripts.
    /// </summary>
    public static class PcSkillTuningRegistry
    {
        // Faction → skillId → level curve points cho attackRadius.
        private static readonly Dictionary<int, Dictionary<int, (int lv, int val)[]>> RadiusCurves = new()
        {
            [CombatFactionExt.ShaolinId] = new()
            {
                [10] = new[] { (1, 90), (20, 90) },
                [11] = new[] { (1, 90), (20, 90) },
                [14] = new[] { (1, 90), (20, 90) },
                [19] = new[] { (1, 200), (20, 200) },
                [16] = new[] { (1, 180), (20, 180) },
            },
            [CombatFactionExt.TianWangId] = new()
            {
                [29] = new[] { (1, 72), (20, 72) },
                [30] = new[] { (1, 90), (20, 90) },
                [32] = new[] { (1, 90), (20, 90) },
                [34] = new[] { (1, 72), (20, 72) },
                [35] = new[] { (1, 90), (20, 90) },
                [37] = new[] { (1, 90), (20, 90) },
                [40] = new[] { (1, 200), (20, 200) },
                [41] = new[] { (1, 90), (20, 90) },
            },
            [CombatFactionExt.TangMenId] = new()
            {
                [45] = new[] { (1, 400), (20, 400) },
                [47] = new[] { (1, 450), (20, 450) },
                [50] = new[] { (1, 360), (20, 360) },
                [54] = new[] { (1, 400), (20, 400) },
                [58] = new[] { (1, 520), (20, 520) },
            },
            [CombatFactionExt.WuDuId] = new()
            {
                [63] = new[] { (1, 180), (20, 180) },
                [65] = new[] { (1, 400), (20, 400) },
                [68] = new[] { (1, 400), (20, 400) },
                [71] = new[] { (1, 420), (20, 420) },
                [74] = new[] { (1, 400), (20, 400) },
            },
            [CombatFactionExt.CaiBangId] = new()
            {
                [117] = new[] { (1, 280), (20, 384) },
                [119] = new[] { (1, 240), (20, 384) },
                [122] = new[] { (1, 300), (20, 384) },
                [125] = new[] { (1, 448), (20, 512) },
                [128] = new[] { (1, 448), (20, 512) },
            },
            [CombatFactionExt.TianRenId] = new()
            {
                [135] = new[] { (1, 270), (20, 270) },
                [138] = new[] { (1, 400), (20, 400) },
                [141] = new[] { (1, 72), (20, 72) },
                [145] = new[] { (1, 280), (20, 280) },
                [148] = new[] { (1, 570), (20, 570) },
            },
            [CombatFactionExt.EMeiId] = new()
            {
                [80] = new[] { (1, 240), (20, 240) },
                [82] = new[] { (1, 570), (20, 570) },
                [85] = new[] { (1, 180), (20, 180) },
                [88] = new[] { (1, 360), (20, 360) },
                [91] = new[] { (1, 400), (20, 400) },
            },
            [CombatFactionExt.CuiYanId] = new()
            {
                [99]  = new[] { (1, 360), (20, 360) },
                [102] = new[] { (1, 360), (20, 360) },
                [105] = new[] { (1, 300), (20, 300) },
                [108] = new[] { (1, 420), (20, 420) },
                [111] = new[] { (1, 72), (20, 72) },
                [113] = new[] { (1, 400), (20, 400) },
            },
            [CombatFactionExt.WuDangId] = new()
            {
                [153] = new[] { (1, 400), (20, 400) },
                [155] = new[] { (1, 480), (20, 480) },
                [158] = new[] { (1, 400), (20, 400) },
            },
            [CombatFactionExt.KunLunId] = new()
            {
                [169] = new[] { (1, 400), (20, 400) },
                [172] = new[] { (1, 570), (20, 570) },
                [175] = new[] { (1, 400), (20, 400) },
                [178] = new[] { (1, 570), (20, 570) },
            },
        };

        /// <summary>
        /// Get interpolated skill spec tại một level.
        /// Fallback sang PcCaiBangSkillTuning cho Cái Bang.
        /// </summary>
        public static SkillTuningSpec GetSkillSpec(int skillId, int level, int factionId = 0)
        {
            int lv = Mathf.Max(1, level);
            var spec = new SkillTuningSpec();

            // Cái Bang legacy path
            if (factionId == CombatFactionExt.CaiBangId || PcCaiBangSkillTuning.Applies(skillId))
            {
                var cb = PcCaiBangSkillTuning.AtLevel(skillId, lv);
                spec.attackRadius = cb.attackRadius;
            }

            // Override từ registry nếu có
            if (RadiusCurves.TryGetValue(factionId, out var factionSkills) &&
                factionSkills.TryGetValue(skillId, out var points))
            {
                spec.attackRadius = InterpolateInt(lv, points);
            }
            else if (spec.attackRadius == 0)
            {
                // Fallback: search all factions
                foreach (var fc in RadiusCurves.Values)
                {
                    if (fc.TryGetValue(skillId, out var pts))
                    {
                        spec.attackRadius = InterpolateInt(lv, pts);
                        break;
                    }
                }
            }

            return spec;
        }

        /// <summary>
        /// Linear interpolation giữa (level, value) control points.
        /// Same algorithm as PcCaiBangSkillTuning.InterpolateInt.
        /// </summary>
        public static int InterpolateInt(int level, (int lv, int val)[] points)
        {
            if (points == null || points.Length == 0) return 0;
            if (level <= points[0].lv) return points[0].val;
            for (int i = 1; i < points.Length; i++)
            {
                var prev = points[i - 1];
                var next = points[i];
                if (level <= next.lv)
                {
                    if (next.lv <= prev.lv) return next.val;
                    float t = (level - prev.lv) / (float)(next.lv - prev.lv);
                    return Mathf.FloorToInt(Mathf.Lerp(prev.val, next.val, t));
                }
            }
            return points[points.Length - 1].val;
        }

        /// <summary>Kiểm tra tuning data có tồn tại cho skill không.</summary>
        public static bool HasTuning(int skillId, int factionId)
        {
            if (PcCaiBangSkillTuning.Applies(skillId)) return true;
            if (RadiusCurves.TryGetValue(factionId, out var fc) && fc.ContainsKey(skillId)) return true;
            foreach (var curveDict in RadiusCurves.Values)
                if (curveDict.ContainsKey(skillId)) return true;
            return false;
        }
    }
}
