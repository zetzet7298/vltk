// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info window content (IPopupContent)
// 3 tabs: Thuộc tính (stats) / Trang bị (paperdoll, default) / Đánh giá (placeholder).
// Binds: PlayerEquipmentService (paperdoll) + PlayerStateResponse (stats).
// Action buttons Khóa/Đính/Tháo non-destructive (log only) — ADR-6.
// REQ-4..9. Body built in C# for EditMode testability.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Backend.Dto;
using VLTK.Core;
using VLTK.Sandbox;
using VLTK.UI.Popup;

namespace VLTK.UI.CharacterInfo
{
    /// <summary>Stat row definition (label VI + PlayerStateResponse field selector).</summary>
    internal readonly struct StatRow
    {
        public readonly string key;
        public readonly string labelVi;
        public readonly Func<PlayerStateResponse, int> selector;
        public StatRow(string key, string labelVi, Func<PlayerStateResponse, int> selector)
        { this.key = key; this.labelVi = labelVi; this.selector = selector; }
    }

    /// <summary>
    /// Character Info popup content: title "Thông Tin Nhân Vật", 3 tabs, paperdoll,
    /// action buttons. Read-only data bind; no mutation.
    /// </summary>
    public sealed class CharacterInfoContent : IPopupContent
    {
        public string TitleVi => "Thông Tin Nhân Vật";

        private readonly PlayerEquipmentService _equipment;
        private readonly Func<PlayerStateResponse> _statsProvider;

        // Tab + body refs (assigned in Build).
        private VisualElement _tabTrangBi, _tabThuocTinh, _tabDanhGia;
        private Button _btnTrangBi, _btnThuocTinh, _btnDanhGia;
        private VisualElement _paperdoll;
        private VisualElement _statsList;
        private readonly Dictionary<string, Label> _statValues = new();

        /// <summary>VI stat rows bound to PlayerStateResponse.</summary>
        private static readonly IReadOnlyList<StatRow> StatRows = new[]
        {
            new StatRow("level",      "Cấp Độ",     s => s.level),
            new StatRow("exp",        "Kinh Nghiệm", s => s.exp),
            new StatRow("transLife",  "Trùng Sinh", s => s.transLife),
            new StatRow("freePoint",  "Điểm Tự Do", s => s.freePoint),
            new StatRow("magicPoint", "Linh Khí",   s => s.magicPoint),
            new StatRow("strength",   "Sức Mạnh",   s => s.strength),
            new StatRow("dexterity",  "Thân Pháp",  s => s.dexterity),
            new StatRow("vitality",   "Thể Lực",    s => s.vitality),
            new StatRow("spirit",     "Nội Lực",    s => s.spirit),
            new StatRow("series",     "Ngũ Hành",   s => s.series),
            new StatRow("money",      "Bạc",        s => s.money),
            new StatRow("repute",     "Danh Vọng",  s => s.repute),
        };

        public CharacterInfoContent(PlayerEquipmentService equipment, Func<PlayerStateResponse> statsProvider)
        {
            _equipment = equipment;
            _statsProvider = statsProvider;
        }

        public void Build(VisualElement body)
        {
            body.Clear();
            body.AddToClassList("char-info-body");

            // --- Tab bar ---
            var tabBar = new VisualElement { name = "CharInfoTabBar" };
            tabBar.AddToClassList("char-tab-bar");

            _btnThuocTinh = MakeTab("Thuộc tính", "tab_thuoctinh", tabBar);
            _btnTrangBi = MakeTab("Trang bị", "tab_trangbi", tabBar);
            _btnDanhGia = MakeTab("Đánh giá", "tab_danhgia", tabBar);

            _btnThuocTinh.clicked += () => SwitchTab("thuoctinh");
            _btnTrangBi.clicked += () => SwitchTab("trangbi");
            _btnDanhGia.clicked += () => SwitchTab("danhgia");

            body.Add(tabBar);

            // --- Tab bodies ---
            _tabThuocTinh = BuildStatsTab();
            _tabTrangBi = BuildEquipmentTab();
            _tabDanhGia = BuildPlaceholderTab("Đánh giá hệ thống trang bị — sắp ra mắt.");
            body.Add(_tabThuocTinh);
            body.Add(_tabTrangBi);
            body.Add(_tabDanhGia);

            // --- Action buttons (non-destructive) ---
            var actions = new VisualElement { name = "CharInfoActions" };
            actions.AddToClassList("char-actions");
            actions.Add(MakeActionButton("Khóa", "btn_lock", "Khóa trang bị"));
            actions.Add(MakeActionButton("Đính", "btn_embed", "Đính ngọc"));
            actions.Add(MakeActionButton("Tháo", "btn_unequip", "Tháo trang bị"));
            body.Add(actions);

            // Default tab = Trang bị (REQ-4).
            SwitchTab("trangbi");
        }

