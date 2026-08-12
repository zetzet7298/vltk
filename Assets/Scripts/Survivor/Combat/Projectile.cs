using UnityEngine;
using VLTK.Sprites;

namespace VLTK.Survivor
{
    public sealed class Projectile : MonoBehaviour
    {
        public Vector2 dir;
        public float speed = 10f;
        public float damage = 1f;
        public float life = 2f;

        /// <summary>Ticket 27 attribution: nguồn skill + caster (hit ghi ledger, kill → KillSource).</summary>
        public SkillImpactSource Source;
        public object Caster;

        private float _t;

        private void Awake()
        {
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProxyVisuals.White();
            sr.color = new Color(1f, 0.9f, 0.3f);
            sr.drawMode = SpriteDrawMode.Simple;
            transform.localScale = new Vector3(0.28f, 0.28f, 1f);
        }

        public void Init(Vector3 pos, Vector2 d, float dmg)
        {
            Init(pos, d, dmg, SkillImpactSource.None, null, "");
        }

        public void Init(Vector3 pos, Vector2 d, float dmg, SkillImpactSource source, object caster, string spriteUid)
        {
            transform.position = pos;
            dir = d;
            damage = dmg;
            _t = 0f;
            Source = source;
            Caster = caster;
            if (!string.IsNullOrEmpty(spriteUid))
            {
                var sp = SpriteLoader.Resolve(spriteUid);
                if (sp != null)
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr != null) { sr.sprite = sp; sr.color = Color.white; }
                }
                // resolve null (uid staged lúc parse nhưng file thiếu runtime) → giữ proxy (fail-closed)
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            transform.position += (Vector3)(dir * speed * dt);
            _t += dt;
            if (_t > life) { SurvivorGameDirector.Instance.OnProjectileGone(this); Destroy(gameObject); return; }

            var list = SurvivorGameDirector.Instance.Monsters;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var m = list[i];
                if (m == null) continue;
                if ((m.transform.position - transform.position).sqrMagnitude < 0.36f)
                {
                    m.TakeDamage(damage, Source, Caster);
                    SurvivorAudioMgr.Instance?.PlayEvent(SurvivorAudioEvent.Hit);
                    SurvivorGameDirector.Instance.OnProjectileGone(this);
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
