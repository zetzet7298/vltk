using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// PC JX1 max-mana formula for character classes (Cái Bang / Thiếu Lâm / etc).
    /// PC source-of-truth: KNpc::CalcCurManaMax → m_CurrentManaMax = m_ManaMax.
    /// m_ManaMax is initialized from the character's class table at character
    /// creation and grows with level + inner-strength (nội công) stat.
    ///
    /// In VLTK PC (Jianghu 6.0), max mana for a level L Cái Bang character with
    /// inner-strength I is approximately:
    ///     MaxMana(L, I) = 100 + L * 18 + I * 1
    /// (Jianghu era 2003-2006 baseline, used in mobile sandbox tests).
    /// A level 200 character with inner-strength 0 → 3700; with 1300 → 5000.
    /// This matches the PC jxwin era where level-150 characters cap ~3500-4500.
    /// </summary>
    public static class PcMaxManaFormula
    {
        public const int BaseMana = 100;
        public const int ManaPerLevel = 18;
        public const int ManaPerInnerStrength = 1;

        public static int Compute(int level, int innerStrength, CombatFaction faction = CombatFaction.None)
        {
            int lv = level < 1 ? 1 : level;
            int inner = innerStrength < 0 ? 0 : innerStrength;
            int raw = BaseMana + lv * ManaPerLevel + inner * ManaPerInnerStrength;
            return raw < 0 ? int.MaxValue : raw;
        }

        public static int ComputeCaiBang(int level, int innerStrength)
            => Compute(level, innerStrength, CombatFaction.CaiBang);
    }
}
