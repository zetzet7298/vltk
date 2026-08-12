// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorVfxService (ticket 34)
// SkillEffectVisualService parity, fail-closed, KHÔNG sửa Sandbox:
//  - Cast visual (precast SPR + missile SPR) đã thuộc ticket 27
//    (SkillCastSpawner.ShowPreCast + Projectile.Init resolve spriteUid staged).
//    Service này KHÔNG render lại cast — dữ liệu missles1.txt đã được
//    SurvivorSkillParser (ticket 26) resolve thành SkillDef.ChildMissile
//    .AnimFileUid (hash GB2312 signed, staged-check lúc generate catalog);
//    Sandbox PcSkillVisualAutoMapper/PcMissileFullVisualParser = reference
//    read-only, không cần đọc trực tiếp ở runtime.
//  - Ticket 34 scope: hit flash (monster nhận dmg → SpriteRenderer color
//    flash ngắn), death effect (die → burst), levelup burst (quanh player).
//  - Fail-closed: staged-sprite gate VfxStagedSprite — uid rỗng hoặc resolve
//    miss → KHÔNG render, KHÔNG crash, KHÔNG bịa path (proxy màu baseline).
//  - Wiring không đụng SurvivorGameDirector (ticket 31): runtime tự lazy-create
//    (EnsureRuntime từ monster hook), tự subscribe SurvivorPlayer.LevelUp trong
//    Update (poll director instance). Player/Projectile/director không sửa.
//  - Pure logic tách cho EditMode test: VfxStagedSprite + VfxFlashTimeline
//    (không scene, không IO — spec Testing Decisions).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Fail-closed staged-sprite gate (pure). Chưa staged → false (caller dùng
    /// proxy màu / không render). Resolver injectable → EditMode test không cần
    /// SprRuntimeService thật. KHÔNG bịa path: uid rỗng → false ngay.
    /// </summary>
    public static class VfxStagedSprite
    {
        public static bool TryResolve(string uid, System.Func<string, Sprite> resolve, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(uid)) return false;   // không bịa path — fail-closed
            if (resolve == null) return false;
            sprite = resolve(uid);
            return sprite != null;                          // resolve miss (file thiếu runtime) → proxy
        }
    }

    /// <summary>
    /// Flash lifecycle thuần (pure): slot theo token, Tick theo dt, tự drop hết
    /// hạn. Progress 1 → 0 (đầu flash đậm nhất, fade về 0). Renderer apply do
    /// SurvivorVfxRuntime — logic này test được không cần scene.
    /// </summary>
    public sealed class VfxFlashTimeline
    {
        private sealed class Flash
        {
            public int Token;
            public float T;
            public float Duration;
        }

        private readonly List<Flash> _flashes = new List<Flash>();
        private int _nextToken = 1;

        public int ActiveCount => _flashes.Count;

        public int Add(float duration)
        {
            var f = new Flash { Token = _nextToken++, T = 0f, Duration = Mathf.Max(0.01f, duration) };
            _flashes.Add(f);
            return f.Token;
        }

        public void Tick(float dt)
        {
            for (int i = _flashes.Count - 1; i >= 0; i--)
            {
                _flashes[i].T += dt;
                if (_flashes[i].T >= _flashes[i].Duration) _flashes.RemoveAt(i);
            }
        }

        public bool IsActive(int token)
        {
            for (int i = 0; i < _flashes.Count; i++)
                if (_flashes[i].Token == token) return true;
            return false;
        }

        /// <summary>1 khi vừa add → 0 khi hết hạn. Token lạ/đã drop → 0.</summary>
        public float Progress(int token)
        {
            for (int i = 0; i < _flashes.Count; i++)
            {
                var f = _flashes[i];
                if (f.Token == token) return Mathf.Clamp01(1f - f.T / f.Duration);
            }
            return 0f;
        }
    }

    /// <summary>
    /// Facade tĩnh — monster/player hook gọi; runtime tự lazy-create (không cần
    /// director wiring, director thuộc ticket 31). Edit-mode (không play) → no-op.
    /// </summary>
    public static class SurvivorVfxService
    {
        public const float DefaultHitFlashDuration = 0.12f;

        internal static SurvivorVfxRuntime _runtime;

        public static void HitFlash(SurvivorMonster m, float duration = DefaultHitFlashDuration)
        {
            EnsureRuntime()?.HitFlash(m, duration);
        }

        public static void PlayDeath(Vector3 pos)
        {
            EnsureRuntime()?.PlayDeath(pos);
        }

        public static void PlayLevelUp(Vector3 pos)
        {
            EnsureRuntime()?.PlayLevelUp(pos);
        }

        private static SurvivorVfxRuntime EnsureRuntime()
        {
            if (_runtime != null) return _runtime;
            if (!Application.isPlaying) return null;
            var go = new GameObject("survivor_vfx");
            return go.AddComponent<SurvivorVfxRuntime>(); // Awake gán _runtime
        }
    }

    /// <summary>
    /// Runtime VFX: flash queue (color lerp theo VfxFlashTimeline), burst
    /// particles (death/levelup, unscaled dt → chạy cả khi timeScale=0 lúc
    /// levelup card), subscribe player LevelUp (poll — player spawn late).
    /// Scene-scoped: OnDestroy gỡ static ref + unsubscribe.
    /// </summary>
    public sealed class SurvivorVfxRuntime : MonoBehaviour
    {
        private static readonly Color HitFlashColor = Color.white;
        private static readonly Color DeathColorA = new Color(1f, 0.45f, 0.2f);
        private static readonly Color DeathColorB = new Color(0.9f, 0.2f, 0.15f);
        private static readonly Color LevelUpColor = new Color(1f, 0.85f, 0.3f);

        private sealed class FlashTarget
        {
            public SurvivorMonster Monster;
            public SpriteRenderer[] Renderers;
            public Color[] Originals;
        }

        private readonly VfxFlashTimeline _flashes = new VfxFlashTimeline();
        private readonly Dictionary<int, FlashTarget> _flashTargets = new Dictionary<int, FlashTarget>();
        private readonly List<int> _deadTokens = new List<int>();
        private SurvivorPlayer _subscribedPlayer;

        private void Awake()
        {
            SurvivorVfxService._runtime = this;
        }

        private void OnDestroy()
        {
            if (SurvivorVfxService._runtime == this) SurvivorVfxService._runtime = null;
            if (_subscribedPlayer != null) _subscribedPlayer.LevelUp -= OnPlayerLevelUp;
        }

        private void Update()
        {
            EnsurePlayerSubscribed();
            _flashes.Tick(Time.deltaTime);
            ApplyFlashes();
        }

        private void EnsurePlayerSubscribed()
        {
            var p = SurvivorGameDirector.Instance != null ? SurvivorGameDirector.Instance.Player : null;
            if (p == _subscribedPlayer) return;
            if (_subscribedPlayer != null) _subscribedPlayer.LevelUp -= OnPlayerLevelUp;
            _subscribedPlayer = p;
            if (p != null) p.LevelUp += OnPlayerLevelUp;
        }

        private void OnPlayerLevelUp(SurvivorPlayer p)
        {
            PlayLevelUp(p != null ? p.transform.position : Vector3.zero);
        }

        // --- hit flash ---

        public void HitFlash(SurvivorMonster m, float duration)
        {
            if (m == null) return;
            int token = _flashes.Add(duration);
            var rs = m.GetComponentsInChildren<SpriteRenderer>(true);
            if (rs == null || rs.Length == 0) return; // chưa có renderer (pre-Start) → bỏ, không crash
            var originals = new Color[rs.Length];
            for (int i = 0; i < rs.Length; i++)
                if (rs[i]) originals[i] = rs[i].color;
            _flashTargets[token] = new FlashTarget { Monster = m, Renderers = rs, Originals = originals };
        }

        private void ApplyFlashes()
        {
            foreach (var kv in _flashTargets)
            {
                var f = kv.Value;
                if (!_flashes.IsActive(kv.Key) || f.Monster == null) // monster chết/destroy giữa flash → restore bỏ qua
                {
                    Restore(f);
                    _deadTokens.Add(kv.Key);
                    continue;
                }
                float p = _flashes.Progress(kv.Key);
                for (int i = 0; i < f.Renderers.Length; i++)
                {
                    var sr = f.Renderers[i];
                    if (sr == null) continue;
                    sr.color = Color.Lerp(f.Originals[i], HitFlashColor, p);
                }
            }
            for (int i = 0; i < _deadTokens.Count; i++) _flashTargets.Remove(_deadTokens[i]);
            _deadTokens.Clear();
        }

        private static void Restore(FlashTarget f)
        {
            for (int i = 0; i < f.Renderers.Length; i++)
            {
                var sr = f.Renderers[i];
                if (sr != null) sr.color = f.Originals[i];
            }
        }

        // --- burst effects (own lightweight, proxy sprites — VFX sprite only khi staged) ---

        public void PlayDeath(Vector3 pos)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * Mathf.PI * 2f / 6f + Random.value * 0.6f;
                SpawnParticle(pos, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * Random.Range(1.5f, 3f),
                    Color.Lerp(DeathColorA, DeathColorB, Random.value), 0.35f, 0.28f);
            }
        }

        public void PlayLevelUp(Vector3 pos)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f;
                SpawnParticle(pos, new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 2.5f,
                    LevelUpColor, 0.5f, 0.22f);
            }
        }

        private void SpawnParticle(Vector3 pos, Vector3 velocity, Color color, float life, float size)
        {
            var go = new GameObject("vfx_particle");
            var p = go.AddComponent<VfxParticle>();
            p.Init(pos, velocity, color, life, size);
        }
    }

    /// <summary>Burst particle: bay + fade + shrink, unscaled dt (chạy khi pause levelup).</summary>
    public sealed class VfxParticle : MonoBehaviour
    {
        private Vector3 _velocity;
        private float _life = 0.4f;
        private float _t;
        private Color _color = Color.white;
        private float _size = 0.3f;
        private SpriteRenderer _sr;

        public void Init(Vector3 pos, Vector3 velocity, Color color, float life, float size)
        {
            transform.position = pos;
            _velocity = velocity;
            _color = color;
            _life = life;
            _size = size;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProxyVisuals.White();
            _sr.color = color;
            _sr.sortingOrder = 100;
            UpdateScale(1f);
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime; // levelup burst vẫn chạy khi timescale 0
            _t += dt;
            transform.position += _velocity * dt;
            float k = 1f - _t / _life;
            if (k <= 0f) { Destroy(gameObject); return; }
            var c = _color;
            c.a = Mathf.Clamp01(k);
            if (_sr) _sr.color = c;
            UpdateScale(k);
        }

        private void UpdateScale(float k)
        {
            float s = _size * Mathf.Max(0.15f, k); // shrink về 15% trước khi destroy
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
