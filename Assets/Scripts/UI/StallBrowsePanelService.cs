// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: Stall Browse (Duyệt Gian Hàng)
// Bảng UI duyệt các gian hàng người chơi, mua bán, đánh giá.
// Vietnamese: "Gian Hàng", "Mua", "Chủ gian hàng", "Tổng giá trị", "Đang mở".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct StallBrowsePanelRow
    {
        public readonly int stallId;
        public readonly int ownerId;
        public readonly string ownerName;
        public readonly string stallName;
        public readonly string items;
        public readonly int totalValue;
        public readonly bool isOnline;
        public readonly int lastActiveSec;
        public readonly bool canTrade;

        public StallBrowsePanelRow(int stallId, int ownerId, string ownerName, string stallName, string items, int totalValue, bool isOnline, int lastActiveSec, bool canTrade)
        {
            this.stallId = stallId;
            this.ownerId = ownerId;
            this.ownerName = ownerName ?? string.Empty;
            this.stallName = stallName ?? string.Empty;
            this.items = items ?? string.Empty;
            this.totalValue = totalValue;
            this.isOnline = isOnline;
            this.lastActiveSec = lastActiveSec;
            this.canTrade = canTrade;
        }
    }

    public sealed class StallBrowsePanelSnapshot
    {
        public int playerId;
        public int browseMode;
        public int currentStallId;
        public IReadOnlyList<StallBrowsePanelRow> rows;
    }

    public static class StallBrowsePanelService
    {
        public const string LabelStall = "Gian Hàng";
        public const string LabelBuy = "Mua";
        public const string LabelOwner = "Chủ gian hàng";
        public const string LabelTotalValue = "Tổng giá trị";
        public const string LabelOpen = "Đang mở";

        public static StallBrowsePanelSnapshot BuildSnapshot(StallService service, int playerId)
        {
            var snapshot = new StallBrowsePanelSnapshot
            {
                playerId = playerId,
                browseMode = 0,
                currentStallId = 0,
                rows = Array.Empty<StallBrowsePanelRow>()
            };
            if (service == null) return snapshot;
            var all = service.GetAll();
            var rows = new List<StallBrowsePanelRow>();
            foreach (var stall in all)
            {
                if (stall == null) continue;
                var sb = new StringBuilder();
                int total = 0;
                foreach (var item in stall.items)
                {
                    if (item == null) continue;
                    sb.Append(item.name).Append(" x").Append(item.count).Append("; ");
                    total += item.price * item.count;
                }
                rows.Add(new StallBrowsePanelRow(
                    stall.stallId, stall.ownerId, stall.ownerName, stall.stallName,
                    sb.ToString().TrimEnd(' ', ';'), total, stall.isOnline, stall.lastActiveSec,
                    stall.isOpen));
            }
            snapshot.rows = rows;
            return snapshot;
        }

        public static StallBrowsePanelRow? GetStall(int stallId)
        {
            if (stallId <= 0) return null;
            return new StallBrowsePanelRow(stallId, 0, string.Empty, string.Empty, string.Empty, 0, false, 0, false);
        }

        public static bool TryBuyFromStall(StallService service, int playerId, int stallId, int itemId, int count)
        {
            if (service == null || playerId <= 0 || stallId <= 0 || itemId <= 0 || count <= 0) return false;
            return service.TryBuy(playerId, stallId, itemId, count);
        }

        public static int GetTotalValue(StallService service, int stallId)
        {
            if (service == null || stallId <= 0) return 0;
            return service.GetTotalValue(stallId);
        }
    }
}
