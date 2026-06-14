// -----------------------------------------------------------------------------
// VLTK Mobile — IPcTaskDailyHost: giao diện host cho PcTaskDailyParser.
// Cho phép runtime dispatch các side-effect khi parse nhiệm vụ hàng ngày
// (UI quest log, log, SFX, save).
// PC source: server settings/task/dailytask/{gather,killmonster,talk,gather_pos,talk_pos}.txt.
// PC surfaces: UpdateDailyQuestUI, Msg2Player, PlayDailyQuestSFX, SaveDailyQuestLog.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PcTaskDailyParser.
    /// </summary>
    public interface IPcTaskDailyHost
    {
        /// <summary>Parse bắt đầu (PC OnDailyTaskParseStart).</summary>
        void OnParseStart(string fileName);

        /// <summary>Parse thành công (PC OnDailyTaskParseComplete).</summary>
        void OnParseComplete(string fileName, int entryCount, long durationMs);

        /// <summary>Parse thất bại (PC OnDailyTaskParseFailed).</summary>
        void OnParseFailed(string fileName, string reason);

        /// <summary>Build registry hoàn thành (PC OnDailyTaskRegistryBuilt).</summary>
        void OnRegistryBuilt(int gatherCount, int killCount, int talkCount, int positionCount, long durationMs);

        /// <summary>Hiển thị UI nhiệm vụ hàng ngày (PC ShowDailyQuestUI).</summary>
        void ShowDailyQuestUI(int totalCount);

        /// <summary>Log thông báo nhiệm vụ hàng ngày lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogDailyQuestEvent(string message);

        /// <summary>Phát SFX khi load nhiệm vụ hàng ngày (PC PlayDailyQuestSFX).</summary>
        void PlayDailyQuestSFX(string action);

        /// <summary>Lưu log nhiệm vụ hàng ngày vào DB (PC SaveDailyQuestLog).</summary>
        void SaveDailyQuestLog(int gatherCount, int killCount, int talkCount, int positionCount);
    }
}
