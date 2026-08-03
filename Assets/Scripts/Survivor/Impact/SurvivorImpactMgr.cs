// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: 4-bucket impact manager.
// Parity dhcd ActorAttrImpactMgr (BattleCore/BattleCore.ActorAttrImpactMgr.cs):
// m_listAbEffect / m_listRelEffect / m_listMulEffect / m_listEffect;
// RefreshFinalAttr order Ab → Rel → Mul → Effect (declare-order, IL phần giữa lost).
// Mgr là view rebuild (parity RefreshBuffAttr: ClearImpact → re-apply → SetDirty),
// nên SurvivorBuffMgr dựng lại toàn bộ impact mỗi khi buff list đổi.
// -----------------------------------------------------------------------------
using System.Collections.Generic;

namespace VLTK.Survivor
{
    public sealed class SurvivorImpactMgr
    {
        private readonly List<ActorAttrImpact> _abs = new List<ActorAttrImpact>();     // ABSOLUTE_VAL
        private readonly List<ActorAttrImpact> _rel = new List<ActorAttrImpact>();     // SUM_PERCENT_VAL
        private readonly List<ActorAttrImpact> _mul = new List<ActorAttrImpact>();     // MUL_PERCENT_VAL
        private readonly List<ActorAttrImpact> _effect = new List<ActorAttrImpact>();  // Effect (flat add cuối)

        public void Add(ActorAttrImpact imp)
        {
            switch (imp.AddType)
            {
                case ActorAttrAddType.Absolute: _abs.Add(imp); break;
                case ActorAttrAddType.SumPercent: _rel.Add(imp); break;
                case ActorAttrAddType.MulPercent: _mul.Add(imp); break;
                case ActorAttrAddType.Effect: _effect.Add(imp); break;
                default: _effect.Add(imp); break; // Invalid → effect bucket (own, dhcd INVAL unused)
            }
        }

        public void Clear()
        {
            _abs.Clear();
            _rel.Clear();
            _mul.Clear();
            _effect.Clear();
        }

        public int Count => _abs.Count + _rel.Count + _mul.Count + _effect.Count;

        /// <summary>Ghi final attr theo công thức Ab → Rel → Mul → Effect.</summary>
        public void RefreshFinalAttr(SurvivorActorAttr attr)
        {
            attr.FinalMaxHp = Final(attr.BaseMaxHp, ActorAttrDataType.MaxHp);
            attr.FinalDamage = Final(attr.BaseDamage, ActorAttrDataType.Damage);
            attr.FinalMoveSpeed = Final(attr.BaseMoveSpeed, ActorAttrDataType.MoveSpeed);
            attr.FinalAttackSpeed = Final(attr.BaseAttackSpeed, ActorAttrDataType.AttackSpeed);
            attr.FinalSkillDamageRatio = Final(attr.BaseSkillDamageRatio, ActorAttrDataType.SkillDamageRatio);
            attr.FinalDamageReduce = Final(attr.BaseDamageReduce, ActorAttrDataType.DamageReduce);
            attr.FinalCritAtkRatio = Final(attr.BaseCritAtkRatio, ActorAttrDataType.CritAtkRatio);
            attr.FinalPickUpRange = Final(attr.BasePickUpRange, ActorAttrDataType.PickUpRange);
        }

        private float Final(float baseVal, ActorAttrDataType t)
        {
            float v = baseVal;
            foreach (var i in _abs) if (i.DataType == t) v += i.Value;
            float rel = 0f;
            foreach (var i in _rel) if (i.DataType == t) rel += i.Value;
            v *= 1f + rel;
            // MUL_PERCENT = product (chain): mỗi impact nhân (1+v) — khác rel (cộng dồn)
            foreach (var i in _mul) if (i.DataType == t) v *= 1f + i.Value;
            float add = 0f;
            foreach (var i in _effect) if (i.DataType == t) add += i.Value;
            return v + add;
        }
    }
}
