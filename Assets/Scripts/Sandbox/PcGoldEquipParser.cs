// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/goldequip.txt (Trang bị Hoàng Kim) parser
// Source: goldequip.txt (5,346 entries, GB2312, ~60 tab columns).
//   Cols 0..3: ItemGenre, DetailType, ParticularType, Name
//   Cols 4..5: SpritePath, EquipPoint
//   Cols 6..7: Series (ngũ hành 0=Kim 1=Mộc 2=Thổ 3=Thủy 4=Hỏa), Quality
//   Col  8:    RequiredLevel
// Mobile keeps the first 9 columns for runtime equip lookup.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcGoldEquipEntry
    {
        public int itemGenre;        // Loại trang bị
        public int detailType;       // Loại chi tiết
        public int particularType;   // Loại đặc biệt
        public string name;          // Tên trang bị
        public string spritePath;    // Đường dẫn sprite
        public int equipPoint;       // Vị trí trang bị (đầu, thân, ...)
        public int series;           // Ngũ hành (0=Kim 1=Mộc 2=Thổ 3=Thủy 4=Hỏa)
        public int quality;          // Phẩm chất
        public int requiredLevel;    // Cấp yêu cầu
    }

    public sealed class PcGoldEquipRegistry
    {
        private readonly Dictionary<long, PcGoldEquipEntry> _byKey = new();
        public int Count => _byKey.Count;

        public void Register(PcGoldEquipEntry e)
        {
            if (e == null) return;
            long k = MakeKey(e.itemGenre, e.detailType, e.particularType);
            _byKey[k] = e;
        }

        public PcGoldEquipEntry Get(int genre, int detail, int particular)
            => _byKey.TryGetValue(MakeKey(genre, detail, particular), out var v) ? v : null;

        public List<PcGoldEquipEntry> GetBySeries(int series)
        {
            var list = new List<PcGoldEquipEntry>();
            foreach (var e in _byKey.Values) if (e.series == series) list.Add(e);
            return list;
        }

        public List<PcGoldEquipEntry> GetByLevel(int level)
        {
            var list = new List<PcGoldEquipEntry>();
            foreach (var e in _byKey.Values) if (e.requiredLevel == level) list.Add(e);
            return list;
        }

        public IEnumerable<PcGoldEquipEntry> All => _byKey.Values;

        private static long MakeKey(int g, int d, int p) => ((long)g << 32) | ((long)d << 16) | (uint)p;
    }

    public static class PcGoldEquipParser
    {
        public const int NameCol = 3;
        public const int SpritePathCol = 4;
        public const int EquipPointCol = 5;
        public const int SeriesCol = 6;
        public const int QualityCol = 7;
        public const int RequiredLevelCol = 8;

        public static List<PcGoldEquipEntry> ParseFile(string path)
        {
            var rows = new List<PcGoldEquipEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 9) continue;
                rows.Add(new PcGoldEquipEntry
                {
                    itemGenre = PcItemCommon.Int(cols, 0),
                    detailType = PcItemCommon.Int(cols, 1),
                    particularType = PcItemCommon.Int(cols, 2),
                    name = PcItemCommon.Str(cols, NameCol),
                    spritePath = PcItemCommon.Str(cols, SpritePathCol),
                    equipPoint = PcItemCommon.Int(cols, EquipPointCol),
                    series = PcItemCommon.Int(cols, SeriesCol),
                    quality = PcItemCommon.Int(cols, QualityCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                });
            }
            return rows;
        }

        public static PcGoldEquipRegistry BuildRegistry(string dir)
        {
            var reg = new PcGoldEquipRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "goldequip*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
