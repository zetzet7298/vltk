// -----------------------------------------------------------------------------
// VLTK Mobile — IPcTaskEventHost: giao diện host cho PcTaskEventParser.
// Cho phép runtime dispatch các side-effect khi parse task_event.txt/task_type.txt/
// task_id.txt (UI quest log, log, SFX, save).
// PC source: server settings/task/{task_event,task_type,task_id}.txt.
// PC surfaces: UpdateTaskLogUI, Msg2Player, PlayTaskSFX, SaveTaskLog.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PcTaskEventParser.
    /// </summary>
    public interface IPcTaskEventHost
    {
        /// <summary>Parse bắt đầu (PC OnTaskParseStart).</summary>
        void OnParseStart(string fileName);

        /// <summary>Parse thành công (PC OnTaskParseComplete).</summary>
        void OnParseComplete(string fileName, int entryCount, long durationMs);

        /// <summary>Parse thất bại (PC OnTaskParseFailed).</summary>
        void OnParseFailed(string fileName, string reason);

        /// <summary>Build registry hoàn thành (PC OnTaskRegistryBuilt).</summary>
        void OnRegistryBuilt(int eventCount, int typeCount, int idCount, long durationMs);

        /// <summary>Hiển thị UI task log (PC ShowTaskLogUI).</summary>
        void ShowTaskLogUI(int totalCount);

        /// <summary>Log thông báo task lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogTaskEvent(string message);

        /// <summary>Phát SFX khi load task log (PC PlayTaskLogSFX).</summary>
        void PlayTaskLogSFX(string action);

        /// <summary>Lưu log truy vấn task log vào DB (PC SaveTaskLog).</summary>
        void SaveTaskLog(int eventCount, int typeCount, int idCount);
    }
}
