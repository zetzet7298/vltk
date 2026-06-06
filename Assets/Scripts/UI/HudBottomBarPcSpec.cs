using System.Collections.Generic;

namespace VLTK.UI
{
    /// <summary>
    /// Source-of-truth bottom-bar layout spec ported verbatim from PC UI INI files.
    /// Origin (decoded from 1024.pak / VLTKUI_1024x768.pak via unpak_tool):
    ///   工具控制条 (uid dc11ac12) — tool control bar buttons, 1024x768 coordinate space.
    ///   主界面玩家信息窗口 (uid e3b06434) — root window, bg \spr\Ui3\thanhcongcu\jx1024.spr.
    /// Coordinates are PC absolute (Top/Left) in 1024x768 space. DO NOT invent values.
    /// </summary>
    public static class HudBottomBarPcSpec
    {
        public readonly struct ButtonRect
        {
            public readonly int left;
            public readonly int top;
            public readonly int width;
            public readonly int height;
            public readonly string spr;       // PC SPR path (\spr\UI3\主界面\...)
            public readonly string classType; // PC ClassType handler
            public readonly string tipVi;      // Vietnamese tooltip (PC font-remapped -> VN)

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

        // jx1024.spr composite: 1024x769; opaque bottom band y[680-768] = 1024x89 = the bar art.
        public const string BarArtSpr = @"\spr\Ui3\thanhcongcu\jx1024.spr";
        public const int BarBandTop = 680;
        public const int BarBandBottom = 768;
        public const int BarWidth = 1024;
        public const int BarHeight = 89;

        public const string ToolBoxSchema = "工具控制条.ini";

        /// <summary>工具控制条 (uid dc11ac12). Menu row Top=728, action row Top=675.</summary>
        public static readonly IReadOnlyDictionary<string, ButtonRect> ToolControlBar =
            new Dictionary<string, ButtonRect>
            {
                // Menu row (Top=728, 28x28)
                ["Status"]   = new ButtonRect(580, 728, 28, 28, @"\spr\UI3\主界面\人物属性按钮_0.spr", "Player_Status",   "Nhân vật"),
                ["Items"]    = new ButtonRect(611, 728, 28, 28, @"\spr\UI3\主界面\背包按钮.spr",       "Player_Items",    "Túi đồ"),
                ["ItemEx"]   = new ButtonRect(642, 728, 28, 28, @"\spr\UI3\主界面\子母袋按钮.spr",     "Player_ItemEx",   "Túi hành trang"),
                ["Skills"]   = new ButtonRect(673, 728, 28, 28, @"\spr\UI3\主界面\技能按钮.spr",       "Player_Skills",   "Võ công"),
                ["Task"]     = new ButtonRect(704, 728, 28, 28, @"\spr\UI3\主界面\任务按钮.spr",       "Player_Task",     "Nhiệm vụ"),
                ["Team"]     = new ButtonRect(766, 728, 28, 28, @"\spr\UI3\主界面\队伍按钮.spr",       "Player_Team",     "Quản lý đội ngũ"),
                ["Faction"]  = new ButtonRect(797, 728, 28, 28, @"\spr\UI3\主界面\帮会按钮.spr",       "Player_Faction",  "Bang phái"),
                ["ChatRoom"] = new ButtonRect(828, 728, 28, 28, @"\spr\UI3\主界面\聊天室按钮.spr",     "Player_ChatRoom", "Phòng"),

                // Action row (Top=675, 31x31)
                ["Sit"]      = new ButtonRect(656, 675, 31, 31, @"\spr\UI3\主界面\打坐按钮.spr",   "Player_Sit",      "Ngồi"),
                ["Run"]      = new ButtonRect(687, 675, 31, 31, @"\spr\UI3\主界面\跑步按钮.spr",   "Player_Run",      "Chạy bộ / đi bộ"),
                ["Horse"]    = new ButtonRect(719, 675, 31, 31, @"\spr\UI3\主界面\骑马按钮.spr",   "Player_Horse",    "Lên xuống ngựa"),
                ["Exchange"] = new ButtonRect(750, 675, 31, 31, @"\spr\UI3\主界面\交易按钮.spr",   "Player_Exchange", "Đóng mở giao dịch"),
                ["Rec"]      = new ButtonRect(783, 675, 31, 31, @"\spr\UI3\主界面\摄像机按钮.spr", "Player_Recorder", "Quay phim"),
                ["PK"]       = new ButtonRect(815, 675, 31, 31, @"\spr\UI3\主界面\PK按钮.spr",     "Player_PK",       "Đóng mở PK"),
            };
    }
}
