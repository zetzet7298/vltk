using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Survivor
{
    /// <summary>Portrait overlay for levelup card choice + game over. parity dhcd NewLevelUpRandomSkillUI.</summary>
    public sealed class OverlayPanel : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _title;
        private readonly List<GameObject> _buttons = new();

        public static OverlayPanel Build()
        {
            var go = new GameObject("SurvivorOverlay");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            // need EventSystem
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var panel = go.AddComponent<OverlayPanel>();
            panel.Construct();
            panel.Hide();
            return panel;
        }

        private void Construct()
        {
            _canvas = GetComponent<Canvas>();

            _title = MakeText("Title", transform, 64, new Color(1f, 0.85f, 0.3f));
            _title.rectTransform.anchorMin = _title.rectTransform.anchorMax = new Vector2(0.5f, 0.82f);
            _title.rectTransform.anchoredPosition = Vector2.zero;
            _title.alignment = TextAnchor.MiddleCenter;
        }

        public void ShowLevelUp(List<SkillCard> cards, System.Action<SkillCard> onPick)
        {
            ClearButtons();
            _title.text = "LÊN CẤP";
            _canvas.enabled = true;
            int n = cards.Count;
            for (int i = 0; i < n; i++)
            {
                var card = cards[i];
                var btn = MakeButton($"Card{i}", transform, card.title, card.desc);
                float y = 150f - i * 260f;
                ((RectTransform)btn.transform).anchoredPosition = new Vector2(0, y);
                btn.onClick.AddListener(() => onPick(card));
                _buttons.Add(btn.gameObject);
            }
        }

        public void ShowGameOver(System.Action onRestart)
        {
            ClearButtons();
            _title.text = "THUA";
            _canvas.enabled = true;
            var btn = MakeButton("Restart", transform, "Chơi lại", "tap để bắt đầu lại");
            ((RectTransform)btn.transform).anchoredPosition = new Vector2(0, 0);
            btn.onClick.AddListener(() => onRestart());
            _buttons.Add(btn.gameObject);
        }

        public void Hide() => _canvas.enabled = false;

        private void ClearButtons()
        {
            foreach (var b in _buttons) if (b != null) Destroy(b);
            _buttons.Clear();
        }

        // --- uGUI helpers ---
        private static Text MakeText(string name, Transform parent, int size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }

        private static Button MakeButton(string name, Transform parent, string label, string desc)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(720, 220);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            var lbl = MakeText("Lbl", go.transform, 46, Color.white);
            lbl.text = label;
            lbl.rectTransform.anchorMin = lbl.rectTransform.anchorMax = new Vector2(0.5f, 0.72f);
            lbl.rectTransform.anchoredPosition = Vector2.zero;

            var d = MakeText("Desc", go.transform, 32, new Color(0.8f, 0.8f, 0.85f));
            d.text = desc;
            d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
            d.rectTransform.anchoredPosition = Vector2.zero;

            return go.GetComponent<Button>();
        }
    }
}
