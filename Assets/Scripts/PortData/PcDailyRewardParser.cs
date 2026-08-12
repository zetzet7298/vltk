// -----------------------------------------------------------------------------
// VLTK Mobile — PC dailyreward.txt parser
// Source: settings/event/dailyreward.txt (30 days).
// Columns: DayIdx ItemId ItemCount GoldBonus ExpBonus RequiredVipLevel
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcDailyRewardParser
    {
        public const int DayIdxCol = 0;
        public const int ItemIdCol = 1;
        public const int ItemCountCol = 2;
        public const int GoldBonusCol = 3;
        public const int ExpBonusCol = 4;
        public const int RequiredVipLevelCol = 5;

        public static List<PcDailyRewardEntry> ParseFile(string path)
        {
            var rows = new List<PcDailyRewardEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int day = PcItemCommon.Int(cols, DayIdxCol);
                if (day <= 0) continue;
                rows.Add(new PcDailyRewardEntry
                {
                    dayIdx = day,
                    itemId = PcItemCommon.Int(cols, ItemIdCol),
                    itemCount = PcItemCommon.Int(cols, ItemCountCol),
                    goldBonus = PcItemCommon.Int(cols, GoldBonusCol),
                    expBonus = PcItemCommon.Int(cols, ExpBonusCol),
                    requiredVipLevel = PcItemCommon.Int(cols, RequiredVipLevelCol),
                });
            }
            return rows;
        }

        public static PcDailyRewardRegistry BuildRegistry(string dir)
        {
            var reg = new PcDailyRewardRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("dailyreward"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcDailyRewardEntry
    {
        public int dayIdx;
        public int itemId;
        public int itemCount;
        public int goldBonus;
        public int expBonus;
        public int requiredVipLevel;
    }

    public sealed class PcDailyRewardRegistry
    {
        private readonly Dictionary<int, PcDailyRewardEntry> _byDay = new();
        public int Count => _byDay.Count;
        public void Register(PcDailyRewardEntry e) { if (e == null || e.dayIdx <= 0) return; _byDay[e.dayIdx] = e; }
        public PcDailyRewardEntry Get(int dayIdx) => _byDay.TryGetValue(dayIdx, out var v) ? v : null;
        public IReadOnlyList<PcDailyRewardEntry> GetForVip(int vipLevel)
        {
            var list = new List<PcDailyRewardEntry>();
            foreach (var e in _byDay.Values)
                if (e.requiredVipLevel <= vipLevel) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcDailyRewardEntry> All => new List<PcDailyRewardEntry>(_byDay.Values);
    }
}
