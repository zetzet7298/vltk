// -----------------------------------------------------------------------------
// VLTK Mobile — Battle System Service Tests
// Coverage: TongJinBattle, BangChien, BossHoangKim, TaskFlagService (PC-parity).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class TongJinBattleServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TongJinBattleService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByMap_FiltersCorrectly()
        {
            var reg = new PcTongJinBattleRegistry();
            reg.Register(new PcTongJinBattleEntry { battleId = 1, mapId = 100, minLevel = 1, maxLevel = 50 });
            reg.Register(new PcTongJinBattleEntry { battleId = 2, mapId = 200, minLevel = 50, maxLevel = 100 });
            reg.Register(new PcTongJinBattleEntry { battleId = 3, mapId = 100, minLevel = 1, maxLevel = 100 });
            var hits = reg.GetByMap(100);
            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void ComputeScore_ReturnsDiff()
        {
            var svc = new TongJinBattleService();
            Assert.AreEqual(20, svc.ComputeScore(1, 30, 10));
            Assert.AreEqual(-5, svc.ComputeScore(1, 5, 10));
            Assert.AreEqual(0, svc.ComputeScore(1, 0, 0));
        }

        [Test]
        public void GetWinner_SongWins()
        {
            var svc = new TongJinBattleService();
            Assert.AreEqual(1, svc.GetWinner(1, 50, 30));
            Assert.AreEqual(2, svc.GetWinner(1, 10, 30));
        }

        [Test]
        public void GetWinner_Draw()
        {
            var svc = new TongJinBattleService();
            Assert.AreEqual(0, svc.GetWinner(1, 25, 25));
        }
    }

    public class BangChienServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BangChienService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByMap_FiltersCorrectly()
        {
            var reg = new PcBangChienRegistry();
            reg.Register(new PcBangChienEntry { cityId = 1, mapId = 50 });
            reg.Register(new PcBangChienEntry { cityId = 2, mapId = 60 });
            reg.Register(new PcBangChienEntry { cityId = 3, mapId = 50 });
            var hits = reg.GetByMap(50);
            Assert.AreEqual(2, hits.Count);
        }

        [Test]
        public void ComputeIncome_ReturnsValue()
        {
            var reg = new PcBangChienRegistry();
            reg.Register(new PcBangChienEntry { cityId = 1, mapId = 50, income = 100 });
            var svc = new BangChienService(reg);
            Assert.AreEqual(1000L, svc.ComputeIncome(1, 10));
            Assert.AreEqual(0L, svc.ComputeIncome(2, 10));
        }
    }

    public class BossHoangKimServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BossHoangKimService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByMap_FiltersCorrectly()
        {
            var reg = new PcBossHoangKimRegistry();
            reg.Register(new PcBossHoangKimEntry { bossId = 1, mapId = 200 });
            reg.Register(new PcBossHoangKimEntry { bossId = 2, mapId = 300 });
            var hits = reg.GetByMap(200);
            Assert.AreEqual(1, hits.Count);
            Assert.AreEqual(1, hits[0].bossId);
        }

        [Test]
        public void ComputeRespawnTime_ReturnsFuture()
        {
            var reg = new PcBossHoangKimRegistry();
            reg.Register(new PcBossHoangKimEntry { bossId = 1, mapId = 200, respawnSec = 3600 });
            var svc = new BossHoangKimService(reg);
            var now = new System.DateTime(2025, 1, 1, 12, 0, 0, System.DateTimeKind.Utc);
            var respawn = svc.ComputeRespawnTime(1, now);
            Assert.AreEqual(now.AddSeconds(3600), respawn);
        }
    }

    public class TaskFlagServiceCatalogTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaskFlagService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new PcTaskFlagRegistry();
            reg.Register(new PcTaskFlagEntry { flagId = 1, taskType = 0 });
            reg.Register(new PcTaskFlagEntry { flagId = 2, taskType = 2 });
            reg.Register(new PcTaskFlagEntry { flagId = 3, taskType = 0 });
            var hits = reg.GetByType(0);
            Assert.AreEqual(2, hits.Count);
        }
    }
}
