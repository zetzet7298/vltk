// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/ranking.txt parser
// Source: ranking.txt (Xếp hạng player theo type).
// Cols: RankId, PlayerId, PlayerName, FactionId, Level, Score, RankType
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcRankingParser
    {
        public const int RankIdCol = 0;
        public const int PlayerIdCol = 1;
        public const int PlayerNameCol = 2;
        public const int FactionIdCol = 3;
        public const int LevelCol = 4;
        public const int ScoreCol = 5;
        public const int RankTypeCol = 6;

        public static List<PcRankingEntry> ParseFile(string path)
        {
            var rows = new List<PcRankingEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, RankIdCol);
                if (id < 0) continue;
                rows.Add(new PcRankingEntry
                {
                    rankId = id,
                    playerId = PcItemCommon.Int(cols, PlayerIdCol),
                    playerName = PcItemCommon.Str(cols, PlayerNameCol),
                    factionId = PcItemCommon.Int(cols, FactionIdCol),
                    level = PcItemCommon.Int(cols, LevelCol),
                    score = PcItemCommon.Int(cols, ScoreCol),
                    rankType = PcItemCommon.Int(cols, RankTypeCol),
                });
            }
            return rows;
        }

        public static PcRankingRegistry BuildRegistry(string dir)
        {
            var reg = new PcRankingRegistry();
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
    public class PcRankingEntry
    {
        public int rankId;
        public int playerId;
        public string playerName;
        public int factionId;
        public int level;
        public int score;
        public int rankType;
    }

    public sealed class PcRankingRegistry
    {
        private readonly Dictionary<int, PcRankingEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcRankingEntry e) { if (e == null) return; _byId[e.rankId] = e; }
        public PcRankingEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcRankingEntry> GetByType(int type)
        {
            var list = new List<PcRankingEntry>();
            foreach (var e in _byId.Values)
                if (e.rankType == type) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcRankingEntry> GetTop(int n)
        {
            var list = new List<PcRankingEntry>(_byId.Values);
            list.Sort((a, b) => b.score.CompareTo(a.score));
            if (n > 0 && list.Count > n) list.RemoveRange(n, list.Count - n);
            return list;
        }
        public IReadOnlyList<PcRankingEntry> All => new List<PcRankingEntry>(_byId.Values);
    }
}
