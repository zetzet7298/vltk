// -----------------------------------------------------------------------------
// VLTK Mobile — PC battle reward config parser (Battle Rewards)
// Source: battlereward.txt (Reference/PcBattlefield).
// Columns: RewardId  BattleType  WinItemId  WinItemCount  WinGold
//          LossItemId  LossItemCount  LossGold  RequiredRank
// Vietnamese: "Phần Thưởng", "Thắng", "Thua", "Hạng Yêu Cầu".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBattleRewardConfigParser
    {
        public const int RewardIdCol = 0;
        public const int BattleTypeCol = 1;
        public const int WinItemIdCol = 2;
        public const int WinItemCountCol = 3;
        public const int WinGoldCol = 4;
        public const int LossItemIdCol = 5;
        public const int LossItemCountCol = 6;
        public const int LossGoldCol = 7;
        public const int RequiredRankCol = 8;

        public static List<PcBattleRewardConfigEntry> ParseFile(string path)
        {
            var rows = new List<PcBattleRewardConfigEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 1) continue;
                int id = PcItemCommon.Int(cols, RewardIdCol);
                if (id <= 0) continue;
                rows.Add(new PcBattleRewardConfigEntry
                {
                    rewardId = id,
                    battleType = PcItemCommon.Int(cols, BattleTypeCol),
                    winItemId = PcItemCommon.Int(cols, WinItemIdCol),
                    winItemCount = PcItemCommon.Int(cols, WinItemCountCol),
                    winGold = PcItemCommon.Int(cols, WinGoldCol),
                    lossItemId = PcItemCommon.Int(cols, LossItemIdCol),
                    lossItemCount = PcItemCommon.Int(cols, LossItemCountCol),
                    lossGold = PcItemCommon.Int(cols, LossGoldCol),
                    requiredRank = PcItemCommon.Int(cols, RequiredRankCol),
                });
            }
            return rows;
        }

        public static PcBattleRewardConfigRegistry BuildRegistry(string dir)
        {
            var reg = new PcBattleRewardConfigRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcBattleRewardConfigEntry
    {
        public int rewardId;
        public int battleType;
        public int winItemId;
        public int winItemCount;
        public int winGold;
        public int lossItemId;
        public int lossItemCount;
        public int lossGold;
        public int requiredRank;
    }

    public sealed class PcBattleRewardConfigRegistry
    {
        private readonly Dictionary<int, PcBattleRewardConfigEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcBattleRewardConfigEntry e)
        {
            if (e == null || e.rewardId <= 0) return;
            _byId[e.rewardId] = e;
        }

        public PcBattleRewardConfigEntry Get(int rewardId)
            => _byId.TryGetValue(rewardId, out var v) ? v : null;

        public IReadOnlyList<PcBattleRewardConfigEntry> GetByBattleType(int battleType)
        {
            var list = new List<PcBattleRewardConfigEntry>();
            foreach (var e in _byId.Values)
                if (e.battleType == battleType) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBattleRewardConfigEntry> GetForRank(int rank)
        {
            var list = new List<PcBattleRewardConfigEntry>();
            foreach (var e in _byId.Values)
                if (e.requiredRank == rank) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBattleRewardConfigEntry> All => new List<PcBattleRewardConfigEntry>(_byId.Values);
    }
}
