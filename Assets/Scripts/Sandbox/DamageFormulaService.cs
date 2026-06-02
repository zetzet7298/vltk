using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Damage type, mirroring PC DAMAGE_TYPE (KNpc.cpp CalcDamage).</summary>
    public enum DamageType
    {
        Physics = 0,
        Cold = 1,
        Fire = 2,
        Light = 3,
        Poison = 4,
    }

    /// <summary>Attacker side of a damage calculation.</summary>
    public struct AttackerStats
    {
        public int minDamage;   // nMin
        public int maxDamage;   // nMax
        public DamageType type;
        public bool isMelee;
    }

    /// <summary>Defender side of a damage calculation.</summary>
    public struct DefenderStats
    {
        public int armor;        // m_*Armor.nValue[0] for the matching type (absorb pool)
        public int resist;       // m_Current*Resist (percentage 0..100)
        public int resistMax;    // m_Current*ResistMax cap
        public int manaShield;   // m_ManaShield.nValue[0] percentage 0..100
        public int currentMana;  // m_CurrentMana
    }

    /// <summary>Decomposed damage result for the GM preview.</summary>
    public struct DamageResult
    {
        public int rolledBase;       // base roll before mitigation
        public int afterArmor;       // after armor absorption
        public int manaAbsorbed;     // damage diverted to mana shield
        public int afterResist;      // after resist percentage
        public int finalDamage;      // final HP damage applied

        public override string ToString()
            => $"base={rolledBase} armor→{afterArmor} mana-{manaAbsorbed} resist→{afterResist} final={finalDamage}";
    }

    /// <summary>
    /// M4.3 — Damage formula port from PC KNpc::CalcDamage (SwordOnline Core/Src).
    /// Pure C# (no MonoBehaviour) so it is fully EditMode-testable. The random roll
    /// is injected so the formula is deterministic for fixtures and the GM preview.
    ///
    /// PC source evidence (StreamingAssets/Reference/KNpc.cpp CalcDamage):
    ///   nDamage   = nMin + rand(nMax - nMin)
    ///   armor:      armor -= nDamage; if armor<0 { nDamage = -armor } else nDamage = 0
    ///   manaShield: nManaDamage = nDamage * manaShield/100; absorbed up to currentMana
    ///   resist:     nRes capped at resistMax then MAX_RESIST; nDamage = nDamage*(100-nRes)/100
    /// </summary>
    public class DamageFormulaService
    {
        /// <summary>
        /// PC constant: maximum effective resistance percentage.
        /// Source: PC GameDataDef.h:128 `#define MAX_RESIST 95`.
        /// </summary>
        public const int MaxResist = 95;

        /// <summary>
        /// Random roll provider: given (min, max) returns a value in [min, max].
        /// Injected for determinism; defaults to the midpoint so previews are stable.
        /// </summary>
        public Func<int, int, int> Roll { get; set; } = (min, max) => min + (max - min) / 2;

        /// <summary>
        /// Compute damage following the PC pipeline. <paramref name="rolledOverride"/>
        /// pins the base roll for fixture tests (skips the Roll provider).
        /// </summary>
        public DamageResult Compute(AttackerStats atk, DefenderStats def, int? rolledOverride = null)
        {
            var r = new DamageResult();

            int min = atk.minDamage;
            int max = atk.maxDamage;
            if (min + max <= 0) return r; // PC early-out

            // 1) Base roll.
            int dmg = rolledOverride ?? RollRange(min, max);
            r.rolledBase = dmg;

            // 2) Armor absorption (typed armor pool).
            int armor = def.armor;
            if (armor > 0)
            {
                armor -= dmg;
                dmg = armor < 0 ? -armor : 0;
            }
            r.afterArmor = dmg;

            // 3) Mana shield diverts a percentage to mana, up to currentMana.
            if (dmg > 0 && def.manaShield > 0 && def.currentMana > 0)
            {
                int manaDamage = dmg * def.manaShield / 100;
                int absorbed = Mathf.Min(manaDamage, def.currentMana);
                dmg -= absorbed;
                r.manaAbsorbed = absorbed;
            }

            // 4) Resist percentage, capped at resistMax then MAX_RESIST.
            int res = ClampResist(def.resist, def.resistMax);
            dmg = dmg * (100 - res) / 100;
            r.afterResist = dmg;

            r.finalDamage = Mathf.Max(0, dmg);
            return r;
        }

        /// <summary>AC#2 — GM stat preview: deterministic final damage for a stat edit.</summary>
        public int PreviewDamage(AttackerStats atk, DefenderStats def)
            => Compute(atk, def).finalDamage;

        private int ClampResist(int resist, int resistMax)
        {
            int nRes = resist;
            if (resistMax > 0 && nRes > resistMax) nRes = resistMax;
            if (nRes > MaxResist) nRes = MaxResist;
            if (nRes < 0) nRes = 0;
            return nRes;
        }

        private int RollRange(int min, int max)
        {
            if (max <= min) return min;
            int v = Roll != null ? Roll(min, max) : min + (max - min) / 2;
            return Mathf.Clamp(v, min, max);
        }
    }
}
