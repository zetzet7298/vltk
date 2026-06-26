// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Treasure / Kỳ Trân Các popup content
// PC sources captured by TreasureMallPanelService:
//   9e5f75d1 = Kỳ Trân Các, 1463f852 = Giỏ hàng, b54fbe43 = Rương báu.
// This slice is read-only UI parity: expose PC-derived controls/runtime summaries;
// buying, cart mutation, and chest betting remain follow-up gameplay work.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.Treasure
{
    /// <summary>Popup body for BtnTreasure: Vietnamese Bảo Vật/Kỳ Trân Các overview.</summary>
    public sealed class TreasureContent : IPopupContent, IPopupLayoutHint
    {
        public string TitleVi => "Bảo Vật";
        public float Width => 520f;
        public float Height => 520f;
        public float Left => 380f;
        public float Top => 70f;

        private readonly MallService _mall;
        private readonly TreasureHuntService _treasureHunt;
        private readonly int _playerId;
        private readonly int _vipLevel;
        private readonly int _currentMapId;
        private readonly float _posX;
        private readonly float _posY;

        private VisualElement _summaryList;
        private VisualElement _controlList;
        private Label _footer;

        public TreasureContent(
            MallService mall,
            TreasureHuntService treasureHunt,
            int playerId = 1,
            int vipLevel = 0,
            int currentMapId = 0,
            float posX = 0f,
            float posY = 0f)
        {
            _mall = mall;
            _treasureHunt = treasureHunt;
            _playerId = playerId;
            _vipLevel = vipLevel;
            _currentMapId = currentMapId;
            _posX = posX;
            _posY = posY;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("treasure-body");

            var tabs = new VisualElement { name = "TreasureTabs" };
            tabs.AddToClassList("treasure-tabs");
            tabs.Add(MakeTab("Kỳ Trân Các", active: true));
            tabs.Add(MakeTab("Giỏ Hàng", active: false));
            tabs.Add(MakeTab("Rương Báu", active: false));
            body.Add(tabs);

            var summaryPanel = new VisualElement { name = "TreasureSummary" };
            summaryPanel.AddToClassList("treasure-panel");
            summaryPanel.Add(new Label("Tổng quan PC") { name = "TreasureSummaryTitle" });
            summaryPanel.Q<Label>("TreasureSummaryTitle").AddToClassList("treasure-section-title");
            _summaryList = new VisualElement { name = "TreasureSummaryList" };
            _summaryList.AddToClassList("treasure-summary-list");
            summaryPanel.Add(_summaryList);
            body.Add(summaryPanel);

            var controlsPanel = new VisualElement { name = "TreasureControls" };
            controlsPanel.AddToClassList("treasure-panel");
            controlsPanel.Add(new Label("Nút/chức năng từ PC") { name = "TreasureControlsTitle" });
            controlsPanel.Q<Label>("TreasureControlsTitle").AddToClassList("treasure-section-title");

            var scroll = new ScrollView { name = "TreasureControlScroll" };
            scroll.AddToClassList("treasure-control-scroll");
            _controlList = new VisualElement { name = "TreasureControlList" };
            _controlList.AddToClassList("treasure-control-list");
            scroll.Add(_controlList);
            controlsPanel.Add(scroll);
            body.Add(controlsPanel);

            _footer = new Label("Read-only: mua/bỏ giỏ/quay rương sẽ làm ở slice gameplay.") { name = "TreasureFooter" };
            _footer.AddToClassList("treasure-footer");
            body.Add(_footer);

            Refresh();
        }

        public void OnShow() => Refresh();

        public void OnClose()
        {
            _summaryList = null;
            _controlList = null;
            _footer = null;
        }

        private static Label MakeTab(string text, bool active)
        {
            var tab = new Label(text);
            tab.AddToClassList("treasure-tab");
            if (active) tab.AddToClassList("active");
            return tab;
        }

        private void Refresh()
        {
            if (_summaryList == null || _controlList == null) return;

            _summaryList.Clear();
            var mall = MallPanelService.BuildSnapshot(_mall, _playerId, _vipLevel);
            var treasure = TreasureHuntPanelService.BuildSnapshot(_treasureHunt, _playerId, _currentMapId, _posX, _posY);
            foreach (var row in TreasureMallPanelService.BuildRows(mall, treasure, page: 0, quantity: 1, cartCount: 0, cartOpen: false, chestBet: 0))
                _summaryList.Add(MakeSummaryRow(row));

            _controlList.Clear();
            foreach (var control in TreasureMallPanelService.PcControls)
                _controlList.Add(MakeControlRow(control));
        }

        private static Label MakeSummaryRow(string text)
        {
            var label = new Label(text);
            label.AddToClassList("treasure-summary-row");
            return label;
        }

        private static VisualElement MakeControlRow(TreasureMallPanelService.PcTreasureMallControl control)
        {
            var row = new VisualElement();
            row.AddToClassList("treasure-control-row");

            var label = new Label(control.labelVi) { name = "ControlLabel" };
            label.AddToClassList("treasure-control-label");
            row.Add(label);

            var action = new Label(control.actionVi) { name = "ControlAction" };
            action.AddToClassList("treasure-control-action");
            row.Add(action);

            var source = new Label(control.pcFile + " / " + control.pcSection) { name = "ControlSource" };
            source.AddToClassList("treasure-control-source");
            row.Add(source);

            return row;
        }
    }
}
