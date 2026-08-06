using UnityEngine;
using UnityEngine.UI;
using VLTK.Sandbox;

namespace VLTK.Survivor
{
    /// <summary>
    /// Joystick UI runtime cho Survivor (portrait 1080x1920, góc dưới-trái).
    /// Build pattern OverlayPanel.Build: canvas riêng sortingOrder 70 (dưới
    /// HUD 80 / supply 90 / overlay modal 100 → dim modal chặn raycast khi
    /// levelup/gameover), GraphicRaycaster + EventSystem (ensure idempotent).
    /// Sprite base/handle từ Resources/UI/VirtualJoystick (giống Sandbox.unity);
    /// thiếu → proxy trắng (fail-closed, không crash). Wire input override vào
    /// SurvivorJoystick (director Input) — joystick pressed → move từ đây.
    /// </summary>
    public sealed class SurvivorJoystickUi : MonoBehaviour
    {
        public MobileJoystick Joystick { get; private set; }

        public static SurvivorJoystickUi Build(SurvivorJoystick input)
        {
            var go = new GameObject("SurvivorJoystickUi");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 70; // dưới overlay modal (100) — modal mở thì joystick không bắt touch
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // root: hit-zone trong suốt + MobileJoystick component
            var root = new GameObject("MobileJoystick", typeof(RectTransform), typeof(Image), typeof(MobileJoystick));
            root.transform.SetParent(go.transform, false);
            var rootRt = (RectTransform)root.transform;
            rootRt.anchorMin = rootRt.anchorMax = Vector2.zero; // góc dưới-trái
            rootRt.anchoredPosition = new Vector2(150f, 140f);  // khớp Sandbox.unity
            rootRt.sizeDelta = new Vector2(280f, 280f);
            var rootImg = root.GetComponent<Image>();
            rootImg.color = new Color(1f, 1f, 1f, 0.001f);
            rootImg.raycastTarget = true;

            // background circle + handle knob (sprite Sandbox; fail-closed proxy trắng)
            var bg = MakeChild("Background", root.transform, 168f);
            bg.img.sprite = LoadSprite("joystick_base") ?? ProxyVisuals.White();
            bg.img.raycastTarget = false;

            var h = MakeChild("Handle", root.transform, 82f);
            h.img.sprite = LoadSprite("joystick_handle") ?? ProxyVisuals.White();
            h.img.raycastTarget = false;

            var joy = root.GetComponent<MobileJoystick>();
            joy.background = bg.rt;
            joy.handle = h.rt;
            joy.radius = 64f;
            joy.inputRadius = 48f;
            joy.sensitivity = 1.35f;
            joy.deadZone = 0.08f;
            joy.knobSmoothing = 0.3f;
            joy.returnSmoothing = 0.2f;

            // ponytail: poll IsPressed/MoveInput (onMove chỉ fire khi drag/up —
            // giữ yên joystick phải tiếp tục move). Cùng nguồn dữ liệu onMove.
            input.TouchOverrideActive = () => joy.IsPressed;
            input.TouchOverrideMove = () => joy.MoveInput;

            var ui = go.AddComponent<SurvivorJoystickUi>();
            ui.Joystick = joy;
            return ui;
        }

        private static Sprite LoadSprite(string name) => Resources.Load<Sprite>("UI/VirtualJoystick/" + name);

        private static (RectTransform rt, Image img) MakeChild(string name, Transform parent, float size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(size, size);
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            img.raycastTarget = false;
            return (rt, img);
        }
    }
}
