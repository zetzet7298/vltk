// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/item/foundryresdemand.ini Luyện Đồ (FOUNDRY) parser
// Source: server settings/item/foundryresdemand.ini (361 entries, GB2312 INI).
// Each row: ItemGenre  ItemDetail  Material1Genre Material1Detail Material1Count
//           Material2Genre Material2Detail Material2Count
//           Material3Genre Material3Detail Material3Count
// Vietnamese: "Luyện Đồ", "Nguyên Liệu", "Công Thức Đúc".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFoundryParser
    {
        public static List<PcFoundryEntry> ParseFile(string path)
        {
            var rows = new List<PcFoundryEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var raw in lines)
            {
                var line = raw;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int genre = PcItemCommon.Int(cols, 0);
                int detail = PcItemCommon.Int(cols, 1);
                if (genre <= 0 && detail <= 0) continue;
                var entry = new PcFoundryEntry
                {
                    itemGenre = genre,
                    itemDetail = detail,
                    mat1Genre = cols.Length > 2 ? PcItemCommon.Int(cols, 2) : 0,
                    mat1Detail = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 0,
                    mat1Count = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 0,
                    mat2Genre = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 0,
                    mat2Detail = cols.Length > 6 ? PcItemCommon.Int(cols, 6) : 0,
                    mat2Count = cols.Length > 7 ? PcItemCommon.Int(cols, 7) : 0,
                    mat3Genre = cols.Length > 8 ? PcItemCommon.Int(cols, 8) : 0,
                    mat3Detail = cols.Length > 9 ? PcItemCommon.Int(cols, 9) : 0,
                    mat3Count = cols.Length > 10 ? PcItemCommon.Int(cols, 10) : 0,
                };
                rows.Add(entry);
            }
            return rows;
        }

        public static PcFoundryRegistry BuildRegistry(string dir)
        {
            var reg = new PcFoundryRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "foundryresdemand.ini");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcFoundryEntry
    {
        public int itemGenre;        // Mã loại vật phẩm thành phẩm
        public int itemDetail;       // Mã chi tiết thành phẩm
        public int mat1Genre;
        public int mat1Detail;
        public int mat1Count;
        public int mat2Genre;
        public int mat2Detail;
        public int mat2Count;
        public int mat3Genre;
        public int mat3Detail;
        public int mat3Count;
    }

    public sealed class PcFoundryRegistry
    {
        private readonly Dictionary<(int, int), PcFoundryEntry> _byKey = new();
        public int Count => _byKey.Count;
        public void Register(PcFoundryEntry e)
        {
            if (e == null) return;
            var key = (e.itemGenre, e.itemDetail);
            _byKey[key] = e;
        }
        public PcFoundryEntry Get(int itemGenre, int itemDetail)
            => _byKey.TryGetValue((itemGenre, itemDetail), out var v) ? v : null;
        public IEnumerable<PcFoundryEntry> All => _byKey.Values;
    }
}
