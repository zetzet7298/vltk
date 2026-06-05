// -----------------------------------------------------------------------------
// VLTK Mobile — PC GBK Map Script parser (per-map script list)
// Source: gbkscripts.txt — script kích hoạt khi vào/ra/tick/event/npc trên map.
// Cols: ScriptId, AreaId, MapId, ScriptFile, TriggerType, FunctionName, Description.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGbkMapScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int AreaIdCol = 1;
        public const int MapIdCol = 2;
        public const int ScriptFileCol = 3;
        public const int TriggerTypeCol = 4;
        public const int FunctionNameCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcGbkMapScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcGbkMapScriptEntry>();
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
                rows.Add(new PcGbkMapScriptEntry
                {
                    scriptId = id,
                    areaId = PcItemCommon.Int(cols, AreaIdCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    scriptFile = PcItemCommon.Str(cols, ScriptFileCol),
                    triggerType = PcItemCommon.Int(cols, TriggerTypeCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    descriptionRaw = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcGbkMapScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcGbkMapScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Use the explicit file-name family to avoid sweeping unrelated
            // .txt/.ini files in the directory tree.
            foreach (var f in Directory.GetFiles(dir, "gbkscripts*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcGbkMapScriptEntry
    {
        public int scriptId;
        public int areaId;
        public int mapId;
        public string scriptFile;
        public int triggerType;
        public string functionName;
        public string descriptionRaw;
    }

    public sealed class PcGbkMapScriptRegistry
    {
        private readonly Dictionary<int, PcGbkMapScriptEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcGbkMapScriptEntry e) { if (e == null || e.scriptId <= 0) return; _byId[e.scriptId] = e; }
        public PcGbkMapScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcGbkMapScriptEntry> All => new List<PcGbkMapScriptEntry>(_byId.Values);

        public IReadOnlyList<PcGbkMapScriptEntry> GetByArea(int areaId)
        {
            var list = new List<PcGbkMapScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.areaId == areaId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGbkMapScriptEntry> GetByMap(int mapId)
        {
            var list = new List<PcGbkMapScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGbkMapScriptEntry> GetByTrigger(int triggerType)
        {
            var list = new List<PcGbkMapScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.triggerType == triggerType) list.Add(e);
            return list;
        }
    }
}
