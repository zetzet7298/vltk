// -----------------------------------------------------------------------------
// VLTK Mobile — PC item/000/brokenequip.txt parser (trang bị tân hư)
// Source: PC item/brokenequip.txt. Mỗi dòng: Name, ItemGenre, SPR, NPC ID, Width, Height, Price, Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcBrokenEquipEntry
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int ItemGenre { get; set; }
        public int NpcId { get; set; }
        public int Price { get; set; }
    }

    public sealed class PcBrokenEquipRegistry
    {
        private readonly Dictionary<int, PcBrokenEquipEntry> _byId = new Dictionary<int, PcBrokenEquipEntry>();
        public int Count => _byId.Count;
        public PcBrokenEquipEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcBrokenEquipEntry> All => _byId.Values;
        public void Add(PcBrokenEquipEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcBrokenEquipParser
    {
        public static PcBrokenEquipRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcBrokenEquipRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "brokenequip.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);
            int nextId = 1;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 7) continue;
                // Skip header
                if (cols[0].Length == 0 || !int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int _)) continue;
                reg.Add(new PcBrokenEquipEntry
                {
                    Id = nextId++,
                    Name = cols[0].Trim(),
                    ItemGenre = int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int g) ? g : 0,
                    NpcId = int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : 0,
                    Price = int.TryParse(cols[6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int p) ? p : 0
                });
            }
            return reg;
        }
    }
}
