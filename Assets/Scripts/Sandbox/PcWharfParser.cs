// -----------------------------------------------------------------------------
// VLTK Mobile — PC wharflist.txt parser (bến tàu - 11 entries)
// Source: settings/wharflist.txt (GB2312). Cột phẳng.
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
        public int ToMapId { get; set; }
        public int BoatId { get; set; }
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
            foreach (var e in _byId.Values) if (e.ToMapId == mapId) yield return e;
        }
        public void Add(PcWharfEntry e) { if (e != null) _byId[e.WharfId] = e; }
    }

    public static class PcWharfParser
    {
        public static PcWharfRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcWharfRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "wharflist.txt");
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
                var e = new PcWharfEntry
                {
                    WharfId = id,
                    FromMapId = cols.Length > 1 && int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) ? f : 0,
                    ToMapId = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 0,
                    BoatId = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b) ? b : 0,
                    CostSilver = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) ? c : 0,
                    RequiredLevel = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
