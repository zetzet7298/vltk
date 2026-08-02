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
        public void SetDirection(int dirIndex8) { }
        public void PlayMove(bool moving) { }
        public void SetAlive(bool alive) => gameObject.SetActive(alive);
    }
}
