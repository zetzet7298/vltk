// -----------------------------------------------------------------------------
// VLTK Mobile — Ranking Panel Service (Xếp Hạng)
// Dựng snapshot cho UI xếp hạng cá nhân + bang hội.
// Vietnamese: "Xếp Hạng", "Hạng", "Điểm", "Cấp", "Môn Phái".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct RankingPanelRow
    {
        public readonly int rank;
        public readonly int playerId;
        public readonly string playerName;
        public readonly int level;
        public readonly int score;
        public readonly int faction;
        public readonly string factionName;
        public readonly bool isMyRank;

        public RankingPanelRow(int rank, int playerId, string playerName, int level, int score, int faction, string factionName, bool isMyRank)
        {
            this.rank = rank;
            this.playerId = playerId;
            this.playerName = playerName;
            this.level = level;
            this.score = score;
            this.faction = faction;
            this.factionName = factionName;
            this.isMyRank = isMyRank;
        }
    }

    public sealed class RankingPanelSnapshot
    {
        public int rankingType;
        public int myRank;
        public int topPlayer;
        public int topScore;
        public int totalPlayers;
        public IReadOnlyList<RankingPanelRow> rows;
    }

    public static class RankingPanelService
    {
        public const string LabelRanking = "Xếp Hạng";
        public const string LabelRank = "Hạng";
        public const string LabelScore = "Điểm";
        public const string LabelLevel = "Cấp";
        public const string LabelFaction = "Môn Phái";
        public const string LabelPersonal = "Cá Nhân";
        public const string LabelGuild = "Bang Hội";

        public static RankingPanelSnapshot BuildSnapshot(RankingService rank, int playerId, int type)
        {
            var snap = new RankingPanelSnapshot
            {
                rankingType = type,
                rows = Array.Empty<RankingPanelRow>(),
                myRank = 0,
                topPlayer = 0,
                topScore = 0,
                totalPlayers = rank?.Count ?? 0,
            };
            if (rank == null) return snap;
            var rows = new List<RankingPanelRow>();
            var all = EnumerateAll(rank);
            int r = 1;
            int top = 0;
            int topScore = 0;
            int myRank = 0;
            foreach (var entry in all)
            {
                string fname = entry.faction >= 0 ? FactionVietnameseCatalog.GetVietnameseName(entry.faction) : null;
                bool mine = entry.playerId == playerId;
                if (mine) myRank = r;
                if (r == 1) { top = entry.playerId; topScore = entry.score; }
                rows.Add(new RankingPanelRow(r, entry.playerId, entry.playerName, entry.level, entry.score, entry.faction, fname, mine));
                r++;
            }
            snap.myRank = myRank;
            snap.topPlayer = top;
            snap.topScore = topScore;
            snap.totalPlayers = rows.Count;
            snap.rows = rows;
            return snap;
        }

        public static int GetMyRank(RankingService rank, int playerId, int type)
        {
            if (rank == null || playerId <= 0) return 0;
            return rank.GetPlayerRank(playerId, (RankingType)type);
        }

        public static IReadOnlyList<RankingPanelRow> GetTopN(RankingService rank, int type, int n)
        {
            if (rank == null || n <= 0) return Array.Empty<RankingPanelRow>();
            var snap = BuildSnapshot(rank, 0, type);
            if (snap.rows.Count <= n) return snap.rows;
            var list = new List<RankingPanelRow>(snap.rows);
            list.RemoveRange(n, list.Count - n);
            return list;
        }

        public static string GetRankingTypeName(int type)
        {
            switch (type)
            {
                case 0: return "Cấp";
                case 1: return "Tài Phú";
                case 2: return "Giết Người";
                case 3: return "Môn Phái";
                case 4: return "Bang Hội";
                default: return "Khác";
            }
        }

        private static IEnumerable<RankingEntry> EnumerateAll(RankingService rank)
        {
            var field = typeof(RankingService).GetField("_entries", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(rank) is List<RankingEntry> list) return list;
            return Array.Empty<RankingEntry>();
        }
    }

    public class RankingEntry
    {
        public int playerId;
        public string playerName;
        public int level;
        public int score;
        public int faction;
    }
}
