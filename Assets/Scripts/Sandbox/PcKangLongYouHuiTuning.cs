using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Level tuning recovered from the JXWin VM runtime data for Kháng Long Hữu Hối (亢龙有悔).
    /// Source: VMDK pagefile/runtime table `kanglong_youhui` referenced by VM Skills row via `\script\skill\gaibang.lua`.
    /// PC format uses Lua breakpoint tables like {{{1,1},{10,1},{20,15},{25,18},{26,18}}}.
    /// </summary>
    public static class PcKangLongYouHuiTuning
    {
        public const int SkillId = 128;

        public static bool Applies(int skillId) => skillId == SkillId;

        public static PcKangLongSpec AtLevel(int level)
        {
            int lv = Mathf.Max(1, level);
            return new PcKangLongSpec
            {
                missileForm = (SkillMissileForm)InterpolateInt(lv, new[] { (1, 1), (10, 1), (10, 2), (20, 2) }),
                missileCount = InterpolateInt(lv, new[] { (1, 1), (10, 1), (20, 15), (25, 18), (26, 18) }),
                param1 = InterpolateInt(lv, new[] { (1, 0), (10, 0), (10, 2), (20, 2), (21, 2) }),
                missileSpeed = InterpolateInt(lv, new[] { (1, 28), (20, 32) }),
                attackRadius = InterpolateInt(lv, new[] { (1, 448), (20, 512) }),
                manaCost = InterpolateInt(lv, new[] { (1, 10), (20, 50) }),
                seriesDamageP = InterpolateInt(lv, new[] { (1, 10), (20, 50), (21, 52) }),
            };
        }

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

    public struct PcKangLongSpec
    {
        public SkillMissileForm missileForm;
        public int missileCount;
        public int param1;
        public int missileSpeed;
        public int attackRadius;
        public int manaCost;
        public int seriesDamageP;
    }
}
