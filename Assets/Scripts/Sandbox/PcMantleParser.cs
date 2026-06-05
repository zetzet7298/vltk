// -----------------------------------------------------------------------------
// VLTK Mobile — PC item/000/mantle.txt parser (Phi Phong)
// Source: PC item/mantle.txt. Wide tab-separated 40+ columns
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcMantleEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Genre { get; set; }
        public int Level { get; set; }
        public int Price { get; set; }
    }

    public sealed class PcMantleRegistry
    {
        private readonly Dictionary<int, PcMantleEntry> _byId = new Dictionary<int, PcMantleEntry>();
        public int Count => _byId.Count;
        public PcMantleEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMantleEntry> All => _byId.Values;
        public void Add(PcMantleEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcMantleParser
    {
        public static PcMantleRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMantleRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "mantle.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            int nextId = 1;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 13) continue;
                if (!int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int _)) continue;
                reg.Add(new PcMantleEntry
                {
                    Id = nextId++,
                    Name = cols[0].Trim(),
                    Genre = int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int g) ? g : 0,
                    Level = int.TryParse(cols[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l) ? l : 0,
                    Price = int.TryParse(cols[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) ? p : 0
                });
            }
            return reg;
        }
    }
}
