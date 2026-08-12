// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.3 Damage Formula Port
// Sách tham khảo: KNpc::CalcDamage (SwordOnline Core/Src/KNpc.cpp)
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Damage type, mirroring PC DAMAGE_TYPE (KNpc.cpp CalcDamage switch 2500-2580).</summary>
    public enum DamageType
    {
        Physics = 0,
        Cold = 1,
        Fire = 2,
        Light = 3,
        Poison = 4,
        // PC source: KNpc.cpp:2564 `case damage_magic: nRes = 0;` — magic bypasses all armor pools
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

        // --- Ngũ hành (PC: Npc[nAttacker].m_Series + m_CurrentFiveElementsEnhance) ---
        public Series series;
        public int fiveElementsEnhance;   // m_CurrentFiveElementsEnhance
        public int fiveElementsDamageP;   // nFiveElements_DamageP (magic_seriesdamage_p)

        // --- Crit (PC: bIsDS deadly strike / bIsFS fatally strike) ---
        // NOTE: PC KNpc.cpp:2470-2479 các multiplier này nằm trong nhánh `if(bReturn)`
        // mà mọi call-site thực tế đều truyền bReturn=FALSE → không thay đổi damage số.
        // Ta vẫn roll + track để hiển thị highlight chí mạng (visual), không thay finalDamage.
        public bool isDeadlyStrike;   // bIsDS
        public bool isFatallyStrike;  // bIsFS

        // --- Hút máu/nội/thể (PC: nStolen_Life / nStolen_Mana / nStolen_Stamina) ---
        public int stolenLifePercent;
        public int stolenManaPercent;
        public int stolenStaminaPercent;

        // --- Tỷ lệ damage PvP/PvE đặc biệt (PC: NpcSet.m_nPKDamageRate, m_nNpcSpecialDamageRate) ---
        // 100 = không đổi (MAX_PERCENT). Chỉ apply trong điều kiện tương ứng (caller quyết định).
        public int pkDamageRatePercent;       // PvP
        public int npcSpecialDamageRatePercent; // quái đặc biệt (boss/elite)
    }

    /// <summary>
    /// Defender side of a damage calculation. PC source: KNpc.cpp CalcDamage switch 2500-2580.
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

        // --- Ngũ hành (PC: m_Series + m_CurrentFiveElementsResist) ---
        public Series series;
        public int fiveElementsResist;   // m_CurrentFiveElementsResist
        public int fatallyStrikeResP;    // m_CurrentFatallyStrikeResP (giảm chance chí mạng)

        // --- Mana shield (PC: `m_ManaShield.nValue[0]` percentage + `m_CurrentMana`) ---
        public int manaShield;      // percentage 0..100
        public int currentMana;
        public int currentManaShield; // PC m_CurrentManaShield (flat subtract)

        // --- Damage return percent (PC: `m_CurrentMeleeDmgRet` / `m_CurrentRangeDmgRet`) ---
        // PC source: KNpc.cpp:2660-2679. Melee return = `m_CurrentMeleeDmgRet + nDamage * m_CurrentMeleeDmgRetPercent / 100`.
        // Range return = `m_CurrentRangeDmgRet + nDamage * m_CurrentRangeDmgRetPercent / 100`.
        public int meleeDmgRet;            // base flat melee return
        public int meleeDmgRetPercent;     // percent of nDamage
        public int rangeDmgRet;            // base flat range return
        public int rangeDmgRetPercent;     // percent of nDamage

        // --- Damage2Mana (PC: `m_CurrentDamage2Mana` percentage of nDamage returned to mana) ---
        // PC source: KNpc.cpp:2704 `m_CurrentMana += m_CurrentDamage2Mana * nDamage / 100;`
        public int damage2ManaPercent;

        // --- PK damage rate (PC: `NpcSet.m_nPKDamageRate` percentage applied in PvP only) ---
        // PC source: KNpc.cpp:2592 `nDamage = nDamage * NpcSet.m_nPKDamageRate / 100;`
        public int pkDamageRatePercent;

        /// <summary>
        /// Returns the typed armor pool for a damage type. Magic returns 0 (PC: magic
        /// bypasses all armor pools; KNpc.cpp:2564 `case damage_magic: nRes = 0; break;`
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
        public bool hit;
        public bool isCrit;
        public DamageType type;
        public int rolledBase;       // base roll before mitigation
        public int afterArmor;       // after typed armor absorption
        public int manaAbsorbed;     // damage diverted to mana shield
        public int afterResist;      // after resist percentage
        public int finalDamage;      // final HP damage applied
        public int meleeReturnDamage; // damage returned to melee attacker (KNpc.cpp:2320-2325)
        public int rangeReturnDamage; // damage returned to range attacker (KNpc.cpp:2327-2330)
        public int damage2ManaGain;   // mana gained from damage taken (KNpc.cpp:2345)
        public int stolenLife;
        public int stolenMana;
        public int stolenStamina;

        public override string ToString()
            => $"base={rolledBase} armor→{afterArmor} mana-{manaAbsorbed} resist→{afterResist} final={finalDamage} ret(M/R)={meleeReturnDamage}/{rangeReturnDamage} d2m={damage2ManaGain}";
    }

    /// <summary>
    /// M4.3 — Damage formula port from PC KNpc::CalcDamage (SwordOnline Core/Src).
    /// Pure C# (no MonoBehaviour) so it is fully EditMode-testable. The random roll
    /// is injected so the formula is deterministic for fixtures and the GM preview.
    /// </summary>
    public class DamageFormulaService
    {
        /// <summary>
        /// PC constant: maximum effective resistance percentage.
        /// Source: PC GameDataDef.h:128 `#define MAX_RESIST 95`.
        /// </summary>
        public const int MaxResist = 95;

        public const int MaxPercent = 100;
        public const int MaxPercentEnhance = 100;
        public const int MaxDeadlyStrikeEnhanceP = 200;

        /// <summary>
        /// Random roll provider: given (min, max) returns a value in [min, max].
        /// Injected for determinism; defaults to the midpoint so previews are stable.
        /// </summary>
        public Func<int, int, int> Roll { get; set; } = (min, max) => min + (max - min) / 2;

        /// <summary>
        /// Random roll provider for percent checks. Given a percent N, returns TRUE with
        /// probability N/100. PC source: g_RandPercent (KMath.h:252) —
        /// `if ((int)g_Random(100) &lt; nPercent) return TRUE;` → rolls 0..99, true if &lt; pct.
        /// Default uses UnityEngine.Random for production; tests inject deterministic roll.
        /// </summary>
        public Func<int, bool> RollPercent { get; set; } = (pct) => UnityEngine.Random.Range(0, 100) < pct;

        /// <summary>
        /// If true, treat the attacker and defender as both players so the PK damage
        /// rate (PC `NpcSet.m_nPKDamageRate`) is applied. Set false for PvE.
        /// PC source: KNpc.cpp:2336 `if (this->m_Kind == kind_player && Npc[nAttacker].m_Kind == kind_player)`.
        /// </summary>
        public bool IsPvp { get; set; } = false;

        /// <summary>
        /// Hit/Miss check — port 100% from PC KNpc::CheckHitTarget (KNpc.cpp:5831-5854).
        /// PC source:
        ///   if (nAR &lt; 0) return FALSE;
        ///   if (nDf &lt; 0) nPercent = MAX_HIT_PERCENT;       // 95
        ///   else if ((nAR + nDefense) == 0) nPercent = 50;  // 50/50
        ///   else nPercent = nAR * 100 / (nAR + nDefense);
        ///   if (nPercent &gt; MAX_HIT_PERCENT + 4) nPercent = MAX_HIT_PERCENT;  // cap 99→95
        ///   if (nPercent &lt; 40) nPercent = 40;               // MINIMUM 40% hit chance!
        ///   return g_RandPercent(nPercent);
        /// </summary>
        public bool CheckHitTarget(int attackRating, int defend, int ignore = 0)
        {
            const int MaxHitPercent = 95;  // PC: MAX_HIT_PERCENT (GameDataDef.h:386)

            // PC: if (nIngore < MAX_PERCENT) nDefense = nDf * (MAX_PERCENT - nIngore) / MAX_PERCENT;
            int nDefense = 0;
            if (ignore < MaxPercent)
                nDefense = defend * (MaxPercent - ignore) / MaxPercent;

            // PC: if (nAR < 0) return FALSE;
            if (attackRating < 0) return false;

            int nPercent;
            // PC: if (nDf < 0) nPercent = MAX_HIT_PERCENT;
            if (defend < 0)
                nPercent = MaxHitPercent;
            // PC: else if ((nAR + nDefense) == 0) nPercent = 50;
            else if (attackRating + nDefense == 0)
                nPercent = 50;
            // PC: else nPercent = nAR * MAX_PERCENT / (nAR + nDefense);
            else
                nPercent = attackRating * MaxPercent / (attackRating + nDefense);

            // PC: if (nPercent > MAX_HIT_PERCENT + 4) nPercent = MAX_HIT_PERCENT;
            if (nPercent > MaxHitPercent + 4)
                nPercent = MaxHitPercent;

            // PC: if (nPercent < 40) nPercent = 40;  ← MINIMUM 40% HIT CHANCE!
            if (nPercent < 40)
                nPercent = 40;

            // PC: return g_RandPercent(nPercent);
            return RollPercent != null ? RollPercent(nPercent) : UnityEngine.Random.Range(0, 100) < nPercent;
        }

        public DamageResult Compute(AttackerStats atk, DefenderStats def, int? rolledOverride = null)
        {
            return CalcDamage(atk, def, rolledOverride, false);
        }

        /// <summary>AC#2 — GM stat preview: deterministic final damage for a stat edit.</summary>
        public int PreviewDamage(AttackerStats atk, DefenderStats def)
            => Compute(atk, def).finalDamage;

        /// <summary>
        /// Damage formula calculation. If bReturn is true, we compute deadly/fatally strike
        /// (reflection) -> according to PC, this applies crit multiplier + npc special rate.
        /// </summary>
        public DamageResult CalcDamage(AttackerStats atk, DefenderStats def, int? rolledOverride, bool bReturn)
        {
            var r = new DamageResult { hit = true, isCrit = atk.isDeadlyStrike, type = atk.type };

            // PC: if (m_Series == series_minus) nDamage = 1;
            if (def.series == Series.Minus)
            {
                r.rolledBase = 1;
                r.afterArmor = 1;
                r.afterResist = 1;
                r.finalDamage = 1;
                return r;
            }

            int min = atk.minDamage;
            int max = atk.maxDamage;
            // PC: if (nMin + nMax <= 0) return FALSE; (KNpc.cpp:2455)
            if (min + max <= 0)
            {
                r.hit = false;
                return r;
            }

            int nRes = 0;
            int nDamageRange = max - min;

            // 1) Base roll (PC: KNpc.cpp:2466-2472).
            int dmg = rolledOverride ?? RollRangeSigned(min, max);
            r.rolledBase = dmg;

            // 2) Crit multiplier + npc special (PC nhánh if(bReturn), KNpc.cpp:2470-2489).
            //    PC truyền bReturn=FALSE ở mọi call-site thực -> branch này dead code
            //    trong source gốc -> crit KHÔNG nhân damage. Ta giữ faithful: chỉ apply
            //    khi bReturn=true (đường reflection). isCrit vẫn track cho visual highlight.
            if (bReturn)
            {
                if (atk.isDeadlyStrike)
                    dmg = dmg * MaxDeadlyStrikeEnhanceP / MaxPercent; // x2
                if (atk.isFatallyStrike)
                    dmg = def.currentMana; // placeholder; PC dùng m_CurrentLife * Random(30,50)/100
                // NPC special damage rate (boss/elite)
                if (atk.npcSpecialDamageRatePercent > 0 && atk.npcSpecialDamageRatePercent != MaxPercent)
                    dmg = dmg * atk.npcSpecialDamageRatePercent / MaxPercent;
            }

            // 3) Typed armor absorption + set return percent (PC switch 2500-2580).
            //    PC reassigns nMax thành m_CurrentMeleeDmgRetPercent / RangeDmgRetPercent.
            int nMaxReturn = 0;
            if (atk.type != DamageType.Magic)
            {
                int armor = def.GetTypedArmor(atk.type);
                if (armor > 0)
                {
                    armor -= dmg;
                    dmg = armor < 0 ? -armor : 0;
                }
                // PC KNpc.cpp:2516-2580 — mỗi case gán nRes = m_CurrentXxxResist
                // rồi if (nRes > m_CurrentXxxResistMax) nRes = m_CurrentXxxResistMax;
                // Ta dùng ClampResist(resist, resistMax) để gộp: cap tại resistMax,
                // sau đó cap toàn cục MAX_RESIST ở bước 6.
                nRes = ClampResist(def.resist, def.resistMax);
                nMaxReturn = atk.isMelee ? def.meleeDmgRetPercent : def.rangeDmgRetPercent;
            }
            // Magic: nRes = 0, no armor (PC: case damage_magic: nRes = 0; break;)
            r.afterArmor = dmg;

            // 4) PK damage rate (PC: KNpc.cpp:2586-2592). Player vs player only.
            //    if (this->m_Kind == kind_player && Npc[nAttacker].m_Kind == kind_player)
            //        nDamage = nDamage * NpcSet.m_nPKDamageRate / 100;
            if (IsPvp && (def.pkDamageRatePercent > 0 || atk.pkDamageRatePercent > 0))
            {
                int pkRate = atk.pkDamageRatePercent > 0 ? atk.pkDamageRatePercent : def.pkDamageRatePercent;
                if (pkRate != MaxPercent)
                    dmg = dmg * pkRate / MaxPercent;
            }

            // PC: if (nDamage <= 0) return FALSE;
            if (dmg <= 0)
            {
                r.hit = false;
                r.afterResist = 0;
                r.finalDamage = 0;
                return r;
            }

            // 5) Ngũ hành tương sinh/khắc (PC: KNpc.cpp:2601-2622).
            //    Khắc (attacker khắc defender): nRes -= dmgP; nDamage -= (atkEnhance - defResist)
            //    Sinh (defender sinh attacker): nRes += dmgP; nDamage -= (defResist - atkEnhance)
            ApplyFiveElements(ref dmg, ref nRes, atk, def);

            // 6) Resist cap (PC: KNpc.cpp:2619 `if (nRes > MAX_RESIST) nRes = MAX_RESIST;`)
            //    PC KHÔNG clamp lower bound -> nRes ÂM (do ngũ hành khắc) làm TĂNG damage
            //    qua `nDamage -= nDamage * nRes / 100` (âm nhân dương = trừ thêm).
            if (nRes > MaxResist) nRes = MaxResist;

            // 7) Mana shield percentage (PC: KNpc.cpp:2624-2640).
            if (def.manaShield > 0 && def.currentMana > 0)
            {
                int nManaDamage = dmg * def.manaShield / MaxPercent;
                if (nManaDamage > def.currentMana)
                {
                    dmg -= def.currentMana;
                    r.manaAbsorbed = def.currentMana;
                }
                else
                {
                    dmg -= nManaDamage;
                    r.manaAbsorbed = nManaDamage;
                }
            }

            // 8) Flat mana shield (PC: if (m_CurrentManaShield > 0) nDamage -= m_CurrentManaShield;)
            if (def.currentManaShield > 0)
                dmg -= def.currentManaShield;

            // 9) Resist percentage (PC: nDamage -= nDamage * nRes / MAX_PERCENT;)
            dmg -= dmg * nRes / MaxPercent;
            r.afterResist = dmg;

            dmg = Mathf.Max(0, dmg);
            if (dmg <= 0)
            {
                r.finalDamage = 0;
                return r;
            }

            // 10) Damage return to attacker (PC: KNpc.cpp:2648-2679).
            //     Chỉ reflect khi KHÔNG phải return-damage (tránh đệ quy vô hạn).
            if (!bReturn)
            {
                if (atk.isMelee && (def.meleeDmgRet > 0 || nMaxReturn > 0))
                {
                    int ret = def.meleeDmgRet + dmg * nMaxReturn / MaxPercent;
                    r.meleeReturnDamage = Mathf.Max(0, ret);
                }
                else if (!atk.isMelee && (def.rangeDmgRet > 0 || nMaxReturn > 0))
                {
                    int ret = def.rangeDmgRet + dmg * nMaxReturn / MaxPercent;
                    r.rangeReturnDamage = Mathf.Max(0, ret);
                }
            }

            // 11) Damage2Mana (PC: KNpc.cpp:2704). Defender gains mana from damage taken.
            if (def.damage2ManaPercent > 0)
                r.damage2ManaGain = dmg * def.damage2ManaPercent / MaxPercent;

            // 12) Steal life/mana/stamina (PC: KNpc.cpp:2692-2700).
            if (atk.stolenLifePercent > 0) r.stolenLife = dmg * atk.stolenLifePercent / MaxPercent;
            if (atk.stolenManaPercent > 0) r.stolenMana = dmg * atk.stolenManaPercent / MaxPercent;
            if (atk.stolenStaminaPercent > 0) r.stolenStamina = dmg * atk.stolenStaminaPercent / MaxPercent;

            r.finalDamage = dmg;
            return r;
        }

        private static void ApplyFiveElements(ref int dmg, ref int nRes, AttackerStats atk, DefenderStats def)
        {
            if (atk.series <= Series.Nil || def.series <= Series.Nil) return;

            int s = (int)atk.series;
            int d = (int)def.series;
            int dmgP = atk.fiveElementsDamageP;

            bool overcomes =
                (atk.series == Series.Metal && def.series == Series.Wood) ||
                (atk.series == Series.Water && def.series == Series.Fire) ||
                (atk.series == Series.Wood && def.series == Series.Earth) ||
                (atk.series == Series.Fire && def.series == Series.Metal) ||
                (atk.series == Series.Earth && def.series == Series.Water);

            bool generates =
                (atk.series == Series.Metal && def.series == Series.Fire) ||
                (atk.series == Series.Water && def.series == Series.Earth) ||
                (atk.series == Series.Wood && def.series == Series.Metal) ||
                (atk.series == Series.Fire && def.series == Series.Water) ||
                (atk.series == Series.Earth && def.series == Series.Wood);

            if (overcomes)
            {
                nRes -= dmgP;
                dmg -= (atk.fiveElementsEnhance - def.fiveElementsResist);
            }
            else if (generates)
            {
                nRes += dmgP;
                dmg -= (def.fiveElementsResist - atk.fiveElementsEnhance);
            }
            _ = s; _ = d;
        }

        private int ClampResist(int resist, int resistMax)
        {
            int nRes = resist;
            if (resistMax > 0 && nRes > resistMax) nRes = resistMax;
            if (nRes > MaxResist) nRes = MaxResist;
            return nRes;
        }

        private int RollRangeSigned(int min, int max)
        {
            int range = max - min;
            if (range < 0)
            {
                int v = Roll != null ? Roll(max, max + (-range)) : max + (-range) / 2;
                return Mathf.Clamp(v, max, max + (-range));
            }
            if (range == 0) return min;
            int vv = Roll != null ? Roll(min, max) : min + range / 2;
            return Mathf.Clamp(vv, min, max);
        }
    }
}
