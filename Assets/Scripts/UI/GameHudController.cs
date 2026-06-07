// -----------------------------------------------------------------------------
// VLTK Mobile — Game HUD Controller
// Loads real SPR art from PC source and wires to UI Toolkit elements.
// PC reference: 顶部控制条.ini, 玩家信息主界面.ini, 工具控制条.ini, 小地图_小.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;
using VLTK.Core;
using VLTK.UI;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.UI
{
    /// <summary>Resolves HUD art paths from the StreamingAssets root so Editor and player builds use the same source.</summary>
    public static class HudArtPathResolver
    {
        public const string GeneratedFolderName = "Generated";

        public static string ResolveArtRoot(string artFolder)
            => ResolveUnderStreamingAssets(Application.streamingAssetsPath, artFolder);

        public static string ResolveGeneratedArtRoot(string artFolder)
            => CombineStreamingPath(ResolveArtRoot(artFolder), GeneratedFolderName);

        public static string ResolveUnderStreamingAssets(string streamingAssetsPath, string relativeFolder)
        {
            var root = streamingAssetsPath ?? string.Empty;
            var normalizedFolder = NormalizeRelativeFolder(relativeFolder);
            return string.IsNullOrEmpty(normalizedFolder)
                ? root
                : CombineStreamingPath(root, normalizedFolder);
        }

        public static string ResolvePngPath(string artRoot, string iconName)
            => CombineStreamingPath(artRoot, iconName + ".png");

        public static string ResolveUserFacingPngPath(string artRoot, string iconName)
            => ResolvePngPath(artRoot, HudUserFacingArtCatalog.ResolveVietnameseArtName(iconName));

        public static bool CanCheckDirectory(string path)
            => !string.IsNullOrEmpty(path) && !RequiresUnityWebRequest(path);

        public static bool RequiresUnityWebRequest(string path)
            => !string.IsNullOrEmpty(path)
               && (path.Contains("://") || path.StartsWith("jar:", System.StringComparison.OrdinalIgnoreCase));

        public static string ToUnityWebRequestUri(string path)
        {
            if (RequiresUnityWebRequest(path))
                return path;

            var fullPath = System.IO.Path.GetFullPath(path).Replace('\\', '/');
            return "file://" + (fullPath.StartsWith("/", System.StringComparison.Ordinal) ? fullPath : "/" + fullPath);
        }

        private static string NormalizeRelativeFolder(string folder)
            => (folder ?? string.Empty).Trim().Trim('/', '\\');

        private static string CombineStreamingPath(string root, string relative)
        {
            if (string.IsNullOrEmpty(root))
                return NormalizeRelativeFolder(relative).Replace('\\', '/');

            var normalizedRelative = NormalizeRelativeFolder(relative).Replace('\\', '/');
            if (string.IsNullOrEmpty(normalizedRelative))
                return root;

            if (root.EndsWith("/", System.StringComparison.Ordinal) || root.EndsWith("\\", System.StringComparison.Ordinal))
                return root + normalizedRelative;

            return RequiresUnityWebRequest(root)
                ? root + "/" + normalizedRelative
                : System.IO.Path.Combine(root, normalizedRelative);
        }
    }

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
        private VisualElement _skillPanel, _skillClose, _skillPageOne, _skillPageTwo;
        private ScrollView _skillList;
        private Label _hpText, _mpText, _staminaText, _expText;
        private Label _levelText, _sceneName, _scenePos, _mapPreviewTitle, _mapPreviewCoords, _skillSummary;
        private TextField _chatInput;

        // New HUD elements
        private VisualElement _buffPanel;
        private VisualElement _teamPreview;
        private VisualElement _tradeInfoPanel, _tradeInfoClose;
        private Label _tradePartnerName, _tradePartnerLevel, _tradePartnerFaction, _tradePartnerGuild;
        private VisualElement _stallCurrencySelector;
        private Button _stallMoneyBtn, _stallCoinBtn;
        private VisualElement _facePickerOverlay, _facePickerClose;
        private ScrollView _facePickerList;
        private Button _faceBtn;

        // Inventory window (Hành Trang)
        private VisualElement _invWindow, _invClose, _invFrame;
        private ScrollView _invGrid;
        private Label _invMoney;


        private HudDataBridge _bridge;
        private MinimapService _minimapService;
        private bool _initialized;
        private SprRuntimeService _sprService;
        private Texture2D _minimapTexture;
        private Texture2D _previewTexture;
        private int _minimapTextureMapId = -1;
        private int _previewTextureMapId = -1;
        private int _skillPageIndex;
        private Vector2 _lastMinimapCenter;
        private Vector2? _lastMoveTarget;

        // Button name → SPR icon file mapping (matching PC 按钮条按钮/*.spr)
        private static readonly Dictionary<string, string> ButtonIcons = new()
        {
            // Round-action icons (run/sit/horse/exchange) are authentic pixels
            // cropped directly from the PC client screenshot pc_hud.png — the
            // button SPRs do not exist in any distributed PAK/manifest, so the
            // PC screen itself is the only authentic source. No flip needed
            // (already upright, unlike SPR-decoded icons).
            { "BtnRun", "btn_run" },
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

            _skillPanel = root.Q("CaiBangSkillPanel");
            _skillClose = root.Q("CaiBangSkillClose");
            _skillList = root.Q<ScrollView>("CaiBangSkillList");
            _skillPageOne = root.Q("CaiBangSkillPageOne");
            _skillPageTwo = root.Q("CaiBangSkillPageTwo");
            _skillSummary = root.Q<Label>("CaiBangSkillSummary");

            // Bind new HUD panels
            _buffPanel = root.Q("BuffPanel");
            _teamPreview = root.Q("TeamPreview");

            _tradeInfoPanel = root.Q("TradeInfoPanel");
            _tradeInfoClose = root.Q("TradeInfoClose");
            _tradePartnerName = root.Q<Label>("TradePartnerName");
            _tradePartnerLevel = root.Q<Label>("TradePartnerLevel");
            _tradePartnerFaction = root.Q<Label>("TradePartnerFaction");
            _tradePartnerGuild = root.Q<Label>("TradePartnerGuild");

            _stallCurrencySelector = root.Q("StallCurrencySelector");
            _stallMoneyBtn = root.Q<Button>("StallMoneyBtn");
            _stallCoinBtn = root.Q<Button>("StallCoinBtn");

            _facePickerOverlay = root.Q("FacePickerOverlay");
            _facePickerClose = root.Q("FacePickerClose");
            _facePickerList = root.Q<ScrollView>("FacePickerList");
            _faceBtn = root.Q<Button>("FaceBtn");

            _invWindow = root.Q("InventoryWindow");
            _invFrame = root.Q("InventoryFrame");
            _invClose = root.Q("InventoryClose");
            _invGrid = root.Q<ScrollView>("InventoryGrid");
            _invMoney = root.Q<Label>("InventoryMoney");

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
            RegisterClick(root, "BtnTreasure", OnTreasureClick);

            if (_faceBtn != null)
            {
                _faceBtn.pickingMode = PickingMode.Position;
                _faceBtn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    OpenFacePicker();
                    evt.StopPropagation();
                });
            }
            if (_facePickerClose != null)
            {
                _facePickerClose.pickingMode = PickingMode.Position;
                _facePickerClose.RegisterCallback<PointerDownEvent>(evt =>
                {
                    CloseFacePicker();
                    evt.StopPropagation();
                });
            }
            if (_tradeInfoClose != null)
            {
                _tradeInfoClose.pickingMode = PickingMode.Position;
                _tradeInfoClose.RegisterCallback<PointerDownEvent>(evt =>
                {
                    CloseTradeInfo();
                    evt.StopPropagation();
                });
            }
            if (_stallMoneyBtn != null)
            {
                _stallMoneyBtn.pickingMode = PickingMode.Position;
                _stallMoneyBtn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    SelectStallCurrency("Bạch Ngân");
                    evt.StopPropagation();
                });
            }
            if (_stallCoinBtn != null)
            {
                _stallCoinBtn.pickingMode = PickingMode.Position;
                _stallCoinBtn.RegisterCallback<PointerDownEvent>(evt =>
                {
                    SelectStallCurrency("Đồng Xu");
                    evt.StopPropagation();
                });
            }

            RegisterPreviewOpen(root, "MinimapPanel");
            RegisterPreviewOpen(root, "MinimapFrame");
            RegisterPreviewOpen(root, "MinimapContent");
            RegisterPreviewOpen(root, "PlayerDot");
            RegisterPreviewOpen(root, "ToggleMapBtn");
            RegisterPreviewOpen(root, "WorldMapBtn");
            RegisterClick(root, "MapPreviewClose", CloseMapPreview);
            RegisterClick(root, "CaiBangSkillClose", CloseSkillPanel);
            RegisterClick(root, "CaiBangSkillPageOne", () => SetSkillPage(0));
            RegisterClick(root, "CaiBangSkillPageTwo", () => SetSkillPage(1));

            RegisterClick(root, "InventoryClose", CloseInventory);
            if (_invGrid != null)
                _invGrid.pickingMode = PickingMode.Position;
            if (_invWindow != null)
            {
                // Tap outside the frame closes the window (PC closes on CloseBtn; mobile adds tap-outside).
                _invWindow.pickingMode = PickingMode.Position;
                _invWindow.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (_invFrame == null || !_invFrame.worldBound.Contains(evt.position))
                    {
                        CloseInventory();
                        evt.StopPropagation();
                    }
                });
            }
            if (_invFrame != null)
                _invFrame.pickingMode = PickingMode.Position;

            if (_skillPanel != null)
                _skillPanel.pickingMode = PickingMode.Position;
            if (_skillList != null)
                _skillList.pickingMode = PickingMode.Position;

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
            var artPath = HudArtPathResolver.ResolveArtRoot(artFolder);
            if (HudArtPathResolver.CanCheckDirectory(artPath) && !System.IO.Directory.Exists(artPath))
            {
                SubsystemLog.Warn("HUD", $"Art folder not found: {artPath}");
                return;
            }

            LoadBarArt(_hpFill, artPath, "bar_hp_fill");
            LoadBarArt(_mpFill, artPath, "bar_mp_fill");
            LoadBarArt(_staminaFill, artPath, "bar_stamina_fill");
            LoadBarArt(_expFill, artPath, "bar_exp_fill");
            LoadPanelArt(artPath);

            var doc = GetComponent<UIDocument>();
            var root = doc.rootVisualElement.Q("GameHud");
            if (root != null)
            {
                UnityEngine.Debug.Log("[HUD] Debug: root is NOT null!");
                foreach (var kv in ButtonIcons)
                {
                    var btn = root.Q(kv.Key);
                    if (btn == null) { UnityEngine.Debug.Log($"[HUD] Debug: btn {kv.Key} is null"); continue; }
                    var icon = btn.Q(kv.Key + "Icon");
                    if (icon == null) { UnityEngine.Debug.Log($"[HUD] Debug: icon {kv.Key}Icon is null"); continue; }
                    UnityEngine.Debug.Log($"[HUD] Debug: loading icon for {kv.Key}");
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
            else
            {
                UnityEngine.Debug.LogError("[HUD] Debug: root IS NULL!");
            }
        }

        private void LoadBarArt(VisualElement fill, string artPath, string name)
        {
            if (fill == null) return;
            var png = HudArtPathResolver.ResolvePngPath(artPath, name);
            LoadTextureIntoElement(this, png, name, tex =>
            {
                fill.style.backgroundImage = new StyleBackground(tex);
                // Fill the full track (166x16) so the authentic SPR covers the
                // dark socket fully; clip-by-percent still works via the track's
                // overflow:hidden + the fill element's percentage width.
                fill.style.backgroundSize = new BackgroundSize(166, 16);
            });
        }

        private void LoadIcon(VisualElement el, string artPath, string name)
        {
            LoadIcon(this, el, artPath, name);
        }

        private static void LoadIcon(MonoBehaviour coroutineHost, VisualElement el, string artPath, string name)
        {
            if (el == null)
            {
                UnityEngine.Debug.LogWarning($"[HUD] LoadIcon: element for {name} is null");
                return;
            }

            var png = HudArtPathResolver.ResolveUserFacingPngPath(artPath, name);
            if (coroutineHost != null)
            {
                LoadTextureIntoElement(coroutineHost, png, name, tex =>
                {
                    el.style.backgroundImage = new StyleBackground(tex);
                    UnityEngine.Debug.Log($"[HUD] LoadIcon: successfully loaded {name} ({tex.width}x{tex.height}) onto {el.name}");
                });
                return;
            }

            LoadTextureIntoElement(null, png, name, tex =>
            {
                el.style.backgroundImage = new StyleBackground(tex);
                UnityEngine.Debug.Log($"[HUD] LoadIcon: successfully loaded {name} ({tex.width}x{tex.height}) onto {el.name}");
            });
        }

        /// <summary>Static version for use by CombatSkillSlotController.</summary>
        public static void LoadIconStatic(MonoBehaviour coroutineHost, VisualElement el, string artPath, string name)
        {
            LoadIcon(coroutineHost, el, artPath, name);
        }

        /// <summary>Legacy synchronous entry point. Prefer passing a MonoBehaviour for StreamingAssets/mobile paths.</summary>
        public static void LoadIconStatic(VisualElement el, string artPath, string name)
        {
            LoadIcon(null, el, artPath, name);
        }

        private void LoadPanelArt(string artPath)
        {
            // Visual panel is rendered by PcHudVietnameseTextOverlay with PC art so it draws above nameplates.
        }

        private void LoadElementImage(VisualElement el, string artPath, string name)
        {
            if (el == null) return;
            var png = HudArtPathResolver.ResolveUserFacingPngPath(artPath, name);
            LoadTextureIntoElement(this, png, name, tex => el.style.backgroundImage = new StyleBackground(tex));
        }

        private static void LoadTextureIntoElement(MonoBehaviour coroutineHost, string path, string name, System.Action<Texture2D> apply)
        {
            if (HudArtPathResolver.RequiresUnityWebRequest(path))
            {
                if (coroutineHost == null)
                {
                    UnityEngine.Debug.LogWarning($"[HUD] LoadTexture: {name} requires UnityWebRequest but no coroutine host was provided: {path}");
                    return;
                }

                coroutineHost.StartCoroutine(LoadTextureIntoElementAsync(path, name, apply));
                return;
            }

            var tex = LoadTextureFromLocalFile(path);
            if (tex != null)
                apply(tex);
        }

        private static System.Collections.IEnumerator LoadTextureIntoElementAsync(string path, string name, System.Action<Texture2D> apply)
        {
            using var request = UnityWebRequestTexture.GetTexture(HudArtPathResolver.ToUnityWebRequestUri(path));
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogWarning($"[HUD] LoadTexture: failed to load {name} from {path}: {request.error}");
                yield break;
            }

            var downloaded = DownloadHandlerTexture.GetContent(request);
            if (downloaded != null)
            {
                downloaded.filterMode = FilterMode.Point;
                apply(downloaded);
            }
        }

        private static Texture2D LoadTextureFromLocalFile(string path)
        {
            if (!System.IO.File.Exists(path))
            {
                UnityEngine.Debug.LogWarning($"[HUD] LoadTexture: file not found {path}");
                return null;
            }

            var data = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            if (!tex.LoadImage(data)) 
            {
                UnityEngine.Debug.LogError($"[HUD] LoadTexture: LoadImage failed for {path}");
                return null;
            }
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
            if (_skillPanel != null)
            {
                _skillPanel.style.left = Mathf.Clamp(338f, 0f, Mathf.Max(0f, w - 205f));
                _skillPanel.style.top = Mathf.Clamp(110f, 0f, Mathf.Max(0f, h - 376f));
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
                SetBar(_expFill, _expText, 0, 100, true);
                return;
            }

            SetLevel(snap.level);
            SetBar(_hpFill, _hpText, snap.currentLife, snap.maxLife);
            SetBar(_mpFill, _mpText, 50, 50);
            SetBar(_staminaFill, _staminaText, 100, 100);
            SetBar(_expFill, _expText, 0, 100, true);

            var viMapName = ToVietnameseMapName(snap.mapName);
            if (_sceneName != null) _sceneName.text = viMapName;
            if (_scenePos != null) _scenePos.text = FormatPcScenePos(snap.playerPosition);
            if (_mapPreviewTitle != null) _mapPreviewTitle.text = viMapName;

            EnsureMinimapTexture(snap);
            UpdateMinimapDots(snap);
            UpdateBuffs();
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

            // Override dot colors from settings config
            if (_playerDot != null)
            {
                Color playerColor = HudDataService.Instance.GetMapColor("SelfPlayerColor", Color.yellow);
                _playerDot.style.unityBackgroundImageTintColor = playerColor;
            }
            if (_mapPreviewPlayerDot != null)
            {
                Color playerColor = HudDataService.Instance.GetMapColor("SelfPlayerColor", Color.yellow);
                _mapPreviewPlayerDot.style.unityBackgroundImageTintColor = playerColor;
            }
            if (_miniMapTarget != null)
            {
                Color targetColor = HudDataService.Instance.GetMapColor("SelfColor", new Color(1f, 0.9f, 0.15f));
                _miniMapTarget.style.backgroundColor = targetColor;
            }
            if (_mapPreviewTarget != null)
            {
                Color targetColor = HudDataService.Instance.GetMapColor("SelfColor", new Color(1f, 0.9f, 0.15f));
                _mapPreviewTarget.style.backgroundColor = targetColor;
            }

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

        private static void SetBar(VisualElement fill, Label text, int cur, int max, bool isExp = false)
        {
            if (fill != null)
            {
                float frac = max > 0 ? Mathf.Clamp01((float)cur / max) : 0f;
                fill.style.width = Length.Percent(frac * 100f);
            }
            if (text != null)
            {
                if (isExp)
                {
                    float pct = max > 0 ? ((float)cur / max) * 100f : 0f;
                    text.text = $"{Mathf.RoundToInt(pct)}%";
                }
                else
                {
                    text.text = $"{cur}/{max}";
                }
            }
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

        public bool IsSkillPanelVisible => _skillPanel != null && !_skillPanel.ClassListContains("hidden");

        public int PcSkillPanelRowCount => _skillList?.childCount ?? 0;

        public PcSkillPanelSnapshot CurrentSkillSnapshot { get; private set; }

        public int CurrentSelectedSkillId { get; private set; }

        private string GetFactionNameVi(CombatFaction faction)
        {
            return faction switch
            {
                CombatFaction.Shaolin => "Thiếu Lâm",
                CombatFaction.TianWang => "Thiên Vương",
                CombatFaction.TangMen => "Đường Môn",
                CombatFaction.CaiBang => "Cái Bang",
                CombatFaction.WuDu => "Ngũ Độc",
                CombatFaction.TianRen => "Thiên Nhẫn",
                CombatFaction.EMei => "Nga Mi",
                CombatFaction.CuiYan => "Thúy Yên",
                CombatFaction.WuDang => "Võ Đang",
                CombatFaction.KunLun => "Côn Lôn",
                _ => "Vô Phái"
            };
        }

        public void OpenSkillPanel()
        {
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null
                ? manager.CombatSkillCatalog
                : PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : new PlayerProgressionState();
            CombatFaction faction = progression.faction != CombatFaction.None ? progression.faction : CombatFaction.CaiBang;
            if (manager != null)
            {
                manager.GrantFactionSkillPanelProgression(faction);
                progression = manager.PlayerProgression;
            }
            else
            {
                progression.GrantFactionSkillPanelProgression(catalog, faction);
            }

            var snap = PcSkillPanelService.BuildPage(catalog, progression, CurrentSelectedSkillId, _skillPageIndex);
            CurrentSkillSnapshot = snap;
            PopulateSkillPanel(snap);
            _skillPanel?.RemoveFromClassList("hidden");
            CloseMapPreview();
            SubsystemLog.Info("HUD", $"Open {GetFactionNameVi(faction)} Skills page {_skillPageIndex + 1} (level={snap.playerLevel}, points={snap.skillPoints}, skills={snap.rows.Count})");
        }

        public int CurrentSkillPageIndex => _skillPageIndex;

        public void SetSkillPage(int pageIndex)
        {
            pageIndex = Mathf.Clamp(pageIndex, 0, PcSkillPanelService.PcFightSkillPageCount - 1);
            if (_skillPageIndex == pageIndex && CurrentSkillSnapshot != null)
                return;
            _skillPageIndex = pageIndex;
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null ? manager.CombatSkillCatalog : PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : new PlayerProgressionState();
            CombatFaction faction = progression.faction != CombatFaction.None ? progression.faction : CombatFaction.CaiBang;
            if (manager != null)
            {
                manager.GrantFactionSkillPanelProgression(faction);
                progression = manager.PlayerProgression;
            }
            else
            {
                progression.GrantFactionSkillPanelProgression(catalog, faction);
            }
            CurrentSkillSnapshot = PcSkillPanelService.BuildPage(catalog, progression, CurrentSelectedSkillId, _skillPageIndex);
            PopulateSkillPanel(CurrentSkillSnapshot);
            SubsystemLog.Info("HUD", $"Switch {GetFactionNameVi(faction)} Skills to page {_skillPageIndex + 1}");
        }

        public void CloseSkillPanel()
        {
            _skillPanel?.AddToClassList("hidden");
        }

        // ── Inventory window (Hành Trang) — PC 物品 / Open([[items]]) ──────────

        public bool IsInventoryVisible => _invWindow != null && !_invWindow.ClassListContains("hidden");

        public void ToggleInventory()
        {
            if (IsInventoryVisible) CloseInventory();
            else OpenInventory();
        }

        public void OpenInventory()
        {
            var manager = SandboxManager.Instance;
            var inventory = manager != null ? manager.InventoryService : null;
            int playerId = manager != null && manager.PlayerProgression != null ? 1 : 0;
            var snap = InventoryPanelService.BuildGridSnapshot(inventory, playerId);
            PopulateInventory(snap);
            _invWindow?.RemoveFromClassList("hidden");
            CloseSkillPanel();
            CloseMapPreview();
            SubsystemLog.Info("HUD", $"Open Inventory (slots={snap.totalSlots}, used={snap.usedSlots})");
        }

        public void CloseInventory()
        {
            _invWindow?.AddToClassList("hidden");
            SubsystemLog.Info("HUD", "Close Inventory");
        }

        public int InventorySlotCount => _invGrid?.contentContainer.childCount ?? 0;

        public void PopulateInventory(InventoryPanelSnapshot snap)
        {
            if (_invMoney != null)
                _invMoney.text = $"Bạc: {(snap != null ? snap.silver : 0)}";
            if (_invGrid == null) return;
            _invGrid.Clear();
            _invGrid.contentContainer.style.flexDirection = FlexDirection.Row;
            _invGrid.contentContainer.style.flexWrap = Wrap.Wrap;
            _invGrid.contentContainer.style.alignContent = Align.FlexStart;

            var rows = snap?.rows;
            int count = rows != null ? rows.Count : InventoryPanelService.GridSlotCount;
            for (int i = 0; i < count; i++)
            {
                var slot = new VisualElement { name = $"InvSlot{i}" };
                slot.AddToClassList("hud-inv-slot");

                var icon = new VisualElement { name = $"InvSlotIcon{i}" };
                icon.AddToClassList("hud-inv-slot-icon");
                slot.Add(icon);

                if (rows != null && i < rows.Count)
                {
                    var r = rows[i];
                    if (r.itemId != 0)
                    {
                        var c = InventoryWindowPcSpec.TierColor(r.itemQuality);
                        slot.style.borderTopColor = slot.style.borderBottomColor =
                            slot.style.borderLeftColor = slot.style.borderRightColor =
                            new StyleColor(new Color(c.r / 255f, c.g / 255f, c.b / 255f));

                        if (r.count > 1)
                        {
                            var countLabel = new Label(r.count.ToString());
                            countLabel.AddToClassList("hud-inv-slot-count");
                            slot.Add(countLabel);
                        }
                    }
                }
                _invGrid.Add(slot);
            }
        }

        private void PopulateSkillPanel(PcSkillPanelSnapshot snap)
        {
            if (_skillSummary != null)
                _skillSummary.text = snap.skillPoints.ToString();
            if (_skillList == null)
                return;
            _skillList.Clear();
            _skillPageOne?.EnableInClassList("hud-cb-page-tab-active", _skillPageIndex == 0);
            _skillPageTwo?.EnableInClassList("hud-cb-page-tab-active", _skillPageIndex == 1);
            _skillList.contentContainer.style.flexDirection = FlexDirection.Row;
            _skillList.contentContainer.style.flexWrap = Wrap.Wrap;
            _skillList.contentContainer.style.alignContent = Align.FlexStart;
            for (int slotIndex = 0; slotIndex < PcSkillPanelService.PcFightSkillSlotsPerPage; slotIndex++)
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
                    LoadIcon(slot, HudArtPathResolver.ResolveGeneratedArtRoot(artFolder), $"cai_bang_skill_{row.skillId}");

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
                        SelectSkill(skillId);
                        evt.StopPropagation();
                    });
                }
                else
                {
                    item.AddToClassList("hud-cb-grid-cell-empty");
                    slot.AddToClassList("hud-cb-grid-slot-empty");
                }

                _skillList.Add(item);
            }
        }

        public void SelectSkill(int skillId)
        {
            CurrentSelectedSkillId = CurrentSelectedSkillId == skillId ? 0 : skillId;
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null ? manager.CombatSkillCatalog : PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : null;
            if (progression == null)
                return;

            CurrentSkillSnapshot = PcSkillPanelService.BuildPage(catalog, progression, CurrentSelectedSkillId, _skillPageIndex);
            PopulateSkillPanel(CurrentSkillSnapshot);
            CombatFaction faction = progression.faction;
            SubsystemLog.Info("HUD", CurrentSelectedSkillId != 0 ? $"Select {GetFactionNameVi(faction)} skill {skillId}" : $"Hide {GetFactionNameVi(faction)} skill detail {skillId}");
        }

        public bool TryUpgradeSelectedSkill()
        {
            return CurrentSelectedSkillId != 0 && TryUpgradeSkill(CurrentSelectedSkillId);
        }

        public bool TryUpgradeSkill(int skillId)
        {
            var manager = SandboxManager.Instance;
            SkillCatalog catalog = manager != null ? manager.CombatSkillCatalog : PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            PlayerProgressionState progression = manager != null ? manager.PlayerProgression : null;
            if (progression == null)
                return false;

            bool upgraded = PcSkillPanelService.TryUpgrade(progression, catalog, skillId);
            if (upgraded)
            {
                CurrentSkillSnapshot = PcSkillPanelService.BuildPage(catalog, progression, CurrentSelectedSkillId, _skillPageIndex);
                PopulateSkillPanel(CurrentSkillSnapshot);
            }
            CombatFaction faction = progression.faction;
            SubsystemLog.Info("HUD", upgraded ? $"Upgrade {GetFactionNameVi(faction)} skill {skillId}" : $"Cannot upgrade {GetFactionNameVi(faction)} skill {skillId}");
            return upgraded;
        }

        private void OnRunClick() => SubsystemLog.Info("HUD", "Toggle Run/Walk");
        private void OnSitClick() => SubsystemLog.Info("HUD", "Toggle Sit");
        private void OnHorseClick() => SubsystemLog.Info("HUD", "Toggle Horse");
        private void OnStatusClick() => SubsystemLog.Info("HUD", "Open Character Status");
        private void OnItemsClick() => ToggleInventory();
        private void OnSkillsClick() => OpenSkillPanel();

        private void OnTeamClick()
        {
            if (_teamPreview != null)
            {
                bool hide = !_teamPreview.ClassListContains("hidden");
                if (hide)
                    _teamPreview.AddToClassList("hidden");
                else
                {
                    _teamPreview.RemoveFromClassList("hidden");
                    PopulateTeamPreview();
                }
                SubsystemLog.Info("HUD", hide ? "Close Team" : "Open Team");
            }
        }

        private void OnFactionClick()
        {
            // Toggle StallCurrencySelector
            if (_stallCurrencySelector != null)
            {
                bool hide = !_stallCurrencySelector.ClassListContains("hidden");
                if (hide)
                    _stallCurrencySelector.AddToClassList("hidden");
                else
                    _stallCurrencySelector.RemoveFromClassList("hidden");
                SubsystemLog.Info("HUD", hide ? "Close Stall Currency" : "Open Stall Currency");
            }
        }

        private void OnPKClick() => SubsystemLog.Info("HUD", "Toggle PK");

        private void OnExchangeClick()
        {
            if (_tradeInfoPanel != null)
            {
                bool hide = !_tradeInfoPanel.ClassListContains("hidden");
                if (hide)
                    _tradeInfoPanel.AddToClassList("hidden");
                else
                {
                    _tradeInfoPanel.RemoveFromClassList("hidden");
                    PopulateTradeInfo();
                }
                SubsystemLog.Info("HUD", hide ? "Close Exchange" : "Open Exchange");
            }
        }

        private void OnTreasureClick() => SubsystemLog.Info("HUD", "Open Kỳ Trân Các / Bảo Vật");

        // ── New HUD logic ──────────────────────────────────────────────────

        public struct BuffSnapshot
        {
            public int skillId;
            public string nameVi;
            public float durationRemaining;
            public bool isDebuff;
        }

        private void UpdateBuffs()
        {
            if (_buffPanel == null) return;
            
            _buffPanel.Clear();
            
            var manager = SandboxManager.Instance;
            var loop = manager != null ? manager.GameplayLoop : null;
            var player = loop != null ? loop.Player : null;
            
            var buffsToShow = new List<BuffSnapshot>();
            
            if (player != null && player.combat != null && player.combat.states != null)
            {
                foreach (var kv in player.combat.states)
                {
                    int skillId = MapStateToSkillId(kv.Key);
                    if (skillId > 0)
                    {
                        buffsToShow.Add(new BuffSnapshot
                        {
                            skillId = skillId,
                            nameVi = GetSkillName(skillId),
                            isDebuff = IsDebuff(kv.Key),
                            durationRemaining = 30f
                        });
                    }
                }
            }
            
            if (buffsToShow.Count == 0)
            {
                buffsToShow.Add(new BuffSnapshot { skillId = 15, nameVi = GetSkillName(15), isDebuff = false, durationRemaining = 24.5f });
                buffsToShow.Add(new BuffSnapshot { skillId = 42, nameVi = GetSkillName(42), isDebuff = false, durationRemaining = 45.2f });
                buffsToShow.Add(new BuffSnapshot { skillId = 157, nameVi = GetSkillName(157), isDebuff = false, durationRemaining = 12.8f });
                buffsToShow.Add(new BuffSnapshot { skillId = 73, nameVi = GetSkillName(73), isDebuff = true, durationRemaining = 5.4f });
            }
            
            foreach (var b in buffsToShow)
            {
                var cell = new VisualElement();
                cell.AddToClassList("hud-buff-cell");
                
                var icon = new VisualElement();
                icon.AddToClassList("hud-buff-icon");
                icon.AddToClassList(b.isDebuff ? "hud-buff-border-orange" : "hud-buff-border-green");
                
                LoadIcon(icon, HudArtPathResolver.ResolveGeneratedArtRoot(artFolder), $"cai_bang_skill_{b.skillId}");
                cell.Add(icon);
                
                var timer = new Label(FormatTimer(b.durationRemaining));
                timer.AddToClassList(b.isDebuff ? "hud-debuff-timer" : "hud-buff-timer");
                cell.Add(timer);
                
                cell.tooltip = $"{b.nameVi}\nCòn lại: {FormatTimer(b.durationRemaining)}s";
                
                _buffPanel.Add(cell);
            }
        }
        
        private int MapStateToSkillId(MagicAttributeKind kind)
        {
            return kind switch
            {
                MagicAttributeKind.ColdResP => 109,
                MagicAttributeKind.PoisonResP => 123,
                MagicAttributeKind.MeleeDamageReturnP => 273,
                MagicAttributeKind.ConfuseP => 73,
                _ => 0
            };
        }
        
        private string GetSkillName(int skillId)
        {
            var buff = HudDataService.Instance.GetBuff(skillId);
            if (buff != null) return buff.name;

            return skillId switch
            {
                15 => "Bất Động Minh Vương",
                42 => "Kim Chung Trào",
                73 => "Vạn Cổ Thực Tâm",
                109 => "Tuyết Ảnh",
                123 => "Khuê Mộc Tinh Chiếu",
                157 => "Tọa Vong Vô Ngã",
                273 => "Như Lai Thiên Diệp",
                _ => "Hiệu ứng võ công"
            };
        }
        
        private bool IsDebuff(MagicAttributeKind kind)
        {
            return kind == MagicAttributeKind.ConfuseP || kind == MagicAttributeKind.PoisonDamageV;
        }
        
        private string FormatTimer(float seconds)
        {
            if (seconds >= 60f)
            {
                int min = Mathf.FloorToInt(seconds / 60f);
                int sec = Mathf.FloorToInt(seconds % 60f);
                return $"{min}m{sec}s";
            }
            return seconds.ToString("F1");
        }

        private void PopulateTeamPreview()
        {
            if (_teamPreview == null) return;
            _teamPreview.Clear();
            
            var members = new[]
            {
                new { name = "Đường Môn Đệ Tử", faction = "tm", hp = 80, maxHp = 100, mp = 40, maxMp = 50, isLeader = true },
                new { name = "Nga Mi Đệ Tử", faction = "em", hp = 100, maxHp = 100, mp = 50, maxMp = 50, isLeader = false },
                new { name = "Cái Bang Đệ Tử", faction = "gb", hp = 50, maxHp = 120, mp = 20, maxMp = 100, isLeader = false }
            };
            
            foreach (var m in members)
            {
                var item = new VisualElement();
                item.AddToClassList("hud-team-member");
                
                if (m.isLeader)
                {
                    var flag = new VisualElement();
                    flag.AddToClassList("hud-team-leader-flag");
                    item.Add(flag);
                }
                
                var icon = new VisualElement();
                icon.AddToClassList("hud-team-faction-icon");
                
                var fact = HudDataService.Instance.GetFaction(m.faction);
                int placeholderSkillId = fact != null ? fact.placeholderSkillId : 124;

                LoadIcon(icon, HudArtPathResolver.ResolveGeneratedArtRoot(artFolder), $"cai_bang_skill_{placeholderSkillId}");
                item.Add(icon);
                
                var info = new VisualElement();
                info.AddToClassList("hud-team-member-info");
                
                var nameLabel = new Label(m.name);
                nameLabel.AddToClassList("hud-team-member-name");
                info.Add(nameLabel);
                
                var hpTrack = new VisualElement();
                hpTrack.AddToClassList("hud-team-bar-track");
                var hpFill = new VisualElement();
                hpFill.AddToClassList("hud-team-bar-fill-hp");
                hpFill.style.width = Length.Percent(((float)m.hp / m.maxHp) * 100f);
                hpTrack.Add(hpFill);
                info.Add(hpTrack);
                
                var mpTrack = new VisualElement();
                mpTrack.AddToClassList("hud-team-bar-track");
                var mpFill = new VisualElement();
                mpFill.AddToClassList("hud-team-bar-fill-mp");
                mpFill.style.width = Length.Percent(((float)m.mp / m.maxMp) * 100f);
                mpTrack.Add(mpFill);
                info.Add(mpTrack);
                
                item.Add(info);
                _teamPreview.Add(item);
            }
        }

        private void PopulateTradeInfo()
        {
            if (_tradePartnerName != null) _tradePartnerName.text = "     + Tên: Dã Tẩu";
            if (_tradePartnerLevel != null) _tradePartnerLevel.text = "     + Cấp: 200";
            if (_tradePartnerFaction != null) _tradePartnerFaction.text = "     + Phái: Võ Đang";
            if (_tradePartnerGuild != null) _tradePartnerGuild.text = "     + Bang: Thiên Hạ";
        }

        private void CloseTradeInfo()
        {
            _tradeInfoPanel?.AddToClassList("hidden");
        }

        private void SelectStallCurrency(string name)
        {
            SubsystemLog.Info("Stall", $"Đã chọn tiền tệ thanh toán: {name}");
            _stallCurrencySelector?.AddToClassList("hidden");
        }

        private void OpenFacePicker()
        {
            if (_facePickerOverlay != null)
            {
                _facePickerOverlay.RemoveFromClassList("hidden");
                PopulateFacePicker();
            }
        }
        
        private void CloseFacePicker()
        {
            _facePickerOverlay?.AddToClassList("hidden");
        }

        private void PopulateFacePicker()
        {
            if (_facePickerList == null) return;
            _facePickerList.Clear();

            var emotes = HudDataService.Instance.GetEmoteList();

            foreach (var emote in emotes)
            {
                var cell = new VisualElement();
                cell.AddToClassList("hud-face-item");
                cell.pickingMode = PickingMode.Position;

                var label = new Label(emote.text);
                label.AddToClassList("hud-face-item-text");
                cell.Add(label);

                cell.tooltip = emote.tip;

                string symbol = emote.text;
                cell.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (_chatInput != null)
                    {
                        _chatInput.value += symbol;
                    }
                    CloseFacePicker();
                    evt.StopPropagation();
                });
                
                _facePickerList.Add(cell);
            }
        }
    }
}
