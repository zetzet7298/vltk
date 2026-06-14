// -----------------------------------------------------------------------------
// VLTK Mobile — IMinimapHost: giao diện host cho MinimapService.
// Cho phép runtime dispatch các side-effect khi resolve minimap, convert toạ độ,
// phát hiện minimap missing (UI, log, SFX).
// PC source: M1.8 minimap/world-map + asset registry.
// PC surfaces: UpdateMinimapUI, Msg2Player, PlayMinimapSFX, SaveMinimapState.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho MinimapService. Implement bởi UI/Asset/Map.
    /// </summary>
    public interface IMinimapHost
    {
        /// <summary>Resolve minimap artifact thành công (PC OnMinimapResolved).</summary>
        void OnMinimapResolved(int mapId, string sourceId, string artifactPath);

        /// <summary>Resolve minimap artifact thất bại (PC OnMinimapMissing).</summary>
        void OnMinimapMissing(int mapId, string sourceId, string reason);

        /// <summary>Map không có minimap ref (PC OnMapNoMinimapRef).</summary>
        void OnMapNoMinimapRef(int mapId, string settingSourceId);

        /// <summary>Convert world to minimap normalized (PC OnWorldToMinimap).</summary>
        void OnWorldToMinimap(int mapId, float worldX, float worldY, float u, float v);

        /// <summary>Convert minimap pixel to world (PC OnMinimapToWorld).</summary>
        void OnMinimapToWorld(int mapId, float pixelX, float pixelY, float worldX, float worldY);

        /// <summary>Hiển thị UI minimap (PC ShowMinimapUI).</summary>
        void ShowMinimapUI(int mapId, string artifactPath, bool missing);

        /// <summary>Log thông báo minimap lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogMinimapEvent(int mapId, string message);

        /// <summary>Phát SFX khi minimap load (PC PlayMinimapSFX).</summary>
        void PlayMinimapSFX(int mapId, string action);

        /// <summary>Lưu state minimap vào DB (PC SaveMinimapState).</summary>
        void SaveMinimapState(int mapId, string sourceId, string artifactPath);
    }
}
