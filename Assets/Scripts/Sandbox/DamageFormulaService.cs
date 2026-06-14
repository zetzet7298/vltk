using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Damage type, mirroring PC DAMAGE_TYPE (KNpc.cpp CalcDamage lines 2125-2292).</summary>
    public enum DamageType
    {
        Physics = 0,
        Cold = 1,
        Fire = 2,
        Light = 3,
        Poison = 4,
        // PC source: KNpc.cpp:2285 `case damage_magic: nRes = 0;` — magic bypasses all armor pools
        // and has no resist mitigation.
        Magic = 5,
    }

    /// <summary>
    /// Attacker side of a damage calculation. Maps to PC `KNpc::CalcDamage(nAttacker, nMin, nMax, ...)`.
    /// </summary>
    public struct AttackerStats
    {
        public int minDamage;   // nMin
        public int maxDamage;   // nMax
        public DamageType type;
        public bool isMelee;
    }

    /// <summary>
    /// Defender side of a damage calculation. PC source: KNpc.cpp lines 2125-2315 CalcDamage.
    /// Each typed armor pool maps to `m_PhysicsArmor.nValue[0]`, `m_ColdArmor.nValue[0]`,
    /// `m_FireArmor.nValue[0]`, `m_LightArmor.nValue[0]`, `m_PoisonArmor.nValue[0]`.
    /// The legacy single `armor` field is preserved as an alias for `physicsArmor` so
    /// existing fixture-based tests (which use `Def(armor: 100)`) keep passing.
    /// </summary>
    public struct DefenderStats
    {
        // --- Typed resist + resistMax (PC `m_CurrentXxxResist` / `m_CurrentXxxResistMax`) ---
        public int resist;          // generic resist for the matching type (default)
        public int resistMax;       // generic resist cap for the matching type

        // --- Typed armor pools (PC: `m_XxxArmor.nValue[0]`) ---
        // Backward compat: `armor` stays as physics-armor alias. Setting it sets
        // physicsArmor too so old tests (Def(armor: 100)) still work.
        public int armor;
        public int physicsArmor;
        public int coldArmor;
        public int fireArmor;
        public int lightArmor;
        public int poisonArmor;

        // --- Mana shield (PC: `m_ManaShield.nValue[0]` percentage + `m_CurrentMana`) ---
        public int manaShield;      // percentage 0..100
        public int currentMana;

        // --- Damage return percent (PC: `m_CurrentMeleeDmgRet` / `m_CurrentRangeDmgRet`) ---
        // PC source: KNpc.cpp:2320-2330. Melee return = `m_CurrentMeleeDmgRet + nDamage * m_CurrentMeleeDmgRetPercent / 100`.
        // Range return = `m_CurrentRangeDmgRet + nDamage * m_CurrentRangeDmgRetPercent / 100`.
        public int meleeDmgRetPercent;
        public int rangeDmgRetPercent;

        // --- Damage2Mana (PC: `m_CurrentDamage2Mana` percentage of nDamage returned to mana) ---
        // PC source: KNpc.cpp:2345 `m_CurrentMana += m_CurrentDamage2Mana * nDamage / 100;`
        public int damage2ManaPercent;

        // --- PK damage rate (PC: `NpcSet.m_nPKDamageRate` percentage applied in PvP only) ---
        // PC source: KNpc.cpp:2337 `nDamage = nDamage * NpcSet.m_nPKDamageRate / 100;`
        public int pkDamageRatePercent;

        /// <summary>
        /// Returns the typed armor pool for a damage type. Magic returns 0 (PC: magic
        /// bypasses all armor pools; KNpc.cpp:2285 `case damage_magic: nRes = 0; break;`
        /// with no `m_XxxArmor.nValue[0] -= nDamage;` step).
        /// </summary>
        public readonly int GetTypedArmor(DamageType type)
        {
            return type switch
            {
                DamageType.Physics => physicsArmor > 0 ? physicsArmor : armor,
                DamageType.Cold => coldArmor,
                DamageType.Fire => fireArmor,
                DamageType.Light => lightArmor,
                DamageType.Poison => poisonArmor,
                DamageType.Magic => 0, // PC: magic bypasses armor entirely
                _ => armor,
            };
        }
    }

    /// <summary>Decomposed damage result for the GM preview.</summary>
    public struct DamageResult
    {
        public int rolledBase;       // base roll before mitigation
        public int afterArmor;       // after typed armor absorption
        public int manaAbsorbed;     // damage diverted to mana shield
        public int afterResist;      // after resist percentage
        public int finalDamage;      // final HP damage applied
        public int meleeReturnDamage; // damage returned to melee attacker (KNpc.cpp:2320-2325)
        public int rangeReturnDamage; // damage returned to range attacker (KNpc.cpp:2327-2330)
        public int damage2ManaGain;   // mana gained from damage taken (KNpc.cpp:2345)

        public override string ToString()
            => $"base={rolledBase} armor→{afterArmor} mana-{manaAbsorbed} resist→{afterResist} final={finalDamage} ret(M/R)={meleeReturnDamage}/{rangeReturnDamage} d2m={damage2ManaGain}";
    }

    /// <summary>
    /// M4.3 — Damage formula port from PC KNpc::CalcDamage (SwordOnline Core/Src).
    /// Pure C# (no MonoBehaviour) so it is fully EditMode-testable. The random roll
    /// is injected so the formula is deterministic for fixtures and the GM preview.
    ///
    /// PC source evidence (StreamingAssets/Reference/KNpc.cpp lines 2125-2352):
    ///   nDamageRange = nMax - nMin
    ///   if (nDamageRange < 0) nDamage = nMax + g_Random(-nDamageRange)
    ///   else nDamage = nMin + g_Random(nMax - nMin)
    ///   case damage_physics: armor = m_PhysicsArmor.nValue[0]; nRes = m_CurrentPhysicsResist;
    ///   case damage_cold:    armor = m_ColdArmor.nValue[0];    nRes = m_CurrentColdResist;
    ///   case damage_fire:    armor = m_FireArmor.nValue[0];    nRes = m_CurrentFireResist;
    ///   case damage_light:   armor = m_LightArmor.nValue[0];   nRes = m_CurrentLightResist;
    ///   case damage_poison:  armor = m_PoisonArmor.nValue[0];  nRes = m_CurrentPoisonResist;
    ///   case damage_magic:   nRes = 0; (no armor pool applied)
    ///   armor -= nDamage; if armor<0 { nDamage = -armor } else nDamage = 0
    ///   manaShield: nManaDamage = nDamage * manaShield/100; absorbed up to currentMana
    ///   resist:     nRes capped at resistMax then MAX_RESIST; nDamage = nDamage*(100-nRes)/100
    ///   meleeReturn: nMin = m_CurrentMeleeDmgRet + nDamage * nMax / 100 (bIsMelee)
    ///   rangeReturn: nMin = m_CurrentRangeDmgRet + nDamage * nMax / 100 (!bIsMelee)
    ///   PK rate:     nDamage = nDamage * NpcSet.m_nPKDamageRate / 100 (player vs player only)
    ///   damage2Mana: m_CurrentMana += m_CurrentDamage2Mana * nDamage / 100
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
        /// If true, treat the attacker and defender as both players so the PK damage
        /// rate (PC `NpcSet.m_nPKDamageRate`) is applied. Set false for PvE.
        /// PC source: KNpc.cpp:2336 `if (this->m_Kind == kind_player && Npc[nAttacker].m_Kind == kind_player)`.
        /// </summary>
        public bool IsPvp { get; set; } = false;

        /// <summary>
        /// Compute damage following the PC pipeline. <paramref name="rolledOverride"/>
        /// pins the base roll for fixture tests (skips the Roll provider).
        /// </summary>
        public DamageResult Compute(AttackerStats atk, DefenderStats def, int? rolledOverride = null)
        {
            var r = new DamageResult();

            int min = atk.minDamage;
            int max = atk.maxDamage;
            if (min + max <= 0) return r; // PC early-out (KNpc.cpp:2130)

            // 1) Base roll (PC: KNpc.cpp:2134-2141).
            //    if (nDamageRange < 0) nDamage = nMax + g_Random(-nDamageRange);
            //    else nDamage = nMin + g_Random(nMax - nMin);
            int dmg = rolledOverride ?? RollRangeSigned(min, max);
            r.rolledBase = dmg;

            // 2) Typed armor absorption (PC: KNpc.cpp:2145-2292 switch).
            //    Magic type skips armor entirely (KNpc.cpp:2285 `case damage_magic: nRes = 0; break;`).
            if (atk.type != DamageType.Magic)
            {
                int armor = def.GetTypedArmor(atk.type);
                if (armor > 0)
                {
                    armor -= dmg;
                    dmg = armor < 0 ? -armor : 0;
                }
            }
            r.afterArmor = dmg;

            // 3) Mana shield diverts a percentage to mana, up to currentMana
            //    (PC: KNpc.cpp:2298-2313).
            if (dmg > 0 && def.manaShield > 0 && def.currentMana > 0)
            {
                int manaDamage = dmg * def.manaShield / 100;
                int absorbed = Mathf.Min(manaDamage, def.currentMana);
                dmg -= absorbed;
                r.manaAbsorbed = absorbed;
            }

            // 4) Resist percentage, capped at resistMax then MAX_RESIST
            //    (PC: KNpc.cpp:2147-2153 + 2314 `nDamage = nDamage * (100 - nRes) / 100;`).
            //    Magic type: resist stays 0 (KNpc.cpp:2285-2290).
            int res = atk.type == DamageType.Magic ? 0 : ClampResist(def.resist, def.resistMax);
            dmg = dmg * (100 - res) / 100;
            r.afterResist = dmg;

            dmg = Mathf.Max(0, dmg);

            // 5) PK damage rate (PC: KNpc.cpp:2336-2337). Applied only in PvP.
            if (IsPvp && def.pkDamageRatePercent > 0)
                dmg = dmg * def.pkDamageRatePercent / 100;

            r.finalDamage = dmg;

            // 6) Damage return to attacker (PC: KNpc.cpp:2318-2333).
            //    Melee: nMin = m_CurrentMeleeDmgRet + nDamage * nMax / 100 (where nMax was
            //    reassigned to m_CurrentMeleeDmgRetPercent during the type switch).
            //    Range: nMin = m_CurrentRangeDmgRet + nDamage * nMax / 100.
            //    We model only the percent term (the absolute m_Current*Ret baseline is
            //    captured into meleeDmgRetPercent/rangeDmgRetPercent as the percent field;
            //    a separate baseline field can be added later if needed).
            if (dmg > 0)
            {
                if (atk.isMelee && def.meleeDmgRetPercent > 0)
                    r.meleeReturnDamage = dmg * def.meleeDmgRetPercent / 100;
                else if (!atk.isMelee && def.rangeDmgRetPercent > 0)
                    r.rangeReturnDamage = dmg * def.rangeDmgRetPercent / 100;
            }

            // 7) Damage2Mana (PC: KNpc.cpp:2345). Defender gains mana from damage taken.
            if (dmg > 0 && def.damage2ManaPercent > 0)
                r.damage2ManaGain = dmg * def.damage2ManaPercent / 100;

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

        /// <summary>
        /// PC signed-random: when (max - min) &lt; 0, returns nMax + Random(-range);
        /// otherwise returns nMin + Random(max - min). The legacy (max &lt;= min)
        /// short-circuit returns min so single-value fixtures stay deterministic.
        /// PC source: KNpc.cpp:2136-2141.
        /// </summary>
        private int RollRangeSigned(int min, int max)
        {
            int range = max - min;
            if (range < 0)
            {
                // PC: nDamage = nMax + g_Random(-nDamageRange);
                int v = Roll != null ? Roll(max, max + (-range)) : max + (-range) / 2;
                return Mathf.Clamp(v, max, max + (-range));
            }
            if (range == 0) return min;
            int vv = Roll != null ? Roll(min, max) : min + range / 2;
            return Mathf.Clamp(vv, min, max);
        }
    }
}
