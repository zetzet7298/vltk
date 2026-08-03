// -----------------------------------------------------------------------------
// VLTK.Survivor — UI: SupplyBar (ticket 33).
// Slot UI cho 4 supply: icon + cd ring (Image Filled Radial360) + số giây còn lại.
// Per-frame đọc SurvivorSupplyMgr (cd riêng từng slot), Tick mgr tại đây.
// KHÔNG đụng OverlayPanel.cs (thuộc ticket 29) — canvas riêng sortingOrder 90
// (dưới Overlay 100). Fail-closed: slot disabled → mờ + không bấm được.
// Effect chạy qua event OnUse — ticket 29 HUD subscribe để wire monster/gem list.
// -----------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Survivor
{
    public sealed class SupplyBar : MonoBehaviour
    {
        /// <summary>Nhấn slot sẵn sàng → fire (sau khi TryUse đã đặt cd). Ticket 29 wire effect.</summary>
        public System.Action<SupplyKind> OnUse;

        private sealed class SlotView
        {
            public SupplyKind Kind;
            public Image Bg;
            public Image Ring;
            public Text CdText;
        }

        private SurvivorSupplyMgr _mgr;
        private readonly System.Collections.Generic.List<SlotView> _views = new System.Collections.Generic.List<SlotView>();

        private static readonly Color[] SlotColors =
        {
            new Color(0.15f, 0.55f, 0.25f), // Heal — xanh lá
            new Color(0.65f, 0.4f, 0.1f),   // Bomb — cam
            new Color(0.1f, 0.5f, 0.6f),    // Magnet — cyan
            new Color(0.55f, 0.2f, 0.6f),   // FullClear — tím
        };

        private static readonly string[] SlotIcons = { "✚", "✖", "◆", "★" };

        public static SupplyBar Build(SurvivorSupplyMgr mgr)
        {
            var go = new GameObject("SurvivorSupplyBar");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // dưới OverlayPanel (100)
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            // cần EventSystem cho click (idempotent — OverlayPanel cũng check)
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var bar = go.AddComponent<SupplyBar>();
            bar._mgr = mgr;
            bar.Construct();
            return bar;
        }

        private void Construct()
        {
            // 4 slot dọc cạnh phải (portrait, tầm ngón cái)
            for (int i = 0; i < 4; i++)
            {
                var kind = (SupplyKind)i;
                var view = MakeSlot(kind, SlotIcons[i], SlotColors[i]);
                ((RectTransform)view.Bg.transform).anchoredPosition = new Vector2(470f, 430f - i * 150f);
                _views.Add(view);
            }
        }

        private void Update()
        {
            if (_mgr == null) return;
            _mgr.Tick(Time.deltaTime);
            for (int i = 0; i < _views.Count; i++)
            {
                var view = _views[i];
                var slot = _mgr.GetSlot(view.Kind);
                float cd = slot.Cooldown;
                var c = SlotColors[i];
                view.Bg.color = slot.Enabled
                    ? c
                    : new Color(c.r, c.g, c.b, 0.35f); // fail-closed: mờ, không bấm
                view.Ring.fillAmount = cd > 0f ? Mathf.Clamp01(slot.Remaining / cd) : 0f;
                view.CdText.text = slot.Remaining > 0f ? Mathf.CeilToInt(slot.Remaining).ToString() : "";
            }
        }

        private SlotView MakeSlot(SupplyKind kind, string icon, Color color)
        {
            var go = new GameObject($"Supply{kind}", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(110f, 110f);
            var bg = go.GetComponent<Image>();
            bg.color = color;
            bg.raycastTarget = true;

            // icon
            var iconText = MakeText("Icon", go.transform, 52, Color.white);
            iconText.text = icon;

            // cd ring — radial fill, đen bán trong suốt; đầy dần theo cd còn lại
            var ringGo = new GameObject("Ring", typeof(RectTransform), typeof(Image));
            ringGo.transform.SetParent(go.transform, false);
            var ring = ringGo.GetComponent<Image>();
            ring.color = new Color(0f, 0f, 0f, 0.55f);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.raycastTarget = false;
            ((RectTransform)ringGo.transform).sizeDelta = rt.sizeDelta;

            // cd số giây
            var cdText = MakeText("Cd", go.transform, 34, Color.white);
            cdText.raycastTarget = false;

            var btn = go.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                if (_mgr != null && _mgr.TryUse(kind)) OnUse?.Invoke(kind);
            });

            return new SlotView { Kind = kind, Bg = bg, Ring = ring, CdText = cdText };
        }

        private static Text MakeText(string name, Transform parent, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.raycastTarget = false;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(110f, 110f);
            return t;
        }
    }
}