        public void OnShow()
        {
            // Refresh paperdoll (real equipment state).
            if (_paperdoll != null)
                CharacterInfoPaperdoll.Build(_paperdoll, _equipment);

            // Refresh stats.
            RefreshStats();
        }

        public void OnClose()
        {
            _statValues.Clear();
        }

        // ---- tabs ----
        private Button MakeTab(string textVi, string name, VisualElement parent)
        {
            var btn = new Button { name = name, text = textVi };
            btn.AddToClassList("char-tab");
            parent.Add(btn);
            return btn;
        }

        private VisualElement BuildEquipmentTab()
        {
            var tab = new VisualElement { name = "TabBody_trangbi" };
            tab.AddToClassList("char-tab-body");
            _paperdoll = new VisualElement { name = "Paperdoll" };
            _paperdoll.AddToClassList("char-paperdoll-wrap");
            tab.Add(_paperdoll);
            CharacterInfoPaperdoll.Build(_paperdoll, _equipment);
            return tab;
        }

        private VisualElement BuildStatsTab()
        {
            var tab = new VisualElement { name = "TabBody_thuoctinh" };
            tab.AddToClassList("char-tab-body");
            _statsList = new VisualElement { name = "StatsList" };
            _statsList.AddToClassList("char-stats-list");
            foreach (var row in StatRows)
            {
                var line = new VisualElement { name = "Stat_" + row.key };
                line.AddToClassList("char-stat-row");

                var lbl = new Label(row.labelVi) { name = "Stat_" + row.key + "_Label" };
                lbl.AddToClassList("char-stat-label");
                var val = new Label("--") { name = "Stat_" + row.key + "_Value" };
                val.AddToClassList("char-stat-value");

                line.Add(lbl);
                line.Add(val);
                _statsList.Add(line);
                _statValues[row.key] = val;
            }
            tab.Add(_statsList);
            return tab;
        }

        private VisualElement BuildPlaceholderTab(string message)
        {
            var tab = new VisualElement { name = "TabBody_danhgia" };
            tab.AddToClassList("char-tab-body");
            var ph = new Label(message) { name = "Placeholder" };
            ph.AddToClassList("char-placeholder");
            tab.Add(ph);
            return tab;
        }

        private void SwitchTab(string key)
        {
            _tabThuocTinh.style.display = key == "thuoctinh" ? DisplayStyle.Flex : DisplayStyle.None;
            _tabTrangBi.style.display   = key == "trangbi"   ? DisplayStyle.Flex : DisplayStyle.None;
            _tabDanhGia.style.display   = key == "danhgia"   ? DisplayStyle.Flex : DisplayStyle.None;

            ToggleTabActive(_btnThuocTinh, key == "thuoctinh");
            ToggleTabActive(_btnTrangBi,   key == "trangbi");
            ToggleTabActive(_btnDanhGia,   key == "danhgia");
        }

        private static void ToggleTabActive(Button btn, bool active)
        {
            if (active) btn.AddToClassList("active");
            else btn.RemoveFromClassList("active");
        }

        private void RefreshStats()
        {
            var stats = _statsProvider?.Invoke();
            foreach (var row in StatRows)
            {
                if (!_statValues.TryGetValue(row.key, out var val)) continue;
                val.text = stats != null
                    ? row.selector(stats).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : "--";
            }
        }

        private static Button MakeActionButton(string textVi, string name, string logAction)
        {
            var btn = new Button { name = name, text = textVi };
            btn.AddToClassList("char-action-btn");
            btn.clicked += () => SubsystemLog.Info("Popup.CharacterInfo", $"{logAction} (slice 1: non-destructive)");
            return btn;
        }
    }
}
