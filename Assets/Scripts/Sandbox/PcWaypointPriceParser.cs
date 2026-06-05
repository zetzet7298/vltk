// -----------------------------------------------------------------------------
// VLTK Mobile — PC waypointprice.txt parser (Bảng giá dịch chuyển điểm)
// Source: settings/waypointprice.txt (GB2312). Ma trận: row=from, col=to, value=price
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcWaypointPriceEntry
    {
        public string FromWaypoint { get; set; }
        public string ToWaypoint { get; set; }
        public int Price { get; set; }
        public bool Reachable => Price >= 0;
    }

    public sealed class PcWaypointPriceRegistry
    {
        private readonly List<PcWaypointPriceEntry> _entries = new List<PcWaypointPriceEntry>();
        public int Count => _entries.Count;
        public IEnumerable<PcWaypointPriceEntry> All => _entries;
        public void Add(PcWaypointPriceEntry e) { if (e != null) _entries.Add(e); }
    }

    public static class PcWaypointPriceParser
    {
        public static PcWaypointPriceRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcWaypointPriceRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "waypointprice.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            string[] headers = null;

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');

                if (headers == null)
                {
                    headers = new string[cols.Length];
                    for (int i = 0; i < cols.Length; i++) headers[i] = cols[i].Trim();
                    continue;
                }

                if (cols.Length < 2) continue;
                string fromWp = cols[0].Trim();
                if (string.IsNullOrEmpty(fromWp)) continue;

                for (int c = 1; c < Math.Min(cols.Length, headers?.Length ?? 0); c++)
                {
                    var valStr = cols[c].Trim();
                    if (string.IsNullOrEmpty(valStr)) continue;
                    if (!int.TryParse(valStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int price)) continue;

                    reg.Add(new PcWaypointPriceEntry
                    {
                        FromWaypoint = fromWp,
                        ToWaypoint = headers[c],
                        Price = price
                    });
                }
            }
            return reg;
        }
    }
}
