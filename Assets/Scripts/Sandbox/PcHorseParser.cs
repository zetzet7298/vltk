// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/horse.txt (Ngựa) parser
// Source (verified GB2312, 46 tab-separated columns, 350 data rows):
//   /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/server/
//     home_jxser_bachkim_6.0/server1/settings/item/004/horse.txt
//
// VERIFIED column layout (header read directly from PC source):
//   col 0  名称              Name
//   col 1  ItemGenre         (=0 for every horse)
//   col 2  DetailType        (=10 for every horse)
//   col 3  ParticularType    (0..34 — the per-horse-family discriminator)
//   col 4  动画文件名        Animation/sprite file path
//   col 5  对应物件索引      Object index (=40 for every horse)
//   col 6  宽度              Width
//   col 7  高度              Height
//   col 8  说明文字          Description
//   col 9  五行属性          Series / element (0..6)
//   col 10 价格              Price (bạc)
//   col 11 等级              Required level (1..10)
//   col 12 是否叠放          Stackable
//   col 13..33               7 × base-attr triplets (type, min, max)
//   col 34..45               6 × require-attr pairs  (type, value)
//
// Mobile indexes by the verified key (ItemGenre, DetailType, ParticularType) =
// (0, 10, 0..34). The previous map was off-by-one (it read the key from cols
// 0,1,2 — i.e. Name,ItemGenre,DetailType — so GetHorse(i,0,0) never matched).
//
// NOTE on speed / stamina: PC stores these as type-coded magic attributes inside
// the col 13..33 triplets (attribute-type ids such as 85/93/233/241), NOT in a
// fixed column. Faithfully decoding which type id means "speed" vs "stamina"
// requires the engine attribute-type table, which is not present in the source
// tree. Per jx-pc-port-rule we do NOT guess: speed/maxStamina are left 0 until
// that table is recovered. No runtime consumer reads them today.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcHorseEntry
    {
        public int itemGenre;
        public int detailType;
        public int particularType;
        public string name;
        public string spritePath;
        public int quality;
        public int series;
        public int cost;
        public int requiredLevel;
        public int speed;        // type-coded magic attr (cols 13..33) — not decoded, 0
        public int maxStamina;   // type-coded magic attr (cols 13..33) — not decoded, 0
    }

    public sealed class PcHorseRegistry
    {
        private readonly Dictionary<long, PcHorseEntry> _byKey = new();
        public int Count => _byKey.Count;

        public void Register(PcHorseEntry e)
        {
            if (e == null) return;
            long k = MakeKey(e.itemGenre, e.detailType, e.particularType);
            _byKey[k] = e;
        }

        public PcHorseEntry Get(int genre, int detail, int particular)
            => _byKey.TryGetValue(MakeKey(genre, detail, particular), out var v) ? v : null;

        public List<PcHorseEntry> GetByLevel(int level)
        {
            var list = new List<PcHorseEntry>();
            foreach (var e in _byKey.Values) if (e.requiredLevel == level) list.Add(e);
            return list;
        }

        public List<PcHorseEntry> GetBySeries(int series)
        {
            var list = new List<PcHorseEntry>();
            foreach (var e in _byKey.Values) if (e.series == series) list.Add(e);
            return list;
        }

        public IEnumerable<PcHorseEntry> All => _byKey.Values;

        private static long MakeKey(int g, int d, int p) => ((long)g << 32) | ((long)d << 16) | (uint)p;
    }

    public static class PcHorseParser
    {
        // Verified PC horse.txt column indices (see file header for full layout).
        public const int NameCol = 0;            // 名称
        public const int ItemGenreCol = 1;       // ItemGenre  (=0)
        public const int DetailTypeCol = 2;      // DetailType (=10)
        public const int ParticularTypeCol = 3;  // ParticularType (0..34)
        public const int SpriteCol = 4;          // 动画文件名
        public const int ObjectIndexCol = 5;     // 对应物件索引 (=40)
        public const int SeriesCol = 9;          // 五行属性 (0..6)
        public const int CostCol = 10;           // 价格 (bạc)
        public const int RequiredLevelCol = 11;  // 等级 (1..10)
        public const int MinColumns = 46;

        public static List<PcHorseEntry> ParseFile(string path)
        {
            var rows = new List<PcHorseEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < MinColumns) continue;
                rows.Add(new PcHorseEntry
                {
                    itemGenre = PcItemCommon.Int(cols, ItemGenreCol),
                    detailType = PcItemCommon.Int(cols, DetailTypeCol),
                    particularType = PcItemCommon.Int(cols, ParticularTypeCol),
                    name = PcItemCommon.Str(cols, NameCol),
                    spritePath = PcItemCommon.Str(cols, SpriteCol),
                    // horse.txt has no 品质/quality column; reuse 对应物件索引 (object index)
                    // as a stable per-row tag rather than inventing a value.
                    quality = PcItemCommon.Int(cols, ObjectIndexCol),
                    series = PcItemCommon.Int(cols, SeriesCol),
                    cost = PcItemCommon.Int(cols, CostCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    // speed/maxStamina are type-coded magic attributes (cols 13..33);
                    // not decodable without the engine attribute-type table — left 0.
                    speed = 0,
                    maxStamina = 0,
                });
            }
            return rows;
        }

        public static PcHorseRegistry BuildRegistry(string dir)
        {
            var reg = new PcHorseRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "horse*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
