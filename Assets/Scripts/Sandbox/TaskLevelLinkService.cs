// -----------------------------------------------------------------------------
// VLTK Mobile — TaskLevelLinkService: runtime cho bảng liên kết cấp NV.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TaskLevelLinkService
    {
        private readonly PcTaskLevelLinkRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TaskLevelLinkService() { _reg = new PcTaskLevelLinkRegistry(); }
        public TaskLevelLinkService(PcTaskLevelLinkRegistry reg) { _reg = reg ?? new PcTaskLevelLinkRegistry(); }

        public static TaskLevelLinkService LoadFromStreamingAssets(string subDir = "Reference/PcTask")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TaskLevelLinkService(PcTaskLevelLinkParser.BuildRegistry(path));
        }

        public PcTaskLevelLinkEntry GetByLevel(int level) => _reg.GetByLevel(level);
        public int GetTaskStartForLevel(int playerLevel) => _reg.GetTaskStartForLevel(playerLevel);
    }
}
