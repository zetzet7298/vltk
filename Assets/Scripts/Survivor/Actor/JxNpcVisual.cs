using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Survivor
{
    /// <summary>
    /// Ticket 35 — monster visual JX bridge: wrap PcNpcVisual qua IActorVisual
    /// (adapter read-only Sandbox, pattern JxPlayerVisual).
    /// Add-then-check: PcNpcVisual tự resolve uid (SprRuntimeService.ComputePathUidHex,
    /// GB2312 signed) trong StreamingAssets/Sprites + Generated/NpcSprites và tự
    /// fail-closed (HasAnyClip=false khi SPR thiếu → không crash, không vẽ rác).
    /// HasAnyClip=false hoặc resType chưa map → ProxyActorVisual màu (fail-closed).
    /// KHÔNG bịa path — path chỉ từ MonsterVisualResolver (verify staged NpcSpriteCatalog.json).
    /// </summary>
    public sealed class JxNpcVisual : MonoBehaviour, IActorVisual
    {
        /// <summary>PC NpcResType (vd "enemy005"). Rỗng → resolve theo spawn-order index (auto variety).</summary>
        public string resType = "";

        [SerializeField] private float pixelsPerUnit = 40f; // quyết định A1+S1 (JxPlayerVisual): char ~1.9 unit
        [SerializeField] private Color fallbackColor = new Color(0.85f, 0.25f, 0.25f);
        [SerializeField] private Vector2 fallbackSize = new Vector2(0.7f, 0.9f);

        private IActorVisual _impl;

        // Init ở Start (không phải Awake): SurvivorMonster set resType SAU AddComponent —
        // Awake chạy ngay trong AddComponent, Start chạy đầu frame sau → nhận được resType.
        private void Start()
        {
            var spec = string.IsNullOrEmpty(resType) ? null : MonsterVisualResolver.Resolve(resType);
            if (spec == null)
            {
                AddProxy(fallbackColor, fallbackSize);
                return;
            }

            var npc = gameObject.AddComponent<PcNpcVisual>();
            npc.renderShadow = false;   // MA_YY_999_ST01/RN01 chưa staged → bỏ shadow (tránh warn spam + IO thừa)
            npc.pixelsPerUnit = pixelsPerUnit;
            npc.Configure(spec.standPath, spec.walkPath, spec.referencePixel);
            if (npc.HasAnyClip)
            {
                // Ticket B: PcNpcVisual.Update tự-drive moving từ position delta (ngưỡng
                // sqrMag > 0.01 = 0.1 units/frame = speed ≥6.25/s) — monster thường 1.6/s
                // KHÔNG qua ngưỡng → ghi đè moving=true của bridge → stand frame khi chạy.
                // Fix adapter: disable Update self-drive, bridge tick thủ công (Tick public
                // advance _time + ApplyFrame với direction/moving do bridge set).
                npc.enabled = false;
                _npc = npc;
                _impl = new NpcBridge(npc);
            }
            else
            {
                // ponytail: fail-closed — SPR map nhưng chưa staged → proxy màu, không crash.
                Destroy(npc);
                AddProxy(spec.fallbackColor, spec.fallbackSize);
            }
        }

        private PcNpcVisual _npc;

        // Thay PcNpcVisual.Update (đã disable): tick animation với direction/moving
        // do bridge SetMoveInput set — walk frame chạy đúng tốc độ monster thật.
        private void Update()
        {
            if (_npc != null) _npc.Tick(Time.deltaTime);
        }

        private void AddProxy(Color color, Vector2 size)
        {
            var proxy = gameObject.AddComponent<ProxyActorVisual>();
            proxy.color = color;
            proxy.worldSize = size;
            _impl = proxy;
        }

        // --- IActorVisual forward ---
        public void SyncPosition(Vector3 p) => _impl?.SyncPosition(p);
        public void SyncDepth(float y) => _impl?.SyncDepth(y);
        public void SetDirection(int d) => _impl?.SetDirection(d);
        public void PlayMove(bool m) => _impl?.PlayMove(m);
        public void SetAlive(bool a) => _impl?.SetAlive(a);

        /// <summary>Map IActorVisual → PcNpcVisual (SetMoveInput self-drive action + direction).</summary>
        private sealed class NpcBridge : IActorVisual
        {
            private readonly PcNpcVisual _v;
            private readonly Transform _t;
            private readonly SpriteRenderer[] _renderers;
            private Vector2 _lastMove;

            internal NpcBridge(PcNpcVisual v)
            {
                _v = v;
                _t = v.transform;
                _renderers = v.GetComponentsInChildren<SpriteRenderer>(true); // NpcSprite + NpcShadow
            }

            public void SyncPosition(Vector3 p) => _t.position = p;

            // Ticket 46: override base + áp dụng ngay (PcNpcVisual ghi sortingOrder hardcode
            // mỗi ApplyFrame — set override để frame sau tự đúng, ApplySortingBase cho ngay lập tức).
            public void SyncDepth(float worldY)
            {
                _v.sortingBaseOverride = ActorDepth.BaseOrder(worldY);
                _v.ApplySortingBase();
            }

            // JX direction order (MalePlayerSpriteCatalog.DirectionFromMove):
            // 0=S 1=SW 2=W 3=NW 4=N 5=NE 6=E 7=SE → vector angle = 270° - 45°*d.
            // Round-trip qua DirectionFromMove là đẳng cấu → SetMoveInput trả đúng dir.
            public void SetDirection(int d)
            {
                float angle = (270f - d * 45f) * Mathf.Deg2Rad;
                _lastMove = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                _v.SetMoveInput(_lastMove);
            }

            // Vector2.zero → DirectionFromMove = -1 → moving=false (idle frame, giữ hướng cuối).
            public void PlayMove(bool m) => _v.SetMoveInput(m ? _lastMove : Vector2.zero);

            public void SetAlive(bool a)
            {
                _v.enabled = a; // dừng Tick/Update khi dead
                foreach (var sr in _renderers)
                    if (sr) sr.enabled = a;
            }
        }
    }
}
