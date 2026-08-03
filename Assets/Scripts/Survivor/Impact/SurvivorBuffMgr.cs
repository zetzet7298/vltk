// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: buff manager (apply/stack/expire/state/gates).
// Parity dhcd BuffManger (BattleCore/BattleCore.BuffManger.cs): m_allBuff dict,
// RefreshBuffAttr (ClearImpact → re-apply → dirty), UpdateBuffState (bitmap diff →
// NotifyBuffStateChange), CanMove/CanSkill/CanBeDamaged gates, RmvBuff/LoseBuff/
// ClearAllBuff, RemoveSleepTypeBuff (bị damage → xóa buff sleep), MarkToFree
// (deferred remove chống mutate-list khi iterate).
// Stun bridge: STUN bit set → SM.OnStateEvent(Enter_Stun, duration);
// clear → Finish_Stun (parity BuffStateStun.OnEnter/OnLeave).
// BufferItem.ReplaceAdd shape: re-apply → refresh duration (remaining + elapsed).
// Mobile: remove deferred xử lý cuối Tick (không frame-delay — pure logic).
// -----------------------------------------------------------------------------
using System.Collections.Generic;

namespace VLTK.Survivor
{
    /// <summary>1 buff instance — parity BufferItem {BuffStackNum, BuffID, m_attrConfig, m_dot}.</summary>
    public sealed class BuffInstance
    {
        public BuffDef Def;
        public int Stack;
        public float Remaining;            // giây; < 0 = infinite
        public BuffAttrConfig AttrConfig;
        public object Caster;              // O9: giữ caster ref (dhcd chỉ giữ trên BuffDot)
        public SkillImpactSource Source;
        public BuffDot Dot;

        public bool PendingRemove;
        public void MarkToFree() => PendingRemove = true; // parity BufferItem.MarkToFree
    }

    public sealed class SurvivorBuffMgr
    {
        private readonly Dictionary<int, BuffInstance> _allBuff = new Dictionary<int, BuffInstance>();
        private readonly SurvivorActorAttr _attr;
        private readonly SurvivorActorSM _sm;
        private readonly ISurvivorDamageable _owner;
        private readonly SurvivorDamageLedger _ledger;
        private BuffStateID _states;
        private bool _muteDamage;

        /// <summary>State bit set/clear (không gồm Stun — Stun đi qua SM event).</summary>
        public event System.Action<BuffStateID> StateSet;
        public event System.Action<BuffStateID> StateCleared;

        public SurvivorBuffMgr(SurvivorActorAttr attr, SurvivorActorSM sm, ISurvivorDamageable owner,
            SurvivorDamageLedger ledger = null)
        {
            _attr = attr;
            _sm = sm;
            _owner = owner;
            _ledger = ledger;
        }

        // ---- gates (parity BuffManger.CanMove/CanSkill/CanBeDamaged) ----
        public bool CanMove => !HasState(BuffStateID.Stun | BuffStateID.NoMove | BuffStateID.Sleep);
        public bool CanSkill => !HasState(BuffStateID.Stun | BuffStateID.NoSkill | BuffStateID.Sleep);
        public bool CanBeDamaged => !_muteDamage;
        public bool MuteDamage
        {
            get => _muteDamage;
            set => _muteDamage = value; // parity m_muteDamage flag (S13: gate cả DOT)
        }

        public BuffStateID States => _states;
        public bool HasState(BuffStateID s) => (_states & s) != 0; // any-bit (mask gộp OK)
        public int BuffCount => _allBuff.Count;
        public bool HasBuff(int buffId) => _allBuff.ContainsKey(buffId);
        public BuffInstance GetBuff(int buffId) => _allBuff.TryGetValue(buffId, out var b) ? b : null;

        // ---- apply ----
        public BuffInstance AddBuff(BuffDef def, object caster, SkillImpactSource source,
            SurvivorActorAttr casterAttr = null)
        {
            if (def == null || def.Levels.Count == 0) return null;
            if (_allBuff.TryGetValue(def.BuffId, out var existing)) return Reapply(existing, def, caster, source, casterAttr);

            var level = def.FindAttr(1);
            var b = new BuffInstance
            {
                Def = def,
                Stack = 1,
                AttrConfig = level,
                Remaining = RemainingOf(def, level),
                Caster = caster,
                Source = source,
            };
            _allBuff[def.BuffId] = b;
            CreateDot(b, casterAttr);
            RefreshAll();
            return b;
        }

