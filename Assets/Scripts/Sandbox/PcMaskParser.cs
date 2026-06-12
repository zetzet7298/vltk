// -----------------------------------------------------------------------------
// VLTK Mobile — PC item/000/mask.txt parser (Mặt nạ)
// Source: PC item/mask.txt. Same wide format as mantle
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcMaskEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ItemGenre { get; set; }
        public int DetailType { get; set; }
        public int ParticularType { get; set; }
        public int Level { get; set; }
        public int Price { get; set; }
    }

    public sealed class PcMaskRegistry
    {
        private readonly Dictionary<int, PcMaskEntry> _byId = new Dictionary<int, PcMaskEntry>();
        public int Count => _byId.Count;
        public PcMaskEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMaskEntry> All => _byId.Values;
        public void Add(PcMaskEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcMaskParser
    {
        public static PcMaskRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMaskRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "mask.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);
            int nextId = 1;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 13) continue;
                if (!int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int _)) continue;
                reg.Add(new PcMaskEntry
                {
                    Id = nextId++,
                    Name = cols[0].Trim(),
                    ItemGenre = int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int g) ? g : 0,
                    DetailType = int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int d) ? d : 0,
                    ParticularType = int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) ? p : 0,
                    Level = int.TryParse(cols[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l) ? l : 0,
                    Price = int.TryParse(cols[10], NumberStyles.Integer, CultureInfo.InvariantCulture, out int pr) ? pr : 0
                });
            }
            return reg;
        }
    }
}
