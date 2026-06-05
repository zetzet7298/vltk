// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for skill data registries (in-memory only, no file IO).
// 12 test cases covering all 6 registry classes.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class SkillLevelUpgradeParserTests
    {
        // ── PcSkillLevelDataRegistry ──────────────────────────────────────
        [Test]
        public void PcSkillLevelDataRegistry_Count_NonNegative()
        {
            var reg = new PcSkillLevelDataRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSkillLevelDataRegistry_GetBySkill_FiltersCorrectly()
        {
            var reg = new PcSkillLevelDataRegistry();
            reg.Register(new PcSkillLevelDataEntry { skillId = 100, level = 1, damageMin = 10, damageMax = 20 });
            reg.Register(new PcSkillLevelDataEntry { skillId = 100, level = 2, damageMin = 20, damageMax = 40 });
            reg.Register(new PcSkillLevelDataEntry { skillId = 200, level = 1, damageMin = 50, damageMax = 60 });
            var forSkill100 = reg.GetBySkill(100);
            Assert.AreEqual(2, forSkill100.Count);
            Assert.AreEqual(2, reg.GetMaxLevelForSkill(100));
        }

        // ── PcSkillUpgradeRegistry ───────────────────────────────────────
        [Test]
        public void PcSkillUpgradeRegistry_Count_NonNegative()
        {
            var reg = new PcSkillUpgradeRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSkillUpgradeRegistry_GetByRequiredSkill_FiltersCorrectly()
        {
            var reg = new PcSkillUpgradeRegistry();
            reg.Register(new PcSkillUpgradeEntry { skillId = 1, requiredPrevSkill = 100, resultSkillId = 200, upgradeType = 0 });
            reg.Register(new PcSkillUpgradeEntry { skillId = 2, requiredPrevSkill = 100, resultSkillId = 201, upgradeType = 1 });
            reg.Register(new PcSkillUpgradeEntry { skillId = 3, requiredPrevSkill = 300, resultSkillId = 301, upgradeType = 0 });
            Assert.AreEqual(2, reg.GetByRequiredSkill(100).Count);
            Assert.AreEqual(1, reg.GetByRequiredSkill(300).Count);
        }

        // ── PcSkillBookRegistry ───────────────────────────────────────────
        [Test]
        public void PcSkillBookRegistry_Count_NonNegative()
        {
            var reg = new PcSkillBookRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSkillBookRegistry_GetByType_FiltersCorrectly()
        {
            var reg = new PcSkillBookRegistry();
            reg.Register(new PcSkillBookEntry { bookId = 1, bookType = 0, teachesSkillId = 100 });
            reg.Register(new PcSkillBookEntry { bookId = 2, bookType = 0, teachesSkillId = 101 });
            reg.Register(new PcSkillBookEntry { bookId = 3, bookType = 2, teachesSkillId = 200 });
            Assert.AreEqual(2, reg.GetByType(0).Count);
            Assert.AreEqual(1, reg.GetByType(2).Count);
        }

        // ── PcSkillComboRegistry ─────────────────────────────────────────
        [Test]
        public void PcSkillComboRegistry_Count_NonNegative()
        {
            var reg = new PcSkillComboRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSkillComboRegistry_GetByClass_FiltersCorrectly()
        {
            var reg = new PcSkillComboRegistry();
            reg.Register(new PcSkillComboEntry { comboId = 1, requiredClass = 5, requiredPlayerLevel = 30, skillSequence = "1;2;3" });
            reg.Register(new PcSkillComboEntry { comboId = 2, requiredClass = 5, requiredPlayerLevel = 50, skillSequence = "4;5;6" });
            reg.Register(new PcSkillComboEntry { comboId = 3, requiredClass = 6, requiredPlayerLevel = 30, skillSequence = "7;8;9" });
            Assert.AreEqual(2, reg.GetByClass(5).Count);
        }

        // ── PcSkillStateRegistry ─────────────────────────────────────────
        [Test]
        public void PcSkillStateRegistry_Count_NonNegative()
        {
            var reg = new PcSkillStateRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSkillStateRegistry_GetByType_FiltersCorrectly()
        {
            var reg = new PcSkillStateRegistry();
            reg.Register(new PcSkillStateEntry { stateId = 1, type = 0, effectValue = 10, stackMax = 3 });
            reg.Register(new PcSkillStateEntry { stateId = 2, type = 0, effectValue = 20, stackMax = 5 });
            reg.Register(new PcSkillStateEntry { stateId = 3, type = 2, effectValue = 0, stackMax = 1 });
            Assert.AreEqual(2, reg.GetByType(0).Count);
            Assert.AreEqual(1, reg.GetByType(2).Count);
        }

        // ── PcSkillMasteryRegistry ───────────────────────────────────────
        [Test]
        public void PcSkillMasteryRegistry_Count_NonNegative()
        {
            var reg = new PcSkillMasteryRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSkillMasteryRegistry_GetByGenre_FiltersCorrectly()
        {
            var reg = new PcSkillMasteryRegistry();
            reg.Register(new PcSkillMasteryEntry { masteryId = 1, classId = 5, skillGenre = 0, bonusValue = 5, maxPoints = 20 });
            reg.Register(new PcSkillMasteryEntry { masteryId = 2, classId = 5, skillGenre = 0, bonusValue = 3, maxPoints = 10 });
            reg.Register(new PcSkillMasteryEntry { masteryId = 3, classId = 6, skillGenre = 3, bonusValue = 4, maxPoints = 15 });
            Assert.AreEqual(2, reg.GetByGenre(0).Count);
            Assert.AreEqual(1, reg.GetByGenre(3).Count);
        }
    }
}
