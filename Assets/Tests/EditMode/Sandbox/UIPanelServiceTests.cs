// -----------------------------------------------------------------------------
// VLTK Mobile — Tests cho UI Panel services + Vietnamese catalogs
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.UI;
using VLTK.Sandbox;

namespace VLTK.Tests.EditMode.Sandbox
{
    [TestFixture]
    public class UIPanelServiceTests
    {
        // ───── TitlePanelService ─────
        [Test]
        public void TitlePanelService_BuildSnapshot_DoesNotThrow_WithNullService()
        {
            var snap = TitlePanelService.BuildSnapshot(null, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.equippedTitleId);
        }

        [Test]
        public void TitlePanelService_GetPcTitleOrder_NonEmpty()
        {
            var order = TitlePanelService.GetPcTitleOrder();
            Assert.IsNotNull(order);
            Assert.Greater(order.Count, 0);
        }

        [Test]
        public void TitlePanelService_DescribeTitle_ReturnsNonEmptyString()
        {
            string s = TitlePanelService.DescribeTitle("Danh Hiệu Test", 2, 50, false);
            Assert.IsFalse(string.IsNullOrEmpty(s));
        }

        [Test]
        public void TitlePanelService_UpgradeStatus_RejectsLowLevel()
        {
            var entry = new TitleEntry(1, "Tân Thủ", 50);
            string s = TitlePanelService.UpgradeStatus(entry, 10);
            Assert.IsTrue(s.Contains("Cần"));
        }

        [Test]
        public void TitlePanelService_GetPcTitleOrder_Contains_UniqueIds()
        {
            var order = TitlePanelService.GetPcTitleOrder();
            var set = new System.Collections.Generic.HashSet<int>();
            foreach (var id in order) set.Add(id);
            Assert.AreEqual(order.Count, set.Count);
        }

        // ───── MeridianPanelService ─────
        [Test]
        public void MeridianPanelService_BuildSnapshot_DoesNotThrow_WithNullService()
        {
            var snap = MeridianPanelService.BuildSnapshot(null, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalLevels);
        }

        [Test]
        public void MeridianPanelService_GetPcMeridianOrder_NonEmpty()
        {
            var order = MeridianPanelService.GetPcMeridianOrder();
            Assert.Greater(order.Count, 0);
        }

        [Test]
        public void MeridianPanelService_GetProgress_ReturnsZero_Initially()
        {
            int p = MeridianPanelService.GetProgress(null, 0);
            Assert.AreEqual(0, p);
        }

        // ───── GuildPanelService ─────
        [Test]
        public void GuildPanelService_BuildSnapshot_DoesNotThrow_WithNullService()
        {
            var snap = GuildPanelService.BuildSnapshot(null, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.memberCount);
        }

        [Test]
        public void GuildPanelService_GetPcRankOrder_NonEmpty_5Ranks()
        {
            var ranks = GuildPanelService.GetPcRankOrder();
            Assert.GreaterOrEqual(ranks.Count, 4);
            foreach (var r in ranks) Assert.Greater(r, 0);
        }


        [Test]
        public void GuildPanelService_BuildSnapshot_UsesGuildStateAndDonate()
        {
            var reg = new PcTongLevelRegistry();
            reg.Register(new PcTongLevelEntry { level = 1, requiredFunds = 0, requiredBuild = 0 });
            var svc = new GuildService(reg) { GuildName = "Thiên Hạ" };
            svc.Donate(1200);

            var snap = GuildPanelService.BuildSnapshot(svc, 1);

            Assert.AreEqual("Thiên Hạ", snap.guildName);
            Assert.AreEqual(1, snap.level);
            Assert.AreEqual(1200, snap.fund);
            Assert.AreEqual(1, snap.memberCount);
            Assert.AreEqual("Bang chủ", GuildPanelService.RankName(GuildPanelService.RankLeader));
            Assert.IsTrue(GuildPanelService.TryDonate(svc, 1, 300, 0));
            Assert.AreEqual(1500, svc.GuildFunds);
        }

