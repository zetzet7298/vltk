// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.8 Task Scripts metadata parser
// Source: taskscripts.txt (Reference/PcTask or root). 316 scripts.
// Cols: ScriptId  TaskId  Trigger  FunctionName  ParamsCount  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTaskScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int TaskIdCol = 1;
        public const int TriggerCol = 2;
        public const int FunctionNameCol = 3;
        public const int ParamsCountCol = 4;
        public const int DescriptionCol = 5;

        public static List<PcTaskScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcTaskScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, ScriptIdCol);
                if (id <= 0) continue;
                rows.Add(new PcTaskScriptEntry
                {
                    scriptId = id,
                    taskId = PcItemCommon.Int(cols, TaskIdCol),
                    trigger = PcItemCommon.Int(cols, TriggerCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    paramsCount = PcItemCommon.Int(cols, ParamsCountCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcTaskScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcTaskScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Use the explicit file-name family to avoid sweeping unrelated
            // .txt/.ini files in the directory tree.
            foreach (var f in Directory.GetFiles(dir, "taskscripts*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcTaskScriptEntry
    {
        public int scriptId;
        public int taskId;
        public int trigger;        // 0=accept, 1=step, 2=complete, 3=fail, 4=abandon
        public string functionName;
        public int paramsCount;
        public string description;
    }

    public sealed class PcTaskScriptRegistry
    {
        private readonly Dictionary<int, PcTaskScriptEntry> _byId = new();
        // Secondary index keyed by taskId to make per-task lookups O(1)
        // instead of scanning every entry in _byId.
        private readonly Dictionary<int, List<PcTaskScriptEntry>> _byTask = new();
        private readonly Dictionary<int, List<PcTaskScriptEntry>> _byTrigger = new();
        public int Count => _byId.Count;

        public void Register(PcTaskScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
            if (!_byTask.TryGetValue(e.taskId, out var tl))
            {
                tl = new List<PcTaskScriptEntry>();
                _byTask[e.taskId] = tl;
            }
            tl.Add(e);
            if (!_byTrigger.TryGetValue(e.trigger, out var trl))
            {
                trl = new List<PcTaskScriptEntry>();
                _byTrigger[e.trigger] = trl;
            }
            trl.Add(e);
        }

        public PcTaskScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcTaskScriptEntry> GetByTask(int taskId)
        {
            return _byTask.TryGetValue(taskId, out var list)
                ? (IReadOnlyList<PcTaskScriptEntry>)list
                : System.Array.Empty<PcTaskScriptEntry>();
        }

        public IReadOnlyList<PcTaskScriptEntry> GetByTrigger(int trigger)
        {
            return _byTrigger.TryGetValue(trigger, out var list)
                ? (IReadOnlyList<PcTaskScriptEntry>)list
                : System.Array.Empty<PcTaskScriptEntry>();
        }

        public IReadOnlyList<PcTaskScriptEntry> All => new List<PcTaskScriptEntry>(_byId.Values);
    }
}
