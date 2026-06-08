// -----------------------------------------------------------------------------
// VLTK Mobile — PC Friend Panel Service (BtnFriend / 2b9c5056.ini)
// PC controls: Group toggle, Friend/Brother/Enemy/Other tabs, Find, Close,
// ScrollUp/Down, Invisible toggle, plus runtime friend list rows.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct FriendPanelControlRow
    {
        public readonly string pcSection;
        public readonly string labelVi;
        public readonly string actionVi;

        public FriendPanelControlRow(string pcSection, string labelVi, string actionVi)
        {
            this.pcSection = pcSection ?? string.Empty;
            this.labelVi = labelVi ?? string.Empty;
            this.actionVi = actionVi ?? string.Empty;
        }
    }

    public sealed class FriendPanelSnapshot
    {
        public int friendCount;
        public int maxFriends;
        public IReadOnlyList<FriendPanelControlRow> controls;
        public IReadOnlyList<string> friendRows;
    }

    /// <summary>
    /// Snapshot for the PC QQ/friend HUD window opened by toolbar [Friend].
    /// Source: 1024 uid 2b9c5056.ini.
    /// </summary>
    public static class FriendPanelService
    {
        public static readonly IReadOnlyList<FriendPanelControlRow> PcControls = new List<FriendPanelControlRow>
        {
            new FriendPanelControlRow("GroupBtn", "Nhóm", "mở/thu nhóm danh sách bằng hữu"),
            new FriendPanelControlRow("UnitBtnFriend", "Bạn hữu", "lọc danh sách bạn"),
            new FriendPanelControlRow("UnitBtnBrother", "Huynh đệ", "lọc huynh đệ"),
            new FriendPanelControlRow("UnitBtnEnemy", "Cừu nhân", "lọc cừu nhân"),
            new FriendPanelControlRow("UnitBtnOther", "Khác", "lọc nhóm khác"),
            new FriendPanelControlRow("FindBtn", "Thêm bạn hữu", "mở tìm/thêm bạn"),
            new FriendPanelControlRow("Invisible", "Đồng hành", "bật/tắt chức năng đồng hành"),
            new FriendPanelControlRow("ScrollUp", "Cuộn lên", "cuộn danh sách lên"),
            new FriendPanelControlRow("ScrollDown", "Cuộn xuống", "cuộn danh sách xuống"),
            new FriendPanelControlRow("CloseBtn", "Đóng", "đóng cửa sổ bằng hữu"),
        };

        public static FriendPanelSnapshot BuildSnapshot(FriendService service, int playerId)
        {
            var friends = service != null ? service.GetFriends(playerId) : System.Array.Empty<FriendEntry>();
            var rows = new List<string>();
            foreach (var f in friends)
                rows.Add($"{f.friendName} — cấp {f.level} — {(f.isOnline ? "online" : "offline")} — thân mật {f.intimacy}");
            if (rows.Count == 0)
                rows.Add("Danh sách bằng hữu đang trống.");

            return new FriendPanelSnapshot
            {
                friendCount = friends.Count,
                maxFriends = FriendService.MaxFriends,
                controls = PcControls,
                friendRows = rows,
            };
        }
    }
}
