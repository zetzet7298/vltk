// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/worldrank/toplist.txt Bảng Xếp Hạng (WORLD RANK) parser
// Source: server settings/worldrank/toplist.txt (GB2312, tab-separated).
//   RankType (0=Level, 1=Money, 2=PK, 3=Guild, 4=Fame)  Count  MinScore  MaxScore  RewardId
// Vietnamese: "Bảng Xếp Hạng", "Top Cấp Độ", "Top Tài Phú", "Top PK", "Top Bang".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcWorldRankParser
    {
        public const int RankTypeCol = 0;
        public const int CountCol = 1;
        public const int MinScoreCol = 2;
        public const int MaxScoreCol = 3;
        public const int RewardIdCol = 4;

        public static List<PcWorldRankEntry> ParseFile(string path)
        {
            var rows = new List<PcWorldRankEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int type = PcItemCommon.Int(cols, RankTypeCol);
                if (type < 0) continue;
                rows.Add(new PcWorldRankEntry
                {
                    rankType = type,
                    count = PcItemCommon.Int(cols, CountCol),
                    minScore = cols.Length > MinScoreCol ? PcItemCommon.Int(cols, MinScoreCol) : 0,
                    maxScore = cols.Length > MaxScoreCol ? PcItemCommon.Int(cols, MaxScoreCol) : 0,
                    rewardId = cols.Length > RewardIdCol ? PcItemCommon.Int(cols, RewardIdCol) : 0,
                });
            }
            return rows;
        }

        public static PcWorldRankRegistry BuildRegistry(string dir)
        {
            var reg = new PcWorldRankRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "toplist.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcWorldRankEntry
    {
        public int rankType;     // 0=Level, 1=Money, 2=PK, 3=Guild, 4=Fame
        public int count;        // Số người trong bảng xếp hạng
        public int minScore;
        public int maxScore;
        public int rewardId;
    }

    public sealed class PcWorldRankRegistry
    {
        private readonly Dictionary<int, PcWorldRankEntry> _byType = new();
        public int Count => _byType.Count;
        public void Register(PcWorldRankEntry e)
        {
            if (e == null || e.rankType < 0) return;
            _byType[e.rankType] = e;
        }
        public PcWorldRankEntry Get(int rankType)
            => _byType.TryGetValue(rankType, out var v) ? v : null;
        public IEnumerable<PcWorldRankEntry> GetAll() => _byType.Values;
        public IReadOnlyList<PcWorldRankEntry> GetByType(int type)
        {
            if (_byType.TryGetValue(type, out var v))
                return new PcWorldRankEntry[] { v };
            return System.Array.Empty<PcWorldRankEntry>();
        }
    }
}
