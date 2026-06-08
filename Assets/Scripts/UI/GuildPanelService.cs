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


        public readonly struct PcGuildControl
        {
            public readonly string pcFile;
            public readonly string pcSection;
            public readonly string labelVi;
            public readonly string actionVi;
            public readonly string spr;

            public PcGuildControl(string pcFile, string pcSection, string labelVi, string actionVi, string spr)
            {
                this.pcFile = pcFile;
                this.pcSection = pcSection;
                this.labelVi = labelVi;
                this.actionVi = actionVi;
                this.spr = spr;
            }
        }

        public static readonly IReadOnlyList<PcGuildControl> PcControls = new[]
        {
            new PcGuildControl("223e63d0", "BtnUpgradeBuildLevel", "Nâng công trình", "dùng quỹ/công trình để nâng cấp bang", @"\spr\Ui3\帮会界面\通用按钮.spr"),
            new PcGuildControl("223e63d0", "BtnRecruit", "Chiêu mộ", "mở trang tuyển người của bang", @"\Spr\Ui3\帮会界面\帮会信息页\帮会信息-招人按钮.spr"),
            new PcGuildControl("223e63d0", "BtnKickOut", "Trục xuất", "trục xuất thành viên đang chọn", @"\spr\Ui3\帮会界面\通用按钮.spr"),
            new PcGuildControl("223e63d0", "BtnDepose", "Bổ nhiệm/bãi nhiệm", "điều chỉnh chức vụ thành viên", @"\spr\Ui3\帮会界面\通用按钮.spr"),
            new PcGuildControl("223e63d0", "BtnStoreTongMoney", "Gửi quỹ bang", "đóng góp bạc vào quỹ bang", @"\spr\Ui3\帮会界面\通用按钮1字版.spr"),
            new PcGuildControl("223e63d0", "BtnStoreBuildFund", "Gửi công trình", "chuyển quỹ vào công trình bang", @"\spr\Ui3\帮会界面\通用按钮1字版.spr"),
            new PcGuildControl("223e63d0", "BtnLeaveTong", "Rời bang", "rời khỏi bang hội hiện tại", @"\spr\Ui3\帮会界面\通用按钮.spr"),
            new PcGuildControl("223e63d0", "BtnRetire", "Thoái ẩn", "thoái ẩn khi lâu không online theo PC", @"\spr\Ui3\帮会界面\通用按钮.spr"),
            new PcGuildControl("223e63d0", "BtnHelp", "Trợ giúp bang", "mở trợ giúp bang hội", @"\Spr\Ui3\帮会界面\帮会信息页\帮会信息-帮会帮助.spr"),
            new PcGuildControl("223e63d0", "BtnPrevPage", "Trang trước", "lùi trang danh sách thành viên/bang", @"\spr\Ui3\帮会界面\通用按钮5字版.spr"),
            new PcGuildControl("223e63d0", "BtnNextPage", "Trang sau", "tiến trang danh sách thành viên/bang", @"\spr\Ui3\帮会界面\通用按钮5字版.spr"),
            new PcGuildControl("223e63d0", "BtnOnlinePriority", "Ưu tiên online", "toggle sắp xếp thành viên online lên trước", @"\Spr\Ui3\帮会界面\功能勾选按钮加长版.spr"),
            new PcGuildControl("223e63d0", "BtnMemberSortMenu", "Sắp xếp thành viên", "đổi kiểu sắp xếp danh sách thành viên", @"\Spr\Ui3\帮会界面\查询列表.spr"),
            new PcGuildControl("120ebf4e", "BtnWeekDaily", "Nhật trình tuần", "chuyển sang tab nhật trình tuần", @"\spr\Ui3\帮会界面\帮会记录页\分页按钮.spr"),
            new PcGuildControl("120ebf4e", "BtnAnnounce", "Thông báo", "chuyển sang tab thông báo bang", @"\spr\Ui3\帮会界面\帮会记录页\分页按钮.spr"),
            new PcGuildControl("120ebf4e", "BtnTongAffair", "Việc bang", "chuyển sang tab sự vụ bang", @"\spr\Ui3\帮会界面\帮会记录页\分页按钮.spr"),
            new PcGuildControl("120ebf4e", "BtnTongHistory", "Lịch sử", "chuyển sang tab lịch sử bang", @"\spr\Ui3\帮会界面\帮会记录页\分页按钮.spr"),
            new PcGuildControl("120ebf4e", "BtnLeaveWord", "Lưu lời nhắn", "gửi lời nhắn bang hội", @"\spr\Ui3\帮会界面\帮会记录页\帮会记录-留言.spr"),
            new PcGuildControl("120ebf4e", "BtnEditAnnounce", "Sửa thông báo", "mở sửa thông báo bang", @"\spr\Ui3\帮会界面\帮会记录页\帮会记录-编辑.spr"),
            new PcGuildControl("f5054c2e", "Save", "Lưu tuyển người", "lưu cấu hình tuyển thành viên", @"\Spr\Ui3\帮会界面\招募页vn\保存招募信息按钮.spr"),
            new PcGuildControl("f5054c2e", "AcceptApply", "Duyệt đơn", "duyệt người xin vào bang", @"\Spr\Ui3\帮会界面\招募页vn\空白按钮.spr"),
            new PcGuildControl("f5054c2e", "RefuseApply", "Từ chối đơn", "từ chối người xin vào bang", @"\Spr\Ui3\帮会界面\招募页vn\空白按钮.spr"),
            new PcGuildControl("f5054c2e", "LastPage", "Đơn trang trước", "lùi trang đơn xin vào bang", @"\Spr\Ui3\帮会界面\招募页vn\空白按钮.spr"),
            new PcGuildControl("f5054c2e", "NextPage", "Đơn trang sau", "tiến trang đơn xin vào bang", @"\Spr\Ui3\帮会界面\招募页vn\空白按钮.spr"),
        };

        public static readonly IReadOnlyDictionary<string, string> PassivePcGuildControls =
            new Dictionary<string, string>
            {
                ["RecordList_Scroll_Btn"] = "120ebf4e scroll drag thumb; mobile ScrollView supplies drag/momentum.",
                ["EditorScroll_Btn"] = "120ebf4e editor scroll drag thumb; mobile ScrollView supplies drag/momentum.",
                ["ListScroll_Btn"] = "a5e5430e dialog scroll drag thumb; not a standalone command.",
            };

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
