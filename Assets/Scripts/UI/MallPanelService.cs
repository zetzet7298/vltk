// -----------------------------------------------------------------------------
// VLTK Mobile — Mall Panel Service (Cửa Hàng VIP)
// Dựng snapshot cho UI cửa hàng VIP. Kết hợp MallService + VIP level + sale.
// Vietnamese: "Cửa Hàng", "VIP", "Giá gốc", "Giá ưu đãi", "Còn hàng", "Giảm giá".
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
            var snap = new MallPanelSnapshot
            {
                playerId = playerId,
                vipLevel = vipLevel,
                totalItems = mall?.Count ?? 0,
                rows = Array.Empty<MallPanelRow>(),
            };
            if (mall == null) return snap;
            var rows = new List<MallPanelRow>();
            int available = 0;
            int onSale = 0;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var entry in EnumerateAll(mall))
            {
                bool onSaleFlag = mall.IsOnSale(entry.mallItemId, now);
                int effPrice = mall.GetEffectivePrice(entry.mallItemId, vipLevel);
                int discount = entry.originalPrice > 0 ? (int)Math.Round((1 - (double)effPrice / entry.originalPrice) * 100) : 0;
                if (onSaleFlag) onSale++;
                if (vipLevel >= entry.requiredVipLevel && entry.stock > 0) available++;
                long endsIn = entry.saleEndUnix > now ? entry.saleEndUnix - now : 0;
                string currency = entry.currency == 0 ? "Vàng" : "KNB";
                rows.Add(new MallPanelRow(entry.mallItemId, entry.itemId, entry.itemName, entry.originalPrice, effPrice, discount, currency, entry.stock, entry.maxBuyPerDay, entry.requiredVipLevel, onSaleFlag, endsIn));
            }
            snap.availableItems = available;
            snap.onSaleItems = onSale;
            snap.rows = rows;
            return snap;
        }

        public static IReadOnlyList<MallPanelRow> GetForVip(MallService mall, int vipLevel)
        {
            if (mall == null) return Array.Empty<MallPanelRow>();
            var list = new List<MallPanelRow>();
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var entry in EnumerateAll(mall))
            {
                if (entry.requiredVipLevel > vipLevel) continue;
                int eff = mall.GetEffectivePrice(entry.mallItemId, vipLevel);
                list.Add(new MallPanelRow(entry.mallItemId, entry.itemId, entry.itemName, entry.originalPrice, eff, 0, "Vàng", entry.stock, entry.maxBuyPerDay, entry.requiredVipLevel, mall.IsOnSale(entry.mallItemId, now), 0));
            }
            return list;
        }

        public static IReadOnlyList<MallPanelRow> GetOnSale(MallService mall, long now)
        {
            if (mall == null) return Array.Empty<MallPanelRow>();
            var list = new List<MallPanelRow>();
            foreach (var entry in EnumerateAll(mall))
            {
                if (!mall.IsOnSale(entry.mallItemId, now)) continue;
                list.Add(new MallPanelRow(entry.mallItemId, entry.itemId, entry.itemName, entry.originalPrice, mall.GetEffectivePrice(entry.mallItemId, 0), 0, "Vàng", entry.stock, entry.maxBuyPerDay, entry.requiredVipLevel, true, 0));
            }
            return list;
        }

        public static bool TryBuy(MallService mall, int playerId, int mallItemId, int alreadyBoughtToday)
        {
            if (mall == null || playerId <= 0 || mallItemId <= 0) return false;
            return mall.TryBuy(mallItemId, playerId, alreadyBoughtToday);
        }

        public static int GetEffectivePrice(MallService mall, int mallItemId, int vipLevel)
        {
            if (mall == null || mallItemId <= 0) return 0;
            return mall.GetEffectivePrice(mallItemId, vipLevel);
        }

        private static IEnumerable<MallEntry> EnumerateAll(MallService mall)
        {
            var field = typeof(MallService).GetField("_reg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(mall) is MallRegistry reg)
            {
                return reg.All;
            }
            return Array.Empty<MallEntry>();
        }
    }

    public class MallEntry
    {
        public int mallItemId;
        public int itemId;
        public string itemName;
        public int originalPrice;
        public int stock;
        public int maxBuyPerDay;
        public int requiredVipLevel;
        public int currency;
        public long saleEndUnix;
    }

    public class MallRegistry
    {
        public IEnumerable<MallEntry> All => Array.Empty<MallEntry>();
    }
}
