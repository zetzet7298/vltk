// -----------------------------------------------------------------------------
// VLTK Mobile — TaskEventService: runtime cho bảng sự kiện nhiệm vụ (event/type/id).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TaskEventService
    {
        private readonly PcTaskEventRegistry _reg;
        public int Count => _reg?.Count ?? 0;
        public int EventCount => _reg?.EventCount ?? 0;
        public int TypeCount => _reg?.TypeCount ?? 0;
        public int IdCount => _reg?.IdCount ?? 0;

        public TaskEventService() { _reg = new PcTaskEventRegistry(); }
        public TaskEventService(PcTaskEventRegistry reg) { _reg = reg ?? new PcTaskEventRegistry(); }

        public static TaskEventService LoadFromStreamingAssets(string subDir = "Reference/PcTask")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TaskEventService(PcTaskEventParser.BuildRegistry(path));
        }

        public PcTaskEventEntry GetEvent(int id) => _reg.GetEvent(id);
        public PcTaskTypeEntry GetType(string t) => _reg.GetType(t);
        public PcTaskIdEntry GetId(int id) => _reg.GetId(id);

        public IEnumerable<PcTaskEventEntry> AllEvents => _reg.AllEvents;
        public IEnumerable<PcTaskTypeEntry> AllTypes => _reg.AllTypes;
        public IEnumerable<PcTaskIdEntry> AllIds => _reg.AllIds;
    }
}
