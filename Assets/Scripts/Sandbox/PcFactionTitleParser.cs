// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/factiontitle.txt faction title parser
// Source: factiontitle.txt (81 entries, GB2312).
//   RANKID  RANKSTR  FACTION   (col0=id, col1=name, col2=factionId)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFactionTitleParser
    {
        public static List<PcFactionTitleEntry> ParseFile(string path)
        {
            var rows = new List<PcFactionTitleEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcFactionTitleEntry
                {
                    titleId = PcItemCommon.Int(cols, 0),
                    factionId = PcItemCommon.Int(cols, 2),
                    nameRaw = PcItemCommon.Str(cols, 1),
                    titleLevel = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 0,
                });
            }
            return rows;
        }

        public static PcFactionTitleRegistry BuildRegistry(string dir)
        {
            var reg = new PcFactionTitleRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "factiontitle.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcFactionTitleEntry
    {
        public int titleId;
        public int factionId;
        public string nameRaw;
        public int titleLevel;
    }

    public sealed class PcFactionTitleRegistry
    {
        private readonly Dictionary<int, PcFactionTitleEntry> _byId = new();
        private readonly Dictionary<int, List<PcFactionTitleEntry>> _byFaction = new();
        public int Count => _byId.Count;
        public void Register(PcFactionTitleEntry e)
        {
            if (e == null || e.titleId <= 0) return;
            _byId[e.titleId] = e;
            if (!_byFaction.TryGetValue(e.factionId, out var list))
            {
                list = new List<PcFactionTitleEntry>();
                _byFaction[e.factionId] = list;
            }
            list.Add(e);
        }
        public PcFactionTitleEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcFactionTitleEntry> GetFactionTitles(int faction)
            => _byFaction.TryGetValue(faction, out var v) ? v : (IReadOnlyList<PcFactionTitleEntry>)System.Array.Empty<PcFactionTitleEntry>();
    }
}
