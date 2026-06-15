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
        FireEnhanceP,
        FastWalkRunP,
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
