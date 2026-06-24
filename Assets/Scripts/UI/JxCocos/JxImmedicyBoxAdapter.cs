// -----------------------------------------------------------------------------
// VLTK Mobile — JX immedicy box rendering adapter (UI Toolkit, port KuiItemImmediaBoxVN.cpp)
//
// Nguồn: HoldObject_/ImmediaCallback. 3 ô dọc, mỗi ô = item icon + stack count
// (vàng, căn phải). Click ô → UseItem. Ô trống = mờ.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Click dùng coordinator public.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI.JxCocos
{
    /// <summary>Adapter UI Toolkit cho 3 ô item dùng ngay.</summary>
    public sealed class JxImmedicyBoxAdapter
    {
        private readonly VisualElement _root;
        private readonly JxImmedicyBoxState _state;
        private readonly IJxHudCommandBus _bus;

        public static class Names
        {
            public const string Box = "jx_immedicy_box";
            public const string SlotPrefix = "jx_immedicy_slot_"; // + boxIndex
            public const string Icon = "jx_immedicy_icon";
            public const string Stack = "jx_immedicy_stack";
        }

        public const string EmptyClass = "jx-immedicy-empty";
        public const string OccupiedClass = "jx-immedicy-occupied";

        public JxImmedicyBoxAdapter(VisualElement root, JxImmedicyBoxState state, IJxHudCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>Bind box element. Trả về false nếu thiếu box.</summary>
        public bool Bind()
        {
            if (Find(_root, Names.Box) == null) return false;
            Render();
            return true;
        }

        /// <summary>Render state → 3 ô icon + stack label.</summary>
        public void Render()
        {
            var box = Find(_root, Names.Box);
            if (box == null) return;
            for (int i = 0; i < JxImmedicyBoxState.SlotCount; i++)
            {
                var slotEl = Find(box, Names.SlotPrefix + i);
                if (slotEl == null) continue;
                var slot = _state.Slot(i);
                bool occupied = JxImmedicyBoxState.IsOccupied(slot);
                slotEl.EnableInClassList(EmptyClass, !occupied);
                slotEl.EnableInClassList(OccupiedClass, occupied);

                var stack = Find(slotEl, Names.Stack) as Label;
                if (stack != null)
                    stack.text = occupied ? slot.StackCount.ToString() : string.Empty;
            }
        }

        /// <summary>Coordinator click ô → UseItem (giảm stack). Trả về false nếu trống.</summary>
        public bool Click(int boxIndex)
        {
            if (boxIndex < 0 || boxIndex >= JxImmedicyBoxState.SlotCount) return false;
            if (!JxImmedicyBoxState.IsOccupied(_state.Slot(boxIndex))) return false;
            bool used = _state.UseItem(boxIndex);
            if (used)
            {
                // Controller nhận lệnh dùng item (hook riêng qua action bus).
                _bus.PublishActionRequested(JxHudAction.Exchange);
                Render();
            }
            return used;
        }

        private static VisualElement Find(VisualElement root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name)) return null;
            var q = new Queue<VisualElement>();
            q.Enqueue(root);
            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                if (cur.name == name) return cur;
                int n = cur.childCount;
                for (int i = 0; i < n; i++) q.Enqueue(cur[i]);
            }
            return null;
        }
    }
}
