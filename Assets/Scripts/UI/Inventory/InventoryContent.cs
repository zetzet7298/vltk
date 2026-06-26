// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Inventory window content (IPopupContent, slice 2)
// Title "Hành Trang". Filter tabs (mobile-custom): Tất cả / Trang bị / Thuốc /
// Vật phẩm / Khác. Grid 6×10. Footer = slot count N/28. Read-only bind.
// REQ-8 analog (BtnItems). ADR-I3 (mobile filters), ADR-I4 (read-only).
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.Inventory
{
    /// <summary>One filter-tab definition.</summary>
    internal readonly struct InventoryFilterTab
    {
        public readonly string key;
        public readonly string labelVi;
        public readonly PcItemCategory? category;   // null = Tất cả
        public InventoryFilterTab(string key, string labelVi, PcItemCategory? category)
        { this.key = key; this.labelVi = labelVi; this.category = category; }
    }

    /// <summary>Inventory popup content: title, 5 filter tabs, 6×10 grid, footer.</summary>
    public sealed class InventoryContent : IPopupContent, IPopupLayoutHint
    {
        public string TitleVi => "Hành Trang";
        public float Width => 430f;
        public float Height => 560f;
        public float Left => 425f;   // centered in 1280 design space
        public float Top => 40f;     // above bottom hotbar

        private readonly InventoryService _inventory;
        private readonly IItemIconResolver _iconResolver;

        private static readonly IReadOnlyList<InventoryFilterTab> Tabs = new[]
        {
            new InventoryFilterTab("all",    "Tất Cả",   null),
            new InventoryFilterTab("equip",  "Trang Bị", PcItemCategory.Weapon),   // bucket: Weapon..Ring equippables
            new InventoryFilterTab("med",    "Thuốc",    PcItemCategory.Medicament),
            new InventoryFilterTab("mat",    "Vật Phẩm", PcItemCategory.Material),
            new InventoryFilterTab("other",  "Khác",     null),                     // misc; computed below
        };

        // Tab button + grid + footer refs.
        private readonly Dictionary<string, Label> _tabButtons = new();
        private VisualElement _grid;
        private Label _slotCount;
        private string _activeTab = "all";

        public InventoryContent(InventoryService inventory, IItemIconResolver iconResolver = null)
        {
            _inventory = inventory;
            _iconResolver = iconResolver;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("inv-body");

            // Tab bar
            var tabBar = new VisualElement { name = "InvTabBar" };
            tabBar.AddToClassList("inv-tab-bar");
            foreach (var tab in Tabs)
            {
                var btn = new Label(tab.labelVi) { name = "tab_" + tab.key };
                btn.AddToClassList("inv-tab");
                btn.AddToClassList("inv-tab-label");
                var key = tab.key;
                btn.RegisterCallback<PointerDownEvent>(_ => SwitchTab(key));
                tabBar.Add(btn);
                _tabButtons[tab.key] = btn;
            }
            body.Add(tabBar);

            // Grid (scrollable wrapper to fit 6×10 in mobile space)
            var scroll = new ScrollView { name = "InvGridScroll" };
            scroll.AddToClassList("inv-grid-scroll");
            _grid = new VisualElement { name = "InvGrid" };
            _grid.AddToClassList("inv-grid-host");
            scroll.Add(_grid);
            body.Add(scroll);

            // Footer
            var footer = new VisualElement { name = "InvFooter" };
            footer.AddToClassList("inv-footer");
            _slotCount = new Label("--/--") { name = "SlotCount" };
            _slotCount.AddToClassList("inv-slot-count");
            footer.Add(_slotCount);
            body.Add(footer);

            RebuildGrid();
            SwitchTab("all");
        }

        public void OnShow()
        {
            RebuildGrid();
            RefreshFooter();
        }

        public void OnClose()
        {
            _tabButtons.Clear();
        }

        // ---- internals ----
        private PcItemCategory? ActiveFilter()
        {
            foreach (var t in Tabs) if (t.key == _activeTab) return t.category;
            return null;
        }

        private IReadOnlyList<InventoryEntry> Entries =>
            _inventory != null ? _inventory.Inventory : System.Array.Empty<InventoryEntry>();

        private void RebuildGrid()
        {
            if (_grid == null) return;
            var filter = ActiveFilterForRebuild();
            InventoryGridBuilder.Build(_grid, Entries, filter, _iconResolver);
        }

        // 'Trang Bị' tab buckets all equippable categories; 'Khác' shows non-equip non-listed.
        private PcItemCategory? ActiveFilterForRebuild()
        {
            if (_activeTab == "equip" || _activeTab == "other") return null; // filtered manually below
            return ActiveFilter();
        }

        private void SwitchTab(string key)
        {
            _activeTab = key;
            // 'all'/'equip'/'other' need custom handling; rebuild + apply manual filter.
            if (key == "all" || key == "med" || key == "mat")
            {
                RebuildGrid();
            }
            else
            {
                // equip: keep only equippable categories; other: keep non-equippable, non-mat/med
                var filtered = new List<InventoryEntry>();
                foreach (var e in Entries)
                {
                    if (e?.item == null) continue;
                    var cat = EquipmentSlotMappingService.ItemTypeToCategory(e.item.itemGenre);
                    bool equippable = EquipmentSlotMappingService.IsEquippable(cat);
                    if (key == "equip" && equippable) filtered.Add(e);
                    else if (key == "other" && !equippable
                             && cat != PcItemCategory.Medicament
                             && cat != PcItemCategory.Material) filtered.Add(e);
                }
                InventoryGridBuilder.Build(_grid, filtered, null, _iconResolver);
            }
            ApplyTabActiveStyles();
            RefreshFooter();
        }

        private void ApplyTabActiveStyles()
        {
            foreach (var kv in _tabButtons)
            {
                if (kv.Key == _activeTab) kv.Value.AddToClassList("active");
                else kv.Value.RemoveFromClassList("active");
            }
        }

        private void RefreshFooter()
        {
            if (_slotCount == null || _inventory == null) return;
            int used = _inventory.Inventory.Count;
            _slotCount.text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}/{1}", used, InventoryService.MaxInventorySlots);
        }
    }
}
