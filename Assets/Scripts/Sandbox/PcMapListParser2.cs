// -----------------------------------------------------------------------------
// VLTK Mobile — PC maplist (2) parser (danh sách map mở rộng)
// Source: settings/maplist2.txt (GB2312). Cột phẳng.
// Note: distinct from PcMapListParser which reads ini-grouped format.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcMapListEntry
    {
        public int MapId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequiredLevel { get; set; }
        public int MaxLevel { get; set; }
        public int Type { get; set; }
        public int IsBattlefield { get; set; }
    }

    public sealed class PcMapListRegistry
    {
        private readonly Dictionary<int, PcMapListEntry> _byId = new Dictionary<int, PcMapListEntry>();
        public int Count => _byId.Count;
        public PcMapListEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMapListEntry> All => _byId.Values;
        public IEnumerable<PcMapListEntry> GetByType(int type)
        {
            foreach (var e in _byId.Values) if (e.Type == type) yield return e;
        }
        public void Add(PcMapListEntry e) { if (e != null) _byId[e.MapId] = e; }
    }

    public static class PcMapListParser2
    {
        public static PcMapListRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMapListRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "maplist2.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 4) cols = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 4) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                var e = new PcMapListEntry
                {
                    MapId = id,
                    Name = cols.Length > 1 ? cols[1] : string.Empty,
                    RequiredLevel = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : 0,
                    MaxLevel = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                    Type = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 0,
                    IsBattlefield = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int bf) ? bf : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
