// -----------------------------------------------------------------------------
// VLTK Mobile — JX immedicy box EditMode tests (port of KuiItemImmediaBoxVN.cpp)
// Verifies: 3 slots, HoldObject_ add/remove (nameID==0 skip), stack count <=0 auto-
// clear (ApplyRemoveItemRef), UseItem decrements + auto-clear at 0, render empty/
// occupied + stack label, click guards. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxImmedicyBoxTests
    {
        private class FakeBus : IJxHudCommandBus
        {
            public List<JxHudPanel> PanelRequests = new();
            public List<JxHudAction> ActionRequests = new();
            public void PublishPanelRequested(JxHudPanel panel) => PanelRequests.Add(panel);
            public void PublishActionRequested(JxHudAction action) => ActionRequests.Add(action);
        }

        // ---- Port constants ----

        [Test]
        public void SlotCount_Is3_PerSource()
        {
            // Nguồn: BoxIndex 0..2.
            Assert.That(JxImmedicyBoxState.SlotCount, Is.EqualTo(3));
        }

        [Test]
        public void SlotSize_Is52_PerSource()
        {
            // Nguồn: colorsize 52x52.
            Assert.That(JxImmedicyBoxState.SlotSize, Is.EqualTo(52));
        }

        // ---- HoldObject_ add/remove ----

        [Test]
        public void SetItem_AddsItem_SkipsZeroNameId()
        {
            // nameID==0 → no-op (source: if (nameID==0) return).
            var s = new JxImmedicyBoxState();
            Assert.IsFalse(s.SetItem(1, 0, "spr/x.spr"));
            Assert.IsTrue(s.SetItem(1, 500, "spr/potion.spr", 7));
            Assert.That(s.Slot(1).NameId, Is.EqualTo(500));
            Assert.That(s.Slot(1).IconPath, Is.EqualTo("spr/potion.spr"));
            Assert.That(s.Slot(1).Genre, Is.EqualTo(7u));
        }

        [Test]
        public void SetItem_RejectsBadIndex()
        {
            var s = new JxImmedicyBoxState();
            Assert.IsFalse(s.SetItem(-1, 1, "x"));
            Assert.IsFalse(s.SetItem(3, 1, "x"));
        }

        [Test]
        public void ClearItem_RemovesContent()
        {
            var s = new JxImmedicyBoxState();
            s.SetItem(0, 100, "spr/a.spr");
            s.SetStackCount(0, 5);
            Assert.IsTrue(s.ClearItem(0));
            Assert.That(s.Slot(0).NameId, Is.EqualTo(0));
            Assert.That(s.Slot(0).StackCount, Is.EqualTo(0));
        }

        // ---- Stack count auto-clear (ApplyRemoveItemRef) ----

        [Test]
        public void SetStackCount_ZeroOrNegative_AutoClearsSlot()
        {
            // Nguồn: if (nAllstackCount <= 0) ApplyRemoveItemRef.
            var s = new JxImmedicyBoxState();
            s.SetItem(2, 200, "spr/b.spr");
            s.SetStackCount(2, 10);
            s.SetStackCount(2, 0);
            Assert.That(s.Slot(2).NameId, Is.EqualTo(0));
            Assert.That(s.Slot(2).StackCount, Is.EqualTo(0));
        }

        [Test]
        public void SetStackCount_Positive_KeepsItem()
        {
            var s = new JxImmedicyBoxState();
            s.SetItem(0, 200, "spr/b.spr");
            s.SetStackCount(0, 7);
            Assert.That(s.Slot(0).StackCount, Is.EqualTo(7));
            Assert.That(s.Slot(0).NameId, Is.EqualTo(200));
        }

        // ---- UseItem ----

        [Test]
        public void UseItem_DecrementsStack_AndClearsAtZero()
        {
            var s = new JxImmedicyBoxState();
            s.SetItem(1, 300, "spr/c.spr");
            s.SetStackCount(1, 3);
            Assert.IsTrue(s.UseItem(1));
            Assert.That(s.Slot(1).StackCount, Is.EqualTo(2));
            Assert.IsTrue(s.UseItem(1));
            Assert.IsTrue(s.UseItem(1));
            // Stack cạn → auto-clear.
            Assert.That(s.Slot(1).NameId, Is.EqualTo(0));
            // Use tiếp → false (trống).
            Assert.IsFalse(s.UseItem(1));
        }

        [Test]
        public void UseItem_EmptySlot_ReturnsFalse()
        {
            var s = new JxImmedicyBoxState();
            Assert.IsFalse(s.UseItem(0));
        }

        [Test]
        public void IsOccupied_RequiresNameIdAndStack()
        {
            var s = new JxImmedicyBoxState();
            Assert.IsFalse(JxImmedicyBoxState.IsOccupied(s.Slot(0)));
            s.SetItem(0, 1, "x");
            // stack 0 → not occupied.
            Assert.IsFalse(JxImmedicyBoxState.IsOccupied(s.Slot(0)));
            s.SetStackCount(0, 1);
            Assert.IsTrue(JxImmedicyBoxState.IsOccupied(s.Slot(0)));
        }

        // ---- Adapter: render + click ----

        private static VisualElement MakeBox()
        {
            var root = new VisualElement();
            var box = new VisualElement { name = JxImmedicyBoxAdapter.Names.Box };
            for (int i = 0; i < JxImmedicyBoxState.SlotCount; i++)
            {
                var slot = new VisualElement { name = JxImmedicyBoxAdapter.Names.SlotPrefix + i };
                slot.Add(new VisualElement { name = JxImmedicyBoxAdapter.Names.Icon });
                slot.Add(new Label { name = JxImmedicyBoxAdapter.Names.Stack });
                box.Add(slot);
            }
            root.Add(box);
            return root;
        }

        [Test]
        public void Adapter_Bind_ReturnsTrue_WhenBoxPresent()
        {
            var adapter = new JxImmedicyBoxAdapter(MakeBox(), new JxImmedicyBoxState(), new FakeBus());
            Assert.IsTrue(adapter.Bind());
        }

        [Test]
        public void Adapter_Render_EmptySlotsGetEmptyClass()
        {
            var root = MakeBox();
            var adapter = new JxImmedicyBoxAdapter(root, new JxImmedicyBoxState(), new FakeBus());
            adapter.Bind();
            var slot0 = root.Q<VisualElement>(JxImmedicyBoxAdapter.Names.SlotPrefix + 0);
            Assert.IsTrue(slot0.ClassListContains(JxImmedicyBoxAdapter.EmptyClass));
        }

        [Test]
        public void Adapter_Render_OccupiedShowsStackLabel()
        {
            var root = MakeBox();
            var state = new JxImmedicyBoxState();
            state.SetItem(1, 800, "spr/p.spr");
            state.SetStackCount(1, 42);
            var adapter = new JxImmedicyBoxAdapter(root, state, new FakeBus());
            adapter.Bind();
            var slot1 = root.Q<VisualElement>(JxImmedicyBoxAdapter.Names.SlotPrefix + 1);
            Assert.IsTrue(slot1.ClassListContains(JxImmedicyBoxAdapter.OccupiedClass));
            var stack = slot1.Q<Label>(JxImmedicyBoxAdapter.Names.Stack);
            Assert.That(stack.text, Is.EqualTo("42"));
        }

        [Test]
        public void Adapter_Click_UsesItemAndPublishes()
        {
            var root = MakeBox();
            var bus = new FakeBus();
            var state = new JxImmedicyBoxState();
            state.SetItem(2, 900, "spr/q.spr");
            state.SetStackCount(2, 5);
            var adapter = new JxImmedicyBoxAdapter(root, state, bus);
            adapter.Bind();
            Assert.IsTrue(adapter.Click(2));
            Assert.That(state.Slot(2).StackCount, Is.EqualTo(4));
            Assert.That(bus.ActionRequests.Count, Is.EqualTo(1));
            // Stack label re-rendered.
            var stack = root.Q<VisualElement>(JxImmedicyBoxAdapter.Names.SlotPrefix + 2)
                .Q<Label>(JxImmedicyBoxAdapter.Names.Stack);
            Assert.That(stack.text, Is.EqualTo("4"));
        }

        [Test]
        public void Adapter_Click_EmptyReturnsFalse()
        {
            var root = MakeBox();
            var bus = new FakeBus();
            var adapter = new JxImmedicyBoxAdapter(root, new JxImmedicyBoxState(), bus);
            adapter.Bind();
            Assert.IsFalse(adapter.Click(0));
            Assert.That(bus.ActionRequests.Count, Is.EqualTo(0));
        }
    }
}
