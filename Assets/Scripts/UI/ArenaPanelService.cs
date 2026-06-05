// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: Arena (Đấu Trường)
// Bảng UI đấu trường PvP, thách đấu, hạng, thắng thua.
// Vietnamese: "Đấu Trường", "Thách đấu", "Hạng", "Thắng", "Thua".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct ArenaPanelRow
    {
        public readonly int arenaId;
        public readonly string name;
        public readonly int type;
        public readonly int requiredLevel;
        public readonly int minRank;
        public readonly string rewardPreview;
        public readonly int myRank;
        public readonly string opponentName;
        public readonly bool isMyTurn;

        public ArenaPanelRow(int arenaId, string name, int type, int requiredLevel, int minRank, string rewardPreview, int myRank, string opponentName, bool isMyTurn)
        {
            this.arenaId = arenaId;
            this.name = name ?? string.Empty;
            this.type = type;
            this.requiredLevel = requiredLevel;
            this.minRank = minRank;
            this.rewardPreview = rewardPreview ?? string.Empty;
            this.myRank = myRank;
            this.opponentName = opponentName ?? string.Empty;
            this.isMyTurn = isMyTurn;
        }
    }

    public sealed class ArenaPanelSnapshot
    {
        public int playerId;
        public int currentRank;
        public int bestRank;
        public int totalWins;
        public int totalLosses;
        public int todayWins;
        public int todayLosses;
        public IReadOnlyList<ArenaPanelRow> rows;
    }

    public static class ArenaPanelService
    {
        public const string LabelArena = "Đấu Trường";
        public const string LabelChallenge = "Thách đấu";
        public const string LabelRank = "Hạng";
        public const string LabelWin = "Thắng";
        public const string LabelLose = "Thua";

        public static ArenaPanelSnapshot BuildSnapshot(ArenaService service, int playerId)
        {
            var snapshot = new ArenaPanelSnapshot
            {
                playerId = playerId,
                currentRank = 0,
                bestRank = 0,
                totalWins = 0,
                totalLosses = 0,
                todayWins = 0,
                todayLosses = 0,
                rows = Array.Empty<ArenaPanelRow>()
            };
            if (service == null) return snapshot;
            var all = service.GetAll();
            var rows = new List<ArenaPanelRow>();
            foreach (var arena in all)
            {
                if (arena == null) continue;
                int rank = service.GetMyRank(playerId, arena.arenaId);
                rows.Add(new ArenaPanelRow(
                    arena.arenaId, arena.nameRaw, arena.type, arena.requiredLevel,
                    arena.minRank, arena.rewardPreview, rank,
                    service.GetOpponentName(playerId, arena.arenaId),
                    service.IsMyTurn(playerId, arena.arenaId)));
            }
            snapshot.rows = rows;
            snapshot.currentRank = service.GetCurrentRank(playerId);
            snapshot.bestRank = service.GetBestRank(playerId);
            snapshot.totalWins = service.GetTotalWins(playerId);
            snapshot.totalLosses = service.GetTotalLosses(playerId);
            snapshot.todayWins = service.GetTodayWins(playerId);
            snapshot.todayLosses = service.GetTodayLosses(playerId);
            return snapshot;
        }

        public static IReadOnlyList<ArenaPanelRow> GetByType(ArenaService service, int type)
        {
            if (service == null) return Array.Empty<ArenaPanelRow>();
            var rows = new List<ArenaPanelRow>();
            foreach (var arena in service.GetAll())
            {
                if (arena == null) continue;
                if (arena.type == type)
                {
                    rows.Add(new ArenaPanelRow(
                        arena.arenaId, arena.nameRaw, arena.type, arena.requiredLevel,
                        arena.minRank, arena.rewardPreview, 0, string.Empty, false));
                }
            }
            return rows;
        }

        public static bool TryChallenge(ArenaService service, int playerId, int arenaId)
        {
            if (service == null || playerId <= 0 || arenaId <= 0) return false;
            return service.TryChallenge(playerId, arenaId);
        }

        public static int GetMyRank(ArenaService service, int playerId)
        {
            if (service == null || playerId <= 0) return 0;
            return service.GetCurrentRank(playerId);
        }

        public static string GetOpponentName(ArenaService service, int playerId)
        {
            if (service == null || playerId <= 0) return string.Empty;
            return service.GetCurrentOpponentName(playerId);
        }
    }
}
