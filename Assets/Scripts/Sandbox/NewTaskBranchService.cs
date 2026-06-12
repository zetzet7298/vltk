// -----------------------------------------------------------------------------
// VLTK Mobile — NewTaskBranchService: runtime service cho nhiệm vụ nhánh tân thủ
// Source: PC settings/task/newtask/branch/auxpasstask.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class NewTaskBranchService
    {
        private readonly PcNewTaskBranchRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public NewTaskBranchService() : this(null) { }

        public NewTaskBranchService(PcNewTaskBranchRegistry reg) { _reg = reg ?? new PcNewTaskBranchRegistry(); }

        public static NewTaskBranchService LoadFromStreamingAssets(string subDir = "Reference/PcTask/newtask/branch")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new NewTaskBranchService(PcNewTaskBranchParser.BuildRegistry(path));
        }

        public IEnumerable<PcNewTaskBranchEntry> GetByTaskId(int taskId) => _reg?.GetByTaskId(taskId) ?? System.Array.Empty<PcNewTaskBranchEntry>();
        public IEnumerable<PcNewTaskBranchEntry> All => _reg?.All ?? System.Array.Empty<PcNewTaskBranchEntry>();
    }
}
