// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.1 Player Equipment Service
// Quản lý equipment slots (giáp, vũ khí, mũ, ngựa) → thay đổi SPR layers.
// Source: PC NpcRes/npcres/man + npcres/woman, 男主角贴图顺序表.txt
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Equipment slot types cho player.
    /// Mỗi slot map tới SPR variant trong MalePlayerSpriteCatalog hoặc FemalePlayerSpriteCatalog.
    /// </summary>
    public enum EquipSlot
    {
        Body,       // Giáp body → BD variant
        Head,       // Mũ/helmet → HD variant
        Hair,       // Tóc → HR variant
        Weapon,     // Vũ khí phải → RW variant
        Offhand,    // Vũ khí trái → LW variant
        Mount,      // Ngựa/horse → HorseType from NpcS.txt
    }

    /// <summary>
    /// Equipment change event data.
    /// </summary>
    public struct EquipChangeEvent
    {
        public EquipSlot slot;
        public int oldVariant;
        public int newVariant;
        public int itemId;
    }

    /// <summary>
    /// Service quản lý player equipment → SPR layer variants.
    /// Khi equip thay đổi → update variant trong sprite catalog → refresh visual.
    /// Pure C#, testable EditMode. Gắn vào player visual qua event callbacks.
    /// </summary>
    public class PlayerEquipmentService
    {
        private readonly Dictionary<EquipSlot, int> _equipped = new();

        /// <summary>Event fired khi equipment thay đổi.</summary>
        public event Action<EquipChangeEvent> OnEquipChanged;

        /// <summary>Get current variant cho một slot.</summary>
        public int GetVariant(EquipSlot slot)
        {
            return _equipped.TryGetValue(slot, out int v) ? v : DefaultVariant(slot);
        }

        /// <summary>
        /// Trang bị một item vào slot. Triggers visual refresh.
        /// Source: PC equipment → SPR path mapping từ NpcRes tables.
        /// </summary>
        public void Equip(EquipSlot slot, int variant, int itemId = 0)
        {
            int old = GetVariant(slot);
            if (old == variant) return;

            _equipped[slot] = variant;

            OnEquipChanged?.Invoke(new EquipChangeEvent
            {
                slot = slot,
                oldVariant = old,
                newVariant = variant,
                itemId = itemId,
            });

            SubsystemLog.Log("Equipment",
                $"Equipped slot {slot}: variant {old} → {variant} (item={itemId})");
        }

        /// <summary>Gỡ trang bị một slot về default.</summary>
        public void Unequip(EquipSlot slot)
        {
            Equip(slot, DefaultVariant(slot));
        }

        /// <summary>Map weapon type từ equipment.</summary>
        public PcWeaponType GetCurrentWeaponType()
        {
            int weaponVariant = GetVariant(EquipSlot.Weapon);
            return weaponVariant switch
            {
                0 => PcWeaponType.EmptyHand,
                >= 1 and <= 9 => PcWeaponType.ShortWeapon,
                >= 10 and <= 19 => PcWeaponType.LongWeapon,
                >= 20 => PcWeaponType.DualWeapon,
                _ => PcWeaponType.EmptyHand,
            };
        }

        /// <summary>
        /// Get armor variant (body) cho current equipment.
        /// PC armor variants: 0-99 body types from NpcRes tables.
        /// </summary>
        public int GetArmorVariant() => GetVariant(EquipSlot.Body);

        /// <summary>
        /// Get helmet variant (head) cho current equipment.
        /// PC helmet variants mapped from ItemDefinition.
        /// </summary>
        public int GetHelmetVariant() => GetVariant(EquipSlot.Head);

        // ── Defaults ───────────────────────────────────────────────────────

        private static int DefaultVariant(EquipSlot slot) => slot switch
        {
            EquipSlot.Body     => 19,   // Default armor variant (same as MalePlayerSpriteCatalog.ArmorVariant)
            EquipSlot.Head     => 19,   // Default head variant
            EquipSlot.Hair     => 19,   // Default hair variant
            EquipSlot.Weapon   => 0,    // Empty hand
            EquipSlot.Offhand  => 0,    // No offhand
            EquipSlot.Mount    => 0,    // No mount
            _ => 0,
        };

        // ── PC Source Evidence ─────────────────────────────────────────────

        /// <summary>
        /// Map PC item type → SPR variant. Source: NpcRes/npcres/man MA_* paths.
        /// Body armor: npcres/man/MA_BD_{variant}_ST01.spr
        /// Helmet: npcres/man/MA_HD_{variant}_ST01.spr
        /// Weapon: npcres/man/MA_RW_{variant}_ST01.spr
        /// </summary>
        public static int ItemToBodyVariant(int itemId)
        {
            // TODO: Full mapping from item DB when item contract is ready
            return 19; // Default
        }

        public static int ItemToWeaponVariant(int itemId)
        {
            // TODO: Full mapping from weapon item DB
            return 0; // Default empty hand
        }

        public static int ItemToHelmetVariant(int itemId)
        {
            // TODO: Full mapping from helmet item DB
            return 19; // Default
        }
    }
}
