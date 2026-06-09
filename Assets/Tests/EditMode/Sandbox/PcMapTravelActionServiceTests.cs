using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMapTravelActionServiceTests
    {
        private const string SettingsPath = "Assets/StreamingAssets/Reference/PcMap";
        private const int ExpectedWaypointRows = 225;
        private const int ExpectedWharfRows = 11;
        private const int ExpectedWharfSectSlots = 16;
        private const int ExpectedScrollValueRows = 2600;
        private static string PcMapDir => Path.Combine(Directory.GetCurrentDirectory(), SettingsPath);

        [Test]
        public void Service_ConsumesRuntimeCountsWithoutReparsingContract()
        {
            var service = PcMapTravelActionService.LoadFromDirectory(PcMapDir);

            Assert.AreEqual(ExpectedWaypointRows, service.Runtime.WaypointCount);
            Assert.AreEqual(ExpectedWharfRows, service.Runtime.WharfCount);
            Assert.AreEqual(241, service.Runtime.ReviveCount);
            Assert.AreEqual(ExpectedScrollValueRows, service.Runtime.ScrollValueCount);
        }

        [Test]
        public void WaypointTeleport_ResolvesRepresentativeFirstAndLastPcRows()
        {
            var service = PcMapTravelActionService.LoadFromDirectory(PcMapDir);

            var first = service.ResolveWaypointTeleport(1);
            Assert.AreEqual(PcMapTravelActionStatus.Ready, first.Status);
            Assert.IsTrue(first.HasTeleport);
            Assert.AreEqual(2, first.TargetMapId);
            Assert.AreEqual(2288, first.X);
            Assert.AreEqual(4091, first.Y);

            var last = service.ResolveWaypointTeleport(225);
            Assert.AreEqual(PcMapTravelActionStatus.Ready, last.Status);
            Assert.AreEqual(340, last.TargetMapId);
            Assert.AreEqual(1853, last.X);
            Assert.AreEqual(3446, last.Y);
        }

        [Test]
        public void WharfTravel_PreservesSectFactsButDoesNotFabricateDestinations()
        {
            var service = PcMapTravelActionService.LoadFromDirectory(PcMapDir);

            var allSectSlots = Enumerable.Range(1, ExpectedWharfRows)
                .Select(id => service.ResolveWharfTravelByWharfId(id))
                .Sum(r => r.SectCount);
            Assert.AreEqual(ExpectedWharfSectSlots, allSectSlots);

            var bienKinh = service.ResolveWharfTravelByWharfId(3);
            Assert.AreEqual(PcMapTravelActionStatus.DataOnly, bienKinh.Status);
            Assert.IsFalse(bienKinh.HasTeleport);
            Assert.AreEqual(37, bienKinh.CurrentMapId);
            Assert.AreEqual(1938, bienKinh.X);
            Assert.AreEqual(2459, bienKinh.Y);
            Assert.AreEqual(2, bienKinh.SectCount);
            StringAssert.Contains("không tự tạo teleport", bienKinh.Message);

            var fromMap = service.ResolveWharfTravelFromMap(37).Single(r => r.SourceId == 3);
            Assert.AreEqual(2, fromMap.SectCount);
        }

        [Test]
        public void DefaultRevive_ResolvesMap949SinglePcRow()
        {
            var service = PcMapTravelActionService.LoadFromDirectory(PcMapDir);

            var revive = service.ResolveDefaultRevive(949);
            Assert.AreEqual(PcMapTravelActionStatus.Ready, revive.Status);
            Assert.IsTrue(revive.HasTeleport);
            Assert.AreEqual(949, revive.TargetMapId);
            Assert.AreEqual(1, revive.SourceId);
            Assert.AreEqual(51264, revive.X);
            Assert.AreEqual(102368, revive.Y);
            Assert.AreEqual(3, revive.SectCount);

            Assert.AreEqual(1, service.Runtime.GetRevivePositionsForMap(949).Count);
        }

        [Test]
        public void ScrollValue_ProvesValueTableAndNoFabricatedMapTeleport()
        {
            var service = PcMapTravelActionService.LoadFromDirectory(PcMapDir);

            var value = service.ResolveScrollValue(2600);
            Assert.AreEqual(PcMapTravelActionStatus.DataOnly, value.Status);
            Assert.IsFalse(value.HasTeleport);
            Assert.AreEqual(2600, value.SourceId);
            Assert.AreEqual(0, value.Value);
            Assert.AreEqual(ExpectedScrollValueRows, service.Runtime.GetScrollValuesByFromMap(0).Count);

            var mapRows = service.ResolveScrollTeleportRowsForMap(2);
            Assert.AreEqual(PcMapTravelActionStatus.Unsupported, mapRows.Status);
            Assert.IsFalse(mapRows.HasTeleport);
            StringAssert.Contains("Không tự tạo teleport", mapRows.Message);
        }
    }
}
