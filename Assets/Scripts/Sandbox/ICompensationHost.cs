// -----------------------------------------------------------------------------
// VLTK Mobile — ICompensationHost: giao diện host cho CompensationIndexRuntimeService.
// Cho phép runtime dispatch các side-effect khi load/save index, query
// compensation scripts (UI list, log, SFX, save).
// PC source: CompensationIndex.json + vng_event/* + activitysys/config/* lua.
// PC surfaces: UpdateCompensationUI, Msg2Player, PlayQuestSFX, SaveLog.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho CompensationIndexRuntimeService.
    /// </summary>
    public interface ICompensationHost
    {
        /// <summary>Load bắt đầu (PC OnCompensationLoadStart).</summary>
        void OnLoadStart(string indexPath);

        /// <summary>Load thành công (PC OnCompensationLoadComplete).</summary>
        void OnLoadComplete(int entryCount, int filenameCount, int relPathCount);

        /// <summary>Load thất bại (PC OnCompensationLoadFailed).</summary>
        void OnLoadFailed(string indexPath, string reason);

        /// <summary>Lookup trúng (PC OnCompensationQuery).</summary>
        void OnQuery(string queryType, string queryKey, bool found, int matchCount);

        /// <summary>Hiển thị UI danh sách compensation (PC ShowCompensationList).</summary>
        void ShowCompensationList(int count, int filteredCount);

        /// <summary>Log thông báo compensation lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogCompensationEvent(string message);

        /// <summary>Phát SFX khi mở panel compensation (PC PlayCompensationSFX).</summary>
        void PlayCompensationSFX(string action);

        /// <summary>Lưu log truy vấn compensation vào DB (PC SaveCompensationLog).</summary>
        void SaveCompensationLog(string queryType, string queryKey, int resultCount);
    }
}
