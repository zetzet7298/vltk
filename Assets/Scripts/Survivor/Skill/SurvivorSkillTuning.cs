// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorSkillTuning
// Phase 3 (PORT_CAIBANG §3 Gap D / §5 Q5): số sao hiển thị card pick.
//   star = ceil(level * N_STARS / MaxLevel) — parity dhcd RandomSkillLibraryConfig.Level.
// Own const (plan §4 Phase 3): 1 const đủ — SO chỉ khi tuning đa instance.
// Fail-closed: maxLevel <= 0 → 0★ (không div-by-zero, không bịa cấp).
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Survivor
{
    /// <summary>Star-level tuning cho card pick (Phase 3).</summary>
    public static class SurvivorSkillTuning
    {
        /// <summary>Số sao tối đa hiển thị (plan Q5: 5★).</summary>
        public const int N_STARS = 5;

        /// <summary>star = ceil(level*N/max); level 0 → 0★; max &lt;= 0 → 0★ (fail-closed).</summary>
        public static int StarCount(int level, int maxLevel)
        {
            if (maxLevel <= 0) return 0;
            int s = (int)Math.Ceiling(level * (double)N_STARS / maxLevel);
            return Math.Max(0, Math.Min(s, N_STARS));
        }
    }
}
