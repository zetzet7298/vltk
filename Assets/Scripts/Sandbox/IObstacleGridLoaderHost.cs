// -----------------------------------------------------------------------------
// VLTK Mobile — IObstacleGridLoaderHost: giao diện host cho ObstacleGridLoader.
// Cho phép runtime dispatch các side-effect khi load obstacle pack, query region,
// missing region (UI log, SFX, save).
// PC source: StreamingAssets/Obstacles.bin (packed from per-region files).
// PC surfaces: OnObstacleLoaded, Msg2Player, PlayRegionSFX, SaveObstacleLog.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho ObstacleGridLoader. Implement bởi UI/Log/DB.
    /// </summary>
    public interface IObstacleGridLoaderHost
    {
        /// <summary>Load pack bắt đầu (PC OnObstacleLoadStart).</summary>
        void OnLoadStart(string packPath);

        /// <summary>Load pack thành công (PC OnObstacleLoadComplete).</summary>
        void OnLoadComplete(int regionCount, int totalBytes);

        /// <summary>Load pack thất bại (PC OnObstacleLoadFailed).</summary>
        void OnLoadFailed(string packPath, string reason);

        /// <summary>Query region thành công (PC OnObstacleRegionLoaded).</summary>
        void OnRegionLoaded(string regionFile, int width, int height, int blockedCells);

        /// <summary>Query region không tìm thấy (PC OnObstacleRegionMissing).</summary>
        void OnRegionMissing(string regionFile, string stem);

        /// <summary>Log thông báo obstacle lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogObstacleEvent(string message);

        /// <summary>Phát SFX khi load obstacle (PC PlayObstacleSFX).</summary>
        void PlayObstacleSFX(string action);

        /// <summary>Lưu log truy vấn obstacle vào DB (PC SaveObstacleLog).</summary>
        void SaveObstacleLog(string regionFile, bool found, int cellCount);
    }
}
