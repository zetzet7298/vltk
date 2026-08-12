// -----------------------------------------------------------------------------
// VLTK Mobile — PC revivepos.txt parser (vị trí hồi sinh)
// Source: settings/revivepos.ini. Format: [mapId], region=start,end, index=x,y.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcRevivePosEntry
    {
        public int ReviveId { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequiredLevel { get; set; }
        public int RegionStart { get; set; }
        public int RegionEnd { get; set; }
    }

    public sealed class PcRevivePosRegistry
    {
        private readonly List<PcRevivePosEntry> _all = new List<PcRevivePosEntry>();
        private readonly Dictionary<int, PcRevivePosEntry> _firstById = new Dictionary<int, PcRevivePosEntry>();
        public int Count => _all.Count;
        public PcRevivePosEntry Get(int id) => _firstById.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcRevivePosEntry> All => _all;
        public IEnumerable<PcRevivePosEntry> GetByMap(int mapId)
        {
            foreach (var e in _all) if (e.MapId == mapId) yield return e;
        }
        public void Add(PcRevivePosEntry e)
        {
            if (e == null) return;
            _all.Add(e);
            if (!_firstById.ContainsKey(e.ReviveId)) _firstById[e.ReviveId] = e;
        }
    }

    public static class PcRevivePosParser
    {
        public static List<VLTK.Model.RevivePos> ParseFile(string path, IReadOnlyList<VLTK.Model.MapEntry> maps = null)
        {
            var rows = new List<VLTK.Model.RevivePos>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var registry = BuildRegistryFromFile(path);
            var campByMap = BuildCampLookup(maps);
            foreach (var e in registry.All)
            {
                rows.Add(new VLTK.Model.RevivePos
                {
                    mapId = e.MapId,
                    x = e.PosX,
                    y = e.PosY,
                    regionStart = e.RegionStart,
                    regionEnd = e.RegionEnd,
                    regionIndex = e.ReviveId,
                    camp = campByMap.TryGetValue(e.MapId, out int camp) ? camp : 0,
                });
            }
            return rows;
        }

        public static PcRevivePosRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcRevivePosRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "revivepos.ini");
            if (!File.Exists(path)) path = Path.Combine(absoluteDir, "revivepos.txt");
            if (!File.Exists(path)) return reg;
            return BuildRegistryFromFile(path);
        }

        private static PcRevivePosRegistry BuildRegistryFromFile(string path)
        {
            var reg = new PcRevivePosRegistry();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            int currentMapId = 0;
            int regionStart = 0;
            int regionEnd = 0;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    int.TryParse(line.Substring(1, line.Length - 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out currentMapId);
                    regionStart = regionEnd = 0;
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq <= 0 || currentMapId <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                if (key.Equals("region", StringComparison.OrdinalIgnoreCase))
                {
                    TryParsePair(value, out regionStart, out regionEnd);
                    continue;
                }
                if (!int.TryParse(key, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                if (!TryParsePair(value, out int x, out int y)) continue;
                var e = new PcRevivePosEntry
                {
                    ReviveId = id,
                    MapId = currentMapId,
                    PosX = x,
                    PosY = y,
                    RegionStart = regionStart,
                    RegionEnd = regionEnd,
                };
                reg.Add(e);
            }
            return reg;
        }

        private static bool TryParsePair(string value, out int left, out int right)
        {
            left = right = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var parts = value.Split(',');
            if (parts.Length < 2) return false;
            return int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out left)
                && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out right);
        }

        private static Dictionary<int, int> BuildCampLookup(IReadOnlyList<VLTK.Model.MapEntry> maps)
        {
            var result = new Dictionary<int, int>();
            if (maps == null) return result;
            foreach (var map in maps)
            {
                if (map == null || map.mapId <= 0) continue;
                result[map.mapId] = CampFromMapType(map.mapType);
            }
            return result;
        }

        private static int CampFromMapType(string mapType)
        {
            var value = (mapType ?? string.Empty).ToLowerInvariant();
            if (value.Contains("tong")) return 3;
            if (value.Contains("battle") || value.Contains("mission") || value.Contains("instance")) return 4;
            if (value.Contains("cave") || value.Contains("indoor")) return 2;
            if (value.Contains("field")) return 1;
            return 0;
        }
    }
}
