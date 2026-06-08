using System.Collections.Generic;

namespace VLTK.UI
{
    /// <summary>
    /// Source-of-truth HUD layout spec for the PC 800x600 client screenshot chosen by the user.
    /// Evidence:
    ///   Top status bar: update03/update01.pak uid 8da7027d, image uid 973816f3.
    ///   Main bottom HUD: slistcache.pak uid e3b06434, Image=\spr\UI3\主界面\快捷栏800x600_vn.spr.
    ///   Tool buttons: slistcache.pak uid dc11ac12, 工具控制条.ini in 800-coordinate space.
    /// Coordinates below are PC absolute coordinates. Mobile keeps the full 800px strip centered at x=240; do not split/right-anchor it.
    /// </summary>
    public static class HudBottomBarPcSpec
    {
        public readonly struct ButtonRect
        {
            public readonly int left;
            public readonly int top;
            public readonly int width;
            public readonly int height;
            public readonly string spr;
            public readonly string classType;
            public readonly string tipVi;

            public ButtonRect(int left, int top, int width, int height, string spr, string classType, string tipVi)
            {
                this.left = left;
                this.top = top;
                this.width = width;
                this.height = height;
                this.spr = spr;
                this.classType = classType;
                this.tipVi = tipVi;
            }
        }

        public const int PcReferenceWidth = 800;
        public const int PcReferenceHeight = 600;
        public const int MobileReferenceWidth = 1280;

        // pc-evidence/pc_hud.png crop x[0..800] y[502..608] contains the PC action row + bottom strip.
        public const int BarBandTop = 502;
        public const int BarWidth = 1280;
        public const int BarHeight = 106;
        public const int CenteredStripLeft = (MobileReferenceWidth - PcReferenceWidth) / 2;

        public const string MainSchema = "主界面玩家信息窗口.ini";
        public const string ToolBoxSchema = "工具控制条.ini";
        public const string MainSchemaUid = "e3b06434";
        public const string ToolBoxSchemaUid = "dc11ac12";

        public static int CenteredLeft(int pcLeft)
            => CenteredStripLeft + pcLeft;

        /// <summary>工具控制条 (uid dc11ac12) from slistcache.pak / 800-space.</summary>
        public static readonly IReadOnlyDictionary<string, ButtonRect> ToolControlBar =
            new Dictionary<string, ButtonRect>
            {
                ["Status"]   = new ButtonRect(460, 559, 28, 28, @"\spr\UI3\主界面\人物属性按钮_0.spr", "Player_Status",   "Nhân vật"),
                ["Items"]    = new ButtonRect(491, 559, 28, 28, @"\spr\UI3\主界面\背包按钮.spr",       "Player_Items",    "Túi đồ"),
                ["ItemEx"]   = new ButtonRect(522, 559, 28, 28, @"\spr\UI3\主界面\子母袋按钮.spr",     "Player_ItemEx",   "Túi hành trang"),
                ["Skills"]   = new ButtonRect(553, 559, 28, 28, @"\spr\UI3\主界面\技能按钮.spr",       "Player_Skills",   "Võ công"),
                ["Task"]     = new ButtonRect(584, 559, 28, 28, @"\spr\UI3\主界面\任务按钮.spr",       "Player_Task",     "Nhiệm vụ"),
                ["Friend"]   = new ButtonRect(615, 559, 28, 28, @"\spr\UI3\主界面\人际关系按钮.spr",   "Player_Friend",   "Bằng hữu"),
                ["Team"]     = new ButtonRect(646, 559, 28, 28, @"\spr\UI3\主界面\队伍按钮.spr",       "Player_Team",     "Quản lý đội ngũ"),
                ["Faction"]  = new ButtonRect(677, 559, 28, 28, @"\spr\UI3\主界面\帮会按钮.spr",       "Player_Faction",  "Bang phái"),
                ["ChatRoom"] = new ButtonRect(708, 559, 28, 28, @"\spr\UI3\主界面\聊天室按钮.spr",     "Player_ChatRoom", "Phòng"),
                ["Options"]  = new ButtonRect(739, 559, 28, 28, @"\spr\UI3\主界面\系统按钮.spr",       "Player_Options",  "Hệ thống"),

                ["Sit"]      = new ButtonRect(536, 502, 31, 31, @"\spr\UI3\主界面\打坐按钮.spr",   "Player_Sit",      "Ngồi"),
                ["Run"]      = new ButtonRect(567, 502, 31, 31, @"\spr\UI3\主界面\跑步按钮.spr",   "Player_Run",      "Chạy bộ / đi bộ"),
                ["Horse"]    = new ButtonRect(599, 502, 31, 31, @"\spr\UI3\主界面\骑马按钮.spr",   "Player_Horse",    "Lên xuống ngựa"),
                ["Exchange"] = new ButtonRect(630, 502, 31, 31, @"\spr\UI3\主界面\交易按钮.spr",   "Player_Exchange", "Đóng mở giao dịch"),
                ["Rec"]      = new ButtonRect(663, 502, 31, 31, @"\spr\UI3\主界面\摄像机按钮.spr", "Player_Recorder", "Quay phim"),
                ["PK"]       = new ButtonRect(695, 502, 31, 31, @"\spr\UI3\主界面\PK按钮.spr",     "Player_PK",       "Đóng mở PK"),
                ["Treasure"] = new ButtonRect(742, 502, 58, 58, @"pc-evidence/pc_hud.png#BaoVat", "Player_Treasure", "Bảo Vật"),
            };

