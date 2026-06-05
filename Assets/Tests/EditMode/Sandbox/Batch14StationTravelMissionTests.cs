// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Batch 14: Station/Travel, Guild Workshop/Task, Mission Config
// Vietnamese: Kiểm thử dịch vụ trạm bến, công xưởng bang, nhiệm vụ đấu trường/mê cung
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class StationServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => StationService.LoadFromStreamingAssets());
            var svc = StationService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetStation_ReturnsNull_ForInvalidId()
        {
            var svc = StationService.LoadFromStreamingAssets();
            var s = svc.GetStation(-1);
            Assert.IsNull(s);
        }
    }

    public class StationPriceServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => StationPriceService.LoadFromStreamingAssets());
            var svc = StationPriceService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class WaypointPriceServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => WaypointPriceService.LoadFromStreamingAssets());
            var svc = WaypointPriceService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class GuildWorkshopLevelServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GuildWorkshopLevelService.LoadFromStreamingAssets());
            var svc = GuildWorkshopLevelService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetWorkshop_ReturnsNull_ForInvalidType()
        {
            var svc = GuildWorkshopLevelService.LoadFromStreamingAssets();
            var w = svc.GetWorkshop(-1);
            Assert.IsNull(w);
        }
    }

    public class GuildTaskDefServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GuildTaskDefService.LoadFromStreamingAssets());
            var svc = GuildTaskDefService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MissionArenaConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MissionArenaConfigService.LoadFromStreamingAssets());
            var svc = MissionArenaConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MissionBattleConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MissionBattleConfigService.LoadFromStreamingAssets());
            var svc = MissionBattleConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MissionMazeConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MissionMazeConfigService.LoadFromStreamingAssets());
            var svc = MissionMazeConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MissionQianchongServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MissionQianchongService.LoadFromStreamingAssets());
            var svc = MissionQianchongService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }
}
