// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info paperdoll
// Equipment slot layout per reference (khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png).
// Real data: Weapon/Body/Head/Mount (PlayerEquipmentService.IsEquipped).
// Mapping framework: Ring/Necklace/Belt/Boots (EquipmentSlotMappingService VI names).
// Display-only (reference-matched, empty): Mask/Amulet/Charm/Trinket.
// ADR-4. Body built in C# (testable, no UXML coupling).
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Sandbox;

namespace VLTK.UI.CharacterInfo
{
    /// <summary>
    /// One paperdoll slot definition: VI label + optional real equipment binding.
    /// </summary>
    public readonly struct PaperdollSlot
    {
        public readonly string key;
        public readonly string labelVi;
        public readonly PlayerEquipSlot? equipmentSlot;   // null = display/framework only

        public PaperdollSlot(string key, string labelVi, PlayerEquipSlot? equipmentSlot = null)
        {
            this.key = key;
            this.labelVi = labelVi;
            this.equipmentSlot = equipmentSlot;
        }
    }

    /// <summary>
    /// Builds the equipment paperdoll grid and binds the 4 real equipment slots.
    /// </summary>
    public static class CharacterInfoPaperdoll
    {
        /// <summary>
        /// Slot layout matching the reference (~12 visible slots). Order = visual
        /// grid order (top-to-bottom, left-to-right as laid out by USS flex-wrap).
        /// </summary>
        public static readonly IReadOnlyList<PaperdollSlot> Slots = new[]
        {
            new PaperdollSlot("helmet",   "Mũ",        PlayerEquipSlot.Head),
            new PaperdollSlot("mask",     "Mặt Nạ"),                       // display-only
            new PaperdollSlot("amulet",   "Hộ Thân Phù"),                  // display-only
            new PaperdollSlot("weapon",   "Vũ Khí",    PlayerEquipSlot.Weapon),
            new PaperdollSlot("armor",    "Giáp",      PlayerEquipSlot.Body),
            new PaperdollSlot("belt",     "Đai Lưng"),                     // framework (mapping-known)
            new PaperdollSlot("ring",     "Nhẫn"),                         // framework
            new PaperdollSlot("necklace", "Liên"),                         // framework
            new PaperdollSlot("boots",    "Giày"),                         // framework
            new PaperdollSlot("mount",    "Ngựa",      PlayerEquipSlot.Mount),
            new PaperdollSlot("charm",    "Ngọc Bội"),                      // display-only
            new PaperdollSlot("trinket",  "Bội Kiện"),                      // display-only
        };

        /// <summary>
        /// Build the paperdoll into <paramref name="container"/>. Each slot becomes a
        /// child named <c>Slot_&lt;key&gt;</c> carrying the VI label and an
        /// <c>equipped</c> class when its real equipment slot is non-default.
        /// </summary>
        public static void Build(VisualElement container, PlayerEquipmentService equipment)
        {
            container.Clear();
            container.AddToClassList("char-paperdoll");

            foreach (var slot in Slots)
            {
                var cell = new VisualElement { name = "Slot_" + slot.key };
                cell.AddToClassList("char-paperdoll-slot");

                var label = new Label(slot.labelVi) { name = "Slot_" + slot.key + "_Label" };
                label.AddToClassList("char-paperdoll-slot-label");
                cell.Add(label);

                // Real equipment binding: mark equipped slots.
                if (slot.equipmentSlot.HasValue && equipment != null)
                {
                    if (equipment.IsEquipped(slot.equipmentSlot.Value))
                    {
                        cell.AddToClassList("equipped");
                    }
                    else
                    {
                        cell.AddToClassList("empty");
                    }
                }
                else
                {
                    cell.AddToClassList("framework");   // mapping-known or display-only
                }

                container.Add(cell);
            }
        }
    }
}
