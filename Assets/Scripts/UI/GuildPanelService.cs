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
            return new int[] { RankMember, RankSteward, RankViceLeader, RankLeader };
        }

        public static GuildPanelSnapshot BuildSnapshot(GuildService svc, int playerId, int selectedMemberId = 0)
        {
            var snap = new GuildPanelSnapshot
            {
                guildId = 0,
                guildName = string.Empty,
                level = 1,
                fund = 0,
                memberCount = 0,
                maxMember = 80,
                leaderId = 0,
                rows = System.Array.Empty<GuildPanelRow>(),
            };
            if (svc == null) return snap;

            snap.guildId = svc.GuildId;
            snap.guildName = svc.GuildName;
            snap.level = svc.GuildLevel;
            snap.fund = svc.GuildFunds;
            snap.memberCount = svc.MemberCount;
            snap.maxMember = svc.MaxMemberCount;
            snap.leaderId = svc.LeaderId;
            // Build a virtual list of members from snapshot storage
            var list = new List<GuildPanelRow>();
            foreach (var m in svc.GetMembers())
            {
                bool online = m.online;
                int nowSec = (int)(System.DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                int lastLogin = (int)(nowSec - m.lastLoginTime);
                int joined = (int)(nowSec - m.joinedTime);
                var row = new GuildPanelRow(m.playerId, m.playerName ?? "Ẩn danh", m.rank, m.contribution, online, lastLogin, joined);
                list.Add(row);
                if (m.playerId == selectedMemberId) snap.selectedRow = row;
            }
            snap.rows = list;
            return snap;
        }

        public static bool TryDonate(GuildService svc, int playerId, int amount, int currency)
        {
            if (svc == null || amount <= 0) return false;
            return svc.Donate(playerId, amount, currency) > 0;
        }

        public static bool TryKick(GuildService svc, int playerId, int targetId)
        {
            if (svc == null || targetId <= 0) return false;
            return svc.KickMember(playerId, targetId);
        }

        public static string RankName(int rank) => rank switch
        {
            RankLeader => "Trưởng bang",
            RankViceLeader => "Phó bang",
            RankSteward => "Đường chủ",
            _ => "Thành viên",
        };

        public static string GetGuildSummary(GuildService svc)
        {
            if (svc == null) return "Chưa có bang";
            return $"{svc.GuildName} (Cấp {svc.GuildLevel}) - {svc.MemberCount}/{svc.MaxMemberCount} thành viên";
        }
    }
}
