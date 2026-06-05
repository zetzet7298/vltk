// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings item_detail.txt parser
// Source: item_detail.txt (chi tiết vật phẩm, 202 entries).
// Columns: DetailId  DetailName  Category  EquipSlot  RequiredLevel  MaxStack
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcItemDetailParser
    {
        public const int DetailIdCol = 0;
        public const int DetailNameCol = 1;
        public const int CategoryCol = 2;
        public const int EquipSlotCol = 3;
        public const int RequiredLevelCol = 4;
        public const int MaxStackCol = 5;

        public static List<PcItemDetailEntry> ParseFile(string path)
        {
            var rows = new List<PcItemDetailEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, DetailIdCol);
                if (id <= 0) continue;
                rows.Add(new PcItemDetailEntry
                {
                    detailId = id,
                    detailName = PcItemCommon.Str(cols, DetailNameCol),
                    category = PcItemCommon.Int(cols, CategoryCol),
                    equipSlot = PcItemCommon.Int(cols, EquipSlotCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    maxStack = PcItemCommon.Int(cols, MaxStackCol),
                });
            }
            return rows;
        }

        public static PcItemDetailRegistry BuildRegistry(string dir)
        {
            var reg = new PcItemDetailRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcItemDetailEntry
    {
        public int detailId;
        public string detailName;
        public int category;
        public int equipSlot;
        public int requiredLevel;
        public int maxStack;
    }

    public sealed class PcItemDetailRegistry
    {
        private readonly Dictionary<int, PcItemDetailEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcItemDetailEntry e) { if (e == null || e.detailId <= 0) return; _byId[e.detailId] = e; }
        public PcItemDetailEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcItemDetailEntry> GetByCategory(int category)
        {
            var list = new List<PcItemDetailEntry>();
            foreach (var e in _byId.Values)
                if (e.category == category) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcItemDetailEntry> All => new List<PcItemDetailEntry>(_byId.Values);
    }
}