        /// <summary>
        /// Declared in 工具控制条.ini [Main] but intentionally not a rendered PC HUD button.
        /// Button14=ZhenFa is preceded by PC comment ";û��" and the same INI has no [ZhenFa]
        /// section / Image / ClassType, so mobile must not invent a fake icon or handler.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> DisabledDeclaredToolButtons =
            new Dictionary<string, string>
            {
                ["ZhenFa"] = "Declared as Button14 in dc11ac12 工具控制条.ini, but no [ZhenFa] section/art/ClassType exists; not visible in pc-evidence/pc_hud.png.",
            };


        public readonly struct MainHudBinding
        {
            public readonly string pcName;
            public readonly string mobileElement;
            public readonly string handlerName;
            public readonly string sourceNote;

            public MainHudBinding(string pcName, string mobileElement, string handlerName, string sourceNote)
            {
                this.pcName = pcName;
                this.mobileElement = mobileElement;
                this.handlerName = handlerName;
                this.sourceNote = sourceNote;
            }
        }

        /// <summary>Interactive controls declared by 主界面玩家信息窗口.ini (uid e3b06434) and their mobile-equivalent behavior.</summary>
        public static readonly IReadOnlyList<MainHudBinding> MainHudControlBindings =
            new List<MainHudBinding>
            {
                new MainHudBinding("Friend", "BtnFriend", "OnFriendClick", @"\spr\UI3\主界面\人际关系按钮.spr -> FriendPanelService"),
                new MainHudBinding("Options", "BtnOptions", "OnOptionsClick", @"\spr\UI3\主界面\系统按钮.spr -> PC system/options menu"),
                new MainHudBinding("InputEdit", "ChatInput", string.Empty, "PC chat text input -> UI Toolkit TextField bound to ChatService"),
                new MainHudBinding("SendBtn", "SendBtn", "OnSendChatClick", @"\Spr\Ui3\主界面\主界面按钮-聊天发送.spr -> ChatService.Send"),
                new MainHudBinding("ChannelBtn", "ChatChannelIdentityBtn", "OnChatChannelIdentityClick", "PC current-channel selector -> cycle active ChatService channel"),
                new MainHudBinding("OpenChannelBtn", "OpenChannelBtn", "OnChatChannelToggleClick", "PC 60x60 channel-open proxy; Image is folder-only, so transparent behavior proxy"),
                new MainHudBinding("Face", "FaceBtn", "OpenFacePicker", @"\Spr\Ui3\表情\01.spr -> face/emote picker"),
                new MainHudBinding("Market", "BtnTreasure", "OnTreasureClick", @"\spr\UI3\主界面\奇珍阁按钮_vn.spr -> Kỳ Trân Các/MallService"),
            };


