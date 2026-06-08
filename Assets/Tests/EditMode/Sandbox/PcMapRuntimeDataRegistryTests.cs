using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMapRuntimeDataRegistryTests
    {
        private const string SettingsPath = "Assets/StreamingAssets/Reference/PcMap";
        private const int ExpectedPcWaypointRows = 225;
        private const int ExpectedPcScrollRows = 2600;
        private const int ExpectedPcWharfRows = 11;
        private const int ExpectedPcReviveRows = 241;
        private static string PcMapDir => Path.Combine(Directory.GetCurrentDirectory(), SettingsPath);

        [Test]
        public void FromBatch_IndexesExactWaypointScrollWharfReviveData()
        {
            var batch = PcMapDataBatchLoader.Load(PcMapDir, PcMapDir);
            var registry = PcMapRuntimeDataRegistry.FromBatch(batch);

            Assert.AreEqual(ExpectedPcWaypointRows, registry.WaypointCount);
            Assert.AreEqual(ExpectedPcScrollRows, registry.ScrollCount);
            Assert.AreEqual(ExpectedPcWharfRows, registry.WharfCount);
            Assert.AreEqual(ExpectedPcReviveRows, registry.ReviveCount);
        }

        [Test]
        public void MapManager_LoadCatalog_ExposesExactTravelData()
        {
            var manager = new MapManager();
            manager.LoadCatalog();

            Assert.IsNotNull(manager.TravelData);
            Assert.AreEqual(ExpectedPcWaypointRows, manager.TravelData.WaypointCount);
            Assert.AreEqual(ExpectedPcScrollRows, manager.TravelData.ScrollCount);
            Assert.AreEqual(ExpectedPcWharfRows, manager.TravelData.WharfCount);
            Assert.AreEqual(ExpectedPcReviveRows, manager.TravelData.ReviveCount);
        }
    }
}
