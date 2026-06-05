// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.8 Task Scripts runtime service
// Quản lý 316 metadata scripts cho nhiệm vụ (task).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class TaskScriptService
    {
        public const string LogTag = "TaskScript";
        public const string DefaultStreamingDir = "Reference/PcTask";

        private PcTaskScriptRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public TaskScriptService() { }
        public TaskScriptService(PcTaskScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcTaskScriptRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Task script registry rỗng");
        }

        public PcTaskScriptEntry GetScript(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcTaskScriptEntry> GetByTask(int taskId)
            => _registry != null ? _registry.GetByTask(taskId) : System.Array.Empty<PcTaskScriptEntry>();
        public IReadOnlyList<PcTaskScriptEntry> GetByTrigger(int trigger)
            => _registry != null ? _registry.GetByTrigger(trigger) : System.Array.Empty<PcTaskScriptEntry>();

        public string GetFunctionName(int scriptId)
        {
            return GetScript(scriptId)?.functionName ?? string.Empty;
        }

        public static TaskScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new TaskScriptService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcTaskScriptParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
