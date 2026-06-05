// -----------------------------------------------------------------------------
// VLTK Mobile — Map data services tests
// Verifies LoadFromStreamingAssets, basic queries, Vietnamese type names.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MapDataServiceTests
    {
        [Test]
        public void MapListFullService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapListFullService.LoadFromStreamingAssets());
        }

        [Test]
        public void MapListFullService_GetCities_NonNull()
        {
            var svc = new MapListFullService();
            var cities = svc.GetCities();
            Assert.IsNotNull(cities);
        }

        [Test]
        public void MapListFullService_GetMapTypeName_NonEmpty_ForValid()
        {
            var svc = new MapListFullService();
            string cityName = svc.GetMapTypeName(PcMapListFullParser.TypeCity);
            string caveName = svc.GetMapTypeName(PcMapListFullParser.TypeCave);
            Assert.IsNotEmpty(cityName);
            Assert.IsNotEmpty(caveName);
        }

        [Test]
        public void MapListFullService_GetMapTypeName_UnknownFallsBackToOther()
        {
            var svc = new MapListFullService();
            string unknown = svc.GetMapTypeName(999);
            Assert.AreEqual("Khác", unknown);
        }

        [Test]
        public void MapListFullService_SearchByName_ReturnsMatches()
        {
            var svc = new MapListFullService();
            var results = svc.SearchByName("Thành");
            Assert.IsNotNull(results);
        }

        [Test]
        public void MapListFullService_SearchByName_EmptyInput_ReturnsEmpty()
        {
            var svc = new MapListFullService();
            var results = svc.SearchByName(string.Empty);
            Assert.AreEqual(0, results.Count);
        }

        [Test]
        public void MapListFullService_IsBattlefield_RejectsUnknown()
        {
            var svc = new MapListFullService();
            Assert.IsFalse(svc.IsBattlefield(99999));
        }

        [Test]
        public void MapListFullService_IsCity_RejectsUnknown()
        {
            var svc = new MapListFullService();
            Assert.IsFalse(svc.IsCity(99999));
        }

        [Test]
        public void MapElementService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapElementService.LoadFromStreamingAssets());
        }

        [Test]
        public void MapElementService_GetDominantElement_ReturnsMinusOne_ForUnknown()
        {
            var svc = new MapElementService();
            int result = svc.GetDominantElement(99999);
            Assert.AreEqual(-1, result);
        }

        [Test]
        public void MapElementService_GetElementalAdvantage_ReturnsZero_ForSameElement()
        {
            var svc = new MapElementService();
            Assert.AreEqual(0, svc.GetElementalAdvantage(0, 0));
            Assert.AreEqual(0, svc.GetElementalAdvantage(1, 1));
            Assert.AreEqual(0, svc.GetElementalAdvantage(4, 4));
        }

        [Test]
        public void MapElementService_GetElementalAdvantage_KimKhacMoc_ReturnsOne()
        {
            var svc = new MapElementService();
            Assert.AreEqual(1, svc.GetElementalAdvantage(0, 1));
        }

        [Test]
        public void MapElementService_GetElementalAdvantage_HoaKhacKim_ReturnsOne()
        {
            var svc = new MapElementService();
            Assert.AreEqual(1, svc.GetElementalAdvantage(3, 0));
        }

        [Test]
        public void MapElementService_GetElementalAdvantage_MocBiKho_ReturnsTwo()
        {
            var svc = new MapElementService();
            Assert.AreEqual(2, svc.GetElementalAdvantage(1, 0));
        }

        [Test]
        public void MapRespawnService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapRespawnService.LoadFromStreamingAssets());
        }

        [Test]
        public void MapRespawnService_GetRespawnPoints_NonNull()
        {
            var svc = new MapRespawnService();
            var list = svc.GetRespawnPoints(99999);
            Assert.IsNotNull(list);
        }

        [Test]
        public void MapRespawnService_GetTownRespawn_NullForUnknown()
        {
            var svc = new MapRespawnService();
            var entry = svc.GetTownRespawn(99999);
            Assert.IsNull(entry);
        }

        [Test]
        public void MapBlockService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapBlockService.LoadFromStreamingAssets());
        }

        [Test]
        public void MapBlockService_GetBlocks_NonNull()
        {
            var svc = new MapBlockService();
            var list = svc.GetBlocks(99999);
            Assert.IsNotNull(list);
        }

        [Test]
        public void MapBlockService_CountBlocksByType_ReturnsDictionary()
        {
            var svc = new MapBlockService();
            var dict = svc.CountBlocksByType(99999);
            Assert.IsNotNull(dict);
        }

        [Test]
        public void MapBlockService_IsPassable_ReturnsTrue_WhenNoBlocks()
        {
            var svc = new MapBlockService();
            Assert.IsTrue(svc.IsPassable(99999, 100, 100));
        }

        [Test]
        public void MapNpcRespawnService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapNpcRespawnService.LoadFromStreamingAssets());
        }

        [Test]
        public void MapNpcRespawnService_GetRespawns_NonNull()
        {
            var svc = new MapNpcRespawnService();
            var list = svc.GetRespawns(99999);
            Assert.IsNotNull(list);
        }

        [Test]
        public void MapNpcRespawnService_GetGroupRespawns_NonNull()
        {
            var svc = new MapNpcRespawnService();
            var list = svc.GetGroupRespawns(99999, 1);
            Assert.IsNotNull(list);
        }

        [Test]
        public void MapMusicService_LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MapMusicService.LoadFromStreamingAssets());
        }

        [Test]
        public void MapMusicService_GetDayMusic_ZeroForUnknown()
        {
            var svc = new MapMusicService();
            Assert.AreEqual(0, svc.GetDayMusic(99999));
        }

        [Test]
        public void MapMusicService_GetNightMusic_ZeroForUnknown()
        {
            var svc = new MapMusicService();
            Assert.AreEqual(0, svc.GetNightMusic(99999));
        }

        [Test]
        public void MapMusicService_GetBattleMusic_ZeroForUnknown()
        {
            var svc = new MapMusicService();
            Assert.AreEqual(0, svc.GetBattleMusic(99999));
        }

        [Test]
        public void MapMusicService_Get_NullForUnknown()
        {
            var svc = new MapMusicService();
            Assert.IsNull(svc.Get(99999));
        }
    }
}
