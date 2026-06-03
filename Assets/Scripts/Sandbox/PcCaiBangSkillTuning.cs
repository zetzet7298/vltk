using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC Cái Bang per-level tuning for skills 115-130 + MOD 274, 277, 357, 359, 1073, 1074, 1539.
    /// Values copied from PC gaibang.lua (skill_attackradius, missle_speed_v) and
    /// PcMissles.txt (Speed, LifeTime). Used by <c>CombatRuntimeService</c>,
    /// <c>SkillEffectVisualService</c>, and auto-targeting to interpolate the
    /// correct PC value at the player's current level instead of a static L20 value.
    /// </summary>
    public static class PcCaiBangSkillTuning
    {
        public static bool Applies(int skillId) => skillId is 117 or 119 or 122 or 125 or 128
            or 357 or 359 or 1073 or 1074 or 1539;

        public static PcCaiBangLevelSpec AtLevel(int skillId, int level)
        {
            int lv = Mathf.Max(1, level);
            return skillId switch
            {
                117 or 119 or 122 => ShortRangeAtLevel(lv),    // PC yanmen_tuobo / jianren_shenshou: radius 320→384
                125 or 128 or 357 or 359 or 1073 or 1074 or 1539 => LongRangeAtLevel(lv), // PC bangda/kanglong/feilong/etc: 448→512
                _ => default,
            };
        }

        /// <summary>PC gaibang.lua: skill_attackradius={{{1,320},{20,384}}}.</summary>
        private static PcCaiBangLevelSpec ShortRangeAtLevel(int lv) => new()
        {
            attackRadius = InterpolateInt(lv, new[] { (1, 320), (20, 384) }),
        };

        /// <summary>PC gaibang.lua: skill_attackradius={{{1,448},{20,512}}}.</summary>
        private static PcCaiBangLevelSpec LongRangeAtLevel(int lv) => new()
        {
            attackRadius = InterpolateInt(lv, new[] { (1, 448), (20, 512) }),
        };

        private static int InterpolateInt(int level, (int level, int value)[] points)
        {
            if (points == null || points.Length == 0) return 0;
            if (level <= points[0].level) return points[0].value;
            for (int i = 1; i < points.Length; i++)
            {
                var prev = points[i - 1];
                var next = points[i];
                if (level <= next.level)
                {
                    if (next.level <= prev.level) return next.value;
                    float t = (level - prev.level) / (float)(next.level - prev.level);
                    return Mathf.FloorToInt(Mathf.Lerp(prev.value, next.value, t));
                }
            }
            return points[points.Length - 1].value;
        }
    }

    public struct PcCaiBangLevelSpec
    {
        public int attackRadius;
    }
}
