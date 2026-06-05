// -----------------------------------------------------------------------------
// VLTK Mobile — PC revivepos.ini parser
// Source: settings/revivepos.ini (camp-based revive points). Each [mapId]
// section declares `region=start,end` and rows `regionIndex=x,y`. We emit one
// RevivePos per row, joined to the mapId. camp is derived from a best-effort
// lookup against the parsed maplist (City/Field → 0, Cave → 1, Tong → 2, else 0).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcRevivePosParser
    {
        public static List<RevivePos> ParseFile(string absolutePath, IReadOnlyList<MapEntry> mapList = null)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<RevivePos>();
            return ParseLines(File.ReadAllLines(absolutePath), mapList);
        }

        public static List<RevivePos> ParseLines(IEnumerable<string> lines, IReadOnlyList<MapEntry> mapList = null)
        {
            var rows = new List<RevivePos>();
            if (lines == null) return rows;

            var mapTypeById = new Dictionary<int, string>();
            if (mapList != null)
            {
                foreach (var m in mapList)
                {
                    if (m == null) continue;
                    mapTypeById[m.mapId] = m.mapType;
                }
            }

            int currentMapId = 0;
            int regionStart = 0, regionEnd = 0;

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var body = line.Substring(1, line.Length - 2).Trim();
                    if (int.TryParse(body, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId))
                    {
                        currentMapId = mapId;
                        regionStart = 0;
                        regionEnd = 0;
                    }
                    continue;
                }
                if (currentMapId <= 0) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                if (string.Equals(key, "region", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = value.Split(',');
                    if (parts.Length >= 1) int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out regionStart);
                    if (parts.Length >= 2) int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out regionEnd);
                    continue;
                }
                if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int regionIndex))
                    continue;
                var xy = value.Split(',');
                if (xy.Length < 2) continue;
                int x = 0, y = 0;
                int.TryParse(xy[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out x);
                int.TryParse(xy[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out y);

                rows.Add(new RevivePos
                {
                    mapId = currentMapId,
                    regionStart = regionStart,
                    regionEnd = regionEnd,
                    regionIndex = regionIndex,
                    x = x,
                    y = y,
                    camp = CampFromMapType(mapTypeById, currentMapId),
                });
            }

            rows.Sort((a, b) =>
            {
                int c = a.mapId.CompareTo(b.mapId);
                if (c != 0) return c;
                return a.regionIndex.CompareTo(b.regionIndex);
            });
            SubsystemLog.Info("PcRevivePos", $"Parsed {rows.Count} revive positions");
            return rows;
        }

        private static int CampFromMapType(Dictionary<int, string> mapTypeById, int mapId)
        {
            if (mapTypeById == null) return 0;
            if (!mapTypeById.TryGetValue(mapId, out var type) || string.IsNullOrEmpty(type))
                return 0;
            if (string.Equals(type, "Cave", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(type, "Tong", StringComparison.OrdinalIgnoreCase)) return 2;
            if (string.Equals(type, "Battle", StringComparison.OrdinalIgnoreCase)) return 3;
            if (string.Equals(type, "Mission", StringComparison.OrdinalIgnoreCase)) return 4;
            return 0;
        }
    }
}
