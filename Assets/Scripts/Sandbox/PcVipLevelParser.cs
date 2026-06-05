// -----------------------------------------------------------------------------
// VLTK Mobile — PC viplevel.txt VIP level parser
// Source: server settings/viplevel.txt (Reference/PcVip).
// Cols: VipLevel, RequiredRecharge, DailyGoldBonus, DailyExpBonus,
//       ShopDiscount, MaxBuyPerDay, MallAccess, Color
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcVipLevelParser
    {
        public const int VipLevelCol = 0;
        public const int RequiredRechargeCol = 1;
        public const int DailyGoldBonusCol = 2;
        public const int DailyExpBonusCol = 3;
        public const int ShopDiscountCol = 4;
        public const int MaxBuyPerDayCol = 5;
        public const int MallAccessCol = 6;
        public const int ColorCol = 7;

        public static List<PcVipLevelEntry> ParseFile(string path)
        {
            var rows = new List<PcVipLevelEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int vip = PcItemCommon.Int(cols, VipLevelCol);
                if (vip <= 0) continue;
                rows.Add(new PcVipLevelEntry
                {
                    vipLevel = vip,
                    requiredRecharge = long.TryParse(PcItemCommon.Str(cols, RequiredRechargeCol), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var r) ? r : 0,
                    dailyGoldBonus = PcItemCommon.Int(cols, DailyGoldBonusCol),
                    dailyExpBonus = PcItemCommon.Int(cols, DailyExpBonusCol),
                    shopDiscount = float.TryParse(PcItemCommon.Str(cols, ShopDiscountCol), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : 0f,
                    maxBuyPerDay = PcItemCommon.Int(cols, MaxBuyPerDayCol),
                    mallAccess = PcItemCommon.Int(cols, MallAccessCol) != 0,
                    color = PcItemCommon.Str(cols, ColorCol),
                });
            }
            return rows;
        }

        public static PcVipLevelRegistry BuildRegistry(string dir)
        {
            var reg = new PcVipLevelRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcVipLevelEntry
    {
        public int vipLevel;
        public long requiredRecharge;
        public int dailyGoldBonus;
        public int dailyExpBonus;
        public float shopDiscount; // 0.0..1.0
        public int maxBuyPerDay;
        public bool mallAccess;
        public string color;
    }

    public sealed class PcVipLevelRegistry
    {
        private readonly Dictionary<int, PcVipLevelEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcVipLevelEntry e) { if (e == null || e.vipLevel <= 0) return; _byId[e.vipLevel] = e; }
        public PcVipLevelEntry GetVipLevel(int vipLevel) => _byId.TryGetValue(vipLevel, out var v) ? v : null;
        public IReadOnlyList<PcVipLevelEntry> All => new List<PcVipLevelEntry>(_byId.Values);
    }
}
