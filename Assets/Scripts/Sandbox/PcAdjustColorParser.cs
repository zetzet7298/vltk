// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings adjustcolor.txt parser
// Source: adjustcolor.txt (điều chỉnh màu sắc).
// Columns: SettingId  R  G  B  A  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcAdjustColorParser
    {
        public const int SettingIdCol = 0;
        public const int RCol = 1;
        public const int GCol = 2;
        public const int BCol = 3;
        public const int ACol = 4;
        public const int DescriptionCol = 5;

        public static List<PcAdjustColorEntry> ParseFile(string path)
        {
            var rows = new List<PcAdjustColorEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, SettingIdCol);
                if (id <= 0) continue;
                rows.Add(new PcAdjustColorEntry
                {
                    settingId = id,
                    r = PcItemCommon.Int(cols, RCol),
                    g = PcItemCommon.Int(cols, GCol),
                    b = PcItemCommon.Int(cols, BCol),
                    a = PcItemCommon.Int(cols, ACol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcAdjustColorRegistry BuildRegistry(string dir)
        {
            var reg = new PcAdjustColorRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcAdjustColorEntry
    {
        public int settingId;
        public int r;
        public int g;
        public int b;
        public int a;
        public string description;
    }

    public sealed class PcAdjustColorRegistry
    {
        private readonly Dictionary<int, PcAdjustColorEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcAdjustColorEntry e) { if (e == null || e.settingId <= 0) return; _byId[e.settingId] = e; }
        public PcAdjustColorEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcAdjustColorEntry> All => new List<PcAdjustColorEntry>(_byId.Values);
    }
}
