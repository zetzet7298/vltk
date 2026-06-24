// -----------------------------------------------------------------------------
// VLTK Mobile — JX Toolbar state + button config
//
// Nguồn: KgameWorldVN.cpp toolbar. Mỗi nút menu có 3 sprite (normal/selected/
// disabled) và 1 callback mở panel. Behavior: toggle (mở nếu đang đóng, đóng nếu
// đang mở). State thuần, EditMode-testable; adapter tách riêng.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.UI.JxCocos
{
    /// <summary>
    /// Static config: maps each toolbar menu button to its panel, the cocos
    /// sprite paths (normal/selected/disabled), and VN label. Mirrors the
    /// CCMenuItemSprite::create(...) calls in KgameWorldVN.cpp.
    /// </summary>
    public readonly struct JxToolbarButtonConfig
    {
        public readonly JxHudPanel Panel;
        public readonly string Label;
        public readonly string NormalSprite;
        public readonly string SelectedSprite;
        public readonly string DisabledSprite;

        public JxToolbarButtonConfig(JxHudPanel panel, string label,
            string normal, string selected, string disabled)
        {
            Panel = panel; Label = label;
            NormalSprite = normal; SelectedSprite = selected; DisabledSprite = disabled;
        }
    }

    /// <summary>Toolbar definitions in original menu order (index 0..8).</summary>
    public static class JxToolbarConfig
    {
        /// <summary>The 9 main menu buttons, in cocos order.</summary>
        public static readonly JxToolbarButtonConfig[] Menu =
        {
            new(JxHudPanel.Character, "Nhân Vật",    "toolbar/nhanvat",   "toolbar/nhanvat2",   "toolbar/nhanvat2"),
            new(JxHudPanel.Inventory, "Hành Trang",  "toolbar/hanhtrang", "toolbar/hanhtrang2", "toolbar/hanhtrang2"),
            new(JxHudPanel.Skill,     "Võ Công",     "toolbar/vocong",    "toolbar/vocong2",    "toolbar/vocong2"),
            new(JxHudPanel.Quest,     "Bảo Danh",    "toolbar/baodanh",   "toolbar/baodanh2",   "toolbar/baodanh2"),
            new(JxHudPanel.Friend,    "Bằng Hữu",    "toolbar/banghuu",   "toolbar/banghuu2",   "toolbar/banghuu2"),
            new(JxHudPanel.Team,      "Tổ Đội",      "toolbar/todoi",     "toolbar/todoi2",     "toolbar/todoi2"),
            new(JxHudPanel.Guild,     "Bang Hội",    "toolbar/banghoi",   "toolbar/banghoi2",   "toolbar/banghoi2"),
            new(JxHudPanel.Settings,  "Cài Đặt",     "toolbar/caidat",    "toolbar/caidat2",    "toolbar/caidat2"),
            // Kỳ Trân Các uses 3 distinct sprites (kytrancac1/2/3) in source.
            new(JxHudPanel.Shop,      "Kỳ Trân Các", "toolbar/kytrancac1","toolbar/kytrancac2","toolbar/kytrancac3"),
        };

        public static int Count => Menu.Length;

        public static JxToolbarButtonConfig Get(JxHudPanel panel)
        {
            for (int i = 0; i < Menu.Length; i++)
                if (Menu[i].Panel == panel) return Menu[i];
            return default;
        }
    }

    /// <summary>
    /// Pure toolbar open/close state. Tracks the currently-open panel so the
    /// adapter can render the selected button highlight. Toggle semantics:
    /// pressing the open panel's button closes it; pressing a different one
    /// switches (single panel open at a time, matching PC modal behavior).
    /// </summary>
    public sealed class JxToolbarState
    {
        /// <summary>Currently open panel (None = all closed).</summary>
        public JxHudPanel OpenPanel { get; private set; } = JxHudPanel.None;

        /// <summary>
        /// Toggle a panel: if it's already open → close; otherwise → open it
        /// (closing any other). Returns the resulting open panel.
        /// </summary>
        public JxHudPanel Toggle(JxHudPanel panel)
        {
            if (panel == JxHudPanel.None) return OpenPanel;
            OpenPanel = (OpenPanel == panel) ? JxHudPanel.None : panel;
            return OpenPanel;
        }

        /// <summary>Force a panel state (controller wiring / panel close btn).</summary>
        public void SetOpen(JxHudPanel panel) => OpenPanel = panel;

        /// <summary>Is the given panel's button the active/selected one?</summary>
        public bool IsSelected(JxHudPanel panel) => OpenPanel == panel;
    }
}
