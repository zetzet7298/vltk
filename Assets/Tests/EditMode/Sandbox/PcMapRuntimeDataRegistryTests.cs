using System.IO;
using System.Linq;
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

        [Test]
        public void RuntimeServices_ExposeExactPcLookupRowsById()
        {
            var travel = PcMapTravelRuntimeService.LoadFromDirectory(PcMapDir);

            Assert.AreEqual(ExpectedPcWaypointRows, travel.WaypointCount);
            Assert.AreEqual(ExpectedPcWharfRows, travel.WharfCount);
            Assert.AreEqual(ExpectedPcReviveRows, travel.ReviveCount);
            Assert.AreEqual(ExpectedPcScrollRows, travel.ScrollValueCount);

            var firstWaypoint = travel.GetWaypoint(1);
            Assert.IsNotNull(firstWaypoint);
            Assert.AreEqual(2, firstWaypoint.MapId);
            Assert.AreEqual(2288, firstWaypoint.PosX);
            Assert.AreEqual(4091, firstWaypoint.PosY);

            var lastWaypoint = travel.GetWaypoint(225);
            Assert.IsNotNull(lastWaypoint);
            Assert.AreEqual(340, lastWaypoint.MapId);
            Assert.AreEqual(1853, lastWaypoint.PosX);
            Assert.AreEqual(3446, lastWaypoint.PosY);

            var bienKinhWharf = travel.GetWharf(3);
            Assert.IsNotNull(bienKinhWharf);
            Assert.AreEqual(37, bienKinhWharf.FromMapId);
            Assert.AreEqual(1938, bienKinhWharf.PosX);
            Assert.AreEqual(2459, bienKinhWharf.PosY);
            Assert.AreEqual(2, bienKinhWharf.SectCount,
                "Known PC mismatch: row 3 declares COUNT=1 but has two real SECT columns.");

            var firstScroll = travel.GetScrollValue(1);
            var lastScroll = travel.GetScrollValue(2600);
            Assert.IsNotNull(firstScroll);
            Assert.IsNotNull(lastScroll);
            Assert.AreEqual(0, firstScroll.cost);
            Assert.AreEqual(0, lastScroll.cost);
        }

        [Test]
        public void RuntimeRegistry_IndexesKnownRowsByMapWithoutChangingPcCoordinates()
        {
            var travel = PcMapTravelRuntimeService.LoadFromDirectory(PcMapDir);

            var map2Waypoint = travel.GetWaypointsForMap(2).Single();
            Assert.AreEqual(1, map2Waypoint.waypointId);
            Assert.AreEqual(2288, map2Waypoint.posX);
            Assert.AreEqual(4091, map2Waypoint.posY);

            var map37Wharf = travel.GetWharvesForMap(37).Single(w => w.wharfId == 3);
            Assert.AreEqual(1938, map37Wharf.posX);
            Assert.AreEqual(2459, map37Wharf.posY);
            Assert.AreEqual(2, map37Wharf.sectCount);

            var defaultRevive = travel.GetDefaultRevivePosition(1);
            Assert.IsNotNull(defaultRevive);
            Assert.AreEqual(0, defaultRevive.regionIndex);
            Assert.AreEqual(51104, defaultRevive.x);
            Assert.AreEqual(102592, defaultRevive.y);

            var map949Revive = travel.GetRevivePositionsForMap(949).Single();
            Assert.AreEqual(1, map949Revive.regionStart);
            Assert.AreEqual(3, map949Revive.regionEnd);
            Assert.AreEqual(1, map949Revive.regionIndex);
            Assert.AreEqual(51264, map949Revive.x);
            Assert.AreEqual(102368, map949Revive.y);
        }

        [Test]
        public void RuntimeQueries_DoNotFabricateRowsForMissingMapOrScrollValueTable()
        {
            var travel = PcMapTravelRuntimeService.LoadFromDirectory(PcMapDir);

            Assert.IsEmpty(travel.GetWaypointsForMap(999999));
            Assert.IsEmpty(travel.GetWharvesForMap(999999));
            Assert.IsEmpty(travel.GetRevivePositionsForMap(999999));

            Assert.AreEqual(ExpectedPcScrollRows, travel.Registry.ScrollCount);
            Assert.IsEmpty(travel.GetScrollMapRowsForMap(2),
                "PC scroll.txt in this source is a two-column value table, not map travel rows.");
            Assert.IsEmpty(travel.GetScrollMapRowsForMap(340));
            Assert.AreEqual(ExpectedPcScrollRows, travel.GetScrollValuesByFromMap(0).Count);
            Assert.AreEqual(0, travel.GetScrollValuesByFromMap(2).Count);
            Assert.AreEqual(0, travel.GetScrollValuesByToMap(340).Count);
        }
    }
}
