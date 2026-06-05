// -----------------------------------------------------------------------------
// VLTK Mobile — PC GBK Trigger parser
// Source: gbktrigger.txt — trigger kích hoạt khi player_enter, npc_kill, item_use, time, death.
// Cols: TriggerId, TriggerName, MapId, EventType, Condition, Action, ScriptId.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGbkTriggerParser
    {
        public const int TriggerIdCol = 0;
        public const int TriggerNameCol = 1;
        public const int MapIdCol = 2;
        public const int EventTypeCol = 3;
        public const int ConditionCol = 4;
        public const int ActionCol = 5;
        public const int ScriptIdCol = 6;

        public static List<PcGbkTriggerEntry> ParseFile(string path)
        {
            var rows = new List<PcGbkTriggerEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TriggerIdCol);
                if (id <= 0) continue;
                rows.Add(new PcGbkTriggerEntry
                {
                    triggerId = id,
                    triggerNameRaw = PcItemCommon.Str(cols, TriggerNameCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    eventType = PcItemCommon.Int(cols, EventTypeCol),
                    condition = PcItemCommon.Str(cols, ConditionCol),
                    action = PcItemCommon.Str(cols, ActionCol),
                    scriptId = PcItemCommon.Int(cols, ScriptIdCol),
                });
            }
            return rows;
        }

        public static PcGbkTriggerRegistry BuildRegistry(string dir)
        {
            var reg = new PcGbkTriggerRegistry();
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
    public class PcGbkTriggerEntry
    {
        public int triggerId;
        public string triggerNameRaw;
        public int mapId;
        public int eventType;
        public string condition;
        public string action;
        public int scriptId;
    }

    public sealed class PcGbkTriggerRegistry
    {
        private readonly Dictionary<int, PcGbkTriggerEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcGbkTriggerEntry e) { if (e == null || e.triggerId <= 0) return; _byId[e.triggerId] = e; }
        public PcGbkTriggerEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcGbkTriggerEntry> All => new List<PcGbkTriggerEntry>(_byId.Values);

        public IReadOnlyList<PcGbkTriggerEntry> GetByMap(int mapId)
        {
            var list = new List<PcGbkTriggerEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGbkTriggerEntry> GetByEvent(int eventType)
        {
            var list = new List<PcGbkTriggerEntry>();
            foreach (var e in _byId.Values)
                if (e.eventType == eventType) list.Add(e);
            return list;
        }
    }
}
