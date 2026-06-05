// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/changefeature_data.txt (Đổi ngoại hình - data) parser
// Source: changefeature_data.txt (cosmetic feature data).
//   Col 0:  Name
//   Col 1:  MagicAttribId (e.g. 126 = sát thương ngoại công)
//   Col 2:  ParamMin
//   Col 3:  ParamMax
//   Col 4:  Sword
//   Col 5:  Blade
//   ...
// We keep magic attrib id + min/max for runtime equip rate display.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcChangeFeatureDataEntry
    {
        public int featureId;
        public string name;
        public int magicAttribId;   // Mã thuộc tính ma pháp
        public int paramMin;        // Giá trị nhỏ nhất
        public int paramMax;        // Giá trị lớn nhất
    }

    public sealed class PcChangeFeatureDataRegistry
    {
        private readonly Dictionary<int, PcChangeFeatureDataEntry> _byId = new();
        private readonly Dictionary<int, List<PcChangeFeatureDataEntry>> _byCategory = new();
        public int Count => _byId.Count;

        public void Register(PcChangeFeatureDataEntry e)
        {
            if (e == null) return;
            _byId[e.featureId] = e;
            int cat = e.magicAttribId;
            if (!_byCategory.TryGetValue(cat, out var cl)) { cl = new(); _byCategory[cat] = cl; }
            cl.Add(e);
        }

        public PcChangeFeatureDataEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public List<PcChangeFeatureDataEntry> GetByCategory(int cat)
            => _byCategory.TryGetValue(cat, out var v) ? v : new List<PcChangeFeatureDataEntry>();

        public IEnumerable<PcChangeFeatureDataEntry> All => _byId.Values;
    }

    public static class PcChangeFeatureDataParser
    {
        public const int NameCol = 0;
        public const int MagicAttribIdCol = 1;
        public const int ParamMinCol = 2;
        public const int ParamMaxCol = 3;

        public static List<PcChangeFeatureDataEntry> ParseFile(string path)
        {
            var rows = new List<PcChangeFeatureDataEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                autoId++;
                rows.Add(new PcChangeFeatureDataEntry
                {
                    featureId = autoId,
                    name = PcItemCommon.Str(cols, NameCol),
                    magicAttribId = PcItemCommon.Int(cols, MagicAttribIdCol),
                    paramMin = PcItemCommon.Int(cols, ParamMinCol),
                    paramMax = PcItemCommon.Int(cols, ParamMaxCol),
                });
            }
            return rows;
        }

        public static PcChangeFeatureDataRegistry BuildRegistry(string dir)
        {
            var reg = new PcChangeFeatureDataRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "changefeature*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
