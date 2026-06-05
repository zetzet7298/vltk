// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/horse.txt (Ngựa) parser
// Source: horse.txt (350 entries, GB2312, ~20 tab columns).
//   Cols 0..2: ItemGenre, DetailType, ParticularType
//   Col  3:    Name
//   Col  4:    SpritePath
//   Col  5..6: Quality, Series
//   Col  7:    Description
//   Col  8:    Genre
//   Col  9:    Cost (bạc)
//   Col  10:   ReqLevel
//   Col  11:   SeriesAttrib (0=Kim 1=Mộc 2=Thổ 3=Thủy 4=Hỏa)
// Mobile indexes by ItemGenre+DetailType+ParticularType and exposes speed/stamina
// from magic attrib columns 17+ (89 speed, 85 stamina).
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
        public int speed;        // Tốc độ ngựa (col 19)
        public int maxStamina;   // Thể lực tối đa (col 21)
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
        public const int NameCol = 3;
        public const int SpriteCol = 4;
        public const int QualityCol = 5;
        public const int SeriesCol = 6;
        public const int CostCol = 9;
        public const int RequiredLevelCol = 10;

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
                if (cols.Length < 11) continue;
                rows.Add(new PcHorseEntry
                {
                    itemGenre = PcItemCommon.Int(cols, 0),
                    detailType = PcItemCommon.Int(cols, 1),
                    particularType = PcItemCommon.Int(cols, 2),
                    name = PcItemCommon.Str(cols, NameCol),
                    spritePath = PcItemCommon.Str(cols, SpriteCol),
                    quality = PcItemCommon.Int(cols, QualityCol),
                    series = PcItemCommon.Int(cols, SeriesCol),
                    cost = PcItemCommon.Int(cols, CostCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    speed = cols.Length > 19 ? PcItemCommon.Int(cols, 19) : 0,
                    maxStamina = cols.Length > 21 ? PcItemCommon.Int(cols, 21) : 0,
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
