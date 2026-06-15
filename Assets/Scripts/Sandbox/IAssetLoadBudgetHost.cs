// -----------------------------------------------------------------------------
// VLTK Mobile — Asset Load Budget Service Host Interface (Unity → sandbox)
// M6.2 AC#1/AC#2/AC#3/AC#4. Pure C# service with progress tracking + memory
// budget enforcement. Unity runtime dispatches begin/progress/complete/fail
// events to a host implementation that owns the actual async loader (Addressables),
// memory monitor UI, log file, save/load of the load mode cache.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="AssetLoadBudgetService"/>. Decouples sandbox
    /// logic (job tracking, progress, memory budget) from Unity-side async loader
    /// (Addressables.LoadAssetAsync), UI (progress bar), and persistence.
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/empty args (assetKey not in jobs list, etc.) — sandbox never throws.
    /// </summary>
    public interface IAssetLoadBudgetHost
    {
        // ── Job lifecycle ───────────────────────────────────────────────────
        /// <summary>BeginLoad — new job tracked or existing returned.</summary>
        void OnLoadBegun(string assetKey, long estimatedBytes, bool isExisting);

        /// <summary>ReportProgress — progress updated for a loading job.</summary>
        void OnLoadProgress(string assetKey, float progress01);

        /// <summary>CompleteLoad — job marked loaded + actualBytes added to total.</summary>
        void OnLoadCompleted(string assetKey, long actualBytes, long totalLoadedBytes);

        /// <summary>FailLoad — job marked failed with error reason.</summary>
        void OnLoadFailed(string assetKey, string errorVi);

        /// <summary>Unload — job removed, bytes subtracted from total.</summary>
        void OnLoadUnloaded(string assetKey, long bytesFreed, long totalLoadedBytes);

        // ── Budget check ────────────────────────────────────────────────────
        /// <summary>CheckBudget — current status snapshot.</summary>
        void OnBudgetChecked(long loadedBytes, long budgetBytes, bool overBudget, float utilization01);

        /// <summary>Over-budget warning fired (loadedBytes > budgetBytes).</summary>
        void OnBudgetOverrunWarning(long loadedBytes, long budgetBytes, float utilization01);

        // ── Load mode query ─────────────────────────────────────────────────
        /// <summary>RuntimeLoadMode resolved — actual runtime mode vs registered mode.</summary>
        void OnRuntimeLoadModeResolved(int registeredModeId, int runtimeModeId, string modeName);

        // ── UI / SFX / Persistence ─────────────────────────────────────────
        /// <summary>Show / update the asset-loading progress bar UI.</summary>
        void ShowLoadProgressUI(string assetKey, float progress01, long loadedBytes, long totalBytes);

        /// <summary>Log a load event (begin, progress, complete, fail) for GM / log file.</summary>
        void LogLoadEvent(string eventType, string assetKey, string detailVi);

        /// <summary>Play load-related SFX: "begin" / "complete" / "fail" / "overrun".</summary>
        void PlayLoadSFX(string action, string assetKey);

        /// <summary>Save the current load mode cache to disk / PlayerPrefs.</summary>
        void SaveLoadModeCache(int runtimeModeId, string modeName);
    }
}