        /// <summary>Declared by 主界面玩家信息窗口.ini but not safely renderable yet because the exact PC art is unresolved/not visible in pc_hud.png.</summary>
        public static readonly IReadOnlyDictionary<string, string> UnresolvedDeclaredMainHudControls =
            new Dictionary<string, string>
            {
                ["Recorder"] = @"e3b06434 [Recorder] points to \Spr\Ui3\主界面\录像按钮.spr, but resolve_uid against 1024/slistcache/client PAKs found no matching entry and pc_hud.png does not show a distinct 25x25 recorder at that coordinate. Do not alias it to toolbar [Rec] \spr\UI3\主界面\摄像机按钮.spr.",
            };


        /// <summary>Ui3/icon_bar.ini (uid fdaebb7f), PC 1024 mode right-side icon strip.</summary>
        public static readonly IReadOnlyList<ButtonRect> IconBar =
            new List<ButtonRect>
            {
                new ButtonRect(994, 150, 25, 25, @"\spr\Ui3\arena\ico.spr", "Icon_0", "Đấu trường"),
                new ButtonRect(994, 175, 25, 25, @"\spr\Ui3\activityguide\guidebutton.spr", "Icon_1", "Hướng dẫn hoạt động"),
                new ButtonRect(994, 200, 25, 25, @"\spr\Ui3\TreasureChest\icon.spr", "Icon_2", "Rương báu"),
                new ButtonRect(994, 225, 25, 25, @"\spr\Ui3\TreasureChest\shop.spr", "Icon_3", "Cửa hàng rương báu"),
                new ButtonRect(994, 250, 25, 25, @"\spr\Ui3\pet\icon.spr", "Icon_4", "Đồng hành / thú cưng"),
                new ButtonRect(994, 275, 25, 25, @"\spr\Ui3\loginprize\icon.spr", "Icon_5", "Điểm danh"),
                new ButtonRect(994, 300, 25, 25, @"\spr\Ui3\funcprize\funcprize.spr", "Icon_6", "Thưởng chức năng"),
            };

        /// <summary>PC item hotkeys Item_0..Item_8 from 主界面玩家信息窗口.ini: 1..9 -> ShortcutUseItem(0..8).</summary>
        public static readonly IReadOnlyDictionary<string, ButtonRect> QuickItemSlots =
            new Dictionary<string, ButtonRect>
            {
                ["Item_0"] = new ButtonRect(15, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_0", "ShortcutUseItem(0)", "Phím tắt vật phẩm 1"),
                ["Item_1"] = new ButtonRect(53, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_1", "ShortcutUseItem(1)", "Phím tắt vật phẩm 2"),
                ["Item_2"] = new ButtonRect(91, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_2", "ShortcutUseItem(2)", "Phím tắt vật phẩm 3"),
                ["Item_3"] = new ButtonRect(129, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_3", "ShortcutUseItem(3)", "Phím tắt vật phẩm 4"),
                ["Item_4"] = new ButtonRect(167, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_4", "ShortcutUseItem(4)", "Phím tắt vật phẩm 5"),
                ["Item_5"] = new ButtonRect(205, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_5", "ShortcutUseItem(5)", "Phím tắt vật phẩm 6"),
                ["Item_6"] = new ButtonRect(243, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_6", "ShortcutUseItem(6)", "Phím tắt vật phẩm 7"),
                ["Item_7"] = new ButtonRect(281, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_7", "ShortcutUseItem(7)", "Phím tắt vật phẩm 8"),
                ["Item_8"] = new ButtonRect(320, 550, 36, 36, @"pc-evidence/pc_hud.png#Item_8", "ShortcutUseItem(8)", "Phím tắt vật phẩm 9"),
            };

        /// <summary>PC immediate left/right skill boxes from 主界面玩家信息窗口.ini; mobile opens the skill assignment picker.</summary>
        public static readonly IReadOnlyDictionary<string, ButtonRect> ImmediateSkillSlots =
            new Dictionary<string, ButtonRect>
            {
                ["ImediaLeftSkill"] = new ButtonRect(372, 529, 36, 36, @"pc-evidence/pc_hud.png#ImediaLeftSkill", "LeftSkillAssign", "Kỹ năng trái"),
                ["ImediaRightSkill"] = new ButtonRect(409, 529, 36, 36, @"pc-evidence/pc_hud.png#ImediaRightSkill", "RightSkillAssign", "Kỹ năng phải"),
            };

    }
}
