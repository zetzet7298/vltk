// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorSkillLibraryConfig
// Parity dhcd BattleCore.RandomSkillLibraryConfig (diffable-cs recovered):
//   DependSkills: RandomSkillDependEntry[]  — chuỗi unlock skill-to-skill
//   SkillID                                — skill thật được cấp
// Wrapper quanh SkillDef cho SkillChoicePool: Def + depend chain.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.Survivor
{
    /// <summary>
    /// 1 mục pool = 1 SkillDef + depend unlock (parity dhcd RandomSkillLibraryConfig
    /// DependSkills). DependSkills null/empty → luôn sẵn (tier 1).
    /// </summary>
    public sealed class SurvivorSkillLibraryConfig
    {
        public readonly SkillDef Def;
        public readonly List<SurvivorSkillDependEntry> DependSkills; // null/empty = luôn sẵn

        public SurvivorSkillLibraryConfig(SkillDef def, List<SurvivorSkillDependEntry> dependSkills = null)
        {
            Def = def;
            DependSkills = dependSkills;
        }

        /// <summary>
        /// Depend unlock (dhcd RandomSkillDependEntry semantics). null/empty → true.
        /// Mỗi entry: met = roster.GetLevel(Id) >= Lv.
        ///  - Remove=false (unlock gate): chưa met → false — skill chưa mở
        ///    (config Phase 1: 1073←{128,5}, 1074←{125,5}).
        ///  - Remove=true (removal trigger): met → false — skill RÚT khỏi pool
        ///    khi prereq đạt tier (literal field name IsRemove; own config Phase 1
        ///    toàn Remove=false nên semantics này chưa exercised — dhcd IL
        ///    decompile (RandomSkillCmpt) quá mangle để trích nhánh chính xác,
        ///    ghi nhận pending nếu port config Remove=true sau này).
        /// </summary>
        public bool IsDependMet(SkillCastRuntime roster)
        {
            if (DependSkills == null || DependSkills.Count == 0) return true;
            for (int i = 0; i < DependSkills.Count; i++)
            {
                var e = DependSkills[i];
                bool met = roster != null && roster.GetLevel(e.Id) >= e.Lv;
                if (e.Remove) { if (met) return false; }
                else { if (!met) return false; }
            }
            return true;
        }
    }
}
