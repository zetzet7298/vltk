// -----------------------------------------------------------------------------
// VLTK Mobile — IRegionStreamingHost: giao diện host cho RegionStreamingService.
// Cho phép runtime dispatch các side-effect khi region stream load/unload
// (UI minimap, NPC spawn, log, save, broadcast).
// PC source: M1.9 region streaming + lua region_event.
// PC surfaces: LoadRegion, UnloadRegion, ShowRegionOverlay, Msg2Player.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho RegionStreamingService. Implement bởi UI/Map/NPC/Chat.
    /// </summary>
    public interface IRegionStreamingHost
    {
        /// <summary>Bắt đầu load region (PC LoadRegion / PreloadRegionAssets).</summary>
        void OnRegionLoadStarted(RegionCoord region, int activeRegionX, int activeRegionY);

        /// <summary>Load region hoàn tất (PC OnRegionLoaded).</summary>
        void OnRegionLoaded(RegionCoord region, int loadTimeMs);

        /// <summary>Region load thất bại (PC OnRegionLoadFailed).</summary>
        void OnRegionLoadFailed(RegionCoord region, string errorMessage);

        /// <summary>Unload region (PC UnloadRegion).</summary>
        void OnRegionUnloaded(RegionCoord region, int activeRegionX, int activeRegionY);

        /// <summary>Hiển thị region overlay trên minimap (PC ShowRegionOverlay).</summary>
        void UpdateRegionOverlay(RegionCoord activeRegion, int loadedCount, int maxLoaded);

        /// <summary>Phát SFX khi region load xong (PC PlayRegionLoadSFX).</summary>
        void PlayRegionLoadSFX(RegionCoord region);

        /// <summary>Log thông báo region stream lên kênu chat hệ thống (PC Msg2Player).</summary>
        void LogRegionEvent(RegionCoord region, string message);

        /// <summary>Lưu region state vào DB (PC SaveRegionState).</summary>
        void SaveRegionState(RegionCoord region, RegionStreamState state, int loadedCount);
    }
}
