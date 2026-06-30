using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>PC faction ids used by Skills.txt CharClass. None=0, Cái Bang=4.</summary>
    [Serializable]
    public enum CombatFaction
    {
        None = 0,
        Shaolin = 1,
        TianWang = 2,
        TangMen = 3,
        CaiBang = 4,
        WuDu = 5,
        TianRen = 6,
        EMei = 7,
        CuiYan = 8,
        WuDang = 9,
        KunLun = 10,
    }

    /// <summary>
    /// PC ngũ hành (OBJ_ATTRIBYTE_TYPE in GameDataDef.h).
    /// Numeric values mirror PC source so parity with KNpc.cpp/mobile combat pipeline is
    /// traceable: Metal=0..Earth=4 are the 5 hành; Nil=5 = no series assigned;
    /// Minus=6 forces damage to 1 (PC KNpc.cpp:2454 `if (m_Series == series_minus) nDamage = 1;`).
    /// </summary>
    [Serializable]
    public enum Series
    {
        Metal = 0,   // PC: series_metal (Kim)
        Wood = 1,    // PC: series_wood (Mộc)
        Water = 2,   // PC: series_water (Thủy)
        Fire = 3,    // PC: series_fire (Hỏa)
        Earth = 4,   // PC: series_earth (Thổ)
        Nil = 5,     // PC: series_nil — sentinel "no series"
        Minus = 6,   // PC: series_minus — damage cap to 1
    }

    /// <summary>
    /// Default ngũ hành cho mỗi môn phái (PC: per-character SetSeries via Lua, default
    /// theo thiết kế gốc VLTK). Dùng khi skill không khai báo Series (Nil) thì combat
    /// pipeline dùng hành mặc định của môn phái thay vì skip ApplyFiveElements.
    /// </summary>
    public static class CombatFactionSeriesExtensions
    {
        public static Series GetFactionSeries(this CombatFaction faction)
        {
            switch (faction)
            {
                case CombatFaction.Shaolin:   return Series.Metal;
                case CombatFaction.TianWang:  return Series.Earth;
                case CombatFaction.TangMen:   return Series.Water;
                case CombatFaction.CaiBang:   return Series.Metal;
                case CombatFaction.WuDu:      return Series.Fire;
                case CombatFaction.TianRen:   return Series.Wood;
                case CombatFaction.EMei:      return Series.Water;
                case CombatFaction.CuiYan:    return Series.Wood;
                case CombatFaction.WuDang:    return Series.Metal;
                case CombatFaction.KunLun:    return Series.Metal;
                default:                       return Series.Nil;
            }
        }
    }

    /// <summary>PC skill style (SkillStyle column): missile/melee/initiative/passive/etc.</summary>
    [Serializable]
    public enum PcSkillStyle
    {
        Missiles = 0,
        Melee = 1,
        InitiativeNpcState = 2,
        PassivityNpcState = 3,
        Summon = 4,
    }

    /// <summary>
    /// PC melee subtype (KNpc::CastMeleeSkill switch line 1834-1891).
    /// Phân biệt các nhánh dash/jump/run trong melee skill. Mặc định = AttackWithBlur.
    /// Áp dụng khi skillStyle=Melee; bỏ qua nếu Missiles.
    /// </summary>
    [Serializable]
    public enum PcMeleeType
    {
        None = 0,              // Không melee (Missiles / Initiative / Passive)
        AttackWithBlur = 1,    // PC: Melee_AttackWithBlur — instant swing, no jump (mặc định cho melee)
        Jump = 2,              // PC: Melee_Jump — chỉ nhảy tới target
        JumpAndAttack = 3,     // PC: Melee_JumpAndAttack — nhảy + chém cùng lúc (Phi Long 357, Kháng Long 128)
        RunAndAttack = 4,      // PC: Melee_RunAndAttack — chạy tới + chém
        ManyAttack = 5,        // PC: Melee_ManyAttack — nhiều hit không cần jump
    }

    /// <summary>PC magic attribute names used by novice + Cái Bang scripts.</summary>
    [Serializable]
    public enum MagicAttributeKind
    {
        PhysicsDamageV,
        FireDamageV,
        PoisonDamageV,
        PhysicsEnhanceP,
        AttackRatingP,
        AddPhysicsDamageP,
        AttackRatingEnhanceP,
        DeadlyStrikeEnhanceP,
        LightingResP,
        FireResP,
        PoisonResP,
        ColdResP,
        PhysicsResP,
        AllResP,
        AddDefenseV,
        ConfuseP,
        SkillCostV,
        MeleeDamageReturnP,
        RangeDamageReturnP,
        LightingDamageV,
        SeriesDamageP,
        ManaShieldP,
        ManaMaxP,
        ManaReplenishV,
        LightingEnhanceP,
        AttackSpeedV,
        CastSpeedV,
        StealManaP,
        DeadlyStrikeP,
        StunP,
        StaminaMaxP,
        ColdDamageV,
        IgnoreDefenseP,
        BadStatusTimeReduceV,
        AddPoisonDamageV,
        AddColdDamageV,
        AddFireDamageV,
        AddLightingDamageV,
        StealLifeP,
        LifeReplenishV,
        StealStaminaP,
        LifeMaxP,
        LifeMaxYanP, // [SECT-QUICKWIN] Gap baocao-all-sect-skills.md: Yan (smoke) variant for life max buff (TianRen 36, 150, 1075, 1076)
        FireEnhanceP,
        FastWalkRunP,
        // [CaiBang-PC-Parity 2026-06-30] PC gaibang120zuzhou debuff attrs (skill 720):
        // physicsresmax_p / fireresmax_p reduce the MAX resistance cap, distinct from *_res_p percent.
        PhysicsResMaxP,
        FireResMaxP,
    }

    [Serializable]
    public class SkillMagicAttribute
    {
        public MagicAttributeKind kind;
        public int value1;
        public int value2;
        public int value3;

        public SkillMagicAttribute() { }
        public SkillMagicAttribute(MagicAttributeKind kind, int value1, int value2, int value3)
        {
            this.kind = kind;
            this.value1 = value1;
            this.value2 = value2;
            this.value3 = value3;
        }

        public override string ToString() => $"{kind}={value1},{value2},{value3}";
    }

    [Serializable]
    public class SkillLevelData
    {
        public int level;
        public List<SkillMagicAttribute> damage = new();
        public List<SkillMagicAttribute> immediate = new();
        public List<SkillMagicAttribute> state = new();
        public List<SkillMagicAttribute> skill = new();

        public IEnumerable<SkillMagicAttribute> AllAttributes()
        {
            foreach (var a in damage) yield return a;
            foreach (var a in immediate) yield return a;
            foreach (var a in state) yield return a;
            foreach (var a in skill) yield return a;
        }

        public SkillMagicAttribute First(MagicAttributeKind kind)
        {
            foreach (var a in AllAttributes())
                if (a.kind == kind) return a;
            return null;
        }
    }

    public enum CombatRelation
    {
        Enemy,
        Ally,
        Self,
    }

    public enum CombatCastRejectReason
    {
        None,
        NoSkill,
        SkillNotKnown,
        NotInFightMode,
        OnCooldown,
        InsufficientLevel,
        InsufficientResource,
        InvalidTarget,
        OutOfRange,
        WeaponSkillMismatch,
        HorseRestricted,
        FactionMismatch,
        TargetBlocked,
    }

    public enum CombatActionState
    {
        Stand,
        Attack,
        Magic,
        Melee,
    }
}
