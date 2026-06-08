// -----------------------------------------------------------------------------
// VLTK Mobile — Mall Panel Service (Cửa Hàng VIP)
// Dựng snapshot cho UI cửa hàng VIP từ MallService.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct MallPanelRow
    {
        public readonly int mallItemId;
        public readonly int itemId;
        public readonly string itemName;
        public readonly int originalPrice;
        public readonly int effectivePrice;
        public readonly int discount;
        public readonly string currency;
        public readonly int stock;
        public readonly int maxBuy;
        public readonly int vipLevel;
        public readonly bool isOnSale;
        public readonly long endsInSec;

        public MallPanelRow(int mallItemId, int itemId, string itemName, int originalPrice, int effectivePrice, int discount, string currency, int stock, int maxBuy, int vipLevel, bool isOnSale, long endsInSec)
        {
            this.mallItemId = mallItemId;
            this.itemId = itemId;
            this.itemName = itemName;
            this.originalPrice = originalPrice;
            this.effectivePrice = effectivePrice;
            this.discount = discount;
            this.currency = currency;
            this.stock = stock;
            this.maxBuy = maxBuy;
            this.vipLevel = vipLevel;
            this.isOnSale = isOnSale;
            this.endsInSec = endsInSec;
        }
    }

    public sealed class MallPanelSnapshot
    {
        public int playerId;
        public int vipLevel;
        public int totalItems;
        public int availableItems;
        public int onSaleItems;
        public IReadOnlyList<MallPanelRow> rows;
    }

    public static class MallPanelService
    {
        public const string LabelMall = "Cửa Hàng";
        public const string LabelVip = "VIP";
        public const string LabelOriginal = "Giá gốc";
        public const string LabelEffective = "Giá ưu đãi";
        public const string LabelStock = "Còn hàng";
        public const string LabelDiscount = "Giảm giá";
        public const string LabelBuy = "Mua";

        public static MallPanelSnapshot BuildSnapshot(MallService mall, int playerId, int vipLevel)
        {
            if (mall == null)
                return new MallPanelSnapshot { playerId = playerId, vipLevel = vipLevel, rows = Array.Empty<MallPanelRow>() };

            var all = mall.All;
            var rows = GetForVip(mall, vipLevel);
            int onSale = 0;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var e in all)
                if (mall.IsOnSale(e.mallItemId, now)) onSale++;

            return new MallPanelSnapshot
            {
                playerId = playerId,
                vipLevel = vipLevel,
                totalItems = all.Count,
                availableItems = rows.Count,
                onSaleItems = onSale,
                rows = rows
            };
        }

        public static IReadOnlyList<MallPanelRow> GetForVip(MallService mall, int vipLevel)
        {
            if (mall == null)
                return Array.Empty<MallPanelRow>();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var entries = mall.GetForVip(vipLevel);
            var rows = new List<MallPanelRow>(entries.Count);
            foreach (var e in entries)
                rows.Add(ToRow(mall, e, vipLevel, now));
            return rows;
        }

        public static IReadOnlyList<MallPanelRow> GetOnSale(MallService mall, long now)
        {
            if (mall == null)
                return Array.Empty<MallPanelRow>();
            var rows = new List<MallPanelRow>();
            foreach (var e in mall.All)
                if (mall.IsOnSale(e.mallItemId, now)) rows.Add(ToRow(mall, e, e.requiredVipLevel, now));
            return rows;
        }

        public static bool TryBuy(MallService mall, int playerId, int mallItemId, int alreadyBoughtToday)
        {
            if (mall == null || playerId <= 0 || mallItemId <= 0)
                return false;
            return mall.CanBuy(mallItemId, 0, alreadyBoughtToday);
        }

        public static int GetEffectivePrice(MallService mall, int mallItemId, int vipLevel)
            => mall == null ? 0 : Math.Max(0, mall.GetEffectivePrice(mallItemId, vipLevel));

        private static MallPanelRow ToRow(MallService mall, PcMallEntry e, int vipLevel, long now)
        {
            long endsIn = e.endTimeUnix > 0 ? Math.Max(0, e.endTimeUnix - now) : 0;
            return new MallPanelRow(e.mallItemId, e.itemId, $"Vật phẩm #{e.itemId}", e.price,
                Math.Max(0, mall.GetEffectivePrice(e.mallItemId, vipLevel)), e.discount,
                CurrencyName(e.currency), e.stock, e.maxBuyPerDay, e.requiredVipLevel,
                mall.IsOnSale(e.mallItemId, now), endsIn);
        }

        private static string CurrencyName(int currency)
        {
            return currency switch
            {
                MallService.CurrencyCoin => "Đồng",
                MallService.CurrencyGold => "Vàng",
                MallService.CurrencyBind => "Đồng khóa",
                MallService.CurrencyVND => "VND",
                _ => "Tiền tệ",
            };
        }
    }
}
