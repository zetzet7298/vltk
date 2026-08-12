// -----------------------------------------------------------------------------
// VLTK Mobile — IAuctionHost: giao diện host cho AuctionService.
// Cho phép runtime dispatch các side-effect khi list/bid/buyout/cancel/expire
// (UI/chat log/notification/mail to seller/buyer).
// PC source: settings/auction.ini (Main + NotifyString sections), lua auction.
// PC surfaces: Msg2Player (winner), Msg2Tong, SendMail, system broadcast.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho AuctionService. Implement bởi UI/Chat/Notify/Mail.
    /// </summary>
    public interface IAuctionHost
    {
        /// <summary>Thông báo khi có listing mới (system broadcast tới toàn server).</summary>
        void OnItemListed(int listingId, int itemId, string sellerName, int startingBid, int buyoutPrice);

        /// <summary>Thông báo tới seller khi có người trả giá cao hơn.</summary>
        void OnOutBid(int listingId, int outbidPlayerId, int newBidderId, int newBid);

        /// <summary>Thông báo tới bidder thắng cuộc.</summary>
        void OnBidWon(int listingId, int winnerId, int finalBid);

        /// <summary>Thông báo tới seller khi bán thành công.</summary>
        void OnItemSold(int listingId, int sellerId, int buyerId, int finalPrice);

        /// <summary>Thông báo khi listing hết hạn (gửi mail cho seller).</summary>
        void OnListingExpired(int listingId, int sellerId, int itemId);

        /// <summary>Thông báo khi listing bị cancel (refund item cho seller).</summary>
        void OnListingCancelled(int listingId, int sellerId, int itemId);

        /// <summary>Trừ tiền player khi đặt giá / mua ngay (PC Pay).</summary>
        bool TryDeductPlayerMoney(int playerId, int amount);

        /// <summary>Cộng tiền cho player khi bán thắng (PC EarnMoney / Pay).</summary>
        void GrantPlayerMoney(int playerId, int amount);
    }
}
