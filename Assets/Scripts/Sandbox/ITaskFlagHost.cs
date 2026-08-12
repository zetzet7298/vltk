// -----------------------------------------------------------------------------
// VLTK Mobile — ITaskFlagHost: giao diện host cho TaskFlagService.
// Cho phép runtime dispatch các side-effect khi task flag thay đổi
// (UI quest log, log, SFX, save).
// PC source: Task flags (0=inactive, 1=active, 2=complete, 3=rewarded) + lua TaskState.
// PC surfaces: UpdateQuestLogUI, Msg2Player, PlayTaskSFX, SaveTaskLog.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho TaskFlagService. Implement bởi UI/QuestLog/DB.
    /// </summary>
    public interface ITaskFlagHost
    {
        /// <summary>Đặt giá trị flag mới (PC OnTaskFlagSet).</summary>
        void OnTaskFlagSet(int taskId, int oldStatus, int newStatus, int progress, int targetCount);

        /// <summary>Nhiệm vụ đã hoàn thành (PC OnTaskComplete, status 2).</summary>
        void OnTaskComplete(int taskId, int progress, int targetCount);

        /// <summary>Nhiệm vụ đã nhận thưởng (PC OnTaskRewarded, status 3).</summary>
        void OnTaskRewarded(int taskId);

        /// <summary>Từ chối nhận nhiệm vụ (PC OnTaskAcceptDenied).</summary>
        void OnTaskAcceptDenied(int taskId, int playerLevel, int reqLevel);

        /// <summary>Catalog attach (PC OnTaskFlagCatalogAttached).</summary>
        void OnCatalogAttached(int flagCount);

        /// <summary>Serialize save state (PC OnTaskFlagSerialized).</summary>
        void OnSerialized(string json, int taskCount);

        /// <summary>Deserialize save state (PC OnTaskFlagDeserialized).</summary>
        void OnDeserialized(int taskCount);

        /// <summary>Hiển thị UI nhiệm vụ (PC ShowTaskUI).</summary>
        void ShowTaskUI(int taskId, int status, int progress, int targetCount);

        /// <summary>Log thông báo nhiệm vụ lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogTaskFlagEvent(int taskId, int status, string message);

        /// <summary>Phát SFX khi nhiệm vụ thay đổi (PC PlayTaskSFX).</summary>
        void PlayTaskSFX(int taskId, int status, string action);

        /// <summary>Lưu state nhiệm vụ vào DB (PC SaveTaskLog).</summary>
        void SaveTaskFlagState(int taskId, int status, int progress, int targetCount);
    }
}
