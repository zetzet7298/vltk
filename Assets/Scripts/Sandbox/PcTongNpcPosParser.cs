// -----------------------------------------------------------------------------
// VLTK Mobile — PC npcpos.txt parser (vị trí NPC bang hội)
// Source: settings/npcpos.txt (GB2312). Cột phẳng.
// Type: 0=gate, 1=elder, 2=stunt_npc
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTongNpcPosEntry
    {
        public int NpcId { get; set; }
        public int NpcTemplateId { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int Type { get; set; }
    }

    public sealed class PcTongNpcPosRegistry
    {
        private readonly Dictionary<int, PcTongNpcPosEntry> _byId = new Dictionary<int, PcTongNpcPosEntry>();
        public int Count => _byId.Count;
        public PcTongNpcPosEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcTongNpcPosEntry> All => _byId.Values;
        public IEnumerable<PcTongNpcPosEntry> GetByMap(int mapId)
        {
            foreach (var e in _byId.Values) if (e.MapId == mapId) yield return e;
        }
        public IEnumerable<PcTongNpcPosEntry> GetByType(int type)
        {
            foreach (var e in _byId.Values) if (e.Type == type) yield return e;
        }
        public void Add(PcTongNpcPosEntry e) { if (e != null) _byId[e.NpcId] = e; }
    }

    public static class PcTongNpcPosParser
    {
        public static PcTongNpcPosRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTongNpcPosRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "npcpos.txt");
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
                var e = new PcTongNpcPosEntry
                {
                    NpcId = id,
                    NpcTemplateId = cols.Length > 1 && int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 0,
                    MapId = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                    PosX = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ? x : 0,
                    PosY = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) ? y : 0,
                    Type = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int tp) ? tp : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
