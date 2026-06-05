// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/hongbaosetting.ini Hồng Bao (lì xì) parser
// Source: hongbaosetting.ini (69 entries, GB2312, tab-separated).
//   Id  Type  ItemGenre  ItemDetail  ItemParticular  Count
//   MinLevel  MaxLevel  Silver  Karma
// Hồng bao = quà tặng sự kiện Tết / sinh nhật / event login.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcHongbaoParser
    {
        public const int IdCol = 0;
        public const int TypeCol = 1;
        public const int ItemGenreCol = 2;
        public const int ItemDetailCol = 3;
        public const int ItemParticularCol = 4;
        public const int CountCol = 5;
        public const int MinLevelCol = 6;
        public const int MaxLevelCol = 7;
        public const int SilverCol = 8;
        public const int KarmaCol = 9;

        public static List<PcHongbaoEntry> ParseFile(string path)
        {
            var rows = new List<PcHongbaoEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                int id = PcItemCommon.Int(cols, IdCol);
                if (id <= 0) continue;
                rows.Add(new PcHongbaoEntry
                {
                    id = id,
                    type = PcItemCommon.Int(cols, TypeCol),
                    itemGenre = PcItemCommon.Int(cols, ItemGenreCol),
                    itemDetail = PcItemCommon.Int(cols, ItemDetailCol),
                    itemParticular = PcItemCommon.Int(cols, ItemParticularCol),
                    count = PcItemCommon.Int(cols, CountCol),
                    minLevel = cols.Length > MinLevelCol ? PcItemCommon.Int(cols, MinLevelCol) : 0,
                    maxLevel = cols.Length > MaxLevelCol ? PcItemCommon.Int(cols, MaxLevelCol) : 0,
                    silver = cols.Length > SilverCol ? PcItemCommon.Int(cols, SilverCol) : 0,
                    karma = cols.Length > KarmaCol ? PcItemCommon.Int(cols, KarmaCol) : 0,
                });
            }
            return rows;
        }

        public static PcHongbaoRegistry BuildRegistry(string dir)
        {
            var reg = new PcHongbaoRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "hongbaosetting.ini");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcHongbaoEntry
    {
        public int id;
        public int type;
        public int itemGenre;
        public int itemDetail;
        public int itemParticular;
        public int count;
        public int minLevel;
        public int maxLevel;
        public int silver;
        public int karma;
    }

    public sealed class PcHongbaoRegistry
    {
        private readonly Dictionary<int, PcHongbaoEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcHongbaoEntry e)
        {
            if (e == null || e.id <= 0) return;
            _byId[e.id] = e;
        }
        public PcHongbaoEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcHongbaoEntry> GetAll() => _byId.Values;
    }
}
