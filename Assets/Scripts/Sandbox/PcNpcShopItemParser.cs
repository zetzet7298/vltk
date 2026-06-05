// -----------------------------------------------------------------------------
// VLTK Mobile — PC npcshopitem.txt NPC shop item parser
// Source: server settings/npcshopitem.txt (Reference/PcShop).
// Cols: ShopNpcId, SlotIdx, ItemId, ItemCount, Price, Currency,
//       RestockSec, RequiredReputation
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcNpcShopItemParser
    {
        public const int ShopNpcIdCol = 0;
        public const int SlotIdxCol = 1;
        public const int ItemIdCol = 2;
        public const int ItemCountCol = 3;
        public const int PriceCol = 4;
        public const int CurrencyCol = 5;
        public const int RestockSecCol = 6;
        public const int RequiredReputationCol = 7;

        public static List<PcNpcShopItemEntry> ParseFile(string path)
        {
            var rows = new List<PcNpcShopItemEntry>();
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
                int shopNpc = PcItemCommon.Int(cols, ShopNpcIdCol);
                if (shopNpc <= 0) continue;
                rows.Add(new PcNpcShopItemEntry
                {
                    id = autoId,
                    shopNpcId = shopNpc,
                    slotIdx = PcItemCommon.Int(cols, SlotIdxCol),
                    itemId = PcItemCommon.Int(cols, ItemIdCol),
                    itemCount = PcItemCommon.Int(cols, ItemCountCol),
                    price = PcItemCommon.Int(cols, PriceCol),
                    currency = PcItemCommon.Int(cols, CurrencyCol),
                    restockSec = PcItemCommon.Int(cols, RestockSecCol),
                    requiredReputation = PcItemCommon.Int(cols, RequiredReputationCol),
                });
            }
            return rows;
        }

        public static PcNpcShopItemRegistry BuildRegistry(string dir)
        {
            var reg = new PcNpcShopItemRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcNpcShopItemEntry
    {
        public int id;
        public int shopNpcId;
        public int slotIdx;
        public int itemId;
        public int itemCount;
        public int price;
        public int currency; // 0=xu, 1=luong, 2=bind xu
        public int restockSec;
        public int requiredReputation;
    }

    public sealed class PcNpcShopItemRegistry
    {
        private readonly Dictionary<int, PcNpcShopItemEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcNpcShopItemEntry e) { if (e == null || e.id <= 0) return; _byId[e.id] = e; }
        public PcNpcShopItemEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcNpcShopItemEntry> GetByShop(int shopNpcId)
        {
            var list = new List<PcNpcShopItemEntry>();
            foreach (var e in _byId.Values)
                if (e.shopNpcId == shopNpcId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcNpcShopItemEntry> GetByItem(int itemId)
        {
            var list = new List<PcNpcShopItemEntry>();
            foreach (var e in _byId.Values)
                if (e.itemId == itemId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcNpcShopItemEntry> All => new List<PcNpcShopItemEntry>(_byId.Values);
    }
}
