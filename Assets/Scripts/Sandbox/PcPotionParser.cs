// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/potion.txt (Thuốc / Vật phẩm tiêu hao) parser
// Source: potion.txt (40+ entries, GB2312, ~20 tab columns).
//   Cols 0..2: ItemGenre, DetailType, ParticularType
//   Col  3:    Name
//   Col  4:    SpritePath
//   Col  5:    Quality
//   Col  6..7: Series, Type
//   Col  8:    Description
//   Col  9:    Genre
//   Col  10:   Cost
//   Col  11:   ReqLevel
//   Col  12:   PotionType (sub-type)
//   Col  13+:  Effects (1..5 effect rows: type, value, duration)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcPotionEntry
    {
        public int itemGenre;
        public int detailType;
        public int particularType;
        public string name;
        public string spritePath;
        public int quality;
        public int series;
        public int type;        // 0=hp, 1=mp, 2=stamina, 3=cure, 4=buff
        public int cost;
        public int requiredLevel;
        public int healAmount;  // Lượng hồi máu/nội lực/thể lực
        public int cooldownSec; // Thời gian cooldown (giây)
    }

    public sealed class PcPotionRegistry
    {
        private readonly Dictionary<long, PcPotionEntry> _byKey = new();
        public int Count => _byKey.Count;

        public void Register(PcPotionEntry e)
        {
            if (e == null) return;
            long k = MakeKey(e.itemGenre, e.detailType, e.particularType);
            _byKey[k] = e;
        }

        public PcPotionEntry Get(int genre, int detail, int particular)
            => _byKey.TryGetValue(MakeKey(genre, detail, particular), out var v) ? v : null;

        public List<PcPotionEntry> GetByType(int type)
        {
            var list = new List<PcPotionEntry>();
            foreach (var e in _byKey.Values) if (e.type == type) list.Add(e);
            return list;
        }

        public IEnumerable<PcPotionEntry> All => _byKey.Values;

        private static long MakeKey(int g, int d, int p) => ((long)g << 32) | ((long)d << 16) | (uint)p;
    }

    public static class PcPotionParser
    {
        public const int NameCol = 3;
        public const int SpriteCol = 4;
        public const int QualityCol = 5;
        public const int SeriesCol = 6;
        public const int TypeCol = 7;
        public const int CostCol = 10;
        public const int ReqLevelCol = 11;
        public const int HealAmountCol = 14;  // First effect value
        public const int CooldownCol = 15;    // First effect duration

        public static List<PcPotionEntry> ParseFile(string path)
        {
            var rows = new List<PcPotionEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 12) continue;
                rows.Add(new PcPotionEntry
                {
                    itemGenre = PcItemCommon.Int(cols, 0),
                    detailType = PcItemCommon.Int(cols, 1),
                    particularType = PcItemCommon.Int(cols, 2),
                    name = PcItemCommon.Str(cols, NameCol),
                    spritePath = PcItemCommon.Str(cols, SpriteCol),
                    quality = PcItemCommon.Int(cols, QualityCol),
                    series = PcItemCommon.Int(cols, SeriesCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                    cost = PcItemCommon.Int(cols, CostCol),
                    requiredLevel = PcItemCommon.Int(cols, ReqLevelCol),
                    healAmount = cols.Length > HealAmountCol ? PcItemCommon.Int(cols, HealAmountCol) : 0,
                    cooldownSec = cols.Length > CooldownCol ? PcItemCommon.Int(cols, CooldownCol) : 0,
                });
            }
            return rows;
        }

        public static PcPotionRegistry BuildRegistry(string dir)
        {
            var reg = new PcPotionRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "potion*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
