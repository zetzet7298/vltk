// -----------------------------------------------------------------------------
// VLTK Mobile — PC signin.txt parser
// Source: settings/event/signin.txt (Điểm Danh 30 ngày).
// Columns: SignInDay RewardItemId RewardCount RewardGold IsDouble TotalDaysSoFar
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSignInParser
    {
        public const int SignInDayCol = 0;
        public const int RewardItemIdCol = 1;
        public const int RewardCountCol = 2;
        public const int RewardGoldCol = 3;
        public const int IsDoubleCol = 4;
        public const int TotalDaysSoFarCol = 5;

        public static List<PcSignInEntry> ParseFile(string path)
        {
            var rows = new List<PcSignInEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int day = PcItemCommon.Int(cols, SignInDayCol);
                if (day <= 0) continue;
                rows.Add(new PcSignInEntry
                {
                    signInDay = day,
                    rewardItemId = PcItemCommon.Int(cols, RewardItemIdCol),
                    rewardCount = PcItemCommon.Int(cols, RewardCountCol),
                    rewardGold = PcItemCommon.Int(cols, RewardGoldCol),
                    isDouble = PcItemCommon.Int(cols, IsDoubleCol) != 0,
                    totalDaysSoFar = PcItemCommon.Int(cols, TotalDaysSoFarCol),
                });
            }
            return rows;
        }

        public static PcSignInRegistry BuildRegistry(string dir)
        {
            var reg = new PcSignInRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("signin"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcSignInEntry
    {
        public int signInDay;
        public int rewardItemId;
        public int rewardCount;
        public int rewardGold;
        public bool isDouble;
        public int totalDaysSoFar;
    }

    public sealed class PcSignInRegistry
    {
        private readonly Dictionary<int, PcSignInEntry> _byDay = new();
        public int Count => _byDay.Count;
        public void Register(PcSignInEntry e) { if (e == null || e.signInDay <= 0) return; _byDay[e.signInDay] = e; }
        public PcSignInEntry Get(int day) => _byDay.TryGetValue(day, out var v) ? v : null;
        public IReadOnlyList<PcSignInEntry> GetByTotalDays(int totalDays)
        {
            var list = new List<PcSignInEntry>();
            foreach (var e in _byDay.Values)
                if (e.totalDaysSoFar == totalDays) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcSignInEntry> All => new List<PcSignInEntry>(_byDay.Values);
    }
}
