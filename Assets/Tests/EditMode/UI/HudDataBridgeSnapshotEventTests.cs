using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture]
    [Category("HUD")]
    public class HudDataBridgeSnapshotEventTests
    {
        private sealed class StubRuntime : IRuntimeStateProvider
        {
            public bool HasActiveMap { get; set; } = true;
            public int ActiveMapId { get; set; } = 53;
            public string ActiveMapName { get; set; } = "Ba Ling";
            public MapDefinition ActiveMapDefinition { get; set; } = null;
            public Vector2 PlayerWorldPosition { get; set; } = Vector2.zero;
            public int PlayerLevel { get; set; } = 10;
            public int PlayerCurrentLife { get; set; } = 100;
            public int PlayerMaxLife { get; set; } = 100;
            public int PlayerCurrentMana { get; set; } = 50;
            public int PlayerMaxMana { get; set; } = 100;
            public int PlayerCurrentStamina { get; set; } = 100;
            public int PlayerMaxStamina { get; set; } = 100;
            public long PlayerExp { get; set; } = 1234;
            public long PlayerMaxExp { get; set; } = 5000;
            public float MiniMapXRatio { get; set; } = 0f;
            public float MiniMapYRatio { get; set; } = 0f;
            public int PlayerCopper { get; set; } = 0;
            public int PlayerGold { get; set; } = 0;
            public int PlayerSilver { get; set; } = 0;
        }

        [Test]
        public void FirstRefresh_RaisesEventWithValidSnapshot()
        {
            var bridge = new HudDataBridge(new StubRuntime(), false);
            HudSnapshot captured = default;
            int hits = 0;
            bridge.SnapshotChanged += snap => { captured = snap; hits++; };

            bool changed = bridge.RefreshAndPublish();

            Assert.IsTrue(changed);
            Assert.AreEqual(1, hits);
            Assert.IsTrue(captured.valid);
            Assert.AreEqual(53, captured.mapId);
            Assert.AreEqual(10, captured.level);
            Assert.AreEqual(100, captured.currentLife);
            Assert.AreEqual(100, captured.maxLife);
            Assert.AreEqual(50, captured.currentMana);
            Assert.AreEqual(1234, captured.currentExp);
        }

        [Test]
        public void RefreshWithIdenticalData_DoesNotRaiseEvent()
        {
            var runtime = new StubRuntime();
            var bridge = new HudDataBridge(runtime, false);
            bridge.RefreshAndPublish();
            int hits = 0;
            bridge.SnapshotChanged += _ => hits++;

            bool changed = bridge.RefreshAndPublish();

            Assert.IsFalse(changed);
            Assert.AreEqual(0, hits);
        }

        [Test]
        public void RefreshWithChangedLife_RaisesEvent()
        {
            var runtime = new StubRuntime();
            var bridge = new HudDataBridge(runtime, false);
            bridge.RefreshAndPublish();
            int hits = 0;
            HudSnapshot captured = default;
            bridge.SnapshotChanged += snap => { captured = snap; hits++; };

            runtime.PlayerCurrentLife = 75;
            bool changed = bridge.RefreshAndPublish();

            Assert.IsTrue(changed);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(75, captured.currentLife);
        }

        [Test]
        public void RefreshWithChangedStamina_RaisesEvent()
        {
            var runtime = new StubRuntime();
            var bridge = new HudDataBridge(runtime, false);
            bridge.RefreshAndPublish();
            int hits = 0;
            HudSnapshot captured = default;
            bridge.SnapshotChanged += snap => { captured = snap; hits++; };

            runtime.PlayerCurrentStamina = 40;
            bool changed = bridge.RefreshAndPublish();

            Assert.IsTrue(changed);
            Assert.AreEqual(1, hits);
            Assert.AreEqual(40, captured.currentStamina);
        }

        [Test]
        public void RefreshWithNoActiveMap_ReturnsInvalidSnapshot()
        {
            var bridge = new HudDataBridge(new StubRuntime { HasActiveMap = false }, false);
            HudSnapshot captured = default;
            bridge.SnapshotChanged += snap => captured = snap;

            bridge.RefreshAndPublish();

            Assert.IsFalse(captured.valid);
        }
    }
}
