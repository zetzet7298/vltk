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
        public int oldItemId;
        public int itemId;
    }

    /// <summary>
    /// Service quản lý player equipment → SPR layer variants.
    /// Khi equip thay đổi → update variant trong sprite catalog → refresh visual.
    /// Pure C#, testable EditMode. Gắn vào player visual qua event callbacks.
    /// </summary>
    public class PlayerEquipmentService
    {
        public bool IsFemale { get; set; }
        private IPlayerEquipmentHost _host;

        private readonly Dictionary<PlayerEquipSlot, int> _equipped = new();
        private readonly Dictionary<PlayerEquipSlot, int> _equippedItemIds = new();
        private int _currentWeaponItemId = 0;

        /// <summary>Event fired khi equipment thay đổi.</summary>
        public event Action<EquipChangeEvent> OnEquipChanged;

        public PlayerEquipmentService() : this(null) { }
        public PlayerEquipmentService(IPlayerEquipmentHost host) { _host = host; }

        public void AttachHost(IPlayerEquipmentHost host) { _host = host; }

        /// <summary>Get current variant cho một slot.</summary>
        public int GetVariant(PlayerEquipSlot slot)
        {
            return _equipped.TryGetValue(slot, out int v) ? v : DefaultVariant(slot);
        }

        /// <summary>
        /// True khi slot có variant khác default hoặc item identity khác 0 (đã được trang bị). Read-only bind.
        /// HUD-003 Character Info paperdoll dùng để phân biệt ô đã/trống.
        /// </summary>
        public bool IsEquipped(PlayerEquipSlot slot)
        {
            return GetVariant(slot) != DefaultVariant(slot) || GetItemId(slot) != 0;
        }

        /// <summary>
        /// Trang bị một item vào slot. Triggers visual refresh.
        /// Source: PC equipment → SPR path mapping từ NpcRes tables.
        /// </summary>
        public void Equip(PlayerEquipSlot slot, int variant, int itemId = 0)
        {
            int old = GetVariant(slot);
            int oldItemId = GetItemId(slot);
            if (old == variant && oldItemId == itemId) return;

            _equipped[slot] = variant;
            _equippedItemIds[slot] = itemId;
            if (slot == PlayerEquipSlot.Weapon)
            {
                _currentWeaponItemId = itemId;
            }

            OnEquipChanged?.Invoke(new EquipChangeEvent
            {
                slot = slot,
                oldVariant = old,
                newVariant = variant,
                oldItemId = oldItemId,
                itemId = itemId,
            });

            if (_host != null)
            {
                _host.RefreshVisual(slot, old, variant, itemId);
                _host.PlayEquipSFX(slot, itemId);
                _host.LogEquipEvent(slot, old, variant, itemId);
                _host.SaveEquipmentState(itemId, slot, variant);
                // Slot-specific dispatch
                switch (slot)
                {
                    case PlayerEquipSlot.Weapon:
                        _host.OnWeaponChanged(oldItemId, itemId, variant);
                        break;
                    case PlayerEquipSlot.Body:
                        _host.OnArmorChanged(old, variant, itemId);
                        break;
                    case PlayerEquipSlot.Head:
                        _host.OnHelmetChanged(old, variant, itemId);
                        break;
                    case PlayerEquipSlot.Mount:
                        _host.OnMountChanged(old, variant, itemId);
                        break;
                }
            }

            SubsystemLog.Info("Equipment",
                $"Equipped slot {slot}: variant {old} → {variant} (item={itemId})");
        }

        /// <summary>Gỡ trang bị một slot về default.</summary>
        public void Unequip(PlayerEquipSlot slot)
        {
            Equip(slot, DefaultVariant(slot), 0);
        }

        /// <summary>Map weapon type từ equipment.</summary>
        public PcWeaponType GetCurrentWeaponType()
        {
            return GetWeaponType(_currentWeaponItemId, GetVariant(PlayerEquipSlot.Weapon));
        }

        public static PcWeaponType WeaponVariantToType(int weaponVariant)
        {
            return weaponVariant switch
            {
                0 => PcWeaponType.EmptyHand,
                (>= 1 and <= 6) or (>= 19 and <= 22) => PcWeaponType.ShortWeapon,
                (>= 7 and <= 12) or (>= 23 and <= 26) => PcWeaponType.LongWeapon,
                (>= 13 and <= 18) or (>= 27 and <= 30) => PcWeaponType.DualWeapon,
                _ => PcWeaponType.EmptyHand,
            };
        }

        /// <summary>
        /// PC GameDataDef.h EQUIPDETAILTYPE + melee particular mapping.
        /// Melee particulars are sword, blade, staff, spear, dual hammer, dual blade.
        /// Range weapons use the hidden-weapon presentation bank.
        /// </summary>
        public static PcWeaponType PcItemTupleToWeaponType(int itemGenre, int detailType, int particularType)
        {
            if (itemGenre != 0)
                return PcWeaponType.EmptyHand;

            return detailType switch
            {
                0 when particularType is 0 or 1 => PcWeaponType.ShortWeapon,
                0 when particularType is 2 or 3 => PcWeaponType.LongWeapon,
                0 when particularType is 4 or 5 => PcWeaponType.DualWeapon,
                1 => PcWeaponType.HiddenWeapon,
                _ => PcWeaponType.EmptyHand,
            };
        }

        public static PcWeaponType GetWeaponType(int itemId, int variant)
        {
            if (itemId > 0)
            {
                var mgr = SandboxManager.Instance;
                if (mgr != null && mgr.ItemDb != null)
                {
                    var item = mgr.ItemDb.Resolve(itemId);
                    if (item != null)
                    {
                        // Mock items
                        if (item.itemId >= 1001 && item.itemId <= 1042)
                        {
                            return WeaponVariantToType(variant);
                        }

                        if (item.itemGenre == 0 && item.detailType is 0 or 1)
                        {
                            return PcItemTupleToWeaponType(item.itemGenre, item.detailType, item.particularType);
                        }
                    }
                }
            }

            // Fallback to variant-based check
            return WeaponVariantToType(variant);
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

        private int GetItemId(PlayerEquipSlot slot)
        {
            return _equippedItemIds.TryGetValue(slot, out int id) ? id : 0;
        }

        // ── Defaults ───────────────────────────────────────────────────────

        private int DefaultVariant(PlayerEquipSlot slot)
        {
            if (!IsFemale)
            {
                switch (slot)
                {
                    case PlayerEquipSlot.Body:
                        if (SandboxManager.Instance != null && SandboxManager.Instance.ItemDb != null)
                        {
                            var item = SandboxManager.Instance.ItemDb.Resolve(100061);
                            if (item != null)
                            {
                                return PlayerAppearanceMapper.MapBody(false, item.resId);
                            }
                        }
                        return 1;
                    case PlayerEquipSlot.Head:
                        if (SandboxManager.Instance != null && SandboxManager.Instance.ItemDb != null)
                        {
                            var item = SandboxManager.Instance.ItemDb.Resolve(200061);
                            if (item != null)
                            {
                                return PlayerAppearanceMapper.MapHead(false, item.resId);
                            }
                        }
                        return 28;
                    case PlayerEquipSlot.Hair:
                        return 19;
                    case PlayerEquipSlot.Mount:
                        return 19;
                    default:
                        return 0;
                }
            }
            else
            {
                switch (slot)
                {
                    case PlayerEquipSlot.Body:
                    case PlayerEquipSlot.Head:
                    case PlayerEquipSlot.Hair:
                        return 50;
                    case PlayerEquipSlot.Mount:
                        return 19;
                    default:
                        return 0;
                }
            }
        }

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
