// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings killer.ini parser
// Source: killer.ini (quy tắc PK).
// Columns: RuleId  MapId  PkType  PenaltyExp  PenaltyItem
//   PkType: 0=disable, 1=normal, 2=full, 3=faction_only
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcKillerParser
    {
        public const int RuleIdCol = 0;
        public const int MapIdCol = 1;
        public const int PkTypeCol = 2;
        public const int PenaltyExpCol = 3;
        public const int PenaltyItemCol = 4;

        public static List<PcKillerEntry> ParseFile(string path)
        {
            var rows = new List<PcKillerEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, RuleIdCol);
                if (id <= 0) continue;
                rows.Add(new PcKillerEntry
                {
                    ruleId = id,
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    pkType = PcItemCommon.Int(cols, PkTypeCol),
                    penaltyExp = PcItemCommon.Int(cols, PenaltyExpCol),
                    penaltyItem = PcItemCommon.Str(cols, PenaltyItemCol),
                });
            }
            return rows;
        }

        public static PcKillerRegistry BuildRegistry(string dir)
        {
            var reg = new PcKillerRegistry();
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
    public class PcKillerEntry
    {
        public int ruleId;
        public int mapId;
        public int pkType;
        public int penaltyExp;
        public string penaltyItem;
    }

    public sealed class PcKillerRegistry
    {
        private readonly Dictionary<int, PcKillerEntry> _byId = new();
        private readonly Dictionary<int, List<PcKillerEntry>> _byMap = new();
        public int Count => _byId.Count;
        public void Register(PcKillerEntry e)
        {
            if (e == null || e.ruleId <= 0) return;
            _byId[e.ruleId] = e;
            if (!_byMap.TryGetValue(e.mapId, out var list))
            {
                list = new List<PcKillerEntry>();
                _byMap[e.mapId] = list;
            }
            list.Add(e);
        }
        public PcKillerEntry Get(int ruleId) => _byId.TryGetValue(ruleId, out var v) ? v : null;
        public IReadOnlyList<PcKillerEntry> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var list) ? (IReadOnlyList<PcKillerEntry>)list : System.Array.Empty<PcKillerEntry>();
        public IReadOnlyList<PcKillerEntry> All => new List<PcKillerEntry>(_byId.Values);
    }
}
