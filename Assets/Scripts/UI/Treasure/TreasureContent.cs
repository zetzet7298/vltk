// -----------------------------------------------------------------------------
// VLTK Mobile — PC Treasure / Kỳ Trân Các popup content
// Source: PC 9e5f75d1.dat (Bao vat), background.spr 6e2472fd, 563×476.
// Art: exact SPR frames vendored under Assets/UI/Popup/Treasure/Art.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.Treasure
{
    /// <summary>Popup body for BtnTreasure: PC Kỳ Trân Các sheet with live mall rows.</summary>
    public sealed class TreasureContent : IPopupContent, IPopupLayoutHint, IPopupChromeHint
    {
        // PC [Main] Image=\spr\ui3\market\background.spr extracted frame size.
        public const float PcWidth = 563f;
        public const float PcHeight = 476f;
        public const float GoodsLeft = 18f;
        public const float GoodsTop = 76f;
        public const float GoodsWidth = 171f;
        public const float GoodsHeight = 74f;
        public const float GoodsGapX = 5f;
        public const float GoodsGapY = 4f;
        public const int GoodsColumns = 3;
        public const int GoodsRows = 4;

        public string TitleVi => "Bảo Vật";
        public float Width => PcWidth;
        public float Height => PcHeight;
        public float Left => (1280f - PcWidth) * 0.5f;
        public float Top => (720f - PcHeight) * 0.5f;
        public PopupChromeKind Chrome => PopupChromeKind.PcTreasure;

        private readonly MallService _mall;
        private readonly TreasureHuntService _treasureHunt;
        private readonly int _playerId;
        private readonly int _vipLevel;
        private readonly int _currentMapId;
        private readonly float _posX;
        private readonly float _posY;

        private VisualElement _goodsLayer;
        private Label _pageInfo;
        private Label _huntStatus;

        public TreasureContent(
            MallService mall,
            TreasureHuntService treasureHunt,
            int playerId = 1,
            int vipLevel = 0,
            int currentMapId = 0,
            float posX = 0f,
            float posY = 0f)
        {
            _mall = mall;
            _treasureHunt = treasureHunt;
            _playerId = playerId;
            _vipLevel = vipLevel;
            _currentMapId = currentMapId;
            _posX = posX;
            _posY = posY;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("treasure-body");

            var panel = new VisualElement { name = "TreasurePanel" };
            panel.AddToClassList("treasure-panel-pc");
            body.Add(panel);

            // [SellType] Left=5 Top=30 Width=62 Height=20. PC category backend is not
            // exposed yet; keep the real tab art visible but disabled instead of faking.
            var sellType = SpriteButton("SellType", "treasure-small-btn treasure-sell-type", "Vật phẩm", enabled: false);
            panel.Add(sellType);

            // [LeftBtn]/[RightBtn] from PC. Pagination will become live when mall paging lands.
            panel.Add(SpriteButton("LeftBtn", "treasure-nav-btn treasure-left-btn", "Trang trước", enabled: false));
            panel.Add(SpriteButton("RightBtn", "treasure-nav-btn treasure-right-btn", "Trang kế", enabled: false));

            _pageInfo = new Label("1/1") { name = "PageInfo" };
            _pageInfo.AddToClassList("treasure-page-info");
            panel.Add(_pageInfo);

            // [ShoppingCart] PC button. Cart mutation/backend is not present in this slice.
            panel.Add(SpriteButton("ShoppingCart", "treasure-nav-btn treasure-cart-btn", "Giỏ hàng", enabled: false));

            _goodsLayer = new VisualElement { name = "MarketGoodsLayer" };
            _goodsLayer.AddToClassList("treasure-goods-layer");
            panel.Add(_goodsLayer);

            // The free_close_radius.spr referenced by PC is absent from the canonical unpack.
            // Keep the PC hit zone and let the baked top-right frame remain untouched.
            var close = new Button { name = "Close", text = string.Empty };
            close.AddToClassList("treasure-close-hit");
            panel.Add(close);

            _huntStatus = new Label(string.Empty) { name = "TreasureHuntStatus" };
            _huntStatus.AddToClassList("treasure-hunt-status");
            panel.Add(_huntStatus);

            Refresh();
        }

        public void OnShow() => Refresh();

        public void OnClose()
        {
            _goodsLayer = null;
            _pageInfo = null;
            _huntStatus = null;
        }

        private void Refresh()
        {
            if (_goodsLayer == null) return;

            var mall = MallPanelService.BuildSnapshot(_mall, _playerId, _vipLevel);
            var treasure = TreasureHuntPanelService.BuildSnapshot(_treasureHunt, _playerId, _currentMapId, _posX, _posY);
            var rows = mall.rows ?? Array.Empty<MallPanelRow>();

            _goodsLayer.Clear();
            if (rows.Count == 0)
            {
                _goodsLayer.Add(MakeEmptyGoodsCard());
            }
            else
            {
                int count = Math.Min(rows.Count, GoodsColumns * GoodsRows);
                for (int i = 0; i < count; i++)
                    _goodsLayer.Add(MakeGoodsCard(rows[i], i));
            }

            if (_pageInfo != null)
            {
                int pages = Math.Max(1, (rows.Count + GoodsColumns * GoodsRows - 1) / (GoodsColumns * GoodsRows));
                _pageInfo.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "1/{0}", pages);
            }

            if (_huntStatus != null)
            {
                _huntStatus.text = string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "Kho báu gần: {0}/{1}  Map {2}",
                    treasure.nearbyTreasures,
                    treasure.totalTreasures,
                    treasure.currentMapId);
            }
        }

        private static VisualElement MakeEmptyGoodsCard()
        {
            var card = BaseGoodsCard(0);
            card.Add(MakeLabel("Chưa có dữ liệu", "treasure-goods-name"));
            card.Add(MakeLabel("Kỳ Trân Các", "treasure-price-label"));
            card.Add(MakeLabel("MallService chưa sẵn sàng", "treasure-price-value"));
            var buy = SpriteButton("Buy0", "treasure-buy-btn", "Mua", enabled: false);
            card.Add(buy);
            return card;
        }

        private static VisualElement MakeGoodsCard(MallPanelRow row, int index)
        {
            var card = BaseGoodsCard(index);

            var itemBox = new VisualElement { name = "ItemBox" };
            itemBox.AddToClassList("treasure-item-box");
            itemBox.Add(MakeLabel(row.itemId.ToString(System.Globalization.CultureInfo.InvariantCulture), "treasure-item-id"));
            card.Add(itemBox);

            card.Add(MakeLabel(string.IsNullOrEmpty(row.itemName) ? "Vật phẩm" : row.itemName, "treasure-goods-name"));
            card.Add(MakeLabel("Giá gốc", "treasure-original-label"));
            card.Add(MakeLabel(row.originalPrice.ToString(System.Globalization.CultureInfo.InvariantCulture), "treasure-original-value"));
            card.Add(MakeLabel("Giá", "treasure-price-label"));
            card.Add(MakeLabel(FormatPrice(row), "treasure-price-value"));

            if (row.discount > 0 || row.isOnSale)
            {
                var discount = new VisualElement { name = "DiscountBadge" };
                discount.AddToClassList("treasure-discount-badge");
                card.Add(discount);
            }
            else if (index == 0)
            {
                var newer = new VisualElement { name = "NewArrivalBadge" };
                newer.AddToClassList("treasure-new-badge");
                card.Add(newer);
            }

            var buy = SpriteButton("Buy" + index.ToString(System.Globalization.CultureInfo.InvariantCulture), "treasure-buy-btn", "Mua", enabled: false);
            card.Add(buy);
            return card;
        }

        private static VisualElement BaseGoodsCard(int index)
        {
            var card = new VisualElement { name = "MarketGoods" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) };
            card.AddToClassList("treasure-goods-card");
            int col = index % GoodsColumns;
            int row = index / GoodsColumns;
            card.style.left = GoodsLeft + col * (GoodsWidth + GoodsGapX);
            card.style.top = GoodsTop + row * (GoodsHeight + GoodsGapY);
            return card;
        }

        private static string FormatPrice(MallPanelRow row)
            => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} {1}", row.effectivePrice, row.currency);

        private static Label MakeLabel(string text, string cls)
        {
            var label = new Label(text);
            label.AddToClassList(cls);
            return label;
        }

        private static Button SpriteButton(string name, string classes, string text, bool enabled)
        {
            var button = new Button { name = name, text = text ?? string.Empty };
            foreach (var cls in classes.Split(' '))
                button.AddToClassList(cls);
            button.SetEnabled(enabled);
            return button;
        }
    }
}
