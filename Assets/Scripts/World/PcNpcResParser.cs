// -----------------------------------------------------------------------------
// VLTK Mobile — PC npcres.txt parser (tài nguyên NPC - mặt, tóc, thân)
// Source: settings/npcres/npcres.txt (GB2312). Cột phẳng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcNpcResEntry
    {
        public int NpcId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Sex { get; set; }
        public int FactionId { get; set; }
        public int HairId { get; set; }
        public int FaceId { get; set; }
        public int BodyId { get; set; }
        public int ArmId { get; set; }
        public int LegId { get; set; }
    }

    public sealed class PcNpcResRegistry
    {
        private readonly Dictionary<int, PcNpcResEntry> _byId = new Dictionary<int, PcNpcResEntry>();
        public int Count => _byId.Count;
        public PcNpcResEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcNpcResEntry> All => _byId.Values;
        public IEnumerable<PcNpcResEntry> GetByFaction(int factionId)
        {
            foreach (var e in _byId.Values) if (e.FactionId == factionId) yield return e;
        }
        public void Add(PcNpcResEntry e) { if (e != null) _byId[e.NpcId] = e; }
    }

    public static class PcNpcResParser
    {
        public static PcNpcResRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcNpcResRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "npcres.txt");
            if (!File.Exists(path))
            {
                path = Path.Combine(absoluteDir, "npcres", "npcres.txt");
                if (!File.Exists(path)) return reg;
            }
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
                var e = new PcNpcResEntry
                {
                    NpcId = id,
                    Name = cols.Length > 1 ? cols[1] : string.Empty,
                    Sex = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 0,
                    FactionId = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) ? f : 0,
                    HairId = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) ? h : 0,
                    FaceId = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fc) ? fc : 0,
                    BodyId = cols.Length > 6 && int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b) ? b : 0,
                    ArmId = cols.Length > 7 && int.TryParse(cols[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) ? a : 0,
                    LegId = cols.Length > 8 && int.TryParse(cols[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l) ? l : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
