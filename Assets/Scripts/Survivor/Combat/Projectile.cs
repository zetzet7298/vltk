using UnityEngine;

namespace VLTK.Survivor
{
    public sealed class Projectile : MonoBehaviour
    {
        public Vector2 dir;
        public float speed = 10f;
        public float damage = 1f;
        public float life = 2f;

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
            transform.position = pos;
            dir = d;
            damage = dmg;
            _t = 0f;
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
                    m.TakeDamage(damage);
                    SurvivorGameDirector.Instance.OnProjectileGone(this);
                    Destroy(gameObject);
                    return;
                }
            }
        }
    }
}
