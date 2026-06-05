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
    [System.Serializable]
    public class PcNpcResEntry
    {
        public int npcId;
        public string name = string.Empty;
        public int sex;
        public int factionId;
        public int hairId;
        public int faceId;
        public int bodyId;
        public int armId;
        public int legId;
    }

    public sealed class PcNpcResRegistry
    {
        private readonly Dictionary<int, PcNpcResEntry> _byId = new Dictionary<int, PcNpcResEntry>();
        public int Count => _byId.Count;
        public PcNpcResEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcNpcResEntry> All => _byId.Values;
        public IEnumerable<PcNpcResEntry> GetByFaction(int factionId)
        {
            foreach (var e in _byId.Values) if (e.factionId == factionId) yield return e;
        }
        public void Add(PcNpcResEntry e) { if (e != null) _byId[e.npcId] = e; }
    }

    public static class PcNpcResParser
    {
        public const int MinColumns = 4;

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
                // Skip malformed rows rather than re-splitting on whitespace;
                // Vietnamese NPC names contain spaces and the fallback would
                // shred the name field and shift every subsequent column.
                if (cols.Length < MinColumns) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                var e = new PcNpcResEntry
                {
                    npcId = id,
                    name = cols.Length > 1 ? cols[1] : string.Empty,
                    sex = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 0,
                    factionId = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) ? f : 0,
                    hairId = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int h) ? h : 0,
                    faceId = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int fc) ? fc : 0,
                    bodyId = cols.Length > 6 && int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int b) ? b : 0,
                    armId = cols.Length > 7 && int.TryParse(cols[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) ? a : 0,
                    legId = cols.Length > 8 && int.TryParse(cols[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l) ? l : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
