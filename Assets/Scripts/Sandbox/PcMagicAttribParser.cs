// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/magicattrib.txt (Magic Attribute) parser
// Source: magicattrib.txt (333 entries, GB2312, 27 tab columns).
//   Cols 0..1:  Prefix, Suffix (e.g., "Tăng", "Giảm")
//   Col  2:     IsPrefix (1=tiền tố, 0=hậu tố)
//   Col  3:     Name (e.g., "Sát thương ngoại công")
//   Col  4:     RequiredPropId
//   Col  5:     RequiredLevel
//   Col  6:     MagicAttribId (mã thuộc tính)
//   Col  7..9:  Min1, Max1, ParamType1
//   Col  10..12: Min2, Max2, ParamType2
//   ...
// Mobile indexes by MagicAttribId for runtime attribute description lookup.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcMagicAttribEntry
    {
        public int attribId;        // Mã thuộc tính
        public string name;         // Tên thuộc tính
        public int attribType;      // Loại thuộc tính
        public int paramCount;      // Số tham số
        public float valueScale;    // Tỉ lệ giá trị
        public string description;  // Mô tả
    }

    public sealed class PcMagicAttribRegistry
    {
        private readonly Dictionary<int, PcMagicAttribEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcMagicAttribEntry e)
        {
            if (e == null) return;
            _byId[e.attribId] = e;
        }

        public PcMagicAttribEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public List<PcMagicAttribEntry> GetAll() => new List<PcMagicAttribEntry>(_byId.Values);

        public IEnumerable<PcMagicAttribEntry> All => _byId.Values;
    }

    public static class PcMagicAttribParser
    {
        public const int NameCol = 3;
        public const int MagicAttribIdCol = 6;
        public const int ParamCountCol = 5;
        public const int ValueScaleCol = 8;

        public static List<PcMagicAttribEntry> ParseFile(string path)
        {
            var rows = new List<PcMagicAttribEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 7) continue;
                rows.Add(new PcMagicAttribEntry
                {
                    attribId = PcItemCommon.Int(cols, MagicAttribIdCol),
                    name = PcItemCommon.Str(cols, NameCol),
                    attribType = cols.Length > 2 ? PcItemCommon.Int(cols, 2) : 0,
                    paramCount = cols.Length > ParamCountCol ? PcItemCommon.Int(cols, ParamCountCol) : 0,
                    valueScale = cols.Length > ValueScaleCol ? (float)PcItemCommon.Int(cols, ValueScaleCol) / 100f : 1f,
                    description = cols.Length > 12 ? PcItemCommon.Str(cols, 12) : PcItemCommon.Str(cols, NameCol),
                });
            }
            return rows;
        }

        public static PcMagicAttribRegistry BuildRegistry(string dir)
        {
            var reg = new PcMagicAttribRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "magicattrib*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
