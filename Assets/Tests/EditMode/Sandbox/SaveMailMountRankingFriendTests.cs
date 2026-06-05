// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests for Save/Mail/Mount/Ranking/Friend services
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class SaveMailMountRankingFriendTests
    {
        // ========== SaveSlotService ==========

        [Test]
        public void SaveSlot_LoadFromStreamingAssets_MatchesCommittedData()
        {
            var svc = PcSaveSlotService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void SaveSlot_GetAllSlots_Initially_Empty()
        {
            var svc = new PcSaveSlotService();
            Assert.AreEqual(0, svc.GetAllSlots().Count);
        }

        [Test]
        public void SaveSlot_SaveGame_AddsToSlots()
        {
            var svc = new PcSaveSlotService();
            var snap = new PlayerSnapshot
            {
                playerName = "TestPlayer",
                playerLevel = 50,
                mapId = 100,
                playTimeSec = 3600,
                faction = 5,
                gold = 9999,
            };
            bool ok = svc.SaveGame(0, snap);
            Assert.IsTrue(ok);
            Assert.AreEqual(1, svc.GetAllSlots().Count);
            var loaded = svc.LoadGame(0);
            Assert.IsNotNull(loaded);
            Assert.AreEqual(50, loaded.playerLevel);
        }

        [Test]
        public void SaveSlot_DeleteSave_RemovesFromSlots()
        {
            var svc = new PcSaveSlotService();
            var snap = new PlayerSnapshot { playerName = "X", playerLevel = 10 };
            svc.SaveGame(0, snap);
            Assert.AreEqual(1, svc.GetAllSlots().Count);
            bool ok = svc.DeleteSave(0);
            Assert.IsTrue(ok);
            Assert.AreEqual(0, svc.GetAllSlots().Count);
        }

        [Test]
        public void SaveSlot_AutoSave_Works()
        {
            var svc = new PcSaveSlotService();
            var snap = new PlayerSnapshot { playerName = "Auto", playerLevel = 20 };
            svc.AutoSave(snap);
            Assert.IsTrue(svc.HasAutoSave);
            var loaded = svc.LoadAutoSave();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(20, loaded.playerLevel);
        }

        // ========== MailService ==========

        [Test]
        public void Mail_LoadFromStreamingAssets_MatchesCommittedData()
        {
            var svc = MailService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Mail_SendMail_ReturnsNonZeroId()
        {
            var svc = new MailService();
            int id = svc.SendMail(1, 2, "Hi", "Body");
            Assert.Greater(id, 0);
        }

        [Test]
        public void Mail_GetMails_ContainsSent()
        {
            var svc = new MailService();
            int id = svc.SendMail(1, 100, "Test", "Body");
            var mails = svc.GetMails(100);
            Assert.AreEqual(1, mails.Count);
            Assert.AreEqual(id, mails[0].mailId);
        }

        [Test]
        public void Mail_MarkRead_SetsTrue()
        {
            var svc = new MailService();
            int id = svc.SendMail(1, 2, "T", "B");
            bool ok = svc.MarkRead(id);
            Assert.IsTrue(ok);
            var mails = svc.GetMails(2);
            Assert.IsTrue(mails[0].isRead);
        }

        [Test]
        public void Mail_ClaimMail_SetsTrue()
        {
            var svc = new MailService();
            int id = svc.SendMail(1, 2, "T", "B", itemId: 100, itemCount: 1, gold: 500);
            bool ok = svc.ClaimMail(id);
            Assert.IsTrue(ok);
            var mails = svc.GetMails(2);
            Assert.IsTrue(mails[0].isClaimed);
        }

        [Test]
        public void Mail_DeleteMail_RemovesFromList()
        {
            var svc = new MailService();
            int id = svc.SendMail(1, 2, "T", "B");
            bool ok = svc.DeleteMail(id);
            Assert.IsTrue(ok);
            Assert.AreEqual(0, svc.GetMails(2).Count);
        }

        [Test]
        public void Mail_GetUnreadCount_Zero_Initially()
        {
            var svc = new MailService();
            int n = svc.GetUnreadCount(999);
            Assert.AreEqual(0, n);
        }

        [Test]
        public void Mail_ClaimAll_Works()
        {
            var svc = new MailService();
            svc.SendMail(1, 2, "A", "a", itemId: 1, itemCount: 1);
            svc.SendMail(1, 2, "B", "b", gold: 100);
            int claimed = svc.ClaimAll(2);
            Assert.AreEqual(2, claimed);
        }

        // ========== MountService ==========

        [Test]
        public void Mount_LoadFromStreamingAssets_MatchesCommittedData()
        {
            var svc = MountService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Mount_TryMount_RejectsInvalidHorse()
        {
            var svc = new MountService();
            bool ok = svc.TryMount(1, 99999); // invalid id
            Assert.IsFalse(ok);
            Assert.IsNull(svc.GetActiveMount(1));
        }

        [Test]
        public void Mount_TryDismount_ResetsActive()
        {
            var svc = new MountService();
            // tạo mount giả để test dismount
            var reg = new PcMountRegistry();
            var entry = new PcMountEntry { mountId = 1, name = "TestHorse", speed = 100, staminaCost = 5, requiredLevel = 1 };
            reg.Register(entry);
            svc.AttachRegistry(reg);
            svc.TryMount(1, 1);
            Assert.IsNotNull(svc.GetActiveMount(1));
            bool ok = svc.TryDismount(1);
            Assert.IsTrue(ok);
            Assert.IsNull(svc.GetActiveMount(1));
        }

        [Test]
        public void Mount_GetMountSpeed_Zero_WhenNotMounted()
        {
            var svc = new MountService();
            int speed = svc.GetMountSpeed(99);
            Assert.AreEqual(0, speed);
        }

        [Test]
        public void Mount_TryFeed_RestoresStamina()
        {
            var svc = new MountService();
            var reg = new PcMountRegistry();
            reg.Register(new PcMountEntry { mountId = 1, name = "H", speed = 50, requiredLevel = 1 });
            svc.AttachRegistry(reg);
            svc.TryMount(1, 1);
            bool ok = svc.TryFeed(1, 100);
            Assert.IsTrue(ok);
            Assert.Greater(svc.GetStamina(1), 0);
        }

        // ========== RankingService ==========

        [Test]
        public void Ranking_LoadFromStreamingAssets_MatchesCommittedData()
        {
            var svc = RankingService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Ranking_GetPlayerRank_Zero_Initially()
        {
            var svc = new RankingService();
            int rank = svc.GetPlayerRank(999, RankingType.Level);
            Assert.AreEqual(0, rank);
        }

        [Test]
        public void Ranking_GetTopPlayers_LimitRespected()
        {
            var svc = new RankingService();
            for (int i = 0; i < 20; i++) svc.UpdateScore(i, RankingType.Level, 100 - i);
            var top = svc.GetTopPlayers(5, RankingType.Level);
            Assert.AreEqual(5, top.Count);
        }

        [Test]
        public void Ranking_GetFactionRank_Zero_Initially()
        {
            var svc = new RankingService();
            int rank = svc.GetFactionRank(5);
            Assert.AreEqual(0, rank);
        }

        [Test]
        public void Ranking_UpdateScore_AddsPlayer()
        {
            var svc = new RankingService();
            bool ok = svc.UpdateScore(1, RankingType.Level, 999);
            Assert.IsTrue(ok);
            int rank = svc.GetPlayerRank(1, RankingType.Level);
            Assert.Greater(rank, 0);
        }

        // ========== FriendService ==========

        [Test]
        public void Friend_LoadFromStreamingAssets_MatchesCommittedData()
        {
            var svc = FriendService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Friend_AddFriend_AddsToList()
        {
            var svc = new FriendService();
            bool ok = svc.AddFriend(1, 100);
            Assert.IsTrue(ok);
            var friends = svc.GetFriends(1);
            Assert.AreEqual(1, friends.Count);
        }

        [Test]
        public void Friend_RemoveFriend_RemovesFromList()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 100);
            bool ok = svc.RemoveFriend(1, 100);
            Assert.IsTrue(ok);
            Assert.AreEqual(0, svc.GetFriends(1).Count);
        }

        [Test]
        public void Friend_GetFriends_ContainsAdded()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 100);
            svc.AddFriend(1, 200);
            var friends = svc.GetFriends(1);
            Assert.AreEqual(2, friends.Count);
        }

        [Test]
        public void Friend_GetOnlineFriends_OnlyOnline()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 100);
            var friends = svc.GetFriends(1);
            friends[0].isOnline = true;
            var online = svc.GetOnlineFriends(1);
            Assert.AreEqual(1, online.Count);
        }

        [Test]
        public void Friend_AddIntimacy_IncreasesValue()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 100);
            int newVal = svc.AddIntimacy(1, 100, 50);
            Assert.AreEqual(50, newVal);
            int newVal2 = svc.AddIntimacy(1, 100, 30);
            Assert.AreEqual(80, newVal2);
        }

        [Test]
        public void Friend_GetBestFriends_SortedByIntimacy()
        {
            var svc = new FriendService();
            svc.AddFriend(1, 100);
            svc.AddFriend(1, 200);
            svc.AddFriend(1, 300);
            svc.AddIntimacy(1, 100, 50);
            svc.AddIntimacy(1, 200, 200);
            svc.AddIntimacy(1, 300, 10);
            var best = svc.GetBestFriends(1, 3);
            Assert.AreEqual(3, best.Count);
            Assert.AreEqual(200, best[0].friendPlayerId); // cao nhất trước
            Assert.AreEqual(100, best[1].friendPlayerId);
            Assert.AreEqual(300, best[2].friendPlayerId);
        }

        [Test]
        public void Friend_SendMessage_Works()
        {
            var svc = new FriendService();
            bool ok = svc.SendMessage(1, 100, "Hello!");
            Assert.IsTrue(ok);
            var msgs = svc.GetMessages(100);
            Assert.AreEqual(1, msgs.Count);
            Assert.IsTrue(msgs[0].Contains("Hello!"));
        }
    }
}
