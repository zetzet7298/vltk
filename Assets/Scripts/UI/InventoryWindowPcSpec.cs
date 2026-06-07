namespace VLTK.UI
{
    /// <summary>
    /// Source-of-truth inventory ("Hành Trang" / 物品 backpack) window spec ported
    /// verbatim from PC UI INI files extracted from 1024.pak via unpak_tool.
    ///
    /// PC open behavior (1024.pak/autoexec.lua):
    ///   背包按钮 (ClassType Player_Items), key F4, key U  ->  Open([[items]])
    ///
    /// PC window layout (6a5d8c4c.ini — grid item window template, 储物箱):
    ///   [Main]   Left=40 Top=145 Width=214 Height=454  Moveable=1
    ///            Image=\spr\Ui3\储物箱\储物箱更新.spr
    ///   [ItemBox] Left=24 Top=72 Width=168 Height=280 HUnits=6 VUnits=10 UnitBorder=2
    ///   [Money]   Left=53 Top=353 Width=138 Height=14 Color=255,217,78
    ///   [CloseBtn] Left=109 Top=394 Width=80 Height=28
    ///
    /// Frame colors ([Settings] block in 6a5d8c4c.ini) and item-tier text colors
    /// (7bfc9072.ini) are exact PC values — used to build the frame because the
    /// window SPR (储物箱更新.spr) is NOT present in any active PAK (verified by
    /// index scan across all 30 data PAKs; scanner validated against the known
    /// jx1024 uid) and pc_hud.png does not show the window open, so there is no
    /// authentic pixel art to extract. No SPR is fabricated.
    ///
    /// Mobile adaptation (approved): 4 columns x 7 rows = 28 slots.
    /// DO NOT invent values — all numbers below trace to the cited PC INI.
    /// </summary>
    public static class InventoryWindowPcSpec
    {
        // PC open trigger (autoexec.lua): Open([[items]]) via 背包按钮 / F4 / U.
        public const string PcOpenCommand = "Open([[items]])";
        public const string PcButtonClassType = "Player_Items";
        public const string PcBackgroundSpr = @"\spr\Ui3\储物箱\储物箱更新.spr";

        // Mobile grid (approved): 4 columns x 7 rows.
        public const int GridColumns = 4;
        public const int GridRows = 7;
        public const int SlotCount = GridColumns * GridRows; // 28

        // PC [ItemBox] UnitBorder=2 (gap between item tiles).
        public const int UnitBorder = 2;

        /// <summary>RGB color (0-255) ported from PC INI.</summary>
        public readonly struct Rgb
        {
            public readonly int r;
            public readonly int g;
            public readonly int b;
            public Rgb(int r, int g, int b) { this.r = r; this.g = g; this.b = b; }
        }

        // Frame colors — 6a5d8c4c.ini [Settings].
        public static readonly Rgb FrameBorderColor = new Rgb(100, 80, 30);  // BGBorderColor
        public static readonly Rgb FrameBgColor = new Rgb(243, 194, 70);     // BGSpriteColor
        public static readonly Rgb FramePurpleColor = new Rgb(188, 40, 255); // BGPurpleColor
        public static readonly Rgb FramePurpleBorder = new Rgb(100, 80, 123);// BGPurpleBorder
        public static readonly Rgb FramePlatinaBorder = new Rgb(110, 110, 110);
        public static readonly Rgb FramePlatinaColor = new Rgb(240, 240, 240);

        // Money text color — 6a5d8c4c.ini [Money] Color=255,217,78.
        public static readonly Rgb MoneyColor = new Rgb(255, 217, 78);

        // Item quality tier text colors — 7bfc9072.ini.
        // itemQuality convention (InventoryPanelRow): 0=white,1=blue,2=purple,3=gold/platina,4=red.
        public static readonly Rgb TierWhite = new Rgb(255, 255, 255);  // [WhiteItem]
        public static readonly Rgb TierBlue = new Rgb(51, 102, 250);    // [BlueItem]
        public static readonly Rgb TierPurple = new Rgb(188, 64, 255);  // [PurpleItem]
        public static readonly Rgb TierGold = new Rgb(243, 194, 90);    // [GoldItem]/[PlatinaItem]
        public static readonly Rgb TierRed = new Rgb(255, 51, 51);      // [RedItem]
        public static readonly Rgb TierOverColor = new Rgb(0, 255, 255);   // OverColor (hover)
        public static readonly Rgb TierSelColor = new Rgb(255, 100, 122);  // SelColor (selected)

        /// <summary>Map an item quality tier (0-4) to its PC text color.</summary>
        public static Rgb TierColor(int itemQuality)
        {
            switch (itemQuality)
            {
                case 1: return TierBlue;
                case 2: return TierPurple;
                case 3: return TierGold;
                case 4: return TierRed;
                default: return TierWhite;
            }
        }
    }
}
