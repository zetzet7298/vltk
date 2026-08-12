// -----------------------------------------------------------------------------
// VLTK Mobile — IQuestItemHost: giao diện host cho QuestItemService.
// Cho phép runtime dispatch các side-effect khi nhận/sử dụng vật phẩm
// nhiệm vụ, clear inventory, save (UI túi đồ, log, SFX).
// PC source: settings/item/questkey.txt + 60 file PcItemFull + lua HaveItem/DelItem.
// PC surfaces: UpdateQuestItemUI, Msg2Player, PlayItemSFX, SaveQuestItem.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho QuestItemService. Implement bởi UI/Audio/DB.
    /// </summary>
    public interface IQuestItemHost
    {
        /// <summary>Nhận vật phẩm nhiệm vụ (PC OnReceiveQuestItem + HaveItem lua).</summary>
        void OnQuestItemReceived(int itemId, int oldCount, int newCount, int added);

        /// <summary>Sử dụng/bỏ vật phẩm nhiệm vụ (PC OnUseQuestItem + DelItem lua).</summary>
        void OnQuestItemRemoved(int itemId, int oldCount, int newCount, int removed);

        /// <summary>Không đủ vật phẩm để trừ (PC Msg2Player "Không đủ vật phẩm").</summary>
        void OnQuestItemInsufficient(int itemId, int required, int current);

        /// <summary>Clear toàn bộ túi đồ nhiệm vụ (PC OnQuestItemCleared).</summary>
        void OnQuestItemCleared(int clearedItemCount);

        /// <summary>Hiển thị UI túi đồ nhiệm vụ (PC ShowQuestItemUI).</summary>
        void ShowQuestItemUI(int itemCount, int totalQuantity);

        /// <summary>Log thông báo vật phẩm nhiệm vụ lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogQuestItemEvent(int itemId, int oldCount, int newCount);

        /// <summary>Phát SFX khi nhận/sử dụng vật phẩm (PC PlayItemSFX).</summary>
        void PlayItemSFX(int itemId, string action);

        /// <summary>Lưu state túi đồ nhiệm vụ vào DB (PC SaveQuestItem).</summary>
        void SaveQuestItemState(int itemCount, int totalQuantity);
    }
}
