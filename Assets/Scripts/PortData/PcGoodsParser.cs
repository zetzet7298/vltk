// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/goods.txt NPC goods table parser
// Source: goods.txt (NPC shop goods override, GB2312, 23 tab columns).
//   ItemGenre  DetailType  ParticularType  FiveElementAttrib  Level  Silver  Karma  Coins
//   + 15 more (counts, prices per range, sell price, ...)
//
// Mobile keeps the first 8 numeric fields for runtime shop pricing.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGoodsParser
    {
        public static List<PcGoodsEntry> ParseFile(string path)
        {
            var rows = new List<PcGoodsEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                autoId++;
                rows.Add(new PcGoodsEntry
                {
                    id = autoId,
                    itemGenre = PcItemCommon.Int(cols, 0),
                    detailType = PcItemCommon.Int(cols, 1),
                    particularType = PcItemCommon.Int(cols, 2),
                    fiveElement = PcItemCommon.Int(cols, 3),
                    level = PcItemCommon.Int(cols, 4),
                    priceSilver = PcItemCommon.Int(cols, 5),
                    karma = PcItemCommon.Int(cols, 6),
                    coins = PcItemCommon.Int(cols, 7),
                });
            }
            return rows;
        }

        public static PcGoodsRegistry BuildRegistry(string dir)
        {
            var reg = new PcGoodsRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "goods.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcGoodsEntry
    {
        public int id;
        public int itemGenre;
        public int detailType;
        public int particularType;
        public int fiveElement;
        public int level;
        public int priceSilver;
        public int karma;
        public int coins;
    }

    public sealed class PcGoodsRegistry
    {
        private readonly Dictionary<int, PcGoodsEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcGoodsEntry e) { if (e == null || e.id <= 0) return; _byId[e.id] = e; }
        public PcGoodsEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcGoodsEntry> All() => _byId.Values;
    }
}
