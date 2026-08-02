using UnityEngine;

namespace VLTK.Survivor
{
    public sealed class SurvivorMonster : MonoBehaviour
    {
        public float MaxHp = 3f;
        public float Speed = 1.6f;
        public int ContactDamage = 1;
        public int XpDrop = 1;

        public float Hp { get; private set; }
        private IActorVisual _visual;
        private Transform _player;

        public void Init(IActorVisual v, Vector3 pos)
        {
            _visual = v;
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
            }
            if (dist < 0.7f) SurvivorGameDirector.Instance.Player.TakeDamage(ContactDamage);
        }

        public bool TakeDamage(float dmg)
        {
            Hp -= dmg;
            if (Hp <= 0f) { Die(); return true; }
            return false;
        }

        private void Die()
        {
            SurvivorGameDirector.Instance.OnMonsterKilled(this);
            Destroy(gameObject);
        }
    }
}
