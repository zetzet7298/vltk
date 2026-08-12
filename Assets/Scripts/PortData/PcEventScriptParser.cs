// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.7 Event Scripts metadata parser
// Source: eventscripts.txt (Reference/PcEvent or root). 455 scripts.
// Cols: ScriptId  EventId  EventName  Trigger  FunctionName  ParamsCount  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcEventScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int EventIdCol = 1;
        public const int EventNameCol = 2;
        public const int TriggerCol = 3;
        public const int FunctionNameCol = 4;
        public const int ParamsCountCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcEventScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcEventScriptEntry>();
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
                rows.Add(new PcEventScriptEntry
                {
                    scriptId = id,
                    eventId = PcItemCommon.Int(cols, EventIdCol),
                    eventName = PcItemCommon.Str(cols, EventNameCol),
                    trigger = PcItemCommon.Int(cols, TriggerCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    paramsCount = PcItemCommon.Int(cols, ParamsCountCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcEventScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcEventScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcEventScriptEntry
    {
        public int scriptId;
        public int eventId;
        public string eventName;
        public int trigger;        // 0=start, 1=tick, 2=end, 3=join, 4=leave
        public string functionName;
        public int paramsCount;
        public string description;
    }

    public sealed class PcEventScriptRegistry
    {
        private readonly Dictionary<int, PcEventScriptEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcEventScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
        }

        public PcEventScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcEventScriptEntry> GetByEvent(int eventId)
        {
            var list = new List<PcEventScriptEntry>();
            foreach (var e in _byId.Values) if (e.eventId == eventId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcEventScriptEntry> GetByTrigger(int trigger)
        {
            var list = new List<PcEventScriptEntry>();
            foreach (var e in _byId.Values) if (e.trigger == trigger) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcEventScriptEntry> All => new List<PcEventScriptEntry>(_byId.Values);
    }
}
