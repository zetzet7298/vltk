// -----------------------------------------------------------------------------
// VLTK Mobile — PC Team / Đội popup content
// Source: PC a05d7a2c.dat (组队), 340×229.
// Art: exact SPR frames vendored under Assets/UI/Popup/Team/Art.
// -----------------------------------------------------------------------------
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.Team
{
    /// <summary>Popup body for BtnTeam: PC team sheet with live party roster.</summary>
    public sealed class TeamContent : IPopupContent, IPopupLayoutHint, IPopupChromeHint
    {
        public const float PcWidth = 340f;
        public const float PcHeight = 229f;

        public string TitleVi => "Đội";
        public float Width => PcWidth;
        public float Height => PcHeight;
        public float Left => (1280f - PcWidth) * 0.5f;
        public float Top => (720f - PcHeight) * 0.5f;
        public PopupChromeKind Chrome => PopupChromeKind.PcTeam;

        private readonly PartyService _party;
        private readonly bool _nearbyListClosed;

        private VisualElement _memberList;
        private VisualElement _nearbyList;
        private Label _leaderAbility;
        private Label _inputEdit;

        public TeamContent(PartyService party, bool nearbyListClosed = false)
        {
            _party = party;
            _nearbyListClosed = nearbyListClosed;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("team-body");

            var panel = new VisualElement { name = "TeamPanel" };
            panel.AddToClassList("team-panel-pc");
            body.Add(panel);

            _memberList = new VisualElement { name = "TeamMemberList" };
            _memberList.AddToClassList("team-member-list");
            panel.Add(_memberList);

            _nearbyList = new VisualElement { name = "TeamNearbyList" };
            _nearbyList.AddToClassList("team-nearby-list");
            panel.Add(_nearbyList);

            var thumb = new VisualElement { name = "NearbyScrollThumb" };
            thumb.AddToClassList("team-nearby-scroll-thumb");
            panel.Add(thumb);

            _leaderAbility = new Label(string.Empty) { name = "LeaderAbility" };
            _leaderAbility.AddToClassList("team-leader-ability");
            panel.Add(_leaderAbility);

            _inputEdit = new Label(string.Empty) { name = "InputEdit" };
            _inputEdit.AddToClassList("team-input-edit");
            panel.Add(_inputEdit);

            panel.Add(DisabledButton("Invite", "team-command-btn team-btn-invite"));
            panel.Add(DisabledButton("Kick", "team-command-btn team-btn-kick"));
            panel.Add(DisabledButton("Appoint", "team-command-btn team-btn-appoint"));
            panel.Add(DisabledButton("Refresh", "team-command-btn team-btn-refresh"));
            panel.Add(DisabledButton(ShowDismissButton() ? "Dismiss" : "Leave", ShowDismissButton()
                ? "team-command-btn team-btn-dismiss"
                : "team-command-btn team-btn-leave"));
            panel.Add(DisabledButton("CloseTeam", "team-close-team-btn"));

            var close = new Button { name = "Close", text = string.Empty };
            close.AddToClassList("team-cancel-btn");
            panel.Add(close);

            Refresh();
        }

        public void OnShow() => Refresh();

        public void OnClose()
        {
            _memberList = null;
            _nearbyList = null;
            _leaderAbility = null;
            _inputEdit = null;
        }

        private void Refresh()
        {
            if (_memberList == null || _nearbyList == null) return;

            _memberList.Clear();
            if (_party == null || _party.MemberCount == 0)
            {
                _memberList.Add(MakeRow("Chưa lập đội"));
                if (_leaderAbility != null) _leaderAbility.text = "";
            }
            else
            {
                foreach (var member in _party.Members)
                    _memberList.Add(MakeRow(FormatMember(member)));
                if (_leaderAbility != null)
                    _leaderAbility.text = string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "{0}/6", _party.MemberCount);
            }

            _nearbyList.Clear();
            _nearbyList.Add(MakeRow(_nearbyListClosed ? "Tìm đội đã đóng" : "Chưa có DS lân cận"));

            if (_inputEdit != null)
                _inputEdit.text = "";
        }

        private bool ShowDismissButton()
        {
            if (_party == null || _party.MemberCount == 0 || _party.Members == null)
                return false;
            return _party.Members.Count > 0 && _party.Members[0] != null && _party.Members[0].isLeader;
        }

        private static Label MakeRow(string text)
        {
            var label = new Label(text);
            label.AddToClassList("team-list-row");
            return label;
        }

        private static string FormatMember(PartyMember member)
        {
            if (member == null) return string.Empty;
            return string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "{0}{1} Lv{2}",
                member.isLeader ? "★" : "•",
                string.IsNullOrEmpty(member.nameVi) ? "?" : member.nameVi,
                member.level);
        }

        private static Button DisabledButton(string name, string classes)
        {
            var button = new Button { name = name, text = string.Empty };
            foreach (var cls in classes.Split(' '))
                button.AddToClassList(cls);
            button.SetEnabled(false);
            return button;
        }
    }
}
