// -----------------------------------------------------------------------------
// VLTK Mobile — Stall runtime service (Bày Bán / Sạp Hàng)
// Quản lý cấu hình sạp hàng: maxItems, thuế, max/min giá, cấp yêu cầu.
// Wraps PcStallRegistry.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class StallService
    {
        public const string LogTag = "Stall";
        public const string DefaultStreamingDir = "Reference/PcStall";

        private PcStallRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IEnumerable<PcStallEntry> All => _registry?.All;

        public StallService() : this(null) { }

        public StallService(PcStallRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"StallService loaded {_registry?.Count ?? 0} loại sạp hàng");
        }

        public void RegisterRegistry(PcStallRegistry registry)
        {
            _registry = registry;
        }

        public PcStallEntry GetStall(int stallId)
            => _registry != null ? _registry.Get(stallId) : null;

        public static StallService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcStallParser.BuildRegistry(dir);
            return new StallService(reg);
        }
    }
}
