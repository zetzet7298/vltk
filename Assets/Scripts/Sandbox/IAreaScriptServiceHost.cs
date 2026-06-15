// -----------------------------------------------------------------------------
// VLTK Mobile — Area Script Service Host Interface (Unity → sandbox)
// PC source: settings/areas.txt — 9 vùng bản đồ GBK (Đông Bắc, Đại Lý, ...).
// Unity runtime dispatches registry load / area query events to a host impl
// that owns UI (world map), quest routing, and category grouping.
// Vietnamese: "Vùng Bản Đồ", "Nhiệm Vụ Môn Phái", "Thị Trấn", "PvP".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="AreaScriptService"/>. Decouples sandbox
    /// logic (registry parse, query by id / map / category) from Unity-side
    /// UI (world map, area tooltip), quest routing, and category grouping.
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface IAreaScriptServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Area catalog loaded — count of registered areas.</summary>
        void OnAreaRegistryAttached(int areaCount);

        /// <summary>Empty or null registry detected — empty-state warning.</summary>
        void OnAreaRegistryEmpty();

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetArea resolved by id — null if not found.</summary>
        void OnAreaResolved(int areaId, string areaNameRaw, int mapId, int category);

        /// <summary>GetByMap — count of areas for the given mapId.</summary>
        void OnAreasByMapQueried(int mapId, int resultCount);

        /// <summary>GetByCategory — count of areas for the given category.</summary>
        void OnAreasByCategoryQueried(int category, int resultCount, string categoryNameVi);

        /// <summary>GetTotalScriptCount — total Lua script count across all areas.</summary>
        void OnTotalScriptCountQueried(int totalScriptCount);

        // ── Category name lookup ──────────────────────────────────────────
        /// <summary>GetCategoryName dispatched — Vietnamese label for the category.</summary>
        void OnCategoryNameResolved(int category, string categoryNameVi);

        /// <summary>GetAreaName resolved — area name or null if not found.</summary>
        void OnAreaNameResolved(int areaId, string areaNameRaw, bool found);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show world map / area marker.</summary>
        void ShowAreaUI(int areaId, string areaNameRaw, int mapId);

        /// <summary>Log an area event (load, query, navigation) for the GM / log file.</summary>
        void LogAreaEvent(string eventType, int areaId, string detailVi);

        /// <summary>Play an area-related SFX: "load" / "open" / "category" / "click".</summary>
        void PlayAreaSFX(string action, int areaId);

        /// <summary>Save the active area / category state to local cache.</summary>
        void SaveAreaState(int areaId, int category, int mapId);
    }
}
