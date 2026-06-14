// -----------------------------------------------------------------------------
// VLTK Mobile — IEconomyHost: giao diện host cho EconomyService.
// Cho phép runtime dispatch các side-effect khi currency / stash / shop /
// trade thay đổi (UI wallet, log, broadcast, save, SFX).
// PC source: KNpc::Stash, Trade dialog, Silver currency + lua trade_event.
// PC surfaces: Msg2Player, AddItemEx, UpdateWalletUI, broadcast, PlayTradeSFX.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho EconomyService. Implement bởi UI/Chat/Inventory.
    /// </summary>
    public interface IEconomyHost
    {
        /// <summary>Refresh UI wallet khi silver/gold thay đổi (PC UpdateWalletUI).</summary>
        void OnCurrencyChanged(int silver, int gold, int huyenTinh);

        /// <summary>Thông báo khi stash slot thêm item mới (PC AddItemEx).</summary>
        void OnStashDeposit(int itemId, int count, int totalStashUsed, int maxStashSlots);

        /// <summary>Thông báo khi rút item khỏi stash (PC RemoveItemEx).</summary>
        void OnStashWithdraw(int itemId, int count, int totalStashUsed);

        /// <summary>Thông báo khi stash đầy (PC StashFull).</summary>
        void OnStashFull(int maxStashSlots);

        /// <summary>Thông báo khi mua từ NPC shop (PC Msg2Player + AddItemEx).</summary>
        void OnShopBuy(int itemId, int count, int totalSilverSpent);

        /// <summary>Thông báo khi bán cho NPC shop (PC Msg2Player + EarnSilver).</summary>
        void OnShopSell(int itemId, int count, int silverEarned);

        /// <summary>Thông báo khi trade session được tạo (PC TradeRequest).</summary>
        void OnTradeSessionCreated(int tradeId, int initiatorId, int targetId);

        /// <summary>Lưu economy state vào DB player (PC SaveWallet).</summary>
        void SaveEconomyState(int silver, int gold, int huyenTinh, int stashUsed, int maxStashSlots);
    }
}
