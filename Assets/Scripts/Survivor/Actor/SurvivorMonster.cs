using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Survivor
{
    public sealed class SurvivorMonster : MonoBehaviour
    {
        public float MaxHp = 3f;
        public float Speed = 1.6f;
        public int ContactDamage = 1;
        public int XpDrop = 1;

        /// <summary>PC NpcResType (vd "enemy005"). Rỗng → auto-cycle theo spawn order (ticket 35, ≥5 loại).</summary>
        public string VisualRes = "";

        public float Hp { get; private set; }
        private IActorVisual _visual;
        private Transform _player;

        /// <summary>Ticket 27 attribution ledger: SumSkillDamage mỗi hit, KillSource = TopSource lúc die.</summary>
        public SurvivorDamageLedger Ledger { get; } = new SurvivorDamageLedger();
        public SkillImpactSource KillSource { get; private set; }

        public void Init(IActorVisual v, Vector3 pos)
        {
            // Ticket 35: thay proxy P1 bằng JxNpcVisual (JX SPR thật / fail-closed proxy màu).
            // Proxy đến từ director: disable TRƯỚC Start → SpriteRenderer không bao giờ được tạo (không flicker).
            if (v is MonoBehaviour mb)
            {
                mb.enabled = false;
                Destroy(mb);
            }
            if (string.IsNullOrEmpty(VisualRes))
            {
                // ponytail: spawner (director) chưa set res → cycle theo spawn order; wave table P2 sẽ set VisualRes.
                int index = SurvivorGameDirector.Instance != null ? SurvivorGameDirector.Instance.Monsters.Count : 0;
                VisualRes = MonsterVisualResolver.ResolveByIndex(index).resType;
            }
            var jx = gameObject.AddComponent<JxNpcVisual>();
            jx.resType = VisualRes;
            _visual = jx;
            Hp = MaxHp;
            transform.position = pos;
            _visual.SyncPosition(pos);
        }

        private void Update()
        {
            if (_player == null) _player = SurvivorGameDirector.Instance.PlayerTransform;
            if (_player == null) return;
            var d = (_player.position - transform.position);
            float dist = d.magnitude;
            if (dist > 0.001f)
            {
                var step = Mathf.Min(Speed * Time.deltaTime, dist);
                var p = transform.position + (Vector3)(d.normalized * step);
                transform.position = p;
                _visual?.SyncPosition(p);
                // Ticket 35: hướng theo vector di chuyển (JX dir order, giống player).
                _visual?.SetDirection(MalePlayerSpriteCatalog.DirectionFromMove(d.normalized));
                _visual?.PlayMove(true);
            }
            else
            {
                _visual?.PlayMove(false);
            }
            if (dist < 0.7f) SurvivorGameDirector.Instance.Player.TakeDamage(ContactDamage);
        }

        public bool TakeDamage(float dmg)
        {
            Hp -= dmg;
            if (Hp <= 0f) { Die(); return true; }
            SurvivorVfxService.HitFlash(this); // ticket 34: hit flash ngắn trên renderer monster
            return false;
        }

        /// <summary>Hit kèm nguồn: ghi ledger (kill credit → XP qua gem P1; TopSource = skill giết).</summary>
        public bool TakeDamage(float dmg, SkillImpactSource source, object caster)
        {
            if (caster != null)
                Ledger.SumSkillDamage(source, caster, Mathf.RoundToInt(dmg));
            return TakeDamage(dmg);
        }

        private void Die()
        {
            Ledger.TryGetTopSource(out var top, out _, out _);
            KillSource = top;
            SurvivorVfxService.PlayDeath(transform.position); // ticket 34: death effect trước khi ẩn visual
            _visual?.SetAlive(false); // ẩn visual đúng lúc, trước khi Destroy
            SurvivorGameDirector.Instance.OnMonsterKilled(this);
            Destroy(gameObject);
        }
    }
}
