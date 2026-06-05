// -----------------------------------------------------------------------------
// VLTK Mobile — PC nativeplacelist.ini parser (danh sách quê hương)
// Source: settings/nativeplacelist.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcNativePlaceEntry
    {
        public int PlaceId { get; set; }
        public string PlaceName { get; set; }
        public int MapId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public sealed class PcNativePlaceRegistry
    {
        private readonly Dictionary<int, PcNativePlaceEntry> _byId = new Dictionary<int, PcNativePlaceEntry>();
        public int Count => _byId.Count;
        public PcNativePlaceEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcNativePlaceEntry> All => _byId.Values;
        public void Add(PcNativePlaceEntry e) { if (e != null) _byId[e.PlaceId] = e; }
    }

    public static class PcNativePlaceParser
    {
        public static PcNativePlaceRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcNativePlaceRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "nativeplacelist.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            // INI format: [List] section with Count=, then Id=/Name=/Img= entries
            // Also may have tab-separated format
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("[")) continue;
                var eqIdx = line.IndexOf('=');
                if (eqIdx > 0)
                {
                    var key = line.Substring(0, eqIdx).Trim();
                    var val = line.Substring(eqIdx + 1).Trim();
                    // Parse Id= entries
                    if (key.StartsWith("Id") && int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id))
                    {
                        reg.Add(new PcNativePlaceEntry { PlaceId = id, PlaceName = $"Place_{id}" });
                    }
                }
                else
                {
                    // Tab-separated fallback: PlaceId, PlaceName, MapId, X, Y
                    var cols = line.Split('\t');
                    if (cols.Length >= 2 && int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pid))
                    {
                        reg.Add(new PcNativePlaceEntry
                        {
                            PlaceId = pid,
                            PlaceName = cols.Length > 1 ? cols[1].Trim() : $"Place_{pid}",
                            MapId = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                            X = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ? x : 0,
                            Y = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) ? y : 0
                        });
                    }
                }
            }
            return reg;
        }
    }
}
