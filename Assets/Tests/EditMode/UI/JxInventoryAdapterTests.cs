// -----------------------------------------------------------------------------
// VLTK Mobile — JX inventory adapter EditMode tests (port KuiItemVN/KuiItem)
// Verifies: render creates element per item at grid pixel position, quality class
// (equip), stack label visibility, locked/broken classes, click→action, drag
// move/swap coordinator. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxInventoryAdapterTests
    {
        private class FakeBus : IJxHudCommandBus
        {
            public List<JxHudPanel> PanelRequests = new();
            public List<JxHudAction> ActionRequests = new();
            public void PublishPanelRequested(JxHudPanel panel) => PanelRequests.Add(panel);
            public void PublishActionRequested(JxHudAction action) => ActionRequests.Add(action);
        }

        private static JxInventoryItem Item(int id, int gx, int gy, int w = 1, int h = 1,
            uint genre = (uint)JxItemGenre.Medicine, int stack = 0, bool stackable = false,
            int durability = -1, JxItemQuality quality = JxItemQuality.Normal, bool locked = false)
            => new()
            {
                ItemId = id, GridX = gx, GridY = gy, Width = w, Height = h,
                Genre = genre, Stack = stack, Stackable = stackable, Durability = durability,
                Quality = quality, Locked = locked,
            };

        private static VisualElement MakeGrid()
        {
            var root = new VisualElement();
            root.Add(new VisualElement { name = JxInventoryAdapter.Names.Grid });
            return root;
        }

        [Test]
        public void Bind_ReturnsTrue_WhenGridPresent()
        {
            var adapter = new JxInventoryAdapter(MakeGrid(), new JxInventoryState(), new FakeBus());
            Assert.IsTrue(adapter.Bind());
        }

        [Test]
        public void Bind_ReturnsFalse_WhenGridMissing()
        {
            var adapter = new JxInventoryAdapter(new VisualElement(), new JxInventoryState(), new FakeBus());
            Assert.IsFalse(adapter.Bind());
        }

        [Test]
        public void Render_CreatesElementPerItem_WithPixelPosition()
        {
            var root = MakeGrid();
            var state = new JxInventoryState(cols: 8, rows: 6);
            state.AddItem(Item(1, 0, 0, 1, 1));
            state.AddItem(Item(2, 2, 2, 1, 1));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();
            adapter.Render(parentHeight: 500f);

            var grid = root.Q<VisualElement>(JxInventoryAdapter.Names.Grid);
            var el1 = grid.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 1);
            var el2 = grid.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 2);
            Assert.IsNotNull(el1);
            Assert.IsNotNull(el2);
            // Item 2 tại (2,2): local y = 54+2*52+26 = 184; parent y = 500-184 = 316 (top).
            Assert.That(el2.style.top.value.value, Is.EqualTo(316f - JxInventoryState.CellSize / 2f));
        }

        [Test]
        public void Render_RemovesElementWhenItemRemoved()
        {
            var root = MakeGrid();
            var state = new JxInventoryState();
            state.AddItem(Item(1, 0, 0));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();
            var grid = root.Q<VisualElement>(JxInventoryAdapter.Names.Grid);
            Assert.IsNotNull(grid.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 1));

            state.RemoveItem(1);
            adapter.Render();
            Assert.IsNull(grid.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 1));
        }

        [Test]
        public void Render_QualityClass_OnlyForEquip()
        {
            var root = MakeGrid();
            var state = new JxInventoryState();
            // Equip purple.
            state.AddItem(Item(1, 0, 0, genre: (uint)JxItemGenre.Equip, quality: JxItemQuality.Purple));
            // Medicine (no quality class).
            state.AddItem(Item(2, 1, 0, genre: (uint)JxItemGenre.Medicine));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();

            var el1 = root.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 1);
            var el2 = root.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 2);
            Assert.IsTrue(el1.ClassListContains(JxInventoryAdapter.QualityClassFor(JxItemQuality.Purple)));
            Assert.IsFalse(el2.ClassListContains(JxInventoryAdapter.QualityClassFor(JxItemQuality.Purple)));
        }

        [Test]
        public void Render_StackLabel_VisibleForStackableNonEquip()
        {
            var root = MakeGrid();
            var state = new JxInventoryState();
            state.AddItem(Item(1, 0, 0, genre: (uint)JxItemGenre.Medicine, stack: 5, stackable: true));
            state.AddItem(Item(2, 1, 0, genre: (uint)JxItemGenre.Equip, stack: 5, stackable: true));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();

            var stack1 = root.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 1)
                .Q<Label>(JxInventoryAdapter.Names.Stack);
            var stack2 = root.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 2)
                .Q<Label>(JxInventoryAdapter.Names.Stack);
            Assert.That(stack1.style.display.value, Is.EqualTo(DisplayStyle.Flex));
            Assert.That(stack1.text, Is.EqualTo("5"));
            Assert.That(stack2.style.display.value, Is.EqualTo(DisplayStyle.None));
        }

        [Test]
        public void Render_LockedAndBrokenClasses()
        {
            var root = MakeGrid();
            var state = new JxInventoryState();
            state.AddItem(Item(1, 0, 0, locked: true));
            state.AddItem(Item(2, 1, 0, genre: (uint)JxItemGenre.Equip, durability: 0));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();

            var el1 = root.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 1);
            var el2 = root.Q<VisualElement>(JxInventoryAdapter.Names.ItemPrefix + 2);
            Assert.IsTrue(el1.ClassListContains(JxInventoryAdapter.LockedClass));
            Assert.IsTrue(el2.ClassListContains(JxInventoryAdapter.BrokenClass));
        }

        [Test]
        public void ClickItem_PublishesAction_WhenItemExists()
        {
            var root = MakeGrid();
            var bus = new FakeBus();
            var state = new JxInventoryState();
            state.AddItem(Item(1, 0, 0));
            var adapter = new JxInventoryAdapter(root, state, bus);
            adapter.Bind();
            Assert.IsTrue(adapter.ClickItem(1));
            Assert.That(bus.ActionRequests.Count, Is.EqualTo(1));
        }

        [Test]
        public void ClickItem_ReturnsFalse_WhenItemMissing()
        {
            var root = MakeGrid();
            var adapter = new JxInventoryAdapter(root, new JxInventoryState(), new FakeBus());
            adapter.Bind();
            Assert.IsFalse(adapter.ClickItem(999));
        }

        [Test]
        public void DragItemTo_EmptyCell_MovesItem()
        {
            var root = MakeGrid();
            var state = new JxInventoryState(cols: 8, rows: 6);
            state.AddItem(Item(1, 0, 0));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();
            Assert.IsTrue(adapter.DragItemTo(1, 3, 3));
            Assert.That(state.Items[1].GridX, Is.EqualTo(3));
            Assert.That(state.Items[1].GridY, Is.EqualTo(3));
        }

        [Test]
        public void DragItemTo_OccupiedCell_Swaps()
        {
            var root = MakeGrid();
            var state = new JxInventoryState(cols: 8, rows: 6);
            state.AddItem(Item(1, 0, 0));
            state.AddItem(Item(2, 5, 5));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();
            // Drag item 1 onto item 2's cell → swap.
            Assert.IsTrue(adapter.DragItemTo(1, 5, 5));
            Assert.That(state.Items[1].GridX, Is.EqualTo(5));
            Assert.That(state.Items[2].GridX, Is.EqualTo(0));
        }

        [Test]
        public void DragItemTo_OutOfBounds_ReturnsFalse()
        {
            var root = MakeGrid();
            var state = new JxInventoryState(cols: 4, rows: 4);
            state.AddItem(Item(1, 0, 0));
            var adapter = new JxInventoryAdapter(root, state, new FakeBus());
            adapter.Bind();
            Assert.IsFalse(adapter.DragItemTo(1, 4, 0));
            Assert.That(state.Items[1].GridX, Is.EqualTo(0));
        }
    }
}
