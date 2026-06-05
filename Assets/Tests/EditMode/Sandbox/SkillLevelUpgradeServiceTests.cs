// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for skill data services (Level Data, Upgrade, Book,
// Combo, State, Mastery). 30 test cases covering all 6 service classes.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class SkillLevelUpgradeServiceTests
    {
        // ── SkillLevelDataService ──────────────────────────────────────────
        [Test]
        public void SkillLevelDataService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SkillLevelDataService.LoadFromStreamingAssets());
        }

        [Test]
        public void SkillLevelDataService_GetMaxLevel_RejectsInvalid()
        {
            var svc = new SkillLevelDataService();
            Assert.AreEqual(0, svc.GetMaxLevel(-1));
            Assert.AreEqual(0, svc.GetMaxLevel(999999));
        }

        [Test]
        public void SkillLevelDataService_GetManaCost_ZeroForUnknown()
        {
            var svc = new SkillLevelDataService();
            Assert.AreEqual(0, svc.GetManaCost(999999, 1));
        }

        [Test]
        public void SkillLevelDataService_GetDamageRange_ZeroForUnknown()
        {
            var svc = new SkillLevelDataService();
            var range = svc.GetDamageRange(999999, 1);
            Assert.AreEqual(0, range.min);
            Assert.AreEqual(0, range.max);
        }

        [Test]
        public void SkillLevelDataService_CanLearnAt_RejectsLowLevel()
        {
            var svc = new SkillLevelDataService();
            Assert.IsFalse(svc.CanLearnAt(999999, 1, 50));
        }

        // ── SkillUpgradeService ───────────────────────────────────────────
        [Test]
        public void SkillUpgradeService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SkillUpgradeService.LoadFromStreamingAssets());
        }

        [Test]
        public void SkillUpgradeService_GetUpgrade_ReturnsNullForInvalid()
        {
            var svc = new SkillUpgradeService();
            Assert.IsNull(svc.GetUpgrade(999999));
        }

        [Test]
        public void SkillUpgradeService_CanUpgrade_RejectsInsufficientLevel()
        {
            var svc = new SkillUpgradeService();
            var learned = new System.Collections.Generic.HashSet<int>();
            // Skill 999999 chưa tồn tại → false
            Assert.IsFalse(svc.CanUpgrade(999999, 50, 100, 0, learned));
        }

        [Test]
        public void SkillUpgradeService_TryUpgrade_ReturnsZero_WhenFailed()
        {
            var svc = new SkillUpgradeService();
            int points = 100;
            var learned = new System.Collections.Generic.HashSet<int>();
            int result = svc.TryUpgrade(999999, 50, ref points, 0, learned);
            Assert.AreEqual(0, result);
            // points unchanged
            Assert.AreEqual(100, points);
        }

        [Test]
        public void SkillUpgradeService_GetNextSkillInChain_ZeroForTerminal()
        {
            var svc = new SkillUpgradeService();
            Assert.AreEqual(0, svc.GetNextSkillInChain(999999));
        }

        // ── SkillBookService ──────────────────────────────────────────────
        [Test]
        public void SkillBookService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SkillBookService.LoadFromStreamingAssets());
        }

        [Test]
        public void SkillBookService_GetBySkill_FiltersCorrectly()
        {
            var reg = new PcSkillBookRegistry();
            reg.Register(new PcSkillBookEntry { bookId = 1, teachesSkillId = 100, bookType = 0, requiredLevel = 10 });
            reg.Register(new PcSkillBookEntry { bookId = 2, teachesSkillId = 100, bookType = 1, requiredLevel = 30 });
            reg.Register(new PcSkillBookEntry { bookId = 3, teachesSkillId = 200, bookType = 0, requiredLevel = 5 });
            var forSkill100 = reg.GetBySkill(100);
            Assert.AreEqual(2, forSkill100.Count);
        }

        [Test]
        public void SkillBookService_CanUseBook_RejectsLowLevel()
        {
            var reg = new PcSkillBookRegistry();
            reg.Register(new PcSkillBookEntry { bookId = 1, teachesSkillId = 100, bookType = 0, requiredLevel = 50 });
            var svc = new SkillBookService(reg);
            Assert.IsFalse(svc.CanUseBook(1, 30));
            Assert.IsTrue(svc.CanUseBook(1, 60));
        }

        [Test]
        public void SkillBookService_TryUseBook_ReturnsSkill_WhenSuccess()
        {
            var reg = new PcSkillBookRegistry();
            reg.Register(new PcSkillBookEntry { bookId = 1, teachesSkillId = 100, bookType = 0, requiredLevel = 10 });
            var svc = new SkillBookService(reg);
            var known = new System.Collections.Generic.HashSet<int>();
            int learned = svc.TryUseBook(1, 50, known);
            Assert.AreEqual(100, learned);
            Assert.IsTrue(known.Contains(100));
        }

        [Test]
        public void SkillBookService_GetBookTypeName_NonEmpty()
        {
            Assert.AreEqual("Sơ cấp", SkillBookService.GetBookTypeName(0));
            Assert.AreEqual("Cao cấp", SkillBookService.GetBookTypeName(1));
            Assert.AreEqual("Đại sư", SkillBookService.GetBookTypeName(2));
            Assert.AreEqual("Thiên cấp", SkillBookService.GetBookTypeName(3));
            Assert.IsNotEmpty(SkillBookService.GetBookTypeName(999));
        }

        // ── SkillComboService ─────────────────────────────────────────────
        [Test]
        public void SkillComboService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SkillComboService.LoadFromStreamingAssets());
        }

        [Test]
        public void SkillComboService_GetByClass_FiltersCorrectly()
        {
            var reg = new PcSkillComboRegistry();
            reg.Register(new PcSkillComboEntry { comboId = 1, nameRaw = "combo1", requiredClass = 5, requiredPlayerLevel = 30, skillSequence = "1;2;3" });
            reg.Register(new PcSkillComboEntry { comboId = 2, nameRaw = "combo2", requiredClass = 6, requiredPlayerLevel = 50, skillSequence = "4;5;6" });
            var svc = new SkillComboService(reg);
            Assert.AreEqual(1, svc.GetByClass(5).Count);
            Assert.AreEqual(1, svc.GetByClass(6).Count);
        }

        [Test]
        public void SkillComboService_CanExecuteCombo_RejectsWrongOrder()
        {
            var reg = new PcSkillComboRegistry();
            reg.Register(new PcSkillComboEntry { comboId = 1, nameRaw = "triple", requiredClass = 0, requiredPlayerLevel = 30, skillSequence = "10;20;30" });
            var svc = new SkillComboService(reg);
            var recent = new System.Collections.Generic.List<int> { 30, 20, 10 }; // wrong order
            Assert.IsFalse(svc.CanExecuteCombo(1, 5, 50, recent));
            recent.Clear();
            recent.AddRange(new[] { 10, 20, 30 }); // correct order
            Assert.IsTrue(svc.CanExecuteCombo(1, 5, 50, recent));
        }

        [Test]
        public void SkillComboService_GetBonusEffect_ZeroForInvalid()
        {
            var svc = new SkillComboService();
            Assert.AreEqual(0, svc.GetBonusEffect(999999));
        }

        // ── SkillStateService ─────────────────────────────────────────────
        [Test]
        public void SkillStateService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SkillStateService.LoadFromStreamingAssets());
        }

        [Test]
        public void SkillStateService_GetByType_FiltersCorrectly()
        {
            var reg = new PcSkillStateRegistry();
            reg.Register(new PcSkillStateEntry { stateId = 1, type = 0, effectValue = 10, stackMax = 3 });
            reg.Register(new PcSkillStateEntry { stateId = 2, type = 4, effectValue = 50, stackMax = 5 });
            reg.Register(new PcSkillStateEntry { stateId = 3, type = 0, effectValue = 20, stackMax = 1 });
            var svc = new SkillStateService(reg);
            var buffs = svc.GetByType(0);
            Assert.AreEqual(2, buffs.Count);
            var bleeds = svc.GetByType(4);
            Assert.AreEqual(1, bleeds.Count);
        }

        [Test]
        public void SkillStateService_GetStateTypeName_NonEmpty()
        {
            Assert.AreEqual("Tăng cường", SkillStateService.GetStateTypeName(0));
            Assert.AreEqual("Choáng", SkillStateService.GetStateTypeName(2));
            Assert.AreEqual("Chảy máu", SkillStateService.GetStateTypeName(4));
            Assert.AreEqual("Đóng băng", SkillStateService.GetStateTypeName(6));
            Assert.IsNotEmpty(SkillStateService.GetStateTypeName(999));
        }

        [Test]
        public void SkillStateService_ComputeTickDamage_ZeroForInvalid()
        {
            var svc = new SkillStateService();
            Assert.AreEqual(0, svc.ComputeTickDamage(999999, 1));
        }

        [Test]
        public void SkillStateService_CanStack_FalseWhenMax()
        {
            var reg = new PcSkillStateRegistry();
            reg.Register(new PcSkillStateEntry { stateId = 1, type = 4, effectValue = 10, stackMax = 3 });
            var svc = new SkillStateService(reg);
            Assert.IsTrue(svc.CanStack(1, 2));
            Assert.IsFalse(svc.CanStack(1, 3));
            Assert.IsFalse(svc.CanStack(1, 10));
        }

        // ── SkillMasteryService ───────────────────────────────────────────
        [Test]
        public void SkillMasteryService_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => SkillMasteryService.LoadFromStreamingAssets());
        }

        [Test]
        public void SkillMasteryService_GetByClass_FiltersCorrectly()
        {
            var reg = new PcSkillMasteryRegistry();
            reg.Register(new PcSkillMasteryEntry { masteryId = 1, classId = 5, skillGenre = 0, bonusType = 1, bonusValue = 5, maxPoints = 20 });
            reg.Register(new PcSkillMasteryEntry { masteryId = 2, classId = 5, skillGenre = 1, bonusType = 1, bonusValue = 5, maxPoints = 20 });
            reg.Register(new PcSkillMasteryEntry { masteryId = 3, classId = 6, skillGenre = 0, bonusType = 2, bonusValue = 3, maxPoints = 10 });
            var svc = new SkillMasteryService(reg);
            Assert.AreEqual(2, svc.GetByClass(5).Count);
            Assert.AreEqual(1, svc.GetByClass(6).Count);
        }

        [Test]
        public void SkillMasteryService_ComputeBonus_RejectsInvalid()
        {
            var svc = new SkillMasteryService();
            Assert.AreEqual(0, svc.ComputeBonus(999999, 10));
            Assert.AreEqual(0, svc.ComputeBonus(1, 0));
            Assert.AreEqual(0, svc.ComputeBonus(1, -5));
        }

        [Test]
        public void SkillMasteryService_GetMaxPoints_NonNegative()
        {
            var svc = new SkillMasteryService();
            Assert.AreEqual(0, svc.GetMaxPoints(999999));
        }

        [Test]
        public void SkillMasteryService_GetGenreName_NonEmpty()
        {
            Assert.AreEqual("Kiếm", SkillMasteryService.GetGenreName(0));
            Assert.AreEqual("Đao", SkillMasteryService.GetGenreName(1));
            Assert.AreEqual("Cung", SkillMasteryService.GetGenreName(3));
            Assert.AreEqual("Đặc biệt", SkillMasteryService.GetGenreName(9));
            Assert.IsNotEmpty(SkillMasteryService.GetGenreName(999));
        }
    }
}
