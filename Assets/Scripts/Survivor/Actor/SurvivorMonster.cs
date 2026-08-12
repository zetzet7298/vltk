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
        private int _facing = -1; // ticket 48: chỉ SetDirection khi hướng đổi (NpcBridge tạo Vector2 mới mỗi lần)

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
            _visual.SyncDepth(pos.y); // ticket 46: depth ngay tại spawn (trước Update đầu tiên)
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
                _visual?.SyncDepth(p.y); // ticket 46: Y-sort mỗi lần di chuyển
                // Ticket 35/48: hướng theo vector di chuyển (JX dir order, giống player);
                // cache — chỉ SetDirection khi hướng đổi (tránh garbage allocation NpcBridge).
                int facing = MalePlayerSpriteCatalog.DirectionFromMove(d.normalized);
                if (facing != _facing)
                {
                    _facing = facing;
                    _visual?.SetDirection(facing);
                }
                _visual?.PlayMove(true);
            }
            else
            {
                _visual?.PlayMove(false);
                _visual?.SyncDepth(transform.position.y); // ticket 47: monster đứng yên vẫn Y-sort (không double-call — else chỉ chạy khi dist≤0.001)
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
