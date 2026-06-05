using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMapRuntimeDataRegistryTests
    {
        private const string SettingsPath = "Assets/StreamingAssets/Reference/PcMap";
        private static string PcMapDir => Path.Combine(Directory.GetCurrentDirectory(), SettingsPath);

        [Test]
        public void FromBatch_IndexesWaypointScrollWharfReviveData()
        {
            var batch = PcMapDataBatchLoader.Load(PcMapDir, PcMapDir);
            var registry = PcMapRuntimeDataRegistry.FromBatch(batch);

            Assert.GreaterOrEqual(registry.WaypointCount, 200);
            Assert.GreaterOrEqual(registry.ScrollCount, 2500);
            Assert.GreaterOrEqual(registry.WharfCount, 10);
            Assert.GreaterOrEqual(registry.ReviveCount, 200);
        }

        [Test]
        public void MapManager_LoadCatalog_ExposesTravelData()
        {
            var manager = new MapManager();
            manager.LoadCatalog();

            Assert.IsNotNull(manager.TravelData);
            Assert.GreaterOrEqual(manager.TravelData.WaypointCount, 200);
            Assert.GreaterOrEqual(manager.TravelData.ScrollCount, 2500);
            Assert.GreaterOrEqual(manager.TravelData.ReviveCount, 200);
        }
    }
}
