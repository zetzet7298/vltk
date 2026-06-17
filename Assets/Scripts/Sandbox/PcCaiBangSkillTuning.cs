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
    /// <summary>
    /// PC Cái Bang per-level tuning for skills 115-130 + MOD 274, 277, 357, 359, 1073, 1074, 1539.
    /// Values copied from PC gaibang.lua (skill_attackradius, missle_speed_v, skill_misslenum_v)
    /// and PcMissles.txt (Speed, LifeTime). Used by <c>CombatRuntimeService</c>,
    /// <c>SkillEffectVisualService</c>, and auto-targeting to interpolate
    /// the correct PC value at the player's current level instead of a static L20 value.
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
                117 or 119 or 122 => ShortRangeAtLevel(lv),
                125 or 128 or 357 or 359 or 1073 or 1074 or 1539 => LongRangeAtLevel(lv),
                _ => default,
            };
        }

        /// <summary>
        /// PC gaibang.lua per-skill missile speed interpolation (missle_speed_v).
        /// Returns -1 if the skill has no Lua speed override (use engine missles.txt Speed).
        /// Source: jx-source bin/client/script/skill/gaibang.lua.
        /// </summary>
        public static int MissileSpeedAtLevel(int skillId, int level)
        {
            int lv = Mathf.Max(1, level);
            return skillId switch
            {
                // gaibang.lua::kanglong_youhui: missle_speed_v={{1,28},{20,32}}
                128 => InterpolateInt(lv, new[] { (1, 28), (20, 32) }),
                // gaibang.lua::feilong_zaitian: missle_speed_v={{1,20},{20,24}}
                357 => InterpolateInt(lv, new[] { (1, 20), (20, 24) }),
                // gaibang.lua::tianxia_wugou: uses missile 168 engine Speed=24 (no Lua override)
                359 => 24,
                // gaibang.lua::gungaibang150: no speed override → engine missile 335 Speed
                1074 => -1,
                // gaibang.lua::zhanggaibang150: no speed override → engine missile 334 Speed
                1073 => -1,
                // gaibang.lua::bangda_ergou (1539): no speed override → engine missile 47 Speed=31
                1539 => 31,
                _ => -1,
            };
        }

        /// <summary>
        /// PC gaibang.lua per-skill missile count interpolation (skill_misslenum_v).
        /// Returns -1 if the skill has no Lua count override (use catalog childSkillNum).
        /// Source: jx-source bin/client/script/skill/gaibang.lua.
        /// </summary>
        public static int MissileCountAtLevel(int skillId, int level)
        {
            int lv = Mathf.Max(1, level);
            return skillId switch
            {
                // gaibang.lua::feilong_zaitian: skill_misslenum_v={{1,1},{20,4}}
                357 => InterpolateIntRound(lv, new[] { (1, 1), (20, 4) }),
                // gaibang.lua::tianxia_wugou: skill_misslenum_v={{1,1},{20,3}}
                359 => InterpolateIntRound(lv, new[] { (1, 1), (20, 3) }),
                // gaibang.lua::gungaibang150: skill_misslenum_v={{1,1},{20,5}}
                1074 => InterpolateIntRound(lv, new[] { (1, 1), (20, 5) }),
                // gaibang.lua::zhanggaibang150: skill_misslenum_v={{1,1},{20,3}}
                1073 => InterpolateIntRound(lv, new[] { (1, 1), (20, 3) }),
                // gaibang.lua::kanglong_youhui (128): single missile, no count override
                128 => -1,
                // gaibang.lua::bangda_ergou (1539): single missile
                1539 => -1,
                _ => -1,
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

        /// <summary>Round-based interpolation for missile counts (PC uses round, not floor).</summary>
        private static int InterpolateIntRound(int level, (int level, int value)[] points)
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
                    return Mathf.RoundToInt(Mathf.Lerp(prev.value, next.value, t));
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
