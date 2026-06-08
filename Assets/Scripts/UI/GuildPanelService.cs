// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Bang Hội (Guild Panel)
// Reference: PC tong/guild system + GuildService.
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

        private static readonly int[] PcRankOrder = { RankLeader, RankViceLeader, RankSteward, RankMember };

        public static IReadOnlyList<int> GetPcRankOrder()
            => PcRankOrder;

        public static GuildPanelSnapshot BuildSnapshot(GuildService svc, int playerId, int selectedMemberId = 0)
        {
            if (svc == null)
                return new GuildPanelSnapshot { guildName = string.Empty, rows = System.Array.Empty<GuildPanelRow>() };

            return new GuildPanelSnapshot
            {
                guildId = svc.GuildName.Length > 0 ? 1 : 0,
                guildName = svc.GuildName,
                level = svc.GuildLevel,
                fund = svc.GuildFunds,
                memberCount = svc.GuildName.Length > 0 ? 1 : 0,
                maxMember = 50,
                leaderId = svc.GuildName.Length > 0 ? playerId : 0,
                rows = svc.GuildName.Length > 0
                    ? new[] { new GuildPanelRow(playerId, "Bang chủ", RankLeader, svc.GuildFunds, true, 0, 0) }
                    : System.Array.Empty<GuildPanelRow>()
            };
        }

        public static bool TryDonate(GuildService svc, int playerId, int amount, int currency)
        {
            if (svc == null || playerId <= 0 || amount <= 0)
                return false;
            svc.Donate(amount);
            return true;
        }

        public static bool TryKick(GuildService svc, int playerId, int targetId)
            => false;

        public static string RankName(int rank)
        {
            return rank switch
            {
                RankLeader => "Bang chủ",
                RankViceLeader => "Phó bang",
                RankSteward => "Đường chủ",
                RankMember => "Thành viên",
                _ => "Chưa rõ",
            };
        }

        public static string GetGuildSummary(GuildService svc)
        {
            if (svc == null)
                return "Chưa gia nhập bang phái.";
            var name = string.IsNullOrWhiteSpace(svc.GuildName) ? "Chưa đặt tên" : svc.GuildName;
            return $"{name} — cấp {svc.GuildLevel}, quỹ {svc.GuildFunds}, công trình {svc.GuildBuild}";
        }
    }
}
