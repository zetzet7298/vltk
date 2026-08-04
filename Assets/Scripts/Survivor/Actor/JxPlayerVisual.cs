using System.IO;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Survivor
{
    /// <summary>
    /// P1.5 JX visual bridge: wrap MalePlayerVisual qua IActorVisual.
    /// Decisions (ticket 16): scope player-only (A); default male variant 019 + pixelsPerUnit (A1+S1);
    /// fail-closed sentinel probe (F4); wrapper + bridge (C1); playAutomatically native (U1).
    /// Y-sort: defer — MalePlayerVisual own sortingOrder (MapRenderer.PlayerSortingOrder + offset);
    /// monster P1.5 vẫn Proxy → Y-sort refine = P2 khi monster cũng PC visual.
    /// </summary>
    public sealed class JxPlayerVisual : MonoBehaviour, IActorVisual
    {
        [SerializeField] private string defaultStandPath = @"spr\npcres\man\MA_BD_019_ST01.spr";
        [SerializeField] private float pixelsPerUnit = 40f;
        [SerializeField] private Color fallbackColor = new Color(0.3f, 0.8f, 1f);
        [SerializeField] private Vector2 fallbackSize = new Vector2(0.7f, 1.1f);

        private IActorVisual _impl;

        private void Awake()
        {
            // ponytail: sentinel probe (F4) — default stand SPR staged = variant batch staged together.
            // Quyết định loại visual trước add component, không create-destroy-flicker.
            if (ProbeSentinel())
            {
                var mpv = gameObject.AddComponent<MalePlayerVisual>();
                mpv.pixelsPerUnit = pixelsPerUnit;   // set sau Awake (render-scale only)
                mpv.RefreshActionParts(force: true); // regenerate sprite với ppu mới (Awake chạy ppu=1)
                _impl = new MaleBridge(mpv);
            }
            else
            {
                AddProxy();
            }
        }

        private bool ProbeSentinel()
        {
            // ponytail: SpritesRuntime lưu theo filename (vd MA_BD_019_ST01.spr), không phải uid.
            // Probe filename trực tiếp, không cần SprRuntimeService hash dep.
            string root = Path.Combine(Application.dataPath, "..", "SpritesRuntime");
            string fileName = Path.GetFileName(defaultStandPath);
            return File.Exists(Path.Combine(root, fileName));
        }

        private void AddProxy()
        {
            var proxy = gameObject.AddComponent<ProxyActorVisual>();
            proxy.color = fallbackColor;
            proxy.worldSize = fallbackSize;
            _impl = proxy;
        }

        // --- IActorVisual forward ---
        public void SyncPosition(Vector3 p) => _impl?.SyncPosition(p);
        public void SyncDepth(float y) => _impl?.SyncDepth(y);
        public void SetDirection(int d) => _impl?.SetDirection(d);
        public void PlayMove(bool m) => _impl?.PlayMove(m);
        public void SetAlive(bool a) => _impl?.SetAlive(a);

        /// <summary>Map IActorVisual → MalePlayerVisual (IPlayerVisual).</summary>
        private sealed class MaleBridge : IActorVisual
        {
            private readonly MalePlayerVisual _v;
            private readonly Transform _t;
            private readonly SpriteRenderer _sr;
            internal MaleBridge(MalePlayerVisual v)
            {
                _v = v;
                _t = v.transform;
                _sr = v.GetComponent<SpriteRenderer>();
            }
            public void SyncPosition(Vector3 p) => _t.position = p;
            // Ticket 46: set base override — MalePlayerVisual.ApplyFrame đọc PlayerBaseSortingOrder
            // mỗi tick (playAutomatically) → tự re-sort toàn bộ part renderer.
            public void SyncDepth(float worldY) => _v.sortingBaseOverride = ActorDepth.BaseOrder(worldY);
            public void SetDirection(int d) => _v.SetDirection(d);
            // ponytail: SetAction Move/Idle đủ; direction do SurvivorPlayer input drive riêng nếu cần.
            public void PlayMove(bool m) => _v.SetAction(m ? PlayerVisualAction.Move : PlayerVisualAction.Idle);
            public void SetAlive(bool a) { if (_sr) _sr.enabled = a; _v.enabled = a; }
        }
    }
}
