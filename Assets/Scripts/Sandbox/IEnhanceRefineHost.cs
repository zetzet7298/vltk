// -----------------------------------------------------------------------------
// VLTK Mobile — IEnhanceRefineHost: giao diện host cho EnhanceRefineService.
// Cho phép runtime dispatch các side-effect khi cường hóa/tinh luyện/reward
// quest (UI, SFX, log, save).
// PC source: KNpc::EnhanceItem, RefineItem, quest reward tables.
// PC surfaces: UpdateItemUI, Msg2Player, PlayEnhanceSFX, SaveItemEnhance.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho EnhanceRefineService. Implement bởi UI/Audio/DB.
    /// </summary>
    public interface IEnhanceRefineHost
    {
        /// <summary>Cường hóa thành công (PC OnEnhanceSuccess).</summary>
        void OnEnhanceSuccess(int itemId, int oldLevel, int newLevel, int silverCost);

        /// <summary>Cường hóa thất bại (PC OnEnhanceFailed).</summary>
        void OnEnhanceFailed(int itemId, int currentLevel, int newLevel, bool itemDestroyed);

        /// <summary>Không đủ Bạc để cường hóa (PC OnEnhanceInsufficientSilver).</summary>
        void OnEnhanceInsufficientSilver(int itemId, int requiredSilver, int currentSilver);

        /// <summary>Tinh luyện thành công (PC OnRefineSuccess).</summary>
        void OnRefineSuccess(int itemId, int oldRefineLevel, int newRefineLevel, int bonusAttrCode, int bonusValue);

        /// <summary>Tinh luyện thất bại (PC OnRefineFailed).</summary>
        void OnRefineFailed(int itemId, int currentRefineLevel, int targetAttrCode);

        /// <summary>Tạo phần thưởng nhiệm vụ (PC OnQuestRewardGenerated).</summary>
        void OnQuestRewardGenerated(int questDifficulty, int playerLevel, int itemCount);

        /// <summary>Hiển thị UI cường hóa/tinh luyện (PC ShowEnhanceRefineUI).</summary>
        void ShowEnhanceRefineUI(int itemId, int currentLevel, int currentRefineLevel);

        /// <summary>Log thông báo cường hóa/tinh luyện lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogEnhanceRefineEvent(int itemId, int level, int refineLevel, string message);

        /// <summary>Phát SFX khi cường hóa/tinh luyện (PC PlayEnhanceSFX).</summary>
        void PlayEnhanceSFX(int itemId, string action);

        /// <summary>Lưu state cường hóa/tinh luyện vào DB (PC SaveItemEnhance).</summary>
        void SaveItemEnhanceState(int itemId, int level, int refineLevel);
    }
}
