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
            return new MallPanelSnapshot { rows = System.Array.Empty<MallPanelRow>() };
        }

        public static IReadOnlyList<MallPanelRow> GetForVip(MallService mall, int vipLevel)
        {
            return System.Array.Empty<MallPanelRow>();
        }

        public static IReadOnlyList<MallPanelRow> GetOnSale(MallService mall, long now)
        {
            return System.Array.Empty<MallPanelRow>();
        }

        public static bool TryBuy(MallService mall, int playerId, int mallItemId, int alreadyBoughtToday)
        {
            return false;
        }

        public static int GetEffectivePrice(MallService mall, int mallItemId, int vipLevel)
        {
            return 0;
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
