using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Survivor
{
    public sealed class SurvivorPlayer : MonoBehaviour
    {
        public float MoveSpeed = 5f;
        public float Damage = 1f;
        public float AttackInterval = 0.6f;
        public int Projectiles = 1;
        public int MaxHp = 5;
        public float PickupRadius = 1.6f;

        /// <summary>Ticket 27: roster skill cast runtime (orchestrator seed debug 1 skill tới ticket 29).</summary>
        public SkillCastRuntime Cast;

        public int Hp { get; private set; }
        public int Level { get; private set; } = 1;
        public int Xp { get; private set; }
        public int XpToNext => 5 + (Level - 1) * 3;
        public bool Dead => Hp <= 0;

        // ponytail: decouple từ Singleton Director — Director subscribe ở SpawnPlayer,
        // test pure logic không cần Director.
        public event System.Action<SurvivorPlayer> LevelUp;
        public event System.Action<SurvivorPlayer> Died;

        private IActorVisual _visual;
        private float _attackCd;
        private float _invuln;
        private int _facing = -1; // ticket 48: facing 8-way cache, -1 = chưa set

        public void Init(IActorVisual v, Vector3 pos)
        {
            _visual = v;
            Hp = MaxHp;
            transform.position = pos;
            _visual.SyncPosition(pos);
            _visual.SyncDepth(pos.y); // ticket 46: depth ngay tại spawn
        }

        public void AddXp(int n)
        {
            Xp += n;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                LevelUp?.Invoke(this);
            }
        }

        public void ApplyCard(SkillCard c)
        {
            switch (c.kind)
            {
                case SkillEffectKind.Damage: Damage *= 1.25f; break;
                case SkillEffectKind.AttackSpeed: AttackInterval *= 0.8f; break;
                case SkillEffectKind.MoveSpeed: MoveSpeed *= 1.15f; break;
                case SkillEffectKind.MultiShot: Projectiles++; break;
                case SkillEffectKind.MaxHp: MaxHp++; Hp = Mathf.Min(MaxHp, Hp + 1); break;
            }
        }

        public void TakeDamage(int d)
        {
            if (_invuln > 0 || Dead) return;
            Hp -= d;
            _invuln = 0.6f;
            if (Hp <= 0)
            {
                Hp = 0;
                _visual?.SetAlive(false);
                Died?.Invoke(this);
            }
        }

        /// <summary>Heal (ticket 43, supply heal qua impact 28) — clamp tới MaxHp.</summary>
        public void Heal(int n)
        {
            if (Dead || n <= 0) return;
            Hp = Mathf.Min(MaxHp, Hp + n);
        }

        private void Update()
        {
            if (Dead) return;
            var inst = SurvivorGameDirector.Instance;
            if (inst == null) return; // scene teardown giữa frame — đừng NRE
            float dt = Time.deltaTime;
            if (_invuln > 0) _invuln -= dt;

            var dir = inst.Input.Move;
            if (dir.sqrMagnitude > 1f) dir.Normalize();
            var p = transform.position + (Vector3)(dir * MoveSpeed * dt);
            var b = inst.ArenaHalf;
            p.x = Mathf.Clamp(p.x, -b.x, b.x);
            p.y = Mathf.Clamp(p.y, -b.y, b.y);
            transform.position = p;
            _visual?.SyncPosition(p);
            _visual?.SyncDepth(p.y); // ticket 46: Y-sort mỗi frame
            bool moving = dir.sqrMagnitude > 0.01f;
            _visual?.PlayMove(moving);
            // Ticket 48: facing 8-way theo hướng di chuyển; idle giữ hướng cuối (không reset),
            // chỉ SetDirection khi hướng ĐỔI (tránh spam mỗi frame).
            if (UpdateFacing(ref _facing, dir))
                _visual?.SetDirection(_facing);

            _attackCd -= dt;
            Cast?.Tick(dt);
            if (_attackCd <= 0f) { _attackCd = AttackInterval; Fire(); }
        }

        /// <summary>
        /// Ticket 48: facing-cache semantics — move ≈ 0 → giữ nguyên (idle không reset);
        /// move hợp lệ → facing 0-7; trả true CHỈ khi facing thực sự đổi (caller SetDirection 1 lần).
        /// Static để EditMode test — logic duy nhất player + monster dùng chung convention.
        /// </summary>
        public static bool UpdateFacing(ref int facing, Vector2 move)
        {
            if (move.sqrMagnitude <= 0.01f) return false;
            int next = MalePlayerSpriteCatalog.DirectionFromMove(move);
            if (next == facing) return false;
            facing = next;
            return true;
        }

        private void Fire()
        {
            var target = SurvivorGameDirector.Instance.NearestMonster(transform.position);
            if (target == null) return;
            var d = (Vector2)(target.transform.position - transform.position);
            if (d.sqrMagnitude < 0.0001f) return;
            var baseDir = d.normalized;
            // Ticket 27 hook tối thiểu: roster skill ready → cast thay cho auto-attack tick này;
            // skill đang cd → TryCast false → auto-attack tiếp tục (không đứt nhịp).
            if (Cast != null && Cast.TryCast(baseDir, out var plan))
            {
                SurvivorAudioMgr.Instance?.PlaySkillCast(plan.SkillId);
                SkillCastSpawner.Spawn(SurvivorGameDirector.Instance, plan, transform.position, this);
                return;
            }
            SurvivorAudioMgr.Instance?.PlayEvent(SurvivorAudioEvent.Cast);
            for (int i = 0; i < Projectiles; i++)
            {
                float spread = Projectiles > 1 ? (i - (Projectiles - 1) / 2f) * 0.18f : 0f;
                SurvivorGameDirector.Instance.SpawnProjectile(transform.position, Rotate(baseDir, spread), Damage);
            }
        }

        private static Vector2 Rotate(Vector2 v, float a)
        {
            float c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }
    }
}
