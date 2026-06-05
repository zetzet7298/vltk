// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings item_type.txt parser
// Source: item_type.txt (loại vật phẩm).
// Columns: TypeId  TypeName  MapLimit  IsConsumable  IsTradeable
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcItemTypeParser
    {
        public const int TypeIdCol = 0;
        public const int TypeNameCol = 1;
        public const int MapLimitCol = 2;
        public const int IsConsumableCol = 3;
        public const int IsTradeableCol = 4;

        public static List<PcItemTypeEntry> ParseFile(string path)
        {
            var rows = new List<PcItemTypeEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TypeIdCol);
                if (id <= 0) continue;
                rows.Add(new PcItemTypeEntry
                {
                    typeId = id,
                    typeName = PcItemCommon.Str(cols, TypeNameCol),
                    mapLimit = PcItemCommon.Str(cols, MapLimitCol),
                    isConsumable = PcItemCommon.Int(cols, IsConsumableCol) != 0,
                    isTradeable = PcItemCommon.Int(cols, IsTradeableCol) != 0,
                });
            }
            return rows;
        }

        public static PcItemTypeRegistry BuildRegistry(string dir)
        {
            var reg = new PcItemTypeRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcItemTypeEntry
    {
        public int typeId;
        public string typeName;
        public string mapLimit;
        public bool isConsumable;
        public bool isTradeable;
    }

    public sealed class PcItemTypeRegistry
    {
        private readonly Dictionary<int, PcItemTypeEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcItemTypeEntry e) { if (e == null || e.typeId <= 0) return; _byId[e.typeId] = e; }
        public PcItemTypeEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcItemTypeEntry> GetAll() => new List<PcItemTypeEntry>(_byId.Values);
        public IReadOnlyList<PcItemTypeEntry> All => new List<PcItemTypeEntry>(_byId.Values);
    }
}
