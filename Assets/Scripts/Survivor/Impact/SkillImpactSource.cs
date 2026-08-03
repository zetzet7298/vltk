// -----------------------------------------------------------------------------
// VLTK.Survivor — Impact: damage attribution + kill credit.
// Parity dhcd: SkillImpactSource {skillId, buffId} (BattleCore/SkillImpactSource.cs),
// DamageInfo.sourceType + DamageSourceType.SourceBuffer=4 (BattleCore/DamageInfo.cs,
// DamageSourceType.cs), ActorEntity.SumSkillDamage(source, damage) (kill bookkeeping).
// Mobile (O9): caster ref đi kèm source (dhcd chỉ giữ caster trên BuffDot).
// Ledger per-damageable: kill credit = TopSource() → XP về caster của source đó.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace VLTK.Survivor
{
    public enum DamageSourceType
    {
        SourceNone = 0,
        SourceBullet = 1,
        SourceLightChain = 2,
        SourceShootPoint = 3,
        SourceBuffer = 4,
    }

    /// <summary>Nguồn sát thương: skill id + buff id gây ra. parity SkillImpactSource.</summary>
    public readonly struct SkillImpactSource : IEquatable<SkillImpactSource>
    {
        public readonly int SkillId;
        public readonly int BuffId;

        public SkillImpactSource(int skillId, int buffId)
        {
            SkillId = skillId;
            BuffId = buffId;
        }

        public static readonly SkillImpactSource None = new SkillImpactSource(0, 0);

        public bool Equals(SkillImpactSource o) => SkillId == o.SkillId && BuffId == o.BuffId;
        public override bool Equals(object o) => o is SkillImpactSource s && Equals(s);
        public override int GetHashCode() => SkillId * 397 ^ BuffId;
    }

    /// <summary>Damage descriptor — parity DamageInfo (subset: damage/isDead/magicType/sourceType).</summary>
    public struct DamageInfo
    {
        public int Damage;
        public bool IsDead;
        public bool IsHeal;                 // own: heal variant (dhcd m_isHPNaturalRecovery)
        public int MagicType;               // element flavor (own config: 0 none, 1 fire, 2 water…)
        public DamageSourceType SourceType;
        public SkillImpactSource Source;
        public object Caster;
    }

    /// <summary>
    /// Sổ sát thương 1 damageable: SumSkillDamage(source, caster, dmg) mỗi hit,
    /// kill credit = TopSource() (tổng cao nhất, tie-break hit gần nhất).
    /// </summary>
    public sealed class SurvivorDamageLedger
    {
        private readonly struct Key : IEquatable<Key>
        {
            public readonly object Caster;
            public readonly SkillImpactSource Source;
            public Key(object caster, SkillImpactSource source) { Caster = caster; Source = source; }

            public bool Equals(Key o) => ReferenceEquals(Caster, o.Caster) && Source.Equals(o.Source);
            public override bool Equals(object o) => o is Key k && Equals(k);
            public override int GetHashCode()
            {
                unchecked
                {
                    int h = (Caster?.GetHashCode() ?? 0) * 397;
                    return (h ^ Source.GetHashCode()) * 31;
                }
            }
        }

        private readonly struct Entry
        {
            public readonly int Total;
            public readonly long LastSeq;
            public Entry(int total, long seq) { Total = total; LastSeq = seq; }
        }

        private readonly Dictionary<Key, Entry> _bySource = new Dictionary<Key, Entry>();
        private long _seq;

        public void SumSkillDamage(SkillImpactSource source, object caster, int damage)
        {
            var k = new Key(caster, source);
            _seq++;
            _bySource.TryGetValue(k, out var e);
            _bySource[k] = new Entry(e.Total + damage, _seq);
        }

        public int GetTotal(SkillImpactSource source, object caster)
        {
            return _bySource.TryGetValue(new Key(caster, source), out var e) ? e.Total : 0;
        }

        public int TotalDamage
        {
            get
            {
                int t = 0;
                foreach (var e in _bySource.Values) t += e.Total;
                return t;
            }
        }

        public int SourceCount => _bySource.Count;

        /// <summary>Nguồn gây nhiều damage nhất (tie-break: hit mới nhất) — kill credit.</summary>
        public bool TryGetTopSource(out SkillImpactSource source, out object caster, out int total)
        {
            source = SkillImpactSource.None;
            caster = null;
            total = 0;
            bool found = false;
            long bestSeq = long.MinValue;
            foreach (var kv in _bySource)
            {
                if (found && kv.Value.Total < total) continue;
                if (kv.Value.Total == total && kv.Value.LastSeq <= bestSeq) continue;
                found = true;
                total = kv.Value.Total;
                bestSeq = kv.Value.LastSeq;
                source = kv.Key.Source;
                caster = kv.Key.Caster;
            }
            return found;
        }
    }
}
