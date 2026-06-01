// -----------------------------------------------------------------------------
// VLTK Mobile — Game HUD Controller
// Loads real SPR art from PC source and wires to UI Toolkit elements.
// PC reference: 顶部控制条.ini, 玩家信息主界面.ini, 工具控制条.ini, 小地图_小.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Core;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.UI
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class GameHudController : MonoBehaviour
    {
        [Header("Data Source")]
        public MonoBehaviour runtimeStateProvider;

        [Header("SPR Art Root (under StreamingAssets)")]
        public string artFolder = "UI/HUD/Art";

        // ── Cached ──
        private VisualElement _hpFill, _mpFill, _staminaFill, _expFill;
        private Label _hpText, _mpText, _staminaText, _expText;
        private Label _levelText;
        private Label _sceneName, _scenePos;
        private TextField _chatInput;

        private HudDataBridge _bridge;
        private bool _initialized;
        private SprRuntimeService _sprService;

        // Button name → SPR icon file mapping (matching PC 按钮条按钮/*.spr)
        private static readonly Dictionary<string, string> ButtonIcons = new()
        {
            { "BtnRun", "btn_run" },      // 跑步 (not extracted, use placeholder name)
            { "BtnSit", "btn_sit" },
            { "BtnHorse", "btn_horse" },
            { "BtnExchange", "btn_exchange" },
            { "BtnStatus", "btn_status" },
            { "BtnItems", "btn_items" },
            { "BtnSkills", "btn_skills" },
            { "BtnTeam", "btn_team" },
            { "BtnFaction", "btn_faction" },
            { "BtnPK", "btn_pk" },
        };

        private void Awake()
        {
            InitBridge();
        }

        private void Start()
        {
            BindElements();
            LoadArt();
            SizeRootToScreen();
        }

        private void Update()
        {
            EnsureRuntimeReady();
            if (!_initialized) return;
            SizeRootToScreen();
            UpdateBars();
        }

        private void EnsureRuntimeReady()
        {
            if (_initialized)
                return;

            BindElements();
            if (_initialized)
            {
                LoadArt();
                SizeRootToScreen();
            }
        }

        // ── Bridge ──
        private void InitBridge()
        {
            var provider = runtimeStateProvider as IRuntimeStateProvider;
            if (provider == null && runtimeStateProvider != null)
                provider = runtimeStateProvider.GetComponent<IRuntimeStateProvider>();

            _bridge = new HudDataBridge(provider, Debug.isDebugBuild);
        }

        // ── Bind ──
        private void BindElements()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null || doc.rootVisualElement == null) return;

            var root = doc.rootVisualElement.Q("GameHud");
            if (root == null) return;

            // Do not let decorative HUD panels steal touches from the uGUI joystick.
            // Specific registered buttons are set back to Position in RegisterClick.
            doc.rootVisualElement.pickingMode = PickingMode.Ignore;
            root.pickingMode = PickingMode.Ignore;
            foreach (var child in root.Children())
                child.pickingMode = PickingMode.Ignore;

            // Bars
            _hpFill = root.Q("HpBarFill");
            _mpFill = root.Q("MpBarFill");
            _staminaFill = root.Q("StaminaBarFill");
            _expFill = root.Q("ExpBarFill");

            _hpText = root.Q<Label>("HpText");
            _mpText = root.Q<Label>("MpText");
            _staminaText = root.Q<Label>("StaminaText");
            _expText = root.Q<Label>("ExpText");

            _levelText = root.Q<Label>("LevelText");
            _sceneName = root.Q<Label>("SceneName");
            _scenePos = root.Q<Label>("ScenePos");
            _chatInput = root.Q<TextField>("ChatInput");

            // Buttons
            RegisterClick(root, "BtnRun", OnRunClick);
            RegisterClick(root, "BtnSit", OnSitClick);
            RegisterClick(root, "BtnHorse", OnHorseClick);
            RegisterClick(root, "BtnStatus", OnStatusClick);
            RegisterClick(root, "BtnItems", OnItemsClick);
            RegisterClick(root, "BtnSkills", OnSkillsClick);
            RegisterClick(root, "BtnTeam", OnTeamClick);
            RegisterClick(root, "BtnFaction", OnFactionClick);
            RegisterClick(root, "BtnPK", OnPKClick);
            RegisterClick(root, "BtnExchange", OnExchangeClick);

            _initialized = true;
        }

        private static void RegisterClick(VisualElement root, string name, System.Action cb)
        {
            var el = root.Q(name);
            if (el != null)
            {
                el.pickingMode = PickingMode.Position;
                el.RegisterCallback<PointerDownEvent>(_ => cb());
            }
        }

        // ── Load SPR Art ──
        private void LoadArt()
        {
            var artPath = System.IO.Path.Combine(Application.dataPath, artFolder);
            if (!System.IO.Directory.Exists(artPath))
            {
                SubsystemLog.Warn("HUD", $"Art folder not found: {artPath}");
                return;
            }

            LoadBarArt(_hpFill, artPath, "bar_hp_fill");
            LoadBarArt(_mpFill, artPath, "bar_mp_fill");
            LoadBarArt(_staminaFill, artPath, "bar_stamina_fill");
            LoadBarArt(_expFill, artPath, "bar_exp_fill");

            // Button icons
            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement.Q("GameHud");
            if (root != null)
            {
                foreach (var kv in ButtonIcons)
                {
                    var btn = root.Q(kv.Key);
                    if (btn == null) continue;
                    var icon = btn.Q(kv.Key + "Icon");
                    if (icon == null) continue;
                    LoadIcon(icon, artPath, kv.Value);
                }

                // Send button icon
                var sendIcon = root.Q("SendBtnIcon");
                if (sendIcon != null)
                    LoadIcon(sendIcon, artPath, "btn_chat_send");

                // Minimap dot
                var dot = root.Q("PlayerDot");
                if (dot != null)
                    LoadIcon(dot, artPath, "minimap_dot");

                // Minimap buttons
                var toggleMap = root.Q("ToggleMapBtn");
                if (toggleMap != null)
                    LoadIcon(toggleMap, artPath, "小地图－世界大地图按钮_01");

                var worldMap = root.Q("WorldMapBtn");
                if (worldMap != null)
                    LoadIcon(worldMap, artPath, "btn_worldmap");
            }
        }

        private static void LoadBarArt(VisualElement fill, string artPath, string name)
        {
            if (fill == null) return;
            var png = System.IO.Path.Combine(artPath, name + ".png");
            if (!System.IO.File.Exists(png)) return;

            var tex = LoadTexture(png);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                fill.style.backgroundImage = new StyleBackground(tex);
                fill.style.backgroundSize = new BackgroundSize(104, 9);
            }
        }

        private static void LoadIcon(VisualElement el, string artPath, string name)
        {
            var png = System.IO.Path.Combine(artPath, name + ".png");
            if (!System.IO.File.Exists(png)) return;

            var tex = LoadTexture(png);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                el.style.backgroundImage = new StyleBackground(tex);
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            var data = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (!tex.LoadImage(data)) return null;
            return tex;
        }

        // ── Size ──
        private void SizeRootToScreen()
        {
            var doc = GetComponent<UIDocument>();
            if (doc == null) return;
            var hud = doc.rootVisualElement.Q("GameHud");
            if (hud == null) return;
            var panel = doc.panelSettings;
            float w = panel != null && panel.referenceResolution.x > 0 ? panel.referenceResolution.x : Screen.width;
            float h = panel != null && panel.referenceResolution.y > 0 ? panel.referenceResolution.y : Screen.height;
            hud.style.width = w;
            hud.style.height = h;
            doc.rootVisualElement.style.width = w;
            doc.rootVisualElement.style.height = h;
        }

        // ── Frame ──
        private void UpdateBars()
        {
            if (_bridge == null)
                InitBridge();
            if (_bridge == null)
                return;

            var snap = _bridge.BuildSnapshot();
            if (!snap.valid)
            {
                SetLevel(1);
                SetBar(_hpFill, _hpText, 100, 100);
                SetBar(_mpFill, _mpText, 50, 50);
                SetBar(_staminaFill, _staminaText, 100, 100);
                SetBar(_expFill, _expText, 0, 100);
                return;
            }

            SetLevel(snap.level);
            SetBar(_hpFill, _hpText, snap.currentLife, snap.maxLife);
            SetBar(_mpFill, _mpText, 50, 50);
            SetBar(_staminaFill, _staminaText, 100, 100);
            SetBar(_expFill, _expText, 0, 100);

            if (_sceneName != null) _sceneName.text = snap.mapName;
            if (_scenePos != null) _scenePos.text = $"{(int)snap.playerPosition.x}, {(int)snap.playerPosition.y}";
        }

        private void SetLevel(int level)
        {
            if (_levelText != null) _levelText.text = level.ToString();
        }

        private static void SetBar(VisualElement fill, Label text, int cur, int max)
        {
            if (fill != null)
            {
                float frac = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
                fill.style.width = Length.Percent(frac * 100f);
            }
            if (text != null)
                text.text = $"{cur}/{max}";
        }

        // ── Button Stubs ──
        private void OnRunClick() => SubsystemLog.Info("HUD", "Toggle Run/Walk");
        private void OnSitClick() => SubsystemLog.Info("HUD", "Toggle Sit");
        private void OnHorseClick() => SubsystemLog.Info("HUD", "Toggle Horse");
        private void OnStatusClick() => SubsystemLog.Info("HUD", "Open Character Status");
        private void OnItemsClick() => SubsystemLog.Info("HUD", "Open Inventory");
        private void OnSkillsClick() => SubsystemLog.Info("HUD", "Open Skills");
        private void OnTeamClick() => SubsystemLog.Info("HUD", "Open Team");
        private void OnFactionClick() => SubsystemLog.Info("HUD", "Open Faction");
        private void OnPKClick() => SubsystemLog.Info("HUD", "Toggle PK");
        private void OnExchangeClick() => SubsystemLog.Info("HUD", "Open Exchange");
    }
}
