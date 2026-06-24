// -----------------------------------------------------------------------------
// VLTK Mobile — JX item tooltip state (port of KuiItemdescVN.cpp)
//
// Nguồn: KuiItemdescVN.cpp (3245 L). addDialogData build tooltip từ Item[uId].
// Port-critical verify-able:
//  - Durability display logic (lines 644-665): FOREVER(-1) / BROKEN(0,1) /
//    NEEDFIX(<=5) / LIFE(>5), với mask exception (COUNT cho mask).
//  - Label set localized (CLIENT_UI_ITEM_*). Việt hoá mặc định.
//  - Item field model: name/genre/level/price/maxDur/detailType.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.UI.JxCocos
{
    /// <summary>Label durability theo nguồn CLIENT_UI_ITEM_*.</summary>
    public enum JxDurabilityLabel
    {
        Forever,   // CLIENT_UI_ITEM_FOREVER — vĩnh viễn
        Broken,    // CLIENT_UI_ITEM_BROKEN — hỏng
        NeedFix,   // CLIENT_UI_ITEM_NEEDFIX — cần sửa
        Life,      // CLIENT_UI_ITEM_LIFE — tuổi thọ
        Count,     // CLIENT_UI_ITEM_COUNT — số lượng
    }

    /// <summary>Model dữ liệu tooltip (Item[uId] fields dùng để render).</summary>
    public sealed class JxItemTooltipData
    {
        public string Name = string.Empty;
        public JxItemGenre Genre;
        public int Level;
        public int Price;
        /// <summary>Độ bền hiện tại. -1 = không có (forever).</summary>
        public int Durability = -1;
        public int MaxDurability;
        /// <summary>Equip detail type (cho mask exception). -1 = non-equip.</summary>
        public int EquipDetailType = -1;
        /// <summary>Số thuộc tính ảo (magic) — cho equip hiển thị.</summary>
        public int MagicCount;
        /// <summary>Có thể xếp chồng không.</summary>
        public bool Stackable;
        public int Stack;
    }

    /// <summary>State thuần build text tooltip. Verify được trong EditMode.</summary>
    public static class JxItemTooltipState
    {
        /// <summary>equip_mask detail type (nguồn EQUIPDETAILTYPE).</summary>
        public const int EquipMaskDetailType = 11;

        /// <summary>Ngưỡng durability "cần sửa" (nguồn: GetDurability()&lt;=5).</summary>
        public const int NeedFixThreshold = 5;

        /// <summary>
        /// Quyết định label durability theo nguồn (lines 644-665):
        /// -1 → Forever; 0/1 → Broken;
        /// (2..5): equip+mask → Count, else → NeedFix;
        /// (&gt;5): equip+mask → Count, else → Life.
        /// </summary>
        public static JxDurabilityLabel ResolveDurabilityLabel(int durability, bool isEquip, int equipDetailType)
        {
            if (durability == -1) return JxDurabilityLabel.Forever;
            if (durability == 0 || durability == 1) return JxDurabilityLabel.Broken;
            bool isMask = isEquip && equipDetailType == EquipMaskDetailType;
            if (durability <= NeedFixThreshold && durability > 0)
                return isMask ? JxDurabilityLabel.Count : JxDurabilityLabel.NeedFix;
            return isMask ? JxDurabilityLabel.Count : JxDurabilityLabel.Life;
        }

        /// <summary>Tiền tố label Việt hoá (mặc định). Override được qua delegate.</summary>
        public static readonly JxDurabilityLabel[] AllLabels =
            (JxDurabilityLabel[])Enum.GetValues(typeof(JxDurabilityLabel));

        private static readonly string[] DefaultViLabels =
        {
            "Vĩnh viễn",      // Forever
            "Hỏng",           // Broken
            "Cần sửa: ",      // NeedFix
            "Tuổi thọ: ",     // Life
            "Số lượng: ",     // Count
        };

        /// <summary>Resovle label text Việt (mặc định).</summary>
        public static string LabelText(JxDurabilityLabel label) =>
            DefaultViLabels[(int)label];

        /// <summary>
        /// Build chuỗi durability hiển thị (port szDurInfo). Format "%s%d/%d"
        /// cho các label có cur/max; Forever/Broken chỉ hiện text.
        /// </summary>
        public static string FormatDurability(int durability, int maxDurability, bool isEquip, int equipDetailType)
        {
            var label = ResolveDurabilityLabel(durability, isEquip, equipDetailType);
            if (label == JxDurabilityLabel.Forever || label == JxDurabilityLabel.Broken)
                return LabelText(label);
            // Cần đảm bảo durability hiện không âm cho %d (Forever/Broken đã thoát).
            int dur = durability < 0 ? 0 : durability;
            return string.Format("{0}{1}/{2}", LabelText(label), dur, maxDurability);
        }

        /// <summary>Helper: dùng JxItemTooltipData.</summary>
        public static string FormatDurability(JxItemTooltipData data)
        {
            if (data == null) return string.Empty;
            bool isEquip = data.Genre == JxItemGenre.Equip;
            return FormatDurability(data.Durability, data.MaxDurability, isEquip, data.EquipDetailType);
        }

        /// <summary>
        /// Tooltip có cần hiện block durability không? Nguồn: chỉ khi item có magic
        /// durability attribute (nAttribType == magic_durability_v). Tương đương:
        /// equip có độ bền, hoặc durability != -1.
        /// </summary>
        public static bool ShowDurability(JxItemTooltipData data) =>
            data != null && data.Durability != -1 || (data.Genre == JxItemGenre.Equip && data.MaxDurability > 0);

        /// <summary>Format giá (nguồn GetPrice). Số nguyên không định dạng đặc biệt.</summary>
        public static string FormatPrice(int price) => price.ToString("N0");

        /// <summary>
        /// Tooltip có hiện nút "Sử dụng" không (port chuanCallBackFunc genre logic)?
        /// Medicine/Task/TownPortal/Fusion → hiện; Equip/Mine/Materials → không.
        /// </summary>
        public static bool CanUse(JxItemGenre genre) =>
            genre == JxItemGenre.Medicine || genre == JxItemGenre.Task
            || genre == JxItemGenre.TownPortal || genre == JxItemGenre.Fusion;

        /// <summary>Tooltip có hiện nút "Vứt bỏ" (diuCallBackFunc)? Mọi genre đều vứt được.</summary>
        public static bool CanDiscard(JxItemGenre genre) => true;

        /// <summary>
        /// Tooltip có hiện "Phím tắt" (kuaiCallBackFunc)? Nguồn: non-equip stackable
        /// hoặc item dùng được. Equip không gán phím tắt.
        /// </summary>
        public static bool CanShortcut(JxItemGenre genre, bool stackable) =>
            genre != JxItemGenre.Equip && (stackable || CanUse(genre));
    }
}
