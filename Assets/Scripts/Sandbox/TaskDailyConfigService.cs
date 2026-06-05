// -----------------------------------------------------------------------------
// VLTK Mobile — TaskDailyConfigService: runtime cho cấu hình nhiệm vụ hàng ngày
// (gather / kill / talk / position) parse từ PcTask/dailytask/*.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TaskDailyConfigService
    {
        private readonly PcTaskDailyRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TaskDailyConfigService() { _reg = new PcTaskDailyRegistry(); }
        public TaskDailyConfigService(PcTaskDailyRegistry reg) { _reg = reg ?? new PcTaskDailyRegistry(); }

        public static TaskDailyConfigService LoadFromStreamingAssets(string subDir = "Reference/PcTask/dailytask")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TaskDailyConfigService(PcTaskDailyParser.BuildRegistry(path));
        }

        public PcTaskDailyEntry Get(int taskId) => _reg.Get(taskId);
        public IReadOnlyList<PcTaskDailyEntry> GetByType(string type) => _reg.GetByType(type);
        public IEnumerable<PcTaskDailyEntry> All => _reg.All;
    }
}
