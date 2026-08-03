using UnityEngine;

namespace VLTK.Survivor
{
    public sealed class XpGem : MonoBehaviour
    {
        public int amount = 1;
        public CollectSettings Settings;

        private float _life;

        private void Awake()
        {
            Settings = CollectSettings.Default();
            _life = Settings.GemLifetime;
            var sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProxyVisuals.White();
            sr.color = new Color(0.4f, 0.9f, 1f);
            sr.drawMode = SpriteDrawMode.Simple;
            transform.localScale = new Vector3(0.3f, 0.3f, 1f);
        }

        public void Init(Vector3 pos, int n)
        {
            Init(pos, n, CollectSettings.Default());
        }

        public void Init(Vector3 pos, int n, CollectSettings settings)
        {
            transform.position = pos;
            amount = n;
            Settings = settings;
            _life = settings.GemLifetime;
        }

        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0f)
            {
                // hết hạn — chống tích tụ gem (parity-shape monster lifetime)
                SurvivorGameDirector.Instance?.OnGemCollected(this);
                Destroy(gameObject);
                return;
            }
            var d = SurvivorGameDirector.Instance;
            if (d == null || d.Player == null) return;
            bool picked = MagnetMath.Pull(transform.position, (Vector2)d.Player.transform.position, Settings, Time.deltaTime, out var np);
            transform.position = np;
            if (picked)
            {
                d.Player.AddXp(amount);
                d.OnGemCollected(this);
                SurvivorAudioMgr.Instance?.PlayEvent(SurvivorAudioEvent.Pickup);
                Destroy(gameObject);
            }
        }
    }
}
