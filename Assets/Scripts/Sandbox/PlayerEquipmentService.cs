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
    public enum PlayerEquipSlot
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
        public PlayerEquipSlot slot;
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
        private readonly Dictionary<PlayerEquipSlot, int> _equipped = new();

        /// <summary>Event fired khi equipment thay đổi.</summary>
        public event Action<EquipChangeEvent> OnEquipChanged;

        /// <summary>Get current variant cho một slot.</summary>
        public int GetVariant(PlayerEquipSlot slot)
        {
            return _equipped.TryGetValue(slot, out int v) ? v : DefaultVariant(slot);
        }

        /// <summary>
        /// Trang bị một item vào slot. Triggers visual refresh.
        /// Source: PC equipment → SPR path mapping từ NpcRes tables.
        /// </summary>
        public void Equip(PlayerEquipSlot slot, int variant, int itemId = 0)
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

            SubsystemLog.Info("Equipment",
                $"Equipped slot {slot}: variant {old} → {variant} (item={itemId})");
        }

        /// <summary>Gỡ trang bị một slot về default.</summary>
        public void Unequip(PlayerEquipSlot slot)
        {
            Equip(slot, DefaultVariant(slot));
        }

        /// <summary>Map weapon type từ equipment.</summary>
        public PcWeaponType GetCurrentWeaponType()
        {
            return WeaponVariantToType(GetVariant(PlayerEquipSlot.Weapon));
        }

        public static PcWeaponType WeaponVariantToType(int weaponVariant)
        {
            return weaponVariant switch
            {
                0 => PcWeaponType.EmptyHand,
                >= 1 and <= 9 => PcWeaponType.ShortWeapon,
                MalePlayerSpriteCatalog.DualWeaponVariant => PcWeaponType.DualWeapon,
                >= 10 and <= 19 => PcWeaponType.LongWeapon,
                >= 20 => PcWeaponType.DualWeapon,
                _ => PcWeaponType.EmptyHand,
            };
        }

        /// <summary>
        /// Get armor variant (body) cho current equipment.
        /// PC armor variants: 0-99 body types from NpcRes tables.
        /// </summary>
        public int GetArmorVariant() => GetVariant(PlayerEquipSlot.Body);

        /// <summary>
        /// Get helmet variant (head) cho current equipment.
        /// PC helmet variants mapped from ItemDefinition.
        /// </summary>
        public int GetHelmetVariant() => GetVariant(PlayerEquipSlot.Head);

        // ── Defaults ───────────────────────────────────────────────────────

        private static int DefaultVariant(PlayerEquipSlot slot) => slot switch
        {
            PlayerEquipSlot.Body     => 19,   // Default armor variant (same as MalePlayerSpriteCatalog.ArmorVariant)
            PlayerEquipSlot.Head     => 19,   // Default head variant
            PlayerEquipSlot.Hair     => 19,   // Default hair variant
            PlayerEquipSlot.Weapon   => 0,    // Empty hand
            PlayerEquipSlot.Offhand  => 0,    // No offhand
            PlayerEquipSlot.Mount    => 0,    // No mount
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
            // Built-in test armors in sandbox:
            if (itemId >= 2001 && itemId <= 2004)
            {
                return 19; // Default staged armor variant
            }

            // PC items (0-based row index from armor.txt):
            return itemId switch
            {
                >= 0 and <= 279 => 8,
                >= 280 and <= 289 => 13,
                _ => 19, // Default
            };
        }

        public static int ItemToWeaponVariant(int itemId)
        {
            // PC Client 6.0/settings/item/meleeweapon.txt uses implicit row ids:
            // 1-21 sword/blade (short weapon), 22-41 staff/spear (long weapon),
            // 42-71 dual/paired weapons (dual weapon).
            return itemId switch
            {
                >= 1 and <= 21 => MalePlayerSpriteCatalog.ShortWeaponVariant,
                >= 22 and <= 41 => MalePlayerSpriteCatalog.StaffWeaponVariant,
                >= 42 and <= 71 => MalePlayerSpriteCatalog.DualWeaponVariant,
                _ => MalePlayerSpriteCatalog.EmptyWeaponVariant,
            };
        }

        public static PcWeaponType ItemToWeaponType(int itemId)
        {
            return WeaponVariantToType(ItemToWeaponVariant(itemId));
        }

        public static int ItemToHelmetVariant(int itemId)
        {
            // Built-in test helmets in sandbox:
            if (itemId >= 3001 && itemId <= 3003)
            {
                return 19; // Default staged helmet variant
            }

            // PC items (0-based row index from helm.txt):
            return itemId switch
            {
                10 or 11 or
                (>= 40 and <= 49) or
                (>= 80 and <= 89) or
                94 or 95 or 96 or
                104 or 105 or 106 or
                117 or 118 or 119 or
                (>= 124 and <= 129) or
                (>= 134 and <= 139) => 10,
                >= 0 and <= 139 => 9,
                _ => 19, // Default
            };
        }
    }
}