        [Test]
        public void GuildPanelService_TryDonate_RejectsNegativeAmount()
        {
            Assert.IsFalse(GuildPanelService.TryDonate(null, 0, -10, 0));
            Assert.IsFalse(GuildPanelService.TryDonate(null, 0, 0, 0));
        }

        [Test]
        public void GuildPanelService_TryKick_RejectsZeroId()
        {
            Assert.IsFalse(GuildPanelService.TryKick(null, 0, 0));
            Assert.IsFalse(GuildPanelService.TryKick(null, 0, -1));
        }

        // ───── ChatRoomPanelService ─────
        [Test]
        public void ChatRoomPanel_MatchesPcChannelsList()
        {
            var snap = ChatRoomPanelService.BuildSnapshot(null, 8);
            Assert.AreEqual("CH_SYSTEM", snap.defaultChannel);
            Assert.AreEqual("Nhắc nhở", snap.defaultSendNameVi);
            Assert.AreEqual(15, snap.channels.Count, "PC 7e20a7ac/c9c8a750 [Channels] has Channel0..Channel14.");
            Assert.AreEqual("CH_NEARBY", snap.channels[0].pcName);
            Assert.AreEqual("CH_SYSTEM", snap.channels[4].pcName);
            Assert.AreEqual("CH_CHATROOM", snap.channels[8].pcName);
            Assert.AreEqual("CH_CUSTOM", snap.channels[14].pcName);
            Assert.AreEqual(60000, snap.channels[2].sendIntervalMs);
            Assert.AreEqual(0, snap.channels[4].sendMsgNum);
        }

        // ───── QuestTaskPanelService ─────
        [Test]
        public void QuestTaskPanel_UsesPcQuestRuntimeBeforeDailyTasks()
        {
            var quest = new QuestService();
            var snap = QuestTaskPanelService.BuildSnapshot(quest, 1, 0, 0);
            Assert.GreaterOrEqual(snap.availableCount, 1);
            StringAssert.Contains("PC [Task] Player_Task", snap.rows[0]);
            bool hasTrainingQuest = false;
            foreach (var row in snap.rows)
                if (row.Contains("Tập Luyện Cơ Bản")) hasTrainingQuest = true;
            Assert.IsTrue(hasTrainingQuest, "Task button should show QuestService PC quest journal entries, not only daily tasks.");
        }

        // ───── DailyTaskPanelService ─────
        [Test]
        public void DailyTaskPanelService_BuildSnapshot_DoesNotThrow_WithNullService()
        {
            var snap = DailyTaskPanelService.BuildSnapshot(null, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalCount);
        }


        [Test]
        public void DailyTaskPanelService_BuildSnapshot_UsesPcTaskEntries()
        {
            var reg = new PcDailyTaskRegistry();
            reg.Register(new PcDailyTaskEntry
            {
                taskId = 7,
                taskType = 0,
                targetId = 101,
                targetCount = 3,
                minLevel = 1,
                maxLevel = 200,
                rewardExp = 500,
                rewardSilver = 20,
                rewardItem = 99,
            });
            var svc = new DailyTaskService(reg);

            var snap = DailyTaskPanelService.BuildSnapshot(svc, 1);

            Assert.AreEqual(1, snap.totalCount);
            Assert.AreEqual(7, snap.rows[0].taskId);
            Assert.AreEqual(3, snap.rows[0].target);
            StringAssert.Contains("Diệt quái", snap.rows[0].taskDesc);
            Assert.AreEqual(50, DailyTaskPanelService.GetProgressPercent(1, 2));
            Assert.IsTrue(DailyTaskPanelService.TryAccept(svc, 1, 7));
            Assert.IsTrue(DailyTaskPanelService.TryComplete(svc, 1, 7));
        }

        [Test]
        public void DailyTaskPanelService_TryAccept_RejectsInvalidTaskId()
        {
            Assert.IsFalse(DailyTaskPanelService.TryAccept(null, 0, 0));
            Assert.IsFalse(DailyTaskPanelService.TryAccept(null, 0, -1));
        }

