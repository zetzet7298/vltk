// -----------------------------------------------------------------------------
// VLTK Mobile — PC wharf.txt parser (bến tàu - 11 entries)
// Source: settings/wharf.txt. Format: ID, DESC, COUNT, SECT1..SECT4.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcWharfEntry
    {
        public int WharfId { get; set; }
        public int FromMapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int SectCount { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CostSilver { get; set; }
        public int RequiredLevel { get; set; }
    }

    public sealed class PcWharfRegistry
    {
        private readonly Dictionary<int, PcWharfEntry> _byId = new Dictionary<int, PcWharfEntry>();
        public int Count => _byId.Count;
        public PcWharfEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcWharfEntry> All => _byId.Values;
        public IEnumerable<PcWharfEntry> GetByFromMap(int mapId)
        {
            foreach (var e in _byId.Values) if (e.FromMapId == mapId) yield return e;
        }
        public IEnumerable<PcWharfEntry> GetByToMap(int mapId)
        {
            foreach (var e in _byId.Values) if (e.FromMapId == mapId) yield return e;
        }
        public void Add(PcWharfEntry e) { if (e != null) _byId[e.WharfId] = e; }
    }

    public static class PcWharfParser
    {
        public static List<VLTK.Model.WharfEntry> ParseFile(string path)
        {
            var rows = new List<VLTK.Model.WharfEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var registry = BuildRegistryFromFile(path);
            foreach (var e in registry.All)
            {
                rows.Add(new VLTK.Model.WharfEntry
                {
                    wharfId = e.WharfId,
                    mapId = e.FromMapId,
                    posX = e.PosX,
                    posY = e.PosY,
                    price = e.CostSilver,
                    sectCount = e.SectCount,
                    nameRaw = e.Name,
                    nameNormalized = e.Name,
                });
            }
            return rows;
        }

        public static PcWharfRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcWharfRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "wharflist.txt");
            if (!File.Exists(path)) path = Path.Combine(absoluteDir, "wharf.txt");
            if (!File.Exists(path)) return reg;
            return BuildRegistryFromFile(path);
        }

        private static PcWharfRegistry BuildRegistryFromFile(string path)
        {
            var reg = new PcWharfRegistry();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                if (!TryParseSect(cols[3], out int mapId, out int posX, out int posY)) continue;
                var e = new PcWharfEntry
                {
                    WharfId = id,
                    FromMapId = mapId,
                    PosX = posX,
                    PosY = posY,
                    Name = cols.Length > 1 ? cols[1].Trim() : string.Empty,
                    SectCount = CountSectColumns(cols),
                    CostSilver = 0,
                };
                reg.Add(e);
            }
            return reg;
        }

        private static int CountSectColumns(string[] cols)
        {
            int count = 0;
            for (int i = 3; i < cols.Length; i++)
                if (TryParseSect(cols[i], out _, out _, out _)) count++;
            return count;
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
