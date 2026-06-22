// -----------------------------------------------------------------------------
// VLTK Mobile — Bag/Inventory panel vltkunity adapter
// Phase 2 port of vltkunity's Storage.cs + EquimentItem.cs. Renders a grid of
// inventory slots with item rarity frames, a bag count label, and supports
// item selection + move-to-storage intents through UI Toolkit.
//
// vltkunity source mapping:
//   Storage.ListBag GridLayoutGroup (200 slots)    → Slot grid
//   Storage.TextBags count display                  → Count label
//   Storage.ListStorage (200 slots)                  → Storage tab
//   EquimentItem.SetItemEquiment (image + frame)     → Slot rendering
//   EquimentItem.OpenDetail → MoveItemToStorage      → SelectItem intent
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace VLTK.UI
{
    public readonly struct BagItemRow
    {
        public readonly int slotIndex;
        public readonly int itemId;
        public readonly string displayName;
        public readonly string rarity;
        public readonly string summary;

        public BagItemRow(int slotIndex, int itemId, string displayName, string rarity, string summary)
        {
            this.slotIndex = slotIndex;
            this.itemId = itemId;
            this.displayName = displayName ?? string.Empty;
            this.rarity = rarity ?? "White";
            this.summary = summary ?? string.Empty;
        }
    }

    public sealed class BagAdapterSnapshot
    {
        public int usedSlots;
        public int totalSlots;
        public IReadOnlyList<BagItemRow> items;
    }

    public sealed class BagPanelVltkUnityAdapter : IDisposable
    {
        private readonly VisualElement _root;
        private readonly IBagCommandBus _bus;

        private VisualElement _bagGrid;
        private VisualElement _storageGrid;
        private Label _bagCountLabel;
        private Label _storageCountLabel;
        private VisualElement _closeBtn;
        private VisualElement _tabBagBtn;
        private VisualElement _tabStorageBtn;

        private int _activeTab;
        private BagAdapterSnapshot _snapshot;

        public const int TabBag = 0;
        public const int TabStorage = 1;

        public int RenderCount { get; private set; }
        public int ActiveTab => _activeTab;

        // vltkunity rarity → frame color mapping (matches Bag/itembase*.png set)
        private static readonly Dictionary<string, string> RarityFrameColors = new()
        {
            { "White",     "#CCCCCC" },
            { "Green",     "#44FF44" },
            { "Blue",      "#4488FF" },
            { "Purple",    "#AA44FF" },
            { "Gold",      "#FFD700" },
            { "Orange",    "#FF8800" },
            { "Pink",      "#FF77BB" },
            { "Platinum",  "#E5E4E2" },
            { "Red",       "#FF4444" },
        };

        public BagPanelVltkUnityAdapter(VisualElement root, IBagCommandBus bus)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
        }

        public void Bind()
        {
            CacheElements();
            WireButtons();
            ShowTab(TabBag);
        }

        private void CacheElements()
        {
            if (_root == null) return;
            _bagGrid = FindByName("VltkBagGrid");
            _storageGrid = FindByName("VltkStorageGrid");
            _bagCountLabel = FindByName("VltkBagCount") as Label;
            _storageCountLabel = FindByName("VltkStorageCount") as Label;
            _closeBtn = FindByName("VltkBagCloseBtn");
            _tabBagBtn = FindByName("VltkBagTabBagBtn");
            _tabStorageBtn = FindByName("VltkBagTabStorageBtn");
        }

        private void WireButtons()
        {
            var bus = _bus;
            if (bus == null) return;
            _closeClick = bus.PublishBagCloseRequested;
            RegisterClick(_closeBtn, _closeClick);
            RegisterClick(_tabBagBtn, () => ShowTab(TabBag));
            RegisterClick(_tabStorageBtn, () => ShowTab(TabStorage));
        }

        private System.Action _closeClick;

        public void SimulateCloseClick() => _closeClick?.Invoke();
        public void SimulateTabSwitch(int tabIndex) => ShowTab(tabIndex);
        public void SimulateItemClick(int slotIndex) => SelectItem(slotIndex);

        private void ShowTab(int tabIndex)
        {
            _activeTab = tabIndex;
            _bus.PublishBagTabChanged(tabIndex);
            SetVisible(_bagGrid, tabIndex == TabBag);
            SetVisible(_storageGrid, tabIndex == TabStorage);
            if (_bagCountLabel != null)
            {
                var parent = _bagCountLabel.parent;
                SetVisible(parent, tabIndex == TabBag);
            }
        }

        private static void SetVisible(VisualElement el, bool visible)
        {
            if (el == null) return;
            el.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RegisterClick(VisualElement target, Action handler)
        {
            if (target == null || handler == null) return;
            target.pickingMode = PickingMode.Position;
            target.RegisterCallback<ClickEvent>(_ => handler());
        }

        /// <summary>Apply a snapshot to populate the bag grid + count label.</summary>
        public void Apply(BagAdapterSnapshot snapshot)
        {
            RenderCount++;
            _snapshot = snapshot;
            if (snapshot == null) return;

            if (_bagCountLabel != null)
                _bagCountLabel.text = $"{snapshot.usedSlots}/{snapshot.totalSlots}";
            if (_storageCountLabel != null)
                _storageCountLabel.text = $"{0}/{snapshot.totalSlots}";

            if (_bagGrid != null && snapshot.items != null)
            {
                _bagGrid.Clear();
                foreach (var item in snapshot.items)
                {
                    var slot = CreateItemSlot(item);
                    _bagGrid.Add(slot);
                }
            }
        }

        private VisualElement CreateItemSlot(BagItemRow item)
        {
            var slot = new VisualElement();
            slot.style.width = 48;
            slot.style.height = 48;
            slot.style.marginRight = 4;
            slot.style.marginBottom = 4;
            slot.style.backgroundColor = new UnityEngine.Color(0.15f, 0.15f, 0.2f, 0.8f);
            slot.style.borderLeftWidth = 2;
            slot.style.borderRightWidth = 2;
            slot.style.borderTopWidth = 2;
            slot.style.borderBottomWidth = 2;

            string colorHex = RarityFrameColors.TryGetValue(item.rarity, out var hex) ? hex : "#CCCCCC";
            slot.style.borderLeftColor = FromHex(colorHex);
            slot.style.borderRightColor = FromHex(colorHex);
            slot.style.borderTopColor = FromHex(colorHex);
            slot.style.borderBottomColor = FromHex(colorHex);

            var name = new Label(item.displayName);
            name.style.fontSize = 9;
            name.style.color = UnityEngine.Color.white;
            name.style.unityTextAlign = UnityEngine.TextAnchor.MiddleCenter;
            name.style.flexGrow = 1;
            slot.Add(name);

            var captured = item;
            slot.RegisterCallback<ClickEvent>(_ => SelectItem(captured.slotIndex));

            return slot;
        }

        private void SelectItem(int slotIndex)
        {
            _bus.PublishItemSelected(slotIndex);
        }

        private static UnityEngine.Color FromHex(string hex)
        {
            if (UnityEngine.ColorUtility.TryParseHtmlString(hex, out var color))
                return color;
            return UnityEngine.Color.white;
        }

        private VisualElement FindByName(string name)
        {
            if (_root == null) return null;
            var queue = new Queue<VisualElement>();
            queue.Enqueue(_root);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.name == name) return current;
                int childCount = current.childCount;
                for (int i = 0; i < childCount; i++)
                    queue.Enqueue(current[i]);
            }
            return null;
        }

        public void Dispose()
        {
        }
    }
}
