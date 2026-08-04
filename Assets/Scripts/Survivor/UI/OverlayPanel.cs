using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Survivor
{
    /// <summary>Portrait overlay: levelup card choice + game over (ticket 37: i18n + stats).
    /// parity dhcd NewLevelUpRandomSkillUI.</summary>
    public sealed class OverlayPanel : MonoBehaviour
    {
        private Canvas _canvas;
        private Text _title;
        private readonly List<GameObject> _buttons = new();
        private readonly List<GameObject> _statRows = new();
        private SurvivorText _language;
        /// <summary>Ticket 44: modal skill thật (service path) đang hiển thị — auto-close poll chỉ chạy khi flag này set.</summary>
        private bool _skillModalVisible;
        /// <summary>Ticket 44: onClosed của modal skill đang hiển thị (cùng closure đường pick — release LevelUpScope).</summary>
        private System.Action _skillOnClosed;

        /// <summary>
        /// Ticket 29/37: SkillChoiceService (owner wiring set). null → levelup
        /// chạy legacy P1 flat-card path (giữ API cũ). KHÔNG đụng file service —
        /// hook qua public API: Request/Current/Select/Close.
        /// </summary>
        public SkillChoiceService SkillService { get; set; }
        /// <summary>i18n (38): override cho hot-switch; mặc định tự load bundle StreamingAssets.</summary>
        public SurvivorText Language
        {
            get => _language != null ? _language : (_language = SurvivorText.LoadFromStreamingAssets());
            set => _language = value;
        }

        private string Loc(string key) => SurvivorHudLogic.Locate(Language, key);

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
            // ticket 37: HUD boot — SurvivorGameDirector không trong danh sách file
            // được sửa, OverlayPanel.Build là điểm hook duy nhất chạy đúng lúc OnInit.
            SurvivorHud.EnsureInstance();
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

        /// <summary>
        /// Ticket 37: levelup flow → SkillChoiceService (29) trước (render qua
        /// ShowSkillChoice), fallback legacy P1 flat-card path (giữ API cũ).
        /// Router: pool 29 phải có card mới kích hoạt; chưa có pool/card hoặc đang
        /// bận request → legacy (fail-closed, không chết màn).
        /// onClosed (ticket 43): fire khi modal đóng (cả 2 path) — director
        /// release pause scope LevelUp; service path KHÔNG gọi onPick nên director
        /// không thể tự biết modal đã đóng nếu không có hook này.
        /// </summary>
        public void ShowLevelUp(List<SkillCard> cards, System.Action<SkillCard> onPick,
            System.Action onClosed = null)
        {
            _skillModalVisible = false; // ticket 44: reset flag — modal mới, poll chờ TryShowSkillChoice set lại
            ClearButtons();
            ClearStatRows();
            if (SkillService != null && TryShowSkillChoice(1u, onClosed)) return; // 29: modal skill thật
            ShowLegacyLevelUp(cards, onPick, onClosed);
        }

        /// <summary>
        /// Game over + stats (ticket 37): title/restart i18n qua SurvivorText.Get
        /// (38), dòng kết quả = SurvivorRunStats snapshot (HUD — nguồn public thật)
        /// đã format thuần SurvivorHudLogic.FormatGameOver.
        /// </summary>
        public void ShowGameOver(System.Action onRestart)
        {
            _skillModalVisible = false; // ticket 44: gameover không phải skill modal — poll không đóng nhầm
            ClearButtons();
            ClearStatRows();
            _title.text = Loc("survivor.gameover.title");
            _canvas.enabled = true;

            var stats = SurvivorHud.Instance != null ? SurvivorHud.Instance.Snapshot() : null;
            var lines = SurvivorHudLogic.FormatGameOver(stats, Language);
            for (int i = 0; i < lines.Count; i++)
            {
                var row = MakeText("Stat" + i, transform, 40, new Color(0.85f, 0.85f, 0.9f));
                row.text = lines[i];
                var rt = (RectTransform)row.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0, 40f - i * 90f);
                _statRows.Add(row.gameObject);
            }

            var btn = MakeButton("Restart", transform, Loc("survivor.gameover.restart"), "tap để bắt đầu lại");
            ((RectTransform)btn.transform).anchoredPosition = new Vector2(0, -160);
            btn.onClick.AddListener(() => onRestart());
            _buttons.Add(btn.gameObject);
        }

        public void Hide() => _canvas.enabled = false;

        /// <summary>Ticket 44: modal hiện đang bật (levelup/gameover).</summary>
        public bool IsVisible => _canvas != null && _canvas.enabled;

        /// <summary>
        /// Ticket 44: auto-close (waiting window timeout) — service Close chỉ
        /// release CardChoiceScope; LevelUpScope + canvas hide chỉ đi qua onClosed
        /// hook (đường pick). Poll mỗi frame: modal skill biến mất mà chưa pick
        /// (service đóng) → hide + fire onClosed (closure đúng lifecycle) — không
        /// leak scope, timescale về 1.
        /// </summary>
        public void PollSkillChoiceAutoClose()
        {
            if (!_skillModalVisible) return;
            if (SkillService == null || SkillService.Current(1u) != null) return;
            _skillModalVisible = false;
            _canvas.enabled = false;
            _skillOnClosed?.Invoke();
        }

        private void Update() => PollSkillChoiceAutoClose();

        /// <summary>
        /// Ticket 29: modal card từ SkillChoiceService (SkillDef-based, icon
        /// fail-closed proxy khi SPR chưa staged). Pause acquire/release do
        /// service quản lý (SurvivorPause scope CardChoice) — Overlay chỉ render + callback
        /// (caller đóng).
        /// </summary>
        public void ShowSkillChoice(IReadOnlyList<SkillChoiceCard> cards, string title,
            System.Action<SkillChoiceCard> onPick)
        {
            ClearButtons();
            ClearStatRows();
            _title.text = title;
            _canvas.enabled = true;
            for (int i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                var btn = MakeChoiceButton($"Card{i}", transform, card.Title, card.Desc, card.IconUid, card.Price);
                float y = 150f - i * 260f;
                ((RectTransform)btn.transform).anchoredPosition = new Vector2(0, y);
                btn.onClick.AddListener(() => onPick(card));
                _buttons.Add(btn.gameObject);
            }
        }

        private void ClearButtons()
        {
            foreach (var b in _buttons) if (b != null) Destroy(b);
            _buttons.Clear();
        }

        private void ClearStatRows()
        {
            foreach (var r in _statRows) if (r != null) Destroy(r);
            _statRows.Clear();
        }

        // --- ticket 37: dual-mode levelup ---

        /// <summary>
        /// Legacy P1 flat-card path: render 3 card, onPick trực tiếp (director
        /// apply + resume) — giữ API cũ cho tới khi pool 29 được wire.
        /// ticket 43: modal tự hide + fire onClosed (director release pause scope).
        /// </summary>
        private void ShowLegacyLevelUp(List<SkillCard> cards, System.Action<SkillCard> onPick,
            System.Action onClosed)
        {
            _title.text = Loc("survivor.card.title");
            _canvas.enabled = true;
            int n = cards != null ? cards.Count : 0;
            for (int i = 0; i < n; i++)
            {
                var card = cards[i];
                var btn = MakeButton($"Card{i}", transform, card.title, card.desc);
                float y = 150f - i * 260f;
                ((RectTransform)btn.transform).anchoredPosition = new Vector2(0, y);
                btn.onClick.AddListener(() =>
                {
                    _canvas.enabled = false;
                    onPick(card);
                    onClosed?.Invoke();
                });
                _buttons.Add(btn.gameObject);
            }
        }

        /// <summary>
        /// Ticket 29 path: Request levelup rồi render Current qua ShowSkillChoice;
        /// pick → service.Select (learn roster + close + release pause). Fail-closed:
        /// đang bận request (false) hoặc pool không có card → false → legacy.
        /// Service path cần roster+pool do owner wiring cấp; chưa wire → luôn false
        /// (không crash, levelup chạy P1). onClosed → director release scope LevelUp
        /// (ticket 43 — service path không gọi onPick).
        /// </summary>
        private bool TryShowSkillChoice(ulong roleId, System.Action onClosed)
        {
            if (!SkillService.Request(roleId, SkillChoiceMode.LevelUp)) return false; // busy → queue
            var ev = SkillService.Current(roleId);
            if (ev == null || ev.Cards == null || ev.Cards.Length == 0)
            {
                SkillService.Close(roleId); // dọn event rỗng (chưa có pool) — không kẹt pause
                return false;
            }
            ShowSkillChoice(ev.Cards, Loc("survivor.card.title"),
                card =>
                {
                    if (!SkillService.Select(roleId, card)) return; // card lạ → modal giữ nguyên
                    _canvas.enabled = false;
                    _skillModalVisible = false; // ticket 44: đóng qua pick — poll không fire lần 2
                    onClosed?.Invoke();
                });
            _skillModalVisible = true; // ticket 44: modal skill thật đang hiển thị — auto-close poll canh
            _skillOnClosed = onClosed;
            return true;
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

        /// <summary>Card skill thật (ticket 29): icon SPR fail-closed + giá shop nếu có.</summary>
        private static Button MakeChoiceButton(string name, Transform parent, string title,
            string desc, string iconUid, int price)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(720, 220);
            var img = go.GetComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.2f, 0.95f);

            // icon skill: staged SPR → hiển thị; chưa staged → proxy màu (fail-closed)
            var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(go.transform, false);
            var iconImg = iconGo.GetComponent<Image>();
            var sp = SpriteLoader.Resolve(iconUid);
            if (sp != null) { iconImg.sprite = sp; iconImg.color = Color.white; }
            else { iconImg.sprite = ProxyVisuals.White(); iconImg.color = new Color(0.55f, 0.8f, 1f, 0.9f); }
            var iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(50f, 0f);
            iconRt.sizeDelta = new Vector2(110f, 110f);

            var lbl = MakeText("Lbl", go.transform, 46, Color.white);
            lbl.text = title;
            lbl.rectTransform.anchorMin = lbl.rectTransform.anchorMax = new Vector2(0.5f, 0.72f);
            lbl.rectTransform.anchoredPosition = new Vector2(60f, 0f);

            var d = MakeText("Desc", go.transform, 32, new Color(0.8f, 0.8f, 0.85f));
            d.text = desc;
            d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
            d.rectTransform.anchoredPosition = new Vector2(60f, 0f);

            if (price > 0)
            {
                var pr = MakeText("Price", go.transform, 34, new Color(1f, 0.85f, 0.3f));
                pr.text = price + " vàng";
                pr.rectTransform.anchorMin = pr.rectTransform.anchorMax = new Vector2(1f, 1f);
                pr.rectTransform.anchoredPosition = new Vector2(-20f, -16f);
                pr.alignment = TextAnchor.UpperRight;
            }

            return go.GetComponent<Button>();
        }
    }
}