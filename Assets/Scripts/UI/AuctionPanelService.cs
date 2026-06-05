// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Đấu Giá (Auction Panel)
// Reference: PC auction system + AuctionService.
// Vietnamese: "Đấu Giá", "Giá hiện tại", "Giá mua ngay", "Số lần đấu".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct AuctionPanelRow
    {
        public readonly int auctionId;
        public readonly int itemId;
        public readonly int sellerId;
        public readonly int currentBid;
        public readonly int buyoutPrice;
        public readonly int bidCount;
        public readonly int timeLeftSec;
        public readonly bool isMyBid;

        public AuctionPanelRow(int auctionId, int itemId, int sellerId, int currentBid, int buyoutPrice, int bidCount, int timeLeftSec, bool isMyBid)
        {
            this.auctionId = auctionId;
            this.itemId = itemId;
            this.sellerId = sellerId;
            this.currentBid = currentBid;
            this.buyoutPrice = buyoutPrice;
            this.bidCount = bidCount;
            this.timeLeftSec = timeLeftSec;
            this.isMyBid = isMyBid;
        }
    }

    public sealed class AuctionPanelSnapshot
    {
        public int playerId;
        public int activeBids;
        public int wonItems;
        public int listedItems;
        public IReadOnlyList<AuctionPanelRow> rows;
    }

    public static class AuctionPanelService
    {
        public static AuctionPanelSnapshot BuildSnapshot(AuctionService svc, int playerId)
        {
            return new AuctionPanelSnapshot { rows = System.Array.Empty<AuctionPanelRow>() };
        }

        public static bool TryBid(AuctionService svc, int playerId, int auctionId, int bid)
        {
            return false;
        }

        public static bool TryCancel(AuctionService svc, int playerId, int auctionId)
        {
            return false;
        }

        public static IReadOnlyList<AuctionBidHistoryEntry> GetBidHistory(int auctionId)
        {
            return System.Array.Empty<AuctionBidHistoryEntry>();
        }

        public static string FormatPrice(int price)
        {
            return string.Empty;
        }

    }

    /// <summary>Stub cho lịch sử đấu giá.</summary>
    public class AuctionBidHistoryEntry
    {
        public int bidderId;
        public string bidderName;
        public int bid;
        public int bidTime;
    }
}
