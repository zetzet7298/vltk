// -----------------------------------------------------------------------------
// VLTK Mobile — JX inventory rendering adapter (UI Toolkit, port KuiItemVN/KuiItem)
//
// Nguồn: addDialogData + AddObject + onTouchItem. Grid render item theo DataX/Y/W/H,
// quality color border (KuiEffect), stack label (vàng), click→tooltip/drag.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Click/drag dùng coordinator
// public (UI Toolkit SendEvent cần live panel).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI.JxCocos
{
    /// <summary>Adapter UI Toolkit cho JX inventory grid.</summary>
    public sealed class JxInventoryAdapter
    {
        private readonly VisualElement _root;
        private readonly JxInventoryState _state;
        private readonly IJxHudCommandBus _bus;

        public static class Names
        {
            public const string Grid = "jx_inventory_grid";
            public const string ItemPrefix = "jx_inv_item_"; // + itemId
            public const string Icon = "jx_inv_icon";
            public const string Stack = "jx_inv_stack";
        }

        public const string EmptyClass = "jx-inv-empty";
        public const string LockedClass = "jx-inv-locked";
        public const string BrokenClass = "jx-inv-broken";

        /// <summary>USS class theo chất lượng equip (port nTempColor border).</summary>
        public static readonly Dictionary<JxItemQuality, string> QualityClass = new()
        {
            { JxItemQuality.Normal, "jx-inv-quality-normal" },
            { JxItemQuality.Purple, "jx-inv-quality-purple" },
            { JxItemQuality.Gold, "jx-inv-quality-gold" },
            { JxItemQuality.Platinum, "jx-inv-quality-platinum" },
        };

        public static string QualityClassFor(JxItemQuality q) =>
            QualityClass.TryGetValue(q, out var c) ? c : QualityClass[JxItemQuality.Normal];

        public JxInventoryAdapter(VisualElement root, JxInventoryState state, IJxHudCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        /// <summary>Bind grid element. Trả về false nếu thiếu grid.</summary>
        public bool Bind()
        {
            if (Find(_root, Names.Grid) == null) return false;
            Render();
            return true;
        }

        /// <summary>Render state → 1 VisualElement mỗi item, định vị theo grid.</summary>
        public void Render(float parentHeight = 0f)
        {
            var grid = Find(_root, Names.Grid);
            if (grid == null) return;

            // Track existing item elements to reuse/cleanup.
            var existing = new HashSet<string>();
            foreach (var kv in _state.Items)
            {
                var item = kv.Value;
                var name = Names.ItemPrefix + item.ItemId;
                existing.Add(name);
                var el = Find(grid, name) ?? CreateItemElement(grid, name);
                ApplyItemStyle(el, item, parentHeight);
            }
            // Remove elements no longer present.
            for (int i = grid.childCount - 1; i >= 0; i--)
            {
                var child = grid[i];
                if (child.name != null && child.name.StartsWith(Names.ItemPrefix, StringComparison.Ordinal)
                    && !existing.Contains(child.name))
                {
                    grid.RemoveAt(i);
                }
            }
        }

        private static VisualElement CreateItemElement(VisualElement grid, string name)
        {
            var el = new VisualElement { name = name };
            el.Add(new VisualElement { name = Names.Icon });
            el.Add(new Label { name = Names.Stack });
            grid.Add(el);
            return el;
        }

        private static void ApplyItemStyle(VisualElement el, JxInventoryItem item, float parentHeight)
        {
            // Pixel placement (cocos Y-flip).
            var (px, py) = JxInventoryState.GridToPixelParent(item, parentHeight);
            // UI Toolkit uses left/top from parent top-left; parentHeight-flip already
            // applied in GridToPixelParent so py is top-distance. Center the element.
            float w = item.Width * JxInventoryState.CellSize;
            float h = item.Height * JxInventoryState.CellSize;
            el.style.left = px - w / 2f;
            el.style.top = py - h / 2f;
            el.style.width = w;
            el.style.height = h;

            // Quality class (equip).
            el.EnableInClassList(QualityClassFor(item.Quality),
                item.Genre == (uint)JxItemGenre.Equip);
            // Locked.
            el.EnableInClassList(LockedClass, item.Locked);
            // Broken.
            el.EnableInClassList(BrokenClass, JxInventoryState.IsBrokenEquip(item));

            // Stack label.
            var stack = Find(el, Names.Stack) as Label;
            if (stack != null)
            {
                bool showStack = JxInventoryState.ShowStackLabel(item);
                stack.style.display = showStack ? DisplayStyle.Flex : DisplayStyle.None;
                stack.text = showStack ? item.Stack.ToString() : string.Empty;
            }
        }

        /// <summary>Coordinator click item → mở tooltip (via command bus/panel). Trả về false nếu không có item.</summary>
        public bool ClickItem(int itemId)
        {
            if (!_state.TryGetItem(itemId, out _)) return false;
            // Controller nhận lệnh mở tooltip (hook riêng). Tooltip dùng itemId.
            _bus.PublishActionRequested(JxHudAction.Interact);
            return true;
        }

        /// <summary>Coordinator drag item đến ô mới (swap if occupied). Trả về false nếu move/swap thất bại.</summary>
        public bool DragItemTo(int itemId, int targetGridX, int targetGridY)
        {
            if (!_state.TryGetItem(itemId, out _)) return false;
            // Ô đích có item khác → swap; không → move.
            foreach (var kv in _state.Items)
            {
                if (kv.Key == itemId) continue;
                var other = kv.Value;
                if (targetGridX >= other.GridX && targetGridX < other.GridX + other.Width
                    && targetGridY >= other.GridY && targetGridY < other.GridY + other.Height)
                {
                    return _state.SwapItems(itemId, kv.Key);
                }
            }
            return _state.MoveItem(itemId, targetGridX, targetGridY);
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
