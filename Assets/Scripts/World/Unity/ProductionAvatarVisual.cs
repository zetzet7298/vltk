using UnityEngine;

namespace VLTK.Production.World.Unity
{
    [DisallowMultipleComponent]
    public sealed class ProductionAvatarVisual : MonoBehaviour
    {
        public Vector2 LastMoveInput { get; private set; }
        public int DirectionIndex { get; private set; }
        public SpriteRenderer Renderer { get; private set; }

        private void Awake()
        {
            EnsureRenderer();
        }

        public void Present(Color color)
        {
            EnsureRenderer();
            Renderer.color = color;
        }

        public void SetMoveInput(Vector2 input)
        {
            LastMoveInput = Vector2.ClampMagnitude(input, 1f);
            if (LastMoveInput.sqrMagnitude > 0.0001f)
                DirectionIndex = DirectionFromMove(LastMoveInput);
        }

        public static int DirectionFromMove(Vector2 input)
        {
            if (input.sqrMagnitude <= 0.0001f)
                return -1;
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            return Mathf.RoundToInt(angle / 45f) % 8;
        }

        private void EnsureRenderer()
        {
            if (Renderer != null)
                return;
            Renderer = GetComponent<SpriteRenderer>();
            if (Renderer == null)
                Renderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
}
