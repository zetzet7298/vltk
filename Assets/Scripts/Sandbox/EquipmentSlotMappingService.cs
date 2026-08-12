// -----------------------------------------------------------------------------
// VLTK Mobile — ST-05.1 Equipment Slot Mapping & Magic Attribute Codes
// PC source: Equipment slot categories, item type codes, magic attribute mapping.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>PC item category codes (ItemType column from ItemList.txt).</summary>
    public enum PcItemCategory
    {
        Weapon = 1,
        Helmet = 2,
        Armor = 3,
        Belt = 4,
        Boots = 5,
        Necklace = 6,
        Ring = 7,
        Medicament = 8,   // Thuốc
        Material = 9,     // Nguyên liệu
        Book = 10,        // Mật tịch / Võ công thư
        TaskItem = 11,    // Nhiệm vụ vật phẩm
        Currency = 12,    // Tiền tệ (Bạc, Kim Bảo)
        // PR-1 bind-accessory-equipment-slots — append-only (values 13–15)
        Mask = 13,         // Mặt Nạ (PC mask.txt, equip_mask D11)
        Pendant = 14,      // Hộ Thân Phù (PC pendant.txt, equip_pendant D9)
        Trinket = 15,      // Bội Kiện / Ngọc Bội (PC shipin.txt, equip_shipin D14)
    }

    /// <summary>PC weapon sub-types.</summary>
    public enum PcWeaponSubType
    {
        None = 0,
        Sword = 1,        // Kiếm
        Blade = 2,        // Đao
        Spear = 3,        // Thương
        Staff = 4,        // Trượng
        Dagger = 5,       // Tiểu đao
        Bow = 6,          // Cung
        Claw = 7,         // Trảo
        Hammer = 8,       // Chùy
    }

    /// <summary>PC armor sub-types.</summary>
    public enum PcArmorSubType
    {
        Cloth = 0,        // Vải (Pháp hệ)
        Light = 1,        // Da (Cận chiến nhẹ)
        Heavy = 2,        // Giáp sắt (Cận chiến nặng)
    }

    [Serializable]
    public class EquipmentSlotMapping
    {
        public PcItemCategory category;
        public string slotNameVi;
        public bool isEquippable;
        public int maxStackSize;
    }

    /// <summary>
    /// Service quản lý ánh xạ Equipment Slots, Item Categories và Magic Attribute Codes.
    /// PC source: ItemList.txt ItemType, EquipSlot, magicattrcode columns.
    /// </summary>
    public static class EquipmentSlotMappingService
    {
        private static readonly Dictionary<PcItemCategory, EquipmentSlotMapping> Mappings = new()
        {
            { PcItemCategory.Weapon, new EquipmentSlotMapping { category = PcItemCategory.Weapon, slotNameVi = "Vũ Khí", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Helmet, new EquipmentSlotMapping { category = PcItemCategory.Helmet, slotNameVi = "Mũ", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Armor, new EquipmentSlotMapping { category = PcItemCategory.Armor, slotNameVi = "Giáp", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Belt, new EquipmentSlotMapping { category = PcItemCategory.Belt, slotNameVi = "Đai Lưng", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Boots, new EquipmentSlotMapping { category = PcItemCategory.Boots, slotNameVi = "Giày", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Necklace, new EquipmentSlotMapping { category = PcItemCategory.Necklace, slotNameVi = "Liên", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Ring, new EquipmentSlotMapping { category = PcItemCategory.Ring, slotNameVi = "Nhẫn", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Medicament, new EquipmentSlotMapping { category = PcItemCategory.Medicament, slotNameVi = "Thuốc", isEquippable = false, maxStackSize = 99 } },
            { PcItemCategory.Material, new EquipmentSlotMapping { category = PcItemCategory.Material, slotNameVi = "Nguyên Liệu", isEquippable = false, maxStackSize = 999 } },
            { PcItemCategory.Book, new EquipmentSlotMapping { category = PcItemCategory.Book, slotNameVi = "Mật Tịch", isEquippable = false, maxStackSize = 1 } },
            { PcItemCategory.TaskItem, new EquipmentSlotMapping { category = PcItemCategory.TaskItem, slotNameVi = "Vật Phẩm Nhiệm Vụ", isEquippable = false, maxStackSize = 99 } },
            { PcItemCategory.Currency, new EquipmentSlotMapping { category = PcItemCategory.Currency, slotNameVi = "Tiền Tệ", isEquippable = false, maxStackSize = 999999 } },
            // PR-1 — accessory categories (equippable, stack 1)
            { PcItemCategory.Mask, new EquipmentSlotMapping { category = PcItemCategory.Mask, slotNameVi = "Mặt Nạ", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Pendant, new EquipmentSlotMapping { category = PcItemCategory.Pendant, slotNameVi = "Hộ Thân Phù", isEquippable = true, maxStackSize = 1 } },
            { PcItemCategory.Trinket, new EquipmentSlotMapping { category = PcItemCategory.Trinket, slotNameVi = "Bội Kiện", isEquippable = true, maxStackSize = 1 } },
        };

        public static EquipmentSlotMapping GetMapping(PcItemCategory category)
            => Mappings.TryGetValue(category, out var m) ? m : null;

        public static bool IsEquippable(PcItemCategory category)
            => Mappings.TryGetValue(category, out var m) && m.isEquippable;

        public static int GetMaxStack(PcItemCategory category)
            => Mappings.TryGetValue(category, out var m) ? m.maxStackSize : 1;

        /// <summary>
        /// Map PC ItemType code (1–12, from ItemList.txt ItemType column) to PcItemCategory.
        /// This axis is distinct from EQUIPDETAILTYPE (0–16); see DetailTypeToCategory.
        /// </summary>
        public static PcItemCategory ItemTypeToCategory(int itemType) => itemType switch
        {
            1 => PcItemCategory.Weapon,
            2 => PcItemCategory.Helmet,
            3 => PcItemCategory.Armor,
            4 => PcItemCategory.Belt,
            5 => PcItemCategory.Boots,
            6 => PcItemCategory.Necklace,
            7 => PcItemCategory.Ring,
            8 => PcItemCategory.Medicament,
            9 => PcItemCategory.Material,
            10 => PcItemCategory.Book,
            11 => PcItemCategory.TaskItem,
            12 => PcItemCategory.Currency,
            _ => PcItemCategory.Material,
        };

        /// <summary>
        /// Map PC EQUIPDETAILTYPE code (0–16, from GameDataDef.h) to PcItemCategory.
        /// This is the single source of truth for equipment-slot classification.
        /// Distinct axis from ItemTypeToCategory (which maps ItemType 1–12).
        /// PC EQUIPDETAILTYPE: ring=3, amulet(necklace)=4, pendant=9, mask=11, shipin=14.
        /// </summary>
        public static PcItemCategory DetailTypeToCategory(int detailType) => detailType switch
        {
            3 => PcItemCategory.Ring,       // equip_ring
            4 => PcItemCategory.Necklace,   // equip_amulet (PC amulet.txt IS necklace "Liên")
            9 => PcItemCategory.Pendant,    // equip_pendant
            11 => PcItemCategory.Mask,      // equip_mask
            14 => PcItemCategory.Trinket,   // equip_shipin
            _ => PcItemCategory.Material,   // fallback (same default as ItemTypeToCategory)
        };

        /// <summary>Get all magic attribute codes for an item's stat deltas.</summary>
        public static Dictionary<int, int> AggregateAttributes(ItemDefinition item)
        {
            var result = new Dictionary<int, int>();
            if (item == null) return result;

            foreach (var delta in item.statDeltas)
            {
                if (!result.ContainsKey(delta.attrCode))
                    result[delta.attrCode] = 0;
                result[delta.attrCode] += delta.value;
            }
            return result;
        }
    }

    // ── Magic Attribute Code Definitions (PC attr codes) ───────────────────

    /// <summary>PC magic attribute code constants (from ItemList.txt magicattr columns).</summary>
    public static class PcMagicAttr
    {
        public const int PhysicsDamageMin = 1;
        public const int PhysicsDamageMax = 2;
        public const int Defense = 3;
        public const int HpMax = 4;
        public const int MpMax = 5;
        public const int Strength = 6;
        public const int Dexterity = 7;
        public const int Vitality = 8;
        public const int InnerStrength = 9;
        public const int AttackRating = 10;
        public const int DeadlyStrike = 11;   // Bạo kích
        public const int FireResist = 20;
        public const int ColdResist = 21;
        public const int LightResist = 22;
        public const int PoisonResist = 23;
        public const int PhysicsResist = 24;
        public const int RunSpeed = 30;
        public const int SkillCost = 31;      // Giảm mana chiêu
        public const int LifeDrain = 40;      // Hút máu
        public const int ManaDrain = 41;      // Hút mana
        public const int DamageReturn = 42;   // Phản đòn
        public const int Luck = 50;           // May mắn (drop rate)
    }
}
