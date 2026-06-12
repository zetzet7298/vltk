// -----------------------------------------------------------------------------
// VLTK Mobile — TimerTaskService: runtime service cho định thời nhiệm vụ
// Source: PC settings/timertask.txt + systemtimetask.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class TimerTaskService
    {
        private readonly PcTimerTaskRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public TimerTaskService() : this(null) { }

        public TimerTaskService(PcTimerTaskRegistry reg) { _reg = reg ?? new PcTimerTaskRegistry(); }

        public static TimerTaskService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new TimerTaskService(PcTimerTaskParser.BuildRegistry(path));
        }

        public PcTimerTaskEntry GetTask(int id) => _reg?.Get(id);
        public IEnumerable<PcTimerTaskEntry> AllTasks => _reg?.All ?? System.Array.Empty<PcTimerTaskEntry>();
    }
}
