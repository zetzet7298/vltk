// -----------------------------------------------------------------------------
// VLTK Mobile — PC item/000/shipin.txt parser (Trang sức tân thủ)
// Source: PC item/shipin.txt. Same wide format
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcShipinEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Genre { get; set; }
        public int Level { get; set; }
    }

    public sealed class PcShipinRegistry
    {
        private readonly Dictionary<int, PcShipinEntry> _byId = new Dictionary<int, PcShipinEntry>();
        public int Count => _byId.Count;
        public PcShipinEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcShipinEntry> All => _byId.Values;
        public void Add(PcShipinEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcShipinParser
    {
        public static PcShipinRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcShipinRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "shipin.txt");
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
                reg.Add(new PcShipinEntry
                {
                    Id = nextId++,
                    Name = cols[0].Trim(),
                    Genre = int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int g) ? g : 0,
                    Level = int.TryParse(cols[11], NumberStyles.Integer, CultureInfo.InvariantCulture, out int l) ? l : 0
                });
            }
            return reg;
        }
    }
}
