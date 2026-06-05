// -----------------------------------------------------------------------------
// VLTK Mobile — PC stringresource.txt parser
// Source: settings/stringresource.txt (GB2312). Mỗi dòng tab-separated:
// ID, STRING text. Catalog bản dịch tiếng Việt cho các chuỗi GM/Event.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcStringResourceEntry
    {
        public int TextId { get; set; }
        public string Text { get; set; }
    }

    public sealed class PcStringResourceRegistry
    {
        private readonly Dictionary<int, PcStringResourceEntry> _byId = new Dictionary<int, PcStringResourceEntry>();
        public int Count => _byId.Count;
        public PcStringResourceEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcStringResourceEntry> All => _byId.Values;
        public void Add(PcStringResourceEntry e) { if (e != null) _byId[e.TextId] = e; }
    }

    public static class PcStringResourceParser
    {
        public static PcStringResourceRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcStringResourceRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "stringresource.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                var e = new PcStringResourceEntry
                {
                    TextId = id,
                    Text = cols[1].Trim()
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
