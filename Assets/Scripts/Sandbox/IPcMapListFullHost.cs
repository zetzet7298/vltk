// -----------------------------------------------------------------------------
// VLTK Mobile — IPcMapListFullHost: giao diện host cho PcMapListFullParser.
// Cho phép runtime dispatch các side-effect khi parse maplist.ini (UI minimap,
// log, SFX, save).
// PC source: Client 6.0/settings/maplist.ini (1,005 maps, INI format).
// PC surfaces: UpdateMapListUI, Msg2Player, PlayMapLoadSFX, SaveMapLog.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PcMapListFullParser.
    /// </summary>
    public interface IPcMapListFullHost
    {
        /// <summary>Parse bắt đầu (PC OnMapParseStart).</summary>
        void OnParseStart(string filePath);

        /// <summary>Parse hoàn thành (PC OnMapParseComplete).</summary>
        void OnParseComplete(string filePath, int entryCount, int durationMs);

        /// <summary>Parse thất bại (PC OnMapParseFailed).</summary>
        void OnParseFailed(string filePath, string reason);

        /// <summary>Build registry hoàn thành (PC OnRegistryBuilt).</summary>
        void OnRegistryBuilt(int totalMaps, int withMapType, int withoutMapType, long durationMs);

        /// <summary>Hiển thị UI danh sách map (PC ShowMapList).</summary>
        void ShowMapList(int totalMaps, int filtered);

        /// <summary>Log thông báo map lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogMapListEvent(string message);

        /// <summary>Phát SFX khi load map list (PC PlayMapLoadSFX).</summary>
        void PlayMapLoadSFX(string action);

        /// <summary>Lưu log truy vấn map list vào DB (PC SaveMapLog).</summary>
        void SaveMapLog(int totalMaps, int withMapType, int withoutMapType);
    }
}
