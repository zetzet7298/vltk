// -----------------------------------------------------------------------------
// VLTK Mobile — UI Final Batch Panel Services tests
// Null-safe behavior tests for 8 new UI panel services.
// Vietnamese: "Boss Thế Giới", "Điểm Hoạt Động", "Lật Thẻ", "Rèn Đúc", "Gian Hàng", "Đấu Trường", "Hiệu Ứng Danh Hiệu", "Bonus Môn Phái".
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class UIFinalBatchPanelServiceTests
    {
        // ── WorldBossPanelService ─────────────────────────────────────────────
        [Test]
        public void WorldBossPanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = WorldBossPanelService.BuildSnapshot(null, 1, 2);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.activeBosses);
            Assert.AreEqual(0, snap.rows.Count);
        }

        [Test]
        public void WorldBossPanelService_GetByMap_Empty_ForNull()
        {
            var rows = WorldBossPanelService.GetByMap(null, 1);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void WorldBossPanelService_GetActive_Empty_ForNull()
        {
            var rows = WorldBossPanelService.GetActive(null, System.DateTime.Now);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void WorldBossPanelService_ComputeDps_ZeroForNull()
        {
            int dps = WorldBossPanelService.ComputeDps(null, 1, 100, 5000);
            Assert.AreEqual(0, dps);
        }

        [Test]
        public void WorldBossPanelService_GetMyRank_ZeroForNull()
        {
            int rank = WorldBossPanelService.GetMyRank(null, 1, 1);
            Assert.AreEqual(0, rank);
        }

        // ── HuoYueDuPanelService ─────────────────────────────────────────────
        [Test]
        public void HuoYueDuPanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = HuoYueDuPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalPoints);
            Assert.AreEqual(0, snap.totalToday);
        }

        [Test]
        public void HuoYueDuPanelService_GetTodayTasks_Empty_ForNull()
        {
            var rows = HuoYueDuPanelService.GetTodayTasks(null);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void HuoYueDuPanelService_GetByReward_Empty_ForNull()
        {
            var rows = HuoYueDuPanelService.GetByReward(null, 100);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        // ── FlipCardPanelService ─────────────────────────────────────────────
        [Test]
        public void FlipCardPanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = FlipCardPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalCards);
            Assert.AreEqual(0, snap.matchedPairs);
            Assert.IsFalse(snap.isComplete);
        }

        [Test]
        public void FlipCardPanelService_GetCard_Null_ForInvalid()
        {
            var card = FlipCardPanelService.GetCard(0);
            Assert.IsNull(card);
        }

        [Test]
        public void FlipCardPanelService_TryFlip_False_ForNull()
        {
            bool ok = FlipCardPanelService.TryFlip(null, 1, 1);
            Assert.IsFalse(ok);
        }

        [Test]
        public void FlipCardPanelService_GetMatchedPairs_Zero_ForNull()
        {
            int pairs = FlipCardPanelService.GetMatchedPairs(null, 1);
            Assert.AreEqual(0, pairs);
        }

        [Test]
        public void FlipCardPanelService_IsComplete_False_ForNull()
        {
            bool done = FlipCardPanelService.IsComplete(null, 1);
            Assert.IsFalse(done);
        }

        // ── FoundryPanelService ─────────────────────────────────────────────
        [Test]
        public void FoundryPanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = FoundryPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.learnedRecipes);
            Assert.AreEqual(0, snap.totalRecipes);
        }

        [Test]
        public void FoundryPanelService_CanCraft_False_ForNull()
        {
            bool can = FoundryPanelService.CanCraft(null, 1, 50, 10);
            Assert.IsFalse(can);
        }

        [Test]
        public void FoundryPanelService_TryCraft_False_ForNull()
        {
            bool ok = FoundryPanelService.TryCraft(null, 1, 1);
            Assert.IsFalse(ok);
        }

        [Test]
        public void FoundryPanelService_ComputeSuccessRate_Zero_ForNull()
        {
            float rate = FoundryPanelService.ComputeSuccessRate(null, 1, 100);
            Assert.AreEqual(0f, rate);
        }

        // ── StallBrowsePanelService ─────────────────────────────────────────
        [Test]
        public void StallBrowsePanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = StallBrowsePanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.rows.Count);
        }

        [Test]
        public void StallBrowsePanelService_GetStall_Null_ForInvalid()
        {
            var stall = StallBrowsePanelService.GetStall(0);
            Assert.IsNull(stall);
        }

        [Test]
        public void StallBrowsePanelService_TryBuyFromStall_False_ForNull()
        {
            bool ok = StallBrowsePanelService.TryBuyFromStall(null, 1, 1, 1, 1);
            Assert.IsFalse(ok);
        }

        [Test]
        public void StallBrowsePanelService_GetTotalValue_Zero_ForNull()
        {
            int v = StallBrowsePanelService.GetTotalValue(null, 1);
            Assert.AreEqual(0, v);
        }

        // ── ArenaPanelService ───────────────────────────────────────────────
        [Test]
        public void ArenaPanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = ArenaPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.currentRank);
            Assert.AreEqual(0, snap.totalWins);
        }

        [Test]
        public void ArenaPanelService_GetByType_Empty_ForNull()
        {
            var rows = ArenaPanelService.GetByType(null, 0);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ArenaPanelService_TryChallenge_False_ForNull()
        {
            bool ok = ArenaPanelService.TryChallenge(null, 1, 1);
            Assert.IsFalse(ok);
        }

        [Test]
        public void ArenaPanelService_GetMyRank_Zero_ForNull()
        {
            int rank = ArenaPanelService.GetMyRank(null, 1);
            Assert.AreEqual(0, rank);
        }

        // ── TitleEffectPanelService ─────────────────────────────────────────
        [Test]
        public void TitleEffectPanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = TitleEffectPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalEffects);
            Assert.AreEqual(0, snap.rows.Count);
        }

        [Test]
        public void TitleEffectPanelService_GetByType_Empty_ForNull()
        {
            var rows = TitleEffectPanelService.GetByType(null, 0);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void TitleEffectPanelService_ComputeTotalEffect_Zero_ForNull()
        {
            int total = TitleEffectPanelService.ComputeTotalEffect(null, 1);
            Assert.AreEqual(0, total);
        }

        [Test]
        public void TitleEffectPanelService_GetEffectTypeName_NonEmpty()
        {
            string name = TitleEffectPanelService.GetEffectTypeName(0);
            Assert.IsNotNull(name);
            Assert.IsTrue(name.Length > 0);
        }

        // ── FactionBonusPanelService ────────────────────────────────────────
        [Test]
        public void FactionBonusPanelService_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = FactionBonusPanelService.BuildSnapshot(null, 1, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalBonus);
            Assert.AreEqual(0, snap.rows.Count);
        }

        [Test]
        public void FactionBonusPanelService_GetByFaction_Empty_ForNull()
        {
            var rows = FactionBonusPanelService.GetByFaction(null, 0);
            Assert.IsNotNull(rows);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void FactionBonusPanelService_GetBonusAtLevel_Zero_ForNull()
        {
            var (hp, mp, atk, def, speed) = FactionBonusPanelService.GetBonusAtLevel(null, 0, 1);
            Assert.AreEqual(0, hp);
            Assert.AreEqual(0, mp);
            Assert.AreEqual(0, atk);
            Assert.AreEqual(0, def);
            Assert.AreEqual(0, speed);
        }
    }
}
