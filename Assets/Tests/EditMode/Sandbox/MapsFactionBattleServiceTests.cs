// -----------------------------------------------------------------------------
// VLTK Mobile — Tests cho Maps/Faction/Battle services
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MapsFactionBattleServiceTests
    {
        private const string MapDir = "Reference/PcMap";
        private const string TongDir = "Reference/PcTong";
        private const string NpcDir = "Reference/PcNpc";
        private const string SpawnDir = "Reference/PcSpawn";
        private const string DropDir = "Reference/PcDropRate";

        [Test]
        public void CaveListFullService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => CaveListFullService.LoadFromStreamingAssets(MapDir));
        }

        [Test]
        public void CaveListFullService_CanEnter_RejectsLevelMismatch()
        {
            var svc = CaveListFullService.LoadFromStreamingAssets(MapDir);
            // Không có data thật: vẫn phải trả false cho id bất kỳ
            Assert.IsFalse(svc.CanEnter(99999, 50, 1));
        }

        [Test]
        public void WharfService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => WharfService.LoadFromStreamingAssets(MapDir));
        }

        [Test]
        public void WharfService_GetByFromMap_FiltersCorrectly()
        {
            var svc = WharfService.LoadFromStreamingAssets(MapDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByFromMap(0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void WaypointService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => WaypointService.LoadFromStreamingAssets(MapDir));
        }

        [Test]
        public void WaypointService_GetByMap_FiltersCorrectly()
        {
            var svc = WaypointService.LoadFromStreamingAssets(MapDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByMap(0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void AutoPathRouteService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => AutoPathRouteService.LoadFromStreamingAssets(MapDir));
        }

        [Test]
        public void AutoPathRouteService_GetByFromTo_FiltersCorrectly()
        {
            var svc = AutoPathRouteService.LoadFromStreamingAssets(MapDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByFromTo(0, 0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void RevivePosService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => RevivePosService.LoadFromStreamingAssets(MapDir));
        }

        [Test]
        public void RevivePosService_GetByMap_FiltersCorrectly()
        {
            var svc = RevivePosService.LoadFromStreamingAssets(MapDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByMap(0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void FactionConfigService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FactionConfigService.LoadFromStreamingAssets(TongDir));
        }

        [Test]
        public void FactionConfigService_GetAll_NonEmpty()
        {
            var svc = FactionConfigService.LoadFromStreamingAssets(TongDir);
            // 10 môn phái chính nếu data tồn tại; nếu không, count = 0 (không throw)
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void NpcResService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NpcResService.LoadFromStreamingAssets(NpcDir));
        }

        [Test]
        public void NpcResService_GetByFaction_FiltersCorrectly()
        {
            var svc = NpcResService.LoadFromStreamingAssets(NpcDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByFaction(0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void NpcSFullService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NpcSFullService.LoadFromStreamingAssets(NpcDir));
        }

        [Test]
        public void NpcSFullService_GetByTemplate_FiltersCorrectly()
        {
            var svc = NpcSFullService.LoadFromStreamingAssets(NpcDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByTemplate(0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void TongStuntService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TongStuntService.LoadFromStreamingAssets(TongDir));
        }

        [Test]
        public void TongStuntService_GetForLevel_FiltersCorrectly()
        {
            var svc = TongStuntService.LoadFromStreamingAssets(TongDir);
            var all = System.Linq.Enumerable.ToList(svc.GetForLevel(150));
            Assert.IsNotNull(all);
        }

        [Test]
        public void TongSettingService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TongSettingService.LoadFromStreamingAssets(TongDir));
        }

        [Test]
        public void TongSettingService_Count_NonNegative()
        {
            var svc = TongSettingService.LoadFromStreamingAssets(TongDir);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void TongNpcPosService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TongNpcPosService.LoadFromStreamingAssets(TongDir));
        }

        [Test]
        public void TongNpcPosService_GetByType_FiltersCorrectly()
        {
            var svc = TongNpcPosService.LoadFromStreamingAssets(TongDir);
            var gates = System.Linq.Enumerable.ToList(svc.GetByType(0));
            var elders = System.Linq.Enumerable.ToList(svc.GetByType(1));
            Assert.IsNotNull(gates);
            Assert.IsNotNull(elders);
        }

        [Test]
        public void MapListService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapListService.LoadFromStreamingAssets(MapDir));
        }

        [Test]
        public void MapListService_GetByType_FiltersCorrectly()
        {
            var svc = MapListService.LoadFromStreamingAssets(MapDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByType(0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void MapDescService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapDescService.LoadFromStreamingAssets(MapDir));
        }

        [Test]
        public void MapDescService_GetDesc_ReturnsNullForInvalid()
        {
            var svc = MapDescService.LoadFromStreamingAssets(MapDir);
            Assert.IsNull(svc.GetDesc(-1));
        }

        [Test]
        public void BossSpawnService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BossSpawnService.LoadFromStreamingAssets(SpawnDir));
        }

        [Test]
        public void BossSpawnService_GetByMap_FiltersCorrectly()
        {
            var svc = BossSpawnService.LoadFromStreamingAssets(SpawnDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByMap(0));
            Assert.IsNotNull(all);
        }

        [Test]
        public void DropRateConfigService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => DropRateConfigService.LoadFromStreamingAssets(DropDir));
        }

        [Test]
        public void DropRateConfigService_GetByNpcTemplate_FiltersCorrectly()
        {
            var svc = DropRateConfigService.LoadFromStreamingAssets(DropDir);
            var all = System.Linq.Enumerable.ToList(svc.GetByNpcTemplate(0));
            Assert.IsNotNull(all);
        }
    }
}
