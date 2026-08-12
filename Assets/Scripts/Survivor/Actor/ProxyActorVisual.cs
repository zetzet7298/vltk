using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>P1 placeholder visual: colored sliced sprite. Swap for JX SPR visual at P1.5 via IActorVisual.</summary>
    public sealed class ProxyActorVisual : MonoBehaviour, IActorVisual
    {
        public Color color = Color.white;
        public Vector2 worldSize = new Vector2(0.8f, 1.2f);

        private SpriteRenderer _sr;

        private void Start()
        {
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = ProxyVisuals.White();
            _sr.color = color;
            _sr.drawMode = SpriteDrawMode.Simple;
            transform.localScale = new Vector3(worldSize.x, worldSize.y, 1f);
        }

        public void SyncPosition(Vector3 worldPos) => transform.position = worldPos;
        // Ticket 46: Y cao = xa = render trước. Fail-closed: renderer chưa có (Start chưa
        // chạy) → bỏ qua, không crash — proxy chưa sync nằm order 0 (dưới mọi JX actor).
        public void SyncDepth(float worldY)
        {
            if (_sr == null) return;
            _sr.sortingOrder = ActorDepth.BaseOrder(worldY);
        }
        public void SetDirection(int dirIndex8) { }
        public void PlayMove(bool moving) { }
        public void SetAlive(bool alive) => gameObject.SetActive(alive);
    }
}
