// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info paperdoll
// Equipment slot layout per reference (khi_nhan_nut_thong_tin_nhan_vat_tab_hanh_trang.png).
// Real data: visual Weapon/Body/Head/Mount (PlayerEquipmentService.IsEquipped)
// + gameplay-equipped state for all canonical EquipSlot slots (InventoryService.Equipped).
// PC semantics: pendant = Hộ Thân Phù (pendant.txt D9), necklace = Liên (amulet.txt D4).
// ADR-4. Body built in C# (testable, no UXML coupling).
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Model;
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
        public readonly EquipSlot? gameplaySlot;          // null = no gameplay item binding
        public readonly PlayerEquipSlot? equipmentSlot;   // null = no visual SPR-layer binding

        public PaperdollSlot(
            string key,
            string labelVi,
            EquipSlot? gameplaySlot = null,
            PlayerEquipSlot? equipmentSlot = null)
        {
            this.key = key;
            this.labelVi = labelVi;
            this.gameplaySlot = gameplaySlot;
            this.equipmentSlot = equipmentSlot;
        }
    }

    /// <summary>
    /// Builds the equipment paperdoll grid and binds visual + gameplay equipment slots.
    /// </summary>
    public static class CharacterInfoPaperdoll
    {
        /// <summary>
        /// Slot layout matching the reference plus PC-parity second ring (13 visible slots).
        /// Order = visual grid order (top-to-bottom, left-to-right as laid out by USS flex-wrap).
        /// </summary>
        public static readonly IReadOnlyList<PaperdollSlot> Slots = new[]
        {
            new PaperdollSlot("helmet",   "Mũ",          EquipSlot.Helmet,   PlayerEquipSlot.Head),
            new PaperdollSlot("mask",     "Mặt Nạ",      EquipSlot.Mask),
            new PaperdollSlot("pendant",  "Hộ Thân Phù", EquipSlot.Pendant),
            new PaperdollSlot("weapon",   "Vũ Khí",      EquipSlot.Weapon,   PlayerEquipSlot.Weapon),
            new PaperdollSlot("armor",    "Giáp",        EquipSlot.Armor,    PlayerEquipSlot.Body),
            new PaperdollSlot("belt",     "Đai Lưng",    EquipSlot.Belt),
            new PaperdollSlot("ring",     "Nhẫn",        EquipSlot.Ring),
            new PaperdollSlot("ring2",    "Nhẫn",        EquipSlot.Ring2),
            new PaperdollSlot("necklace", "Liên",        EquipSlot.Necklace),
            new PaperdollSlot("boots",    "Giày",        EquipSlot.Boots),
            new PaperdollSlot("mount",    "Ngựa",        EquipSlot.Mount,    PlayerEquipSlot.Mount),
            new PaperdollSlot("trinket",  "Bội Kiện",    EquipSlot.Trinket),
            new PaperdollSlot("trinket2", "Ngọc Bội",    EquipSlot.Trinket2),
        };

        /// <summary>
        /// Build the paperdoll into <paramref name="container"/>. Each slot becomes a
        /// child named <c>Slot_&lt;key&gt;</c> carrying the VI label. Visual binding is checked
        /// first (regression guard), then gameplay equipped-state binding.
        /// </summary>
        public static void Build(
            VisualElement container,
            PlayerEquipmentService equipment,
            IReadOnlyDictionary<EquipSlot, ItemDefinition> equippedItems = null)
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

                bool hasVisualBinding = slot.equipmentSlot.HasValue;
                bool hasGameplayBinding = slot.gameplaySlot.HasValue;
                bool visualEquipped = hasVisualBinding
                    && equipment != null
                    && equipment.IsEquipped(slot.equipmentSlot.Value);
                bool gameplayEquipped = hasGameplayBinding
                    && equippedItems != null
                    && equippedItems.ContainsKey(slot.gameplaySlot.Value);

                if (visualEquipped)
                {
                    cell.AddToClassList("equipped");
                }
                else if (gameplayEquipped)
                {
                    cell.AddToClassList("equipped");
                }
                else if (hasVisualBinding || hasGameplayBinding)
                {
                    cell.AddToClassList("empty");
                }
                else
                {
                    cell.AddToClassList("framework");
                }

                container.Add(cell);
            }
        }
    }
}
