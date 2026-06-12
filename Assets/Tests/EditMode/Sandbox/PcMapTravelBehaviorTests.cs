// -----------------------------------------------------------------------------
// VLTK Mobile — PcMapTravelBehaviorService Tests
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public sealed class PcMapTravelBehaviorTests
    {
        private sealed class MockTeleportHost : IMapTeleportHost
        {
            public int MapId { get; private set; }
            public Vector2 WorldPosition { get; private set; }
            public bool Called { get; private set; }

            public bool HasMap(int mapId) => mapId == 53 || mapId == 907;

            public void SwitchMapAndPlacePlayer(int mapId, Vector2 worldPosition)
            {
                MapId = mapId;
                WorldPosition = worldPosition;
                Called = true;
            }
        }

        [Test]
        public void ExecuteTravelAction_NullResult_ReturnsInvalid()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);

            var response = service.ExecuteTravelAction(null);

            Assert.IsFalse(response.success);
            Assert.AreEqual(GmItemActionStatus.Invalid, response.status);
            Assert.IsFalse(host.Called);
        }

        [Test]
        public void ExecuteTravelAction_NoTeleportData_ReturnsBlocked()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);

            var actionResult = new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.WharfTravel,
                Status = PcMapTravelActionStatus.DataOnly,
                Message = "Data only"
            };

            var response = service.ExecuteTravelAction(actionResult);

            Assert.IsFalse(response.success);
            Assert.AreEqual(GmItemActionStatus.Blocked, response.status);
            Assert.IsFalse(host.Called);
        }

        [Test]
        public void ExecuteTravelAction_MapNotInstalled_ReturnsNotPorted()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);

            var actionResult = new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.WaypointTeleport,
                Status = PcMapTravelActionStatus.Ready,
                TargetMapId = 9999 // Not in MockTeleportHost
            };

            var response = service.ExecuteTravelAction(actionResult);

            Assert.IsFalse(response.success);
            Assert.AreEqual(GmItemActionStatus.NotPorted, response.status);
            Assert.IsFalse(host.Called);
        }

        [Test]
        public void ExecuteTravelAction_WaypointTeleport_ConvertsCellToWorld()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);

            var actionResult = new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.WaypointTeleport,
                Status = PcMapTravelActionStatus.Ready,
                TargetMapId = 53,
                X = 100, // cellX
                Y = 200  // cellY
            };

            var response = service.ExecuteTravelAction(actionResult);

            Assert.IsTrue(response.success);
            Assert.IsTrue(host.Called);
            Assert.AreEqual(53, host.MapId);

            // MpsToWorld(100 * 32, 200 * 32) -> (3200, -6400)
            Assert.AreEqual(new Vector2(3200f, -6400f), host.WorldPosition);
        }

        [Test]
        public void ExecuteTravelAction_DefaultRevive_ConvertsMpsToWorld()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);

            var actionResult = new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.DefaultRevive,
                Status = PcMapTravelActionStatus.Ready,
                TargetMapId = 907,
                X = 3200, // mpsX
                Y = 6400  // mpsY
            };

            var response = service.ExecuteTravelAction(actionResult);

            Assert.IsTrue(response.success);
            Assert.IsTrue(host.Called);
            Assert.AreEqual(907, host.MapId);

            // MpsToWorld(3200, 6400) -> (3200, -6400)
            Assert.AreEqual(new Vector2(3200f, -6400f), host.WorldPosition);
        }
    }
}
