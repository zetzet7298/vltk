// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests for Mail/Mount/Ranking/Friend registries
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MountMailRankingPetParserTests
    {
        // ========== PcMailRegistry ==========

        [Test]
        public void PcMail_Count_NonNegative()
        {
            var reg = new PcMailRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMail_Get_ReturnsNullForInvalid()
        {
            var reg = new PcMailRegistry();
            Assert.IsNull(reg.Get(99999));
        }

        [Test]
        public void PcMail_Register_AddsEntry()
        {
            var reg = new PcMailRegistry();
            var e = new PcMailEntry { templateId = 1, titleTemplate = "Xin chào", senderName = "Hệ Thống" };
            reg.Register(e);
            Assert.AreEqual(1, reg.Count);
            Assert.AreEqual("Xin chào", reg.Get(1).titleTemplate);
        }

        // ========== PcMountRegistry ==========

        [Test]
        public void PcMount_Count_NonNegative()
        {
            var reg = new PcMountRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMount_GetByLevel_FiltersCorrectly()
        {
            var reg = new PcMountRegistry();
            reg.Register(new PcMountEntry { mountId = 1, name = "A", requiredLevel = 10, speed = 50 });
            reg.Register(new PcMountEntry { mountId = 2, name = "B", requiredLevel = 30, speed = 80 });
            reg.Register(new PcMountEntry { mountId = 3, name = "C", requiredLevel = 60, speed = 100 });

            var at20 = reg.GetByLevel(20);
            Assert.AreEqual(1, at20.Count);
            Assert.AreEqual(1, at20[0].mountId);

            var at100 = reg.GetByLevel(100);
            Assert.AreEqual(3, at100.Count);
        }

        [Test]
        public void PcMount_Get_ReturnsCorrect()
        {
            var reg = new PcMountRegistry();
            reg.Register(new PcMountEntry { mountId = 5, name = "Test", speed = 100 });
            var m = reg.Get(5);
            Assert.IsNotNull(m);
            Assert.AreEqual(100, m.speed);
        }

        // ========== PcRankingRegistry ==========

        [Test]
        public void PcRanking_Count_NonNegative()
        {
            var reg = new PcRankingRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcRanking_GetByType_FiltersCorrectly()
        {
            var reg = new PcRankingRegistry();
            reg.Register(new PcRankingEntry { rankId = 1, playerId = 1, rankType = 0, score = 50 });
            reg.Register(new PcRankingEntry { rankId = 2, playerId = 2, rankType = 1, score = 100 });
            reg.Register(new PcRankingEntry { rankId = 3, playerId = 3, rankType = 0, score = 80 });

            var byLevel = reg.GetByType(0);
            Assert.AreEqual(2, byLevel.Count);
            var byGold = reg.GetByType(1);
            Assert.AreEqual(1, byGold.Count);
        }

        [Test]
        public void PcRanking_GetTop_SortsByScoreDesc()
        {
            var reg = new PcRankingRegistry();
            reg.Register(new PcRankingEntry { rankId = 1, playerId = 1, rankType = 0, score = 50 });
            reg.Register(new PcRankingEntry { rankId = 2, playerId = 2, rankType = 0, score = 100 });
            reg.Register(new PcRankingEntry { rankId = 3, playerId = 3, rankType = 0, score = 75 });

            var top = reg.GetTop(2);
            Assert.AreEqual(2, top.Count);
            Assert.AreEqual(100, top[0].score);
            Assert.AreEqual(75, top[1].score);
        }

        // ========== PcFriendRegistry ==========

        [Test]
        public void PcFriend_Count_NonNegative()
        {
            var reg = new PcFriendRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcFriend_GetByPlayer_FiltersCorrectly()
        {
            var reg = new PcFriendRegistry();
            reg.Register(new PcFriendEntry { friendId = 1, playerId = 1, friendPlayerId = 100, intimacy = 50 });
            reg.Register(new PcFriendEntry { friendId = 2, playerId = 1, friendPlayerId = 200, intimacy = 100 });
            reg.Register(new PcFriendEntry { friendId = 3, playerId = 2, friendPlayerId = 300, intimacy = 75 });

            var p1Friends = reg.GetByPlayer(1);
            Assert.AreEqual(2, p1Friends.Count);
            var p2Friends = reg.GetByPlayer(2);
            Assert.AreEqual(1, p2Friends.Count);
        }

        [Test]
        public void PcFriend_Get_ReturnsCorrect()
        {
            var reg = new PcFriendRegistry();
            reg.Register(new PcFriendEntry { friendId = 10, playerId = 5, friendPlayerId = 50, intimacy = 25 });
            var f = reg.Get(10);
            Assert.IsNotNull(f);
            Assert.AreEqual(50, f.friendPlayerId);
        }
    }
}
