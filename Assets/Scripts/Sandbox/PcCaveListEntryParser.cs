// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/cavelist.ini (Hang động) simple-line parser
// Source: cavelist.ini (subset of maplist with cave-scoped entries).
//   CaveId  Name  MapId  RequiredLevel  MaxLevel  MinParty  MaxParty
//   Each non-header, non-bracket line is a row; we use tab or whitespace split.
// This is a line-list parser (not section-based) for cave summary data.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcCaveListEntryRow
    {
        public int caveId;
        public string name;
        public int mapId;
        public int requiredLevel;
        public int maxLevel;
        public int minParty;
        public int maxParty;
    }

    public sealed class PcCaveListEntryRegistry
    {
        private readonly Dictionary<int, PcCaveListEntryRow> _byId = new();
        private readonly Dictionary<int, List<PcCaveListEntryRow>> _byMap = new();
        public int Count => _byId.Count;

        public void Register(PcCaveListEntryRow e)
        {
            if (e == null) return;
            _byId[e.caveId] = e;
            if (!_byMap.TryGetValue(e.mapId, out var ml)) { ml = new(); _byMap[e.mapId] = ml; }
            ml.Add(e);
        }

        public PcCaveListEntryRow Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public List<PcCaveListEntryRow> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v) ? v : new List<PcCaveListEntryRow>();

        public List<PcCaveListEntryRow> GetByLevel(int level)
        {
            var list = new List<PcCaveListEntryRow>();
            foreach (var e in _byId.Values) if (e.requiredLevel <= level && level <= e.maxLevel) list.Add(e);
            return list;
        }

        public bool CanEnter(int id, int level, int party)
        {
            var e = Get(id);
            if (e == null) return false;
            if (level < e.requiredLevel || level > e.maxLevel) return false;
            if (party < e.minParty || party > e.maxParty) return false;
            return true;
        }

        public IEnumerable<PcCaveListEntryRow> All => _byId.Values;
    }

    public static class PcCaveListEntryParser
    {
        public static List<PcCaveListEntryRow> ParseFile(string path)
        {
            var rows = new List<PcCaveListEntryRow>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']') continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                autoId++;
                rows.Add(new PcCaveListEntryRow
                {
                    caveId = PcItemCommon.Int(cols, 0),
                    name = cols.Length > 1 ? PcItemCommon.Str(cols, 1) : $"Cave{autoId}",
                    mapId = cols.Length > 2 ? PcItemCommon.Int(cols, 2) : 0,
                    requiredLevel = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 0,
                    maxLevel = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 999,
                    minParty = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 1,
                    maxParty = cols.Length > 6 ? PcItemCommon.Int(cols, 6) : 6,
                });
            }
            return rows;
        }

        public static PcCaveListEntryRegistry BuildRegistry(string dir)
        {
            var reg = new PcCaveListEntryRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "cavelist*.ini"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
