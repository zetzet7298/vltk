// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: generic DOT (poison/burn/heal).
// Parity dhcd BuffDot (BattleCore/BattleCore.BuffDot.cs): loop timer tick
// (timerMgr.CreateLoopTimer → m_dot), TickWhenAdd → Dot() ngay, RemoveAfterDot →
// m_buff.MarkToFree(), heal variant (m_isHPNaturalRecovery), damageInfo.sourceType
// = SourceBuffer(4), guard caster/target died.
// Config parity: SkillAttrDamageData {MagicType, AttrType, Param1/2/3} +
// BuffDotTickConfig {TickTime, TickWhenAdd, RemoveAfterDot}.
// Mobile: Tick(dt) manual (EditMode pure — không FTimer, không MonoBehaviour).
// Formula DOT (own, O7): val = casterAttr[AttrType] * Param1 + Param2 (round).
// -----------------------------------------------------------------------------
using System;

namespace VLTK.Survivor
{
    public sealed class BuffDotTickConfig
    {
        public float TickTime = 1f;
        public bool TickWhenAdd;
        public bool RemoveAfterDot;

        public BuffDotTickConfig() { }

        public BuffDotTickConfig(float tickTime, bool tickWhenAdd = false, bool removeAfterDot = false)
        {
            TickTime = tickTime;
            TickWhenAdd = tickWhenAdd;
            RemoveAfterDot = removeAfterDot;
        }
    }

    /// <summary>Config damage DOT — parity SkillAttrDamageData (số = own).</summary>
    public sealed class SkillAttrDamageData
    {
        public int MagicType;                                  // element flavor (own: 0 none, 1 fire, 2 water…)
        public ActorAttrDataType AttrType = ActorAttrDataType.Damage; // attr nguồn (caster)
        public float Param1 = 1f;                              // hệ số attr
        public float Param2;                                   // flat cộng thêm
        public bool IsHeal;                                    // heal variant (m_isHPNaturalRecovery)
    }

    /// <summary>Bề mặt nhận DOT tick — test stub hoặc actor (player/monster).</summary>
    public interface ISurvivorDamageable
    {
        int Hp { get; }
        int MaxHp { get; }
        void ApplyDot(DamageInfo info);
    }

    public sealed class BuffDot
    {
        public bool Active { get; private set; }
        public int DotVal { get; private set; }

        private BuffInstance _buff;
        private ISurvivorDamageable _target;
        private object _caster;
        private SkillImpactSource _source;
        private SurvivorDamageLedger _ledger;
        private SurvivorBuffMgr _buffMgr;
        private SkillAttrDamageData _dmg;
        private float _tickTime;
        private float _elapsed;
        private bool _removeAfterDot;

        public void Init(BuffInstance buff, ISurvivorDamageable target, object caster,
            SkillAttrDamageData dmg, BuffDotTickConfig tick, SkillImpactSource source,
            SurvivorDamageLedger ledger, SurvivorBuffMgr buffMgr, SurvivorActorAttr casterAttr)
        {
            _buff = buff;
            _target = target;
            _caster = caster;
            _dmg = dmg;
            _source = source;
            _ledger = ledger;
            _buffMgr = buffMgr;
            _elapsed = 0f;
            _tickTime = tick.TickTime;
            _removeAfterDot = tick.RemoveAfterDot;
            Active = true;

            float attrVal = casterAttr != null ? casterAttr.FinalOf(dmg.AttrType) : 0f;
            DotVal = (int)Math.Round(attrVal * dmg.Param1 + dmg.Param2);

            if (tick.TickWhenAdd) TickNow();
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            if (_dmg == null) { Active = false; return; }
            if (_tickTime <= 0f)
            {
                // config lỗi (TickTime <= 0): fire mỗi Tick — own, tránh loop vô hạn
                TickNow();
                return;
            }
            // accumulate + epsilon: trừ dần (_cd -= dt) tích lũy float dust
            // (vd 1.0 - 0.9f - 0.1f = +2e-8 → miss tick); cộng dồn rồi so sánh thì khỏi.
            _elapsed += dt;
            while (Active && _elapsed + 1e-6f >= _tickTime)
            {
                _elapsed -= _tickTime;
                TickNow();
            }
        }

        private void TickNow()
        {
            if (!Active || _target == null || _target.Hp <= 0) { Active = false; return; }
            if (_buffMgr != null && _buffMgr.MuteDamage) return; // parity: BuffDot.Dot() check m_muteDamage

            if (_dmg.IsHeal)
            {
                // heal đi qua damageInfo nhưng KHÔNG vào ledger damage (own)
                _target.ApplyDot(new DamageInfo
                {
                    Damage = DotVal,
                    IsHeal = true,
                    IsDead = false,
                    SourceType = DamageSourceType.SourceBuffer,
                    Source = _source,
                    Caster = _caster,
                    MagicType = _dmg.MagicType,
                });
            }
            else
            {
                _ledger?.SumSkillDamage(_source, _caster, DotVal);
                var info = new DamageInfo
                {
                    Damage = DotVal,
                    IsHeal = false,
                    IsDead = _target.Hp <= DotVal,
                    SourceType = DamageSourceType.SourceBuffer,
                    Source = _source,
                    Caster = _caster,
                    MagicType = _dmg.MagicType,
                };
                _target.ApplyDot(info);
            }

            if (_removeAfterDot)
            {
                Active = false;
                _buff?.MarkToFree(); // parity: m_removeAfterDot → m_buff.MarkToFree()
            }
        }
    }
}
