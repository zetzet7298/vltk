// -----------------------------------------------------------------------------
// VLTK Mobile — Game HUD Controller
// Loads real SPR art from PC source and wires to UI Toolkit elements.
// PC reference: 顶部控制条.ini, 玩家信息主界面.ini, 工具控制条.ini, 小地图_小.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Core;
using VLTK.Model;
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

        private VisualElement _hpFill, _mpFill, _staminaFill, _expFill;
        private VisualElement _minimapContent, _previewContent;
        private VisualElement _playerDot, _mapPreviewOverlay, _mapPreviewFrame, _mapPreviewPlayerDot;
        private VisualElement _miniMapTarget, _mapPreviewTarget;
        private VisualElement _caiBangSkillPanel, _caiBangSkillClose, _caiBangSkillPageOne, _caiBangSkillPageTwo;
        private ScrollView _caiBangSkillList;
        private Label _hpText, _mpText, _staminaText, _expText;
        private Label _levelText, _sceneName, _scenePos, _mapPreviewTitle, _mapPreviewCoords, _caiBangSkillSummary;
        private TextField _chatInput;

        private HudDataBridge _bridge;
        private MinimapService _minimapService;
        private bool _initialized;
        private SprRuntimeService _sprService;
        private Texture2D _minimapTexture;
        private Texture2D _previewTexture;
        private int _minimapTextureMapId = -1;
        private int _previewTextureMapId = -1;
        private int _caiBangSkillPageIndex;
        private Vector2 _lastMinimapCenter;
        private Vector2? _lastMoveTarget;

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
            InitializeCombatSkillSlots();
        }

        private void InitializeCombatSkillSlots()
        {
            var slots = GetComponent<CombatSkillSlotController>();
            if (slots == null)
                slots = gameObject.AddComponent<CombatSkillSlotController>();

            var manager = SandboxManager.Instance;
            var catalog = manager != null ? manager.CombatSkillCatalog : null;
            var progression = manager != null ? manager.PlayerProgression : null;
            slots.Initialize(catalog, progression);

            // Ensure skill effect overlays for rendering combat visuals.
            // IMGUI overlay is kept for debug labels; world overlay makes VFX visible in camera/game view.
            if (GetComponent<SkillEffectOverlay>() == null)
                gameObject.AddComponent<SkillEffectOverlay>();
            if (GetComponent<SkillEffectWorldOverlay>() == null)
                gameObject.AddComponent<SkillEffectWorldOverlay>();
        }

        private void Update()
        {
            EnsureRuntimeReady();
            if (!_initialized) return;
            SizeRootToScreen();
            UpdateBarsAndMinimap();
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

        private void InitBridge()
        {
            var provider = runtimeStateProvider as IRuntimeStateProvider;
            if (provider == null && runtimeStateProvider != null)
                provider = runtimeStateProvider.GetComponent<IRuntimeStateProvider>();

            _bridge = new HudDataBridge(provider, Debug.isDebugBuild);
            _minimapService = new MinimapService(SandboxManager.Instance?.AssetRegistry);
        }

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
            _minimapContent = root.Q("MinimapContent");
            _playerDot = root.Q("PlayerDot");
            _miniMapTarget = root.Q("MiniMapTarget");

            _mapPreviewOverlay = root.Q("MapPreviewOverlay");
            _mapPreviewFrame = root.Q("MapPreviewFrame");
            _previewContent = root.Q("MapPreviewContent");
            _mapPreviewPlayerDot = root.Q("MapPreviewPlayerDot");
            _mapPreviewTarget = root.Q("MapPreviewTarget");
            _mapPreviewTitle = root.Q<Label>("MapPreviewTitle");
            _mapPreviewCoords = root.Q<Label>("MapPreviewCoords");

            _caiBangSkillPanel = root.Q("CaiBangSkillPanel");
            _caiBangSkillClose = root.Q("CaiBangSkillClose");
            _caiBangSkillList = root.Q<ScrollView>("CaiBangSkillList");
            _caiBangSkillPageOne = root.Q("CaiBangSkillPageOne");
            _caiBangSkillPageTwo = root.Q("CaiBangSkillPageTwo");
            _caiBangSkillSummary = root.Q<Label>("CaiBangSkillSummary");

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

            RegisterPreviewOpen(root, "MinimapPanel");
            RegisterPreviewOpen(root, "MinimapFrame");
            RegisterPreviewOpen(root, "MinimapContent");
            RegisterPreviewOpen(root, "PlayerDot");
            RegisterPreviewOpen(root, "ToggleMapBtn");
            RegisterPreviewOpen(root, "WorldMapBtn");
            RegisterClick(root, "MapPreviewClose", CloseMapPreview);
            RegisterClick(root, "CaiBangSkillClose", CloseCaiBangSkillPanel);
            RegisterClick(root, "CaiBangSkillPageOne", () => SetCaiBangSkillPage(0));
            RegisterClick(root, "CaiBangSkillPageTwo", () => SetCaiBangSkillPage(1));

            if (_caiBangSkillPanel != null)
                _caiBangSkillPanel.pickingMode = PickingMode.Position;
            if (_caiBangSkillList != null)
                _caiBangSkillList.pickingMode = PickingMode.Position;

            if (_mapPreviewOverlay != null)
            {
                _mapPreviewOverlay.pickingMode = PickingMode.Position;
                _mapPreviewOverlay.RegisterCallback<PointerDownEvent>(evt =>
                {
                    // Mobile change: any tap outside the actual preview map closes it.
                    if (_mapPreviewFrame == null || !_mapPreviewFrame.worldBound.Contains(evt.position))
                    {
                        CloseMapPreview();
                        evt.StopPropagation();
                    }
                });
            }

            if (_mapPreviewFrame != null)
            {
                _mapPreviewFrame.pickingMode = PickingMode.Position;
                _mapPreviewFrame.RegisterCallback<PointerDownEvent>(OnPreviewMapPointerDown);
            }

            _initialized = true;
        }

        private void RegisterPreviewOpen(VisualElement root, string name)
        {
            var el = root.Q(name);
            if (el == null) return;
            el.pickingMode = PickingMode.Position;
            el.RegisterCallback<PointerDownEvent>(evt =>
            {
                OpenMapPreview();
                evt.StopImmediatePropagation();
            });
        }

        private static void RegisterClick(VisualElement root, string name, System.Action cb)
        {
            var el = root.Q(name);
            if (el != null)
            {
                el.pickingMode = PickingMode.Position;
                el.RegisterCallback<PointerDownEvent>(evt =>
                {
                    cb();
                    evt.StopPropagation();
                });
            }
        }

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
            LoadCaiBangPanelArt(artPath);

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

                var sendIcon = root.Q("SendBtnIcon");
                if (sendIcon != null)
                    LoadIcon(sendIcon, artPath, "btn_chat_send");

                LoadIcon(_playerDot, artPath, "minimap_dot");
                LoadIcon(_mapPreviewPlayerDot, artPath, "minimap_dot");

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
            if (el == null) return;
            var png = System.IO.Path.Combine(artPath, name + ".png");
            if (!System.IO.File.Exists(png)) return;

            var tex = LoadTexture(png);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                el.style.backgroundImage = new StyleBackground(tex);
            }
        }

        /// <summary>Static version for use by CombatSkillSlotController.</summary>
        public static void LoadIconStatic(VisualElement el, string artPath, string name)
        {
            LoadIcon(el, artPath, name);
        }

        private void LoadCaiBangPanelArt(string artPath)
        {
            // Visual panel is rendered by PcHudVietnameseTextOverlay with PC art so it draws above nameplates.
        }

        private static void LoadElementImage(VisualElement el, string artPath, string name)
        {
            if (el == null) return;
            var png = System.IO.Path.Combine(artPath, name + ".png");
            if (!System.IO.File.Exists(png)) return;
            var tex = LoadTexture(png);
            if (tex != null)
                el.style.backgroundImage = new StyleBackground(tex);
        }

        private static Texture2D LoadTexture(string path)
        {
            var data = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (!tex.LoadImage(data)) return null;
            return tex;
        }

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
            if (_mapPreviewOverlay != null)
            {
                _mapPreviewOverlay.style.width = w;
                _mapPreviewOverlay.style.height = h;
            }
            if (_caiBangSkillPanel != null)
            {
                _caiBangSkillPanel.style.left = Mathf.Clamp(338f, 0f, Mathf.Max(0f, w - 205f));
                _caiBangSkillPanel.style.top = Mathf.Clamp(110f, 0f, Mathf.Max(0f, h - 376f));
            }
        }

        private void UpdateBarsAndMinimap()
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

            var viMapName = ToVietnameseMapName(snap.mapName);
            if (_sceneName != null) _sceneName.text = viMapName;
            if (_scenePos != null) _scenePos.text = FormatPcScenePos(snap.playerPosition);
            if (_mapPreviewTitle != null) _mapPreviewTitle.text = viMapName;

            EnsureMinimapTexture(snap);
            UpdateMinimapDots(snap);
        }

        private void EnsureMinimapTexture(HudSnapshot snap)
        {
            if (snap.activeMap == null)
                return;

            var renderer = SandboxManager.Instance != null ? SandboxManager.Instance.MapRenderer : null;
            if (renderer == null || !renderer.HasContent)
                return;

            if (_previewTexture == null || snap.mapId != _previewTextureMapId)
            {
                var full = RenderMapTexture(renderer.ContentBounds, 512);
                if (full != null)
                {
                    if (_previewTexture != null) Destroy(_previewTexture);
                    _previewTexture = full;
                    _previewTextureMapId = snap.mapId;
                    if (_previewContent != null)
                        _previewContent.style.backgroundImage = new StyleBackground(_previewTexture);
                }
            }

            bool needsMini = _minimapTexture == null
                || snap.mapId != _minimapTextureMapId
                || Vector2.Distance(_lastMinimapCenter, snap.playerPosition) > 128f;
            if (!needsMini)
                return;

            var miniBounds = BuildZoomedMinimapBounds(snap.activeMap, snap.playerPosition);
            var zoomed = RenderMapTexture(miniBounds, 256);
            if (zoomed == null)
                return;

            if (_minimapTexture != null) Destroy(_minimapTexture);
            _minimapTexture = zoomed;
            _minimapTextureMapId = snap.mapId;
            _lastMinimapCenter = snap.playerPosition;
            if (_minimapContent != null)
                _minimapContent.style.backgroundImage = new StyleBackground(_minimapTexture);
        }

        private Bounds BuildZoomedMinimapBounds(MapDefinition map, Vector2 center)
        {
            var rect = map?.sourceBoundsRect;
            if (rect == null || rect.width <= 0f || rect.height <= 0f)
                return new Bounds(center, new Vector3(2048f, 2048f, 1f));

            // PC small minimap is a zoomed nearby-place view, not the whole world map.
            // Keep a square world window centered on player and clamped inside active map bounds.
            const float span = 2048f;
            float half = span * 0.5f;
            float cx = Mathf.Clamp(center.x, rect.x + half, rect.x + rect.width - half);
            float cy = Mathf.Clamp(center.y, rect.y + half, rect.y + rect.height - half);
            return new Bounds(new Vector3(cx, cy, 0f), new Vector3(span, span, 1f));
        }

        private Texture2D RenderMapTexture(Bounds bounds, int size)
        {
            var camGo = new GameObject("HUD_MinimapCaptureCamera");
            camGo.hideFlags = HideFlags.HideAndDontSave;
            var cam = camGo.AddComponent<Camera>();
            var rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32);
            rt.filterMode = FilterMode.Point;
            try
            {
                cam.enabled = false;
                cam.orthographic = true;
                cam.transform.position = new Vector3(bounds.center.x, bounds.center.y, -1000f);
                cam.transform.rotation = Quaternion.identity;
                cam.projectionMatrix = Matrix4x4.Ortho(
                    -bounds.size.x * 0.5f, bounds.size.x * 0.5f,
                    -bounds.size.y * 0.5f, bounds.size.y * 0.5f,
                    0.1f, 5000f);
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.10f, 0.18f, 0.12f, 1f);
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 5000f;
                cam.cullingMask = ~0;
                cam.targetTexture = rt;
                cam.Render();

                var prev = RenderTexture.active;
                RenderTexture.active = rt;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.ReadPixels(new Rect(0, 0, size, size), 0, 0);
                tex.Apply(false, false);
                tex.filterMode = FilterMode.Point;
                RenderTexture.active = prev;
                return tex;
            }
            finally
            {
                cam.targetTexture = null;
                rt.Release();
                Destroy(rt);
                Destroy(camGo);
            }
        }

        private void UpdateMinimapDots(HudSnapshot snap)
        {
            if (snap.activeMap == null || _minimapService == null)
                return;

            SetZoomedDotFromWorld(_playerDot, snap.activeMap, snap.playerPosition, snap.playerPosition, new Vector2(128f, 128f), 10f);
            SetDotFromWorld(_mapPreviewPlayerDot, snap.activeMap, snap.playerPosition, PreviewSize(), 14f);

            if (_lastMoveTarget.HasValue)
            {
                SetZoomedDotFromWorld(_miniMapTarget, snap.activeMap, _lastMoveTarget.Value, snap.playerPosition, new Vector2(128f, 128f), 8f);
                SetDotFromWorld(_mapPreviewTarget, snap.activeMap, _lastMoveTarget.Value, PreviewSize(), 8f);
            }
        }

        private void SetDotFromWorld(VisualElement dot, MapDefinition map, Vector2 world, Vector2 size, float dotSize)
        {
            if (dot == null) return;
            var px = _minimapService.WorldToMinimapPixel(map, world, size);
            dot.style.left = px.x - dotSize * 0.5f;
            dot.style.top = px.y - dotSize * 0.5f;
        }

        private void SetZoomedDotFromWorld(VisualElement dot, MapDefinition map, Vector2 world, Vector2 center, Vector2 size, float dotSize)
        {
            if (dot == null) return;
            var b = BuildZoomedMinimapBounds(map, center);
            var temp = new MapDefinition
            {
                sourceBoundsRect = new RectDef
                {
                    x = b.min.x,
                    y = b.min.y,
                    width = b.size.x,
                    height = b.size.y,
                }
            };
            var px = _minimapService.WorldToMinimapPixel(temp, world, size);
            dot.style.left = px.x - dotSize * 0.5f;
            dot.style.top = px.y - dotSize * 0.5f;
        }

        private Vector2 PreviewSize()
        {
            if (_mapPreviewFrame != null && _mapPreviewFrame.resolvedStyle.width > 1f && _mapPreviewFrame.resolvedStyle.height > 1f)
                return new Vector2(_mapPreviewFrame.resolvedStyle.width, _mapPreviewFrame.resolvedStyle.height);
            return new Vector2(500f, 500f);
        }

        private static string FormatPcScenePos(Vector2 world)
            => $"{Mathf.FloorToInt(world.x / 8f)}/{Mathf.FloorToInt(-world.y / 8f)}";

        private static string ToVietnameseMapName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Bản đồ";
            return raw switch
            {
                "巴陵县" => "Ba Lăng huyện",
                "Map_79" => "Ba Lăng huyện",
                _ => raw,
            };
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

        private void OpenMapPreview()
        {
            if (_mapPreviewOverlay == null) return;
            _mapPreviewOverlay.RemoveFromClassList("hidden");
            if (_mapPreviewCoords != null)
                _mapPreviewCoords.text = _lastMoveTarget.HasValue
                    ? $"Mục tiêu: {FormatPcScenePos(_lastMoveTarget.Value)}"
                    : "Chọn điểm đến";
        }

        private void CloseMapPreview()
        {
            if (_mapPreviewOverlay == null) return;
            _mapPreviewOverlay.AddToClassList("hidden");
        }

        private void OnPreviewMapPointerDown(PointerDownEvent evt)
        {
            if (_bridge == null || _minimapService == null)
                return;

            var snap = _bridge.BuildSnapshot();
            if (!snap.valid || snap.activeMap == null)
                return;

            var eventPos = new Vector2(evt.position.x, evt.position.y);
            var local = _mapPreviewFrame.WorldToLocal(eventPos);
            var size = PreviewSize();
            var target = _minimapService.MinimapPixelToWorld(snap.activeMap, local, size);
            MovePlayerTo(target);
            _lastMoveTarget = target;

            if (_mapPreviewCoords != null)
                _mapPreviewCoords.text = $"Đến: {FormatPcScenePos(target)}";
            UpdateMinimapDots(snap);
            CloseMapPreview();
            evt.StopPropagation();
        }

        private void MovePlayerTo(Vector2 worldTarget)
        {
            var player = SandboxManager.Instance != null ? SandboxManager.Instance.PlayerController : FindObjectOfType<SandboxPlayerController>();
            if (player == null)
            {
                SubsystemLog.Warn("HUD", $"Map preview target {worldTarget} ignored: no player");
                return;
            }

            player.MoveTo(worldTarget);
            SubsystemLog.Info("HUD", $"Map preview move target {worldTarget} ({FormatPcScenePos(worldTarget)})");
        }

        public bool IsCaiBangSkillPanelVisible => _caiBangSkillPanel != null && !_caiBangSkillPanel.ClassListContains("hidden");

        public int CaiBangSkillPanelRowCount => _caiBangSkillList?.childCount ?? 0;

        public CaiBangSkillPanelSnapshot CurrentCaiBangSkillSnapshot { get; private set; }

        public int CurrentCaiBangSelectedSkillId { get; private set; }

        public void OpenCaiBangSkillPanel()
        {
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null
                ? manager.CombatSkillCatalog
                : PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : new PlayerProgressionState();
            if (manager != null)
            {
                manager.GrantCaiBangSkillPanelProgression();
                progression = manager.PlayerProgression;
            }
            else
            {
                progression.GrantCaiBangSkillPanelProgression(catalog);
            }

            var snap = CaiBangSkillPanelService.BuildPage(catalog, progression, CurrentCaiBangSelectedSkillId, _caiBangSkillPageIndex);
            CurrentCaiBangSkillSnapshot = snap;
            PopulateCaiBangSkillPanel(snap);
            _caiBangSkillPanel?.RemoveFromClassList("hidden");
            CloseMapPreview();
            SubsystemLog.Info("HUD", $"Open Cái Bang Skills page {_caiBangSkillPageIndex + 1} (level={snap.playerLevel}, points={snap.skillPoints}, skills={snap.rows.Count})");
        }

        public int CurrentCaiBangSkillPageIndex => _caiBangSkillPageIndex;

        public void SetCaiBangSkillPage(int pageIndex)
        {
            pageIndex = Mathf.Clamp(pageIndex, 0, CaiBangSkillPanelService.PcFightSkillPageCount - 1);
            if (_caiBangSkillPageIndex == pageIndex && CurrentCaiBangSkillSnapshot != null)
                return;
            _caiBangSkillPageIndex = pageIndex;
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null ? manager.CombatSkillCatalog : PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : new PlayerProgressionState();
            if (manager != null)
            {
                manager.GrantCaiBangSkillPanelProgression();
                progression = manager.PlayerProgression;
            }
            else
            {
                progression.GrantCaiBangSkillPanelProgression(catalog);
            }
            CurrentCaiBangSkillSnapshot = CaiBangSkillPanelService.BuildPage(catalog, progression, CurrentCaiBangSelectedSkillId, _caiBangSkillPageIndex);
            PopulateCaiBangSkillPanel(CurrentCaiBangSkillSnapshot);
            SubsystemLog.Info("HUD", $"Switch Cái Bang Skills to page {_caiBangSkillPageIndex + 1}");
        }

        public void CloseCaiBangSkillPanel()
        {
            _caiBangSkillPanel?.AddToClassList("hidden");
        }

        private void PopulateCaiBangSkillPanel(CaiBangSkillPanelSnapshot snap)
        {
            if (_caiBangSkillSummary != null)
                _caiBangSkillSummary.text = snap.skillPoints.ToString();
            if (_caiBangSkillList == null)
                return;
            _caiBangSkillList.Clear();
            _caiBangSkillPageOne?.EnableInClassList("hud-cb-page-tab-active", _caiBangSkillPageIndex == 0);
            _caiBangSkillPageTwo?.EnableInClassList("hud-cb-page-tab-active", _caiBangSkillPageIndex == 1);
            _caiBangSkillList.contentContainer.style.flexDirection = FlexDirection.Row;
            _caiBangSkillList.contentContainer.style.flexWrap = Wrap.Wrap;
            _caiBangSkillList.contentContainer.style.alignContent = Align.FlexStart;
            for (int slotIndex = 0; slotIndex < CaiBangSkillPanelService.PcFightSkillSlotsPerPage; slotIndex++)
            {
                var item = new VisualElement();
                item.AddToClassList("hud-cb-grid-cell");
                item.pickingMode = PickingMode.Position;

                var slot = new VisualElement();
                slot.AddToClassList("hud-cb-grid-slot");
                item.Add(slot);

                if (slotIndex < snap.rows.Count)
                {
                    var row = snap.rows[slotIndex];
                    if (row.canUpgrade)
                        item.AddToClassList("hud-cb-grid-cell-upgradable");
                    LoadIcon(slot, System.IO.Path.Combine(Application.dataPath, artFolder, "Generated"), $"cai_bang_skill_{row.skillId}");

                    var levelText = row.learnedLevel > 0 ? row.learnedLevel.ToString() : string.Empty;
                    var level = new Label(levelText);
                    level.AddToClassList("hud-cb-grid-level");
                    item.Add(level);

                    var add = new VisualElement();
                    add.AddToClassList("hud-cb-add-point");
                    item.Add(add);

                    var name = new Label(row.displayName);
                    name.AddToClassList("hud-cb-grid-name");
                    item.Add(name);

                    int skillId = row.skillId;
                    item.RegisterCallback<PointerDownEvent>(evt =>
                    {
                        SelectCaiBangSkill(skillId);
                        evt.StopPropagation();
                    });
                }
                else
                {
                    item.AddToClassList("hud-cb-grid-cell-empty");
                    slot.AddToClassList("hud-cb-grid-slot-empty");
                }

                _caiBangSkillList.Add(item);
            }
        }

        public void SelectCaiBangSkill(int skillId)
        {
            CurrentCaiBangSelectedSkillId = CurrentCaiBangSelectedSkillId == skillId ? 0 : skillId;
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null ? manager.CombatSkillCatalog : PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : null;
            if (progression == null)
                return;

            CurrentCaiBangSkillSnapshot = CaiBangSkillPanelService.BuildPage(catalog, progression, CurrentCaiBangSelectedSkillId, _caiBangSkillPageIndex);
            PopulateCaiBangSkillPanel(CurrentCaiBangSkillSnapshot);
            SubsystemLog.Info("HUD", CurrentCaiBangSelectedSkillId != 0 ? $"Select Cái Bang skill {skillId}" : $"Hide Cái Bang skill detail {skillId}");
        }

        public bool TryUpgradeCaiBangSelectedSkill()
        {
            return CurrentCaiBangSelectedSkillId != 0 && TryUpgradeCaiBangSkill(CurrentCaiBangSelectedSkillId);
        }

        public bool TryUpgradeCaiBangSkill(int skillId)
        {
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null ? manager.CombatSkillCatalog : PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : null;
            if (progression == null)
                return false;

            bool upgraded = CaiBangSkillPanelService.TryUpgrade(progression, catalog, skillId);
            if (upgraded)
            {
                CurrentCaiBangSkillSnapshot = CaiBangSkillPanelService.BuildPage(catalog, progression, CurrentCaiBangSelectedSkillId, _caiBangSkillPageIndex);
                PopulateCaiBangSkillPanel(CurrentCaiBangSkillSnapshot);
            }
            SubsystemLog.Info("HUD", upgraded ? $"Upgrade Cái Bang skill {skillId}" : $"Cannot upgrade Cái Bang skill {skillId}");
            return upgraded;
        }

        private void OnRunClick() => SubsystemLog.Info("HUD", "Toggle Run/Walk");
        private void OnSitClick() => SubsystemLog.Info("HUD", "Toggle Sit");
        private void OnHorseClick() => SubsystemLog.Info("HUD", "Toggle Horse");
        private void OnStatusClick() => SubsystemLog.Info("HUD", "Open Character Status");
        private void OnItemsClick() => SubsystemLog.Info("HUD", "Open Inventory");
        private void OnSkillsClick() => OpenCaiBangSkillPanel();
        private void OnTeamClick() => SubsystemLog.Info("HUD", "Open Team");
        private void OnFactionClick() => SubsystemLog.Info("HUD", "Open Faction");
        private void OnPKClick() => SubsystemLog.Info("HUD", "Toggle PK");
        private void OnExchangeClick() => SubsystemLog.Info("HUD", "Open Exchange");
    }
}
