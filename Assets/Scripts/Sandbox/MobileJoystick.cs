using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace VLTK.Sandbox
{
    /// <summary>
    /// uGUI virtual joystick shell for the mobile HUD. It keeps the handle clamped
    /// to a circular gate and routes the normalized drag through <see cref="TouchInputService"/>
    /// so dead-zone behavior matches the M6.1 touch-control tests.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MobileJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI")]
        public RectTransform background;
        public RectTransform handle;

        [Tooltip("Maximum visible handle travel in Canvas pixels. Keep this matched to the visual joystick art.")]
        [Min(1f)]
        public float radius = 64f;

        [Tooltip("Distance from center that reaches full movement input. Smaller values make the joystick feel more sensitive without changing visual size.")]
        [Min(1f)]
        public float inputRadius = 48f;

        [Tooltip("Multiplier applied before dead-zone processing for a more responsive feel.")]
        [Min(0.1f)]
        public float sensitivity = 1.35f;

        [Range(0f, 0.95f)]
        public float deadZone = 0.08f;

        public bool resetHandleOnRelease = true;

        [Header("Knob Smoothing")]
        [Tooltip("Làm mượt chuyển động handle (knob) khi kéo để giảm jitter tay. 0 = 1:1 theo ngón. 0.25..0.4 = buttery smooth.")]
        [Range(0f, 0.95f)]
        public float knobSmoothing = 0.3f;

        [Tooltip("Làm mượt vị trí handle khi thả ngón (snap về giữa). 0.18..0.3 = đàn hồi nhẹ.")]
        [Range(0f, 0.95f)]
        public float returnSmoothing = 0.2f;

        [Header("Output")]
        [SerializeField]
        private Vector2 rawInput;

        [SerializeField]
        private Vector2 moveInput;

        public Vector2 RawInput => rawInput;
        public Vector2 MoveInput => moveInput;
        public bool IsPressed { get; private set; }

        public UnityEvent<Vector2> onMove = new UnityEvent<Vector2>();

        private readonly TouchInputService touchInput = new TouchInputService();

        private void Reset()
        {
            background = transform as RectTransform;
            if (transform.childCount > 0)
                handle = transform.GetChild(0) as RectTransform;
        }

        private void Awake()
        {
            if (background == null)
                background = transform as RectTransform;

            touchInput.JoystickDeadZone = deadZone;
            CenterHandle();
        }

private void OnValidate()
        {
            radius = Mathf.Max(1f, radius);
            inputRadius = Mathf.Max(1f, inputRadius);
            sensitivity = Mathf.Max(0.1f, sensitivity);
            deadZone = Mathf.Clamp(deadZone, 0f, 0.95f);
            touchInput.JoystickDeadZone = deadZone;

            if (!Application.isPlaying)
                CenterHandle();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            IsPressed = true;
            OnDrag(eventData);
        }

public void OnDrag(PointerEventData eventData)
        {
            if (background == null)
                background = transform as RectTransform;
            if (background == null)
                return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    background,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                return;
            }

            var normalized = (localPoint / Mathf.Max(1f, inputRadius)) * Mathf.Max(0.1f, sensitivity);
            rawInput = Vector2.ClampMagnitude(normalized, 1f);

            touchInput.JoystickDeadZone = deadZone;
            moveInput = touchInput.JoystickToMove(rawInput);

            // Handle chạy tới _handleTarget; Update() lerp mượt. smoothing=0 → 1:1 tức thì.

            onMove.Invoke(moveInput);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            IsPressed = false;
            rawInput = Vector2.zero;
            moveInput = Vector2.zero;

            // Không snap tức thì — Update() lerp về giữa với returnSmoothing
            // tạo hiệu ứng đàn hồi nhẹ khi thả ngón.
            onMove.Invoke(moveInput);
        }

        private Vector2 _handleTarget = Vector2.zero;

        // Cập nhật mỗi frame: lerp handle tới target để chuyển động mượt,
        // giảm jitter do tay rung. (Best practice: animate knob smoothly.)
        private void Update()
        {
            if (handle == null)
                return;

            Vector2 target = IsPressed ? rawInput * radius : Vector2.zero;
            _handleTarget = target;
            float t = IsPressed ? knobSmoothing : returnSmoothing;

            if (t <= 0f)
            {
                handle.anchoredPosition = target;
            }
            else
            {
                // Frame-rate independent lerp.
                float k = 1f - Mathf.Pow(1f - (1f - t), Time.unscaledDeltaTime * 60f);
                handle.anchoredPosition = Vector2.Lerp(handle.anchoredPosition, target, k);
            }
        }

        public void CenterHandle()
        {
            if (handle != null)
                handle.anchoredPosition = Vector2.zero;
        }

        public void ResetInput(bool notify = false)
        {
            IsPressed = false;
            rawInput = Vector2.zero;
            moveInput = Vector2.zero;
            CenterHandle();
            if (notify)
                onMove.Invoke(Vector2.zero);
        }
    }
}
