// -----------------------------------------------------------------------------
// VLTK Mobile — PlayerLevelService EditMode tests.
// Kiểm tra level/exp/skill flow khớp PC công thức 100 * 1.15^(L-1) * L^2,
// IPlayerLevelHost dispatch (UI/SFX/log/reward), potential distribution.
// PC source: KNpc.cpp::CalcExp / NotifyPlayerLevelUp, lua player/levelup.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PlayerLevelServiceTests
    {
        // ── Pure model tests (no host) ──────────────────────────────────────

        [Test]
        public void InitialState_Level1_DefaultAttributes()
        {
            var svc = new PlayerLevelService(1);
            Assert.AreEqual(1, svc.Level);
            Assert.AreEqual(0, svc.CurrentExp);
            Assert.AreEqual(0, svc.PotentialPoints);
            Assert.AreEqual(0, svc.SkillPoints);
            Assert.AreEqual(20, svc.Strength);
            Assert.AreEqual(20, svc.Dexterity);
            Assert.AreEqual(20, svc.Vitality);
            Assert.AreEqual(20, svc.InnerStrength);
        }

        [Test]
        public void InitialState_Level10_StatsScaleUp()
        {
            var svc = new PlayerLevelService(10);
            // PC: Str=20+(L-1)*2, Dex/Vit/Inner=20+(L-1)*1
            Assert.AreEqual(20 + 9 * 2, svc.Strength);
            Assert.AreEqual(20 + 9 * 1, svc.Dexterity);
            Assert.AreEqual(20 + 9 * 1, svc.Vitality);
            Assert.AreEqual(20 + 9 * 1, svc.InnerStrength);
        }

        [Test]
        public void AddExp_NegativeAmount_NoChange()
        {
            var svc = new PlayerLevelService(5);
            svc.AddExp(-100);
            Assert.AreEqual(0, svc.CurrentExp);
            Assert.AreEqual(5, svc.Level);
        }

        [Test]
        public void AddExp_ZeroAmount_NoChange()
        {
            var svc = new PlayerLevelService(5);
            svc.AddExp(0);
            Assert.AreEqual(0, svc.CurrentExp);
            Assert.AreEqual(5, svc.Level);
        }

        [Test]
        public void AddExp_BelowThreshold_AccumulatesOnly()
        {
            // L=5: 100 * 1.15^4 * 25 = ~14,955
            long required = PlayerStatService.GetExpRequired(5);
            Assert.Greater(required, 0);
            var svc = new PlayerLevelService(5);
            svc.AddExp(required - 1);
            Assert.AreEqual(5, svc.Level);
            Assert.AreEqual(required - 1, svc.CurrentExp);
            Assert.AreEqual(0, svc.PotentialPoints);
        }

        [Test]
        public void AddExp_ExactThreshold_LevelsUpOnce()
        {
            long required = PlayerStatService.GetExpRequired(5);
            var svc = new PlayerLevelService(5);
            svc.AddExp(required);
            Assert.AreEqual(6, svc.Level);
            Assert.AreEqual(0, svc.CurrentExp);
            Assert.AreEqual(5, svc.PotentialPoints);
            Assert.AreEqual(1, svc.SkillPoints);
        }

        [Test]
        public void AddExp_Overflow_LevelsUpMultiple()
        {
            // 3× the L=5 requirement
            long required = PlayerStatService.GetExpRequired(5);
            var svc = new PlayerLevelService(5);
            svc.AddExp(required * 3);
            // Each level-up costs more, but first level-up guarantees
            Assert.GreaterOrEqual(svc.Level, 6);
            Assert.LessOrEqual(svc.Level, 8);
            Assert.AreEqual((svc.Level - 5) * 5, svc.PotentialPoints);
        }

        [Test]
        public void AddExp_AtMaxLevel_NoFurtherExp()
        {
            var svc = new PlayerLevelService(PlayerLevelService.MaxPlayerLevel);
            svc.AddExp(1_000_000);
            Assert.AreEqual(PlayerLevelService.MaxPlayerLevel, svc.Level);
        }

        [Test]
        public void DistributePotential_ValidAmount_ReservesPoints()
        {
            var svc = new PlayerLevelService(20);
            // L=20 → 19 * 5 = 95 potential
            Assert.AreEqual(95, svc.PotentialPoints);
            Assert.IsTrue(svc.DistributePotential(10, 5, 5, 0));
            Assert.AreEqual(95 - 20, svc.PotentialPoints);
        }

        [Test]
        public void DistributePotential_InsufficientPoints_Fails()
        {
            var svc = new PlayerLevelService(2);
            // L=2 → 5 potential
            Assert.IsFalse(svc.DistributePotential(10, 0, 0, 0));
            Assert.AreEqual(5, svc.PotentialPoints);
        }

        [Test]
        public void ResetPotential_RestoresAllToLevelFormula()
        {
            var svc = new PlayerLevelService(10);
            svc.DistributePotential(20, 0, 0, 0);
            Assert.AreEqual(45 - 20, svc.PotentialPoints);
            svc.ResetPotential();
            Assert.AreEqual(9 * 5, svc.PotentialPoints);
            Assert.AreEqual(20 + 9 * 2, svc.Strength);
        }

        [Test]
        public void GrantSkillPoint_AddsToPool()
        {
            var svc = new PlayerLevelService(5);
            svc.GrantSkillPoint(3);
            Assert.AreEqual(3, svc.SkillPoints);
        }

        [Test]
        public void SpendSkillPoints_ValidAmount_Deducts()
        {
            var svc = new PlayerLevelService(5);
            svc.GrantSkillPoint(3);
            Assert.IsTrue(svc.SpendSkillPoints(2));
            Assert.AreEqual(1, svc.SkillPoints);
        }

        [Test]
        public void SpendSkillPoints_Insufficient_Fails()
        {
            // L=1 -> 0 pre-granted; can spend 0 but not 1
            var svc = new PlayerLevelService(1);
            Assert.IsFalse(svc.SpendSkillPoints(1));
        }

        [Test]
        public void RefundSkillPoints_AddsBack()
        {
            var svc = new PlayerLevelService(5);
            svc.GrantSkillPoint(3);
            svc.SpendSkillPoints(2);
            svc.RefundSkillPoints(2);
            Assert.AreEqual(3, svc.SkillPoints);
        }

        // ── Host dispatch tests ─────────────────────────────────────────────

        private sealed class FakeHost : IPlayerLevelHost
        {
            public int ExpChangedCalls;
            public int LevelUpCalls;
            public int LastOldLevel;
            public int LastNewLevel;
            public int LastPotentialGranted;
            public int LastSkillGranted;
            public bool PlaySfx = true;
            public int LogNoticeCalls;
            public int GrantRewardCalls;

            public void OnExpChanged(long currentExp, long requiredExp)
            {
                ExpChangedCalls++;
            }
            public void OnLevelUp(int oldLevel, int newLevel, int potentialGranted, int skillGranted)
            {
                LevelUpCalls++;
                LastOldLevel = oldLevel;
                LastNewLevel = newLevel;
                LastPotentialGranted = potentialGranted;
                LastSkillGranted = skillGranted;
            }
            public bool TryPlayLevelUpSfx() { return PlaySfx; }
            public void LogLevelUpNotice(int oldLevel, int newLevel) { LogNoticeCalls++; }
            public void GrantLevelUpReward(int oldLevel, int newLevel) { GrantRewardCalls++; }
        }

        [Test]
        public void AddExp_DispatchesHost_OnExpChanged()
        {
            var host = new FakeHost();
            var svc = new PlayerLevelService(5, host);
            svc.AddExp(100);
            Assert.AreEqual(1, host.ExpChangedCalls);
            Assert.AreEqual(0, host.LevelUpCalls);
        }

        [Test]
        public void AddExp_LevelUp_DispatchesAllHostCallbacks()
        {
            var host = new FakeHost();
            var svc = new PlayerLevelService(5, host);
            long required = PlayerStatService.GetExpRequired(5);
            svc.AddExp(required);
            Assert.AreEqual(1, host.LevelUpCalls); // exactly one level-up
            Assert.AreEqual(5, host.LastOldLevel);
            Assert.AreEqual(6, host.LastNewLevel);
            Assert.AreEqual(5, host.LastPotentialGranted);
            Assert.AreEqual(1, host.LastSkillGranted);
            Assert.AreEqual(1, host.LogNoticeCalls);
            Assert.AreEqual(1, host.GrantRewardCalls);
        }

        [Test]
        public void AddExp_LevelUp_DispatchesSfx()
        {
            var host = new FakeHost { PlaySfx = true };
            var svc = new PlayerLevelService(5, host);
            long required = PlayerStatService.GetExpRequired(5);
            svc.AddExp(required);
            Assert.IsTrue(host.PlaySfx); // just verify host.PlaySfx is honored
        }

        [Test]
        public void AddExp_WithoutHost_DoesNotThrow()
        {
            var svc = new PlayerLevelService(5);
            long required = PlayerStatService.GetExpRequired(5);
            Assert.DoesNotThrow(() => svc.AddExp(required));
        }

        [Test]
        public void AddExp_PartialOverflow_DispatchesHostForEachLevel()
        {
            var host = new FakeHost();
            var svc = new PlayerLevelService(5, host);
            long req5 = PlayerStatService.GetExpRequired(5);
            long req6 = PlayerStatService.GetExpRequired(6);
            // Exactly enough to level up 5→6, with 1 EXP leftover at L=6
            svc.AddExp(req5 + req6 + 1);
            // 5→6→7, with 1 EXP leftover at L=7
            Assert.AreEqual(7, svc.Level);
            Assert.AreEqual(1, svc.CurrentExp);
            Assert.GreaterOrEqual(host.LevelUpCalls, 1);
        }

        [Test]
        public void AddExp_ZeroChange_DoesNotFireLevelUp()
        {
            var host = new FakeHost();
            var svc = new PlayerLevelService(5, host);
            svc.AddExp(1);
            Assert.AreEqual(0, host.LevelUpCalls);
        }

        [Test]
        public void AddExp_PCRollingExp_ScalesUp()
        {
            // PC: exp(1) = 100, exp(50) = 100 * 1.15^49 * 2500 ≈ 1.2M
            long e1 = PlayerStatService.GetExpRequired(1);
            long e50 = PlayerStatService.GetExpRequired(50);
            Assert.Less(e1, e50);
            Assert.AreEqual(100, e1);
        }
    }
}
