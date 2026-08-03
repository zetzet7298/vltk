// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorAudioSettingsPanel (ticket 40, audio scope)
// Settings UI: 3 slider Master/BGM/SFX + ngôn ngữ toggle (vi/en). Persist:
// BGM/SFX/lang qua SurvivorSaveService (ticket 39, public API — KHÔNG đụng
// service/data model), master qua key PlayerPrefs riêng (SurvivorSettingsData
// không có slot master → giữ key riêng để không sửa file 39). Apply-runtime:
// mỗi thay đổi → IAudioVolumeSink (default = SurvivorAudioMgr) + Save() ngay
// (research 09 §3.4: Save() sau mỗi write).
//
// Fail-closed: thiếu save service / sink / storage → no-op, không crash.
// Volume vẫn được lưu → lần sau SurvivorAudioMgr.Awake bootstrap sẽ áp.
//
// Test seam: SurvivorAudioSettingsController là class thuần (không MonoBehaviour)
// — EditMode test inject MemoryStorage + FakeSink (spec Testing Decisions).
// -----------------------------------------------------------------------------

using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Survivor
{
    /// <summary>
    /// Sink volume — 1 impl thật (SurvivorAudioMgr) + 1 fake trong test.
    /// Null sink = fail-closed: settings vẫn persist, không crash.
    /// </summary>
    public interface IAudioVolumeSink
    {
        float GetVolume(SurvivorAudioBus bus);
        void SetVolume(SurvivorAudioBus bus, float volume);
    }

    /// <summary>Wrapper singleton SurvivorAudioMgr; null instance → fail-closed.</summary>
    public sealed class SurvivorAudioMgrSink : IAudioVolumeSink
    {
        public float GetVolume(SurvivorAudioBus bus)
            => SurvivorAudioMgr.Instance != null ? SurvivorAudioMgr.Instance.GetVolume(bus) : 1f;

        public void SetVolume(SurvivorAudioBus bus, float volume)
            => SurvivorAudioMgr.Instance?.SetVolume(bus, volume);
    }

    /// <summary>
    /// Pure-logic controller settings audio: load → apply, set (clamp) → apply ngay,
    /// save persist. Không phụ thuộc MonoBehaviour/PlayerPrefs trực tiếp — storage
    /// qua ISaveStorage, persistence settings qua SurvivorSaveService (public API).
    /// Master volume lưu key riêng vì SurvivorSettingsData (39) không có slot master.
    /// </summary>
    public sealed class SurvivorAudioSettingsController
    {
        /// <summary>Key PlayerPrefs cho master volume (settings data không có slot master).</summary>
        public const string MasterVolumeKey = "survivor.settings.master";

        private readonly SurvivorSaveService _save;
        private readonly IAudioVolumeSink _sink;
        private readonly ISaveStorage _masterStorage;
        private readonly SurvivorText _text;

        private SurvivorSettingsData _settings;
        private float _master;
        private float _bgm;
        private float _sfx;
        private string _lang;

        public float MasterVolume => _master;
        public float BgmVolume => _bgm;
        public float SfxVolume => _sfx;
        public string Language => _lang;

        /// <summary>Bất kỳ dep nào null đều được (fail-closed) — mất khả năng apply tương ứng.</summary>
        public SurvivorAudioSettingsController(SurvivorSaveService save, IAudioVolumeSink sink,
            ISaveStorage masterStorage, SurvivorText text = null)
        {
            _save = save;
            _sink = sink;
            _masterStorage = masterStorage;
            _text = text;
        }

        /// <summary>Load settings persist + áp ngay master/bgm/sfx + lang (nếu có text).</summary>
        public void Load()
        {
            _settings = _save != null ? _save.LoadSettings(out _) : SurvivorSettingsData.CreateDefault();
            _master = ReadMaster();
            _bgm = _settings.audioBgm;
            _sfx = _settings.audioSfx;
            _lang = string.IsNullOrEmpty(_settings.lang) ? SurvivorText.FallbackLang : _settings.lang;

            ApplyVolumes();
            _text?.SetLanguage(_lang);
        }

        public void SetMasterVolume(float volume)
        {
            _master = Mathf.Clamp01(volume);
            _sink?.SetVolume(SurvivorAudioBus.Master, _master);
        }

        public void SetBgmVolume(float volume)
        {
            _bgm = Mathf.Clamp01(volume);
            _sink?.SetVolume(SurvivorAudioBus.Bgm, _bgm);
        }

        public void SetSfxVolume(float volume)
        {
            _sfx = Mathf.Clamp01(volume);
            _sink?.SetVolume(SurvivorAudioBus.Sfx, _sfx);
        }

        /// <summary>Ngôn ngữ toggle vi/en; giá trị khác bị bỏ qua (fail-closed chỉnh sai).</summary>
        public void SetLanguage(string lang)
        {
            if (lang != "vi" && lang != "en") return;
            if (lang == _lang) return;
            _lang = lang;
            _text?.SetLanguage(_lang);
        }

        /// <summary>Persist toàn bộ state hiện tại (master key riêng + settings JSON).</summary>
        public void Save()
        {
            if (_save != null)
            {
                _settings ??= SurvivorSettingsData.CreateDefault(); // fail-closed: chưa Load()
                _settings.audioBgm = _bgm;
                _settings.audioSfx = _sfx;
                _settings.lang = _lang ?? SurvivorText.FallbackLang;
                _save.SaveSettings(_settings);
            }
            _masterStorage?.SetString(MasterVolumeKey, _master.ToString(CultureInfo.InvariantCulture));
        }

        private void ApplyVolumes()
        {
            _sink?.SetVolume(SurvivorAudioBus.Master, _master);
            _sink?.SetVolume(SurvivorAudioBus.Bgm, _bgm);
            _sink?.SetVolume(SurvivorAudioBus.Sfx, _sfx);
        }

        /// <summary>Đọc float từ storage; thiếu/corrupt → fallback.</summary>
        private float ReadFloat(string raw, float fallback)
        {
            if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var v))
                return fallback;
            return Mathf.Clamp01(v);
        }

        private float ReadMaster()
            => ReadFloat(_masterStorage?.GetString(MasterVolumeKey, null), 1f);
    }

    /// <summary>
    /// uGUI settings panel (portrait): 3 slider Master/BGM/SFX + toggle vi/en.
    /// Build() dựng runtime (pattern OverlayPanel) — không cần prefab/scene setup.
    /// Mọi thay đổi → controller (apply mixer ngay + save persist). Fail-closed:
    /// thiếu SurvivorAudioMgr → sink null, value chỉ persist; bootstrap Awake áp sau.
    /// </summary>
    public sealed class SurvivorAudioSettingsPanel : MonoBehaviour
    {
        private Canvas _canvas;
        private Slider _masterSlider;
        private Slider _bgmSlider;
        private Slider _sfxSlider;
        private Image _viImage;
        private Image _enImage;
        private SurvivorAudioSettingsController _ctrl;

        public static SurvivorAudioSettingsPanel Build()
        {
            var go = new GameObject("SurvivorAudioSettingsPanel");
            go.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var canvas = go.GetComponent<Canvas>();
            canvas.sortingOrder = 150;
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

            var panel = go.AddComponent<SurvivorAudioSettingsPanel>();
            panel.Construct();
            panel.Hide();
            return panel;
        }

        private void Construct()
        {
            _canvas = GetComponent<Canvas>();
            _ctrl = new SurvivorAudioSettingsController(
                new SurvivorSaveService(),
                SurvivorAudioMgr.Instance != null ? new SurvivorAudioMgrSink() : null,
                new PlayerPrefsSaveStorage());
            _ctrl.Load();

            MakeText(transform, "Title", new Vector2(0.5f, 0.92f), new Vector2(0.5f, 0.5f), 64,
                new Color(1f, 0.85f, 0.3f), "CÀI ĐẶT", TextAnchor.MiddleCenter);

            MakeText(transform, "MasterLbl", new Vector2(0.5f, 0.84f), new Vector2(0.5f, 0.5f), 38,
                Color.white, "Âm lượng tổng", TextAnchor.MiddleCenter);
            _masterSlider = MakeSlider("MasterSlider", transform, new Vector2(0.5f, 0.78f), _ctrl.MasterVolume);

            MakeText(transform, "BgmLbl", new Vector2(0.5f, 0.66f), new Vector2(0.5f, 0.5f), 38,
                Color.white, "Nhạc nền", TextAnchor.MiddleCenter);
            _bgmSlider = MakeSlider("BgmSlider", transform, new Vector2(0.5f, 0.60f), _ctrl.BgmVolume);

            MakeText(transform, "SfxLbl", new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.5f), 38,
                Color.white, "Hiệu ứng", TextAnchor.MiddleCenter);
            _sfxSlider = MakeSlider("SfxSlider", transform, new Vector2(0.5f, 0.42f), _ctrl.SfxVolume);

            MakeText(transform, "LangLbl", new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.5f), 38,
                Color.white, "Ngôn ngữ", TextAnchor.MiddleCenter);
            _viImage = MakeLangButton("LangVi", transform, new Vector2(0.40f, 0.24f), "VI");
            _enImage = MakeLangButton("LangEn", transform, new Vector2(0.60f, 0.24f), "EN");
            _viImage.transform.parent.GetComponent<Button>().onClick.AddListener(() => SetLang("vi"));
            _enImage.transform.parent.GetComponent<Button>().onClick.AddListener(() => SetLang("en"));
            RefreshLangHighlight();

            _masterSlider.onValueChanged.AddListener(v => { _ctrl.SetMasterVolume(v); _ctrl.Save(); });
            _bgmSlider.onValueChanged.AddListener(v => { _ctrl.SetBgmVolume(v); _ctrl.Save(); });
            _sfxSlider.onValueChanged.AddListener(v => { _ctrl.SetSfxVolume(v); _ctrl.Save(); });
        }

        private void SetLang(string lang)
        {
            _ctrl.SetLanguage(lang);
            _ctrl.Save();
            RefreshLangHighlight();
        }

        private void RefreshLangHighlight()
        {
            SetButtonTint(_viImage, _ctrl.Language == "vi");
            SetButtonTint(_enImage, _ctrl.Language == "en");
        }

        public void Show() => _canvas.enabled = true;
        public void Hide() => _canvas.enabled = false;

        // --- uGUI helpers (pattern OverlayPanel) ---

        private static void SetButtonTint(Image img, bool active)
        {
            if (img == null) return;
            img.color = active ? new Color(1f, 0.85f, 0.3f, 0.95f) : new Color(0.2f, 0.2f, 0.28f, 0.9f);
        }

        private static Text MakeText(Transform parent, string name, Vector2 anchor, Vector2 pivot,
            int fontSize, Color color, string content, TextAnchor alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = fontSize;
            t.color = color;
            t.text = content;
            t.alignment = alignment;
            t.raycastTarget = false;
            t.rectTransform.anchorMin = anchor;
            t.rectTransform.anchorMax = anchor;
            t.rectTransform.anchoredPosition = Vector2.zero;
            return t;
        }

        private static Slider MakeSlider(string name, Transform parent, Vector2 anchor, float value)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(720, 56);
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.28f, 0.9f);

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(go.transform, false);
            var fillRt = (RectTransform)fillGo.transform;
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            fillGo.GetComponent<Image>().color = new Color(1f, 0.85f, 0.3f, 0.95f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(go.transform, false);
            var hRt = (RectTransform)handleGo.transform;
            hRt.sizeDelta = new Vector2(40, 72);
            handleGo.GetComponent<Image>().color = Color.white;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fillRt;
            slider.handleRect = hRt;
            slider.targetGraphic = handleGo.GetComponent<Image>();
            slider.value = Mathf.Clamp01(value);
            return slider;
        }

        private static Image MakeLangButton(string name, Transform parent, Vector2 anchor, string label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(200, 72);
            var img = go.GetComponent<Image>();

            MakeText(go.transform, "Lbl", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), 36,
                Color.white, label, TextAnchor.MiddleCenter);
            return img;
        }
    }
}