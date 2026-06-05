// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Auction Service (Đấu Giá runtime)
// Wraps PcAuctionConfigRegistry + in-memory active listings. PC source:
// settings/auction.ini (Main + NotifyString sections, INI key/value format).
// Mobile manages runtime bid/buyout flow with Vietnamese logs.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum AuctionBidResult
    {
        Success = 0,    // Đặt giá thành công
        OutBid = 1,     // Có người trả giá cao hơn
        Expired = 2,    // Phiên đấu giá đã hết hạn
        NotFound = 3,   // Không tìm thấy vật phẩm đấu giá
        BidTooLow = 4,  // Giá đặt thấp hơn giá hiện tại
    }

    [Serializable]
    public class AuctionListing
    {
        public int listingId;
        public int itemId;
        public int sellerId;
        public string sellerName;
        public int bidPrice;          // Giá khởi điểm
        public int buyoutPrice;       // Giá mua ngay
        public long expireTime;       // Unix ms timestamp
        public int currentBidder;     // 0 nếu chưa ai đặt
        public int currentBid;
        public bool sold;

        public bool IsExpired => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= expireTime;
    }

    /// <summary>
    /// Service quản lý đấu giá (Đấu Giá, Rao Bán, Trả Giá, Mua Ngay, Hết Hạn).
    /// PC source: settings/auction.ini (cấu hình) + runtime in-memory listings.
    /// </summary>
    public class AuctionService
    {
        public const string LogTag = "Auction";

        private PcAuctionConfigRegistry _config;
        private readonly Dictionary<int, AuctionListing> _activeListings = new();

        public event Action<AuctionListing> OnListed;
        public event Action<AuctionListing, int, int> OnBidPlaced; // (listing, bidderId, bid)
        public event Action<AuctionListing, int> OnSold;           // (listing, buyerId)
        public event Action<int> OnExpired;                         // (listingId)

        public int Count => _config != null ? _config.Count : 0;
        public int ActiveListingCount => _activeListings.Count;

        public AuctionService() : this(null) { }

        public AuctionService(PcAuctionConfigRegistry config)
        {
            _config = config;
        }

        public void RegisterConfig(PcAuctionConfigRegistry config)
        {
            _config = config;
        }

        // ── Config lookups ────────────────────────────────────────────

        /// <summary>Đọc giá trị cấu hình từ section/key.</summary>
        public string GetConfig(string section, string key)
            => _config != null ? _config.Get(section, key) : null;

        /// <summary>Đọc giá trị cấu hình integer (fallback nếu không parse được).</summary>
        public int GetConfigInt(string section, string key, int defaultValue = 0)
        {
            string v = GetConfig(section, key);
            if (string.IsNullOrEmpty(v)) return defaultValue;
            v = v.Trim().TrimEnd(';').Trim();
            return int.TryParse(v, out int n) ? n : defaultValue;
        }

        // ── Listing lifecycle ──────────────────────────────────────────

        /// <summary>Rao bán vật phẩm lên sàn đấu giá.</summary>
        public AuctionListing ListItem(
            int listingId, int itemId, int sellerId, string sellerName,
            int startingBid, int buyoutPrice, long durationSec)
        {
            if (listingId <= 0) return null;
            // Cho phép duration âm (đã expired sẵn) để test inject edge case
            long expireMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                + durationSec * 1000L;
            var listing = new AuctionListing
            {
                listingId = listingId,
                itemId = itemId,
                sellerId = sellerId,
                sellerName = sellerName ?? string.Empty,
                bidPrice = Math.Max(0, startingBid),
                buyoutPrice = Math.Max(0, buyoutPrice),
                expireTime = expireMs,
                currentBidder = 0,
                currentBid = 0,
                sold = false,
            };
            _activeListings[listingId] = listing;
            SubsystemLog.Info(LogTag,
                $"Rao Bán listing={listingId} item={itemId} bid={startingBid} buyout={buyoutPrice}");
            OnListed?.Invoke(listing);
            return listing;
        }

        /// <summary>Hủy rao bán (chỉ seller).</summary>
        public bool CancelListing(int listingId)
        {
            return _activeListings.Remove(listingId);
        }

        /// <summary>Tra cứu listing theo id.</summary>
        public AuctionListing GetListing(int listingId)
            => _activeListings.TryGetValue(listingId, out var v) ? v : null;

        /// <summary>Toàn bộ listing đang hoạt động.</summary>
        public IEnumerable<AuctionListing> GetAllListings() => _activeListings.Values;

        /// <summary>Đã hết hạn (theo timestamp).</summary>
        public bool IsExpired(int listingId)
        {
            var l = GetListing(listingId);
            return l != null && l.IsExpired;
        }

        /// <summary>Đặt giá (trả giá).</summary>
        public AuctionBidResult PlaceBid(int listingId, int bidderId, int bidAmount)
        {
            var l = GetListing(listingId);
            if (l == null) return AuctionBidResult.NotFound;
            if (l.sold) return AuctionBidResult.Expired;
            if (l.IsExpired)
            {
                OnExpired?.Invoke(listingId);
                return AuctionBidResult.Expired;
            }
            int minBid = l.currentBid > 0 ? l.currentBid : l.bidPrice;
            if (bidAmount <= minBid) return AuctionBidResult.BidTooLow;
            l.currentBid = bidAmount;
            l.currentBidder = bidderId;
            SubsystemLog.Info(LogTag,
                $"Trả Giá listing={listingId} bidder={bidderId} bid={bidAmount}");
            OnBidPlaced?.Invoke(l, bidderId, bidAmount);
            return AuctionBidResult.Success;
        }

        /// <summary>Mua ngay (buyout). Trả về listing nếu thành công.</summary>
        public AuctionListing Buyout(int listingId, int buyerId)
        {
            var l = GetListing(listingId);
            if (l == null || l.sold || l.IsExpired) return null;
            if (l.buyoutPrice <= 0) return null;
            l.sold = true;
            l.currentBidder = buyerId;
            l.currentBid = l.buyoutPrice;
            SubsystemLog.Info(LogTag,
                $"Mua Ngay listing={listingId} buyer={buyerId} price={l.buyoutPrice}");
            OnSold?.Invoke(l, buyerId);
            _activeListings.Remove(listingId);
            return l;
        }

        /// <summary>Load từ StreamingAssets/Reference/PcAuction.</summary>
        public static AuctionService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcAuction");
            var cfg = PcAuctionConfigParser.BuildRegistry(root);
            return new AuctionService(cfg);
        }
    }
}
