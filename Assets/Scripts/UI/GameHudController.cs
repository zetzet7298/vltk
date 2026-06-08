// -----------------------------------------------------------------------------
// VLTK Mobile — Game HUD Controller
// Loads real SPR art from PC source and wires to UI Toolkit elements.
// PC reference: 顶部控制条.ini, 玩家信息主界面.ini, 工具控制条.ini, 小地图_小.ini
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
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
        private VisualElement _topBarPanel, _bottomPanel, _minimapPanel, _chatPanel;
        private VisualElement _minimapContent, _previewContent;
        private VisualElement _playerDot, _mapPreviewOverlay, _mapPreviewFrame, _mapPreviewPlayerDot;
        private VisualElement _miniMapTarget, _mapPreviewTarget;
        private VisualElement _skillPanel, _skillClose, _skillPageOne, _skillPageTwo;
        private ScrollView _skillList;
        private Label _hpText, _mpText, _staminaText, _expText;
        private Label _levelText, _sceneName, _scenePos, _mapPreviewTitle, _mapPreviewCoords, _skillSummary, _pcConnectionStatus;
        private TextField _chatInput, _mapPosInput;
        private VisualElement _chatTabs;
        private Label _chatWarning;
        private ChatChannel _selectedChatChannel = ChatChannel.All;
        private int _chatHistoryOffset;
        private bool _chatChannelsVisible = true;
        private bool _chatExpanded;
        private bool _chatRightAnchored;
        private bool _chatShadowVisible = true;
        private bool _systemReminderVisible;
        private bool _minimapExpanded;

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
        private VisualElement _utilityDock, _utilityActionRow, _utilityMenuRowA, _utilityMenuRowB;
        private VisualElement _pcShortcutDock, _pcShortcutToggleBtn;
        private Label _utilityToggleLabel, _utilitySwitchLabel, _pcShortcutToggleLabel;
        private VisualElement _pcToolPanel, _pcToolClose;
        private ScrollView _pcToolList;
        private Label _pcToolTitle;
        private int _utilityBarMode;
        private bool _isRunning = true;
        private bool _isSitting;
        private bool _recEnabled;
        private bool _pkEnabled;
        private bool _offlineMode;
        private bool _friendGroupExpanded = true;
        private bool _friendInvisible;
        private int _friendScrollOffset;
        private string _friendFilter = "UnitBtnFriend";
        private bool _teamNearbyListClosed;
        private float _defaultRunSpeed;
        private TradeSession _tradeSession;
        private PartyMember _tradeTarget;
        private EconomyService _tradeEconomy;
        private const float RecorderFrameIntervalSeconds = 5f;
        private float _recFrameTimer;
        private int _recFrameCount;
        private string _recLastCapturePath;
        private bool _recCaptureToDisk = true;

        // Inventory window (Hành Trang)
        private VisualElement _invWindow, _invClose, _invFrame;
        private ScrollView _invGrid;
        private Label _invMoney;
        private VisualElement _gmItemOverlay, _gmItemFrame;
        private ScrollView _gmItemList;
        private Label _gmItemTitle, _gmItemMessage;
        private List<GmTeleportDestination> _gmTeleportDestinations;
        private VisualElement _gmTeleportResults;
        private string _gmTeleportQuery = string.Empty;
        private string _gmTeleportFilter = GmTeleportCatalogService.FilterAll;
        private int _gmTeleportPage;
        private VisualElement _pressedInventorySlot;
        private InventoryPanelRow _pressedInventoryRow;
        private IVisualElementScheduledItem _inventoryLongPressTimer;
        private int _inventoryPressedPointerId = -1;
        private bool _inventoryLongPressFired;

        private HudDataBridge _bridge;
        private MinimapService _minimapService;
        private bool _initialized;
        private SprRuntimeService _sprService;
        private Texture2D _minimapTexture;
        private Texture2D _previewTexture;
        private int _minimapTextureMapId = -1;
        private int _previewTextureMapId = -1;
        private int _skillPageIndex;
        private VisualElement _boundRoot;
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
            { "UtilityToggleBtn", "btn_options" },
            { "UtilitySwitchBtn", "btn_options" },
            { "BtnRun", "btn_run" },
            { "BtnSit", "btn_sit" },
            { "BtnHorse", "btn_horse" },
            { "BtnExchange", "btn_exchange" },
            { "BtnRec", "btn_rec" },
            { "BtnStatus", "btn_status" },
            { "BtnItems", "btn_items" },
            { "BtnItemEx", "btn_itemex" },
            { "BtnSkills", "btn_skills" },
            { "BtnTask", "btn_task" },
            { "BtnFriend", "btn_friend" },
            { "BtnTeam", "btn_team" },
            { "BtnFaction", "btn_faction" },
            { "BtnChatRoom", "btn_chatroom" },
            { "BtnOptions", "btn_options" },
            { "BtnPK", "btn_pk" },
            { "BtnTreasure", "btn_treasure" },
            { "PrimaryAttackBtn", "btn_primary_attack" },
            { "IconBarArenaBtn", "icon_bar_arena" },
            { "IconBarActivityBtn", "icon_bar_activity" },
            { "IconBarTreasureBtn", "icon_bar_treasure" },
            { "IconBarShopBtn", "icon_bar_shop" },
            { "IconBarPetBtn", "icon_bar_pet" },
            { "IconBarLoginPrizeBtn", "icon_bar_loginprize" },
            { "IconBarFuncPrizeBtn", "icon_bar_funcprize" },
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
            UpdateRecorder(Time.deltaTime);
        }

        private void EnsureRuntimeReady()
        {
            if (_initialized && !IsBoundToCurrentVisualTree())
                _initialized = false;

            if (_initialized)
                return;

            BindElements();
            if (_initialized)
            {
                LoadArt();
                SizeRootToScreen();
            }
        }

        private bool IsBoundToCurrentVisualTree()
        {
            var doc = GetComponent<UIDocument>();
            var root = doc?.rootVisualElement?.Q("GameHud");
            if (root == null || !ReferenceEquals(root, _boundRoot))
                return false;
            if (!ReferenceEquals(root.Q("InventoryWindow"), _invWindow))
                return false;
            return root.Q("BottomPanel") != null;
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

            _topBarPanel = root.Q("TopBarPanel");
            _bottomPanel = root.Q("BottomPanel");
            _minimapPanel = root.Q("MinimapPanel");
            _chatPanel = root.Q("ChatBar");

            _hpFill = root.Q("HpBarFill");
            _mpFill = root.Q("MpBarFill");
            _staminaFill = root.Q("StaminaBarFill");
            _expFill = root.Q("ExpBarFill");

            _hpText = root.Q<Label>("HpText");
            _mpText = root.Q<Label>("MpText");
            _staminaText = root.Q<Label>("StaminaText");
            _expText = root.Q<Label>("ExpText");

            _levelText = root.Q<Label>("LevelText");
            _pcConnectionStatus = root.Q<Label>("PcConnectionStatusText");
            _sceneName = root.Q<Label>("SceneName");
            _scenePos = root.Q<Label>("ScenePos");
            _mapPosInput = root.Q<TextField>("MapPosInput");
            _chatInput = root.Q<TextField>("ChatInput");
            _chatTabs = root.Q("ChatTabs");
            _chatWarning = root.Q<Label>("ChatWarning");
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
            _utilityDock = root.Q("MobileUtilityDock");
            _utilityActionRow = root.Q("MobileUtilityActionRow");
            _utilityMenuRowA = root.Q("MobileUtilityMenuRowA");
            _utilityMenuRowB = root.Q("MobileUtilityMenuRowB");
            _pcShortcutDock = root.Q("PcShortcutDock");
            _pcShortcutToggleBtn = root.Q("PcShortcutToggleBtn");
            _utilityToggleLabel = root.Q<Label>("UtilityToggleLabel");
            _utilitySwitchLabel = root.Q<Label>("UtilitySwitchLabel");
            _pcShortcutToggleLabel = root.Q<Label>("PcShortcutToggleLabel");
            _pcToolPanel = root.Q("PcToolPanel");
            _pcToolClose = root.Q("PcToolClose");
            _pcToolList = root.Q<ScrollView>("PcToolList");
            _pcToolTitle = root.Q<Label>("PcToolTitle");
            var chatRoomScrollTrack = root.Q("ChatRoomScrollTrack");
            if (chatRoomScrollTrack != null)
                chatRoomScrollTrack.pickingMode = PickingMode.Ignore;

            _invWindow = root.Q("InventoryWindow");
            _invFrame = root.Q("InventoryFrame");
            _invClose = root.Q("InventoryClose");
            _invGrid = root.Q<ScrollView>("InventoryGrid");
            _invMoney = root.Q<Label>("InventoryMoney");

            // Mobile-first HUD uses anchored controls instead of PC-coordinate hit proxies.

            RegisterClick(root, "UtilityToggleBtn", OnUtilityToggleClick);
            RegisterClick(root, "UtilitySwitchBtn", OnUtilitySwitchClick);
            RegisterClick(root, "PcShortcutToggleBtn", OnPcShortcutToggleClick);
            for (int i = 0; i < 9; i++)
            {
                int slot = i;
                RegisterClick(root, $"PcItemSlot{slot}", () => OnPcItemShortcutClick(slot));
            }
            RegisterClick(root, "PcLeftSkillBtn", () => OnPcSkillShortcutClick(0));
            RegisterClick(root, "PcRightSkillBtn", () => OnPcSkillShortcutClick(1));
            RegisterClick(root, "BtnRun", OnRunClick);
            RegisterClick(root, "BtnSit", OnSitClick);
            RegisterClick(root, "BtnHorse", OnHorseClick);
            RegisterClick(root, "BtnStatus", OnStatusClick);
            RegisterClick(root, "BtnItems", OnItemsClick);
            RegisterClick(root, "BtnItemEx", OnItemExClick);
            RegisterClick(root, "BtnSkills", OnSkillsClick);
            RegisterClick(root, "BtnTask", OnTaskClick);
            RegisterClick(root, "BtnFriend", OnFriendClick);
            RegisterClick(root, "BtnTeam", OnTeamClick);
            RegisterClick(root, "BtnFaction", OnFactionClick);
            RegisterClick(root, "BtnChatRoom", OnChatRoomClick);
            RegisterClick(root, "BtnOptions", OnOptionsClick);
            RegisterClick(root, "BtnPK", OnPKClick);
            RegisterClick(root, "BtnExchange", OnExchangeClick);
            RegisterClick(root, "BtnRec", OnRecClick);
            RegisterClick(root, "BtnTreasure", OnTreasureClick);
            RegisterClick(root, "IconBarArenaBtn", () => OnIconBarClick(0));
            RegisterClick(root, "IconBarActivityBtn", () => OnIconBarClick(1));
            RegisterClick(root, "IconBarTreasureBtn", () => OnIconBarClick(2));
            RegisterClick(root, "IconBarShopBtn", () => OnIconBarClick(3));
            RegisterClick(root, "IconBarPetBtn", () => OnIconBarClick(4));
            RegisterClick(root, "IconBarLoginPrizeBtn", () => OnIconBarClick(5));
            RegisterClick(root, "IconBarFuncPrizeBtn", () => OnIconBarClick(6));
            RegisterClick(root, "PcToolClose", ClosePcToolPanel);
            RegisterClick(root, "OpenChannelBtn", OnChatChannelToggleClick);
            RegisterClick(root, "ChatChannelIdentityBtn", OnChatChannelIdentityClick);
            RegisterClick(root, "SendBtn", OnSendChatClick);
            RegisterClick(root, "ChatSizeBtn", OnChatSizeClick);
            RegisterClick(root, "ChatMoveBtn", OnChatMoveClick);
            RegisterClick(root, "ChatShadowBtn", OnChatShadowClick);
            RegisterClick(root, "ChatScrollUpBtn", OnChatScrollUpClick);
            RegisterClick(root, "ChatScrollThumbBtn", OnChatScrollThumbClick);
            RegisterClick(root, "ChatScrollDownBtn", OnChatScrollDownClick);
            RegisterClick(root, "ChatSplitBtn", OnChatSplitClick);
            RegisterClick(root, "ChatChannelToggleBtn", OnChatChannelToggleClick);
            RegisterClick(root, "ChatSysUpBtn", OnChatSystemUpClick);
            RegisterClick(root, "ChatSysOpenBtn", OnChatSystemOpenClick);
            RegisterClick(root, "ChatSysDownBtn", OnChatSystemDownClick);
            RegisterClick(root, "ChatTabAll", () => SelectChatChannel(ChatChannel.All));
            RegisterClick(root, "ChatTabPrivate", () => SelectChatChannel(ChatChannel.Private));
            RegisterClick(root, "ChatTabRoom", () => SelectChatChannel(ChatChannel.Room));
            RegisterClick(root, "ChatTabGuild", () => SelectChatChannel(ChatChannel.Guild));
            RegisterClick(root, "ChatTabFaction", () => SelectChatChannel(ChatChannel.Faction));
            RegisterClick(root, "ChatTabOther", () => SelectChatChannel(ChatChannel.Other));
            HighlightChatTab(_selectedChatChannel);

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
            RegisterClick(root, "ScenePos", OnScenePosClick);
            RegisterClick(root, "ToggleMapBtn", OnToggleMapClick);
            RegisterClick(root, "MinimapMarkerBtn", OnMinimapMarkerClick);
            RegisterClick(root, "WorldMapBtn", OnWorldMapClick);
            RegisterClick(root, "CaveMapBtn", OnCaveMapClick);
            RegisterClick(root, "MapPreviewClose", CloseMapPreview);
            RegisterClick(root, "CaiBangSkillClose", CloseSkillPanel);
            if (_mapPosInput != null)
            {
                _mapPosInput.pickingMode = PickingMode.Position;
                _mapPosInput.RegisterCallback<KeyDownEvent>(evt =>
                {
                    if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        ApplyMapPosInput();
                        evt.StopPropagation();
                    }
                });
                _mapPosInput.RegisterCallback<FocusOutEvent>(_ => _mapPosInput.AddToClassList("hidden"));
            }

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

            _boundRoot = root;
            ApplyUtilityBarMode(_utilityBarMode);
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
            LoadElementImage(_invFrame, artPath, InventoryWindowPcSpec.PcBackgroundArtName);

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

                var markerMap = root.Q("MinimapMarkerBtn");
                if (markerMap != null)
                    LoadIcon(markerMap, artPath, "btn_minimap_flag_pc");

                var toggleMap = root.Q("ToggleMapBtn");
                if (toggleMap != null)
                    LoadIcon(toggleMap, artPath, "btn_minimap_switch_pc");

                var worldMap = root.Q("WorldMapBtn");
                if (worldMap != null)
                    LoadIcon(worldMap, artPath, "btn_minimap_world_full_pc");

                var caveMap = root.Q("CaveMapBtn");
                if (caveMap != null)
                    LoadIcon(caveMap, artPath, "btn_minimap_cave_pc");
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
                // PC 800 top status bars use 106x11 SPR fills clipped inside
                // 104x9 INI tracks; do not stretch them to stale 1024/1280 sizes.
                fill.style.backgroundSize = new BackgroundSize(106, 11);
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

            // Responsive mobile rule: keep a 1280x720 PC-HUD reference rectangle
            // uniformly scaled by PanelSettings Shrink. Extra width/height becomes
            // safe-area padding; PC chrome and hitboxes stay pixel-aligned inside it.
            const float referenceWidth = 1280f;
            const float referenceHeight = 720f;
            float screenWidth = Mathf.Max(1f, Screen.width);
            float screenHeight = Mathf.Max(1f, Screen.height);
            float scale = Mathf.Min(screenWidth / referenceWidth, screenHeight / referenceHeight);
            float w = screenWidth / scale;
            float h = screenHeight / scale;
            float safeX = Mathf.Max(0f, (w - referenceWidth) * 0.5f);
            float safeY = Mathf.Max(0f, (h - referenceHeight) * 0.5f);

            hud.style.width = w;
            hud.style.height = h;
            doc.rootVisualElement.style.width = w;
            doc.rootVisualElement.style.height = h;

            if (_topBarPanel != null)
            {
                _topBarPanel.style.left = safeX;
                _topBarPanel.style.top = safeY;
                _topBarPanel.style.width = referenceWidth;
                _topBarPanel.style.height = 40f;
            }
            if (_bottomPanel != null)
            {
                _bottomPanel.style.left = safeX;
                _bottomPanel.style.bottom = safeY;
                _bottomPanel.style.width = referenceWidth;
                _bottomPanel.style.height = 220f;
            }
            if (_chatPanel != null)
            {
                _chatPanel.style.left = safeX;
                _chatPanel.style.bottom = safeY + 176f;
            }
            if (_minimapPanel != null)
            {
                _minimapPanel.style.left = safeX + 1138f;
                _minimapPanel.style.right = StyleKeyword.Auto;
                _minimapPanel.style.top = safeY + 4f;
            }
            if (_mapPreviewOverlay != null)
            {
                _mapPreviewOverlay.style.width = w;
                _mapPreviewOverlay.style.height = h;
            }
            if (_skillPanel != null)
            {
                _skillPanel.style.left = safeX + Mathf.Clamp(338f, 0f, Mathf.Max(0f, referenceWidth - 205f));
                _skillPanel.style.top = safeY + Mathf.Clamp(110f, 0f, Mathf.Max(0f, referenceHeight - 376f));
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
                UpdatePcConnectionStatus();
                return;
            }

            SetLevel(snap.level);
            SetBar(_hpFill, _hpText, snap.currentLife, snap.maxLife);
            SetBar(_mpFill, _mpText, 50, 50);
            SetBar(_staminaFill, _staminaText, 100, 100);
            SetBar(_expFill, _expText, 0, 100, true);
            UpdatePcConnectionStatus();

            var viMapName = ToVietnameseMapName(snap.mapName);
            if (_sceneName != null) _sceneName.text = viMapName;
            if (_scenePos != null) _scenePos.text = FormatPcScenePos(snap.playerPosition);
            if (_mapPreviewTitle != null) _mapPreviewTitle.text = viMapName;

            EnsureMinimapTexture(snap);
            UpdateMinimapDots(snap);
            UpdateBuffs();
        }


        private void UpdatePcConnectionStatus()
        {
            if (_pcConnectionStatus == null) return;

            float delta = Time.smoothDeltaTime > 0.0001f ? Time.smoothDeltaTime : Time.deltaTime;
            int fps = Mathf.Clamp(Mathf.RoundToInt(1f / Mathf.Max(0.0001f, delta)), 0, 999);
            string label;
            Color color;
            if (fps >= 45)
            {
                label = "Hoạt động tốt";
                color = new Color(60f / 255f, 1f, 160f / 255f);
            }
            else if (fps >= 20)
            {
                label = "Quá đông";
                color = new Color(1f, 200f / 255f, 0f);
            }
            else
            {
                label = "Bị giật";
                color = Color.red;
            }

            _pcConnectionStatus.text = $"{label} {fps}";
            _pcConnectionStatus.style.color = color;
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
                "风之骑" => "Phong Kỳ (Vượt ải 120+)",
                "Map_389" => "Phong Kỳ (Vượt ải 120+)",
                "Phong Kỳ (trên 120)" => "Phong Kỳ (Vượt ải 120+)",
                "Phong K?(tr猲 120)" => "Phong Kỳ (Vượt ải 120+)",
                "Phong K� (tr�n 120)" => "Phong Kỳ (Vượt ải 120+)",
                "沙漠山洞1" => "Vượt ải Nhiếp Thí Trần",
                "Map_907" => "Vượt ải Nhiếp Thí Trần",
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

        private void OnScenePosClick()
        {
            // PC ec10b91e [ScenePos] Tip=click to find path and [MapPosInput] Type=2.
            // Mobile keeps the same coordinate entry semantics: tap coords, enter x/y, move to that PC coordinate.
            if (_mapPosInput == null)
            {
                OpenMapPreview();
                return;
            }

            _mapPosInput.value = _scenePos != null ? _scenePos.text : string.Empty;
            _mapPosInput.RemoveFromClassList("hidden");
            _mapPosInput.Focus();
        }

        private void ApplyMapPosInput()
        {
            if (_mapPosInput == null) return;
            if (!TryParsePcScenePos(_mapPosInput.value, out var target))
            {
                if (_mapPreviewCoords != null)
                    _mapPreviewCoords.text = "Tọa độ không hợp lệ. Dùng dạng x/y.";
                OpenMapPreview();
                return;
            }

            MovePlayerTo(target);
            _lastMoveTarget = target;
            _mapPosInput.AddToClassList("hidden");
            if (_mapPreviewCoords != null)
                _mapPreviewCoords.text = $"Đến: {FormatPcScenePos(target)}";
            OpenMapPreview();
        }

        private static bool TryParsePcScenePos(string text, out Vector2 world)
        {
            world = Vector2.zero;
            if (string.IsNullOrWhiteSpace(text)) return false;
            var parts = text.Trim().Replace(',', '/').Split('/');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0].Trim(), out var x)) return false;
            if (!int.TryParse(parts[1].Trim(), out var y)) return false;
            world = new Vector2(x * 8f, -y * 8f);
            return true;
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
            CloseGmItemOverlay();
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
                slot.pickingMode = PickingMode.Position;

                var icon = new VisualElement { name = $"InvSlotIcon{i}" };
                icon.AddToClassList("hud-inv-slot-icon");
                slot.Add(icon);

                var r = rows != null && i < rows.Count
                    ? rows[i]
                    : new InventoryPanelRow(i, 0, 0, 0, 0, 0, false, false, string.Empty, 0);
                if (r.itemId != 0)
                {
                    var c = InventoryWindowPcSpec.TierColor(r.itemQuality);
                    slot.style.borderTopColor = slot.style.borderBottomColor =
                        slot.style.borderLeftColor = slot.style.borderRightColor =
                        new StyleColor(new Color(c.r / 255f, c.g / 255f, c.b / 255f));

                    if (IsGmTestServerRow(r))
                        LoadIcon(this, icon, HudArtPathResolver.ResolveArtRoot(artFolder), "yupai_haozhao");

                    if (r.count > 1)
                    {
                        var countLabel = new Label(r.count.ToString());
                        countLabel.AddToClassList("hud-inv-slot-count");
                        slot.Add(countLabel);
                    }
                }

                RegisterInventorySlotGestures(slot, r);
                _invGrid.Add(slot);
            }
        }

        private static bool IsGmTestServerRow(InventoryPanelRow row)
            => row.itemGenre == GmTestServerItemService.ItemGenre
               && row.itemDetail == GmTestServerItemService.DetailType
               && row.itemParticular == GmTestServerItemService.ParticularType;

        private void RegisterInventorySlotGestures(VisualElement slot, InventoryPanelRow row)
        {
            if (slot == null || row.itemId == 0) return;
            slot.RegisterCallback<PointerDownEvent>(evt => BeginInventorySlotPress(slot, row, evt));
            slot.RegisterCallback<PointerUpEvent>(EndInventorySlotPress);
            slot.RegisterCallback<PointerCancelEvent>(CancelInventorySlotPress);
        }

        private void BeginInventorySlotPress(VisualElement slot, InventoryPanelRow row, PointerDownEvent evt)
        {
            _pressedInventorySlot = slot;
            _pressedInventoryRow = row;
            _inventoryPressedPointerId = evt.pointerId;
            _inventoryLongPressFired = false;
            _inventoryLongPressTimer?.Pause();

            if (IsGmTestServerRow(row))
            {
                slot.CapturePointer(evt.pointerId);
                _inventoryLongPressTimer = slot.schedule.Execute(() =>
                {
                    if (_pressedInventorySlot != slot) return;
                    _inventoryLongPressFired = true;
                    OpenGmItemActionSheet(GmTestServerItemService.MainMenuId);
                }).StartingIn(550);
            }
            evt.StopPropagation();
        }

        private void EndInventorySlotPress(PointerUpEvent evt)
        {
            _inventoryLongPressTimer?.Pause();
            if (_pressedInventorySlot != null && _pressedInventorySlot.HasPointerCapture(_inventoryPressedPointerId))
                _pressedInventorySlot.ReleasePointer(_inventoryPressedPointerId);

            if (!_inventoryLongPressFired && _pressedInventoryRow.itemId != 0)
                OpenInventoryItemDetail(_pressedInventoryRow);

            _pressedInventorySlot = null;
            _inventoryPressedPointerId = -1;
            evt.StopPropagation();
        }

        private void CancelInventorySlotPress(PointerCancelEvent evt)
        {
            _inventoryLongPressTimer?.Pause();
            _pressedInventorySlot = null;
            _inventoryPressedPointerId = -1;
            _inventoryLongPressFired = false;
        }

        private GmTestServerItemService GetGmItemService()
            => SandboxManager.Instance != null && SandboxManager.Instance.GmTestServerItemService != null
                ? SandboxManager.Instance.GmTestServerItemService
                : new GmTestServerItemService();

        public bool IsGmItemOverlayVisible => _gmItemOverlay != null && !_gmItemOverlay.ClassListContains("hidden");

        private void EnsureGmItemOverlay()
        {
            var parent = _boundRoot ?? _invWindow;
            if (parent == null) return;
            if (_gmItemOverlay != null && _gmItemOverlay.parent == parent) return;

            _gmItemOverlay = new VisualElement { name = "GmItemActionOverlay" };
            _gmItemOverlay.AddToClassList("hidden");
            _gmItemOverlay.style.position = Position.Absolute;
            _gmItemOverlay.style.left = 0; _gmItemOverlay.style.right = 0;
            _gmItemOverlay.style.top = 0; _gmItemOverlay.style.bottom = 0;
            _gmItemOverlay.style.backgroundColor = new Color(0f, 0f, 0f, 0.55f);
            _gmItemOverlay.style.justifyContent = Justify.Center;
            _gmItemOverlay.style.alignItems = Align.Center;
            _gmItemOverlay.pickingMode = PickingMode.Position;

            _gmItemFrame = new VisualElement { name = "GmItemActionFrame" };
            _gmItemFrame.style.width = 420;
            _gmItemFrame.style.maxHeight = 560;
            _gmItemFrame.style.paddingLeft = 12; _gmItemFrame.style.paddingRight = 12;
            _gmItemFrame.style.paddingTop = 10; _gmItemFrame.style.paddingBottom = 10;
            _gmItemFrame.style.backgroundColor = new Color(0.11f, 0.08f, 0.04f, 0.96f);
            _gmItemFrame.style.borderTopWidth = _gmItemFrame.style.borderBottomWidth = 2;
            _gmItemFrame.style.borderLeftWidth = _gmItemFrame.style.borderRightWidth = 2;
            _gmItemFrame.style.borderTopColor = _gmItemFrame.style.borderBottomColor = new Color(0.95f, 0.76f, 0.35f);
            _gmItemFrame.style.borderLeftColor = _gmItemFrame.style.borderRightColor = new Color(0.95f, 0.76f, 0.35f);

            _gmItemTitle = new Label { name = "GmItemActionTitle" };
            _gmItemTitle.style.color = new Color(1f, 0.86f, 0.35f);
            _gmItemTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            _gmItemTitle.style.fontSize = 18;
            _gmItemMessage = new Label { name = "GmItemActionMessage" };
            _gmItemMessage.style.whiteSpace = WhiteSpace.Normal;
            _gmItemMessage.style.color = Color.white;
            _gmItemMessage.style.marginBottom = 8;
            _gmItemList = new ScrollView { name = "GmItemActionList" };
            _gmItemList.style.maxHeight = 430;

            _gmItemFrame.Add(_gmItemTitle);
            _gmItemFrame.Add(_gmItemMessage);
            _gmItemFrame.Add(_gmItemList);
            _gmItemOverlay.Add(_gmItemFrame);
            parent.Add(_gmItemOverlay);
            _gmItemOverlay.BringToFront();
        }

        private void CloseGmItemOverlay()
        {
            _gmItemOverlay?.AddToClassList("hidden");
        }

        private void OpenInventoryItemDetail(InventoryPanelRow row)
        {
            EnsureGmItemOverlay();
            if (_gmItemOverlay == null) return;
            _gmItemOverlay.RemoveFromClassList("hidden");
            _gmItemOverlay.BringToFront();
            _gmItemTitle.text = row.itemName;
            _gmItemMessage.text = IsGmTestServerRow(row)
                ? "Lệnh bài này chỉ được GM sử dụng. Chọn Dùng để mở menu GM Test Server."
                : "Vật phẩm này chưa có hành động sử dụng trên mobile.";
            _gmItemList.Clear();
            if (IsGmTestServerRow(row))
                _gmItemList.Add(MakeGmButton("Dùng", () => OpenGmItemActionSheet(GmTestServerItemService.MainMenuId)));
            _gmItemList.Add(MakeGmButton("Đóng", CloseGmItemOverlay));
        }

        private void OpenGmItemActionSheet(string menuId)
        {
            EnsureGmItemOverlay();
            if (_gmItemOverlay == null) return;
            var service = GetGmItemService();
            _gmItemOverlay.RemoveFromClassList("hidden");
            _gmItemOverlay.BringToFront();
            _gmItemTitle.text = "Lệnh bài GM Test Server";
            _gmItemMessage.text = service.CanUse
                ? "Chọn chức năng theo menu PC."
                : "Chỉ GM/dev mới được sử dụng Lệnh bài GM Test Server.";
            _gmItemList.Clear();

            if (menuId != GmTestServerItemService.MainMenuId)
                _gmItemList.Add(MakeGmButton("← Quay lại", () => OpenGmItemActionSheet(GmTestServerItemService.MainMenuId)));

            foreach (var option in service.GetMenu(menuId))
            {
                var captured = option;
                _gmItemList.Add(MakeGmButton(captured.label, () => OnGmMenuOption(captured)));
            }
            _gmItemList.Add(MakeGmButton("Kết thúc đối thoại", CloseGmItemOverlay));
        }

        private Button MakeGmButton(string text, System.Action action)
        {
            var btn = new Button(action) { text = text };
            btn.style.height = 34;
            btn.style.marginBottom = 4;
            btn.style.unityTextAlign = TextAnchor.MiddleLeft;
            return btn;
        }

        private void OnGmMenuOption(GmItemMenuOption option)
        {
            if (option == null) return;
            if (!string.IsNullOrEmpty(option.nextMenuId))
            {
                OpenGmItemActionSheet(option.nextMenuId);
                return;
            }
            var service = GetGmItemService();
            var result = service.Execute(option.actionId, confirmed: false);
            if (result.status == GmItemActionStatus.NeedsConfirmation)
            {
                RenderGmConfirmation(option, result.message);
                return;
            }
            HandleGmActionResult(result);
        }

        private void RenderGmConfirmation(GmItemMenuOption option, string message)
        {
            EnsureGmItemOverlay();
            _gmItemOverlay?.RemoveFromClassList("hidden");
            _gmItemTitle.text = option?.label ?? "Xác nhận";
            _gmItemMessage.text = message;
            _gmItemList.Clear();
            _gmItemList.Add(MakeGmButton("Đúng vậy!", () =>
            {
                var result = GetGmItemService().Execute(option.actionId, confirmed: true);
                HandleGmActionResult(result);
                OpenInventory();
            }));
            _gmItemList.Add(MakeGmButton("Ta nhầm.", () => OpenGmItemActionSheet(GmTestServerItemService.MainMenuId)));
        }

        private void HandleGmActionResult(GmItemActionResult result)
        {
            if (result == null) return;
            if (result.success && result.message == GmTestServerItemService.AllMapsActionId)
            {
                OpenGmTeleportBrowser(resetCatalog: true);
                return;
            }
            if (result.success && result.message == "OPEN_SKILL_PANEL")
            {
                CloseGmItemOverlay();
                OpenSkillPanel();
                return;
            }
            if (result.success && result.message == "OPEN_TONG_KIM_SHOP")
            {
                SandboxManager.Instance?.ShopPanel?.OpenShop(93);
                _gmItemMessage.text = "Đã mở shop Tống Kim (Sale 93).";
                return;
            }
            _gmItemMessage.text = result.message;
            SubsystemLog.Info("GMItem", $"{result.status}: {result.message}");
        }

        private void OpenGmTeleportBrowser(bool resetCatalog)
        {
            EnsureGmItemOverlay();
            if (_gmItemOverlay == null) return;
            var service = GetGmItemService();
            _gmItemOverlay.RemoveFromClassList("hidden");
            _gmItemOverlay.BringToFront();
            _gmItemTitle.text = "Dịch chuyển bản đồ";

            if (!service.CanUse)
            {
                _gmItemMessage.text = "Chỉ GM/dev mới được sử dụng dịch chuyển bản đồ.";
                _gmItemList.Clear();
                _gmItemList.Add(MakeGmButton("Đóng", CloseGmItemOverlay));
                return;
            }

            if (resetCatalog || _gmTeleportDestinations == null)
            {
                _gmTeleportDestinations = new List<GmTeleportDestination>(service.GetTeleportDestinations());
                _gmTeleportPage = 0;
            }

            _gmItemMessage.text = $"Đủ {_gmTeleportDestinations.Count} map PC. Tìm theo tên hoặc ID, rồi chạm map để đi.";
            _gmItemList.Clear();
            _gmItemList.Add(MakeGmButton("← Quay lại", () => OpenGmItemActionSheet(GmTestServerItemService.TravelMenuId)));

            var search = new TextField("Tìm");
            search.name = "GmTeleportSearch";
            search.style.height = 32;
            search.style.marginBottom = 4;
            search.SetValueWithoutNotify(_gmTeleportQuery ?? string.Empty);
            search.RegisterValueChangedCallback(evt =>
            {
                _gmTeleportQuery = evt.newValue ?? string.Empty;
                _gmTeleportPage = 0;
                RenderGmTeleportRows();
            });
            _gmItemList.Add(search);

            var filterRow = new VisualElement { name = "GmTeleportFilters" };
            filterRow.style.flexDirection = FlexDirection.Row;
            filterRow.style.flexWrap = Wrap.Wrap;
            filterRow.style.marginBottom = 6;
            AddGmTeleportFilterButton(filterRow, GmTeleportCatalogService.FilterAll);
            AddGmTeleportFilterButton(filterRow, GmTeleportCatalogService.FilterCity);
            AddGmTeleportFilterButton(filterRow, GmTeleportCatalogService.FilterField);
            AddGmTeleportFilterButton(filterRow, GmTeleportCatalogService.FilterCave);
            AddGmTeleportFilterButton(filterRow, GmTeleportCatalogService.FilterBattlefield);
            AddGmTeleportFilterButton(filterRow, GmTeleportCatalogService.FilterTong);
            AddGmTeleportFilterButton(filterRow, GmTeleportCatalogService.FilterOthers);
            _gmItemList.Add(filterRow);

            _gmTeleportResults = new VisualElement { name = "GmTeleportResults" };
            _gmItemList.Add(_gmTeleportResults);
            RenderGmTeleportRows();
        }

        private void AddGmTeleportFilterButton(VisualElement row, string filter)
        {
            var btn = new Button(() =>
            {
                _gmTeleportFilter = filter;
                _gmTeleportPage = 0;
                OpenGmTeleportBrowser(resetCatalog: false);
            }) { text = GmTeleportCatalogService.FilterLabel(filter) };
            btn.style.height = 28;
            btn.style.marginRight = 4;
            btn.style.marginBottom = 4;
            btn.style.backgroundColor = filter == _gmTeleportFilter
                ? new Color(0.55f, 0.36f, 0.12f, 0.95f)
                : new Color(0.18f, 0.14f, 0.08f, 0.95f);
            row.Add(btn);
        }

        private void RenderGmTeleportRows()
        {
            if (_gmTeleportResults == null) return;
            _gmTeleportResults.Clear();
            var filtered = GmTeleportCatalogService.Filter(_gmTeleportDestinations, _gmTeleportQuery, _gmTeleportFilter);
            int total = filtered.Count;
            int pageSize = GmTeleportCatalogService.DefaultPageSize;
            int maxPage = total <= 0 ? 0 : (total - 1) / pageSize;
            _gmTeleportPage = Mathf.Clamp(_gmTeleportPage, 0, maxPage);
            int start = _gmTeleportPage * pageSize;
            int end = Mathf.Min(start + pageSize, total);

            var nav = new VisualElement { name = "GmTeleportPageNav" };
            nav.style.flexDirection = FlexDirection.Row;
            nav.style.marginBottom = 5;
            var prev = new Button(() => { _gmTeleportPage--; RenderGmTeleportRows(); }) { text = "← Trước" };
            prev.SetEnabled(_gmTeleportPage > 0);
            prev.style.height = 28;
            prev.style.marginRight = 6;
            var page = new Label(total == 0 ? "0 map" : $"{start + 1}-{end}/{total} map");
            page.style.color = new Color(1f, 0.86f, 0.35f);
            page.style.unityTextAlign = TextAnchor.MiddleCenter;
            page.style.flexGrow = 1;
            var next = new Button(() => { _gmTeleportPage++; RenderGmTeleportRows(); }) { text = "Sau →" };
            next.SetEnabled(_gmTeleportPage < maxPage);
            next.style.height = 28;
            next.style.marginLeft = 6;
            nav.Add(prev); nav.Add(page); nav.Add(next);
            _gmTeleportResults.Add(nav);

            if (total == 0)
            {
                var empty = new Label("Không tìm thấy map phù hợp.");
                empty.style.color = Color.white;
                _gmTeleportResults.Add(empty);
                return;
            }

            for (int i = start; i < end; i++)
            {
                var captured = filtered[i];
                _gmTeleportResults.Add(MakeGmButton(captured.DisplayLabel, () => OnGmTeleportMap(captured.mapId)));
            }
        }

        private void OnGmTeleportMap(int mapId)
        {
            var result = GetGmItemService().TeleportToMap(mapId);
            _gmItemMessage.text = result?.message ?? "Dịch chuyển thất bại.";
            if (result != null)
                SubsystemLog.Info("GMItem", $"Teleport browser {mapId}: {result.status} {result.message}");
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

        private void OnUtilityToggleClick()
        {
            int nextMode = _utilityBarMode == 0 ? 1 : 0;
            if (nextMode != 0)
                ApplyPcShortcutDock(false);
            ApplyUtilityBarMode(nextMode);
            SubsystemLog.Info("HUD", nextMode == 0 ? "Hide utility bar" : "Show action utility bar");
        }

        private void OnUtilitySwitchClick()
        {
            int nextMode = _utilityBarMode == 2 ? 1 : 2;
            ApplyPcShortcutDock(false);
            ApplyUtilityBarMode(nextMode);
            SubsystemLog.Info("HUD", nextMode == 1 ? "Switch to action utility bar" : "Switch to menu utility bar");
        }

        private void ApplyUtilityBarMode(int mode)
        {
            _utilityBarMode = Mathf.Clamp(mode, 0, 2);
            bool showAction = _utilityBarMode == 1;
            bool showMenu = _utilityBarMode == 2;
            bool showDock = _utilityBarMode != 0;

            _utilityDock?.EnableInClassList("hidden", !showDock);
            _utilityDock?.EnableInClassList("action-mode", showAction);
            _utilityDock?.EnableInClassList("menu-mode", showMenu);
            _utilityActionRow?.EnableInClassList("hidden", !showAction);
            _utilityMenuRowA?.EnableInClassList("hidden", !showMenu);
            _utilityMenuRowB?.EnableInClassList("hidden", !showMenu);
            _boundRoot?.Q("UtilityToggleBtn")?.EnableInClassList("active", showDock);
            _boundRoot?.Q("UtilitySwitchBtn")?.EnableInClassList("active", showMenu);

            if (_utilityToggleLabel != null)
                _utilityToggleLabel.text = showDock ? "Ẩn" : "Mở";
            if (_utilitySwitchLabel != null)
                _utilitySwitchLabel.text = showMenu ? "Tác" : "Menu";
        }

        public int CurrentUtilityBarMode => _utilityBarMode;

        private void OnPcShortcutToggleClick()
        {
            bool show = _pcShortcutDock == null || _pcShortcutDock.ClassListContains("hidden");
            if (show)
                ApplyUtilityBarMode(0);
            ApplyPcShortcutDock(show);
            SubsystemLog.Info("HUD", show ? "Show PC quick shortcuts" : "Hide PC quick shortcuts");
        }

        private void ApplyPcShortcutDock(bool show)
        {
            _pcShortcutDock?.EnableInClassList("hidden", !show);
            _pcShortcutToggleBtn?.EnableInClassList("active", show);
            if (_pcShortcutToggleLabel != null)
                _pcShortcutToggleLabel.text = show ? "Ẩn" : "1-9";
        }

        private void OnPcItemShortcutClick(int index)
        {
            int pcSlot = Mathf.Clamp(index, 0, 8);
            string title = $"Phím tắt vật phẩm {pcSlot + 1}";
            var rows = new List<string>
            {
                $"PC autoexec.lua: phím {pcSlot + 1} → ShortcutUseItem({pcSlot})",
            };

            if (TryUsePcShortcutItem(pcSlot, out var itemName, out var remaining))
            {
                rows.Add($"Đã dùng: {itemName}");
                rows.Add($"Còn lại: {remaining}");
            }
            else
            {
                rows.Add("Chưa có vật phẩm ở ô tắt này trong runtime mobile.");
                rows.Add("Mở Túi đồ để kiểm tra/gán vật phẩm hồi phục.");
            }

            OpenPcToolPanel(title, rows);
            SubsystemLog.Info("HUD", $"ShortcutUseItem({pcSlot})");
        }

        private bool TryUsePcShortcutItem(int index, out string itemName, out int remaining)
        {
            itemName = string.Empty;
            remaining = 0;
            var inventory = SandboxManager.Instance?.InventoryService;
            var entries = inventory?.Inventory;
            if (entries == null || index < 0 || index >= entries.Count)
                return false;

            var entry = entries[index];
            if (entry?.item == null || entry.count <= 0)
                return false;

            itemName = string.IsNullOrWhiteSpace(entry.item.DisplayName) ? $"Item {entry.item.itemId}" : entry.item.DisplayName;
            entry.count = Mathf.Max(0, entry.count - 1);
            remaining = entry.count;
            return true;
        }

        private void OnPcSkillShortcutClick(int slot)
        {
            int mobileSlot = Mathf.Clamp(slot, 0, 1);
            var combatSlots = FindObjectOfType<CombatSkillSlotController>();
            if (combatSlots != null)
            {
                combatSlots.OpenSkillPicker(mobileSlot);
                OpenPcToolPanel(mobileSlot == 0 ? "Kỹ năng trái" : "Kỹ năng phải", new[]
                {
                    $"PC 主界面玩家信息窗口.ini: {(mobileSlot == 0 ? "ImediaLeftSkill" : "ImediaRightSkill")}",
                    $"Mobile: mở bảng gán kỹ năng cho ô {mobileSlot + 1}.",
                });
            }
            else
            {
                OpenSkillPanel();
                OpenPcToolPanel(mobileSlot == 0 ? "Kỹ năng trái" : "Kỹ năng phải", new[]
                {
                    "CombatSkillSlotController chưa sẵn sàng.",
                    "Đã mở bảng Võ công để chọn/nâng kỹ năng.",
                });
            }
            SubsystemLog.Info("HUD", mobileSlot == 0 ? "Open left skill assignment" : "Open right skill assignment");
        }

        private void OnMinimapMarkerClick()
        {
            var target = _lastMoveTarget ?? ResolveCurrentPlayerWorldForFlag();
            _lastMoveTarget = target;

            if (_bridge != null)
            {
                var snap = _bridge.BuildSnapshot();
                if (snap.valid && snap.activeMap != null)
                    UpdateMinimapDots(snap);
            }

            OpenMapPreview();
            if (_mapPreviewCoords != null)
                _mapPreviewCoords.text = $"Cắm cờ: {FormatPcScenePos(target)}";
            OpenPcToolPanel("Đánh dấu bản đồ", new[]
            {
                $"Đã cắm cờ: {FormatPcScenePos(target)}",
                "PC: ec10b91e/f8bf2550 [BtnFlag] dùng 小地图－旗帜按钮.spr + FlagImage=地图小旗帜.spr.",
                "Chạm bản đồ lớn để dời cờ/đặt điểm đến.",
            });
            SubsystemLog.Info("HUD", $"Set minimap flag {target} ({FormatPcScenePos(target)})");
        }

        private Vector2 ResolveCurrentPlayerWorldForFlag()
        {
            if (_bridge != null)
            {
                var snap = _bridge.BuildSnapshot();
                if (snap.valid)
                    return snap.playerPosition;
            }

            var player = SandboxManager.Instance != null ? SandboxManager.Instance.PlayerController : FindObjectOfType<SandboxPlayerController>();
            return player != null ? (Vector2)player.transform.position : Vector2.zero;
        }

        private void OnToggleMapClick()
        {
            _minimapExpanded = !_minimapExpanded;
            _minimapPanel?.EnableInClassList("hud-minimap-large", _minimapExpanded);
            OpenPcToolPanel("Chuyển bản đồ nhỏ/lớn", new[]
            {
                _minimapExpanded ? "Đã chuyển sang bản đồ lớn." : "Đã thu về bản đồ nhỏ.",
                "PC: ec10b91e [SwitchBtn] dùng 小地图－切换按钮0.spr để đổi 小地图_小.ini ↔ 小地图_大.ini.",
            });
            SubsystemLog.Info("HUD", _minimapExpanded ? "Minimap large" : "Minimap small");
        }

        private void OnWorldMapClick()
        {
            OpenMapPreview();
            var catalog = new GmTeleportCatalogService(SandboxManager.Instance?.MapManager);
            var destinations = catalog.GetAllDestinations();
            var rows = new List<string>
            {
                $"Bản đồ thế giới PC: {destinations.Count}",
                _sceneName != null ? $"Map hiện tại: {_sceneName.text}" : "Map hiện tại: --",
                "PC: ec10b91e/f8bf2550 [WorldMapBtn] dùng 小地图－世界大地图按钮.spr để mở bản đồ thế giới.",
            };
            int count = Math.Min(10, destinations.Count);
            for (int i = 0; i < count; i++)
            {
                var dest = destinations[i];
                rows.Add($"{dest.DisplayLabel} @ {FormatPcScenePos(dest.worldPosition)} ({dest.coordinateSource})");
            }
            if (destinations.Count > count)
                rows.Add($"… còn {destinations.Count - count} map PC trong danh sách thế giới.");
            OpenPcToolPanel("Bản đồ thế giới", rows);
            SubsystemLog.Info("HUD", "Open world map catalog");
        }

        private void OnCaveMapClick()
        {
            OpenMapPreview();
            var catalog = new GmTeleportCatalogService(SandboxManager.Instance?.MapManager);
            var caves = GmTeleportCatalogService.Filter(catalog.GetAllDestinations(), string.Empty, GmTeleportCatalogService.FilterCave);
            var rows = new List<string>
            {
                $"Hang động/me cung PC: {caves.Count}",
                _sceneName != null ? $"Map hiện tại: {_sceneName.text}" : "Map hiện tại: --",
            };
            int count = Math.Min(8, caves.Count);
            for (int i = 0; i < count; i++)
                rows.Add(caves[i].DisplayLabel);
            if (caves.Count > count)
                rows.Add($"… còn {caves.Count - count} map trong danh sách cave PC.");
            OpenPcToolPanel("Bản đồ sơn động", rows);
            SubsystemLog.Info("HUD", "Open cave map filter");
        }

        private void OnRunClick()
        {
            var player = SandboxManager.Instance?.PlayerController;
            if (player != null)
            {
                if (_defaultRunSpeed <= 0f)
                    _defaultRunSpeed = Mathf.Max(1f, player.moveSpeed);
                _isRunning = !_isRunning;
                player.moveSpeed = _isRunning ? _defaultRunSpeed : _defaultRunSpeed * 0.5f;
            }
            SetButtonActive("BtnRun", _isRunning);
            SubsystemLog.Info("HUD", _isRunning ? "Chạy bộ" : "Đi bộ");
        }

        private void OnSitClick()
        {
            _isSitting = !_isSitting;
            if (_isSitting)
                SandboxManager.Instance?.PlayerController?.ResetMovementState();
            SetButtonActive("BtnSit", _isSitting);
            OpenPcToolPanel("Ngồi", _isSitting ? new[] { "Đang ngồi tĩnh tọa", "Di chuyển hoặc chạm lại để đứng dậy." } : new[] { "Đã đứng dậy." });
            SubsystemLog.Info("HUD", _isSitting ? "Sit enabled" : "Sit disabled");
        }

        private void OnHorseClick()
        {
            var player = SandboxManager.Instance?.PlayerController;
            if (player != null)
            {
                if (player.Mount.IsMounted)
                    player.Mount.Dismount();
                else
                    player.Mount.Mount(player.defaultHorseId > 0 ? player.defaultHorseId : 5);
                SetButtonActive("BtnHorse", player.Mount.State == MountState.Mounted || player.Mount.State == MountState.Mounting);
                SubsystemLog.Info("HUD", player.Mount.IsMounted ? "Dismount Horse" : "Mount Horse");
            }
            else
            {
                OpenPcToolPanel("Lên xuống ngựa", new[] { "Player runtime chưa sẵn sàng." });
            }
        }

        private void OnStatusClick()
        {
            var manager = SandboxManager.Instance;
            var snap = CharacterPanelService.BuildSnapshot(manager?.PlayerProgression, null, 1);
            var rows = new List<string>
            {
                $"Tên: {snap.playerName}",
                $"Cấp: {snap.level}",
                $"Sinh lực: {snap.hp}/{snap.hpMax}",
                $"Nội lực: {snap.mp}/{snap.mpMax}",
                $"Thể lực: {snap.stamina}/{snap.staminaMax}",
                $"Công/Thủ: {snap.attack}/{snap.defense}",
                $"Chính xác/Né/Bạo/Đỡ: {snap.hit}/{snap.dodge}/{snap.crit}/{snap.block}",
                $"Sức mạnh: {CharacterPanelService.ComputePowerLevel(snap)}",
            };
            OpenPcToolPanel(CharacterPanelService.Title, rows);
            SubsystemLog.Info("HUD", "Open Character Status");
        }

        private void OnItemsClick() => ToggleInventory();

        private void OnItemExClick()
        {
            var snap = BagPanelService.BuildSnapshot(1, SandboxManager.Instance?.InventoryService);
            var rows = new List<string> { $"Tổng rương: {snap.totalBags}", $"Ô: {snap.usedSlots}/{snap.totalSlots}" };
            if (snap.rows != null)
            {
                foreach (var r in snap.rows)
                    rows.Add($"{r.name}: {r.itemCount}/{r.slots} {(r.isFull ? BagPanelService.LabelFull : BagPanelService.LabelEmptySlot)}");
            }
            OpenPcToolPanel("Túi hành trang", rows);
            SubsystemLog.Info("HUD", "Open ItemEx / Bag panel");
        }

        private void OnSkillsClick() => OpenSkillPanel();

        private void OnTaskClick()
        {
            var manager = SandboxManager.Instance;
            int level = manager?.PlayerProgression?.level ?? 1;
            int faction = manager?.PlayerProgression != null ? (int)manager.PlayerProgression.faction : 0;
            var taskSnap = QuestTaskPanelService.BuildSnapshot(manager?.QuestService, level, faction, 0, manager?.DailyTaskService);
            var rows = new List<string>
            {
                $"Đang làm: {taskSnap.activeCount}",
                $"Có thể nhận: {taskSnap.availableCount}",
                $"Đã hoàn thành: {taskSnap.completedCount}",
            };
            foreach (var row in taskSnap.rows)
                rows.Add(row);
            OpenPcToolPanel("Nhiệm vụ", rows);
            SubsystemLog.Info("HUD", "Open Task/Quest panel");
        }

        private void OnFriendClick()
        {
            var snap = FriendPanelService.BuildSnapshot(SandboxManager.Instance?.FriendService, 1);
            OpenPcFriendPanel(snap);
            SubsystemLog.Info("HUD", "Open Friend panel");
        }

        private void OpenPcFriendPanel(FriendPanelSnapshot snap)
        {
            if (_pcToolPanel == null || _pcToolList == null)
                return;
            if (_pcToolTitle != null)
                _pcToolTitle.text = "Bằng hữu";
            _pcToolList.Clear();

            AddPcToolRow($"Bằng hữu: {snap.friendCount}/{snap.maxFriends} — lọc {FriendFilterLabel(_friendFilter)} — nhóm {(_friendGroupExpanded ? "mở" : "thu")} — ẩn thân {(_friendInvisible ? "bật" : "tắt")}");
            if (snap.controls != null)
            {
                foreach (var control in snap.controls)
                {
                    var section = control.pcSection;
                    AddPcToolActionRow($"PC [{control.pcSection}] {control.labelVi}: {control.actionVi}", () => OnPcFriendControlClick(section));
                }
            }

            if (_friendGroupExpanded)
            {
                if (snap.friendRows != null)
                {
                    int index = 0;
                    foreach (var friendRow in snap.friendRows)
                    {
                        if (index++ < _friendScrollOffset) continue;
                        AddPcToolRow($"{FriendFilterLabel(_friendFilter)}: {friendRow}");
                    }
                }
            }
            else
            {
                AddPcToolRow("Nhóm bằng hữu đang thu gọn.");
            }

            _pcToolPanel.RemoveFromClassList("hidden");
            _pcToolPanel.BringToFront();
        }

        private void OnPcFriendControlClick(string pcSection)
        {
            switch (pcSection)
            {
                case "GroupBtn":
                    _friendGroupExpanded = !_friendGroupExpanded;
                    OnFriendClick();
                    break;
                case "UnitBtnFriend":
                case "UnitBtnBrother":
                case "UnitBtnEnemy":
                case "UnitBtnOther":
                    _friendFilter = pcSection;
                    _friendScrollOffset = 0;
                    OnFriendClick();
                    break;
                case "FindBtn":
                    OpenPcToolPanel("Thêm bạn hữu", new[]
                    {
                        @"PC [FindBtn] \Spr\Ui3\好友qq\好友－查找.spr",
                        "Mở tìm/thêm bạn hữu theo FriendService runtime; nhập tên người chơi trong luồng xã giao.",
                    });
                    break;
                case "Invisible":
                    _friendInvisible = !_friendInvisible;
                    OpenPcToolPanel("Đồng hành", new[]
                    {
                        @"PC [Invisible] \Spr\Ui3\好友qq\好友－隐身.spr",
                        _friendInvisible ? "Đã bật trạng thái ẩn/đồng hành." : "Đã tắt trạng thái ẩn/đồng hành.",
                    });
                    break;
                case "ScrollUp":
                    _friendScrollOffset = System.Math.Max(0, _friendScrollOffset - 1);
                    OnFriendClick();
                    break;
                case "ScrollDown":
                    _friendScrollOffset++;
                    OnFriendClick();
                    break;
                case "CloseBtn":
                    ClosePcToolPanel();
                    break;
            }
        }

        private static string FriendFilterLabel(string pcSection)
        {
            switch (pcSection)
            {
                case "UnitBtnBrother": return "Huynh đệ";
                case "UnitBtnEnemy": return "Cừu nhân";
                case "UnitBtnOther": return "Khác";
                default: return "Bạn hữu";
            }
        }


        private void OnTeamClick()
        {
            var partyPanel = SandboxManager.Instance?.PartyPanel;
            partyPanel?.Toggle();

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

            OpenPcTeamPanel("Mở giao diện tổ đội");
        }

        private void OpenPcTeamPanel(string statusLine = null)
        {
            if (_pcToolPanel == null || _pcToolList == null)
                return;
            if (_pcToolTitle != null)
                _pcToolTitle.text = "Tổ đội";
            _pcToolList.Clear();

            if (!string.IsNullOrEmpty(statusLine))
                AddPcToolRow(statusLine);
            foreach (var row in TeamPanelService.BuildRows(SandboxManager.Instance?.PartyService, _teamNearbyListClosed))
                AddPcToolRow(row);
            foreach (var control in TeamPanelService.PcControls)
            {
                var section = control.pcSection;
                AddPcToolActionRow($"PC [{control.pcSection}] {control.labelVi}: {control.actionVi}", () => OnPcTeamControlClick(section));
            }

            _pcToolPanel.RemoveFromClassList("hidden");
            _pcToolPanel.BringToFront();
        }

        private void OnPcTeamControlClick(string pcSection)
        {
            var party = SandboxManager.Instance?.PartyService;
            switch (pcSection)
            {
                case "Invite":
                    OpenPcTeamPanel(party == null
                        ? "PC [Invite]: PartyService chưa sẵn sàng để mời người chơi."
                        : "PC [Invite]: cần chọn người chơi ở danh sách xung quanh trước khi gửi lời mời.");
                    break;
                case "Kick":
                    if (party != null && TryGetFirstNonLeaderMember(party, out var kickMember))
                    {
                        party.RemoveMember(kickMember.memberId);
                        PopulateTeamPreview();
                        OpenPcTeamPanel($"PC [Kick]: đã trục xuất {kickMember.nameVi}.");
                    }
                    else
                    {
                        OpenPcTeamPanel("PC [Kick]: chưa có thành viên thường để trục xuất.");
                    }
                    break;
                case "Appoint":
                    if (party != null && TryGetFirstNonLeaderMember(party, out var appointMember) && party.TransferLeadership(appointMember.memberId))
                    {
                        PopulateTeamPreview();
                        OpenPcTeamPanel($"PC [Appoint]: đã giao đội trưởng cho {appointMember.nameVi}.");
                    }
                    else
                    {
                        OpenPcTeamPanel("PC [Appoint]: chưa có thành viên nhận quyền đội trưởng.");
                    }
                    break;
                case "Refresh":
                    PopulateTeamPreview();
                    OpenPcTeamPanel("PC [Refresh]: đã làm mới danh sách tổ đội/lân cận.");
                    break;
                case "Leave":
                    if (party != null && party.IsInParty)
                    {
                        int leaveId = party.LeaderId != 0 ? party.LeaderId : party.Members[0].memberId;
                        party.LeaveParty(leaveId);
                        PopulateTeamPreview();
                        OpenPcTeamPanel("PC [Leave]: đã rời đội.");
                    }
                    else
                    {
                        OpenPcTeamPanel("PC [Leave]: hiện chưa tham gia đội.");
                    }
                    break;
                case "Dismiss":
                    if (party != null && party.IsInParty)
                    {
                        party.DisbandParty();
                        PopulateTeamPreview();
                        OpenPcTeamPanel("PC [Dismiss]: đã giải tán đội.");
                    }
                    else
                    {
                        OpenPcTeamPanel("PC [Dismiss]: không có đội để giải tán.");
                    }
                    break;
                case "CloseTeam":
                    _teamNearbyListClosed = !_teamNearbyListClosed;
                    OpenPcTeamPanel(_teamNearbyListClosed ? "PC [CloseTeam]: đã đóng danh sách lân cận." : "PC [CloseTeam]: đã mở danh sách lân cận.");
                    break;
                case "Cancel":
                    _teamPreview?.AddToClassList("hidden");
                    ClosePcToolPanel();
                    break;
            }
        }

        private static bool TryGetFirstNonLeaderMember(PartyService party, out PartyMember member)
        {
            member = null;
            if (party == null || party.Members == null)
                return false;
            foreach (var candidate in party.Members)
            {
                if (candidate != null && !candidate.isLeader)
                {
                    member = candidate;
                    return true;
                }
            }
            return false;
        }

        private void OnFactionClick()
        {
            var manager = SandboxManager.Instance;
            var snap = GuildPanelService.BuildSnapshot(manager?.GuildService, 1);
            var rows = new List<string>
            {
                string.IsNullOrWhiteSpace(snap.guildName) ? "Chưa gia nhập bang phái." : $"Bang: {snap.guildName}",
                $"Cấp bang: {snap.level}",
                $"Quỹ bang: {snap.fund}",
                $"Thành viên: {snap.memberCount}/{snap.maxMember}",
            };
            if (snap.rows != null)
            {
                foreach (var r in snap.rows)
                    rows.Add($"{r.memberName} — {GuildPanelService.RankName(r.rank)} — {(r.isOnline ? "online" : "offline")}");
            }
            OpenPcToolPanel("Bang phái", rows);
            SubsystemLog.Info("HUD", "Open Faction/Guild panel");
        }

        private void OnChatSizeClick()
        {
            _chatExpanded = !_chatExpanded;
            _chatPanel?.EnableInClassList("hud-chat-expanded", _chatExpanded);
            OpenPcToolPanel("Chat", new[]
            {
                _chatExpanded ? "Đã mở rộng khung chat." : "Đã thu gọn khung chat.",
                "PC: [SizeBtn] dùng chat_bar_top để kéo/đổi kích thước chat.",
            });
            SubsystemLog.Info("HUD", _chatExpanded ? "Expand chat panel" : "Collapse chat panel");
        }

        private void OnChatMoveClick()
        {
            _chatRightAnchored = !_chatRightAnchored;
            _chatPanel?.EnableInClassList("hud-chat-right", _chatRightAnchored);
            OpenPcToolPanel("Chat", new[]
            {
                _chatRightAnchored ? "Chat neo sang phải." : "Chat neo về trái.",
                "PC: [MoveImg] là tay nắm moveable; mobile đổi anchor để tránh che vùng điều khiển.",
            });
            SubsystemLog.Info("HUD", _chatRightAnchored ? "Anchor chat right" : "Anchor chat left");
        }

        private void OnChatShadowClick()
        {
            _chatShadowVisible = !_chatShadowVisible;
            _chatPanel?.EnableInClassList("hud-chat-shadow-off", !_chatShadowVisible);
            OpenPcToolPanel("Chat", new[]
            {
                _chatShadowVisible ? "Đã bật bóng/nền chat." : "Đã giảm bóng/nền chat.",
                "PC: [ShadowBtn] dùng 聊天条阴影按钮 để bật/tắt bóng chat.",
            });
            SubsystemLog.Info("HUD", _chatShadowVisible ? "Enable chat shadow" : "Disable chat shadow");
        }

        private void OnChatScrollUpClick()
        {
            _chatHistoryOffset = Mathf.Min(_chatHistoryOffset + 6, 194);
            OpenPcToolPanel("Lịch sử chat", BuildChatHistoryRows());
            SubsystemLog.Info("HUD", $"Chat scroll up offset={_chatHistoryOffset}");
        }

        private void OnChatScrollDownClick()
        {
            _chatHistoryOffset = Mathf.Max(0, _chatHistoryOffset - 6);
            OpenPcToolPanel("Lịch sử chat", BuildChatHistoryRows());
            SubsystemLog.Info("HUD", $"Chat scroll down offset={_chatHistoryOffset}");
        }

        private void OnChatScrollThumbClick()
        {
            OpenPcToolPanel("Cuộn chat", BuildChatHistoryRows());
            SubsystemLog.Info("HUD", $"Chat scrollbar thumb offset={_chatHistoryOffset}");
        }

        private void OnChatSplitClick()
        {
            _chatExpanded = !_chatExpanded;
            _chatPanel?.EnableInClassList("hud-chat-expanded", _chatExpanded);
            OpenPcToolPanel("Chia khung chat", new[]
            {
                _chatExpanded ? "Đã mở rộng vùng chat/MSNRoom." : "Đã thu gọn vùng chat/MSNRoom.",
                "PC: [SplitBtn] 14x85 là handle đổi kích thước MSNRoom/ChatRoom.",
            });
            SubsystemLog.Info("HUD", _chatExpanded ? "Expand chat split" : "Collapse chat split");
        }

        private void OnChatSystemUpClick()
        {
            _chatHistoryOffset = Mathf.Min(_chatHistoryOffset + 3, 194);
            OpenPcToolPanel("Nhắc nhở hệ thống", BuildChatHistoryRows());
            SubsystemLog.Info("HUD", $"System room up offset={_chatHistoryOffset}");
        }

        private void OnChatSystemDownClick()
        {
            _chatHistoryOffset = Mathf.Max(0, _chatHistoryOffset - 3);
            OpenPcToolPanel("Nhắc nhở hệ thống", BuildChatHistoryRows());
            SubsystemLog.Info("HUD", $"System room down offset={_chatHistoryOffset}");
        }

        private void OnChatSystemOpenClick()
        {
            _systemReminderVisible = !_systemReminderVisible;
            _chatWarning?.EnableInClassList("hidden", !_systemReminderVisible);
            OpenPcToolPanel("Nhắc nhở hệ thống", new[]
            {
                _systemReminderVisible ? "Đã mở dòng nhắc hệ thống." : "Đã ẩn dòng nhắc hệ thống.",
                "PC: [SysRoom_Open] dùng 提示信息窗－开关 để bật/tắt ô nhắc hệ thống.",
            });
            SubsystemLog.Info("HUD", _systemReminderVisible ? "Show system reminder" : "Hide system reminder");
        }

        private void OnChatChannelToggleClick()
        {
            _chatChannelsVisible = !_chatChannelsVisible;
            _chatTabs?.EnableInClassList("hidden", !_chatChannelsVisible);
            OpenPcToolPanel("Kênh chat", new[]
            {
                _chatChannelsVisible ? "Đã mở dải chọn kênh." : "Đã ẩn dải chọn kênh.",
                "PC: nút 频道开与关b bật/tắt cụm kênh chat ở HUD.",
            });
            SubsystemLog.Info("HUD", _chatChannelsVisible ? "Show chat channels" : "Hide chat channels");
        }

        private void OnChatChannelIdentityClick()
        {
            var chat = SandboxManager.Instance?.ChatService;
            var next = NextChatChannel(_selectedChatChannel);
            SelectChatChannel(next);
            OpenPcToolPanel("Chọn kênh chat", new[]
            {
                $"Kênh hiện tại: {ChatService.ChannelNameVi(next)}",
                "PC: ô biểu tượng bên trái dòng nhập là current channel identity/menu của MSNRoom.",
                chat != null ? $"Runtime channel: {ChatService.ChannelNameVi(chat.ActiveChannel)}" : "ChatService chưa sẵn sàng.",
            });
            SubsystemLog.Info("HUD", $"Cycle chat identity {ChatService.ChannelNameVi(next)}");
        }

        private static ChatChannel NextChatChannel(ChatChannel channel)
        {
            return channel switch
            {
                ChatChannel.All => ChatChannel.Private,
                ChatChannel.Private => ChatChannel.Room,
                ChatChannel.Room => ChatChannel.Guild,
                ChatChannel.Guild => ChatChannel.Faction,
                ChatChannel.Faction => ChatChannel.Other,
                _ => ChatChannel.All,
            };
        }

        private IReadOnlyList<string> BuildChatHistoryRows()
        {
            var rows = new List<string>();
            var chat = SandboxManager.Instance?.ChatService;
            if (chat == null || chat.History == null || chat.History.Count == 0)
            {
                rows.Add("Chưa có tin nhắn.");
                return rows;
            }

            var filtered = new List<ChatMessage>();
            for (int i = chat.History.Count - 1; i >= 0; i--)
            {
                var msg = chat.History[i];
                if (chat.ActiveChannel == ChatChannel.All || msg.channel == chat.ActiveChannel || msg.channel == ChatChannel.System)
                    filtered.Add(msg);
            }

            int start = Mathf.Clamp(_chatHistoryOffset, 0, Mathf.Max(0, filtered.Count - 1));
            int end = Mathf.Min(filtered.Count, start + 8);
            rows.Add($"Kênh: {ChatService.ChannelNameVi(chat.ActiveChannel)} — {start + 1}/{filtered.Count}");
            for (int i = start; i < end; i++)
            {
                var msg = filtered[i];
                string sender = string.IsNullOrWhiteSpace(msg.senderName) ? string.Empty : msg.senderName + ": ";
                rows.Add($"[{ChatService.ChannelNameVi(msg.channel)}] {sender}{msg.text}");
            }
            return rows;
        }

        private void SelectChatChannel(ChatChannel channel)
        {
            _selectedChatChannel = channel;
            var chat = SandboxManager.Instance?.ChatService;
            chat?.SetChannel(channel);
            HighlightChatTab(channel);
            _chatInput?.Focus();
            SubsystemLog.Info("HUD", $"Select chat channel {ChatService.ChannelNameVi(channel)}");
        }

        private void HighlightChatTab(ChatChannel channel)
        {
            if (_boundRoot == null) return;
            foreach (var pair in ChatTabButtons())
                _boundRoot.Q(pair.buttonName)?.EnableInClassList("active", pair.channel == channel);
        }

        private static (string buttonName, ChatChannel channel)[] ChatTabButtons() => new[]
        {
            ("ChatTabAll", ChatChannel.All),
            ("ChatTabPrivate", ChatChannel.Private),
            ("ChatTabRoom", ChatChannel.Room),
            ("ChatTabGuild", ChatChannel.Guild),
            ("ChatTabFaction", ChatChannel.Faction),
            ("ChatTabOther", ChatChannel.Other),
        };

        private void OnSendChatClick()
        {
            string text = _chatInput?.value?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                _chatInput?.Focus();
                return;
            }

            var chat = SandboxManager.Instance?.ChatService;
            if (chat != null)
            {
                var channel = chat.ActiveChannel == ChatChannel.All ? _selectedChatChannel : chat.ActiveChannel;
                chat.SendPlayerMessage(channel, "Người chơi", text);
                _chatInput.value = string.Empty;
                SubsystemLog.Info("HUD", $"Send chat {ChatService.ChannelNameVi(channel)}: {text}");
            }
            else
            {
                OpenPcToolPanel("Chat", new[] { "Chat runtime chưa sẵn sàng.", $"Tin nhắn nháp: {text}" });
            }
        }

        private void OnChatRoomClick()
        {
            var chat = SandboxManager.Instance?.ChatService;
            var snap = ChatRoomPanelService.BuildSnapshot(chat, 8);
            OpenPcChatRoomPanel(snap);
            _chatInput?.Focus();
            SubsystemLog.Info("HUD", "Open ChatRoom panel");
        }

        private void OpenPcChatRoomPanel(ChatRoomPanelSnapshot snap)
        {
            if (_pcToolPanel == null || _pcToolList == null)
                return;
            if (_pcToolTitle != null)
                _pcToolTitle.text = "Phòng chat";
            _pcToolList.Clear();

            AddPcToolRow($"PC [Channels] Default={snap.defaultChannel} ({snap.defaultSendNameVi})");
            if (snap.channels != null)
            {
                foreach (var channel in snap.channels)
                {
                    var pcChannel = channel;
                    AddPcToolActionRow($"Channel{channel.index}: {channel.pcName} — {channel.labelVi} — {channel.sendIntervalMs}ms/{channel.sendMsgNum}",
                        () => OnPcChatRoomChannelClick(pcChannel));
                }
            }

            if (snap.historyRows != null)
            {
                foreach (var row in snap.historyRows)
                    AddPcToolRow(row);
            }

            _pcToolPanel.RemoveFromClassList("hidden");
            _pcToolPanel.BringToFront();
        }

        private void OnPcChatRoomChannelClick(PcChatChannelRow pcChannel)
        {
            var channel = MapPcChatChannel(pcChannel.pcName);
            SelectChatChannel(channel);
            OpenPcToolPanel("Phòng chat", new[]
            {
                $"PC [Channels] Channel{pcChannel.index}: {pcChannel.pcName}",
                $"Đã chọn: {pcChannel.labelVi} → {ChatService.ChannelNameVi(channel)}",
                $"Giới hạn PC: {pcChannel.sendIntervalMs}ms/{pcChannel.sendMsgNum}",
            });
        }

        private static ChatChannel MapPcChatChannel(string pcName)
        {
            switch (pcName)
            {
                case "CH_WORLD": return ChatChannel.World;
                case "CH_NEARBY":
                case "CH_CITY": return ChatChannel.Map;
                case "CH_TEAM": return ChatChannel.Team;
                case "CH_FACTION": return ChatChannel.Faction;
                case "CH_SYSTEM": return ChatChannel.System;
                case "CH_CHATROOM": return ChatChannel.Room;
                case "CH_TONG":
                case "CH_TONGUNION": return ChatChannel.Guild;
                default: return ChatChannel.Other;
            }
        }

        private void OnIconBarClick(int index)
        {
            var spec = index >= 0 && index < HudBottomBarPcSpec.IconBar.Count ? HudBottomBarPcSpec.IconBar[index] : default;
            foreach (var button in IconBarButtonNames())
                _boundRoot?.Q(button)?.EnableInClassList("active", false);
            if (index >= 0 && index < IconBarButtonNames().Length)
                _boundRoot?.Q(IconBarButtonNames()[index])?.EnableInClassList("active", true);

            OpenPcToolPanel(spec.tipVi, BuildIconBarRows(index, spec));
            SubsystemLog.Info("HUD", $"Open PC icon bar {index}: {spec.classType}");
        }

        private IReadOnlyList<string> BuildIconBarRows(int index, HudBottomBarPcSpec.ButtonRect spec)
        {
            var manager = SandboxManager.Instance;
            var rows = new List<string>
            {
                $"PC source: Ui3/icon_bar.ini {spec.classType}",
                $"SPR: {spec.spr}",
            };

            switch (index)
            {
                case 0:
                    rows.Add($"Đấu trường PC loaded: {manager?.ArenaService?.Count ?? 0}");
                    if (manager?.ArenaService != null)
                    {
                        int count = 0;
                        foreach (var arena in manager.ArenaService.GetAllArenas())
                        {
                            if (arena == null) continue;
                            rows.Add($"#{arena.arenaId} map={arena.mapId} level={arena.minLevel}-{arena.maxLevel}");
                            if (++count >= 5) break;
                        }
                    }
                    break;
                case 1:
                    rows.Add($"Hoạt động PC: {manager?.ActivityService?.Count ?? 0}; điểm hoạt động: {manager?.HuoYueDuService?.Count ?? 0}");
                    if (manager?.ActivityService != null)
                    {
                        int count = 0;
                        foreach (var activity in manager.ActivityService.GetAllActivities())
                        {
                            if (activity == null) continue;
                            rows.Add($"#{activity.activityId}: {activity.nameRaw}");
                            if (++count >= 5) break;
                        }
                    }
                    break;
                case 2:
                    rows.Add($"Săn kho báu PC: {manager?.TreasureHuntService?.Count ?? 0}");
                    if (manager?.TreasureHuntService != null)
                    {
                        int count = 0;
                        foreach (var t in manager.TreasureHuntService.All)
                        {
                            if (t == null) continue;
                            rows.Add($"#{t.treasureId} map={t.mapId} pos={t.posX},{t.posY} lv>={t.requiredLevel}");
                            if (++count >= 5) break;
                        }
                    }
                    break;
                case 3:
                    rows.Add($"Cửa hàng PC: {manager?.MallService?.Count ?? 0}");
                    if (manager?.MallService != null)
                    {
                        int count = 0;
                        foreach (var item in manager.MallService.All)
                        {
                            if (item == null) continue;
                            rows.Add($"#{item.mallItemId}: item={item.itemId} giá={item.price} tồn={item.stock}");
                            if (++count >= 5) break;
                        }
                    }
                    break;
                case 4:
                    rows.Add($"Đồng hành active: {manager?.PartnerService?.ActivePetCount ?? 0}/{VLTK.Sandbox.PartnerService.MaxPetSlots}");
                    if (manager?.PartnerService != null)
                    {
                        foreach (var pet in manager.PartnerService.AllActivePets)
                            rows.Add($"Pet #{pet.petId}: {pet.nameVi} lv{pet.level} hp={pet.currentHp}/{pet.maxHp}");
                    }
                    break;
                case 5:
                    rows.Add($"Điểm danh PC rewards: {manager?.SignInService?.Count ?? 0}");
                    if (manager?.SignInService != null)
                    {
                        int count = 0;
                        foreach (var reward in manager.SignInService.All)
                        {
                            if (reward == null) continue;
                            rows.Add($"Ngày {reward.signInDay}: item={reward.rewardItemId} x{reward.rewardCount} gold={reward.rewardGold}");
                            if (++count >= 5) break;
                        }
                    }
                    break;
                case 6:
                    rows.Add($"Thưởng chức năng/event: {manager?.EventBonusService?.Count ?? 0}; lật thẻ: {manager?.FlipCardService?.Count ?? 0}");
                    if (manager?.EventBonusService != null)
                    {
                        int count = 0;
                        foreach (var eventName in manager.EventBonusService.GetAllEvents())
                        {
                            rows.Add($"Event: {eventName}");
                            if (++count >= 5) break;
                        }
                    }
                    break;
            }

            if (rows.Count == 2)
                rows.Add("Runtime service chưa sẵn sàng hoặc không có dữ liệu PC để hiển thị.");
            return rows;
        }

        private static string[] IconBarButtonNames() => new[]
        {
            "IconBarArenaBtn", "IconBarActivityBtn", "IconBarTreasureBtn", "IconBarShopBtn",
            "IconBarPetBtn", "IconBarLoginPrizeBtn", "IconBarFuncPrizeBtn"
        };

        private void OnOptionsClick()
        {
            var snap = SystemMenuPanelService.BuildSnapshot();
            OpenPcSystemMenuPanel(snap.rows);
            SubsystemLog.Info("HUD", "Open System Options");
        }

        private void OpenPcSystemMenuPanel(IEnumerable<SystemMenuRow> rows)
        {
            if (_pcToolPanel == null || _pcToolList == null)
                return;

            if (_pcToolTitle != null)
                _pcToolTitle.text = "Hệ thống";
            _pcToolList.Clear();

            if (rows != null)
            {
                foreach (var row in rows)
                {
                    string text = $"PC [{SystemMenuSectionName(row.menuId)}] {row.name}: {row.description}";
                    AddPcToolActionRow(text, () => OnPcSystemMenuRowClick(row.menuId));
                }
            }

            if (_pcToolList.contentContainer.childCount == 0)
                AddPcToolRow("Không có dữ liệu.");
            _pcToolPanel.RemoveFromClassList("hidden");
            _pcToolPanel.BringToFront();
        }

        private void OnPcSystemMenuRowClick(int menuId)
        {
            switch (menuId)
            {
                case SystemMenuPanelService.MenuExitGame:
                    OpenPcToolPanel("Thoát game", new[]
                    {
                        @"PC [ExitGame] \spr\Ui3\系统\系统－退出.spr",
                        "Yêu cầu xác nhận trước khi rời game; mobile không tự thoát ngay khi chạm để tránh thao tác nhầm.",
                    });
                    break;
                case SystemMenuPanelService.MenuGameHelp:
                    OpenPcToolPanel("Trợ giúp", new[]
                    {
                        @"PC [GameHelp] \spr\Ui3\系统\系统－帮助.spr",
                        "Mở hướng dẫn trò chơi / danh sách phím và thao tác mobile tương đương.",
                    });
                    break;
                case SystemMenuPanelService.MenuOptions:
                    var settings = SettingsPanelService.BuildSnapshot();
                    var settingRows = new List<string>
                    {
                        @"PC [Options] \spr\Ui3\系统\系统－选项.spr",
                        $"Tùy chọn khả dụng: {(settings.rows == null ? 0 : settings.rows.Count)}",
                    };
                    if (settings.rows != null)
                    {
                        foreach (var row in settings.rows)
                            settingRows.Add($"{row.displayName}: {row.description}");
                    }
                    OpenPcToolPanel("Tùy chọn", settingRows);
                    break;
                case SystemMenuPanelService.MenuOffLine:
                    _offlineMode = !_offlineMode;
                    OpenPcToolPanel("Treo máy offline", new[]
                    {
                        @"PC [OffLine] \spr\Ui3\系统\系统－离线托管.spr",
                        _offlineMode ? "Đã bật treo máy offline." : "Đã tắt treo máy offline.",
                    });
                    break;
                case SystemMenuPanelService.MenuContinueGame:
                    ClosePcToolPanel();
                    break;
            }
        }

        private static string SystemMenuSectionName(int menuId)
        {
            switch (menuId)
            {
                case SystemMenuPanelService.MenuExitGame: return "ExitGame";
                case SystemMenuPanelService.MenuGameHelp: return "GameHelp";
                case SystemMenuPanelService.MenuOptions: return "Options";
                case SystemMenuPanelService.MenuOffLine: return "OffLine";
                case SystemMenuPanelService.MenuContinueGame: return "ContiumeGame";
                default: return "Unknown";
            }
        }

        private void OnPKClick()
        {
            _pkEnabled = !_pkEnabled;
            var pk = SandboxManager.Instance?.GameplayLoop?.PkRules;
            pk?.SetPkMode(_pkEnabled ? PkMode.Free : PkMode.Peace);
            SetButtonActive("BtnPK", _pkEnabled);
            OpenPcToolPanel("PK", new[] { _pkEnabled ? "PK: Tự do" : "PK: Hòa bình" });
            SubsystemLog.Info("HUD", _pkEnabled ? "Enable PK Free" : "Disable PK / Peace");
        }

        private void OnExchangeClick()
        {
            if (_tradeInfoPanel != null)
            {
                bool hide = !_tradeInfoPanel.ClassListContains("hidden");
                if (hide)
                {
                    _tradeSession = null;
                    _tradeTarget = null;
                    _tradeEconomy = null;
                    _tradeInfoPanel.AddToClassList("hidden");
                    SetButtonActive("BtnExchange", false);
                    ClosePcToolPanel();
                }
                else
                {
                    _tradeInfoPanel.RemoveFromClassList("hidden");
                    var manager = SandboxManager.Instance;
                    BeginExchangeSession(manager?.GameplayLoop?.Economy, manager?.PartyService?.Members);
                    PopulateTradeInfo();
                    SetButtonActive("BtnExchange", _tradeSession != null);
                    OpenPcExchangePanel("Mở giao dịch người chơi");
                }
                SubsystemLog.Info("HUD", hide ? "Close Exchange" : "Open Exchange");
            }
        }

        private void OpenPcExchangePanel(string statusLine = null)
        {
            if (_pcToolPanel == null || _pcToolList == null)
                return;
            if (_pcToolTitle != null)
                _pcToolTitle.text = "Giao dịch";
            _pcToolList.Clear();

            if (!string.IsNullOrEmpty(statusLine))
                AddPcToolRow(statusLine);
            foreach (var row in ExchangePanelService.BuildRows(_tradeSession, _tradeTarget, _tradeEconomy ?? SandboxManager.Instance?.GameplayLoop?.Economy))
                AddPcToolRow(row);
            foreach (var control in ExchangePanelService.PcControls)
            {
                var section = control.pcSection;
                AddPcToolActionRow($"PC [{control.pcSection}] {control.labelVi}: {control.actionVi}", () => OnPcExchangeControlClick(section));
            }

            _pcToolPanel.RemoveFromClassList("hidden");
            _pcToolPanel.BringToFront();
        }

        private void OnPcExchangeControlClick(string pcSection)
        {
            switch (pcSection)
            {
                case "OkBtn":
                    if (_tradeSession != null)
                    {
                        _tradeSession.Lock(SandboxManager.PlayerActorId);
                        PopulateTradeInfo();
                        OpenPcExchangePanel("PC [OkBtn]: đã khóa giao dịch của bản thân.");
                    }
                    else
                    {
                        OpenPcExchangePanel("PC [OkBtn]: chưa có phiên giao dịch để khóa.");
                    }
                    break;
                case "TradeBtn":
                    if (_tradeSession != null && _tradeSession.IsReady)
                    {
                        OpenPcExchangePanel("PC [TradeBtn]: giao dịch đủ điều kiện xác nhận cuối.");
                    }
                    else
                    {
                        OpenPcExchangePanel("PC [TradeBtn]: đang chờ cả hai bên khóa giao dịch.");
                    }
                    break;
                case "CancelBtn":
                    _tradeSession = null;
                    _tradeTarget = null;
                    _tradeEconomy = null;
                    _tradeInfoPanel?.AddToClassList("hidden");
                    SetButtonActive("BtnExchange", false);
                    ClosePcToolPanel();
                    break;
                case "AddMoney":
                    AdjustTradeSilver(100);
                    break;
                case "ReduceMoney":
                    AdjustTradeSilver(-100);
                    break;
            }
        }

        private void AdjustTradeSilver(int delta)
        {
            if (_tradeSession == null)
            {
                OpenPcExchangePanel(delta > 0 ? "PC [AddMoney]: chưa có phiên giao dịch." : "PC [ReduceMoney]: chưa có phiên giao dịch.");
                return;
            }

            var economy = _tradeEconomy ?? SandboxManager.Instance?.GameplayLoop?.Economy;
            int wallet = economy != null ? economy.Wallet.silver : int.MaxValue;
            int next = Mathf.Clamp(_tradeSession.initiatorSilver + delta, 0, wallet);
            _tradeSession.SetSilver(SandboxManager.PlayerActorId, next);
            PopulateTradeInfo();
            OpenPcExchangePanel(delta > 0 ? $"PC [AddMoney]: đã tăng bạc đặt lên {next}." : $"PC [ReduceMoney]: đã giảm bạc đặt còn {next}.");
        }

        private TradeSession BeginExchangeSession(EconomyService economy, IReadOnlyList<PartyMember> members)
        {
            _tradeSession = null;
            _tradeTarget = null;
            _tradeEconomy = null;
            if (economy == null)
                return null;

            var target = SelectExchangeTarget(members);
            if (target == null)
                return null;

            _tradeTarget = target;
            _tradeEconomy = economy;
            _tradeSession = economy.CreateTradeSession(SandboxManager.PlayerActorId, target.memberId);
            _tradeSession.SetSilver(SandboxManager.PlayerActorId, 0);
            return _tradeSession;
        }

        private static PartyMember SelectExchangeTarget(IReadOnlyList<PartyMember> members)
        {
            if (members == null) return null;
            foreach (var member in members)
            {
                if (member == null) continue;
                if (member.memberId == SandboxManager.PlayerActorId) continue;
                if (!member.isOnline) continue;
                return member;
            }
            return null;
        }

        private void OnRecClick()
        {
            _recEnabled = !_recEnabled;
            if (_recEnabled)
            {
                _recFrameCount = 0;
                _recFrameTimer = RecorderFrameIntervalSeconds;
                CaptureRecorderFrame();
            }
            SetButtonActive("BtnRec", _recEnabled);
            OpenPcToolPanel("Quay phim", BuildRecorderRows());
            SubsystemLog.Info("HUD", _recEnabled ? "Recorder on" : "Recorder off");
        }

        private void UpdateRecorder(float deltaTime)
        {
            if (!_recEnabled) return;
            _recFrameTimer += Mathf.Max(0f, deltaTime);
            if (_recFrameTimer < RecorderFrameIntervalSeconds) return;

            CaptureRecorderFrame();
        }

        private IReadOnlyList<string> BuildRecorderRows()
        {
            var recSpec = HudBottomBarPcSpec.ToolControlBar["Rec"];
            var rows = new List<string>
            {
                _recEnabled ? "Đang ghi hình HUD dạng chuỗi ảnh." : "Đã dừng ghi hình HUD.",
                $"Khung đã lưu: {_recFrameCount}",
                string.IsNullOrEmpty(_recLastCapturePath) ? "File cuối: --" : $"File cuối: {_recLastCapturePath}",
                $"PC source: {recSpec.classType} / {recSpec.spr}",
            };
            return rows;
        }

        private string CaptureRecorderFrame()
        {
            _recFrameTimer = 0f;
            string path = BuildRecorderCapturePath(DateTime.Now);
            _recLastCapturePath = path;
            _recFrameCount++;

            if (!_recCaptureToDisk)
                return path;

            try
            {
                string dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                ScreenCapture.CaptureScreenshot(path);
            }
            catch (Exception ex)
            {
                _recLastCapturePath = $"Lỗi lưu: {ex.Message}";
                SubsystemLog.Warn("HUD", $"Recorder capture failed: {ex.Message}");
            }

            return _recLastCapturePath;
        }

        private static string BuildRecorderCapturePath(DateTime now)
        {
            string stamp = now.ToString("yyyyMMdd_HHmmss_fff");
            return Path.Combine(Application.persistentDataPath, "VltkRecorder", $"pc_hud_rec_{stamp}.png");
        }

        private void OnTreasureClick()
        {
            var manager = SandboxManager.Instance;
            var rows = new List<string>();

            var mall = MallPanelService.BuildSnapshot(manager?.MallService, 1, 0);
            rows.Add($"Kỳ Trân Các: {mall.availableItems}/{mall.totalItems} vật phẩm, đang ưu đãi {mall.onSaleItems}.");
            if (mall.rows != null)
            {
                int shown = 0;
                foreach (var r in mall.rows)
                {
                    rows.Add($"Mall #{r.mallItemId}: {r.itemName} — {r.effectivePrice} {r.currency}, tồn {r.stock}");
                    if (++shown >= 4) break;
                }
            }

            int currentMapId = manager?.MapManager?.ActiveMapId ?? 0;
            var playerPos = manager?.PlayerController != null
                ? (Vector2)manager.PlayerController.transform.position
                : Vector2.zero;
            var treasure = TreasureHuntPanelService.BuildSnapshot(manager?.TreasureHuntService, 1, currentMapId, playerPos.x, playerPos.y);
            rows.Add($"Săn kho báu: gần {treasure.nearbyTreasures}/{treasure.totalTreasures} điểm trên map {currentMapId}.");
            if (treasure.rows != null)
            {
                int shown = 0;
                foreach (var r in treasure.rows)
                {
                    rows.Add($"Kho #{r.treasureId}: {r.itemName} x{r.itemCount}, cách {r.distance:0}.");
                    if (++shown >= 4) break;
                }
            }

            OpenPcToolPanel("Bảo Vật", rows);
            SubsystemLog.Info("HUD", "Open Kỳ Trân Các / Bảo Vật");
        }

        private void SetButtonActive(string name, bool active)
        {
            var el = _boundRoot?.Q(name);
            el?.EnableInClassList("active", active);
        }

        public bool IsPcToolPanelVisible => _pcToolPanel != null && !_pcToolPanel.ClassListContains("hidden");

        public void ClosePcToolPanel()
        {
            _pcToolPanel?.AddToClassList("hidden");
        }

        private void OpenPcToolPanel(string title, IEnumerable<string> rows)
        {
            if (_pcToolPanel == null || _pcToolList == null)
                return;
            if (_pcToolTitle != null)
                _pcToolTitle.text = title ?? string.Empty;
            _pcToolList.Clear();
            if (rows != null)
            {
                foreach (var row in rows)
                    AddPcToolRow(row);
            }
            if (_pcToolList.contentContainer.childCount == 0)
                AddPcToolRow("Không có dữ liệu.");
            _pcToolPanel.RemoveFromClassList("hidden");
            _pcToolPanel.BringToFront();
        }

        private void AddPcToolRow(string text)
        {
            var row = new VisualElement();
            row.AddToClassList("hud-pc-tool-row");
            var label = new Label(text ?? string.Empty);
            label.AddToClassList("hud-pc-tool-row-text");
            row.Add(label);
            _pcToolList?.Add(row);
        }

        private void AddPcToolActionRow(string text, System.Action action)
        {
            var row = new VisualElement();
            row.AddToClassList("hud-pc-tool-row");
            row.AddToClassList("hud-pc-tool-action-row");
            row.pickingMode = PickingMode.Position;
            var label = new Label(text ?? string.Empty);
            label.AddToClassList("hud-pc-tool-row-text");
            row.Add(label);
            row.RegisterCallback<PointerDownEvent>(evt =>
            {
                action?.Invoke();
                evt.StopPropagation();
            });
            _pcToolList?.Add(row);
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

            PopulateTeamPreviewFromMembers(SandboxManager.Instance?.PartyService?.Members);
        }

        private void PopulateTeamPreviewFromMembers(IReadOnlyList<PartyMember> members)
        {
            if (_teamPreview == null) return;
            _teamPreview.Clear();

            if (members == null || members.Count == 0)
            {
                var empty = new VisualElement();
                empty.AddToClassList("hud-team-member");
                var label = new Label("Chưa tham gia đội");
                label.AddToClassList("hud-team-member-name");
                empty.Add(label);
                _teamPreview.Add(empty);
                return;
            }

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

                var fact = HudDataService.Instance.GetFaction(FactionIconKey(m.factionId));
                int placeholderSkillId = fact != null ? fact.placeholderSkillId : 124;

                LoadIcon(icon, HudArtPathResolver.ResolveGeneratedArtRoot(artFolder), $"cai_bang_skill_{placeholderSkillId}");
                item.Add(icon);

                var info = new VisualElement();
                info.AddToClassList("hud-team-member-info");

                string faction = PartyService.FactionNameVi(m.factionId);
                var nameLabel = new Label($"{m.nameVi} Lv{m.level} [{faction}]");
                nameLabel.AddToClassList("hud-team-member-name");
                info.Add(nameLabel);

                var hpTrack = new VisualElement();
                hpTrack.AddToClassList("hud-team-bar-track");
                var hpFill = new VisualElement();
                hpFill.AddToClassList("hud-team-bar-fill-hp");
                hpFill.style.width = Length.Percent(PercentOrFull(m.hpCurrent, m.hpMax));
                hpTrack.Add(hpFill);
                info.Add(hpTrack);

                var mpTrack = new VisualElement();
                mpTrack.AddToClassList("hud-team-bar-track");
                var mpFill = new VisualElement();
                mpFill.AddToClassList("hud-team-bar-fill-mp");
                mpFill.style.width = Length.Percent(PercentOrFull(m.mpCurrent, m.mpMax));
                mpTrack.Add(mpFill);
                info.Add(mpTrack);

                item.Add(info);
                _teamPreview.Add(item);
            }
        }

        private static float PercentOrFull(int current, int max)
        {
            if (max <= 0) return 100f;
            return Mathf.Clamp01((float)current / max) * 100f;
        }

        private static string FactionIconKey(int factionId) => factionId switch
        {
            1 => "sl",
            2 => "vd",
            3 => "em",
            4 => "tv",
            5 => "tm",
            6 => "wd",
            7 => "cb",
            8 => "tr",
            9 => "cy",
            10 => "cl",
            _ => "cb",
        };

        private void PopulateTradeInfo()
        {
            var economy = _tradeEconomy ?? SandboxManager.Instance?.GameplayLoop?.Economy;
            if (_tradeSession != null && _tradeTarget != null)
            {
                if (_tradePartnerName != null) _tradePartnerName.text = $"     + Đối tượng: {_tradeTarget.nameVi} (ID {_tradeTarget.memberId})";
                if (_tradePartnerLevel != null) _tradePartnerLevel.text = $"     + Cấp: {_tradeTarget.level} / Ví bạc: {(economy != null ? economy.Wallet.silver : 0)}";
                if (_tradePartnerFaction != null) _tradePartnerFaction.text = $"     + Phiên: #{_tradeSession.initiatorId}->{_tradeSession.targetId} đang yêu cầu";
                if (_tradePartnerGuild != null) _tradePartnerGuild.text = $"     + Đặt bạc: {_tradeSession.initiatorSilver} / Khóa: {(_tradeSession.IsReady ? "đủ" : "chưa")}";
                return;
            }

            if (_tradePartnerName != null) _tradePartnerName.text = "     + Đối tượng: Chưa chọn người chơi";
            if (_tradePartnerLevel != null) _tradePartnerLevel.text = economy != null ? $"     + Ví bạc: {economy.Wallet.silver}" : "     + Ví bạc: --";
            if (_tradePartnerFaction != null) _tradePartnerFaction.text = "     + Phiên: Chưa tạo — chọn thành viên đội để gửi yêu cầu";
            if (_tradePartnerGuild != null) _tradePartnerGuild.text = economy != null ? $"     + Kho: {economy.StashUsed}/{economy.StashUsed + economy.StashRemaining}" : "     + Trạng thái: Runtime chưa sẵn sàng";
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
