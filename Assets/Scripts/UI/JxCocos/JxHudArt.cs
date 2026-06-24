// -----------------------------------------------------------------------------
// VLTK Mobile — JX HUD sprite path map (jx-cocos ui_vn assets)
//
// Nguồn sprite truth: /home/zet/Projects/jx-cocos/client/Resources/ui_vn/**
// (127 PNG gốc — copy vào Assets/UI/HudJxCocos/**, tái sử dụng, KHÔNG tự vẽ).
//
// Lớp này map mỗi element HUD → asset path dưới Assets/UI/HudJxCocos, khớp 1:1
// với sprite được load trong source *VN.cpp (Sprite::create("ui_vn/...")).
// -----------------------------------------------------------------------------

namespace VLTK.UI.JxCocos
{
    /// <summary>
    /// Static asset-path map for the jx-cocos HUD sprites. Paths resolve under
    /// Assets/UI/HudJxCocos/** (Resources-relative names mirror the cocos
    /// ui_vn/ layout). Renderer adapters reference these via JxHudArt.
    /// </summary>
    public static class JxHudArt
    {
        // Top status bar — KuiTopControlVN.cpp
        public const string TopStatusBg = "KuiTopControl/rolestate";       // 211x71 nền
        public const string HpFill = "KuiTopControl/blood";                // 139x14
        public const string ManaFill = "KuiTopControl/mana";               // 139x15
        public const string StaminaFill = "KuiTopControl/stamina";         // 139x15
        public const string ExpFill = "KuiTopControl/kinhnghiem";          // 139x15
        public const string AvatarMale = "KuiTopControl/AvatarNam";        // 70x70
        public const string AvatarFemale = "KuiTopControl/AvatarNu";       // 70x70

        // Toolbar menu buttons — KgameWorldVN.cpp (9 buttons, normal + pressed)
        // Pressed/active state = suffix "2" in cocos ui_vn/toolbar.
        public static class Toolbar
        {
            public const string Dir = "toolbar";
            public const string CharNormal = "toolbar/nhanvat";   public const string CharPressed = "toolbar/nhanvat2";     // 0 Nhân Vật
            public const string InvNormal = "toolbar/hanhtrang";  public const string InvPressed = "toolbar/hanhtrang2";    // 1 Hành Trang
            public const string SkillNormal = "toolbar/vocong";   public const string SkillPressed = "toolbar/vocong2";     // 2 Võ Công
            public const string QuestNormal = "toolbar/baodanh";  public const string QuestPressed = "toolbar/baodanh2";    // 3 Bảo Danh
            public const string FriendNormal = "toolbar/banghuu"; public const string FriendPressed = "toolbar/banghuu2";   // 4 Bằng Hữu
            public const string TeamNormal = "toolbar/todoi";     public const string TeamPressed = "toolbar/todoi2";       // 5 Tổ Đội
            public const string GuildNormal = "toolbar/banghoi";  public const string GuildPressed = "toolbar/banghoi2";    // 6 Bang Hội
            public const string SettingsNormal = "toolbar/caidat";public const string SettingsPressed = "toolbar/caidat2";  // 7 Cài Đặt
            public const string ShopNormal = "toolbar/kytrancac"; public const string ShopPressed = "toolbar/kytrancac2";   // 8 Kỳ Trân Các
        }
    }
}
