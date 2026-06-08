using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>PC 1024 team window manifest from a05d7a2c.dat.</summary>
    public static class TeamPanelService
    {
        public readonly struct PcTeamControl
        {
            public readonly string pcSection;
            public readonly string labelVi;
            public readonly string actionVi;
            public readonly string spr;

            public PcTeamControl(string pcSection, string labelVi, string actionVi, string spr)
            {
                this.pcSection = pcSection;
                this.labelVi = labelVi;
                this.actionVi = actionVi;
                this.spr = spr;
            }
        }

        public static readonly IReadOnlyList<PcTeamControl> PcControls = new[]
        {
            new PcTeamControl("Invite", "Mời vào đội", "mời người chơi đang chọn ở danh sách xung quanh", @"\Spr\Ui3\组队\邀请加入.spr"),
            new PcTeamControl("Kick", "Trục xuất", "đưa thành viên đang chọn ra khỏi đội", @"\Spr\Ui3\组队\踢出队伍.spr"),
            new PcTeamControl("Appoint", "Giao đội trưởng", "chuyển quyền đội trưởng cho thành viên đang chọn", @"\Spr\Ui3\组队\队长移交.spr"),
            new PcTeamControl("Refresh", "Làm mới", "cập nhật danh sách đội/người chơi xung quanh", @"\Spr\Ui3\组队\刷新列表.spr"),
            new PcTeamControl("Leave", "Rời đội", "rời khỏi đội hiện tại", @"\Spr\Ui3\组队\离开队伍.spr"),
            new PcTeamControl("Dismiss", "Giải tán đội", "giải tán đội nếu đang là đội trưởng", @"\Spr\Ui3\组队\解散队伍.spr"),
            new PcTeamControl("CloseTeam", "Đóng/mở tìm đội", "bật/tắt danh sách lân cận theo PC checkbox", @"\Spr\Ui3\组队\组队开关.spr"),
            new PcTeamControl("Cancel", "Đóng", "đóng giao diện tổ đội", @"\spr\Ui3\组队\关闭.spr"),
        };

        public static readonly IReadOnlyDictionary<string, string> DisabledPcTeamControls =
            new Dictionary<string, string>
            {
                ["NearbyScroll_Btn"] = "a05d7a2c [NearbyScroll_Btn] is the drag thumb for the nearby list scrollbar, not a standalone command button; mobile uses the ScrollView momentum/drag behavior.",
            };

        public static IReadOnlyList<string> BuildRows(PartyService party, bool nearbyListClosed)
        {
            var rows = new List<string>
            {
                "PC a05d7a2c [Main] tổ đội: Invite/Kick/Appoint/Refresh/Leave/Dismiss/CloseTeam/Cancel.",
                party == null ? "PartyService chưa sẵn sàng." : $"Đội hiện tại: {party.MemberCount}/6 — trưởng đội id={party.LeaderId} — tìm đội {(nearbyListClosed ? "đóng" : "mở")}",
            };

            if (party != null && party.Members != null)
            {
                foreach (var m in party.Members)
                    rows.Add($"{(m.isLeader ? "★" : "•")} {m.nameVi} Lv{m.level} [{PartyService.FactionNameVi(m.factionId)}] {(m.isOnline ? "online" : "offline")}");
            }

            return rows;
        }
    }
}
