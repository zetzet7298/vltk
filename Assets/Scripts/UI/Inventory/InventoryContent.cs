// -----------------------------------------------------------------------------
// VLTK Mobile — PC Hành Trang popup content
// Source: PC UiItem 05ea8560.dat (update03), 214×454, 6×10 ItemBox.
// Art: exact SPR frames vendored under Assets/UI/Popup/Inventory/Art.
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
    /// <summary>Inventory popup content: PC Hành trang sheet, 6×10 grid, live item bind.</summary>
    public sealed class InventoryContent : IPopupContent, IPopupLayoutHint, IPopupChromeHint
    {
        // PC [Main] from 05ea8560.dat.
        public const float PcWidth = 214f;
        public const float PcHeight = 454f;
        public const float ItemBoxLeft = 24f;
        public const float ItemBoxTop = 72f;
        public const float ItemBoxWidth = 168f;
        public const float ItemBoxHeight = 280f;
        public const int ItemBoxColumns = 6;
        public const int ItemBoxRows = 10;

        public string TitleVi => "Hành Trang";
        public float Width => PcWidth;
        public float Height => PcHeight;
        public float Left => (1280f - PcWidth) * 0.5f;
        public float Top => (720f - PcHeight) * 0.5f;
        public PopupChromeKind Chrome => PopupChromeKind.PcInventory;

        private readonly InventoryService _inventory;
        private readonly IItemIconResolver _iconResolver;
        private readonly Action _openStatusPanel;

        private VisualElement _grid;
        private Label _slotCount;

        public InventoryContent(
            InventoryService inventory,
            IItemIconResolver iconResolver = null,
            Action openStatusPanel = null)
        {
            _inventory = inventory;
            _iconResolver = iconResolver;
            _openStatusPanel = openStatusPanel;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("inv-body");

            var panel = new VisualElement { name = "InventoryPanel" };
            panel.AddToClassList("inv-panel");
            body.Add(panel);

            // PC ItemBox: Left=24 Top=72 Width=168 Height=280 HUnits=6 VUnits=10 UnitBorder=1.
            _grid = new VisualElement { name = "InvGrid" };
            _grid.AddToClassList("inv-grid-host");
            panel.Add(_grid);

            // The PC panel has a Money text lane at Left=53 Top=353. Runtime currently
            // exposes item count, not authoritative money, so show used/capacity rather
            // than fabricate currency.
            _slotCount = new Label("0/60") { name = "SlotCount" };
            _slotCount.AddToClassList("inv-slot-count");
            panel.Add(_slotCount);

            var makeAdv = DisabledButton("MakeAdvBtn", "inv-stall-btn inv-stall-adv");
            var markPrice = DisabledButton("MarkPriceBtn", "inv-stall-btn inv-stall-price");
            var makeStall = DisabledButton("MakeStallBtn", "inv-stall-btn inv-stall-toggle");
            panel.Add(makeAdv);
            panel.Add(markPrice);
            panel.Add(makeStall);

            var getMoney = DisabledButton("GetMoneyBtn", "inv-action-btn inv-money-btn");
            panel.Add(getMoney);

            var openStatus = new Button { name = "OpenStatus", text = string.Empty };
            openStatus.AddToClassList("inv-action-btn");
            openStatus.AddToClassList("inv-status-btn");
            openStatus.clicked += () => _openStatusPanel?.Invoke();
            panel.Add(openStatus);

            var close = new Button { name = "Close", text = string.Empty };
            close.AddToClassList("inv-action-btn");
            close.AddToClassList("inv-close-btn");
            panel.Add(close);

            RebuildGrid();
            RefreshFooter();
        }

        public void OnShow()
        {
            RebuildGrid();
            RefreshFooter();
        }

        public void OnClose()
        {
        }

        private IReadOnlyList<InventoryEntry> Entries =>
            _inventory != null ? _inventory.Inventory : Array.Empty<InventoryEntry>();

        private void RebuildGrid()
        {
            if (_grid == null) return;
            InventoryGridBuilder.Build(_grid, Entries, filter: null, iconResolver: _iconResolver);
        }

        private void RefreshFooter()
        {
            if (_slotCount == null) return;
            int used = _inventory != null ? _inventory.Inventory.Count : 0;
            _slotCount.text = string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}/{1}", used, InventoryService.MaxInventorySlots);
        }

        private static Button DisabledButton(string name, string classes)
        {
            var button = new Button { name = name, text = string.Empty };
            foreach (var cls in classes.Split(' '))
                button.AddToClassList(cls);
            button.SetEnabled(false);
            return button;
        }
    }
}
