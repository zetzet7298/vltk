// -----------------------------------------------------------------------------
// VLTK Mobile — PC cavelist_full.txt parser (48 hang động đầy đủ)
// Source: settings/cavelist_full.txt (GB2312). Cột phẳng, có header line.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcCaveListFullEntry
    {
        public int CaveId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MapId { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public int MinParty { get; set; }
        public int MaxParty { get; set; }
        public int BossNpcId { get; set; }
        public string RewardItem { get; set; } = string.Empty;
    }

    public sealed class PcCaveListFullRegistry
    {
        private readonly Dictionary<int, PcCaveListFullEntry> _byId = new Dictionary<int, PcCaveListFullEntry>();
        public int Count => _byId.Count;
        public PcCaveListFullEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcCaveListFullEntry> All => _byId.Values;
        public IEnumerable<PcCaveListFullEntry> GetByMap(int mapId)
        {
            foreach (var e in _byId.Values) if (e.MapId == mapId) yield return e;
        }
        public IEnumerable<PcCaveListFullEntry> GetByLevel(int level)
        {
            foreach (var e in _byId.Values) if (level >= e.MinLevel && level <= e.MaxLevel) yield return e;
        }
        public void Add(PcCaveListFullEntry e) { if (e != null) _byId[e.CaveId] = e; }
    }

    public static class PcCaveListFullParser
    {
        public static PcCaveListFullRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcCaveListFullRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "cavelist_full.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            int idx = 0;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 8) cols = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (idx++ == 0) continue; // skip header
                if (cols.Length < 6) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                var e = new PcCaveListFullEntry
                {
                    CaveId = id,
                    Name = cols.Length > 1 ? cols[1] : string.Empty,
                    MapId = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                    MinLevel = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l1) ? l1 : 0,
                    MaxLevel = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l2) ? l2 : 0,
                    MinParty = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int p1) ? p1 : 1,
                    MaxParty = cols.Length > 6 && int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int p2) ? p2 : 6,
                    BossNpcId = cols.Length > 7 && int.TryParse(cols[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b) ? b : 0,
                    RewardItem = cols.Length > 8 ? cols[8] : string.Empty
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
