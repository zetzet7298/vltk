// -----------------------------------------------------------------------------
// VLTK Mobile — UI Vietnamese Localization Panel Tests
// Test null-safe behavior cho 8 panel services: Mail, Ranking, Achievement,
// SignIn, Fashion, Mall, TreasureHunt, Mount.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class UIVietnameseLocalizationPanelTests
    {
        // ── MailPanelService ─────────────────────────────────────────────────
        [Test]
        public void Mail_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = MailPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.unreadCount);
            Assert.AreEqual(0, snap.totalCount);
        }

        [Test]
        public void Mail_GetUnreadCount_Zero_ForNull()
        {
            Assert.AreEqual(0, MailPanelService.GetUnreadCount(null, 1));
        }

        [Test]
        public void Mail_ComposeNewMail_Returns_NonNull()
        {
            var entry = MailPanelService.ComposeNewMail(1, 2, "Tiêu đề", "Nội dung");
            Assert.IsNotNull(entry);
            Assert.AreEqual(1, entry.senderId);
            Assert.AreEqual(2, entry.receiverId);
        }

        // ── RankingPanelService ─────────────────────────────────────────────
        [Test]
        public void Ranking_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = RankingPanelService.BuildSnapshot(null, 1, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.myRank);
        }

        [Test]
        public void Ranking_GetMyRank_Zero_ForNull()
        {
            Assert.AreEqual(0, RankingPanelService.GetMyRank(null, 1, 0));
        }

        [Test]
        public void Ranking_GetTopN_Empty_ForNull()
        {
            var top = RankingPanelService.GetTopN(null, 0, 10);
            Assert.IsNotNull(top);
            Assert.AreEqual(0, top.Count);
        }

        [Test]
        public void Ranking_GetRankingTypeName_NonEmpty()
        {
            Assert.AreEqual("Cấp", RankingPanelService.GetRankingTypeName(0));
            Assert.AreEqual("Tài Phú", RankingPanelService.GetRankingTypeName(1));
            Assert.AreEqual("Giết Người", RankingPanelService.GetRankingTypeName(2));
            Assert.AreEqual("Môn Phái", RankingPanelService.GetRankingTypeName(3));
        }

        // ── AchievementPanelService ─────────────────────────────────────────
        [Test]
        public void Achievement_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = AchievementPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalAchievements);
        }

        [Test]
        public void Achievement_GetByCategory_Empty_ForNull()
        {
            var list = AchievementPanelService.GetByCategory(null, 1);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Achievement_GetProgress_Zero_ForNull()
        {
            Assert.AreEqual(0f, AchievementPanelService.GetProgress(null, 1));
        }

        // ── SignInPanelService ──────────────────────────────────────────────
        [Test]
        public void SignIn_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = SignInPanelService.BuildSnapshot(null, 1, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalDays);
        }

        [Test]
        public void SignIn_GetStreak_Zero_ForNull()
        {
            Assert.AreEqual(0, SignInPanelService.GetStreak(null, 1));
        }

        [Test]
        public void SignIn_GetTodayReward_Null_ForNull()
        {
            var row = SignInPanelService.GetTodayReward(null, 1);
            Assert.AreEqual(0, row.dayIdx);
        }

        // ── FashionPanelService ─────────────────────────────────────────────
        [Test]
        public void Fashion_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = FashionPanelService.BuildSnapshot(null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalFashions);
        }

        [Test]
        public void Fashion_GetBySlot_Empty_ForNull()
        {
            var list = FashionPanelService.GetBySlot(null, 0);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Fashion_CanEquip_False_ForNull()
        {
            Assert.IsFalse(FashionPanelService.CanEquip(null, 1, 50, 1));
        }

        // ── MallPanelService ────────────────────────────────────────────────
        [Test]
        public void Mall_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = MallPanelService.BuildSnapshot(null, 1, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalItems);
        }


        [Test]
        public void Mall_BuildSnapshot_UsesMallEntries()
        {
            var reg = new PcMallRegistry();
            reg.Register(new PcMallEntry { mallItemId = 10, itemId = 99, price = 1000, currency = MallService.CurrencyGold, discount = 20, requiredVipLevel = 0, stock = 5, maxBuyPerDay = 2 });
            var svc = new MallService(reg);

            var snap = MallPanelService.BuildSnapshot(svc, 1, 0);

            Assert.AreEqual(1, snap.totalItems);
            Assert.AreEqual(1, snap.availableItems);
            Assert.AreEqual(10, snap.rows[0].mallItemId);
            Assert.AreEqual(800, snap.rows[0].effectivePrice);
            Assert.AreEqual("Vàng", snap.rows[0].currency);
            Assert.IsTrue(MallPanelService.TryBuy(svc, 1, 10, 0));
        }

        [Test]
        public void Mall_GetForVip_Empty_ForNull()
        {
            var list = MallPanelService.GetForVip(null, 5);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Mall_GetOnSale_Empty_ForNull()
        {
            var list = MallPanelService.GetOnSale(null, 0);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Mall_GetEffectivePrice_Zero_ForNull()
        {
            Assert.AreEqual(0, MallPanelService.GetEffectivePrice(null, 1, 0));
        }

        // ── TreasureHuntPanelService ────────────────────────────────────────
        [Test]
        public void Treasure_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = TreasureHuntPanelService.BuildSnapshot(null, 1, 1, 0, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalTreasures);
        }


        [Test]
        public void Treasure_BuildSnapshot_UsesMapAndDistance()
        {
            var reg = new PcTreasureHuntRegistry();
            reg.Register(new PcTreasureHuntEntry { treasureId = 3, mapId = 907, posX = 10, posY = 0, itemId = 88, itemCount = 2, requiredLevel = 1, detectionRange = 20 });
            var svc = new TreasureHuntService(reg);

            var snap = TreasureHuntPanelService.BuildSnapshot(svc, 1, 907, 0, 0);

            Assert.AreEqual(1, snap.totalTreasures);
            Assert.AreEqual(1, snap.nearbyTreasures);
            Assert.AreEqual(3, snap.rows[0].treasureId);
            Assert.AreEqual(10f, snap.rows[0].distance, 0.001f);
            Assert.IsTrue(TreasureHuntPanelService.TryDig(svc, 1, 3));
        }

        [Test]
        public void Treasure_GetNearby_Empty_ForNull()
        {
            var list = TreasureHuntPanelService.GetNearby(null, 1, 0, 0);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        // ── MountPanelService ───────────────────────────────────────────────
        [Test]
        public void Mount_BuildSnapshot_DoesNotThrow_Null()
        {
            var snap = MountPanelService.BuildSnapshot(null, null, 1);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.currentSpeed);
        }

        [Test]
        public void Mount_GetOwnedMounts_Empty_ForNull()
        {
            var list = MountPanelService.GetOwnedMounts(null, 1);
            Assert.IsNotNull(list);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void Mount_TryMount_False_ForNull()
        {
            Assert.IsFalse(MountPanelService.TryMount(null, 1, 1));
        }

        [Test]
        public void Mount_TryDismount_False_ForNull()
        {
            Assert.IsFalse(MountPanelService.TryDismount(null, 1));
        }

        [Test]
        public void Mount_GetMountSpeed_Zero_ForNull()
        {
            Assert.AreEqual(0, MountPanelService.GetMountSpeed(null, 1));
        }
    }
}
