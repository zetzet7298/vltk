// -----------------------------------------------------------------------------
// VLTK Mobile — PC waypoint.txt parser (225 waypoints dịch chuyển)
// Source: settings/waypoint.txt. Format: ID, DESC, SECT(map,x,y), FightState.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcWaypointEntry
    {
        public int WaypointId { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequiredLevel { get; set; }
        public int FightState { get; set; }
    }

    public sealed class PcWaypointRegistry
    {
        private readonly Dictionary<int, PcWaypointEntry> _byId = new Dictionary<int, PcWaypointEntry>();
        public int Count => _byId.Count;
        public PcWaypointEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcWaypointEntry> All => _byId.Values;
        public IEnumerable<PcWaypointEntry> GetByMap(int mapId)
        {
            foreach (var e in _byId.Values) if (e.MapId == mapId) yield return e;
        }
        public void Add(PcWaypointEntry e) { if (e != null) _byId[e.WaypointId] = e; }
    }

    public static class PcWaypointParser
    {
        public static List<VLTK.Model.WaypointEntry> ParseFile(string path)
        {
            var rows = new List<VLTK.Model.WaypointEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var registry = BuildRegistryFromFile(path);
            foreach (var e in registry.All)
            {
                rows.Add(new VLTK.Model.WaypointEntry
                {
                    waypointId = e.WaypointId,
                    mapId = e.MapId,
                    posX = e.PosX,
                    posY = e.PosY,
                    nameRaw = e.Name,
                    nameNormalized = e.Name,
                    fightState = e.FightState,
                });
            }
            return rows;
        }

        public static PcWaypointRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcWaypointRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "waypoint.txt");
            if (!File.Exists(path)) return reg;
            return BuildRegistryFromFile(path);
        }

        private static PcWaypointRegistry BuildRegistryFromFile(string path)
        {
            var reg = new PcWaypointRegistry();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                if (!TryParseSect(cols[2], out int mapId, out int posX, out int posY)) continue;
                var e = new PcWaypointEntry
                {
                    WaypointId = id,
                    MapId = mapId,
                    PosX = posX,
                    PosY = posY,
                    Name = cols.Length > 1 ? cols[1].Trim() : string.Empty,
                    FightState = cols.Length > 3 && int.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) ? f : 0,
                };
                reg.Add(e);
            }
            return reg;
        }

        private static bool TryParseSect(string value, out int mapId, out int posX, out int posY)
        {
            mapId = posX = posY = 0;
            if (string.IsNullOrWhiteSpace(value)) return false;
            var parts = value.Split(',');
            if (parts.Length < 3) return false;
            return int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out mapId)
                && int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out posX)
                && int.TryParse(parts[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out posY);
        }
    }
}
