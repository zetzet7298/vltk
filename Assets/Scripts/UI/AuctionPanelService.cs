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
            var snap = new AuctionPanelSnapshot
            {
                playerId = playerId,
                activeBids = 0,
                wonItems = 0,
                listedItems = 0,
                rows = System.Array.Empty<AuctionPanelRow>(),
            };
            if (svc == null) return snap;
            int nowSec = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var list = new List<AuctionPanelRow>();
            foreach (var a in svc.GetActiveListings())
            {
                int timeLeft = a.expireTime > 0 ? Math.Max(0, a.expireTime - nowSec) : 0;
                bool mine = a.currentBidder == playerId;
                if (a.sellerId == playerId) snap.listedItems++;
                if (mine) snap.activeBids++;
                list.Add(new AuctionPanelRow(
                    a.listingId,
                    a.itemId,
                    a.sellerId,
                    a.currentBid,
                    a.buyoutPrice,
                    a.bidCount,
                    timeLeft,
                    mine));
            }
            snap.wonItems = svc.GetWonCount(playerId);
            snap.rows = list;
            return snap;
        }

        public static bool TryBid(AuctionService svc, int playerId, int auctionId, int bid)
        {
            if (svc == null || auctionId <= 0 || bid <= 0) return false;
            return svc.PlaceBid(auctionId, playerId, bid);
        }

        public static bool TryCancel(AuctionService svc, int playerId, int auctionId)
        {
            if (svc == null || auctionId <= 0) return false;
            return svc.CancelListing(auctionId, playerId);
        }

        public static IReadOnlyList<AuctionBidHistoryEntry> GetBidHistory(int auctionId)
        {
            // No persistent history; return empty for invalid
            if (auctionId <= 0) return System.Array.Empty<AuctionBidHistoryEntry>();
            return System.Array.Empty<AuctionBidHistoryEntry>();
        }

        public static string FormatPrice(int price) => price.ToString("N0");
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
