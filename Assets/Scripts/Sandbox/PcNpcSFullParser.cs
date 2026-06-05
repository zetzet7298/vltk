// -----------------------------------------------------------------------------
// VLTK Mobile — PC npcs.txt parser (toàn bộ NPC client-side)
// Source: settings/npcs.txt (GB2312). Cột phẳng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcNpcSFullEntry
    {
        public int npcId;
        public string name = string.Empty;
        public int npcTemplateId;
        public int level;
        public int factionId;
        public int series;
        public int aiType;
        public int dialogId;
    }

    public sealed class PcNpcSFullRegistry
    {
        private readonly Dictionary<int, PcNpcSFullEntry> _byId = new Dictionary<int, PcNpcSFullEntry>();
        public int Count => _byId.Count;
        public PcNpcSFullEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcNpcSFullEntry> All => _byId.Values;
        public IEnumerable<PcNpcSFullEntry> GetByTemplate(int tpl)
        {
            foreach (var e in _byId.Values) if (e.npcTemplateId == tpl) yield return e;
        }
        public IEnumerable<PcNpcSFullEntry> GetByFaction(int f)
        {
            foreach (var e in _byId.Values) if (e.factionId == f) yield return e;
        }
        public void Add(PcNpcSFullEntry e) { if (e != null) _byId[e.npcId] = e; }
    }

    public static class PcNpcSFullParser
    {
        public const int MinColumns = 4;

        public static PcNpcSFullRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcNpcSFullRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "npcs.txt");
            if (!File.Exists(path)) return reg;
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
                var e = new PcNpcSFullEntry
                {
                    npcId = id,
                    name = cols.Length > 1 ? cols[1] : string.Empty,
                    npcTemplateId = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t) ? t : 0,
                    level = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l) ? l : 0,
                    factionId = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int f) ? f : 0,
                    series = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 0,
                    aiType = cols.Length > 6 && int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int a) ? a : 0,
                    dialogId = cols.Length > 7 && int.TryParse(cols[7], NumberStyles.Integer, CultureInfo.InvariantCulture, out int d) ? d : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
