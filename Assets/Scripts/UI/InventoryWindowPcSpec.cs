namespace VLTK.UI
{
    /// <summary>
    /// PC inventory ("Hành Trang" / 道具界面) window spec extracted from
    /// 1024.pak: trigger UID dc11ac12 [Items] -> Player_Items/Open([[items]]),
    /// window UID 05ea8560, item colors UID 7bfc9072.
    /// </summary>
    public static class InventoryWindowPcSpec
    {
        public const string PcOpenCommand = "Open([[items]])";
        public const string PcButtonClassType = "Player_Items";
        public const string PcToolbarUid = "dc11ac12";
        public const string PcWindowUid = "05ea8560";
        public const string PcItemColorUid = "7bfc9072";
        public const string PcBackgroundSpr = @"\spr\Ui3\道具\daojumianban.spr";
        public const string PcBackgroundArtName = "道具面板2";
        public const string PcBackgroundSpriteUid = "16503a96";
        public const string PcBackgroundDuplicateSpriteUid = "77b67466";

        public const int WindowLeft = 805;
        public const int WindowTop = 145;
        public const int WindowWidth = 214;
        public const int WindowHeight = 474;

        // PC source-of-truth ItemBox (05ea8560): 6 columns × 10 rows = 60 slots.
        public const int PcGridLeft = 24;
        public const int PcGridTop = 72;
        public const int PcGridWidth = 168;
        public const int PcGridHeight = 280;
        public const int PcGridColumns = 6;
        public const int PcGridRows = 10;
        public const int PcSlotCount = PcGridColumns * PcGridRows;
        public const int PcUnitBorder = 1;

        // Mobile capacity override requested by the player: 4 columns × 7 rows = 28 slots.
        public const int GridLeft = PcGridLeft;
        public const int GridTop = PcGridTop;
        public const int GridColumns = 4;
        public const int GridRows = 7;
        public const int SlotCount = GridColumns * GridRows;
        public const int UnitBorder = PcUnitBorder;
        public const int SlotInnerSize = 26;
        public const int SlotMargin = 1;
        public const int SlotOuterSize = SlotInnerSize + SlotMargin * 2;
        public const int GridWidth = GridColumns * SlotOuterSize;
        public const int GridHeight = GridRows * SlotOuterSize;

        public const int MoneyLeft = 53;
        public const int MoneyTop = 353;
        public const int MoneyWidth = 110;
        public const int MoneyHeight = 14;

        public const int CloseLeft = 142;
        public const int CloseTop = 414;
        public const int CloseWidth = 65;
        public const int CloseHeight = 28;

        /// <summary>RGB color (0-255) ported from PC INI.</summary>
        public readonly struct Rgb
        {
            public readonly int r;
            public readonly int g;
            public readonly int b;
            public Rgb(int r, int g, int b) { this.r = r; this.g = g; this.b = b; }
        }

        // Frame colors — 05ea8560 [Settings].
        public static readonly Rgb FrameBorderColor = new Rgb(100, 80, 30);  // BGBorderColor
        public static readonly Rgb FrameBgColor = new Rgb(243, 194, 70);     // BGSpriteColor
        public static readonly Rgb FramePurpleColor = new Rgb(188, 40, 255); // BGPurpleColor
        public static readonly Rgb FramePurpleBorder = new Rgb(100, 80, 123);// BGPurpleBorder
        public static readonly Rgb FramePlatinaBorder = new Rgb(110, 110, 110);
        public static readonly Rgb FramePlatinaColor = new Rgb(240, 240, 240);

        // Money text color — 05ea8560 [Money] Color=255,217,78.
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
