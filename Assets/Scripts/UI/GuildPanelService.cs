// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Bang Hội (Guild Panel)
// Reference: PC tong/guild system + GuildService.
// Vietnamese: "Bang Hội", "Tên bang", "Cấp bang", "Quỹ bang", "Thành viên".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một hàng thành viên trong panel bang.</summary>
    public readonly struct GuildPanelRow
    {
        public readonly int memberId;
        public readonly string memberName;
        public readonly int rank; // 1=thành viên, 2=đường chủ, 3=phó bang, 4=trưởng bang
        public readonly int contribution;
        public readonly bool isOnline;
        public readonly int lastLoginSec;
        public readonly int joinedSec;

        public GuildPanelRow(int memberId, string memberName, int rank, int contribution, bool isOnline, int lastLoginSec, int joinedSec)
        {
            this.memberId = memberId;
            this.memberName = memberName;
            this.rank = rank;
            this.contribution = contribution;
            this.isOnline = isOnline;
            this.lastLoginSec = lastLoginSec;
            this.joinedSec = joinedSec;
        }
    }

    public sealed class GuildPanelSnapshot
    {
        public int guildId;
        public string guildName;
        public int level;
        public int fund;
        public int memberCount;
        public int maxMember;
        public int leaderId;
        public IReadOnlyList<GuildPanelRow> rows;
        public GuildPanelRow? selectedRow;
    }

    public static class GuildPanelService
    {
        public const int RankMember = 1;
        public const int RankLeader = 4;
        public const int RankViceLeader = 3;
        public const int RankSteward = 2;

        public static IReadOnlyList<int> GetPcRankOrder()
        {
            return System.Array.Empty<int>();
        }

        public static GuildPanelSnapshot BuildSnapshot(GuildService svc, int playerId, int selectedMemberId = 0)
        {
            return new GuildPanelSnapshot { rows = System.Array.Empty<GuildPanelRow>() };
        }

        public static bool TryDonate(GuildService svc, int playerId, int amount, int currency)
        {
            return false;
        }

        public static bool TryKick(GuildService svc, int playerId, int targetId)
        {
            return false;
        }

        public static string RankName(int rank)
        {
            return string.Empty;
        }

        public static string GetGuildSummary(GuildService svc)
        {
            return string.Empty;
        }

    }
}
