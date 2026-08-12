// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/shop_* Shop Config parser (Cấu Hình Cửa Hàng)
// Source: settings/shops/shop_xxx.txt (1,521 entries, GB2312, tab-separated).
//   Cols: ShopId  ItemId  Price  Currency  Stock  MaxStock  RestockSec
//         RequiredLevel  RequiredFame
// Currency: 0 = bạc, 1 = bạc khóa, 2 = đồng, 3 = điểm PK, 4 = điểm danh vọng
// Tolerant of missing file (trả về registry rỗng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcShopConfigParser
    {
        public const int ShopIdCol = 0;
        public const int ItemIdCol = 1;
        public const int PriceCol = 2;
        public const int CurrencyCol = 3;
        public const int StockCol = 4;
        public const int MaxStockCol = 5;
        public const int RestockSecCol = 6;
        public const int RequiredLevelCol = 7;
        public const int RequiredFameCol = 8;

        public const int CurrencyBac = 0;
        public const int CurrencyBacKhoa = 1;
        public const int CurrencyDong = 2;
        public const int CurrencyDiemPK = 3;
        public const int CurrencyDanhVong = 4;

        public static List<ShopConfigEntry> ParseFile(string path)
        {
            var rows = new List<ShopConfigEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path).ToArray(); }
            catch { try { lines = File.ReadAllLines(path); } catch { return rows; } }
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int shopId = PcItemCommon.Int(cols, ShopIdCol);
                int itemId = PcItemCommon.Int(cols, ItemIdCol);
                if (shopId <= 0 && itemId <= 0) continue;
                rows.Add(new ShopConfigEntry
                {
                    shopId = shopId,
                    itemId = itemId,
                    price = PcItemCommon.Int(cols, PriceCol),
                    currency = PcItemCommon.Int(cols, CurrencyCol),
                    stock = PcItemCommon.Int(cols, StockCol),
                    maxStock = PcItemCommon.Int(cols, MaxStockCol),
                    restockSec = PcItemCommon.Int(cols, RestockSecCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    requiredFame = PcItemCommon.Int(cols, RequiredFameCol),
                });
            }
            return rows;
        }

        public static ShopConfigRegistry BuildRegistry(string dir)
        {
            var reg = new ShopConfigRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Quét shop_*.txt hoặc tất cả *.txt trong shops/
            foreach (var f in Directory.GetFiles(dir, "shop_*.txt", SearchOption.AllDirectories))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            // Nếu không có file shop_*, thử đọc trực tiếp
            if (reg.Count == 0)
            {
                foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class ShopConfigEntry
    {
        public int shopId;
        public int itemId;
        public int price;
        public int currency;       // 0=bạc, 1=bạc khóa, 2=đồng, 3=điểm PK, 4=danh vọng
        public int stock;          // 0 = unlimited
        public int maxStock;
        public int restockSec;     // 0 = không restock
        public int requiredLevel;
        public int requiredFame;
    }

    public sealed class ShopConfigRegistry
    {
        private readonly Dictionary<int, ShopConfigEntry> _byId = new();
        private readonly Dictionary<int, List<ShopConfigEntry>> _byShop = new();
        private readonly Dictionary<int, List<ShopConfigEntry>> _byItem = new();
        public int Count => _byId.Count;
        public IEnumerable<ShopConfigEntry> All => _byId.Values;

        public void Register(ShopConfigEntry e)
        {
            if (e == null) return;
            int key = (e.shopId << 16) | (e.itemId & 0xffff);
            if (key <= 0) return;
            _byId[key] = e;
            if (!_byShop.TryGetValue(e.shopId, out var sList))
            {
                sList = new List<ShopConfigEntry>();
                _byShop[e.shopId] = sList;
            }
            sList.Add(e);
            if (!_byItem.TryGetValue(e.itemId, out var iList))
            {
                iList = new List<ShopConfigEntry>();
                _byItem[e.itemId] = iList;
            }
            iList.Add(e);
        }
        public ShopConfigEntry Get(int shopId, int itemId)
        {
            int key = (shopId << 16) | (itemId & 0xffff);
            return _byId.TryGetValue(key, out var v) ? v : null;
        }
        public IReadOnlyList<ShopConfigEntry> GetByShop(int shopId)
            => _byShop.TryGetValue(shopId, out var v)
                ? (IReadOnlyList<ShopConfigEntry>)v
                : (IReadOnlyList<ShopConfigEntry>)System.Array.Empty<ShopConfigEntry>();
        public IReadOnlyList<ShopConfigEntry> GetByItem(int itemId)
            => _byItem.TryGetValue(itemId, out var v)
                ? (IReadOnlyList<ShopConfigEntry>)v
                : (IReadOnlyList<ShopConfigEntry>)System.Array.Empty<ShopConfigEntry>();
    }
}
