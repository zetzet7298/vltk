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
using VLTK.UI.Popup;
using VLTK.UI.CharacterInfo;
using VLTK.UI.Inventory;
using VLTK.UI.Treasure;
using VLTK.UI.Team;
using VLTK.UI.Faction;
using VLTK.UI.Skill;
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
            => CombineStreamingPath(artRoot, string.Concat(iconName, ".png"));

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
            return string.Concat("file://", fullPath.StartsWith("/", System.StringComparison.Ordinal) ? fullPath : string.Concat("/", fullPath));
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
                return string.Concat(root, normalizedRelative);

            return RequiresUnityWebRequest(root)
                ? string.Concat(root, "/", normalizedRelative)
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
        private Label _hpText, _mpText, _staminaText, _expText;
        private Label _levelText, _sceneName, _scenePos, _mapPreviewTitle, _mapPreviewCoords;
        private TextField _chatInput;

        // New HUD elements
        private VisualElement _buffPanel;
        private VisualElement _tradeInfoPanel, _tradeInfoClose;
        private Label _tradePartnerName, _tradePartnerLevel, _tradePartnerFaction, _tradePartnerGuild;
        private VisualElement _stallCurrencySelector;
        private Button _stallMoneyBtn, _stallCoinBtn;
        private VisualElement _facePickerOverlay, _facePickerClose;
        private ScrollView _facePickerList;
        private Button _faceBtn;


        private HudDataBridge _bridge;
        private MinimapService _minimapService;
        private bool _initialized;
        private SprRuntimeService _sprService;
        private Texture2D _minimapTexture;
        private Texture2D _previewTexture;
        private int _minimapTextureMapId = -1;
        private int _previewTextureMapId = -1;
        private Vector2 _lastMinimapCenter;
        private Vector2? _lastMoveTarget;

        // Button name → SPR icon file mapping. Keys resolve to <key>.png in the HUD
        // art folder. Each PNG is the decoded REAL PC SPR (主界面按钮, README §2):
        //   toggles (31px): btn_sit/run/horse/exchange(交易)/rec(摄像机)/pk
        //   menu (28px):    btn_status(人物F1)/items(背包F2)/itemex/skills(技能F3)/
        //                   quest(任务F4)/team(队伍F6)/faction(帮会F7)/chatroom
        private static readonly Dictionary<string, string> ButtonIcons = new()
        {
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
            { "BtnRec", "btn_rec" },
            { "BtnChatRoom", "btn_chatroom" },
            { "BtnItemEx", "btn_itemex" },
            { "BtnQuest", "btn_quest" },
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
            EnsurePcParityOverlayActive();
        }

        // S1 (HUD-004): the uGUI virtual joystick (MobileJoystick, jade medallion
        // bottom-left, spawned by SandboxManager at sortingOrder 500) is now kept VISIBLE
        // and ACTIVE for mobile play. The previous force-hide (HideMobileJoystick) matched
        // a PC-parity baseline that is no longer the target — the HUD is mobile-native now.
        // The joystick stays above the UIToolkit HUD so it never gets covered.

        /// <summary>
        /// The IMGUI <see cref="PcHudVietnameseTextOverlay"/> renders the level number, bar
        /// values, rank, chat tabs and bottom-menu labels that match the PC client. The
        /// UIToolkit bar labels are <c>display:none</c> by design (IMGUI draws them so they
        /// sit above nameplates). Ensure the overlay is enabled so the HUD reflects the PC
        /// runtime state; a scene may ship it disabled. <see cref="GMPanelController"/> only
        /// temporarily hides it while a GM panel is open and restores it on close.
        /// </summary>
        private void EnsurePcParityOverlayActive()
        {
            var vnOverlay = GetComponent<PcHudVietnameseTextOverlay>();
            if (vnOverlay != null && !vnOverlay.enabled)
                vnOverlay.enabled = true;
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

        private void Update ()
        {
            EnsureRuntimeReady();
            if (!_initialized) return;
            SizeRootToScreen();
            UpdateBarsAndMinimap();
            // S1 (HUD-004): joystick force-hide removed — stays active for mobile play.
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

            // Robustness: when the serialized reference is unset (e.g. a scene ships without
            // it wired), fall back to the sibling runtime state — the HUD GameObject carries
            // SandboxRuntimeState, which implements IRuntimeStateProvider — so the HUD always
            // binds real player data (level/hp/mp/stamina/exp) when the runtime exists,
            // matching the PC client instead of defaulting to placeholder values.
            if (provider == null)
                provider = GetComponent<IRuntimeStateProvider>();

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

            // HUD-003: bind the reusable popup host. The PopupOverlay element is
            // the full-screen mount point; PopupManager adds backdrop + window there.
            var popupHost = root.Q("PopupOverlay");
            if (popupHost != null)
            {
                PopupManager.SetInstance(new PopupManager(popupHost));
            }

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

            // Bind new HUD panels
            _buffPanel = root.Q("BuffPanel");

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
                SubsystemLog.Warn("HUD", string.Format(System.Globalization.CultureInfo.InvariantCulture, "Art folder not found: {0}", artPath));
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
                foreach (var kv in ButtonIcons)
                {
                    var btn = root.Q(kv.Key);
                    if (btn == null) continue;
                    var icon = btn.Q(string.Concat(kv.Key, "Icon"));
                    if (icon == null) icon = btn.Q("Icon");
                    if (icon == null)
                    {
                        var children = btn.Children();
                        foreach (var child in children)
                        {
                            if (child is Label) continue;
                            icon = child;
                            break;
                        }
                    }
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
                    LoadIcon(worldMap, artPath, "btn_minimap_world_pc");

                var caveMap = root.Q("CaveMapBtn");
                if (caveMap != null)
                    LoadIcon(caveMap, artPath, "btn_minimap_cave_pc");

                var flagMap = root.Q("FlagMapBtn");
                if (flagMap != null)
                    LoadIcon(flagMap, artPath, "btn_minimap_local_pc");

                var treasure = root.Q("BtnTreasure");
                if (treasure != null)
                    LoadIcon(treasure, artPath, "btn_treasure");
            }
        }

        private void LoadBarArt(VisualElement fill, string artPath, string name)
        {
            if (fill == null) return;
            var png = HudArtPathResolver.ResolvePngPath(artPath, name);
            LoadTextureIntoElement(this, png, name, tex =>
            {
                fill.style.backgroundImage = new StyleBackground(tex);
                fill.style.backgroundSize = new BackgroundSize(104, 9);
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
                UnityEngine.Debug.LogWarning(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[HUD] LoadIcon: element for {0} is null", name));
                return;
            }

            var png = HudArtPathResolver.ResolveUserFacingPngPath(artPath, name);
            if (coroutineHost != null)
            {
                LoadTextureIntoElement(coroutineHost, png, name, tex =>
                {
                    el.style.backgroundImage = new StyleBackground(tex);
                    UnityEngine.Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[HUD] LoadIcon: successfully loaded {0} ({1}x{2}) onto {3}", name, tex.width, tex.height, el.name));
                });
                return;
            }

            LoadTextureIntoElement(null, png, name, tex =>
            {
                el.style.backgroundImage = new StyleBackground(tex);
                UnityEngine.Debug.Log(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[HUD] LoadIcon: successfully loaded {0} ({1}x{2}) onto {3}", name, tex.width, tex.height, el.name));
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
                    UnityEngine.Debug.LogWarning(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[HUD] LoadTexture: {0} requires UnityWebRequest but no coroutine host was provided: {1}", name, path));
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
                UnityEngine.Debug.LogWarning(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[HUD] LoadTexture: failed to load {0} from {1}: {2}", name, path, request.error));
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
                UnityEngine.Debug.LogWarning(string.Format(System.Globalization.CultureInfo.InvariantCulture, "[HUD] LoadTexture: file not found {0}", path));
                return null;
            }

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
            var root = doc.rootVisualElement;
            if (root == null) return;
            var hud = root.Q("GameHud");
            if (hud == null) return;
            var panel = doc.panelSettings;
            float w = panel != null && panel.referenceResolution.x > 0 ? panel.referenceResolution.x : Screen.width;
            float h = panel != null && panel.referenceResolution.y > 0 ? panel.referenceResolution.y : Screen.height;
            hud.style.width = w;
            hud.style.height = h;
            root.style.width = w;
            root.style.height = h;
            if (_mapPreviewOverlay != null)
            {
                _mapPreviewOverlay.style.width = w;
                _mapPreviewOverlay.style.height = h;
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

            // Player stats always bind from the runtime (PC parity): HUD renders
            // level/hp/mp/stamina/exp even when no map is active.
            SetLevel(snap.level);
            SetBar(_hpFill, _hpText, snap.currentLife, snap.maxLife);
            SetBar(_mpFill, _mpText, snap.currentMana, snap.maxMana);
            SetBar(_staminaFill, _staminaText, snap.currentStamina, snap.maxStamina);
            SetBar(_expFill, _expText, (int)snap.currentExp, (int)snap.maxExp, true);

            if (snap.hasActiveMap)
            {
                var viMapName = ToVietnameseMapName(snap.mapName);
                if (_sceneName != null) _sceneName.text = viMapName;
                if (_scenePos != null) _scenePos.text = FormatPcScenePos(snap.playerPosition);
                if (_mapPreviewTitle != null) _mapPreviewTitle.text = viMapName;

                EnsureMinimapTexture(snap);
                UpdateMinimapDots(snap);
            }
            else
            {
                if (_sceneName != null) _sceneName.text = string.Empty;
                if (_scenePos != null) _scenePos.text = string.Empty;
            }
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
            => string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/{1}", Mathf.FloorToInt(world.x / 8f), Mathf.FloorToInt(-world.y / 8f));

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
                    text.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}%", Mathf.RoundToInt(pct));
                }
                else
                {
                    text.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}/{1}", cur, max);
                }
            }
        }

        private void OpenMapPreview()
        {
            if (_mapPreviewOverlay == null) return;
            _mapPreviewOverlay.RemoveFromClassList("hidden");
            if (_mapPreviewCoords != null)
                _mapPreviewCoords.text = _lastMoveTarget.HasValue
                    ? string.Format(System.Globalization.CultureInfo.InvariantCulture, "Mục tiêu: {0}", FormatPcScenePos(_lastMoveTarget.Value))
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
                _mapPreviewCoords.text = string.Format(System.Globalization.CultureInfo.InvariantCulture, "Đến: {0}", FormatPcScenePos(target));
            UpdateMinimapDots(snap);
            CloseMapPreview();
            evt.StopPropagation();
        }

        private void MovePlayerTo(Vector2 worldTarget)
        {
            var player = SandboxManager.Instance != null ? SandboxManager.Instance.PlayerController : Object.FindFirstObjectByType<SandboxPlayerController>();
            if (player == null)
            {
                SubsystemLog.Warn("HUD", string.Format(System.Globalization.CultureInfo.InvariantCulture, "Map preview target {0} ignored: no player", worldTarget));
                return;
            }

            player.MoveTo(worldTarget);
            SubsystemLog.Info("HUD", string.Format(System.Globalization.CultureInfo.InvariantCulture, "Map preview move target {0} ({1})", worldTarget, FormatPcScenePos(worldTarget)));
        }

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

        private void OnRunClick() => SubsystemLog.Info("HUD", "Toggle Run/Walk");
        private void OnSitClick() => SubsystemLog.Info("HUD", "Toggle Sit");
        private void OnHorseClick() => SubsystemLog.Info("HUD", "Toggle Horse");
        private void OnStatusClick()
        {
            // HUD-003: open Character Info via PopupManager. Equipment binds to the
            // live PlayerEquipmentService; stats provider is null until the backend
            // PlayerStateResponse is wired into the HUD (slice 1 shows '--' rows).
            var manager = PopupManager.Instance;
            if (manager == null)
            {
                SubsystemLog.Info("HUD", "PopupManager not initialised");
                return;
            }
            var sandbox = SandboxManager.Instance;
            var equipment = sandbox != null ? sandbox.EquipmentService : null;
            var inventory = sandbox != null ? sandbox.InventoryService : null;
            manager.Show(new CharacterInfoContent(equipment, statsProvider: null, inventory: inventory));
            SubsystemLog.Info("HUD", "Open Character Status");
        }
        private void OnItemsClick()
        {
            var manager = PopupManager.Instance;
            var sandbox = SandboxManager.Instance;
            var inventory = sandbox != null ? sandbox.InventoryService : null;
            manager.Show(new InventoryContent(inventory));
            SubsystemLog.Info("HUD", "Open Inventory");
        }
        private void OnSkillsClick()
        {
            // HUD-003: open the Skill popup via PopupManager (mirrors OnFactionClick /
            // OnStatusClick). SkillContent owns the 30-cell grid, skill-point summary,
            // tap-to-select detail, and "+" upgrade that spends a live fight-skill point.
            var manager = PopupManager.Instance;
            if (manager == null)
            {
                SubsystemLog.Info("HUD", "PopupManager not initialised");
                return;
            }

            var sandbox = SandboxManager.Instance;
            SkillCatalog catalog = sandbox != null
                ? sandbox.CombatSkillCatalog
                : PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            // LIVE progression ref (gameplay-critical, reviewer hand-off): SkillContent.OnShow
            // runs the grant callback BEFORE BuildPage/Refresh. At runtime the callback is
            // SandboxManager.GrantFactionSkillPanelProgression, which mutates
            // manager.PlayerProgression IN PLACE (verified in SandboxManager.cs:
            //   PlayerProgression ??= new PlayerProgressionState();
            //   PlayerProgression.GrantFactionSkillPanelProgression(CombatSkillCatalog, targetFaction);
            // ), i.e. the SAME instance this 'progression' ref points to. So the popup body reads
            // the granted fight-skill points without a post-grant re-fetch (the prior inline
            // OpenSkillPanel re-fetched manager.PlayerProgression defensively; the in-place
            // mutation makes that unnecessary — same live ref). When the sandbox is null
            // (EditMode), grantProgression is null and SkillContent.OnShow falls back to
            // progression.GrantFactionSkillPanelProgression(catalog, faction), still on this ref.
            PlayerProgressionState progression = sandbox != null ? sandbox.PlayerProgression : new PlayerProgressionState();
            CombatFaction faction = progression != null && progression.faction != CombatFaction.None
                ? progression.faction
                : CombatFaction.CaiBang;
            manager.Show(new SkillContent(catalog, progression, faction, GetFactionNameVi(faction), artFolder,
                grantProgression: sandbox != null ? sandbox.GrantFactionSkillPanelProgression : null));
            SubsystemLog.Info("HUD", "Open Kỹ năng võ công");
            CloseMapPreview();
        }

        private void OnTeamClick()
        {
            var manager = PopupManager.Instance;
            if (manager == null)
            {
                SubsystemLog.Info("HUD", "PopupManager not initialised");
                return;
            }

            var sandbox = SandboxManager.Instance;
            var party = sandbox != null ? sandbox.PartyService : null;
            manager.Show(new TeamContent(party));
            SubsystemLog.Info("HUD", "Open Tổ đội / Đội");
        }

        private void OnFactionClick()
        {
            var manager = PopupManager.Instance;
            if (manager == null)
            {
                SubsystemLog.Info("HUD", "PopupManager not initialised");
                return;
            }

            var sandbox = SandboxManager.Instance;
            var bonus = sandbox != null ? sandbox.FactionBonusService : null;
            CombatFaction faction = sandbox != null && sandbox.PlayerProgression != null
                ? sandbox.PlayerProgression.faction
                : CombatFaction.None;
            if (faction == CombatFaction.None)
                faction = CombatFaction.CaiBang;
            int level = sandbox != null && sandbox.PlayerProgression != null
                ? sandbox.PlayerProgression.level
                : 1;
            manager.Show(new FactionContent(bonus, (int)faction, GetFactionNameVi(faction), level));
            SubsystemLog.Info("HUD", "Open Bonus Môn Phái");
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

        private void OnTreasureClick()
        {
            var manager = PopupManager.Instance;
            if (manager == null)
            {
                SubsystemLog.Info("HUD", "PopupManager not initialised");
                return;
            }

            var sandbox = SandboxManager.Instance;
            var mall = sandbox != null ? sandbox.MallService : null;
            var treasureHunt = sandbox != null ? sandbox.TreasureHuntService : null;
            manager.Show(new TreasureContent(mall, treasureHunt));
            SubsystemLog.Info("HUD", "Open Kỳ Trân Các / Bảo Vật");
        }

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
                
                LoadIcon(icon, HudArtPathResolver.ResolveGeneratedArtRoot(artFolder), string.Format(System.Globalization.CultureInfo.InvariantCulture, "cai_bang_skill_{0}", b.skillId));
                cell.Add(icon);
                
                var timer = new Label(FormatTimer(b.durationRemaining));
                timer.AddToClassList(b.isDebuff ? "hud-debuff-timer" : "hud-buff-timer");
                cell.Add(timer);
                
                cell.tooltip = string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}\nCòn lại: {1}s", b.nameVi, FormatTimer(b.durationRemaining));
                
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
                return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}m{1}s", min, sec);
            }
            return seconds.ToString("F1");
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
            SubsystemLog.Info("Stall", string.Format(System.Globalization.CultureInfo.InvariantCulture, "Đã chọn tiền tệ thanh toán: {0}", name));
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
                        _chatInput.value = string.Concat(_chatInput.value, symbol);
                    }
                    CloseFacePicker();
                    evt.StopPropagation();
                });
                
                _facePickerList.Add(cell);
            }
        }
    }
}
