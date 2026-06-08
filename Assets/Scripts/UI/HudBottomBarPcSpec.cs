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
    }
}
