// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.5 Item Scripts metadata parser
// Source: itemscripts.txt (Reference/PcItem or root). 635 scripts.
// Cols: ScriptId  ItemId  Trigger  FunctionName  ParamsCount  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcItemScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int ItemIdCol = 1;
        public const int TriggerCol = 2;
        public const int FunctionNameCol = 3;
        public const int ParamsCountCol = 4;
        public const int DescriptionCol = 5;

        public static List<PcItemScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcItemScriptEntry>();
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
                rows.Add(new PcItemScriptEntry
                {
                    scriptId = id,
                    itemId = PcItemCommon.Int(cols, ItemIdCol),
                    trigger = PcItemCommon.Int(cols, TriggerCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    paramsCount = PcItemCommon.Int(cols, ParamsCountCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcItemScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcItemScriptRegistry();
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
    public class PcItemScriptEntry
    {
        public int scriptId;
        public int itemId;
        public int trigger;        // 0=use, 1=equip, 2=unequip, 3=drop, 4=obtain
        public string functionName;
        public int paramsCount;
        public string description;
    }

    public sealed class PcItemScriptRegistry
    {
        private readonly Dictionary<int, PcItemScriptEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcItemScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
        }

        public PcItemScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcItemScriptEntry> GetByItem(int itemId)
        {
            var list = new List<PcItemScriptEntry>();
            foreach (var e in _byId.Values) if (e.itemId == itemId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcItemScriptEntry> GetByTrigger(int trigger)
        {
            var list = new List<PcItemScriptEntry>();
            foreach (var e in _byId.Values) if (e.trigger == trigger) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcItemScriptEntry> All => new List<PcItemScriptEntry>(_byId.Values);
    }
}
