// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: attr data + impact type enums.
// Parity dhcd: ActorAttrData (BattleCore/BattleCore.ActorAttrData.cs),
// ActorAttrDataType (GameProto/ResDef/ActorAttrDataType.cs),
// ActorAttrAddType (GameProto/ResDef/ActorAttrAddType.cs: INVAL=0 ABS=1 SUM%=2 MUL%=3).
// Attr subset = own (O5: bỏ RPG-only fields); AttackSpeed = own, ngoài enum dhcd.
// Công thức final (order bucket Ab → Rel → Mul → Effect, parity RefreshFinalAttr):
//   v = base; v += Σabs; v *= 1+Σrel; v *= 1+Σmul; v += Σeffect
// -----------------------------------------------------------------------------
using System;

namespace VLTK.Survivor
{
    /// <summary>Attr field id — parity ActorAttrDataType, giữ value dhcd cho field chung.</summary>
    public enum ActorAttrDataType
    {
        None = 0,
        MaxHp = 1,
        Damage = 2,
        DamageReduce = 10,
        CritAtkRatio = 12,
        MoveSpeed = 29,
        PickUpRange = 33,
        SkillDamageRatio = 49,
        // own: dhcd dùng CD-reduce cho nhịp đánh; survivor dùng attack speed trực tiếp.
        AttackSpeed = 88,
    }

    /// <summary>Kiểu phép cộng impact — parity ActorAttrAddType (Effect = own, bucket m_listEffect).</summary>
    public enum ActorAttrAddType
    {
        Invalid = 0,    // INVAL_VAL
        Absolute = 1,   // ABSOLUTE_VAL: flat cộng vào base
        SumPercent = 2, // SUM_PERCENT_VAL: cộng % (additive nhau)
        MulPercent = 3, // MUL_PERCENT_VAL: nhân % (multiplicative, chain)
        Effect = 4,     // m_listEffect: flat add sau cùng (dhcd enum không có value riêng — own)
    }

    /// <summary>1 impact entry — parity ActorAttrImpactData {dataType, addType, value}.</summary>
    public readonly struct ActorAttrImpact
    {
        public readonly ActorAttrDataType DataType;
        public readonly ActorAttrAddType AddType;
        public readonly float Value;

        public ActorAttrImpact(ActorAttrDataType dataType, ActorAttrAddType addType, float value)
        {
            DataType = dataType;
            AddType = addType;
            Value = value;
        }

        public override string ToString() => $"{AddType} {DataType} {Value}";
    }

    /// <summary>
    /// Attr 3 lớp (base → runtime impact → final), parity ActorData m_baseData/m_attrData
    /// + m_baseChanged/m_runtimeChanged → RefreshAttr. Impact list sống trong SurvivorImpactMgr.
    /// </summary>
    public sealed class SurvivorActorAttr
    {
        // ---- base (own: config/player) ----
        public float BaseMaxHp = 5f;
        public float BaseDamage = 1f;
        public float BaseMoveSpeed = 5f;
        public float BaseAttackSpeed = 1f;   // nhịp đánh/s (own, xem enum note)
        public float BaseSkillDamageRatio = 1f;
        public float BaseDamageReduce = 0f;
        public float BaseCritAtkRatio = 0.05f;
        public float BasePickUpRange = 1.6f;

        // ---- final (sau RefreshFinalAttr) ----
        public float FinalMaxHp;
        public float FinalDamage;
        public float FinalMoveSpeed;
        public float FinalAttackSpeed;
        public float FinalSkillDamageRatio;
        public float FinalDamageReduce;
        public float FinalCritAtkRatio;
        public float FinalPickUpRange;

        public SurvivorImpactMgr ImpactMgr { get; } = new SurvivorImpactMgr();

        /// <summary>Fired khi final thay đổi (parity ActorData.RefreshAttr → event).</summary>
        public event Action Changed;

        public void Recompute()
        {
            ImpactMgr.RefreshFinalAttr(this);
            Changed?.Invoke();
        }

        /// <summary>Final value theo field id — dùng cho DOT formula (attr nguồn).</summary>
        public float FinalOf(ActorAttrDataType t)
        {
            switch (t)
            {
                case ActorAttrDataType.MaxHp: return FinalMaxHp;
                case ActorAttrDataType.Damage: return FinalDamage;
                case ActorAttrDataType.DamageReduce: return FinalDamageReduce;
                case ActorAttrDataType.CritAtkRatio: return FinalCritAtkRatio;
                case ActorAttrDataType.MoveSpeed: return FinalMoveSpeed;
                case ActorAttrDataType.PickUpRange: return FinalPickUpRange;
                case ActorAttrDataType.SkillDamageRatio: return FinalSkillDamageRatio;
                case ActorAttrDataType.AttackSpeed: return FinalAttackSpeed;
                default: return 0f;
            }
        }
    }
}
