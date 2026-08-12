// -----------------------------------------------------------------------------
// VLTK Mobile — PC bossspawn.txt parser (boss spawn points + drop table)
// Source: settings/bossspawn.txt (GB2312). Cột phẳng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcBossSpawnEntry
    {
        public int BossId { get; set; }
        public int NpcTemplateId { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int RespawnSec { get; set; }
        public string DropTable { get; set; } = string.Empty;
    }

    public sealed class PcBossSpawnRegistry
    {
        private readonly Dictionary<int, PcBossSpawnEntry> _byId = new Dictionary<int, PcBossSpawnEntry>();
        public int Count => _byId.Count;
        public PcBossSpawnEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcBossSpawnEntry> All => _byId.Values;
        public IEnumerable<PcBossSpawnEntry> GetByMap(int mapId)
        {
            foreach (var e in _byId.Values) if (e.MapId == mapId) yield return e;
        }
        public void Add(PcBossSpawnEntry e) { if (e != null) _byId[e.BossId] = e; }
    }

    public static class PcBossSpawnParser
    {
        public static PcBossSpawnRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcBossSpawnRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "bossspawn.txt");
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
                var e = new PcBossSpawnEntry
                {
                    BossId = id,
                    NpcTemplateId = cols.Length > 1 && int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 0,
                    MapId = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                    PosX = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ? x : 0,
                    PosY = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) ? y : 0,
                    RespawnSec = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : 3600,
                    DropTable = cols.Length > 6 ? cols[6] : string.Empty
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
