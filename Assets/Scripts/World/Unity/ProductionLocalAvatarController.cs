using UnityEngine;

namespace VLTK.Production.World.Unity
{
    [DisallowMultipleComponent]
    public sealed class ProductionLocalAvatarController : MonoBehaviour
    {
        public float moveSpeed = 360f;
        public ProductionAvatarVisual visual;
        public bool clampToBounds = true;
        public Rect mapBounds;

        public Vector2 LastMoveDelta { get; private set; }

        private void Awake()
        {
            if (visual == null)
                visual = GetComponent<ProductionAvatarVisual>();
            if (visual == null)
                visual = gameObject.AddComponent<ProductionAvatarVisual>();
        }

        public void PlaceAt(Vector2 worldPosition, Rect bounds)
        {
            mapBounds = bounds;
            transform.position = new Vector3(worldPosition.x, worldPosition.y, transform.position.z);
        }

        public void ApplyMoveIntent(Vector2 input, float deltaTime)
        {
            Vector2 clamped = Vector2.ClampMagnitude(input, 1f);
            LastMoveDelta = clamped * Mathf.Max(0f, moveSpeed) * Mathf.Max(0f, deltaTime);
            Vector2 next = (Vector2)transform.position + LastMoveDelta;
            if (clampToBounds && mapBounds.width > 0f && mapBounds.height > 0f)
            {
                next.x = Mathf.Clamp(next.x, mapBounds.xMin, mapBounds.xMax);
                next.y = Mathf.Clamp(next.y, mapBounds.yMin, mapBounds.yMax);
            }
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            if (visual != null)
                visual.SetMoveInput(clamped);
        }
    }
}
