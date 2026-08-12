// -----------------------------------------------------------------------------
// VLTK Mobile — Adjust Color Service Host Interface (Unity → sandbox)
// PC source: settings/adjustcolor.txt — Cấu hình điều chỉnh màu sắc (R/G/B/A).
// Unity runtime dispatches registry load / color query events to a host impl
// that owns UI (color picker preview), save/load, and color application.
// Vietnamese: "Điều Chỉnh Màu Sắc", "RGBA".
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="AdjustColorService"/>. Decouples sandbox
    /// logic (registry parse, color lookup) from Unity-side UI (color picker
    /// preview), persistence (color preset cache), and color application.
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface IAdjustColorServiceHost
    {
        // ── Registry lifecycle ─────────────────────────────────────────────
        /// <summary>Color catalog loaded — count of registered color presets.</summary>
        void OnColorRegistryAttached(int colorCount);

        /// <summary>Empty or null registry detected — empty-state warning.</summary>
        void OnColorRegistryEmpty();

        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetColor resolved by id — null if not found.</summary>
        void OnColorResolved(int settingId, int r, int g, int b, int a, string descriptionVi);

        /// <summary>GetAll snapshot — count of all colors in registry.</summary>
        void OnAllColorsQueried(int resultCount);

        // ── Color application (called by UI code) ──────────────────────────
        /// <summary>A color preset was applied to a UI element.</summary>
        void OnColorApplied(int settingId, int r, int g, int b, int a);

        /// <summary>A color preset was previewed (before commit).</summary>
        void OnColorPreviewed(int settingId, int r, int g, int b, int a);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show color picker / preview panel.</summary>
        void ShowColorUI(int settingId, int r, int g, int b, int a);

        /// <summary>Log a color event (load, query, apply) for the GM / log file.</summary>
        void LogColorEvent(string eventType, int settingId, string detailVi);

        /// <summary>Play a color-related SFX: "load" / "apply" / "preview" / "reset".</summary>
        void PlayColorSFX(string action, int settingId);

        /// <summary>Save the active color preset / override to local cache.</summary>
        void SaveColorState(int settingId, int r, int g, int b, int a);
    }
}
