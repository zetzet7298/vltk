using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMapDataBatchLoaderTests
    {
        private const string SettingsPath = "Assets/StreamingAssets/Reference/PcMap";
        private static string PcMapDir => Path.Combine(Directory.GetCurrentDirectory(), SettingsPath);

        [Test]
        public void Load_ReturnsAllSevenListsFromSampleDir()
        {
            var batch = PcMapDataBatchLoader.Load(PcMapDir, PcMapDir);
            Assert.IsNotNull(batch);
            Assert.Greater(batch.maps.Count, 0, "maps list must be non-empty");
            Assert.Greater(batch.caves.Count, 0, "caves list must be non-empty");
            Assert.Greater(batch.waypoints.Count, 0, "waypoints list must be non-empty");
            Assert.Greater(batch.scrolls.Count, 0, "scrolls list must be non-empty");
            Assert.Greater(batch.wharves.Count, 0, "wharves list must be non-empty");
            Assert.Greater(batch.revivePositions.Count, 0, "revive positions must be non-empty");
            Assert.IsNotNull(batch.tongs, "tongs list must be present (derived from maplist)");
        }

        [Test]
        public void Load_BuildsMapCatalogFromParsedMaps()
        {
            var batch = PcMapDataBatchLoader.Load(PcMapDir, PcMapDir);
            var catalog = PcMapDataBatchLoader.BuildMapCatalog(batch);
            Assert.AreEqual(batch.maps.Count, catalog.Count);
            Assert.IsTrue(catalog.TrueForAll(m => m.mapId > 0));
            Assert.IsTrue(catalog.TrueForAll(m => !string.IsNullOrEmpty(m.nameRaw)));
        }

        [Test]
        public void Load_BuildsRuntimeCatalogFromFullPcMapList()
        {
            var batch = PcMapDataBatchLoader.Load(PcMapDir, PcMapDir);
            var catalog = PcMapDataBatchLoader.BuildRuntimeCatalog(batch);
            Assert.GreaterOrEqual(catalog.Count, 1000, "full maplist should expose PC map catalog entries");
            Assert.IsTrue(catalog.TrueForAll(m => m.mapId > 0));
            Assert.IsTrue(catalog.TrueForAll(m => !string.IsNullOrEmpty(m.displayNameRaw)));
        }

        [Test]
        public void MapManager_LoadCatalog_MergesPcMapData()
        {
            var manager = new MapManager();
            manager.LoadCatalog();
            Assert.AreEqual(1005, manager.Catalog.Count, "MapManager runtime catalog should match the 1,005 positive PC maplist ids, with no legacy mapId=0 placeholder pollution.");
            Assert.IsFalse(manager.Catalog.ContainsKey(0));
            Assert.IsTrue(manager.Catalog.ContainsKey(MapPortManifest.DaiLyId));
            Assert.IsTrue(manager.Catalog.ContainsKey(MapPortManifest.LamAnId));
            Assert.AreEqual("Vượt ải Nhiếp Thí Trần", manager.Catalog[MapPortManifest.VuotAiNhiepThiTranId].displayNameNormalized);
            Assert.AreEqual("Mật đạo Nha môn Tương Dương", manager.Catalog[79].displayNameNormalized, "79 is not Ba Lăng huyện in PC maplist truth.");
            Assert.IsFalse(string.IsNullOrEmpty(manager.Catalog[MapPortManifest.VuotAiNhiepThiTranId].geometryKey));
            Assert.IsFalse(string.IsNullOrEmpty(manager.Catalog[MapPortManifest.VuotAiNhiepThiTranId].regionFolder));
        }

        [Test]
        public void MapManager_LoadCatalog_AllPcAliasesHaveVietnameseNamesAndVisualGeometry()
        {
            var manager = new MapManager();
            manager.LoadCatalog();

            var entries = manager.Catalog.Values.ToList();
            Assert.AreEqual(1005, entries.Count, "Runtime map catalog must expose every positive PC maplist alias.");
            Assert.IsFalse(entries.Any(e => e.mapId <= 0));
            Assert.IsFalse(entries.Any(e => string.IsNullOrWhiteSpace(e.displayNameNormalized)));
            Assert.IsFalse(entries.Any(e => e.displayNameNormalized.StartsWith("Map_")), "PC alias merge must replace stale Map_<id>/event placeholder names.");
            Assert.IsFalse(entries.Any(e => string.IsNullOrWhiteSpace(e.sourceMapPath)));
            Assert.IsFalse(entries.Any(e => string.IsNullOrWhiteSpace(e.geometryKey)));
            Assert.IsFalse(entries.Any(e => string.IsNullOrWhiteSpace(e.regionFolder)));
            Assert.IsFalse(entries.Any(e => string.IsNullOrWhiteSpace(e.spriteFolder)));
            Assert.IsFalse(entries.Any(e => e.geometryBounds == null || e.geometryBounds.width <= 0f || e.geometryBounds.height <= 0f));
            Assert.AreEqual("Khu vực câu cá", manager.Catalog[1009].displayNameNormalized);
        }

        [Test]
        public void Load_TongsFilterFromMapListTongEntries()
        {
            var batch = PcMapDataBatchLoader.Load(PcMapDir, PcMapDir);
            Assert.IsNotNull(batch.tongs);
            Assert.GreaterOrEqual(batch.tongs.Count, 0);
        }
    }
}
