// -----------------------------------------------------------------------------
// VLTK Mobile — PC mall.txt parser
// Source: settings/shop/mall.txt (Cửa Hàng).
// Columns: MallItemId ItemId Price Currency Discount RequiredVipLevel
//          Stock MaxBuyPerDay StartTimeUnix EndTimeUnix
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMallParser
    {
        public const int MallItemIdCol = 0;
        public const int ItemIdCol = 1;
        public const int PriceCol = 2;
        public const int CurrencyCol = 3;
        public const int DiscountCol = 4;
        public const int RequiredVipLevelCol = 5;
        public const int StockCol = 6;
        public const int MaxBuyPerDayCol = 7;
        public const int StartTimeUnixCol = 8;
        public const int EndTimeUnixCol = 9;

        public static List<PcMallEntry> ParseFile(string path)
        {
            var rows = new List<PcMallEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, MallItemIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMallEntry
                {
                    mallItemId = id,
                    itemId = PcItemCommon.Int(cols, ItemIdCol),
                    price = PcItemCommon.Int(cols, PriceCol),
                    currency = PcItemCommon.Int(cols, CurrencyCol),
                    discount = PcItemCommon.Int(cols, DiscountCol),
                    requiredVipLevel = PcItemCommon.Int(cols, RequiredVipLevelCol),
                    stock = PcItemCommon.Int(cols, StockCol),
                    maxBuyPerDay = PcItemCommon.Int(cols, MaxBuyPerDayCol),
                    startTimeUnix = PcItemCommon.Int(cols, StartTimeUnixCol),
                    endTimeUnix = PcItemCommon.Int(cols, EndTimeUnixCol),
                });
            }
            return rows;
        }

        public static PcMallRegistry BuildRegistry(string dir)
        {
            var reg = new PcMallRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("mall"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcMallEntry
    {
        public int mallItemId;
        public int itemId;
        public int price;
        public int currency;
        public int discount;
        public int requiredVipLevel;
        public int stock;
        public int maxBuyPerDay;
        public int startTimeUnix;
        public int endTimeUnix;
    }

    public sealed class PcMallRegistry
    {
        private readonly Dictionary<int, PcMallEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMallEntry e) { if (e == null || e.mallItemId <= 0) return; _byId[e.mallItemId] = e; }
        public PcMallEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcMallEntry> GetByItem(int itemId)
        {
            var list = new List<PcMallEntry>();
            foreach (var e in _byId.Values)
                if (e.itemId == itemId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMallEntry> GetForVip(int vipLevel)
        {
            var list = new List<PcMallEntry>();
            foreach (var e in _byId.Values)
                if (e.requiredVipLevel <= vipLevel) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMallEntry> All => new List<PcMallEntry>(_byId.Values);
    }
}
