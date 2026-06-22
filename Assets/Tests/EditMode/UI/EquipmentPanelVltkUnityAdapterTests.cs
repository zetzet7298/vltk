// -----------------------------------------------------------------------------
// VLTK Mobile — Equipment panel vltkunity adapter EditMode tests
// Phase 2 Commit 2d. Category: HUD.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HUD")]
    public class EquipmentPanelVltkUnityAdapterTests
    {
        private class FakeEquipBus : IEquipmentCommandBus
        {
            public event System.Action<int> OnEquipmentTabChanged;
            public event System.Action<string> OnAttributeIncrementRequested;
            public event System.Action OnEquipmentCloseRequested;

            public int TabChangeCount, AttrIncrCount, CloseCount;
            public int LastTabId = -1;
            public string LastAttrName;

            public void PublishEquipmentTabChanged(int tabIndex) { TabChangeCount++; LastTabId = tabIndex; OnEquipmentTabChanged?.Invoke(tabIndex); }
            public void PublishAttributeIncrementRequested(string attributeName) { AttrIncrCount++; LastAttrName = attributeName; OnAttributeIncrementRequested?.Invoke(attributeName); }
            public void PublishEquipmentCloseRequested() { CloseCount++; OnEquipmentCloseRequested?.Invoke(); }
        }

        private static CharacterPanelSnapshot MakeSnapshot()
        {
            return new CharacterPanelSnapshot
            {
                playerName = "TestPlayer",
                level = 30,
                exp = 5000,
                expMax = 10000,
                hp = 800,
                hpMax = 1000,
                mp = 400,
                mpMax = 500,
                stamina = 200,
                staminaMax = 300,
                rows = new List<CharacterPanelRow>
                {
                    new CharacterPanelRow(1, "Sức Mạnh", 50, 10, 5, 65, "Tăng sát thương"),
                    new CharacterPanelRow(2, "Sinh Khí", 40, 8, 0, 48, "Tăng sinh lực"),
                },
            };
        }

        private static VisualElement MakeRoot()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkEquipPropertiesTab" });
            root.Add(new VisualElement { name = "VltkEquipEquipmentTab" });
            root.Add(new VisualElement { name = "VltkEquipItemsTab" });
            root.Add(new VisualElement { name = "VltkEquipSeriesTab" });
            root.Add(new VisualElement { name = "VltkEquipTabPropertiesBtn" });
            root.Add(new VisualElement { name = "VltkEquipTabEquipmentBtn" });
            root.Add(new VisualElement { name = "VltkEquipTabItemsBtn" });
            root.Add(new VisualElement { name = "VltkEquipTabSeriesBtn" });
            root.Add(new VisualElement { name = "VltkEquipCloseBtn" });
            root.Add(new VisualElement { name = "VltkEquipStrengthAddBtn" });
            root.Add(new VisualElement { name = "VltkEquipVitalityAddBtn" });
            root.Add(new VisualElement { name = "VltkEquipDexterityAddBtn" });
            root.Add(new VisualElement { name = "VltkEquipEnergyAddBtn" });
            root.Add(new Label { name = "VltkEquipPlayerName" });
            root.Add(new Label { name = "VltkEquipLevel" });
            root.Add(new Label { name = "VltkEquipExp" });
            root.Add(new Label { name = "VltkEquipHp" });
            root.Add(new Label { name = "VltkEquipMp" });
            root.Add(new Label { name = "VltkEquipStamina" });
            root.Add(new VisualElement { name = "VltkEquipStatsList" });
            return root;
        }

        [Test]
        public void Bind_DefaultsToPropertiesTab()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            Assert.AreEqual(EquipmentPanelVltkUnityAdapter.TabProperties, adapter.ActiveTab);
        }

        [Test]
        public void Apply_PopulatesPlayerInfo()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.Apply(MakeSnapshot());

            var name = root.Q<Label>("VltkEquipPlayerName");
            var level = root.Q<Label>("VltkEquipLevel");
            var hp = root.Q<Label>("VltkEquipHp");
            Assert.AreEqual("TestPlayer", name.text);
            Assert.AreEqual("30", level.text);
            Assert.AreEqual("800/1000", hp.text);
        }

        [Test]
        public void Apply_RendersStatRows()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.Apply(MakeSnapshot());

            var statsList = root.Q("VltkEquipStatsList");
            Assert.AreEqual(2, statsList.childCount);
        }

        [Test]
        public void SimulateCloseClick_PublishesClose()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.SimulateCloseClick();

            Assert.AreEqual(1, bus.CloseCount);
        }

        [Test]
        public void SimulateStrengthAdd_PublishesIncrement()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.SimulateStrengthAdd();

            Assert.AreEqual(1, bus.AttrIncrCount);
            Assert.AreEqual("Strength", bus.LastAttrName);
        }

        [Test]
        public void SimulateVitalityAdd_PublishesIncrement()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.SimulateVitalityAdd();

            Assert.AreEqual("Vitality", bus.LastAttrName);
        }

        [Test]
        public void TabSwitch_ShowsCorrectPanel()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            // Properties is visible initially
            var propTab = root.Q("VltkEquipPropertiesTab");
            var equipTab = root.Q("VltkEquipEquipmentTab");
            Assert.AreEqual(DisplayStyle.Flex, propTab.style.display.value);
            Assert.AreEqual(DisplayStyle.None, equipTab.style.display.value);

            // Switch to equipment tab via SimulateClick pattern: invoke tab change
            adapter.SimulateTabSwitch(EquipmentPanelVltkUnityAdapter.TabEquipment);

            Assert.AreEqual(DisplayStyle.None, propTab.style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, equipTab.style.display.value);
            Assert.AreEqual(EquipmentPanelVltkUnityAdapter.TabEquipment, adapter.ActiveTab);
        }

        [Test]
        public void Apply_NullSnapshotDoesNotCrash()
        {
            var bus = new FakeEquipBus();
            var root = MakeRoot();
            var adapter = new EquipmentPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            Assert.DoesNotThrow(() => adapter.Apply(null));
        }
    }
}
