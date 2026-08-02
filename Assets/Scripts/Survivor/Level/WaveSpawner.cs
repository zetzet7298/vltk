using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Minimal wave spawner. parity dhcd WaveRefresh (spawn time/interval/limit).
    /// P1: perimeter spawn, interval ramps down, batch ramps up. P2: real WaveConfig pools.
    /// </summary>
    public sealed class WaveSpawner
    {
        private float _timer = 1f;     // first wave quick
        private float _elapsed;

        public void Tick(float dt, System.Action<Vector3> spawnAt)
        {
            _elapsed += dt;
            _timer -= dt;
            float interval = Mathf.Max(0.6f, 2.5f - _elapsed * 0.01f);
            if (_timer > 0f) return;

            _timer = interval;
            int count = 4 + Mathf.FloorToInt(_elapsed / 15f);
            var half = SurvivorGameDirector.Instance.ArenaHalf;
            float r = Mathf.Max(half.x, half.y) + 1f;
            for (int i = 0; i < count; i++)
            {
                float ang = Random.value * Mathf.PI * 2f;
                spawnAt(new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f));
            }
        }
    }
}
