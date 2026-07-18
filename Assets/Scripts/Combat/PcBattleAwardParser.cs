// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/battleaward.txt Battle Award parser
// Source: battleaward.txt — phần thưởng xếp hạng chiến đấu (Tống Kim, Quốc Chiến, Boss, Võ Đài).
//   AwardId  BattleType  Rank  RewardSilver  RewardExp  RewardItem
// BattleType: 0=Tống Kim, 1=Quốc Chiến, 2=Boss, 3=Võ Đài
// Rank: 1=quán quân, 2-10=top 10
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBattleAwardParser
    {
        public const int AwardIdCol = 0;
        public const int BattleTypeCol = 1;
        public const int RankCol = 2;
        public const int RewardSilverCol = 3;
        public const int RewardExpCol = 4;
        public const int RewardItemCol = 5;

        public static List<PcBattleAwardEntry> ParseFile(string path)
        {
            var rows = new List<PcBattleAwardEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcBattleAwardEntry
                {
                    awardId = PcItemCommon.Int(cols, AwardIdCol),
                    battleType = PcItemCommon.Int(cols, BattleTypeCol),
                    rank = PcItemCommon.Int(cols, RankCol),
                    rewardSilver = PcItemCommon.Int(cols, RewardSilverCol),
                    rewardExp = PcItemCommon.Int(cols, RewardExpCol),
                    rewardItem = PcItemCommon.Int(cols, RewardItemCol),
                });
            }
            return rows;
        }

        public static PcBattleAwardRegistry BuildRegistry(string dir)
        {
            var reg = new PcBattleAwardRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "battleaward.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcBattleAwardEntry
    {
        public int awardId;
        public int battleType;     // 0=Tống Kim, 1=Quốc Chiến, 2=Boss, 3=Võ Đài
        public int rank;           // 1-10
        public int rewardSilver;
        public int rewardExp;
        public int rewardItem;     // 0 = không có vật phẩm
    }

    public sealed class PcBattleAwardRegistry
    {
        private readonly Dictionary<int, PcBattleAwardEntry> _byId = new();
        private readonly Dictionary<int, List<PcBattleAwardEntry>> _byType = new();
        private readonly Dictionary<int, List<PcBattleAwardEntry>> _byRank = new();
        public int Count => _byId.Count;
        public IEnumerable<PcBattleAwardEntry> All => _byId.Values;
        public void Register(PcBattleAwardEntry e)
        {
            if (e == null || e.awardId <= 0) return;
            _byId[e.awardId] = e;
            if (!_byType.TryGetValue(e.battleType, out var tlist))
            {
                tlist = new List<PcBattleAwardEntry>();
                _byType[e.battleType] = tlist;
            }
            tlist.Add(e);
            if (!_byRank.TryGetValue(e.rank, out var rlist))
            {
                rlist = new List<PcBattleAwardEntry>();
                _byRank[e.rank] = rlist;
            }
            rlist.Add(e);
        }
        public PcBattleAwardEntry Get(int awardId)
            => _byId.TryGetValue(awardId, out var v) ? v : null;
        public IReadOnlyList<PcBattleAwardEntry> GetByBattleType(int type)
            => _byType.TryGetValue(type, out var v)
                ? (IReadOnlyList<PcBattleAwardEntry>)v
                : (IReadOnlyList<PcBattleAwardEntry>)System.Array.Empty<PcBattleAwardEntry>();
        public IReadOnlyList<PcBattleAwardEntry> GetByRank(int rank)
            => _byRank.TryGetValue(rank, out var v)
                ? (IReadOnlyList<PcBattleAwardEntry>)v
                : (IReadOnlyList<PcBattleAwardEntry>)System.Array.Empty<PcBattleAwardEntry>();
    }
}
