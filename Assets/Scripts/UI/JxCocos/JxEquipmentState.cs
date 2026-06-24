// -----------------------------------------------------------------------------
// VLTK Mobile — JX role/equipment panel state (port KuiRoleStateVN/KuiRoleState)
//
// Nguồn:
//  - KuiRoleStateVN.h: ITEM_CELL_SIZE = 35.
//  - KuiRoleStateVN.cpp addDialogData: m_StartPos=(24,72).
//  - KuiRoleState.cpp CtrlItemMap[R_ITEM_COUNT]: 15 equip slots, key, cell span,
//    offset (x,y). addpicBox renders item + quality color, remove clears slot.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.UI.JxCocos
{
    /// <summary>UIEP_* slot order from CtrlItemMap. Thứ tự quan trọng.</summary>
    public enum JxEquipmentPanelSlot
    {
        Head = 0,      // UIEP_HEAD, key Cap, 2x2, offset 119,51
        Body = 1,      // UIEP_BODY, key Cloth, 2x3, offset 119,111
        Belt = 2,      // UIEP_BELT, key Sash, 2x1, offset 119,199
        Weapon = 3,    // UIEP_WEAPON, key Weapon, 2x4, offset 217,122
        Foot = 4,      // UIEP_FOOT, key Shoes, 2x2, offset 217,239
        Cuff = 5,      // UIEP_CUFF, key Bangle, 1x2, offset 46,95
        Amulet = 6,    // UIEP_AMULET, key Necklace, 2x1, offset 218,83
        Ring1 = 7,     // UIEP_RING1, key Ring1, 1x1, offset 46,153
        Ring2 = 8,     // UIEP_RING2, key Ring2, 1x1, offset 46,183
        Pendant = 9,   // UIEP_PENDANT, key Pendant, 1x2, offset 46,216
        Horse = 10,    // UIEP_HORSE, key Horse, 2x3, offset 119,240
        Mask = 11,     // UIEP_MASK, key Mask, 1x1, offset 46,51
        Mantle = 12,   // UIEP_PIFENG, key Mantle, 1x1, offset 46,305
        Signet = 13,   // UIEP_YINJIAN, key Signet, 1x1, offset 213,297
        Ornament = 14, // UIEP_SHIPING, key Shipin, 1x1, offset 247,297
    }

    public readonly struct JxEquipmentSlotDef
    {
        public readonly JxEquipmentPanelSlot Slot;
        public readonly string Key;
        public readonly int CellX;
        public readonly int CellY;
        public readonly float OffsetX;
        public readonly float OffsetY;

        public JxEquipmentSlotDef(JxEquipmentPanelSlot slot, string key, int cellX, int cellY, float offsetX, float offsetY)
        {
            Slot = slot;
            Key = key;
            CellX = cellX;
            CellY = cellY;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }
    }

    /// <summary>Item đang mặc trên người (source nItemIndx + Item[] fields).</summary>
    public sealed class JxEquippedItem
    {
        public int ItemId;
        public string Name = string.Empty;
        public string IconPath = string.Empty;
        public JxItemGenre Genre = JxItemGenre.Equip;
        public JxItemQuality Quality = JxItemQuality.Normal;
        public bool NotUsable; // IIEP_NOT_USEABLE → red overlay
        public int WidthCells = 1;
        public int HeightCells = 1;
    }

    public sealed class JxEquipmentState
    {
        public const int CellSize = 35;
        public const float StartX = 24f;
        public const float StartY = 72f;
        /// <summary>VN addpicBox uses tempOffsetY=55.</summary>
        public const float TempOffsetY = 55f;

        public static readonly JxEquipmentSlotDef[] SlotDefs =
        {
            new(JxEquipmentPanelSlot.Head, "Cap", 2, 2, 119, 51),
            new(JxEquipmentPanelSlot.Body, "Cloth", 2, 3, 119, 111),
            new(JxEquipmentPanelSlot.Belt, "Sash", 2, 1, 119, 199),
            new(JxEquipmentPanelSlot.Weapon, "Weapon", 2, 4, 217, 122),
            new(JxEquipmentPanelSlot.Foot, "Shoes", 2, 2, 217, 239),
            new(JxEquipmentPanelSlot.Cuff, "Bangle", 1, 2, 46, 95),
            new(JxEquipmentPanelSlot.Amulet, "Necklace", 2, 1, 218, 83),
            new(JxEquipmentPanelSlot.Ring1, "Ring1", 1, 1, 46, 153),
            new(JxEquipmentPanelSlot.Ring2, "Ring2", 1, 1, 46, 183),
            new(JxEquipmentPanelSlot.Pendant, "Pendant", 1, 2, 46, 216),
            new(JxEquipmentPanelSlot.Horse, "Horse", 2, 3, 119, 240),
            new(JxEquipmentPanelSlot.Mask, "Mask", 1, 1, 46, 51),
            new(JxEquipmentPanelSlot.Mantle, "Mantle", 1, 1, 46, 305),
            new(JxEquipmentPanelSlot.Signet, "Signet", 1, 1, 213, 297),
            new(JxEquipmentPanelSlot.Ornament, "Shipin", 1, 1, 247, 297),
        };

        private readonly Dictionary<JxEquipmentPanelSlot, JxEquippedItem> _items = new();
        public IReadOnlyDictionary<JxEquipmentPanelSlot, JxEquippedItem> Items => _items;

        public static bool TryGetSlotDef(JxEquipmentPanelSlot slot, out JxEquipmentSlotDef def)
        {
            int i = (int)slot;
            if (i >= 0 && i < SlotDefs.Length)
            {
                def = SlotDefs[i];
                return true;
            }
            def = default;
            return false;
        }

        public bool Equip(JxEquipmentPanelSlot slot, JxEquippedItem item, bool replace = true)
        {
            if (item == null || !JxInventoryState.IsValidItemId(item.ItemId)) return false;
            if (item.Genre != JxItemGenre.Equip) return false;
            if (!TryGetSlotDef(slot, out _)) return false;
            if (!replace && _items.ContainsKey(slot)) return false;
            _items[slot] = item;
            return true;
        }

        public bool Unequip(JxEquipmentPanelSlot slot)
        {
            return _items.Remove(slot);
        }

        public bool TryGetItem(JxEquipmentPanelSlot slot, out JxEquippedItem item) => _items.TryGetValue(slot, out item);

        /// <summary>
        /// addpicBox bgcolorLayer position:
        /// (panelWidth/2 + offsetX, panelHeight - offsetY - slotHeight - tempOffsetY)
        /// </summary>
        public static (float x, float y) SlotBackgroundPosition(JxEquipmentPanelSlot slot, float panelWidth, float panelHeight)
        {
            if (!TryGetSlotDef(slot, out var def)) throw new ArgumentOutOfRangeException(nameof(slot));
            float slotH = def.CellY * CellSize;
            return (panelWidth / 2f + def.OffsetX, panelHeight - def.OffsetY - slotH - TempOffsetY);
        }

        /// <summary>
        /// addpicBox item sprite position:
        /// (panelWidth/2 + offsetX + bgW/2 - texW/2,
        ///  panelHeight - offsetY - (bgH+texH)/2 - tempOffsetY)
        /// </summary>
        public static (float x, float y) ItemSpritePosition(JxEquipmentPanelSlot slot, float panelWidth, float panelHeight, float textureWidth, float textureHeight)
        {
            if (!TryGetSlotDef(slot, out var def)) throw new ArgumentOutOfRangeException(nameof(slot));
            float bgW = def.CellX * CellSize;
            float bgH = def.CellY * CellSize;
            return (panelWidth / 2f + def.OffsetX + bgW / 2f - textureWidth / 2f,
                panelHeight - def.OffsetY - (bgH + textureHeight) / 2f - TempOffsetY);
        }
    }
}
