// -----------------------------------------------------------------------------
// VLTK Mobile — Stall Panel Service (Gian hàng cá nhân)
// UI service: dựng panel bày bán, thêm/gỡ vật phẩm, đặt giá.
// PC reference: StallService + PcStallEntry từ settings/item/stall.txt.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một ô trong gian hàng.</summary>
    public readonly struct StallPanelRow
    {
        public readonly int slotIdx;
        public readonly int itemId;
        public readonly string itemName;
        public readonly int count;
        public readonly int price;
        public readonly int currency; // 0=đồng, 1=bạc, 2=vàng, 3=KNB
        public readonly bool isLocked;

        public StallPanelRow(int slotIdx, int itemId, string itemName, int count, int price, int currency, bool isLocked)
        {
            this.slotIdx = slotIdx;
            this.itemId = itemId;
            this.itemName = itemName ?? string.Empty;
            this.count = count;
            this.price = price;
            this.currency = currency;
            this.isLocked = isLocked;
        }
    }

    /// <summary>Snapshot toàn bộ panel gian hàng.</summary>
    public sealed class StallPanelSnapshot
    {
        public int playerId;
        public string stallName;
        public int totalSlots;
        public int usedSlots;
        public int totalValue;
        public IReadOnlyList<StallPanelRow> rows;
    }

    /// <summary>Dịch vụ UI: panel gian hàng cá nhân.</summary>
    public static class StallPanelService
    {
        public const string Title = "Gian Hàng";
        public const string LabelSell = "Bày bán";
        public const string LabelPrice = "Giá bán";
        public const string LabelRemove = "Gỡ xuống";
        public const string LabelSetPrice = "Đặt giá";
        public const string LabelCurrencySilver = "Bạc";
        public const string LabelCurrencyGold = "Vàng";
        public const string LabelCurrencyCoin = "KNB";
        public const int DefaultSlotCount = 20;
        public const int MaxItemsPerSlot = 99;

        /// <summary>Dựng snapshot gian hàng.</summary>
        public static StallPanelSnapshot BuildSnapshot(StallService svc, int playerId)
        {
            string stallName = svc != null ? $"Gian hàng #{playerId}" : "Gian hàng";
            return new StallPanelSnapshot
            {
                playerId = playerId,
                stallName = stallName,
                totalSlots = DefaultSlotCount,
                usedSlots = 0,
                totalValue = 0,
                rows = System.Array.Empty<StallPanelRow>(),
            };
        }

        /// <summary>Thử thêm vật phẩm vào gian hàng.</summary>
        public static bool TryAddItem(int playerId, int slot, int itemId, int count, int price)
        {
            if (playerId <= 0) return false;
            if (slot < 0 || slot >= DefaultSlotCount) return false;
            if (itemId <= 0) return false;
            if (count <= 0 || count > MaxItemsPerSlot) return false;
            if (price <= 0) return false;
            return false;
        }

        /// <summary>Thử gỡ vật phẩm khỏi gian hàng.</summary>
        public static bool TryRemoveItem(int playerId, int slot)
        {
            if (playerId <= 0 || slot < 0 || slot >= DefaultSlotCount) return false;
            return false;
        }

        /// <summary>Đặt giá mới cho ô gian hàng.</summary>
        public static bool TrySetPrice(int playerId, int slot, int newPrice)
        {
            if (playerId <= 0 || slot < 0 || slot >= DefaultSlotCount) return false;
            if (newPrice <= 0) return false;
            return false;
        }

        /// <summary>Tổng giá trị gian hàng.</summary>
        public static int GetTotalValue(int playerId)
        {
            if (playerId <= 0) return 0;
            return 0;
        }
    }
}
