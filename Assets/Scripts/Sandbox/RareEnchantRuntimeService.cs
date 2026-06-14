// -----------------------------------------------------------------------------
// VLTK Mobile — Runtime service cho rare enchant (PC rare.txt tier resolution +
// level roll + host dispatch).
//
// PC source: settings/rare.txt = itemexchange_setting/rare.txt (byte-identical).
// 29 cols. Each row = one tier of one MAGIC_ID. The tier specifies:
//   - level range [magP1Min, magP1Max]
//   - weapon-type weight (SWORD..CROSSBOW, 9 weights)
//   - slot-type weight (ARMOR..PENDANT, 10 weights)
//   - elemental weight (METAL..EARTH, 5 weights)
//
// Resolution: when a player enchants a weapon with magicId, all tiers for that
// magicId are scanned. The tier whose [magP1Min, magP1Max] contains the
// requested (or rolled) level is selected. If multiple tiers match, the one
// with the highest level is preferred (PC: highest tier "wins" because the
// server re-rolls on upgrade). If no tier matches, the request is denied.
// Weights must all be > 0 for the resolve to succeed (PC: weight 0 means
// the magic cannot be rolled on that weapon/slot/elemental).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Weapon type enum mapping to rare.txt columns 4..12.
    /// Index into PcRareEnchantEntry fields is WeaponTypes[weaponType].
    /// </summary>
    public enum RareWeaponType
    {
        Sword = 0,
        Blade = 1,
        Wand = 2,
        Spear = 3,
        Hammer = 4,
        DualBlades = 5,
        Darts = 6,
        Knife = 7,
        Crossbow = 8,
    }

    /// <summary>
    /// Slot type enum mapping to rare.txt columns 13..22.
    /// </summary>
    public enum RareSlotType
    {
        Armor = 0,
        Ring = 1,
        Necklace = 2,
        Amulet = 3,
        Boot = 4,
        Belt = 5,
        Helm = 6,
        Cuff = 7,
        Sachet = 8,
        Pendant = 9,
    }

    /// <summary>
    /// Elemental (ngũ hành) mapping to rare.txt columns 23..27.
    /// </summary>
    public enum RareElemental
    {
        Metal = 0,
        Wood = 1,
        Water = 2,
        Fire = 3,
        Earth = 4,
    }

    /// <summary>
    /// Result of ApplyEnchant: tier resolution + host-side effect summary.
    /// </summary>
    public readonly struct PcRareEnchantApplyOutcome
    {
        public readonly bool Applied;
        public readonly PcRareEnchantEntry Tier;
        public readonly int Level;
        public readonly bool MagicWritten;
        public readonly bool PoolBumped;
        public readonly string ReasonVi;

        public PcRareEnchantApplyOutcome(bool applied, PcRareEnchantEntry tier, int level,
            bool magicWritten, bool poolBumped, string reasonVi)
        {
            Applied = applied;
            Tier = tier;
            Level = level;
            MagicWritten = magicWritten;
            PoolBumped = poolBumped;
            ReasonVi = reasonVi ?? string.Empty;
        }

        public override string ToString()
            => Applied
                ? $"APPLIED magic={Tier?.magicId} lvl={Level} magicWritten={MagicWritten} poolBumped={PoolBumped}"
                : $"DENIED: {ReasonVi}";
    }

    public class RareEnchantRuntimeService
    {
        public const string LogTag = "RareEnchantRuntime";

        private readonly RareEnchantService _service;
        private readonly IRareEnchantHost _host;
        private readonly System.Random _rng;

        public RareEnchantRuntimeService(
            RareEnchantService service,
            IRareEnchantHost host,
            int? seed = null)
        {
            _service = service;
            _host = host;
            _rng = seed.HasValue ? new System.Random(seed.Value) : null;
        }

        /// <summary>Resolve the tier for a (magicId, level) and validate weights.</summary>
        public PcRareEnchantResolve ResolveTier(
            int magicId,
            int weaponType,
            int slotType,
            int elemental,
            int? requestedLevel = null)
        {
            if (_service == null)
                return new PcRareEnchantResolve(false, null, 0, 0, 0, 0, "NoService");

            var tiers = _service.GetByMagicId(magicId);
            if (tiers == null || tiers.Count == 0)
                return new PcRareEnchantResolve(false, null, 0, 0, 0, 0, "UnknownMagicId");

            int rolledLevel = requestedLevel ?? RollLevelInTiers(tiers);

            // PC: pick the highest-level tier that contains the rolled level.
            PcRareEnchantEntry matched = null;
            int matchedMin = int.MinValue;
            foreach (var t in tiers)
            {
                if (rolledLevel < t.magP1Min || rolledLevel > t.magP1Max) continue;
                if (t.magP1Min > matchedMin)
                {
                    matched = t;
                    matchedMin = t.magP1Min;
                }
            }
            if (matched == null)
                return new PcRareEnchantResolve(false, null, rolledLevel, 0, 0, 0, "LevelOutOfAllTiers");

            int wType = GetWeaponTypeWeight(matched, weaponType);
            int wSlot = GetSlotTypeWeight(matched, slotType);
            int wElem = GetElementalWeight(matched, elemental);

            if (wType == 0)
                return new PcRareEnchantResolve(false, matched, rolledLevel, wType, wSlot, wElem, "WeaponTypeWeightZero");
            if (wSlot == 0)
                return new PcRareEnchantResolve(false, matched, rolledLevel, wType, wSlot, wElem, "SlotTypeWeightZero");
            if (wElem == 0)
                return new PcRareEnchantResolve(false, matched, rolledLevel, wType, wSlot, wElem, "ElementalWeightZero");

            return new PcRareEnchantResolve(true, matched, rolledLevel, wType, wSlot, wElem, string.Empty);
        }

        /// <summary>
        /// Apply an enchant end-to-end: resolve the tier, then call the host
        /// to write the magic to the weapon and bump the magic-pool.
        /// </summary>
        public PcRareEnchantApplyOutcome ApplyEnchant(
            string player,
            int itemIndex,
            int magicId,
            RareWeaponType weaponType,
            RareSlotType slotType,
            RareElemental elemental,
            int? requestedLevel = null)
        {
            var r = ResolveTier((int)magicId, (int)weaponType, (int)slotType, (int)elemental, requestedLevel);
            if (!r.Resolved)
                return new PcRareEnchantApplyOutcome(false, r.Tier, r.Level, false, false, r.ReasonVi);

            if (_host == null)
                return new PcRareEnchantApplyOutcome(false, r.Tier, r.Level, false, false, "NoHost");

            int currentMagic = _host.GetWeaponMagicId(player, itemIndex);
            if (currentMagic != 0 && currentMagic != magicId)
            {
                return new PcRareEnchantApplyOutcome(false, r.Tier, r.Level, false, false,
                    "WeaponHasDifferentMagic");
            }

            bool written = _host.SetWeaponMagic(player, itemIndex, magicId, r.Level);
            if (!written)
                return new PcRareEnchantApplyOutcome(false, r.Tier, r.Level, false, false, "HostSetFailed");

            _host.IncrementMagicPool(player, magicId);

            return new PcRareEnchantApplyOutcome(true, r.Tier, r.Level, true, true, string.Empty);
        }

        /// <summary>
        /// Roll a level across the [min(magP1Min), max(magP1Max)] of all tiers
        /// for this magicId. PC: when no level is specified, the engine picks a
        /// uniform level within the union of tier ranges.
        /// </summary>
        public int RollLevelInTiers(List<PcRareEnchantEntry> tiers)
        {
            if (tiers == null || tiers.Count == 0) return 0;
            int min = int.MaxValue, max = int.MinValue;
            foreach (var t in tiers)
            {
                if (t.magP1Min < min) min = t.magP1Min;
                if (t.magP1Max > max) max = t.magP1Max;
            }
            if (min > max) return 0;
            int range = max - min + 1;
            if (_rng != null) return min + _rng.Next(range);
            // Deterministic fallback for test parity: mid-point of range.
            return min + range / 2;
        }

        public static int GetWeaponTypeWeight(PcRareEnchantEntry e, int weaponType)
        {
            if (e == null) return 0;
            switch ((RareWeaponType)weaponType)
            {
                case RareWeaponType.Sword: return e.wSword;
                case RareWeaponType.Blade: return e.wBlade;
                case RareWeaponType.Wand: return e.wWand;
                case RareWeaponType.Spear: return e.wSpear;
                case RareWeaponType.Hammer: return e.wHammer;
                case RareWeaponType.DualBlades: return e.wDualBlades;
                case RareWeaponType.Darts: return e.wDarts;
                case RareWeaponType.Knife: return e.wKnife;
                case RareWeaponType.Crossbow: return e.wCrossbow;
                default: return 0;
            }
        }

        public static int GetSlotTypeWeight(PcRareEnchantEntry e, int slotType)
        {
            if (e == null) return 0;
            switch ((RareSlotType)slotType)
            {
                case RareSlotType.Armor: return e.wArmor;
                case RareSlotType.Ring: return e.wRing;
                case RareSlotType.Necklace: return e.wNecklace;
                case RareSlotType.Amulet: return e.wAmulet;
                case RareSlotType.Boot: return e.wBoot;
                case RareSlotType.Belt: return e.wBelt;
                case RareSlotType.Helm: return e.wHelm;
                case RareSlotType.Cuff: return e.wCuff;
                case RareSlotType.Sachet: return e.wSachet;
                case RareSlotType.Pendant: return e.wPendant;
                default: return 0;
            }
        }

        public static int GetElementalWeight(PcRareEnchantEntry e, int elemental)
        {
            if (e == null) return 0;
            switch ((RareElemental)elemental)
            {
                case RareElemental.Metal: return e.wMetal;
                case RareElemental.Wood: return e.wWood;
                case RareElemental.Water: return e.wWater;
                case RareElemental.Fire: return e.wFire;
                case RareElemental.Earth: return e.wEarth;
                default: return 0;
            }
        }
    }
}
