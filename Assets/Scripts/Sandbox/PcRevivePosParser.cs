// -----------------------------------------------------------------------------
// VLTK Mobile — PC revivepos.txt parser (vị trí hồi sinh)
// Source: settings/revivepos.txt (GB2312).
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
    }

    public sealed class PcRevivePosRegistry
    {
        private readonly Dictionary<int, PcRevivePosEntry> _byId = new Dictionary<int, PcRevivePosEntry>();
        public int Count => _byId.Count;
        public PcRevivePosEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcRevivePosEntry> All => _byId.Values;
        public IEnumerable<PcRevivePosEntry> GetByMap(int mapId)
        {
            foreach (var e in _byId.Values) if (e.MapId == mapId) yield return e;
        }
        public void Add(PcRevivePosEntry e) { if (e != null) _byId[e.ReviveId] = e; }
    }

    public static class PcRevivePosParser
    {
        public static List<VLTK.Model.RevivePos> ParseFile(string path, IReadOnlyList<VLTK.Model.MapEntry> maps = null)
        {
            var rows = new List<VLTK.Model.RevivePos>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var registry = BuildRegistry(Path.GetDirectoryName(path));
            foreach (var e in registry.All)
            {
                rows.Add(new VLTK.Model.RevivePos
                {
                    mapId = e.MapId,
                    x = e.PosX,
                    y = e.PosY,
                    regionIndex = e.ReviveId,
                });
            }
            return rows;
        }

        public static PcRevivePosRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcRevivePosRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "revivepos.txt");
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
                var e = new PcRevivePosEntry
                {
                    ReviveId = id,
                    MapId = cols.Length > 1 && int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                    PosX = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ? x : 0,
                    PosY = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) ? y : 0,
                    Name = cols.Length > 4 ? cols[4] : string.Empty,
                    RequiredLevel = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
