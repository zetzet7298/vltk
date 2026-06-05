// -----------------------------------------------------------------------------
// VLTK Mobile — PC station.txt parser (Trạm xe runtime)
// Source: settings/station.txt (GB2312). Mỗi dòng: ID, DESC, COUNT, SECT1..SECT4
// Mỗi SECT là "MapId, X, Y" (comma-separated trong tab column).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcStationSect
    {
        public int MapId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class PcStationEntry
    {
        public int StationId { get; set; }
        public string Desc { get; set; }
        public int StationCount { get; set; }
        public List<PcStationSect> Sects { get; set; } = new List<PcStationSect>();
    }

    public sealed class PcStationRegistry
    {
        private readonly Dictionary<int, PcStationEntry> _byId = new Dictionary<int, PcStationEntry>();
        public int Count => _byId.Count;
        public PcStationEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcStationEntry> All => _byId.Values;
        public void Add(PcStationEntry e) { if (e != null) _byId[e.StationId] = e; }
    }

    public static class PcStationParser
    {
        public static PcStationRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcStationRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "station.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;

                var e = new PcStationEntry
                {
                    StationId = id,
                    Desc = cols.Length > 1 ? cols[1].Trim() : string.Empty,
                    StationCount = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) ? c : 0
                };

                // Parse SECT1..SECT4 (cols[3..6])
                for (int i = 3; i < Math.Min(cols.Length, 7); i++)
                {
                    var sectStr = cols[i].Trim();
                    if (string.IsNullOrEmpty(sectStr)) continue;
                    var parts = sectStr.Split(',');
                    if (parts.Length < 3) continue;
                    if (!int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId)) continue;
                    if (!int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)) continue;
                    if (!int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)) continue;
                    e.Sects.Add(new PcStationSect { MapId = mapId, X = x, Y = y });
                }
                reg.Add(e);
            }
            return reg;
        }
    }
}