        /// <summary>Re-apply — parity BufferItem.ReplaceAdd (refresh duration + stack policy).</summary>
        private BuffInstance Reapply(BuffInstance b, BuffDef def, object caster, SkillImpactSource source,
            SurvivorActorAttr casterAttr)
        {
            if (def.ReplaceType == BuffReplaceType.Stack && b.Stack < def.MaxStack)
            {
                b.Stack++;
                b.AttrConfig = def.FindAttr(b.Stack);
                // level mới → dot phải theo config mới (dhcd BuffDot.RefreshAttr recompute
                // m_dotVal khi re-apply); level mới không DOT → tắt dot cũ.
                b.Dot = null;
            }
            if (def.TimeType == BuffTimeType.During)
                b.Remaining = RemainingOf(def, b.AttrConfig); // own: refresh full (dhcd remaining+elapsed ≡ full)
            b.Caster = caster;
            b.Source = source;
            if (b.AttrConfig.HasDot && b.Dot == null) CreateDot(b, casterAttr);
            RefreshAll();
            return b;
        }

        private static float RemainingOf(BuffDef def, BuffAttrConfig level)
        {
            // DurTime <= 0 = infinite (giống TimeType.Infinit) — own
            return def.TimeType == BuffTimeType.Infinit || level.DurTime <= 0f ? -1f : level.DurTime;
        }

        private void CreateDot(BuffInstance b, SurvivorActorAttr casterAttr)
        {
            if (!b.AttrConfig.HasDot) return;
            b.Dot = new BuffDot();
            b.Dot.Init(b, _owner, b.Caster, b.AttrConfig.DotDamageData, b.AttrConfig.DotTick,
                b.Source, _ledger, this, casterAttr);
        }

        // ---- tick / expire ----
        public void Tick(float dt)
        {
            foreach (var b in _allBuff.Values)
            {
                if (b.Remaining > 0f)
                {
                    b.Remaining -= dt;
                    if (b.Remaining < 0f) b.Remaining = 0f;
                }
                b.Dot?.Tick(dt);
            }

            // deferred remove: expire + MarkToFree (chống mutate-list khi iterate)
            bool changed = false;
            if (_allBuff.Count > 0)
            {
                var expired = new List<int>();
                foreach (var kv in _allBuff)
                {
                    if (kv.Value.PendingRemove || (kv.Value.Remaining <= 0f && kv.Value.Def.TimeType == BuffTimeType.During))
                        expired.Add(kv.Key);
                }
                foreach (var id in expired) { _allBuff.Remove(id); changed = true; }
            }
            if (changed) RefreshAll();
        }

        // ---- remove paths (parity RmvBuff / LoseBuff / ClearAllBuff) ----
        public void RmvBuff(int buffId)
        {
            if (_allBuff.Remove(buffId)) RefreshAll();
        }

        public void ClearAllBuff()
        {
            if (_allBuff.Count == 0) return;
            _allBuff.Clear();
            RefreshAll();
        }

        /// <summary>Bị damage → xóa buff sleep-type (parity RemoveSleepTypeBuff).</summary>
        public void NotifyDamaged()
        {
            bool changed = false;
            if (_allBuff.Count > 0)
            {
                var ids = new List<int>(_allBuff.Keys);
                foreach (var id in ids)
                {
                    var b = _allBuff[id];
                    if ((b.AttrConfig.States & BuffStateID.Sleep) != 0)
                    {
                        _allBuff.Remove(id);
                        changed = true;
                    }
                }
            }
            if (changed) RefreshAll();
        }

        // ---- rebuild (parity RefreshBuffAttr + UpdateBuffState) ----
        private void RefreshAll()
        {
            RefreshBuffAttr();
            RecomputeStates();
        }

        private void RefreshBuffAttr()
        {
            _attr.ImpactMgr.Clear();
            foreach (var b in _allBuff.Values)
                foreach (var imp in b.AttrConfig.AttrData)
                    _attr.ImpactMgr.Add(imp);
            _attr.Recompute();
        }

        private void RecomputeStates()
        {
            BuffStateID cur = BuffStateID.None;
            float stunDur = 0f;
            foreach (var b in _allBuff.Values)
            {
                cur |= b.AttrConfig.States;
                if ((b.AttrConfig.States & BuffStateID.Stun) != 0 && b.Remaining > stunDur)
                    stunDur = b.Remaining;
            }

            foreach (var s in BuffStates.All)
            {
                bool had = (_states & s) != 0;
                bool has = (cur & s) != 0;
                if (had == has) continue;
                if (s == BuffStateID.Stun)
                {
                    if (has) _sm.OnStateEvent(ActorStateEvent.Actor_Enter_Stun, stunDur);
                    else _sm.OnStateEvent(ActorStateEvent.Actor_Finish_Stun, 0f);
                }
                else
                {
                    if (has) StateSet?.Invoke(s);
                    else StateCleared?.Invoke(s);
                }
            }
            _states = cur;
        }
    }
}
