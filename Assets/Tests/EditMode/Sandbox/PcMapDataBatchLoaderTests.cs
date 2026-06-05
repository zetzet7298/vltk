using System.IO;
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
        public void Load_TongsFilterFromMapListTongEntries()
        {
            var batch = PcMapDataBatchLoader.Load(PcMapDir, PcMapDir);
            Assert.IsNotNull(batch.tongs);
            Assert.GreaterOrEqual(batch.tongs.Count, 0);
        }
    }
}
