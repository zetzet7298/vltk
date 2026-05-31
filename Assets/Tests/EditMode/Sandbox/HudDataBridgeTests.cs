using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M6.4 — Production HUD Bridge tests. HUD reads runtime systems not conversion
    /// internals (AC#1), GM openable in dev builds (AC#2), and debug controls hidden
    /// in release builds (AC#3).
    /// </summary>
    public class HudDataBridgeTests
    {
        /// <summary>Fake runtime state provider (stands in for the live runtime systems).</summary>
        private class FakeRuntime : IRuntimeStateProvider
        {
            public bool HasActiveMap { get; set; } = true;
            public int ActiveMapId { get; set; } = 5;
            public string ActiveMapName { get; set; } = "Test Map";
            public Vector2 PlayerWorldPosition { get; set; } = new Vector2(10, 20);
            public int PlayerLevel { get; set; } = 12;
            public int PlayerCurrentLife { get; set; } = 80;
            public int PlayerMaxLife { get; set; } = 100;
        }

        // --- AC#1: snapshot from runtime systems ---

        [Test]
        public void BuildSnapshot_ReadsRuntimeState()
        {
            var bridge = new HudDataBridge(new FakeRuntime());
            var snap = bridge.BuildSnapshot();
            Assert.IsTrue(snap.valid);
            Assert.AreEqual(5, snap.mapId);
            Assert.AreEqual("Test Map", snap.mapName);
            Assert.AreEqual(new Vector2(10, 20), snap.playerPosition);
            Assert.AreEqual(12, snap.level);
            Assert.AreEqual(0.8f, snap.lifeFraction, 0.001f);
        }

        [Test]
        public void BuildSnapshot_NoActiveMap_Invalid()
        {
            var rt = new FakeRuntime { HasActiveMap = false };
            var bridge = new HudDataBridge(rt);
            Assert.IsFalse(bridge.BuildSnapshot().valid);
        }

        [Test]
        public void BuildSnapshot_NullRuntime_Invalid()
        {
            var bridge = new HudDataBridge(null);
            Assert.IsFalse(bridge.BuildSnapshot().valid);
        }

        [Test]
        public void BuildSnapshot_ClampsLifeToMax()
        {
            var rt = new FakeRuntime { PlayerCurrentLife = 200, PlayerMaxLife = 100 };
            var bridge = new HudDataBridge(rt);
            var snap = bridge.BuildSnapshot();
            Assert.AreEqual(100, snap.currentLife);
            Assert.AreEqual(1f, snap.lifeFraction, 0.001f);
        }

        [Test]
        public void BuildSnapshot_ZeroMaxLife_NoDivideByZero()
        {
            var rt = new FakeRuntime { PlayerCurrentLife = 0, PlayerMaxLife = 0 };
            var bridge = new HudDataBridge(rt);
            var snap = bridge.BuildSnapshot();
            Assert.AreEqual(1, snap.maxLife); // guarded to >= 1
            Assert.AreEqual(0f, snap.lifeFraction, 0.001f);
        }

        // --- AC#2: GM openable in dev ---

        [Test]
        public void DevBuild_CanOpenGm()
        {
            var bridge = new HudDataBridge(new FakeRuntime(), isDevelopmentBuild: true);
            Assert.IsTrue(bridge.CanOpenGmPanel());
            Assert.IsTrue(bridge.DebugControlsAllowed());
        }

        [Test]
        public void DevBuild_RunsDebugAction()
        {
            var bridge = new HudDataBridge(new FakeRuntime(), isDevelopmentBuild: true);
            bool ran = false;
            Assert.IsTrue(bridge.TryRunDebugAction("toggleObstacles", () => ran = true));
            Assert.IsTrue(ran);
        }

        // --- AC#3: debug controls hidden in release ---

        [Test]
        public void ReleaseBuild_HidesGm()
        {
            var bridge = new HudDataBridge(new FakeRuntime(), isDevelopmentBuild: false);
            Assert.IsFalse(bridge.CanOpenGmPanel());
            Assert.IsFalse(bridge.DebugControlsAllowed());
        }

        [Test]
        public void ReleaseBuild_BlocksDebugAction()
        {
            var bridge = new HudDataBridge(new FakeRuntime(), isDevelopmentBuild: false);
            bool ran = false;
            LogAssert.Expect(LogType.Warning, "[HUD] Debug action 'spawnNpc' blocked in release build");
            Assert.IsFalse(bridge.TryRunDebugAction("spawnNpc", () => ran = true));
            Assert.IsFalse(ran);
        }
    }
}
