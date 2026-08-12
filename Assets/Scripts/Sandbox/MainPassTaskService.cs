// -----------------------------------------------------------------------------
// VLTK Mobile — MainPassTaskService: runtime service cho nhiệm vụ chính tuyến
// Source: PC settings/task/newtask/mastertask/mainpasstask.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MainPassTaskService
    {
        private readonly PcMainPassTaskRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MainPassTaskService() : this(null) { }

        public MainPassTaskService(PcMainPassTaskRegistry reg) { _reg = reg ?? new PcMainPassTaskRegistry(); }

        public static MainPassTaskService LoadFromStreamingAssets(string subDir = "Reference/PcTask/newtask/mastertask")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MainPassTaskService(PcMainPassTaskParser.BuildRegistry(path));
        }

        public IEnumerable<PcMainPassTaskEntry> GetByTaskId(int taskId) => _reg?.GetByTaskId(taskId) ?? System.Array.Empty<PcMainPassTaskEntry>();
        public IEnumerable<PcMainPassTaskEntry> All => _reg?.All ?? System.Array.Empty<PcMainPassTaskEntry>();
    }
}
