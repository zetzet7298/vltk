// -----------------------------------------------------------------------------
// VLTK.Survivor — UI: SurvivorHud (ticket 37)
// HUD portrait uGUI: HP bar + XP bar + cấp + timer + wave/boss banner.
// Nguồn dữ liệu = public surface hiện có, KHÔNG sửa file ngoài UI/:
//   - HP/XP/Level  : SurvivorPlayer public (Hp/MaxHp/Xp/XpToNext/Level)
//   - boss banner  : SurvivorGameDirector.Instance.ActiveBoss (poll) — tín hiệu thật
//   - wave index   : WaveIndexSource (Func<int>) — director chưa expose wave index
//     → default null = banner số wave TẮT (fail-closed, không bịa số)
//   - labels       : SurvivorText.Get (ticket 38, bundle StreamingAssets/SurvivorText)
// Pause (card/settings) → Time.deltaTime = 0 → timer đứng, đúng ý.
// Logic thuần (banner state machine, bar clamp, format, Locate) nằm trong
// SurvivorBanner/SurvivorHudLogic — EditMode test seam (SurvivorHudTests).
// Boot: SurvivorHud.EnsureInstance() — SurvivorGameDirector ngoài danh sách file
// được sửa, nên OverlayPanel.Build() là điểm hook duy nhất.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Survivor
{
    /// <summary>Snapshot một run lúc gameover — chỉ số từ nguồn public, không bịa.</summary>
    public sealed class SurvivorRunStats
    {
        public int Level = 1;
        public float TimeSurvived;   // HUD timer (đã pause-aware)
        public int Kills;            // 0 = chưa có counter nguồn → UI ẩn dòng này (fail-closed)
        public int AliveMonsters;    // SurvivorGameDirector.Monsters.Count
    }

    /// <summary>
    /// Wave/boss banner state machine (thuần — test seam). "Đợt N" khi wave index
    /// đổi, "BOSS" khi boss mới xuất hiện (ActiveBoss transition). Tự ẩn sau
    /// ShowSeconds. Fail-closed: waveIndex 0 → không số; cả hai transition cùng
    /// lúc → hiện boss (boss flag đắt hơn số wave).
    /// </summary>
    public sealed class SurvivorBanner
    {
        public const float ShowSeconds = 2.5f;

        private int _lastWave;
        private bool _lastBoss;
        private float _age = float.MaxValue; // chưa hiện gì từ đầu → Current ""

        public string Current { get; private set; } = "";

        public void Poll(int waveIndex, bool bossAlive, float dt)
        {
            bool waveChanged = waveIndex > 0 && waveIndex != _lastWave;
            bool bossChanged = bossAlive && !_lastBoss;
            _lastWave = waveIndex;
            _lastBoss = bossAlive;
            if (waveChanged || bossChanged)
            {
                _age = 0f;
                Current = Text(waveIndex, bossAlive);
            }
            else if (_age >= ShowSeconds)
            {
                Current = "";
            }
            _age += dt;
        }

        /// <summary>Boss đang sống → banner boss thay vì số wave (đợt boss ưu tiên).</summary>
        public static string Text(int waveIndex, bool bossAlive)
            => bossAlive ? "BOSS" : "Đợt " + waveIndex;
    }

    /// <summary>UI helpers thuần — chuỗi i18n + clamp/format — EditMode test được.</summary>
    public static class SurvivorHudLogic
    {
        // Bundle 38 đã có key; đây là map fallback VN chỉ dùng khi bundle thiếu →
        // KHÔNG hiện raw key lên UI (fail-closed). Ít key → dictionary tĩnh.
        private static readonly Dictionary<string, string> FallbackVn = new Dictionary<string, string>
        {
            ["survivor.gameover.title"]   = "THUA",
            ["survivor.gameover.result"]  = "Kết quả",
            ["survivor.gameover.restart"] = "Chơi lại",
            ["survivor.card.title"]       = "LÊN CẤP",
            ["survivor.hud.hp"]            = "HP",
            ["survivor.hud.xp"]            = "XP",
            ["survivor.hud.level"]         = "Cấp",
            ["survivor.hud.timer"]         = "Thời gian",
            ["survivor.hud.kills"]         = "Tiêu diệt",
        };

        /// <summary>
        /// Chuỗi i18n cho key: SurvivorText.Get (lang → vi → raw key) trước; bundle
        /// không có (Get trả đúng key) → fallback VN tĩnh; key lạ → trả key.
        /// </summary>
        public static string Locate(SurvivorText text, string key)
        {
            if (!string.IsNullOrEmpty(key) && text != null)
            {
                string s = text.Get(key);
                if (!string.IsNullOrEmpty(s) && s != key) return s;
            }
            return FallbackVn.TryGetValue(key ?? string.Empty, out var vn) ? vn : key ?? string.Empty;
        }

        /// <summary>Fill bar clamp: max ≤ 0 → 0 (fail-closed, tránh NaN/inf).</summary>
        public static float BarFill(float value, float max)
        {
            if (max <= 0f) return 0f;
            if (value <= 0f) return 0f;
            return Mathf.Clamp01(value / max);
        }

        /// <summary>mm:ss (≥ 100 phút quay về 00 — đủ cho survivor run).</summary>
        public static string FormatTime(float seconds)
        {
            if (seconds < 0f) seconds = 0f;
            int total = Mathf.FloorToInt(seconds);
            return (total / 60).ToString("D2") + ":" + (total % 60).ToString("D2");
        }

        /// <summary>
        /// Dòng kết quả gameover (label i18n + số thật). Kills &gt; 0 mới hiện dòng
        /// (chưa có nguồn → 0 → ẩn, không hiện "Tiêu diệt 0"). stats null → level 1 /
        /// time 0 (fail-safe cho UI). outputsList để panel render nhiều dòng.
        /// </summary>
        public static List<string> FormatGameOver(SurvivorRunStats s, SurvivorText text)
        {
            var lines = new List<string> { Locate(text, "survivor.gameover.result") };
            lines.Add(Locate(text, "survivor.hud.level") + " " + Mathf.Max(1, s != null ? s.Level : 1));
            lines.Add(Locate(text, "survivor.hud.timer") + " " + FormatTime(s != null ? s.TimeSurvived : 0f));
            if (s != null && s.Kills > 0)
                lines.Add(Locate(text, "survivor.hud.kills") + " " + s.Kills);
            return lines;
        }
    }

    /// <summary>
    /// HUD runtime (uGUI). Update() poll mỗi frame từ director/player public.
    /// Không GraphicRaycaster → không chặn click canvas khác (supply 90 / overlay 100).
    /// </summary>
    public sealed class SurvivorHud : MonoBehaviour
    {
        public static SurvivorHud Instance { get; private set; }

        /// <summary>
        /// Nguồn wave index poll mỗi frame. null → banner số wave tắt (fail-closed;
        /// director chưa expose wave index).
        /// </summary>
        public Func<int> WaveIndexSource;

        private Canvas _canvas;
        private Image _hpFill;
        private Image _xpFill;
        private Text _hpText;
        private Text _xpText;
        private Text _levelText;
        private Text _timerText;
        private Text _bannerText;
        private readonly SurvivorBanner _banner = new SurvivorBanner();
        private SurvivorText _text;
        private float _timer;

        // ticket 42: safe-area notch — top-anchored elements dịch xuống khi padding.Top > 0.
        private RectTransform[] _topElements;
        private Vector2[] _topBase;
        private float _appliedTopPx = -1f;
        private const float ReferenceHeight = 1920f; // CanvasScaler reference (1080×1920)

        /// <summary>i18n: override cho hot-switch (ticket 38) / test; mặc định tự load bundle.</summary>
        public SurvivorText Texts
        {
            get => _text != null ? _text : (_text = SurvivorText.LoadFromStreamingAssets());
            set => _text = value;
        }

        /// <summary>Boot: tạo singleton nếu chưa (OverlayPanel.Build gọi — director lock).</summary>
        public static SurvivorHud EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SurvivorHud");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80; // dưới supply bar (90) và overlay modal (100)
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            // không GraphicRaycaster — HUD không bắt click

            var hud = go.AddComponent<SurvivorHud>();
            hud.Construct();
            return hud;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy() { if (Instance == this) Instance = null; }

        private void Construct()
        {
            _canvas = GetComponent<Canvas>();

            // HP bar (trên trái): nền tối + fill đỏ
            _hpFill = MakeBar("HpBar", _canvas.transform, new Vector2(60, -70), new Vector2(500, 42),
                new Color(0.75f, 0.12f, 0.12f), out _hpText);

            // XP bar (dưới HP): fill xanh lục
            _xpFill = MakeBar("XpBar", _canvas.transform, new Vector2(60, -122), new Vector2(500, 38),
                new Color(0.2f, 0.75f, 0.3f), out _xpText);

            // Level: phải trên
            _levelText = MakeAnchorText("Level", _canvas.transform, 40, Color.white,
                new Vector2(1f, 1f), new Vector2(-60, -70), TextAnchor.UpperRight);

            // Timer: giữa trên
            _timerText = MakeAnchorText("Timer", _canvas.transform, 36, new Color(0.9f, 0.9f, 0.95f),
                new Vector2(0.5f, 1f), new Vector2(0, -150), TextAnchor.MiddleCenter);

            // Wave/boss banner: giữa cao, vàng
            _bannerText = MakeAnchorText("Banner", _canvas.transform, 52, new Color(1f, 0.85f, 0.3f),
                new Vector2(0.5f, 1f), new Vector2(0, -230), TextAnchor.MiddleCenter);

            // ticket 42: thu thập top-anchored elements (bar bg + text) cho safe-area inset
            _topElements = new[]
            {
                (RectTransform)_hpFill.transform.parent, // HpBar bg
                (RectTransform)_xpFill.transform.parent, // XpBar bg
                (RectTransform)_levelText.transform,
                (RectTransform)_timerText.transform,
                (RectTransform)_bannerText.transform,
            };
            _topBase = new Vector2[_topElements.Length];
            for (int i = 0; i < _topElements.Length; i++) _topBase[i] = _topElements[i].anchoredPosition;
        }

        private void Update()
        {
            var d = SurvivorGameDirector.Instance;
            var p = d != null ? d.Player : null;
            bool live = p != null;
            _canvas.enabled = live; // fail-closed: không có player (scene khác) → ẩn HUD
            if (!live) return;

            _hpFill.fillAmount = SurvivorHudLogic.BarFill(p.Hp, p.MaxHp);
            _xpFill.fillAmount = SurvivorHudLogic.BarFill(p.Xp, p.XpToNext);
            _hpText.text = SurvivorHudLogic.Locate(Texts, "survivor.hud.hp") + " " + p.Hp + "/" + p.MaxHp;
            _xpText.text = SurvivorHudLogic.Locate(Texts, "survivor.hud.xp") + " " + p.Xp + "/" + p.XpToNext;
            _levelText.text = SurvivorHudLogic.Locate(Texts, "survivor.hud.level") + " " + p.Level;

            if (!p.Dead) _timer += Time.deltaTime; // timescale 0 (card/pause) → timer dừng
            _timerText.text = SurvivorHudLogic.FormatTime(_timer);

            int waveIndex = WaveIndexSource != null ? WaveIndexSource() : 0;
            _banner.Poll(waveIndex, d.ActiveBoss != null, Time.deltaTime);
            _bannerText.text = _banner.Current;

            ApplySafeArea();
        }

        /// <summary>
        /// ticket 42: notch/cutout — dịch top-anchored elements xuống theo
        /// SurvivorPlatformSettings.CurrentSafePadding.Top (normalized × reference
        /// height ≈ px). Chỉ apply khi giá trị đổi (tránh layout churn mỗi frame).
        /// Editor/desktop safeArea = full screen → padding 0 → vị trí base (no-op).
        /// </summary>
        private void ApplySafeArea()
        {
            float topPx = SurvivorPlatformSettings.CurrentSafePadding.Top * ReferenceHeight;
            if (Mathf.Abs(topPx - _appliedTopPx) <= 0.5f) return;
            _appliedTopPx = topPx;
            var off = new Vector2(0f, -topPx);
            for (int i = 0; i < _topElements.Length; i++)
                _topElements[i].anchoredPosition = _topBase[i] + off;
        }

        /// <summary>Snapshot run cho gameover (nguồn public thật; Kills chưa có counter → 0).</summary>
        public SurvivorRunStats Snapshot()
        {
            var d = SurvivorGameDirector.Instance;
            var p = d != null ? d.Player : null;
            return new SurvivorRunStats
            {
                Level = p != null ? p.Level : 1,
                TimeSurvived = _timer,
                AliveMonsters = d != null ? d.Monsters.Count : 0,
            };
        }

        // --- uGUI helpers ---

        /// <summary>Bar: nền + fill (Filled ngang, trái → phải) + label bên trong.</summary>
        private static Image MakeBar(string name, Transform parent, Vector2 pos, Vector2 size,
            Color fillColor, out Text label)
        {
            var bg = new GameObject(name, typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(parent, false);
            var bgRt = (RectTransform)bg.transform;
            bgRt.anchorMin = new Vector2(0f, 1f);
            bgRt.anchorMax = new Vector2(0f, 1f);
            bgRt.pivot = new Vector2(0f, 1f);
            bgRt.anchoredPosition = pos;
            bgRt.sizeDelta = size;
            bg.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.1f, 0.85f);

            var fg = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fg.transform.SetParent(bg.transform, false);
            var fgRt = (RectTransform)fg.transform;
            fgRt.anchorMin = new Vector2(0f, 0f);
            fgRt.anchorMax = new Vector2(1f, 1f); // stretch đúng khung bg — fillAmount vẽ phần trăm
            fgRt.offsetMin = Vector2.zero;
            fgRt.offsetMax = Vector2.zero;
            var fgImg = fg.GetComponent<Image>();
            fgImg.type = Image.Type.Filled;
            fgImg.fillMethod = Image.FillMethod.Horizontal;
            fgImg.fillOrigin = 0;
            fgImg.color = fillColor;

            label = MakeText("Label", bg.transform, 28, Color.white);
            var lrt = (RectTransform)label.transform;
            lrt.anchorMin = lrt.anchorMax = new Vector2(1f, 0.5f);
            lrt.anchoredPosition = new Vector2(-10f, 0f);
            lrt.sizeDelta = new Vector2(size.x - 20f, 30f);
            label.alignment = TextAnchor.MiddleRight;
            return fgImg;
        }

        private static Text MakeAnchorText(string name, Transform parent, int fontSize, Color color,
            Vector2 anchor, Vector2 pos, TextAnchor alignment)
        {
            var t = MakeText(name, parent, fontSize, color);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400f, 64f);
            t.alignment = alignment;
            return t;
        }

        private static Text MakeText(string name, Transform parent, int fontSize, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.raycastTarget = false;
            return t;
        }
    }
}