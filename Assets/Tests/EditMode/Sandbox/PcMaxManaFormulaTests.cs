using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Tests for PC JX1 max-mana formula. Mirrors KNpc::CalcCurManaMax and
    /// the character class table from PC jxwin era (Jianghu 6.0).
    /// </summary>
    public class PcMaxManaFormulaTests
    {
        [Test]
        public void Compute_Level1_ZeroInnerStrength_ReturnsBase()
        {
            Assert.AreEqual(100 + 1 * 18, PcMaxManaFormula.Compute(1, 0));
        }

        [Test]
        public void Compute_Level200_ZeroInnerStrength_ReturnsPcMaxMana()
        {
            // PC JX1: level 200 character with no inner-strength → ~3700 mana.
            // This matches the previous hardcoded "5000" upper bound (level 200 + I=1300).
            int expected = 100 + 200 * 18;
            Assert.AreEqual(expected, PcMaxManaFormula.Compute(200, 0));
        }

        [Test]
        public void Compute_HighInnerStrength_AddsMana()
        {
            // PC JX1: inner-strength is character class's primary mana stat.
            int noInner = PcMaxManaFormula.Compute(150, 0);
            int withInner = PcMaxManaFormula.Compute(150, 1300);
            Assert.AreEqual(1300, withInner - noInner);
        }

        [Test]
        public void Compute_NegativeInputs_AreClamped()
        {
            // Defensive: level=0/-1 must not produce negative or huge mana.
            Assert.AreEqual(100 + 1 * 18, PcMaxManaFormula.Compute(0, -50));
            Assert.GreaterOrEqual(PcMaxManaFormula.Compute(-10, -100), 0);
        }

        [Test]
        public void ComputeCaiBang_Level200_ReachesPreviousSandboxBudget()
        {
            // Regression: the old hardcoded 5000 must still be reachable for a
            // level-200 character (PC jxwin era 150-tier with high inner-strength).
            int level200FullMana = PcMaxManaFormula.ComputeCaiBang(200, 1300);
            Assert.AreEqual(5000, level200FullMana);
        }
    }
}
