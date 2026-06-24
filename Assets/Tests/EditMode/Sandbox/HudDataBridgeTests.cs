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
    [TestFixture, Category("HUD")]
    public class HudDataBridgeTests
    {
        /// <summary>Fake runtime state provider (stands in for the live runtime systems).</summary>
        private class FakeRuntime : IRuntimeStateProvider
        {
            public bool HasActiveMap { get; set; } = true;
            public int ActiveMapId { get; set; } = 5;
            public string ActiveMapName { get; set; } = "Test Map";
            public VLTK.Model.MapDefinition ActiveMapDefinition { get; set; }
            public Vector2 PlayerWorldPosition { get; set; } = new Vector2(10, 20);
            public int PlayerLevel { get; set; } = 12;
            public int PlayerCurrentLife { get; set; } = 80;
            public int PlayerMaxLife { get; set; } = 100;
            public int PlayerCurrentMana { get; set; } = 50;
            public int PlayerMaxMana { get; set; } = 100;
            public int PlayerCurrentStamina { get; set; } = 60;
            public int PlayerMaxStamina { get; set; } = 100;
            public long PlayerExp { get; set; } = 999;
            public long PlayerMaxExp { get; set; } = 2000;
            public float MiniMapXRatio { get; set; } = 0f;
            public float MiniMapYRatio { get; set; } = 0f;
            public int PlayerCopper { get; set; } = 100;
            public int PlayerGold { get; set; } = 5;
            public int PlayerSilver { get; set; } = 151160;
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
        public void BuildSnapshot_NoActiveMap_StillValidAndBindsStats_PcParity()
        {
            // PC parity: player stats (level/hp/mp/stamina/exp) are always bound
            // from the runtime even when no map is active (login/sandbox). Only
            // the hasActiveMap flag is cleared to gate minimap rendering. See
            // HudDataBridge.BuildSnapshot for the rationale.
            var rt = new FakeRuntime { HasActiveMap = false };
            var bridge = new HudDataBridge(rt);
            var snap = bridge.BuildSnapshot();
            Assert.IsTrue(snap.valid);
            Assert.IsFalse(snap.hasActiveMap);
            Assert.AreEqual(12, snap.level);
            Assert.AreEqual(80, snap.currentLife);
            Assert.AreEqual(50, snap.currentMana);
            Assert.AreEqual(60, snap.currentStamina);
            Assert.AreEqual(999, snap.currentExp);
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

        // --- S5 §1: widened contract (stamina, real maxMana, real maxExp) ---

        [Test]
        public void BuildSnapshot_PopulatesStaminaAndFraction()
        {
            var bridge = new HudDataBridge(new FakeRuntime());
            var snap = bridge.BuildSnapshot();
            Assert.AreEqual(60, snap.currentStamina);
            Assert.AreEqual(100, snap.maxStamina);
            Assert.AreEqual(0.6f, snap.staminaFraction, 0.001f);
        }

        [Test]
        public void BuildSnapshot_PopulatesRealMaxMana_NotHardcoded100()
        {
            var rt = new FakeRuntime { PlayerCurrentMana = 150, PlayerMaxMana = 300 };
            var bridge = new HudDataBridge(rt);
            var snap = bridge.BuildSnapshot();
            Assert.AreEqual(300, snap.maxMana); // not hardcoded 100
            Assert.AreEqual(150, snap.currentMana);
            Assert.AreEqual(0.5f, snap.manaFraction, 0.001f);
        }

        [Test]
        public void BuildSnapshot_PopulatesRealMaxExpAndFraction()
        {
            var rt = new FakeRuntime { PlayerExp = 500, PlayerMaxExp = 1000 };
            var bridge = new HudDataBridge(rt);
            var snap = bridge.BuildSnapshot();
            Assert.AreEqual(1000, snap.maxExp);
            Assert.AreEqual(500, snap.currentExp);
            Assert.AreEqual(0.5f, snap.expFraction, 0.001f);
        }

        [Test]
        public void BuildSnapshot_GuardsStaminaMaxAgainstZero()
        {
            var rt = new FakeRuntime { PlayerCurrentStamina = 5, PlayerMaxStamina = 0 };
            var bridge = new HudDataBridge(rt);
            var snap = bridge.BuildSnapshot();
            Assert.AreEqual(1, snap.maxStamina); // guarded to >= 1
            Assert.AreEqual(0f, snap.staminaFraction, 0.001f);
        }
    }
}
