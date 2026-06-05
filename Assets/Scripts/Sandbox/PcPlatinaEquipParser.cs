// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/platinaequip.txt (Trang bị Bạch Kim) parser
// Source: platinaequip.txt (5,336 entries, GB2312, ~70 tab columns).
//   Same shape as goldequip.txt but Bạch Kim (platina) tier.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcPlatinaEquipEntry
    {
        public int itemGenre;
        public int detailType;
        public int particularType;
        public string name;
        public string spritePath;
        public int equipPoint;
        public int series;
        public int quality;
        public int requiredLevel;
    }

    public sealed class PcPlatinaEquipRegistry
    {
        private readonly Dictionary<long, PcPlatinaEquipEntry> _byKey = new();
        public int Count => _byKey.Count;

        public void Register(PcPlatinaEquipEntry e)
        {
            if (e == null) return;
            long k = MakeKey(e.itemGenre, e.detailType, e.particularType);
            _byKey[k] = e;
        }

        public PcPlatinaEquipEntry Get(int genre, int detail, int particular)
            => _byKey.TryGetValue(MakeKey(genre, detail, particular), out var v) ? v : null;

        public List<PcPlatinaEquipEntry> GetBySeries(int series)
        {
            var list = new List<PcPlatinaEquipEntry>();
            foreach (var e in _byKey.Values) if (e.series == series) list.Add(e);
            return list;
        }

        public IEnumerable<PcPlatinaEquipEntry> All => _byKey.Values;

        private static long MakeKey(int g, int d, int p) => ((long)g << 32) | ((long)d << 16) | (uint)p;
    }

    public static class PcPlatinaEquipParser
    {
        public const int NameCol = 3;
        public const int SpritePathCol = 4;
        public const int EquipPointCol = 5;
        public const int SeriesCol = 6;
        public const int QualityCol = 7;
        public const int RequiredLevelCol = 8;

        public static List<PcPlatinaEquipEntry> ParseFile(string path)
        {
            var rows = new List<PcPlatinaEquipEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 9) continue;
                rows.Add(new PcPlatinaEquipEntry
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

        public static PcPlatinaEquipRegistry BuildRegistry(string dir)
        {
            var reg = new PcPlatinaEquipRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "platinaequip*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
