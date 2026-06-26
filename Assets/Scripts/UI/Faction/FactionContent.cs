// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Faction Bonus / Môn Phái popup content
// PC source: faction_bonus.txt (Reference/PcFaction) read by FactionBonusService.
// This slice is read-only UI parity + fixes BtnFaction wiring (was wrongly
// toggling the StallCurrencySelector). Faction bonus grant/level-up gameplay
// remains follow-up work.
//
// NOTE on faction id schemes: CombatFaction enum ints and PartyService.
// FactionNameVi ints use DIFFERENT orderings. The caller resolves the
// Vietnamese name from the authoritative CombatFaction mapping and passes both
// the (int) faction id (for bonus-table queries) and the resolved name. When
// the PC faction_bonus.txt data lands, reconcile the factionId scheme (follow-up).
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.Faction
{
    /// <summary>Popup body for BtnFaction: Vietnamese Bonus Môn Phái overview.</summary>
    public sealed class FactionContent : IPopupContent, IPopupLayoutHint
    {
        public string TitleVi => "Môn Phái";
        public float Width => 460f;
        public float Height => 480f;
        public float Left => 410f;
        public float Top => 80f;

        private readonly FactionBonusService _service;
        private readonly int _factionId;
        private readonly string _factionNameVi;
        private readonly int _playerLevel;

        private Label _header;
        private VisualElement _totals;
        private VisualElement _rowList;
        private Label _footer;

        public FactionContent(FactionBonusService service, int factionId, string factionNameVi, int playerLevel)
        {
            _service = service;
            _factionId = factionId;
            _factionNameVi = string.IsNullOrEmpty(factionNameVi) ? PartyService.FactionNameVi(factionId) : factionNameVi;
            _playerLevel = playerLevel;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("faction-body");

            _header = new Label { name = "FactionHeader" };
            _header.AddToClassList("faction-header");
            body.Add(_header);

            _totals = new VisualElement { name = "FactionTotals" };
            _totals.AddToClassList("faction-totals");
            body.Add(_totals);

            var tablePanel = new VisualElement { name = "FactionTable" };
            tablePanel.AddToClassList("faction-panel");
            tablePanel.Add(new Label("Bảng thưởng theo cấp") { name = "FactionTableTitle" });
            tablePanel.Q<Label>("FactionTableTitle").AddToClassList("faction-section-title");
            tablePanel.Add(MakeColumnHeader());

            var scroll = new ScrollView { name = "FactionRowScroll" };
            scroll.AddToClassList("faction-row-scroll");
            _rowList = new VisualElement { name = "FactionRowList" };
            _rowList.AddToClassList("faction-row-list");
            scroll.Add(_rowList);
            tablePanel.Add(scroll);
            body.Add(tablePanel);

            _footer = new Label("Read-only: thưởng cấp/gia nhập môn phái sẽ làm ở slice gameplay.") { name = "FactionFooter" };
            _footer.AddToClassList("faction-footer");
            body.Add(_footer);

            Refresh();
        }

        public void OnShow() => Refresh();

        public void OnClose()
        {
            _header = null;
            _totals = null;
            _rowList = null;
            _footer = null;
        }

        private void Refresh()
        {
            if (_header == null || _rowList == null || _totals == null) return;

            _header.text = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0} — Bonus Môn Phái (cấp {1})", _factionNameVi, _playerLevel);

            _totals.Clear();
            int hp = Safe.Compute(_service, s => s.ComputeHpBonus(_factionId, _playerLevel));
            int mp = Safe.Compute(_service, s => s.ComputeMpBonus(_factionId, _playerLevel));
            int atk = Safe.Compute(_service, s => s.ComputeAtkBonus(_factionId, _playerLevel));
            int def = Safe.Compute(_service, s => s.ComputeDefBonus(_factionId, _playerLevel));
            _totals.Add(MakeTotal(FactionBonusPanelService.LabelHp, hp));
            _totals.Add(MakeTotal(FactionBonusPanelService.LabelMp, mp));
            _totals.Add(MakeTotal(FactionBonusPanelService.LabelAtk, atk));
            _totals.Add(MakeTotal(FactionBonusPanelService.LabelDef, def));

            _rowList.Clear();
            var rows = Safe.GetByFaction(_service, _factionId);
            if (rows == null || rows.Count == 0)
            {
                var empty = new Label("(chưa có dữ liệu thưởng — Reference/PcFaction/faction_bonus.txt)") { name = "FactionEmpty" };
                empty.AddToClassList("faction-empty");
                _rowList.Add(empty);
                return;
            }

            foreach (var e in rows)
                _rowList.Add(MakeRow(e.level, e.hpBonus, e.mpBonus, e.atkBonus, e.defBonus, e.level <= _playerLevel));
        }

        private static VisualElement MakeColumnHeader()
        {
            var row = new VisualElement();
            row.AddToClassList("faction-row");
            row.AddToClassList("faction-row-head");
            row.Add(MakeCell("Cấp", "faction-cell-level"));
            row.Add(MakeCell(FactionBonusPanelService.LabelHp, "faction-cell"));
            row.Add(MakeCell(FactionBonusPanelService.LabelMp, "faction-cell"));
            row.Add(MakeCell(FactionBonusPanelService.LabelAtk, "faction-cell"));
            row.Add(MakeCell(FactionBonusPanelService.LabelDef, "faction-cell"));
            return row;
        }

        private static VisualElement MakeRow(int level, int hp, int mp, int atk, int def, bool active)
        {
            var row = new VisualElement();
            row.AddToClassList("faction-row");
            if (active) row.AddToClassList("active");
            row.Add(MakeCell(level.ToString(System.Globalization.CultureInfo.InvariantCulture), "faction-cell-level"));
            row.Add(MakeCell(hp.ToString(System.Globalization.CultureInfo.InvariantCulture), "faction-cell"));
            row.Add(MakeCell(mp.ToString(System.Globalization.CultureInfo.InvariantCulture), "faction-cell"));
            row.Add(MakeCell(atk.ToString(System.Globalization.CultureInfo.InvariantCulture), "faction-cell"));
            row.Add(MakeCell(def.ToString(System.Globalization.CultureInfo.InvariantCulture), "faction-cell"));
            return row;
        }

        private static VisualElement MakeTotal(string label, int value)
        {
            var cell = new VisualElement();
            cell.AddToClassList("faction-total");
            var l = new Label(label);
            l.AddToClassList("faction-total-label");
            var v = new Label(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            v.AddToClassList("faction-total-value");
            cell.Add(l);
            cell.Add(v);
            return cell;
        }

        private static Label MakeCell(string text, string cls)
        {
            var cell = new Label(text);
            cell.AddToClassList(cls);
            return cell;
        }

        private static class Safe
        {
            public static int Compute(FactionBonusService service, System.Func<FactionBonusService, int> selector)
                => service != null ? selector(service) : 0;

            public static IReadOnlyList<PcFactionBonusEntry> GetByFaction(FactionBonusService service, int factionId)
                => service != null ? service.GetByFaction(factionId) : System.Array.Empty<PcFactionBonusEntry>();
        }
    }
}
