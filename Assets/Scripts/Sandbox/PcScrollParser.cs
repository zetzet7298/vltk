// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/scroll.txt (Cuộn dịch chuyển) parser
// Source: scroll.txt (2,600 entries, GB2312).
//   Cols 0:  ScrollId
//   Col  1:  Name
//   Col  2:  FromMapId
//   Col  3:  ToMapId
//   Col  4:  RequiredLevel
//   Col  5:  Cost
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcScrollEntry
    {
        public int scrollId;
        public string name;
        public int fromMapId;
        public int toMapId;
        public int requiredLevel;
        public int cost;
    }

    public sealed class PcScrollRegistry
    {
        private readonly Dictionary<int, PcScrollEntry> _byId = new();
        private readonly Dictionary<int, List<PcScrollEntry>> _byFrom = new();
        private readonly Dictionary<int, List<PcScrollEntry>> _byTo = new();
        public int Count => _byId.Count;

        public void Register(PcScrollEntry e)
        {
            if (e == null) return;
            _byId[e.scrollId] = e;
            if (!_byFrom.TryGetValue(e.fromMapId, out var fl)) { fl = new(); _byFrom[e.fromMapId] = fl; }
            fl.Add(e);
            if (!_byTo.TryGetValue(e.toMapId, out var tl)) { tl = new(); _byTo[e.toMapId] = tl; }
            tl.Add(e);
        }

        public PcScrollEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public List<PcScrollEntry> GetByFromMap(int mapId)
            => _byFrom.TryGetValue(mapId, out var v) ? v : new List<PcScrollEntry>();

        public List<PcScrollEntry> GetByToMap(int mapId)
            => _byTo.TryGetValue(mapId, out var v) ? v : new List<PcScrollEntry>();

        public IEnumerable<PcScrollEntry> All => _byId.Values;
    }

    public static class PcScrollParser
    {
        public const int NameCol = 1;
        public const int FromMapCol = 2;
        public const int ToMapCol = 3;
        public const int ReqLevelCol = 4;
        public const int CostCol = 5;

        public static List<PcScrollEntry> ParseFile(string path)
        {
            var rows = new List<PcScrollEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                rows.Add(new PcScrollEntry
                {
                    scrollId = PcItemCommon.Int(cols, 0),
                    name = PcItemCommon.Str(cols, NameCol),
                    fromMapId = PcItemCommon.Int(cols, FromMapCol),
                    toMapId = PcItemCommon.Int(cols, ToMapCol),
                    requiredLevel = PcItemCommon.Int(cols, ReqLevelCol),
                    cost = PcItemCommon.Int(cols, CostCol),
                });
            }
            return rows;
        }

        public static PcScrollRegistry BuildRegistry(string dir)
        {
            var reg = new PcScrollRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "scroll*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