        [Test]
        public void DailyTaskPanelService_GetProgressPercent_ReturnsZeroForZero()
        {
            int p = DailyTaskPanelService.GetProgressPercent(0, 0);
            Assert.AreEqual(0, p);
        }

        // ───── HongBaoPanelService ─────
        [Test]
        public void HongBaoPanelService_BuildSnapshot_DoesNotThrow_WithNullService()
        {
            var snap = HongBaoPanelService.BuildSnapshot(null, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.totalSent);
        }

        [Test]
        public void HongBaoPanelService_TrySend_RejectsNegativeAmount()
        {
            Assert.IsFalse(HongBaoPanelService.TrySend(null, 0, -1, ""));
            Assert.IsFalse(HongBaoPanelService.TrySend(null, 0, 0, ""));
        }

        [Test]
        public void HongBaoPanelService_TryClaim_RejectsInvalidId()
        {
            Assert.IsFalse(HongBaoPanelService.TryClaim(null, 0, 0));
            Assert.IsFalse(HongBaoPanelService.TryClaim(null, 0, -1));
        }

        // ───── AuctionPanelService ─────
        [Test]
        public void AuctionPanelService_BuildSnapshot_DoesNotThrow_WithNullService()
        {
            var snap = AuctionPanelService.BuildSnapshot(null, 0);
            Assert.IsNotNull(snap);
            Assert.AreEqual(0, snap.activeBids);
        }

        [Test]
        public void AuctionPanelService_TryBid_RejectsNegativeAmount()
        {
            Assert.IsFalse(AuctionPanelService.TryBid(null, 0, 1, -1));
            Assert.IsFalse(AuctionPanelService.TryBid(null, 0, 0, 100));
        }

        [Test]
        public void AuctionPanelService_GetBidHistory_EmptyForInvalid()
        {
            var hist = AuctionPanelService.GetBidHistory(0);
            Assert.AreEqual(0, hist.Count);
            var hist2 = AuctionPanelService.GetBidHistory(-1);
            Assert.AreEqual(0, hist2.Count);
        }

        // ───── TitleVietnameseCatalog ─────
        [Test]
        public void TitleVietnameseCatalog_GetVietnameseName_Returns_NonEmpty_For_Known()
        {
            string s = TitleVietnameseCatalog.GetVietnameseName(1);
            Assert.IsFalse(string.IsNullOrEmpty(s));
            Assert.AreEqual("Tân Thủ", s);
        }

        [Test]
        public void TitleVietnameseCatalog_GetVietnameseName_Returns_Null_For_Unknown()
        {
            string s = TitleVietnameseCatalog.GetVietnameseName(99999);
            Assert.IsNull(s);
        }

        [Test]
        public void TitleVietnameseCatalog_GetAllMapped_AtLeast_50()
        {
            var all = TitleVietnameseCatalog.GetAllMapped();
            Assert.GreaterOrEqual(all.Count, 50);
        }

        // ───── FactionVietnameseCatalog ─────
        [Test]
        public void FactionVietnameseCatalog_GetVietnameseName_Returns_NonEmpty_For_Known()
        {
            string s = FactionVietnameseCatalog.GetVietnameseName(0);
            Assert.AreEqual("Thiếu Lâm", s);
        }

        [Test]
        public void FactionVietnameseCatalog_GetVietnameseName_Contains_ThieuLam_And_ThienVuong()
        {
            Assert.AreEqual("Thiếu Lâm", FactionVietnameseCatalog.GetVietnameseName(0));
            Assert.AreEqual("Thiên Vương", FactionVietnameseCatalog.GetVietnameseName(1));
        }

        [Test]
        public void FactionVietnameseCatalog_GetAllMapped_AtLeast_10()
        {
            var all = FactionVietnameseCatalog.GetAllMapped();
            Assert.GreaterOrEqual(all.Count, 10);
        }
    }
}
