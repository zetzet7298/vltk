// -----------------------------------------------------------------------------
// VLTK Mobile — PC Area Script parser (14.x GBK map areas)
// Source: areascripts.txt (9 vùng: Đông Bắc, Đại Lý, Thiên Vương, ...).
// Cols: AreaId, AreaName, MapId, ScriptFileName, ScriptCount, Category, Description.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcAreaScriptParser
    {
        public const int AreaIdCol = 0;
        public const int AreaNameCol = 1;
        public const int MapIdCol = 2;
        public const int ScriptFileNameCol = 3;
        public const int ScriptCountCol = 4;
        public const int CategoryCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcAreaScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcAreaScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, AreaIdCol);
                if (id <= 0) continue;
                rows.Add(new PcAreaScriptEntry
                {
                    areaId = id,
                    areaNameRaw = PcItemCommon.Str(cols, AreaNameCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    scriptFileName = PcItemCommon.Str(cols, ScriptFileNameCol),
                    scriptCount = PcItemCommon.Int(cols, ScriptCountCol),
                    category = PcItemCommon.Int(cols, CategoryCol),
                    descriptionRaw = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcAreaScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcAreaScriptRegistry();
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
    public class PcAreaScriptEntry
    {
        public int areaId;
        public string areaNameRaw;
        public int mapId;
        public string scriptFileName;
        public int scriptCount;
        public int category;
        public string descriptionRaw;
    }

    public sealed class PcAreaScriptRegistry
    {
        private readonly Dictionary<int, PcAreaScriptEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcAreaScriptEntry e) { if (e == null || e.areaId <= 0) return; _byId[e.areaId] = e; }
        public PcAreaScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcAreaScriptEntry> All => new List<PcAreaScriptEntry>(_byId.Values);

        public IReadOnlyList<PcAreaScriptEntry> GetByCategory(int category)
        {
            var list = new List<PcAreaScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.category == category) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcAreaScriptEntry> GetByMap(int mapId)
        {
            var list = new List<PcAreaScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public int GetTotalScriptCount()
        {
            int total = 0;
            foreach (var e in _byId.Values) total += e.scriptCount;
            return total;
        }
    }
}
