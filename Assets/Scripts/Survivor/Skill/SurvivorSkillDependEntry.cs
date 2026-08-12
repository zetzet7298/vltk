// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorSkillDependEntry
// Parity dhcd BattleCore.RandomSkillDependEntry (diffable-cs recovered):
//   public uint ID;    // Field offset 0x10 — prereq skillId
//   public int  Lv;    // Field offset 0x14 — prereq level cần có
//   public byte IsRemove; // Field offset 0x18 — byte ↔ bool (Remove)
// -----------------------------------------------------------------------------

namespace VLTK.Survivor
{
    /// <summary>
    /// 1 ràng buộc depend skill-to-skill (dhcd RandomSkillDependEntry parity).
    /// Dùng trong SurvivorSkillLibraryConfig.DependSkills — skill sau chỉ vào
    /// pool khi prereq đạt Lv (plan §5b Q2 đã chốt: depend chain, KHÔNG time).
    /// </summary>
    public readonly struct SurvivorSkillDependEntry
    {
        public readonly int Id;      // prereq skillId
        public readonly int Lv;      // prereq level cần có
        public readonly bool Remove; // dhcd IsRemove byte → bool

        public SurvivorSkillDependEntry(int id, int lv, bool remove = false)
        {
            Id = id;
            Lv = lv;
            Remove = remove;
        }
    }
}
