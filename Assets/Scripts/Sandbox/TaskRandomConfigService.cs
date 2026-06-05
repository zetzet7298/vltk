// -----------------------------------------------------------------------------
// VLTK Mobile — TaskRandomConfigService: runtime cho cấu hình nhiệm vụ ngẫu nhiên
// (kill / coll / talk / next) parse từ PcTask/random/*/entity.txt.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TaskRandomConfigService
    {
        private readonly PcTaskRandomRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TaskRandomConfigService() { _reg = new PcTaskRandomRegistry(); }
        public TaskRandomConfigService(PcTaskRandomRegistry reg) { _reg = reg ?? new PcTaskRandomRegistry(); }

        public static TaskRandomConfigService LoadFromStreamingAssets(string subDir = "Reference/PcTask/random")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TaskRandomConfigService(PcTaskRandomParser.BuildRegistry(path));
        }

        public IEnumerable<PcTaskRandomEntry> All => _reg.All;
        public IReadOnlyList<PcTaskRandomEntry> GetBySource(string source) => _reg.GetBySource(source);
    }
}
