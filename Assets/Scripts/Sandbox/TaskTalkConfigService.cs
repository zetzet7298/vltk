// -----------------------------------------------------------------------------
// VLTK Mobile — TaskTalkConfigService: runtime cho bảng task talk (TextID)
// (buygoods/findgoods/findmaps/showgoods/upground/worldmap).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TaskTalkConfigService
    {
        private readonly PcTaskTalkRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TaskTalkConfigService() { _reg = new PcTaskTalkRegistry(); }
        public TaskTalkConfigService(PcTaskTalkRegistry reg) { _reg = reg ?? new PcTaskTalkRegistry(); }

        public static TaskTalkConfigService LoadFromStreamingAssets(string subDir = "Reference/PcTask")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TaskTalkConfigService(PcTaskTalkParser.BuildRegistry(path));
        }

        public PcTaskTalkEntry Get(int textId) => _reg.Get(textId);
        public IReadOnlyList<PcTaskTalkEntry> GetBySource(string source) => _reg.GetBySource(source);
        public IEnumerable<PcTaskTalkEntry> All => _reg.All;
    }
}
