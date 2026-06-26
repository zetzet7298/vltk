// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Team / Tổ đội popup content
// PC source captured by TeamPanelService: a05d7a2c.dat (组队 window).
// This slice is read-only UI parity: expose PC-derived controls + live party
// roster (PartyService). Invite/kick/appoint/leave/dismiss gameplay remains
// follow-up work.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.Team
{
    /// <summary>Popup body for BtnTeam: Vietnamese Đội / tổ đội overview.</summary>
    public sealed class TeamContent : IPopupContent, IPopupLayoutHint
    {
        public string TitleVi => "Đội";
        public float Width => 480f;
        public float Height => 520f;
        public float Left => 400f;
        public float Top => 70f;

        private readonly PartyService _party;
        private readonly bool _nearbyListClosed;

        private VisualElement _rosterList;
        private VisualElement _controlList;
        private Label _footer;

        public TeamContent(PartyService party, bool nearbyListClosed = false)
        {
            _party = party;
            _nearbyListClosed = nearbyListClosed;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("team-body");

            var rosterPanel = new VisualElement { name = "TeamRoster" };
            rosterPanel.AddToClassList("team-panel");
            rosterPanel.Add(new Label("Thành viên đội") { name = "TeamRosterTitle" });
            rosterPanel.Q<Label>("TeamRosterTitle").AddToClassList("team-section-title");

            var rosterScroll = new ScrollView { name = "TeamRosterScroll" };
            rosterScroll.AddToClassList("team-roster-scroll");
            _rosterList = new VisualElement { name = "TeamRosterList" };
            _rosterList.AddToClassList("team-roster-list");
            rosterScroll.Add(_rosterList);
            rosterPanel.Add(rosterScroll);
            body.Add(rosterPanel);

            var controlsPanel = new VisualElement { name = "TeamControls" };
            controlsPanel.AddToClassList("team-panel");
            controlsPanel.Add(new Label("Nút/chức năng từ PC") { name = "TeamControlsTitle" });
            controlsPanel.Q<Label>("TeamControlsTitle").AddToClassList("team-section-title");

            var controlScroll = new ScrollView { name = "TeamControlScroll" };
            controlScroll.AddToClassList("team-control-scroll");
            _controlList = new VisualElement { name = "TeamControlList" };
            _controlList.AddToClassList("team-control-list");
            controlScroll.Add(_controlList);
            controlsPanel.Add(controlScroll);
            body.Add(controlsPanel);

            _footer = new Label("Read-only: mời/trục xuất/rời đội sẽ làm ở slice gameplay.") { name = "TeamFooter" };
            _footer.AddToClassList("team-footer");
            body.Add(_footer);

            Refresh();
        }

        public void OnShow() => Refresh();

        public void OnClose()
        {
            _rosterList = null;
            _controlList = null;
            _footer = null;
        }

        private void Refresh()
        {
            if (_rosterList == null || _controlList == null) return;

            _rosterList.Clear();
            foreach (var row in TeamPanelService.BuildRows(_party, _nearbyListClosed))
                _rosterList.Add(MakeRosterRow(row));

            _controlList.Clear();
            foreach (var control in TeamPanelService.PcControls)
                _controlList.Add(MakeControlRow(control));
        }

        private static Label MakeRosterRow(string text)
        {
            var label = new Label(text);
            label.AddToClassList("team-roster-row");
            return label;
        }

        private static VisualElement MakeControlRow(TeamPanelService.PcTeamControl control)
        {
            var row = new VisualElement();
            row.AddToClassList("team-control-row");

            var label = new Label(control.labelVi) { name = "ControlLabel" };
            label.AddToClassList("team-control-label");
            row.Add(label);

            var action = new Label(control.actionVi) { name = "ControlAction" };
            action.AddToClassList("team-control-action");
            row.Add(action);

            var source = new Label("a05d7a2c / " + control.pcSection) { name = "ControlSource" };
            source.AddToClassList("team-control-source");
            row.Add(source);

            return row;
        }
    }
}
