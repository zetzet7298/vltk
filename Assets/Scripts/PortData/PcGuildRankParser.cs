// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/tong/tong_rank.txt Bang (GUILD) rank parser
// Source: server settings/tong/tong_rank.txt (5 ranks, GB2312, tab-separated).
//   Rank  RankName  Authority  MaxCount  WeeklySalary
// Authority: 0=Thành Viên, 1=Trưởng Lão, 2=Bang Chủ.
// Vietnamese: "Cấp Bậc Bang", "Bang Chủ", "Trưởng Lão", "Thành Viên".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGuildRankParser
    {
        public const int RankCol = 0;
        public const int RankNameCol = 1;
        public const int AuthorityCol = 2;
        public const int MaxCountCol = 3;
        public const int WeeklySalaryCol = 4;

        public static List<PcGuildRankEntry> ParseFile(string path)
        {
            var rows = new List<PcGuildRankEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int rank = PcItemCommon.Int(cols, RankCol);
                if (rank <= 0) continue;
                rows.Add(new PcGuildRankEntry
                {
                    rank = rank,
                    rankName = PcItemCommon.Str(cols, RankNameCol),
                    authority = PcItemCommon.Int(cols, AuthorityCol),
                    maxCount = cols.Length > MaxCountCol ? PcItemCommon.Int(cols, MaxCountCol) : 0,
                    weeklySalary = cols.Length > WeeklySalaryCol ? PcItemCommon.Int(cols, WeeklySalaryCol) : 0,
                });
            }
            return rows;
        }

        public static PcGuildRankRegistry BuildRegistry(string dir)
        {
            var reg = new PcGuildRankRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "tong_rank.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcGuildRankEntry
    {
        public int rank;
        public string rankName;
        public int authority;     // 0=Thành Viên, 1=Trưởng Lão, 2=Bang Chủ
        public int maxCount;
        public int weeklySalary;
    }

    public sealed class PcGuildRankRegistry
    {
        private readonly Dictionary<int, PcGuildRankEntry> _byRank = new();
        private readonly Dictionary<int, List<PcGuildRankEntry>> _byAuthority = new();
        public int Count => _byRank.Count;
        public void Register(PcGuildRankEntry e)
        {
            if (e == null || e.rank <= 0) return;
            _byRank[e.rank] = e;
            if (!_byAuthority.TryGetValue(e.authority, out var list))
            {
                list = new List<PcGuildRankEntry>();
                _byAuthority[e.authority] = list;
            }
            list.Add(e);
        }
        public PcGuildRankEntry Get(int rank) => _byRank.TryGetValue(rank, out var v) ? v : null;
        public IEnumerable<PcGuildRankEntry> GetAll() => _byRank.Values;
        public IReadOnlyList<PcGuildRankEntry> GetByAuthority(int auth)
            => _byAuthority.TryGetValue(auth, out var v)
                ? (IReadOnlyList<PcGuildRankEntry>)v
                : System.Array.Empty<PcGuildRankEntry>();
    }
}
