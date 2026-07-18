// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/buysell.txt shop (cửa hàng) parser
// Source: buysell.txt (1,521 entries, GB2312, 117 tab columns).
//   Each row is one shop with up to 117 item slots. We only expose a few
//   primary slots for mobile runtime lookup.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcShopParser
    {
        public static List<PcShopEntry> ParseFile(string path)
        {
            var rows = new List<PcShopEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                autoId++;
                var shop = new PcShopEntry { shopId = autoId, nameRaw = PcItemCommon.Str(cols, 0) };
                for (int i = 1; i < cols.Length && i <= 16; i++)
                {
                    int itemId = PcItemCommon.Int(cols, i);
                    if (itemId > 0) shop.itemIds.Add(itemId);
                }
                if (shop.itemIds.Count > 0 || !string.IsNullOrEmpty(shop.nameRaw))
                    rows.Add(shop);
            }
            return rows;
        }

        public static PcShopRegistry BuildRegistry(string dir)
        {
            var reg = new PcShopRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "buysell.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcShopEntry
    {
        public int shopId;
        public string nameRaw;
        public List<int> itemIds = new();
    }

    public sealed class PcShopRegistry
    {
        private readonly Dictionary<int, PcShopEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcShopEntry e) { if (e == null || e.shopId <= 0) return; _byId[e.shopId] = e; }
        public PcShopEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcShopEntry> All => _byId.Values;
    }
}
