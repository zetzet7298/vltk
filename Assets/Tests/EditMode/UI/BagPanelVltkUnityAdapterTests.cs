// -----------------------------------------------------------------------------
// VLTK Mobile — Bag panel vltkunity adapter EditMode tests
// Phase 2 Commit 2e. Category: HUD.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HUD")]
    public class BagPanelVltkUnityAdapterTests
    {
        private class FakeBagBus : IBagCommandBus
        {
            public event System.Action<int> OnBagTabChanged;
            public event System.Action<int> OnItemSelected;
            public event System.Action OnBagCloseRequested;

            public int TabChangeCount, SelectCount, CloseCount;
            public int LastTabId = -1, LastSlotIndex = -1;

            public void PublishBagTabChanged(int tabIndex) { TabChangeCount++; LastTabId = tabIndex; OnBagTabChanged?.Invoke(tabIndex); }
            public void PublishItemSelected(int slotIndex) { SelectCount++; LastSlotIndex = slotIndex; OnItemSelected?.Invoke(slotIndex); }
            public void PublishBagCloseRequested() { CloseCount++; OnBagCloseRequested?.Invoke(); }
        }

        private static BagAdapterSnapshot MakeSnapshot()
        {
            return new BagAdapterSnapshot
            {
                usedSlots = 5,
                totalSlots = 200,
                items = new List<BagItemRow>
                {
                    new BagItemRow(0, 1001, "Kiếm Sắt", "Green", "Sát thương 10-20"),
                    new BagItemRow(1, 1002, "Mũ Da", "White", "Phòng thủ 5"),
                    new BagItemRow(2, 1003, "Giáp Vàng", "Gold", "Phòng thủ 50"),
                },
            };
        }

        private static VisualElement MakeRoot()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = "VltkBagGrid" });
            root.Add(new VisualElement { name = "VltkStorageGrid" });
            root.Add(new Label { name = "VltkBagCount" });
            root.Add(new Label { name = "VltkStorageCount" });
            root.Add(new VisualElement { name = "VltkBagCloseBtn" });
            root.Add(new VisualElement { name = "VltkBagTabBagBtn" });
            root.Add(new VisualElement { name = "VltkBagTabStorageBtn" });
            return root;
        }

        [Test]
        public void Bind_DefaultsToBagTab()
        {
            var bus = new FakeBagBus();
            var adapter = new BagPanelVltkUnityAdapter(MakeRoot(), bus);
            adapter.Bind();

            Assert.AreEqual(BagPanelVltkUnityAdapter.TabBag, adapter.ActiveTab);
        }

        [Test]
        public void Apply_RendersItemSlots()
        {
            var bus = new FakeBagBus();
            var root = MakeRoot();
            var adapter = new BagPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.Apply(MakeSnapshot());

            var grid = root.Q("VltkBagGrid");
            Assert.AreEqual(3, grid.childCount);
        }

        [Test]
        public void Apply_UpdatesCountLabel()
        {
            var bus = new FakeBagBus();
            var root = MakeRoot();
            var adapter = new BagPanelVltkUnityAdapter(root, bus);
            adapter.Bind();

            adapter.Apply(MakeSnapshot());

            var count = root.Q<Label>("VltkBagCount");
            Assert.AreEqual("5/200", count.text);
        }

        [Test]
        public void SimulateCloseClick_PublishesClose()
        {
            var bus = new FakeBagBus();
            var adapter = new BagPanelVltkUnityAdapter(MakeRoot(), bus);
            adapter.Bind();

            adapter.SimulateCloseClick();

            Assert.AreEqual(1, bus.CloseCount);
        }

        [Test]
        public void SimulateTabSwitch_PublishesTabChange()
        {
            var bus = new FakeBagBus();
            var adapter = new BagPanelVltkUnityAdapter(MakeRoot(), bus);
            adapter.Bind();

            adapter.SimulateTabSwitch(BagPanelVltkUnityAdapter.TabStorage);

            Assert.AreEqual(BagPanelVltkUnityAdapter.TabStorage, adapter.ActiveTab);
            Assert.AreEqual(2, bus.TabChangeCount);
            Assert.AreEqual(BagPanelVltkUnityAdapter.TabStorage, bus.LastTabId);
        }

        [Test]
        public void SimulateItemClick_PublishesItemSelected()
        {
            var bus = new FakeBagBus();
            var adapter = new BagPanelVltkUnityAdapter(MakeRoot(), bus);
            adapter.Bind();

            adapter.SimulateItemClick(42);

            Assert.AreEqual(1, bus.SelectCount);
            Assert.AreEqual(42, bus.LastSlotIndex);
        }

        [Test]
        public void Apply_NullSnapshotDoesNotCrash()
        {
            var bus = new FakeBagBus();
            var adapter = new BagPanelVltkUnityAdapter(MakeRoot(), bus);
            adapter.Bind();

            Assert.DoesNotThrow(() => adapter.Apply(null));
        }

        [Test]
        public void Apply_EmptyItemsClearsGrid()
        {
            var bus = new FakeBagBus();
            var root = MakeRoot();
            var adapter = new BagPanelVltkUnityAdapter(root, bus);
            adapter.Bind();
            adapter.Apply(MakeSnapshot());
            Assert.AreEqual(3, root.Q("VltkBagGrid").childCount);

            var emptySnap = new BagAdapterSnapshot
            {
                usedSlots = 0,
                totalSlots = 200,
                items = new List<BagItemRow>(),
            };
            adapter.Apply(emptySnap);

            Assert.AreEqual(0, root.Q("VltkBagGrid").childCount);
        }
    }
}
