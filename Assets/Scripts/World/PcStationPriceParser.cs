// -----------------------------------------------------------------------------
// VLTK Mobile — PC stationprice.txt parser (Bảng giá trạm xe)
// Source: settings/stationprice.txt (GB2312). Ma trận: row=from, col=to, value=price
// Dòng 1: header (tên trạm đích), cột 1: tên trạm nguồn
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcStationPriceEntry
    {
        public string FromStation { get; set; }
        public string ToStation { get; set; }
        public int Price { get; set; }
        public bool Reachable => Price >= 0;
    }

    public sealed class PcStationPriceRegistry
    {
        private readonly List<PcStationPriceEntry> _entries = new List<PcStationPriceEntry>();
        public int Count => _entries.Count;
        public IEnumerable<PcStationPriceEntry> All => _entries;
        public void Add(PcStationPriceEntry e) { if (e != null) _entries.Add(e); }
    }

    public static class PcStationPriceParser
    {
        public static PcStationPriceRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcStationPriceRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "stationprice.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);
            string[] headers = null;

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');

                if (headers == null)
                {
                    // Header row: station names (index 0 is row label, 1+ are column names)
                    headers = new string[cols.Length];
                    for (int i = 0; i < cols.Length; i++) headers[i] = cols[i].Trim();
                    continue;
                }

                if (cols.Length < 2) continue;
                string fromStation = cols[0].Trim();
                if (string.IsNullOrEmpty(fromStation)) continue;

                for (int c = 1; c < Math.Min(cols.Length, headers?.Length ?? 0); c++)
                {
                    var valStr = cols[c].Trim();
                    if (string.IsNullOrEmpty(valStr)) continue;
                    if (!int.TryParse(valStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int price)) continue;

                    reg.Add(new PcStationPriceEntry
                    {
                        FromStation = fromStation,
                        ToStation = headers[c],
                        Price = price
                    });
                }
            }
            return reg;
        }
    }
}
