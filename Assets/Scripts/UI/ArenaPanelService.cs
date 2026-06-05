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
            return new ArenaPanelSnapshot { rows = System.Array.Empty<ArenaPanelRow>() };
        }

        public static IReadOnlyList<ArenaPanelRow> GetByType(ArenaService service, int type)
        {
            return System.Array.Empty<ArenaPanelRow>();
        }

        public static bool TryChallenge(ArenaService service, int playerId, int arenaId)
        {
            return false;
        }

        public static int GetMyRank(ArenaService service, int playerId)
        {
            return 0;
        }

        public static string GetOpponentName(ArenaService service, int playerId)
        {
            return string.Empty;
        }

    }
}
