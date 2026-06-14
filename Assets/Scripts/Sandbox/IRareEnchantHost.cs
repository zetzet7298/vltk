// -----------------------------------------------------------------------------
// VLTK Mobile — Runtime host for weapon rare-enchant side effects.
//
// PC source: settings/rare.txt 29 cols. Enchant writes item.magicId +
// item.magicLevel on the target weapon, and bumps a per-character magic-pool
// count when the resulting magic is "set-magic" (5th-magic combo). The host is
// the seam: this batch only models the decision + dispatch, host implementation
// lives in the inventory/character subsystem.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Decision result of resolving a rare-enchant attempt.
    /// </summary>
    public readonly struct PcRareEnchantResolve
    {
        /// <summary>True if the tier was found and weights allow this weapon/slot/elemental.</summary>
        public readonly bool Resolved;
        /// <summary>Tier row that matches the requested level (or null).</summary>
        public readonly PcRareEnchantEntry Tier;
        /// <summary>Final level used (after roll if requestedLevel was null).</summary>
        public readonly int Level;
        /// <summary>Sum of weapon-type weight (0 means this weapon type cannot roll).</summary>
        public readonly int WeaponTypeWeight;
        /// <summary>Sum of slot-type weight (0 means this slot cannot roll).</summary>
        public readonly int SlotTypeWeight;
        /// <summary>Sum of elemental weight (0 means this elemental cannot roll).</summary>
        public readonly int ElementalWeight;
        /// <summary>Vietnamese reason string (success path returns empty).</summary>
        public readonly string ReasonVi;

        public PcRareEnchantResolve(bool resolved, PcRareEnchantEntry tier, int level,
            int wType, int wSlot, int wElem, string reasonVi)
        {
            Resolved = resolved;
            Tier = tier;
            Level = level;
            WeaponTypeWeight = wType;
            SlotTypeWeight = wSlot;
            ElementalWeight = wElem;
            ReasonVi = reasonVi ?? string.Empty;
        }

        public override string ToString()
            => Resolved
                ? $"OK tier=magic:{Tier?.magicId}/lvl={Level} w=[t={WeaponTypeWeight},s={SlotTypeWeight},e={ElementalWeight}]"
                : $"DENY: {ReasonVi}";
    }

    /// <summary>
    /// Host seam for rare-enchant side effects (apply magic to weapon, log
    /// effect, update character magic-pool). PC source: server script
    /// itemexchange_setting/rare.txt consumers (server1/gateway/equivalent).
    /// </summary>
    public interface IRareEnchantHost
    {
        /// <summary>Read current magicId on the weapon (0 = none).</summary>
        int GetWeaponMagicId(string player, int itemIndex);

        /// <summary>Set weapon magicId + magicLevel atomically. Returns true if accepted.</summary>
        bool SetWeaponMagic(string player, int itemIndex, int magicId, int level);

        /// <summary>Bump per-character magic-pool (server-side pool for set-magic count).</summary>
        void IncrementMagicPool(string player, int magicId);
    }
}
