using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.PortFactorySmoke
{
    public sealed class PcMapTravelBehaviorServiceTests
    {
        private sealed class MockTeleportHost : IMapTeleportHost
        {
            public int LastMapId { get; private set; }
            public Vector2 LastWorldPosition { get; private set; }
            public bool TeleportCalled { get; private set; }
            public bool ReturnHasMap { get; set; } = true;

            public bool HasMap(int mapId) => ReturnHasMap;

            public void SwitchMapAndPlacePlayer(int mapId, Vector2 worldPosition)
            {
                LastMapId = mapId;
                LastWorldPosition = worldPosition;
                TeleportCalled = true;
            }
        }

        [Test]
        public void ExecuteTravelAction_WithNullResult_ReturnsInvalid()
        {
            var service = new PcMapTravelBehaviorService(new MockTeleportHost());
            var result = service.ExecuteTravelAction(null);
            
            Assert.AreEqual(GmItemActionStatus.Invalid, result.status);
        }

        [Test]
        public void ExecuteTravelAction_WithNoTeleport_ReturnsBlocked()
        {
            var service = new PcMapTravelBehaviorService(new MockTeleportHost());
            var actionResult = new PcMapTravelActionResult { Status = PcMapTravelActionStatus.Unsupported, Message = "Test block" };
            
            var result = service.ExecuteTravelAction(actionResult);
            
            Assert.AreEqual(GmItemActionStatus.Blocked, result.status);
            Assert.AreEqual("Test block", result.message);
        }

        [Test]
        public void ExecuteTravelAction_WithNullHost_ReturnsNotPorted()
        {
            var service = new PcMapTravelBehaviorService(null);
            var actionResult = new PcMapTravelActionResult { Status = PcMapTravelActionStatus.Ready, TargetMapId = 1 };
            
            var result = service.ExecuteTravelAction(actionResult);
            
            Assert.AreEqual(GmItemActionStatus.NotPorted, result.status);
        }

        [Test]
        public void ExecuteTravelAction_WithMissingMap_ReturnsNotPorted()
        {
            var host = new MockTeleportHost { ReturnHasMap = false };
            var service = new PcMapTravelBehaviorService(host);
            var actionResult = new PcMapTravelActionResult { Status = PcMapTravelActionStatus.Ready, TargetMapId = 999 };
            
            var result = service.ExecuteTravelAction(actionResult);
            
            Assert.AreEqual(GmItemActionStatus.NotPorted, result.status);
            Assert.IsFalse(host.TeleportCalled);
        }

        [Test]
        public void ExecuteTravelAction_WaypointTeleport_UsesCellCoordinates()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);
            var actionResult = new PcMapTravelActionResult 
            { 
                Status = PcMapTravelActionStatus.Ready, 
                Kind = PcMapTravelActionKind.WaypointTeleport,
                TargetMapId = 1,
                X = 10 * 32,
                Y = 20 * 32
            };
            
            var result = service.ExecuteTravelAction(actionResult);
            
            Assert.AreEqual(GmItemActionStatus.Success, result.status);
            Assert.IsTrue(host.TeleportCalled);
            Assert.AreEqual(1, host.LastMapId);
            // 10 * 32, 20 * 32 -> then MpsToWorld
            Vector2 expected = MapEnemyDatabase.MpsToWorld(320, 640);
            Assert.AreEqual(expected, host.LastWorldPosition);
        }

        [Test]
        public void ExecuteTravelAction_ScrollValue_UsesCellCoordinates()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);
            var actionResult = new PcMapTravelActionResult 
            { 
                Status = PcMapTravelActionStatus.Ready, 
                Kind = PcMapTravelActionKind.ScrollValue,
                TargetMapId = 2,
                X = 15 * 32,
                Y = 25 * 32
            };
            
            var result = service.ExecuteTravelAction(actionResult);
            
            Assert.AreEqual(GmItemActionStatus.Success, result.status);
            Assert.IsTrue(host.TeleportCalled);
            Assert.AreEqual(2, host.LastMapId);
            Vector2 expected = MapEnemyDatabase.MpsToWorld(15 * 32, 25 * 32);
            Assert.AreEqual(expected, host.LastWorldPosition);
        }

        [Test]
        public void ExecuteTravelAction_DefaultRevive_UsesMpsCoordinates()
        {
            var host = new MockTeleportHost();
            var service = new PcMapTravelBehaviorService(host);
            var actionResult = new PcMapTravelActionResult 
            { 
                Status = PcMapTravelActionStatus.Ready, 
                Kind = PcMapTravelActionKind.DefaultRevive,
                TargetMapId = 3,
                X = 50000,
                Y = 100000
            };
            
            var result = service.ExecuteTravelAction(actionResult);
            
            Assert.AreEqual(GmItemActionStatus.Success, result.status);
            Assert.IsTrue(host.TeleportCalled);
            Assert.AreEqual(3, host.LastMapId);
            Vector2 expected = MapEnemyDatabase.MpsToWorld(50000, 100000);
            Assert.AreEqual(expected, host.LastWorldPosition);
        }
    }
}
