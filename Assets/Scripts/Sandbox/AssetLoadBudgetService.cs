using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>State of an async asset load job (M6.2 AC#2).</summary>
    public enum AssetLoadState
    {
        Queued,
        Loading,
        Loaded,
        Failed,
    }

    /// <summary>A tracked async load job with progress.</summary>
    public class AssetLoadJob
    {
        public string assetKey;
        public AssetLoadState state = AssetLoadState.Queued;
        public float progress;      // 0..1
        public long estimatedBytes;
        public string error;
    }

    /// <summary>Memory budget snapshot for the GM/Logs report (M6.2 AC#3).</summary>
    public struct MemoryBudgetStatus
    {
        public long loadedBytes;
        public long budgetBytes;
        public bool overBudget;
        public float utilization; // loadedBytes / budgetBytes
    }

    /// <summary>
    /// M6.2 — Mobile-friendly asset loading coordinator. Pure C# (no MonoBehaviour)
    /// so it is fully EditMode-testable. Tracks async load jobs with progress so
    /// large maps load without freezing (AC#2), enforces a memory budget and reports
    /// a warning when exceeded (AC#3), and keeps Asset Registry load modes stable
    /// regardless of the packaging strategy (AC#1/AC#4). A MonoBehaviour driver feeds
    /// real async progress and byte counts in.
    /// </summary>
    public class AssetLoadBudgetService
    {
        private readonly Dictionary<string, AssetLoadJob> _jobs = new();
        private long _loadedBytes;

        /// <summary>Mobile memory budget in bytes (default 256 MB).</summary>
        public long BudgetBytes { get; set; } = 256L * 1024 * 1024;

        public IReadOnlyCollection<AssetLoadJob> Jobs => _jobs.Values;
        public long LoadedBytes => _loadedBytes;

        /// <summary>AC#2 — begin tracking an async load job.</summary>
        public AssetLoadJob BeginLoad(string assetKey, long estimatedBytes)
        {
            if (_jobs.TryGetValue(assetKey, out var existing)) return existing;
            var job = new AssetLoadJob
            {
                assetKey = assetKey,
                state = AssetLoadState.Loading,
                progress = 0f,
                estimatedBytes = estimatedBytes,
            };
            _jobs[assetKey] = job;
            return job;
        }

        /// <summary>AC#2 — report async progress (0..1) for a job.</summary>
        public void ReportProgress(string assetKey, float progress)
        {
            if (_jobs.TryGetValue(assetKey, out var job) && job.state == AssetLoadState.Loading)
                job.progress = Math.Max(0f, Math.Min(1f, progress));
        }

        /// <summary>AC#2/AC#3 — mark a job loaded and add its bytes to the running total.</summary>
        public MemoryBudgetStatus CompleteLoad(string assetKey, long actualBytes)
        {
            if (_jobs.TryGetValue(assetKey, out var job))
            {
                job.state = AssetLoadState.Loaded;
                job.progress = 1f;
                job.estimatedBytes = actualBytes;
                _loadedBytes += actualBytes;
            }
            return CheckBudget();
        }

        public void FailLoad(string assetKey, string error)
        {
            if (_jobs.TryGetValue(assetKey, out var job))
            {
                job.state = AssetLoadState.Failed;
                job.error = error;
                SubsystemLog.Warn("AssetLoad", $"Load failed for '{assetKey}': {error}");
            }
        }

        /// <summary>AC#3 — unload an asset, freeing its bytes.</summary>
        public void Unload(string assetKey)
        {
            if (_jobs.TryGetValue(assetKey, out var job) && job.state == AssetLoadState.Loaded)
            {
                _loadedBytes -= job.estimatedBytes;
                if (_loadedBytes < 0) _loadedBytes = 0;
                _jobs.Remove(assetKey);
            }
        }

        /// <summary>AC#3 — current memory budget status; logs a warning when over budget.</summary>
        public MemoryBudgetStatus CheckBudget()
        {
            var status = new MemoryBudgetStatus
            {
                loadedBytes = _loadedBytes,
                budgetBytes = BudgetBytes,
                overBudget = _loadedBytes > BudgetBytes,
                utilization = BudgetBytes > 0 ? (float)_loadedBytes / BudgetBytes : 0f,
            };
            if (status.overBudget)
                SubsystemLog.Warn("AssetLoad",
                    $"Memory budget exceeded: {_loadedBytes} / {BudgetBytes} bytes ({status.utilization:P0})");
            return status;
        }

        /// <summary>
        /// AC#1/AC#4 — load mode is stable regardless of packaging strategy: the
        /// runtime always uses StreamingAssets (or Addressables) without changing the
        /// registry's recorded LoadMode. This returns the canonical runtime load mode
        /// so callers/docs stay consistent when the packaging decision changes.
        /// </summary>
        public LoadMode RuntimeLoadMode(LoadMode registeredMode)
        {
            // EditorDirect/TestFixture collapse to StreamingAssets at runtime; bundle/
            // addressable modes are preserved. The registry entry itself is unchanged.
            switch (registeredMode)
            {
                case LoadMode.AssetBundle:
                case LoadMode.Addressables:
                    return registeredMode;
                default:
                    return LoadMode.StreamingAssets;
            }
        }
    }
}
