// -----------------------------------------------------------------------------
// VLTK Mobile — PC autopathfindroutes.txt parser (đường đi tự động)
// Source: settings/autopathfindroutes.txt (GB2312). Mỗi dòng: RouteId, FromMap, ToMap, WaypointSequence, Distance
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcAutoPathRouteEntry
    {
        public int RouteId { get; set; }
        public int FromMapId { get; set; }
        public int ToMapId { get; set; }
        public List<int> WaypointSequence { get; set; } = new List<int>();
        public int Distance { get; set; }
    }

    public sealed class PcAutoPathRouteRegistry
    {
        private readonly Dictionary<int, PcAutoPathRouteEntry> _byId = new Dictionary<int, PcAutoPathRouteEntry>();
        public int Count => _byId.Count;
        public PcAutoPathRouteEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcAutoPathRouteEntry> All => _byId.Values;
        public IEnumerable<PcAutoPathRouteEntry> GetByFromTo(int fromMap, int toMap)
        {
            foreach (var e in _byId.Values) if (e.FromMapId == fromMap && e.ToMapId == toMap) yield return e;
        }
        public void Add(PcAutoPathRouteEntry e) { if (e != null) _byId[e.RouteId] = e; }
    }

    public static class PcAutoPathRouteParser
    {
        public static PcAutoPathRouteRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcAutoPathRouteRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "autopathfindroutes.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 4) cols = line.Split(',');
                if (cols.Length < 4) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                var e = new PcAutoPathRouteEntry
                {
                    RouteId = id,
                    FromMapId = cols.Length > 1 && int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) ? f : 0,
                    ToMapId = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 0,
                    Distance = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int d) ? d : 0
                };
                if (cols.Length > 3)
                {
                    var seqStr = cols[3].Trim();
                    foreach (var s in seqStr.Split(new[] { ';', ',', ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int wp))
                            e.WaypointSequence.Add(wp);
                    }
                }
                reg.Add(e);
            }
            return reg;
        }
    }
}
