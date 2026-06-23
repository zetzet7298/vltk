using System;
using System.Collections.Generic;
using System.IO;
using Stopwatch = System.Diagnostics.Stopwatch;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.Profiling;
using VLTK.Core;
using VLTK.Sprites;
using VLTK.Model;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public enum SubsystemKind
    {
        Unknown,
        Game,
        Camera,
        UI,
        World,
        Debug,
        Services,
    }

    public enum SandboxBootProfile
    {
        Full,
        FastEditor,
    }

    public sealed class SandboxBootReport
    {
        public readonly List<(SubsystemKind kind, bool ok, string message)> Entries
            = new();
        public readonly List<(string stepName, long milliseconds)> Timings
            = new();

        public SandboxBootProfile BootProfile { get; private set; } = SandboxBootProfile.Full;
        public long TotalMilliseconds { get; private set; }
        /// <summary>
        /// Number of timings recorded during the synchronous boot phase (before
        /// Complete()). Timings beyond this index are deferred/async (e.g. item
        /// table lazy-loaded after the map is shown) and are reported separately
        /// so the synchronous-boot summary is not polluted.
        /// </summary>
        public int SynchronousTimingCount { get; private set; }

        public void Start(SandboxBootProfile bootProfile)
        {
            BootProfile = bootProfile;
            Entries.Clear();
            Timings.Clear();
            TotalMilliseconds = 0;
            SynchronousTimingCount = 0;
        }

        public void Record(SubsystemKind kind, bool ok, string message)
        {
            Entries.Add((kind, ok, message));
        }

        public void RecordTiming(string stepName, long milliseconds)
        {
            Timings.Add((stepName, milliseconds));
        }

        public void Complete(long totalMilliseconds)
        {
            TotalMilliseconds = totalMilliseconds;
            SynchronousTimingCount = Timings.Count;
        }
    }

    public enum SandboxServiceDataStatus
    {
        Unknown,
        Loaded,
        MissingData,
        Unavailable,
        SkippedForFastBoot,
        Error,
    }

    public sealed class SandboxServiceLoadStatus
    {
        public string serviceName;
        public string relativePath;
        public SandboxServiceDataStatus status;
        public int count;
        public string message;

        public bool IsLoaded => status == SandboxServiceDataStatus.Loaded;
        public bool IsMissingData => status == SandboxServiceDataStatus.MissingData;
        public bool IsSkipped => status == SandboxServiceDataStatus.SkippedForFastBoot;
    }

    /// <summary>
    /// VLTK Mobile — Sandbox runtime owner. Bootstraps the world, the
    /// HUD, services, and the active map. Holds a <see cref="SandboxBootProfile"/>
    /// that decides how much of the PC parity surface is brought up at
    /// editor play time.
    /// <para>
    /// ── Dev workflow (the "skip map for fast iteration" pattern) ──
    /// </para>
    /// <para>
    /// For most dev work (HUD, input, services, networking, gameplay
    /// systems, UI) you do NOT need the BLH pentagon terrain, 618
    /// region files, or 812 enemy spawns loaded. The default scene
    /// here uses <c>useFastEditorBoot=true</c> + <c>skipMapVisualsInFastEditorBoot=true</c>
    /// so PlayMode boots in ~2-3s (sandbox skeleton + UI + active
    /// map metadata, no region/enemy load).
    /// </para>
    /// <para>
    /// When you need to see the actual BLH terrain / verify a region
    /// file / screenshot the visual / test enemy spawn rendering,
    /// un-tick <c>useFastEditorBoot</c> in the Inspector and re-enter
    /// PlayMode. Boot will take ~30s (618 regions + 812 enemies) but
    /// the Full profile runs the real pipeline. Re-tick the flag
    /// when you are done to keep subsequent dev iterations fast.
    /// </para>
    /// <para>
    /// This is intentionally serialized to the scene so the
    /// setting is shared across the team — keep it on FastEditor
    /// for everyday dev. The visual-load-balh and coord-scene-pos
    /// commits left both Full-profile boot artifacts in the scene
    /// after a one-off verification pass; this class documents the
    /// toggle so nobody has to re-derive the trade-off.
    /// </para>
    /// </summary>
    public class SandboxManager : MonoBehaviour, IMapTeleportHost
    {
        public const int BaLangHuyenMapId = 53;
        public const int TinSuVuotAiPhongKy120MapId = 389;
        public const int VuotAiNhiepThiTranMapId = 907;
        public const int ThachThucThoiGianSoCap1MapId = 464;
        public const int DauTruongLienDauMapId = 397;
        public const int LamDuQuanMapId = 319;

        public const int PlayerActorId = 1;
        private const string FastBootOptionalServicesSource = "StreamingAssets optional service batches";
        private static readonly ProfilerMarker BootProfilerMarker = new ProfilerMarker("VLTK.Sandbox.Boot");
        private static readonly ProfilerMarker BootStepProfilerMarker = new ProfilerMarker("VLTK.Sandbox.BootStep");
        private static RegionCatalogFile _fastEditorRegionCatalogCache;
        private static string _fastEditorItemDirCacheKey;
        private static ItemContractImporter _fastEditorItemImporterCache;
        private static string _fastEditorDropDirCacheKey;
        private static DropRateRegistry _fastEditorDropRegistryCache;
        private static string _fastEditorSkillDirCacheKey;
        private static PcSkillRegistry _fastEditorSkillRegistryCache;

        private readonly Dictionary<string, SandboxServiceLoadStatus> _serviceLoadStatuses = new();
        private readonly List<string> _missingServiceSummaries = new();
        private readonly List<string> _unavailableServiceSummaries = new();

        public IReadOnlyDictionary<string, SandboxServiceLoadStatus> ServiceLoadStatuses => _serviceLoadStatuses;

        [Header("Roots")]
        public Transform gameRoot;
        public Transform cameraRoot;
        public Transform uiRoot;
        public Transform worldRoot;
        public Transform debugRoot;
        public Transform servicesRoot;

        [Header("Boot — dev workflow toggle (see SandboxManager class doc)")]
        [Tooltip("Map id loaded on boot. Default is Ba Lăng huyện (PC map 53) — the BLH training pentagon.")]
        public int defaultMapId = BaLangHuyenMapId;
        [Tooltip("Master switch: if true, SandboxManager.Run() calls LoadDefaultMapOnBoot (slow path). " +
                 "Turn OFF for tasks that don't need the world loaded (HUD, services, networking, input).")]
        public bool loadDefaultMapOnBoot = true;
        [Tooltip("Explicit boot profile override. Full loads every subsystem including the map regions and enemy spawns (~30s on BLH). " +
                 "FastEditor loads only the skeleton + active-map metadata + UI (~2-3s) — use this for dev iteration.")]
        public SandboxBootProfile bootProfile = SandboxBootProfile.Full;
        [Tooltip("★ THE toggle the team uses for fast dev iteration. " +
                 "Tick to boot in ~2-3s (skeleton + UI + map metadata, NO region files, NO enemy spawns). " +
                 "Un-tick to boot in ~30s (Full profile, BLH terrain + 618 regions + 812 enemies render). " +
                 "Default in the scene is ON (fast); flip OFF in Inspector when you need to see the visual.")]
        public bool useFastEditorBoot = false;
        // Set during InitializeSubsystems when item/drop/skill loading should be
        // deferred; consumed/launched in Start().
        private bool _pendingItemDataLoad;
        // True only while the boot default map is being loaded. After that it is
        // reset so runtime map switches (GM panel / SwitchMap) always render the
        // map visuals, regardless of the FastEditor skipVisuals flag (which is a
        // boot-time optimization only).
        private bool _initialBootMapLoad;

        [Tooltip("When useFastEditorBoot is on, also load optional service batches (skill CDB, item CDB extras, etc). " +
                 "Leave OFF for fastest boot.")]
        public bool loadOptionalServicesInFastEditorBoot = false;
        [Tooltip("When useFastEditorBoot is on, still load the active map's metadata (so the player can teleport / see map name) " +
                 "without the heavy region/enemy load. Leave ON so MapManager / MapTravel stay functional.")]
        public bool loadDefaultMapInFastEditorBoot = true;
        [Tooltip("When useFastEditorBoot is on, also skip the MapRenderer.LoadMapRegions call (618 region files). " +
                 "This is the single biggest boot-time saving — ~25s on BLH. Leave ON for fastest dev boot.")]
        public bool skipMapVisualsInFastEditorBoot = true;
        [Tooltip("When useFastEditorBoot is on, also skip the item table load. Safe to leave ON for dev.")]
        public bool skipItemLoadingInFastEditorBoot = false;
        [Tooltip("When useFastEditorBoot is on, cache parsed PC reference data (NpcS, Skill, etc.) in memory " +
                 "across boots. Saves ~1-2s on the next play. Leave ON.")]
        public bool cacheReferenceDataInFastEditorBoot = true;
        [Tooltip("Log a [SandboxBoot] line per subsystem step + a final summary table. Cheap, leave ON.")]
        public bool logBootTimings = true;
        [Tooltip("Subsystem boot steps slower than this threshold (ms) get a ⚠ marker in the log. " +
                 "50ms catches hitch sources without spamming.")]
        public int bootTimingLogThresholdMs = 50;

        public static SandboxManager Instance { get; private set; }
        public SandboxBootReport BootReport { get; private set; }
        public bool IsInitialized { get; private set; }
        public SandboxBootProfile ActiveBootProfile { get; private set; } = SandboxBootProfile.Full;
        public bool IsFastEditorBootActive => ActiveBootProfile == SandboxBootProfile.FastEditor;
        public int CurrentFightState { get; private set; } = 1;
        public int CurrentCamp { get; private set; } = 0;
        public int OriginalCamp { get; private set; } = 0;
        public int CurrentLogoutRv { get; private set; } = 0;
        public int CurrentPkFlag { get; private set; } = 0;
        public int CurrentForbidChangePk { get; private set; } = 0;
        public int CurrentPunish { get; private set; } = 0;
        public int CurrentCreateTeam { get; private set; } = 0;
        public string CurrentDeathScript { get; private set; } = string.Empty;
        public int CurrentReviveMapId { get; private set; } = 0;
        public int CurrentReviveId { get; private set; } = 0;
        private readonly Dictionary<int, int> _taskTempValues = new();
        private readonly Dictionary<int, int> _pcMissionValues = new();
        private readonly Dictionary<int, int> _pcMissionPlayerGroups = new();
        private readonly Dictionary<int, int> _pcPartnerMasterTaskStates = new();
        private bool _pcHasSummonedPartner;
        public AssetRegistry AssetRegistry { get; private set; }
        public MapManager MapManager { get; private set; }
        public MapRenderer MapRenderer { get; private set; }
        public SandboxPlayerController PlayerController { get; private set; }
        public MalePlayerVisual PlayerVisual { get; private set; }
        public MobileJoystick PlayerJoystick { get; private set; }
        public MapEnemySpawnRuntime EnemyRuntime { get; private set; }
        public MapInteractiveObjectRuntime ObjectRuntime { get; private set; }
        public MapTrapRuntime TrapRuntime { get; private set; }
        public BaLangEnemyNameplateOverlay EnemyNameplateOverlay { get; private set; }
        public TrainingNpcSpawner TrainingSpawner { get; private set; }
        public FemalePlayerVisual FemalePlayerVisual { get; private set; }
        public SkillCatalog CombatSkillCatalog { get; private set; }
        public CombatRuntimeService CombatRuntime { get; private set; }
        public GameplayLoopService GameplayLoop { get; private set; }
        public PlayerProgressionState PlayerProgression { get; private set; }
        public QuestService QuestService { get; private set; }
        public PcSkillRegistry PcSkillsFull { get; private set; }
        public ItemDatabase ItemDb { get; private set; }
        public LootDropService LootService { get; private set; }
        public AudioService AudioService { get; private set; }
        public QuestTrackerPanel QuestPanel { get; private set; }
        public InventoryPanel InventoryPanel { get; private set; }
        public MapSelectPanel MapSelectPanel { get; private set; }
        public MinimapPanel MinimapPanel { get; private set; }
        public ChatService ChatService { get; private set; }
        public ChatPanel ChatPanel { get; private set; }
        public PartyService PartyService { get; private set; }
        public PartyPanel PartyPanel { get; private set; }
        public FactionPanel FactionPanel { get; private set; }
        public ShopService ShopService { get; private set; }
        public ShopPanel ShopPanel { get; private set; }
        public GMPanelController GmPanel { get; private set; }
        private InventoryService _inventoryService;
        private PlayerEquipmentService _equipmentService;

        /// <summary>Runtime inventory service (item search/add/equip). Used by the HUD bag window.</summary>
        public InventoryService InventoryService => _inventoryService;
        public PlayerEquipmentService EquipmentService => _equipmentService;
        public GmAccessService GmAccessService { get; private set; }
        public GmTestServerItemService GmTestServerItemService { get; private set; }

        // PC-parity runtime services (meridian, partner, title, lottery, recipe, etc.)
        public MeridianService MeridianService { get; private set; }
        public PartnerService PartnerService { get; private set; }
        public PetSkillService PetSkillService { get; private set; }
        public TitleService TitleService { get; private set; }
        public LotteryService LotteryService { get; private set; }
        public CompoundRecipeService CompoundRecipeService { get; private set; }
        public QuestItemService QuestItemService { get; private set; }
        public AdventureService AdventureService { get; private set; }
        public GuildService GuildService { get; private set; }
        public AttribConstService AttribConstService { get; private set; }
        public MissleCatalogService MissleCatalogService { get; private set; }
        public EventBonusService EventBonusService { get; private set; }
        public CityWarService CityWarService { get; private set; }
        public AuctionService AuctionService { get; private set; }
        public GoodsCatalogService GoodsCatalogService { get; private set; }
        public ShopConfigService ShopConfigService { get; private set; }
        // PC-parity runtime services batch 2 (battlefield, instance, skills, etc.)
        public BattlefieldService BattlefieldService { get; private set; }
        public InstanceMapService InstanceMapService { get; private set; }
        public HongbaoService HongbaoService { get; private set; }
        public ItemExchangeService ItemExchangeService { get; private set; }
        public SpecialSkillService SpecialSkillService { get; private set; }
        public NpcSkillService NpcSkillService { get; private set; }
        public TranslifeSkillService TranslifeSkillService { get; private set; }
        public SkillTemplateService SkillTemplateService { get; private set; }
        public NpcLevelScriptService NpcLevelScriptService { get; private set; }
        public NpcDeathScriptService NpcDeathScriptService { get; private set; }
        public DailyTaskService DailyTaskService { get; private set; }
        public BossMissionService BossMissionService { get; private set; }
        // PC-parity runtime services batch 3 (events, weather, music, tasks)
        public ServerEventService ServerEventService { get; private set; }
        public VngEventService VngEventService { get; private set; }
        public BattleScriptService BattleScriptService { get; private set; }
        public WeatherService WeatherService { get; private set; }
        public MusicService MusicService { get; private set; }
        public GuildWorkshopService GuildWorkshopService { get; private set; }
        public HuoYueDuService HuoYueDuService { get; private set; }
        public CityDefenceService CityDefenceService { get; private set; }
        public ActivityService ActivityService { get; private set; }
        public RandomTaskService RandomTaskService { get; private set; }
        public PartnerTaskService PartnerTaskService { get; private set; }
        public MetempsychosisTaskService MetempsychosisTaskService { get; private set; }
        public ArenaService ArenaService { get; private set; }
        public TripService TripService { get; private set; }
        public BonusOnlineService BonusOnlineService { get; private set; }
        // PC-parity runtime services batch 4 (guild extras, misc systems)
        public GuildRankService GuildRankService { get; private set; }
        public GuildStuntService GuildStuntService { get; private set; }
        public GuildTaskService GuildTaskService { get; private set; }
        public HonorService HonorService { get; private set; }
        public ShituService ShituService { get; private set; }
        public FoundryService FoundryService { get; private set; }
        public WorldRankService WorldRankService { get; private set; }
        public NewPlayerGuideService NewPlayerGuideService { get; private set; }
        public ChangeFeatureService ChangeFeatureService { get; private set; }
        public StallService StallService { get; private set; }
        public FlipCardService FlipCardService { get; private set; }
        public BaoRuongThanBiService BaoRuongThanBiService { get; private set; }
        public SeasonalEventService SeasonalEventService { get; private set; }
        public CompensationService CompensationService { get; private set; }
        // PC-parity runtime services batch 5 (final client systems)
        public FactionMapService FactionMapService { get; private set; }
        public BattleAwardService BattleAwardService { get; private set; }
        public DoubleExpService DoubleExpService { get; private set; }
        public SimCityPluginService SimCityPluginService { get; private set; }
        public ClientSkillScriptService ClientSkillScriptService { get; private set; }
        // Battle systems batch 7 (Tống Kim, Công Thành Chiến, Boss Hoàng Kim, Task Flag)
        public TongJinBattleService TongJinBattleService { get; private set; }
        public BangChienService BangChienService { get; private set; }
        public BossHoangKimService BossHoangKimService { get; private set; }
        public TaskFlagService TaskFlagService { get; private set; }
        // Batch 8: Save/Load, Mail, Mount, Ranking, Friend, Pet, ShopConfig, Missile, HudArt, FactionMapRuntime, BattleScript, TaskFlagRegistry
        public SaveSlotService SaveSlotService { get; private set; }
        public MailService MailService { get; private set; }
        public MountService MountService { get; private set; }
        public RankingService RankingService { get; private set; }
        public FriendService FriendService { get; private set; }
        public PetService PetService { get; private set; }
        public MissileEffectService MissileEffectService { get; private set; }
        public HudArtCatalogService HudArtCatalogService { get; private set; }
        public FactionMapRuntimeService FactionMapRuntimeService { get; private set; }
        public BattleScriptRuntimeService BattleScriptRuntimeService { get; private set; }
        public TaskFlagRegistryService TaskFlagRegistryService { get; private set; }
        // Batch 9: Map data, Skill data, World boss, Achievement, Mall, Fashion, Sign-in, Treasure, Encounter, Friend gift, Text resource, Animation bank
        public MapListFullService MapListFullService { get; private set; }
        public MapElementService MapElementService { get; private set; }
        public MapRespawnService MapRespawnService { get; private set; }
        public MapBlockService MapBlockService { get; private set; }
        public MapNpcRespawnService MapNpcRespawnService { get; private set; }
        public MapMusicService MapMusicService { get; private set; }
        public SkillLevelDataService SkillLevelDataService { get; private set; }
        public SkillUpgradeService SkillUpgradeService { get; private set; }
        public SkillBookService SkillBookService { get; private set; }
        public SkillComboService SkillComboService { get; private set; }
        public SkillStateService SkillStateService { get; private set; }
        public SkillMasteryService SkillMasteryService { get; private set; }
        public WorldBossService WorldBossService { get; private set; }
        public AchievementService AchievementService { get; private set; }
        public DailyRewardService DailyRewardService { get; private set; }
        public MallService MallService { get; private set; }
        public FashionService FashionService { get; private set; }
        public SignInService SignInService { get; private set; }
        public TreasureHuntService TreasureHuntService { get; private set; }
        public EncounterService EncounterService { get; private set; }
        public FriendGiftService FriendGiftService { get; private set; }
        public TextResourceService TextResourceService { get; private set; }
        public AnimationBankService AnimationBankService { get; private set; }
        // Batch 10: Faction systems, Guild scripts, Battle maps/rewards/honors, Sơ/Trung/Cao Jin
        public FactionSkillTreeService FactionSkillTreeService { get; private set; }
        public FactionBonusService FactionBonusService { get; private set; }
        public FactionRelationService FactionRelationService { get; private set; }
        public GuildScriptService GuildScriptService { get; private set; }
        public BattleMapConfigService BattleMapConfigService { get; private set; }
        public BattleRewardConfigService BattleRewardConfigService { get; private set; }
        public BattleHonorService BattleHonorService { get; private set; }
        public SjBattleService SjBattleService { get; private set; }
        // Batch 11: Hoa Sơn Luận Kiếm, Sprite asset, Sound effect, Map connection, NPC shop item, Reputation, Title effect, VIP level
        public HuaShanLuanJianService HuaShanLuanJianService { get; private set; }
        public SpriteAssetService SpriteAssetService { get; private set; }
        public SoundEffectService SoundEffectService { get; private set; }
        public MapConnectionService MapConnectionService { get; private set; }
        public NpcShopItemService NpcShopItemService { get; private set; }
        public ReputationService ReputationService { get; private set; }
        public TitleEffectService TitleEffectService { get; private set; }
        public VipLevelService VipLevelService { get; private set; }
        // Batch 12: Guild city war + Script registries (mission, skill, item, event, task, global, library)
        public GuildCityWarService GuildCityWarService { get; private set; }
        public GuildCityWarLogService GuildCityWarLogService { get; private set; }
        public MissionScriptService MissionScriptService { get; private set; }
        public SkillScriptService SkillScriptService { get; private set; }
        public ItemScriptService ItemScriptService { get; private set; }
        public EventScriptService EventScriptService { get; private set; }
        public TaskScriptService TaskScriptService { get; private set; }
        public GlobalScriptService GlobalScriptService { get; private set; }
        public LibraryScriptService LibraryScriptService { get; private set; }
        // Batch 13: Area script registries (14.x GBK areas, faction quest, town, gbk trigger, tong battle)
        public AreaScriptService AreaScriptService { get; private set; }
        public GbkMapScriptService GbkMapScriptService { get; private set; }
        public FactionQuestAreaService FactionQuestAreaService { get; private set; }
        public TownScriptService TownScriptService { get; private set; }
        public GbkTriggerService GbkTriggerService { get; private set; }
        public TongBattleScriptService TongBattleScriptService { get; private set; }
        // PC-parity runtime services batch 6 (client settings + items + maps)
        public PortraitService PortraitService { get; private set; }
        public SoundListService SoundListService { get; private set; }
        public KillerService KillerService { get; private set; }
        public ItemDetailService ItemDetailService { get; private set; }
        public ItemTypeService ItemTypeService { get; private set; }
        public MapTrafficService MapTrafficService { get; private set; }
        public MapTypeService MapTypeService { get; private set; }
        public AdjustColorService AdjustColorService { get; private set; }
        public ClientWeaponSkillService ClientWeaponSkillService { get; private set; }
        public GoldEquipService GoldEquipService { get; private set; }
        public PlatinaEquipService PlatinaEquipService { get; private set; }
        public HorseService HorseService { get; private set; }
        public PotionService PotionService { get; private set; }
        public MagicScriptService MagicScriptService { get; private set; }
        public MagicAttribService MagicAttribService { get; private set; }
        public ScrollService ScrollService { get; private set; }
        public CaveListFullService CaveListFullService { get; private set; }
        public GoldBossService GoldBossService { get; private set; }
        public ChangeFeatureDataService ChangeFeatureDataService { get; private set; }
        public GlobalConfigService GlobalConfigService { get; private set; }
        public NormalSpawnService NormalSpawnService { get; private set; }
        public RareEnchantService RareEnchantService { get; private set; }
        public WharfService WharfService { get; private set; }
        public WaypointService WaypointService { get; private set; }
        public AutoPathRouteService AutoPathRouteService { get; private set; }
        public RevivePosService RevivePosService { get; private set; }
        public FactionConfigService FactionConfigService { get; private set; }
        public NpcResService NpcResService { get; private set; }
        public NpcSFullService NpcSFullService { get; private set; }
        public TongStuntService TongStuntService { get; private set; }
        public TongSettingService TongSettingService { get; private set; }
        public TongNpcPosService TongNpcPosService { get; private set; }
        public MapListService MapListService { get; private set; }
        public MapDescService MapDescService { get; private set; }
        public BossSpawnService BossSpawnService { get; private set; }
        public DropRateConfigService DropRateConfigService { get; private set; }
        // Batch 14: Station/Travel, Guild Workshop/Task, Mission Config services
        public StationService StationService { get; private set; }
        public StationPriceService StationPriceService { get; private set; }
        public WaypointPriceService WaypointPriceService { get; private set; }
        public GuildWorkshopLevelService GuildWorkshopLevelService { get; private set; }
        public GuildTaskDefService GuildTaskDefService { get; private set; }
        public MissionArenaConfigService MissionArenaConfigService { get; private set; }
        public MissionBattleConfigService MissionBattleConfigService { get; private set; }
        public MissionMazeConfigService MissionMazeConfigService { get; private set; }
        public MissionQianchongService MissionQianchongService { get; private set; }
        // Batch 14b: Task detail config
        public TaskDailyConfigService TaskDailyConfigService { get; private set; }
        public TaskRandomConfigService TaskRandomConfigService { get; private set; }
        public TaskLevelLinkService TaskLevelLinkService { get; private set; }
        public TaskTalkConfigService TaskTalkConfigService { get; private set; }
        public TaskEventService TaskEventService { get; private set; }
        // Batch 14c: Object/ItemValue/Music/Weather/Partner/NativePlace/Timer services
        public ObjDataService ObjDataService { get; private set; }
        public ObjectSettingService ObjectSettingService { get; private set; }
        public MusicConfigService MusicConfigService { get; private set; }
        public WeatherConfigService WeatherConfigService { get; private set; }
        public ItemValueService ItemValueService { get; private set; }
        public PartnerEventService PartnerEventService { get; private set; }
        public PartnerBagService PartnerBagService { get; private set; }
        public PartnerSettingService PartnerSettingService { get; private set; }
        public NativePlaceService NativePlaceService { get; private set; }
        public TimerTaskService TimerTaskService { get; private set; }
        // Batch 15: Item sub-types
        public BrokenEquipService BrokenEquipService { get; private set; }
        public FusionService FusionService { get; private set; }
        public MantleService MantleService { get; private set; }
        public MaskService MaskService { get; private set; }
        public SignetService SignetService { get; private set; }
        public ShipinService ShipinService { get; private set; }
        public SuiteActivateCountService SuiteActivateCountService { get; private set; }
        public CompoundScriptService CompoundScriptService { get; private set; }
        // Batch 15b: Config services (forbit, tax, progress, rank, foundry, platina magic, recoin, city hongbao)
        public ForbitItemService ForbitItemService { get; private set; }
        public TaxRateService TaxRateService { get; private set; }
        public ProgressConfigService ProgressConfigService { get; private set; }
        public RankSettingService RankSettingService { get; private set; }
        public FoundryResDemandService FoundryResDemandService { get; private set; }
        public PlatinaMagicRateService PlatinaMagicRateService { get; private set; }
        public RecoinService RecoinService { get; private set; }
        public CityHongbaoService CityHongbaoService { get; private set; }
        // Batch 16: Task tollgate/newtask + remaining config
        public TollgateKillerService TollgateKillerService { get; private set; }
        public NewTaskBranchService NewTaskBranchService { get; private set; }
        public MainPassTaskService MainPassTaskService { get; private set; }
        // Batch 16b: Remaining config services
        public AutoUpdateConfigService AutoUpdateConfigService { get; private set; }
        public TiredWarningService TiredWarningService { get; private set; }
        public PlayerLimitTimeService PlayerLimitTimeService { get; private set; }
        public PermitDialogNpcService PermitDialogNpcService { get; private set; }
        public ProductConfigService ProductConfigService { get; private set; }
        public UtilitiesService UtilitiesService { get; private set; }
        public ForbitHeartService ForbitHeartService { get; private set; }
        public StringResourceCatalogService StringResourceCatalogService { get; private set; }
        private float _combatTickAccumulator;
        // M1.2: Region catalog and report — LAZY-LOADED on first access.
        // RegionCatalog.json is a 12 MB catalog enumerating ALL ~64k PC region .dat
        // files (metadata flags only: hasObstacle/hasNpc/…). It is NOT needed for
        // normal gameplay — per-map geometry/enemy/obstacle data is loaded
        // separately and on-demand by MapManager.LoadMap + MapRenderer.
        // Parsing it synchronously at boot added a large main-thread stall for
        // data that nothing reads at runtime, so it is now deferred. The first
        // read of RegionCatalog triggers the load (and reuses the FastEditor
        // static cache when applicable).
        private RegionCatalogFile _regionCatalogLazy;
        private bool _regionCatalogLoaded;
        private RegionConversionReport _regionReportLazy;
        public RegionCatalogFile RegionCatalog
        {
            get
            {
                if (!_regionCatalogLoaded)
                {
                    _regionCatalogLazy = LoadRegionCatalogForBoot();
                    _regionReportLazy = _regionCatalogLazy != null
                        ? RegionCatalogLoader.ToConversionReport(_regionCatalogLazy)
                        : null;
                    _regionCatalogLoaded = true;
                    if (_regionCatalogLazy != null)
                        SubsystemLog.Info("Sandbox", $"Regions (lazy): {_regionCatalogLazy.totalRegions} loaded");
                }
                return _regionCatalogLazy;
            }
        }
        public RegionConversionReport RegionReport
        {
            get
            {
                // Trigger the lazy load (which also caches the report) and return it.
                _ = RegionCatalog;
                return _regionReportLazy;
            }
        }

        public event Action<SandboxBootReport> OnBootComplete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticsForFastPlayMode()
        {
            Instance = null;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BootReport = new SandboxBootReport();
            using (BootProfilerMarker.Auto())
            {
                InitializeSubsystems();
            }
        }

        // Deferred item/drop/skill loading is launched here (not in Awake) so the
        // synchronous boot — which only needs the single active map — finishes and
        // renders the map first. Start() runs after Awake returns, so the heavy PC
        // item table (~1.4s) is parsed afterwards instead of blocking boot.
        private void Start()
        {
            if (_pendingItemDataLoad)
            {
                _pendingItemDataLoad = false;
                StartCoroutine(LoadItemDataCoroutine());
            }
        }

        public void BootstrapCombatForTests(AssetRegistry registry = null)
        {
            if (Instance == null) 
            {
                var field = typeof(SandboxManager).GetProperty("Instance").GetSetMethod(true);
                field?.Invoke(this, new object[] { this });
            }
            
            if (PcSkillsFull == null)
            {
                PcSkillsFull = PcSkillRegistry.LoadFromDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcSkill"));
            }
            
            AssetRegistry = registry ?? new AssetRegistry();
            BootstrapCombatRuntime();
        }

        private void InitializeSubsystems()
        {
            // CTS-03: Initialize() null-safety + ordering. A missing dependency
            // catalog (RegionCatalog, ItemDb, MapManager.Catalog, AssetRegistry)
            // must NOT crash startup — SandboxManager must still become
            // "initialized" with whatever is available so HUD/services that read
            // via `manager?.XxxService?.Foo` keep working instead of NRE.
            //
            // Ordering:
            //   1. Always-create BootReport if missing (InitSubsystem reads it).
            //   2. Always-create subsystem root GameObjects (so child spawns find a parent).
            //   3. Always-create AssetRegistry (UI services may read from it on Awake).
            //   4. Always-construct MapManager (cheap; can be empty if LoadCatalog fails).
            //   5. Try/catch each catalog/service so a single failure does not abort
            //      the rest. Errors are recorded to BootReport + logged as warnings.
            //   6. Set IsInitialized = true so HUD wiring proceeds regardless of
            //      catalog completeness.
            var bootWatch = Stopwatch.StartNew();
            ActiveBootProfile = ResolveBootProfile();
            // BootReport may already exist (Awake created it). If a caller invokes
            // InitializeSubsystems directly without going through Awake (e.g. an
            // EditMode test fixture or a custom boot orchestrator), create a fresh
            // one so downstream steps that call BootReport.Record do not NRE.
            if (BootReport == null) BootReport = new SandboxBootReport();
            BootReport?.Start(ActiveBootProfile);

            try { InitSubsystem(SubsystemKind.Game, "Game", ref gameRoot); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"InitSubsystem(Game) failed: {e.Message}"); }
            try { InitSubsystem(SubsystemKind.Camera, "Camera", ref cameraRoot); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"InitSubsystem(Camera) failed: {e.Message}"); }
            try { InitSubsystem(SubsystemKind.UI, "UI", ref uiRoot); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"InitSubsystem(UI) failed: {e.Message}"); }
            try { InitSubsystem(SubsystemKind.World, "World", ref worldRoot); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"InitSubsystem(World) failed: {e.Message}"); }
            try { InitSubsystem(SubsystemKind.Debug, "Debug", ref debugRoot); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"InitSubsystem(Debug) failed: {e.Message}"); }
            try { InitSubsystem(SubsystemKind.Services, "Services", ref servicesRoot); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"InitSubsystem(Services) failed: {e.Message}"); }

            IsInitialized = true;

            try { EnsureSandboxCamera(); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"EnsureSandboxCamera failed: {e.Message}"); }

            // M0.6: create shared registry, pass to all systems that need resource lookup
            // (no try/catch — AssetRegistry construction is in-memory and cannot fail under
            // normal use; if it ever does, the exception is informative and the test that
            // called us gets a clear stack trace).
            AssetRegistry = new AssetRegistry();
            try { TimedBootStep("BootstrapCombatRuntime", BootstrapCombatRuntime); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"BootstrapCombatRuntime failed: {e.Message}"); }

            // MapManager + LoadCatalog wrapped together — if LoadCatalog throws on a
            // missing/corrupt catalog, we keep the empty MapManager so subsequent
            // `manager.MapManager != null` checks still hold.
            try
            {
                MapManager = new MapManager(AssetRegistry);
                try { TimedBootStep("MapManager.LoadCatalog", MapManager.LoadCatalog); }
                catch (Exception e) { SubsystemLog.Warn("Sandbox", $"MapManager.LoadCatalog failed: {e.Message}"); }
            }
            catch (Exception e)
            {
                SubsystemLog.Warn("Sandbox", $"MapManager construction failed: {e.Message}");
                MapManager = null;
            }

            // M1.2: Region catalog is now LAZY-LOADED via the RegionCatalog /
            // RegionReport properties. It enumerates ALL ~64k PC region files but is
            // not consumed by gameplay, so parsing the 12 MB JSON at boot was pure
            // overhead. It loads on first access (if ever) instead of here.

            // Instantiate MapRenderer on the worldRoot
            if (worldRoot != null)
            {
                var rendererGo = new GameObject("MapRenderer");
                rendererGo.transform.SetParent(worldRoot, false);
                MapRenderer = rendererGo.AddComponent<MapRenderer>();
                
                // Subscribe to MapManager events
                MapManager.OnMapLoaded += (mapId) => {
                    if (MapManager.ActiveMap != null)
                    {
                        // skipMapVisualsInFastEditorBoot is a BOOT-time optimization
                        // only — it must not suppress visuals when the player
                        // deliberately switches maps at runtime.
                        bool skipVisuals = _initialBootMapLoad
                            && ActiveBootProfile == SandboxBootProfile.FastEditor
                            && skipMapVisualsInFastEditorBoot;
                        if (!skipVisuals)
                            MapRenderer.LoadMapRegions(MapManager.ActiveMap);
                        else
                        {
                            // Apply bounds from map definition so camera/player work without visuals
                            MapRenderer.ApplyBoundsFromDefinition(MapManager.ActiveMap);
                            SubsystemLog.Info("SandboxBoot", "FastEditor: skipped map visual rendering.");
                        }
                        EnsurePlayerController();
                        ApplyActiveMapBoundsToPlayer();
                        EnsureEnemyRuntime();
                        EnsureObjectRuntime();
                        EnsureTrapRuntime();
                        PlacePlayerOnActiveMap();
                        // Enemy/object/trap spawn is only skipped during the FastEditor
                        // boot load. Runtime map switches always spawn content.
                        if (_initialBootMapLoad && ActiveBootProfile == SandboxBootProfile.FastEditor)
                        {
                            SubsystemLog.Info("SandboxBoot", "FastEditor: skipped enemy/object/trap spawn.");
                        }
                        else
                        {
                            SpawnEnemiesForActiveMap();
                            RenderObjectsForActiveMap();
                            BuildTrapsForActiveMap();
                        }
                        // Training NPCs (5 bao cát/cọc gỗ/mộc nhân) are spawned for testing
                        // only on Ba Lăng huyện (their home training ground) — and on
                        // no-map fast boot (handled at the end of boot). Always clear any
                        // leftover training NPCs first so they don't leak into other maps.
                        TrainingSpawner?.Clear();
                        if (mapId == BaLangHuyenMapId)
                            SpawnTrainingNpcs();
                        ConfigureCameraForMap();
                        PlayerController?.SnapCamera();
                    }
                };
                MapManager.OnMapUnloaded += (mapId) => {
                    EnemyRuntime?.Clear();
                    ObjectRuntime?.Clear();
                    TrapRuntime?.Clear();
                    MapRenderer.Clear();
                };

                EnsurePlayerController();

                // ── New Subsystems ──────────────────────────────────
                try { QuestService = new QuestService(); }
                catch (Exception e) { SubsystemLog.Warn("Sandbox", $"new QuestService failed: {e.Message}"); }
                // Item/drop/skill loading is DEFERRED to Start() (LoadItemDataCoroutine),
                // which runs AFTER Awake/InitializeSubsystems returns. Parsing the
                // full PC item table (~28k items, ~1.4s) is not needed to show the
                // single active map, so it is NOT part of the synchronous boot.
                // ItemDb/LootService/InventoryService/ShopService stay null until the
                // coroutine completes, then item-dependent UI panels/GM-login are
                // (re)wired. FastEditor boot keeps the skip flag behaviour below.
                bool skipItems = ActiveBootProfile == SandboxBootProfile.FastEditor && skipItemLoadingInFastEditorBoot;
                if (skipItems)
                {
                    SubsystemLog.Info("SandboxBoot", "FastEditor: skipped item/drop/skill loading.");
                }
                else
                {
                    // Flag for Start() to launch LoadItemDataCoroutine (Awake must not
                    // start it — StartCoroutine in Awake runs the first iteration inline
                    // on some Unity versions, pulling item loading back into boot).
                    _pendingItemDataLoad = true;
                }
                try { AudioService = new AudioService(); }
                catch (Exception e) { SubsystemLog.Warn("Sandbox", $"new AudioService failed: {e.Message}"); }
                if (servicesRoot != null && AudioService != null)
                    AudioService.Initialize(servicesRoot);

                // Wire quest events to combat loot
                if (GameplayLoop != null)
                    GameplayLoop.OnDeath += e =>
                    {
                        if (e.isPlayer)
                        {
                            // Player died -> check for revive pos (RevivePosService using current map or default map)
                            var mapId = MapManager?.ActiveMapId ?? defaultMapId;
                            var revivePos = RevivePosService?.GetDefaultRevivePosition(mapId);
                            if (revivePos != null)
                            {
                                SubsystemLog.Info("Sandbox", $"Player death event -> revive requested at Map={revivePos.MapId}, Pos=({revivePos.PosX}, {revivePos.PosY})");
                                // A real implementation would start a timer, show UI, then call PlayerController.PlaceAt() + restore HP/MP.
                            }
                        }
                        if (!e.isPlayer && e.victimTemplateId != null)
                            QuestService?.UpdateKillObjective(e.victimTemplateId.Value);
                    };

                // Equipment service is cheap and needed by the player visual layer,
                // so it is created at boot. The inventory service (needs the deferred
                // PC item importer) and the OnEquipChanged wiring are set up inside
                // LoadItemDataCoroutine once items are available.
                _equipmentService = new PlayerEquipmentService();
                GmAccessService = new GmAccessService();
                // GmTestServerItemService / EnsureGmLoginInGame need the inventory, so
                // they are created in LoadItemDataCoroutine after items load.

                // Initialize Chat system
                ChatService = new ChatService();
                ChatService.PostSystemMessage("Chào mừng đến Võ Lâm Truyền Kỳ Mobile!");

                // Initialize Party system
                PartyService = new PartyService();

                // Shop system needs ItemDb — created in LoadItemDataCoroutine after
                // items load (kept null until then).

                // ── PC-parity runtime services (meridian, partner, title, …) ────────
                _serviceLoadStatuses.Clear();
                _missingServiceSummaries.Clear();
                _unavailableServiceSummaries.Clear();

                if (ShouldLoadOptionalStreamingServices(ActiveBootProfile))
                {
                    if (Application.isPlaying)
                    {
                        StartCoroutine(LoadOptionalServicesCoroutine());
                    }
                    else
                    {
                        InitializeFastBootFallbackServices();
                    }
                }
                else
                {
                    InitializeFastBootFallbackServices();
                }
                // GmTestServerItemService.EnsureGmLoginInGame() is invoked inside
                // LoadItemDataCoroutine after items/inventory are ready (it grants
                // GM login items, which need the item table).

                // Wire combat events to chat log
                if (GameplayLoop != null)
                    GameplayLoop.OnDeath += e =>
                    {
                        if (!e.isPlayer)
                            ChatService?.PostCombatLog($"{e.victimNameVi} bị giết. +{e.expReward}EXP");
                    };

                // Build mobile UI panels
                try { EnsureMobileUiPanels(); }
                catch (Exception e) { SubsystemLog.Warn("Sandbox", $"EnsureMobileUiPanels failed: {e.Message}"); }

                // Place player at training pentagon center immediately so it never
                // appears at (0,0) before the map finishes loading.
                try { PlacePlayerAtDefaultSpawn(); }
                catch (Exception e) { SubsystemLog.Warn("Sandbox", $"PlacePlayerAtDefaultSpawn failed: {e.Message}"); }

                if (ShouldLoadDefaultMapOnBoot(ActiveBootProfile) && MapManager != null && MapManager.Catalog.ContainsKey(defaultMapId))
                {
                    _initialBootMapLoad = true;
                    try { TimedBootStep("MapManager.LoadMap", () => MapManager.LoadMap(defaultMapId)); }
                    catch (Exception e) { SubsystemLog.Warn("Sandbox", $"MapManager.LoadMap({defaultMapId}) failed: {e.Message}"); }
                    _initialBootMapLoad = false;
                }

                // Training NPCs spawn condition #1: no map loaded (fast boot no-map).
                // (Condition #2 — map is Ba Lăng huyện — is handled in the OnMapLoaded
                // handler above.) When no map is loaded OnMapLoaded never fires, so
                // spawn the 5 training NPCs around the player now for testing.
                if (MapManager != null && MapManager.ActiveMap == null)
                {
                    try
                    {
                        EnsureEnemyRuntime();
                        if (TrainingSpawner != null)
                        {
                            TrainingSpawner.usePlayerPosition = true;
                            SpawnTrainingNpcs();
                            SubsystemLog.Info("SandboxBoot",
                                "FastEditor no-map boot: spawned 5 training NPCs around player for testing.");
                        }
                    }
                    catch (Exception e) { SubsystemLog.Warn("Sandbox", $"Boot training NPC spawn failed: {e.Message}"); }
                }
            }

            bootWatch.Stop();
            BootReport?.Complete(bootWatch.ElapsedMilliseconds);
            LogBootTimingSummary();
            SubsystemLog.Info("Sandbox",
                $"Initialized v{SandboxVersion.Version} ({SandboxVersion.Codename}) " +
                $"profile={ActiveBootProfile} in {bootWatch.ElapsedMilliseconds}ms " +
                $"at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            // Wrapped to ensure subscribers cannot crash SandboxManager boot.
            try { OnBootComplete?.Invoke(BootReport); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"OnBootComplete subscriber threw: {e.Message}"); }
        }

        /// <summary>
        /// Lazily loads the PC item table, drop rates, and skill registry AFTER the
        /// synchronous boot + initial map load, then wires up the item-dependent
        /// services (ItemDb/LootService/InventoryService/ShopService), item UI
        /// panels (Inventory/Shop), and GM-login item grants. Yielding between
        /// heavy steps keeps the main thread responsive while the map is already
        /// visible.
        /// </summary>
        private System.Collections.IEnumerator LoadItemDataCoroutine()
        {
            // Yield first so nothing runs inline in Start(); the map is already
            // rendered by the time the synchronous boot returned.
            yield return null;
            var watch = System.Diagnostics.Stopwatch.StartNew();
            ItemContractImporter importer = null;
            try
            {
                var itemDir = System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcItemFull");
                importer = LoadItemImporterForBoot(itemDir);
            }
            catch (Exception e)
            {
                SubsystemLog.Warn("Sandbox", $"Deferred item loading failed: {e.Message}");
                yield break;
            }
            yield return null;

            try
            {
                ItemDb = new ItemDatabase(importer);
                LootService = new LootDropService(ItemDb);
                var dropRegistry = LoadDropRateRegistryForBoot(
                    System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcDropRate"));
                LootService.AttachRegistry(dropRegistry);
            }
            catch (Exception e)
            {
                SubsystemLog.Warn("Sandbox", $"Deferred ItemDb/Loot setup failed: {e.Message}");
                ItemDb = null;
                LootService = null;
            }
            yield return null;

            try
            {
                PcSkillsFull = LoadPcSkillRegistryForBoot(
                    System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcSkill"));
            }
            catch (Exception e)
            {
                SubsystemLog.Warn("Sandbox", $"Deferred PcSkillRegistry load failed: {e.Message}");
                PcSkillsFull = null;
            }
            yield return null;

            // Wire inventory + equipment events + shop, now that item data exists.
            try
            {
                if (importer != null && _equipmentService != null)
                {
                    _inventoryService = new InventoryService(importer, _equipmentService);
                    _equipmentService.OnEquipChanged += (evt) => {
                        if (PlayerController != null && PlayerController.visual != null)
                        {
                            PlayerController.visual.SetEquipVariant(evt.slot, evt.newVariant);
                            if (evt.slot == PlayerEquipSlot.Weapon)
                            {
                                var wType = PlayerEquipmentService.GetWeaponType(evt.itemId, evt.newVariant);
                                PlayerController.EquipWeapon(wType);
                            }
                        }
                        if (FemalePlayerVisual != null)
                        {
                            FemalePlayerVisual.SetEquipVariant(evt.slot, evt.newVariant);
                            if (evt.slot == PlayerEquipSlot.Weapon)
                            {
                                var wType = PlayerEquipmentService.GetWeaponType(evt.itemId, evt.newVariant);
                                FemalePlayerVisual.SetWeapon(wType);
                            }
                        }
                    };
                }
                ShopService = new ShopService(ItemDb, initialSilver: 5000);
                if (_inventoryService != null && GmAccessService != null)
                    GmTestServerItemService = new GmTestServerItemService(this, _inventoryService, GmAccessService);
            }
            catch (Exception e)
            {
                SubsystemLog.Warn("Sandbox", $"Deferred inventory/shop setup failed: {e.Message}");
            }
            yield return null;

            // (Re)initialize item-dependent UI panels now that ItemDb/inventory exist.
            // The panels were created (null-data) at boot; re-Initialize with real data.
            try
            {
                if (InventoryPanel != null)
                    InventoryPanel.Initialize(ItemDb, _inventoryService);
                else
                    EnsureMobileUiPanels(); // create them if boot skipped panel build
                if (ShopPanel != null && ShopService != null)
                    ShopPanel.Initialize(ShopService, _inventoryService, ItemDb);
                else
                    EnsureMobileUiPanels();
            }
            catch (Exception e)
            {
                SubsystemLog.Warn("Sandbox", $"Deferred item-panel rewire failed: {e.Message}");
            }

            // Grant GM login items now that inventory is ready.
            try { GmTestServerItemService?.EnsureGmLoginInGame(); }
            catch (Exception e) { SubsystemLog.Warn("Sandbox", $"Deferred EnsureGmLoginInGame failed: {e.Message}"); }

            watch.Stop();
            RecordBootTiming("ItemData.LazyLoad", watch.ElapsedMilliseconds);
            SubsystemLog.Info("SandboxBoot", $"Item data lazy-loaded in {watch.ElapsedMilliseconds}ms " +
                $"(ItemDb={(ItemDb != null ? "ok" : "null")}, inv={(_inventoryService != null ? "ok" : "null")}).");
        }

        private System.Collections.IEnumerator LoadOptionalServicesCoroutine()
        {
            // Initial backup / fallback services setup to prevent NREs
            TaskFlagService = new TaskFlagService();
            if (FactionMapRuntimeService == null)
                FactionMapRuntimeService = new FactionMapRuntimeService();
            if (BattleScriptRuntimeService == null)
                BattleScriptRuntimeService = new BattleScriptRuntimeService();

            yield return null;

            // Meridian, Partner, Title, PetSkill, Lottery, CompoundRecipe, QuestItem, Adventure, Guild, AttribConst, MissleCatalog, EventBonus, CityWar, Auction, GoodsCatalog, ShopConfig
            MeridianService = MeridianService.LoadFromStreamingAssets();
            yield return null;
            PartnerService = PartnerService.LoadFromStreamingAssets();
            yield return null;
            PetSkillService = PetSkillService.LoadFromStreamingAssets();
            yield return null;
            TitleService = TitleService.LoadFromStreamingAssets();
            yield return null;
            LotteryService = LotteryService.LoadFromStreamingAssets();
            yield return null;
            CompoundRecipeService = CompoundRecipeService.LoadFromStreamingAssets();
            yield return null;
            QuestItemService = QuestItemService.LoadFromStreamingAssets();
            yield return null;
            AdventureService = AdventureService.LoadFromStreamingAssets();
            yield return null;
            GuildService = GuildService.LoadFromStreamingAssets();
            yield return null;
            AttribConstService = AttribConstService.LoadFromStreamingAssets();
            yield return null;
            MissleCatalogService = MissleCatalogService.LoadFromStreamingAssets();
            yield return null;
            EventBonusService = EventBonusService.LoadFromStreamingAssets();
            yield return null;
            CityWarService = CityWarService.LoadFromStreamingAssets();
            yield return null;
            AuctionService = AuctionService.LoadFromStreamingAssets();
            yield return null;
            GoodsCatalogService = GoodsCatalogService.LoadFromStreamingAssets();
            yield return null;
            ShopConfigService = ShopConfigService.LoadFromStreamingAssets();

            yield return null;

            // ── Batch 2: Battlefield, Instance, Special skills, NPC scripts, etc. ─
            BattlefieldService = LoadOptionalStreamingService(nameof(BattlefieldService), () => BattlefieldService.LoadFromStreamingAssets());
            InstanceMapService = LoadOptionalStreamingService(nameof(InstanceMapService), () => InstanceMapService.LoadFromStreamingAssets());
            HongbaoService = LoadOptionalStreamingService(nameof(HongbaoService), () => HongbaoService.LoadFromStreamingAssets());
            ItemExchangeService = LoadOptionalStreamingService(nameof(ItemExchangeService), () => ItemExchangeService.LoadFromStreamingAssets());
            SpecialSkillService = LoadOptionalStreamingService(nameof(SpecialSkillService), () => SpecialSkillService.LoadFromStreamingAssets());
            NpcSkillService = LoadOptionalStreamingService(nameof(NpcSkillService), () => NpcSkillService.LoadFromStreamingAssets());
            TranslifeSkillService = LoadOptionalStreamingService(nameof(TranslifeSkillService), () => TranslifeSkillService.LoadFromStreamingAssets());
            SkillTemplateService = LoadOptionalStreamingService(nameof(SkillTemplateService), () => SkillTemplateService.LoadFromStreamingAssets());
            NpcLevelScriptService = LoadOptionalStreamingService(nameof(NpcLevelScriptService), () => NpcLevelScriptService.LoadFromStreamingAssets());
            NpcDeathScriptService = LoadOptionalStreamingService(nameof(NpcDeathScriptService), () => NpcDeathScriptService.LoadFromStreamingAssets());
            DailyTaskService = LoadOptionalStreamingService(nameof(DailyTaskService), () => DailyTaskService.LoadFromStreamingAssets());
            BossMissionService = LoadOptionalStreamingService(nameof(BossMissionService), () => BossMissionService.LoadFromStreamingAssets());

            yield return null;

            // ── Batch 3: Events, weather, music, tasks, arena, trip, bonus ───────
            ServerEventService = LoadOptionalStreamingService(nameof(ServerEventService), () => ServerEventService.LoadFromStreamingAssets());
            VngEventService = LoadOptionalStreamingService(nameof(VngEventService), () => VngEventService.LoadFromStreamingAssets());
            BattleScriptService = LoadOptionalStreamingService(nameof(BattleScriptService), () => BattleScriptService.LoadFromStreamingAssets());
            WeatherService = LoadOptionalStreamingService(nameof(WeatherService), () => WeatherService.LoadFromStreamingAssets());
            MusicService = LoadOptionalStreamingService(nameof(MusicService), () => MusicService.LoadFromStreamingAssets());
            GuildWorkshopService = LoadOptionalStreamingService(nameof(GuildWorkshopService), () => GuildWorkshopService.LoadFromStreamingAssets());
            HuoYueDuService = LoadOptionalStreamingService(nameof(HuoYueDuService), () => HuoYueDuService.LoadFromStreamingAssets());
            CityDefenceService = LoadOptionalStreamingService(nameof(CityDefenceService), () => CityDefenceService.LoadFromStreamingAssets());
            ActivityService = LoadOptionalStreamingService(nameof(ActivityService), () => ActivityService.LoadFromStreamingAssets());
            RandomTaskService = LoadOptionalStreamingService(nameof(RandomTaskService), () => RandomTaskService.LoadFromStreamingAssets());
            PartnerTaskService = LoadOptionalStreamingService(nameof(PartnerTaskService), () => PartnerTaskService.LoadFromStreamingAssets());
            MetempsychosisTaskService = LoadOptionalStreamingService(nameof(MetempsychosisTaskService), () => MetempsychosisTaskService.LoadFromStreamingAssets());
            ArenaService = LoadOptionalStreamingService(nameof(ArenaService), () => ArenaService.LoadFromStreamingAssets());
            TripService = LoadOptionalStreamingService(nameof(TripService), () => TripService.LoadFromStreamingAssets());
            BonusOnlineService = LoadOptionalStreamingService(nameof(BonusOnlineService), () => BonusOnlineService.LoadFromStreamingAssets());

            yield return null;

            // ── Batch 4: Guild extras, honor, shitu, foundry, world rank, misc ──
            GuildRankService = LoadOptionalStreamingService(nameof(GuildRankService), () => GuildRankService.LoadFromStreamingAssets());
            GuildStuntService = LoadOptionalStreamingService(nameof(GuildStuntService), () => GuildStuntService.LoadFromStreamingAssets());
            GuildTaskService = LoadOptionalStreamingService(nameof(GuildTaskService), () => GuildTaskService.LoadFromStreamingAssets());
            HonorService = LoadOptionalStreamingService(nameof(HonorService), () => HonorService.LoadFromStreamingAssets());
            ShituService = LoadOptionalStreamingService(nameof(ShituService), () => ShituService.LoadFromStreamingAssets());
            FoundryService = LoadOptionalStreamingService(nameof(FoundryService), () => FoundryService.LoadFromStreamingAssets());
            WorldRankService = LoadOptionalStreamingService(nameof(WorldRankService), () => WorldRankService.LoadFromStreamingAssets());
            NewPlayerGuideService = LoadOptionalStreamingService(nameof(NewPlayerGuideService), () => NewPlayerGuideService.LoadFromStreamingAssets());
            ChangeFeatureService = LoadOptionalStreamingService(nameof(ChangeFeatureService), () => ChangeFeatureService.LoadFromStreamingAssets());
            StallService = LoadOptionalStreamingService(nameof(StallService), () => StallService.LoadFromStreamingAssets());
            FlipCardService = LoadOptionalStreamingService(nameof(FlipCardService), () => FlipCardService.LoadFromStreamingAssets());
            BaoRuongThanBiService = LoadOptionalStreamingService(nameof(BaoRuongThanBiService), () => BaoRuongThanBiService.LoadFromStreamingAssets());
            SeasonalEventService = LoadOptionalStreamingService(nameof(SeasonalEventService), () => SeasonalEventService.LoadFromStreamingAssets());
            CompensationService = LoadOptionalStreamingService(nameof(CompensationService), () => CompensationService.LoadFromStreamingAssets());

            yield return null;

            // ── Batch 5: Final client systems (faction maps, awards, double exp, sim city, client skill scripts) ─
            FactionMapService = LoadOptionalStreamingService(nameof(FactionMapService), () => FactionMapService.LoadFromStreamingAssets());
            BattleAwardService = LoadOptionalStreamingService(nameof(BattleAwardService), () => BattleAwardService.LoadFromStreamingAssets());
            DoubleExpService = LoadOptionalStreamingService(nameof(DoubleExpService), () => DoubleExpService.LoadFromStreamingAssets());
            SimCityPluginService = LoadOptionalStreamingService(nameof(SimCityPluginService), () => SimCityPluginService.LoadFromStreamingAssets());
            ClientSkillScriptService = LoadOptionalStreamingService(nameof(ClientSkillScriptService), () => ClientSkillScriptService.LoadFromStreamingAssets());
            TongJinBattleService = LoadOptionalStreamingService(nameof(TongJinBattleService), () => TongJinBattleService.LoadFromStreamingAssets());
            BangChienService = LoadOptionalStreamingService(nameof(BangChienService), () => BangChienService.LoadFromStreamingAssets());
            BossHoangKimService = LoadOptionalStreamingService(nameof(BossHoangKimService), () => BossHoangKimService.LoadFromStreamingAssets());

            yield return null;

            SaveSlotService = LoadOptionalStreamingService(nameof(SaveSlotService), () => SaveSlotService.LoadFromStreamingAssets());
            MailService = LoadOptionalStreamingService(nameof(MailService), () => MailService.LoadFromStreamingAssets());
            MountService = LoadOptionalStreamingService(nameof(MountService), () => MountService.LoadFromStreamingAssets());
            RankingService = LoadOptionalStreamingService(nameof(RankingService), () => RankingService.LoadFromStreamingAssets());
            FriendService = LoadOptionalStreamingService(nameof(FriendService), () => FriendService.LoadFromStreamingAssets());
            PetService = LoadOptionalStreamingService(nameof(PetService), () => PetService.LoadFromStreamingAssets());
            ShopConfigService = LoadOptionalStreamingService(nameof(ShopConfigService), () => ShopConfigService.LoadFromStreamingAssets());
            MissileEffectService = LoadOptionalStreamingService(nameof(MissileEffectService), () => MissileEffectService.LoadFromStreamingAssets());
            HudArtCatalogService = LoadOptionalStreamingService(nameof(HudArtCatalogService), () => HudArtCatalogService.LoadFromStreamingAssets());
            FactionMapRuntimeService = FactionMapRuntimeService != null ? FactionMapRuntimeService : new FactionMapRuntimeService(FactionMapService);
            BattleScriptRuntimeService = BattleScriptRuntimeService != null ? BattleScriptRuntimeService : new BattleScriptRuntimeService(BattleScriptService);
            TaskFlagRegistryService = LoadOptionalStreamingService(nameof(TaskFlagRegistryService), () => TaskFlagRegistryService.LoadFromStreamingAssets());

            yield return null;

            MapListFullService = LoadOptionalStreamingService(nameof(MapListFullService), () => MapListFullService.LoadFromStreamingAssets());
            MapElementService = LoadOptionalStreamingService(nameof(MapElementService), () => MapElementService.LoadFromStreamingAssets());
            MapRespawnService = LoadOptionalStreamingService(nameof(MapRespawnService), () => MapRespawnService.LoadFromStreamingAssets());
            MapBlockService = LoadOptionalStreamingService(nameof(MapBlockService), () => MapBlockService.LoadFromStreamingAssets());
            MapNpcRespawnService = LoadOptionalStreamingService(nameof(MapNpcRespawnService), () => MapNpcRespawnService.LoadFromStreamingAssets());
            MapMusicService = LoadOptionalStreamingService(nameof(MapMusicService), () => MapMusicService.LoadFromStreamingAssets());
            SkillLevelDataService = LoadOptionalStreamingService(nameof(SkillLevelDataService), () => SkillLevelDataService.LoadFromStreamingAssets());
            SkillUpgradeService = LoadOptionalStreamingService(nameof(SkillUpgradeService), () => SkillUpgradeService.LoadFromStreamingAssets());
            SkillBookService = LoadOptionalStreamingService(nameof(SkillBookService), () => SkillBookService.LoadFromStreamingAssets());
            SkillComboService = LoadOptionalStreamingService(nameof(SkillComboService), () => SkillComboService.LoadFromStreamingAssets());
            SkillStateService = LoadOptionalStreamingService(nameof(SkillStateService), () => SkillStateService.LoadFromStreamingAssets());
            SkillMasteryService = LoadOptionalStreamingService(nameof(SkillMasteryService), () => SkillMasteryService.LoadFromStreamingAssets());
            WorldBossService = LoadOptionalStreamingService(nameof(WorldBossService), () => WorldBossService.LoadFromStreamingAssets());
            AchievementService = LoadOptionalStreamingService(nameof(AchievementService), () => AchievementService.LoadFromStreamingAssets());
            DailyRewardService = LoadOptionalStreamingService(nameof(DailyRewardService), () => DailyRewardService.LoadFromStreamingAssets());
            MallService = LoadOptionalStreamingService(nameof(MallService), () => MallService.LoadFromStreamingAssets());
            FashionService = LoadOptionalStreamingService(nameof(FashionService), () => FashionService.LoadFromStreamingAssets());
            SignInService = LoadOptionalStreamingService(nameof(SignInService), () => SignInService.LoadFromStreamingAssets());
            TreasureHuntService = LoadOptionalStreamingService(nameof(TreasureHuntService), () => TreasureHuntService.LoadFromStreamingAssets());
            EncounterService = LoadOptionalStreamingService(nameof(EncounterService), () => EncounterService.LoadFromStreamingAssets());
            FriendGiftService = LoadOptionalStreamingService(nameof(FriendGiftService), () => FriendGiftService.LoadFromStreamingAssets());
            TextResourceService = LoadOptionalStreamingService(nameof(TextResourceService), () => TextResourceService.LoadFromStreamingAssets());
            AnimationBankService = LoadOptionalStreamingService(nameof(AnimationBankService), () => AnimationBankService.LoadFromStreamingAssets());

            yield return null;

            FactionSkillTreeService = LoadOptionalStreamingService(nameof(FactionSkillTreeService), () => FactionSkillTreeService.LoadFromStreamingAssets());
            FactionBonusService = LoadOptionalStreamingService(nameof(FactionBonusService), () => FactionBonusService.LoadFromStreamingAssets());
            FactionRelationService = LoadOptionalStreamingService(nameof(FactionRelationService), () => FactionRelationService.LoadFromStreamingAssets());
            GuildScriptService = LoadOptionalStreamingService(nameof(GuildScriptService), () => GuildScriptService.LoadFromStreamingAssets());
            BattleMapConfigService = LoadOptionalStreamingService(nameof(BattleMapConfigService), () => BattleMapConfigService.LoadFromStreamingAssets());
            BattleRewardConfigService = LoadOptionalStreamingService(nameof(BattleRewardConfigService), () => BattleRewardConfigService.LoadFromStreamingAssets());
            BattleHonorService = LoadOptionalStreamingService(nameof(BattleHonorService), () => BattleHonorService.LoadFromStreamingAssets());
            SjBattleService = LoadOptionalStreamingService(nameof(SjBattleService), () => SjBattleService.LoadFromStreamingAssets());

            yield return null;

            HuaShanLuanJianService = LoadOptionalStreamingService(nameof(HuaShanLuanJianService), () => HuaShanLuanJianService.LoadFromStreamingAssets());
            SpriteAssetService = LoadOptionalStreamingService(nameof(SpriteAssetService), () => SpriteAssetService.LoadFromStreamingAssets());
            SoundEffectService = LoadOptionalStreamingService(nameof(SoundEffectService), () => SoundEffectService.LoadFromStreamingAssets());
            MapConnectionService = LoadOptionalStreamingService(nameof(MapConnectionService), () => MapConnectionService.LoadFromStreamingAssets());
            NpcShopItemService = LoadOptionalStreamingService(nameof(NpcShopItemService), () => NpcShopItemService.LoadFromStreamingAssets());
            ReputationService = LoadOptionalStreamingService(nameof(ReputationService), () => ReputationService.LoadFromStreamingAssets());
            TitleEffectService = LoadOptionalStreamingService(nameof(TitleEffectService), () => TitleEffectService.LoadFromStreamingAssets());
            VipLevelService = LoadOptionalStreamingService(nameof(VipLevelService), () => VipLevelService.LoadFromStreamingAssets());

            yield return null;

            GuildCityWarService = new GuildCityWarService(CityWarService);
            GuildCityWarLogService = LoadOptionalStreamingService(nameof(GuildCityWarLogService), () => GuildCityWarLogService.LoadFromStreamingAssets());
            MissionScriptService = LoadOptionalStreamingService(nameof(MissionScriptService), () => MissionScriptService.LoadFromStreamingAssets());
            SkillScriptService = LoadOptionalStreamingService(nameof(SkillScriptService), () => SkillScriptService.LoadFromStreamingAssets());
            ItemScriptService = LoadOptionalStreamingService(nameof(ItemScriptService), () => ItemScriptService.LoadFromStreamingAssets());
            EventScriptService = LoadOptionalStreamingService(nameof(EventScriptService), () => EventScriptService.LoadFromStreamingAssets());
            TaskScriptService = LoadOptionalStreamingService(nameof(TaskScriptService), () => TaskScriptService.LoadFromStreamingAssets());
            GlobalScriptService = LoadOptionalStreamingService(nameof(GlobalScriptService), () => GlobalScriptService.LoadFromStreamingAssets());
            LibraryScriptService = LoadOptionalStreamingService(nameof(LibraryScriptService), () => LibraryScriptService.LoadFromStreamingAssets());

            yield return null;

            AreaScriptService = LoadOptionalStreamingService(nameof(AreaScriptService), () => AreaScriptService.LoadFromStreamingAssets());
            GbkMapScriptService = LoadOptionalStreamingService(nameof(GbkMapScriptService), () => GbkMapScriptService.LoadFromStreamingAssets());
            FactionQuestAreaService = LoadOptionalStreamingService(nameof(FactionQuestAreaService), () => FactionQuestAreaService.LoadFromStreamingAssets());
            TownScriptService = LoadOptionalStreamingService(nameof(TownScriptService), () => TownScriptService.LoadFromStreamingAssets());
            GbkTriggerService = LoadOptionalStreamingService(nameof(GbkTriggerService), () => GbkTriggerService.LoadFromStreamingAssets());
            TongBattleScriptService = LoadOptionalStreamingService(nameof(TongBattleScriptService), () => TongBattleScriptService.LoadFromStreamingAssets());

            yield return null;

            PortraitService = LoadOptionalStreamingService(nameof(PortraitService), () => PortraitService.LoadFromStreamingAssets());
            SoundListService = LoadOptionalStreamingService(nameof(SoundListService), () => SoundListService.LoadFromStreamingAssets());
            KillerService = LoadOptionalStreamingService(nameof(KillerService), () => KillerService.LoadFromStreamingAssets());
            ItemDetailService = LoadOptionalStreamingService(nameof(ItemDetailService), () => ItemDetailService.LoadFromStreamingAssets());
            ItemTypeService = LoadOptionalStreamingService(nameof(ItemTypeService), () => ItemTypeService.LoadFromStreamingAssets());
            MapTrafficService = LoadOptionalStreamingService(nameof(MapTrafficService), () => MapTrafficService.LoadFromStreamingAssets());
            MapTypeService = LoadOptionalStreamingService(nameof(MapTypeService), () => MapTypeService.LoadFromStreamingAssets());
            AdjustColorService = LoadOptionalStreamingService(nameof(AdjustColorService), () => AdjustColorService.LoadFromStreamingAssets());
            ClientWeaponSkillService = LoadOptionalStreamingService(nameof(ClientWeaponSkillService), () => ClientWeaponSkillService.LoadFromStreamingAssets());
            GoldEquipService = LoadOptionalStreamingService(nameof(GoldEquipService), () => GoldEquipService.LoadFromStreamingAssets());
            PlatinaEquipService = LoadOptionalStreamingService(nameof(PlatinaEquipService), () => PlatinaEquipService.LoadFromStreamingAssets());
            HorseService = LoadOptionalStreamingService(nameof(HorseService), () => HorseService.LoadFromStreamingAssets());
            PotionService = LoadOptionalStreamingService(nameof(PotionService), () => PotionService.LoadFromStreamingAssets());
            MagicScriptService = LoadOptionalStreamingService(nameof(MagicScriptService), () => MagicScriptService.LoadFromStreamingAssets());
            MagicAttribService = LoadOptionalStreamingService(nameof(MagicAttribService), () => MagicAttribService.LoadFromStreamingAssets());
            ScrollService = LoadOptionalStreamingService(nameof(ScrollService), () => ScrollService.LoadFromStreamingAssets());
            CaveListFullService = LoadOptionalStreamingService(nameof(CaveListFullService), () => CaveListFullService.LoadFromStreamingAssets());
            GoldBossService = LoadOptionalStreamingService(nameof(GoldBossService), () => GoldBossService.LoadFromStreamingAssets());
            ChangeFeatureDataService = LoadOptionalStreamingService(nameof(ChangeFeatureDataService), () => ChangeFeatureDataService.LoadFromStreamingAssets());
            GlobalConfigService = LoadOptionalStreamingService(nameof(GlobalConfigService), () => GlobalConfigService.LoadFromStreamingAssets());
            NormalSpawnService = LoadOptionalStreamingService(nameof(NormalSpawnService), () => NormalSpawnService.LoadFromStreamingAssets());
            RareEnchantService = LoadOptionalStreamingService(nameof(RareEnchantService), () => RareEnchantService.LoadFromStreamingAssets());
            WharfService = LoadOptionalStreamingService(nameof(WharfService), () => WharfService.LoadFromStreamingAssets());
            WaypointService = LoadOptionalStreamingService(nameof(WaypointService), () => WaypointService.LoadFromStreamingAssets());
            AutoPathRouteService = LoadOptionalStreamingService(nameof(AutoPathRouteService), () => AutoPathRouteService.LoadFromStreamingAssets());
            RevivePosService = LoadOptionalStreamingService(nameof(RevivePosService), () => RevivePosService.LoadFromStreamingAssets());
            FactionConfigService = LoadOptionalStreamingService(nameof(FactionConfigService), () => FactionConfigService.LoadFromStreamingAssets());
            NpcResService = LoadOptionalStreamingService(nameof(NpcResService), () => NpcResService.LoadFromStreamingAssets());
            NpcSFullService = LoadOptionalStreamingService(nameof(NpcSFullService), () => NpcSFullService.LoadFromStreamingAssets());
            TongStuntService = LoadOptionalStreamingService(nameof(TongStuntService), () => TongStuntService.LoadFromStreamingAssets());
            TongSettingService = LoadOptionalStreamingService(nameof(TongSettingService), () => TongSettingService.LoadFromStreamingAssets());
            TongNpcPosService = LoadOptionalStreamingService(nameof(TongNpcPosService), () => TongNpcPosService.LoadFromStreamingAssets());
            MapListService = LoadOptionalStreamingService(nameof(MapListService), () => MapListService.LoadFromStreamingAssets());
            MapDescService = LoadOptionalStreamingService(nameof(MapDescService), () => MapDescService.LoadFromStreamingAssets());
            BossSpawnService = LoadOptionalStreamingService(nameof(BossSpawnService), () => BossSpawnService.LoadFromStreamingAssets());
            DropRateConfigService = LoadOptionalStreamingService(nameof(DropRateConfigService), () => DropRateConfigService.LoadFromStreamingAssets());

            yield return null;

            StationService = LoadOptionalStreamingService(nameof(StationService), () => StationService.LoadFromStreamingAssets());
            StationPriceService = LoadOptionalStreamingService(nameof(StationPriceService), () => StationPriceService.LoadFromStreamingAssets());
            WaypointPriceService = LoadOptionalStreamingService(nameof(WaypointPriceService), () => WaypointPriceService.LoadFromStreamingAssets());
            GuildWorkshopLevelService = LoadOptionalStreamingService(nameof(GuildWorkshopLevelService), () => GuildWorkshopLevelService.LoadFromStreamingAssets());
            GuildTaskDefService = LoadOptionalStreamingService(nameof(GuildTaskDefService), () => GuildTaskDefService.LoadFromStreamingAssets());
            MissionArenaConfigService = LoadOptionalStreamingService(nameof(MissionArenaConfigService), () => MissionArenaConfigService.LoadFromStreamingAssets());
            MissionBattleConfigService = LoadOptionalStreamingService(nameof(MissionBattleConfigService), () => MissionBattleConfigService.LoadFromStreamingAssets());
            MissionMazeConfigService = LoadOptionalStreamingService(nameof(MissionMazeConfigService), () => MissionMazeConfigService.LoadFromStreamingAssets());
            MissionQianchongService = LoadOptionalStreamingService(nameof(MissionQianchongService), () => MissionQianchongService.LoadFromStreamingAssets());

            yield return null;

            TaskDailyConfigService = LoadOptionalStreamingService(nameof(TaskDailyConfigService), () => TaskDailyConfigService.LoadFromStreamingAssets());
            TaskRandomConfigService = LoadOptionalStreamingService(nameof(TaskRandomConfigService), () => TaskRandomConfigService.LoadFromStreamingAssets());
            TaskLevelLinkService = LoadOptionalStreamingService(nameof(TaskLevelLinkService), () => TaskLevelLinkService.LoadFromStreamingAssets());
            TaskTalkConfigService = LoadOptionalStreamingService(nameof(TaskTalkConfigService), () => TaskTalkConfigService.LoadFromStreamingAssets());
            TaskEventService = LoadOptionalStreamingService(nameof(TaskEventService), () => TaskEventService.LoadFromStreamingAssets());

            yield return null;

            ObjDataService = LoadOptionalStreamingService(nameof(ObjDataService), () => ObjDataService.LoadFromStreamingAssets());
            ObjectSettingService = LoadOptionalStreamingService(nameof(ObjectSettingService), () => ObjectSettingService.LoadFromStreamingAssets());
            MusicConfigService = LoadOptionalStreamingService(nameof(MusicConfigService), () => MusicConfigService.LoadFromStreamingAssets());
            WeatherConfigService = LoadOptionalStreamingService(nameof(WeatherConfigService), () => WeatherConfigService.LoadFromStreamingAssets());
            ItemValueService = LoadOptionalStreamingService(nameof(ItemValueService), () => ItemValueService.LoadFromStreamingAssets());
            PartnerEventService = LoadOptionalStreamingService(nameof(PartnerEventService), () => PartnerEventService.LoadFromStreamingAssets());
            PartnerBagService = LoadOptionalStreamingService(nameof(PartnerBagService), () => PartnerBagService.LoadFromStreamingAssets());
            PartnerSettingService = LoadOptionalStreamingService(nameof(PartnerSettingService), () => PartnerSettingService.LoadFromStreamingAssets());
            NativePlaceService = LoadOptionalStreamingService(nameof(NativePlaceService), () => NativePlaceService.LoadFromStreamingAssets());
            TimerTaskService = LoadOptionalStreamingService(nameof(TimerTaskService), () => TimerTaskService.LoadFromStreamingAssets());

            yield return null;

            BrokenEquipService = LoadOptionalStreamingService(nameof(BrokenEquipService), () => BrokenEquipService.LoadFromStreamingAssets());
            FusionService = LoadOptionalStreamingService(nameof(FusionService), () => FusionService.LoadFromStreamingAssets());
            MantleService = LoadOptionalStreamingService(nameof(MantleService), () => MantleService.LoadFromStreamingAssets());
            MaskService = LoadOptionalStreamingService(nameof(MaskService), () => MaskService.LoadFromStreamingAssets());
            SignetService = LoadOptionalStreamingService(nameof(SignetService), () => SignetService.LoadFromStreamingAssets());
            ShipinService = LoadOptionalStreamingService(nameof(ShipinService), () => ShipinService.LoadFromStreamingAssets());
            SuiteActivateCountService = LoadOptionalStreamingService(nameof(SuiteActivateCountService), () => SuiteActivateCountService.LoadFromStreamingAssets());
            CompoundScriptService = LoadOptionalStreamingService(nameof(CompoundScriptService), () => CompoundScriptService.LoadFromStreamingAssets());

            yield return null;

            ForbitItemService = LoadOptionalStreamingService(nameof(ForbitItemService), () => ForbitItemService.LoadFromStreamingAssets());
            TaxRateService = LoadOptionalStreamingService(nameof(TaxRateService), () => TaxRateService.LoadFromStreamingAssets());
            ProgressConfigService = LoadOptionalStreamingService(nameof(ProgressConfigService), () => ProgressConfigService.LoadFromStreamingAssets());
            RankSettingService = LoadOptionalStreamingService(nameof(RankSettingService), () => RankSettingService.LoadFromStreamingAssets());
            FoundryResDemandService = LoadOptionalStreamingService(nameof(FoundryResDemandService), () => FoundryResDemandService.LoadFromStreamingAssets());
            PlatinaMagicRateService = LoadOptionalStreamingService(nameof(PlatinaMagicRateService), () => PlatinaMagicRateService.LoadFromStreamingAssets());
            RecoinService = LoadOptionalStreamingService(nameof(RecoinService), () => RecoinService.LoadFromStreamingAssets());
            CityHongbaoService = LoadOptionalStreamingService(nameof(CityHongbaoService), () => CityHongbaoService.LoadFromStreamingAssets());

            yield return null;

            TollgateKillerService = LoadOptionalStreamingService(nameof(TollgateKillerService), () => TollgateKillerService.LoadFromStreamingAssets());
            NewTaskBranchService = LoadOptionalStreamingService(nameof(NewTaskBranchService), () => NewTaskBranchService.LoadFromStreamingAssets());
            MainPassTaskService = LoadOptionalStreamingService(nameof(MainPassTaskService), () => MainPassTaskService.LoadFromStreamingAssets());

            yield return null;

            AutoUpdateConfigService = LoadOptionalStreamingService(nameof(AutoUpdateConfigService), () => AutoUpdateConfigService.LoadFromStreamingAssets());
            TiredWarningService = LoadOptionalStreamingService(nameof(TiredWarningService), () => TiredWarningService.LoadFromStreamingAssets());
            PlayerLimitTimeService = LoadOptionalStreamingService(nameof(PlayerLimitTimeService), () => PlayerLimitTimeService.LoadFromStreamingAssets());
            PermitDialogNpcService = LoadOptionalStreamingService(nameof(PermitDialogNpcService), () => PermitDialogNpcService.LoadFromStreamingAssets());
            ProductConfigService = LoadOptionalStreamingService(nameof(ProductConfigService), () => ProductConfigService.LoadFromStreamingAssets());
            UtilitiesService = LoadOptionalStreamingService(nameof(UtilitiesService), () => UtilitiesService.LoadFromStreamingAssets());
            ForbitHeartService = LoadOptionalStreamingService(nameof(ForbitHeartService), () => ForbitHeartService.LoadFromStreamingAssets());
            StringResourceCatalogService = LoadOptionalStreamingService(nameof(StringResourceCatalogService), () => StringResourceCatalogService.LoadFromStreamingAssets());

            LogOptionalServiceSummary();
            GmTestServerItemService?.EnsureGmLoginInGame();
        }


        private SandboxBootProfile ResolveBootProfile()
        {
#if UNITY_EDITOR
            if (useFastEditorBoot)
                return SandboxBootProfile.FastEditor;
#endif
            return bootProfile;
        }

        private bool ShouldLoadOptionalStreamingServices(SandboxBootProfile profile)
        {
            if (profile == SandboxBootProfile.FastEditor)
                return loadOptionalServicesInFastEditorBoot;
            return true;
        }

        private bool ShouldLoadDefaultMapOnBoot(SandboxBootProfile profile)
        {
            if (!loadDefaultMapOnBoot)
                return false;
            if (profile == SandboxBootProfile.FastEditor)
                return loadDefaultMapInFastEditorBoot;
            return true;
        }

        private bool ShouldUseFastEditorReferenceDataCache()
        {
#if UNITY_EDITOR
            return ActiveBootProfile == SandboxBootProfile.FastEditor && cacheReferenceDataInFastEditorBoot;
#else
            return false;
#endif
        }

        private RegionCatalogFile LoadRegionCatalogForBoot()
        {
            if (ShouldUseFastEditorReferenceDataCache() && _fastEditorRegionCatalogCache != null)
            {
                RecordBootTiming("RegionCatalog.CacheHit", 0);
                return _fastEditorRegionCatalogCache;
            }

            var regionCat = TimedBootStep("RegionCatalog.Load", RegionCatalogLoader.LoadFromStreamingAssets);
            if (ShouldUseFastEditorReferenceDataCache() && regionCat != null)
                _fastEditorRegionCatalogCache = regionCat;
            return regionCat;
        }

        private ItemContractImporter LoadItemImporterForBoot(string itemDir)
        {
            if (ShouldUseFastEditorReferenceDataCache() &&
                _fastEditorItemImporterCache != null &&
                string.Equals(_fastEditorItemDirCacheKey, itemDir, StringComparison.Ordinal))
            {
                RecordBootTiming("PcItemReferenceData.CacheHit", 0);
                return _fastEditorItemImporterCache;
            }

            var importer = TimedBootStep("PcItemBatchLoader.ImportInto",
                () => PcItemBatchLoader.ImportInto(itemDir));
            TimedBootStep("PcMagicScriptItemParser.ImportInto",
                () => PcMagicScriptItemParser.ImportInto(itemDir, importer));

            if (ShouldUseFastEditorReferenceDataCache())
            {
                _fastEditorItemDirCacheKey = itemDir;
                _fastEditorItemImporterCache = importer;
            }

            return importer;
        }

        private DropRateRegistry LoadDropRateRegistryForBoot(string dropDir)
        {
            if (ShouldUseFastEditorReferenceDataCache() &&
                _fastEditorDropRegistryCache != null &&
                string.Equals(_fastEditorDropDirCacheKey, dropDir, StringComparison.Ordinal))
            {
                RecordBootTiming("DropRateRegistry.CacheHit", 0);
                return _fastEditorDropRegistryCache;
            }

            var dropRegistry = new DropRateRegistry();
            TimedBootStep("DropRateRegistry.LoadDirectory", () => dropRegistry.LoadDirectory(dropDir));

            if (ShouldUseFastEditorReferenceDataCache())
            {
                _fastEditorDropDirCacheKey = dropDir;
                _fastEditorDropRegistryCache = dropRegistry;
            }

            return dropRegistry;
        }

        private PcSkillRegistry LoadPcSkillRegistryForBoot(string skillDir)
        {
            if (ShouldUseFastEditorReferenceDataCache() &&
                _fastEditorSkillRegistryCache != null &&
                string.Equals(_fastEditorSkillDirCacheKey, skillDir, StringComparison.Ordinal))
            {
                RecordBootTiming("PcSkillRegistry.CacheHit", 0);
                return _fastEditorSkillRegistryCache;
            }

            var registry = TimedBootStep("PcSkillRegistry.LoadFromDirectory",
                () => PcSkillRegistry.LoadFromDirectory(skillDir));

            if (ShouldUseFastEditorReferenceDataCache())
            {
                _fastEditorSkillDirCacheKey = skillDir;
                _fastEditorSkillRegistryCache = registry;
            }

            return registry;
        }

        private void InitializeFastBootFallbackServices()
        {
            TaskFlagService = new TaskFlagService();
            if (FactionMapRuntimeService == null)
                FactionMapRuntimeService = new FactionMapRuntimeService();
            if (BattleScriptRuntimeService == null)
                BattleScriptRuntimeService = new BattleScriptRuntimeService();

            RecordServiceStatus(
                "OptionalStreamingServices",
                FastBootOptionalServicesSource,
                SandboxServiceDataStatus.SkippedForFastBoot,
                0,
                "FastEditor boot bỏ qua batch optional StreamingAssets; tắt useFastEditorBoot để nạp đầy đủ.");
            SubsystemLog.Info("SandboxBoot",
                "FastEditor boot: skipped optional StreamingAssets service batches.");
        }

        private T TimedBootStep<T>(string stepName, Func<T> action)
        {
            var watch = Stopwatch.StartNew();
            using (BootStepProfilerMarker.Auto())
            {
                try
                {
                    return action();
                }
                finally
                {
                    watch.Stop();
                    RecordBootTiming(stepName, watch.ElapsedMilliseconds);
                }
            }
        }

        private void TimedBootStep(string stepName, Action action)
        {
            var watch = Stopwatch.StartNew();
            using (BootStepProfilerMarker.Auto())
            {
                try
                {
                    action();
                }
                finally
                {
                    watch.Stop();
                    RecordBootTiming(stepName, watch.ElapsedMilliseconds);
                }
            }
        }

        private void RecordBootTiming(string stepName, long milliseconds)
        {
            BootReport?.RecordTiming(stepName, milliseconds);
            if (logBootTimings && milliseconds >= Math.Max(0, bootTimingLogThresholdMs))
                SubsystemLog.Info("SandboxBoot", $"{stepName}: {milliseconds}ms");
        }

        private void LogBootTimingSummary()
        {
            if (!logBootTimings || BootReport == null)
                return;

            SubsystemLog.Info("SandboxBoot",
                $"profile={BootReport.BootProfile}, total={BootReport.TotalMilliseconds}ms, slowest={BuildBootTimingSummary(5)}");
        }

        private string BuildBootTimingSummary(int maxSteps)
        {
            if (BootReport == null || BootReport.Timings.Count == 0)
                return "(none)";

            // Only consider timings recorded during the synchronous boot phase.
            // Deferred/async steps (e.g. item table lazy-loaded after the map is
            // shown) are recorded AFTER Complete() and reported separately so the
            // synchronous-boot `slowest` breakdown is not polluted by them.
            int syncCount = Math.Min(BootReport.SynchronousTimingCount, BootReport.Timings.Count);
            var timings = new List<(string stepName, long milliseconds)>(syncCount);
            for (int i = 0; i < syncCount; i++)
                timings.Add(BootReport.Timings[i]);
            timings.Sort((a, b) => b.milliseconds.CompareTo(a.milliseconds));
            int count = Math.Min(Math.Max(0, maxSteps), timings.Count);
            var parts = new List<string>(count);
            for (int i = 0; i < count; i++)
                parts.Add($"{timings[i].stepName}={timings[i].milliseconds}ms");
            return string.Join(", ", parts);
        }


        private T LoadOptionalStreamingService<T>(string serviceName, Func<T> loader) where T : class
        {
            string relativePath = GetDefaultStreamingDir(typeof(T));
            if (!string.IsNullOrEmpty(relativePath) && !StreamingSourceExists(relativePath))
            {
                RecordServiceStatus(serviceName, relativePath, SandboxServiceDataStatus.MissingData, 0,
                    "Thiếu data StreamingAssets; service không được auto-wire.");
                return null;
            }

            try
            {
                var service = loader();
                int count = GetServiceCount(service);
                if (count == 0)
                {
                    RecordServiceStatus(serviceName, relativePath, SandboxServiceDataStatus.Unavailable, count,
                        "Parser không nạp được record nào; service không được auto-wire như feature sẵn sàng.");
                    return null;
                }

                string countMessage = count > 0 ? $"Đã nạp {count} record." : "Đã khởi tạo service (không có Count public).";
                RecordServiceStatus(serviceName, relativePath, SandboxServiceDataStatus.Loaded, count, countMessage);
                return service;
            }
            catch (Exception e)
            {
                RecordServiceStatus(serviceName, relativePath, SandboxServiceDataStatus.Error, 0, e.Message);
                return null;
            }
        }

        private static string GetDefaultStreamingDir(Type serviceType)
        {
            var field = serviceType.GetField("DefaultStreamingDir",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            return field?.GetValue(null) as string;
        }

        private static int GetServiceCount(object service)
        {
            if (service == null) return 0;
            var prop = service.GetType().GetProperty("Count",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (prop == null || prop.PropertyType != typeof(int)) return -1;
            return (int)prop.GetValue(service);
        }

        private static bool StreamingSourceExists(string relativePath)
        {
            string fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, relativePath);
            if (System.IO.Directory.Exists(fullPath))
                return HasDataFiles(fullPath);
            return System.IO.File.Exists(fullPath);
        }

        private static bool HasDataFiles(string directory)
        {
            try
            {
                foreach (var file in System.IO.Directory.EnumerateFiles(directory, "*", System.IO.SearchOption.AllDirectories))
                {
                    string name = System.IO.Path.GetFileName(file);
                    if (name.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)) continue;
                    if (name.StartsWith(".", StringComparison.Ordinal)) continue;
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        private void RecordServiceStatus(string serviceName, string relativePath, SandboxServiceDataStatus status, int count, string message)
        {
            var entry = new SandboxServiceLoadStatus
            {
                serviceName = serviceName,
                relativePath = relativePath,
                status = status,
                count = count,
                message = message,
            };
            _serviceLoadStatuses[serviceName] = entry;

            string source = string.IsNullOrEmpty(relativePath) ? "(không khai báo DefaultStreamingDir)" : relativePath;
            if (status == SandboxServiceDataStatus.MissingData)
                _missingServiceSummaries.Add($"{serviceName}<{source}>");
            else if (status == SandboxServiceDataStatus.Unavailable || status == SandboxServiceDataStatus.Error)
                _unavailableServiceSummaries.Add($"{serviceName}<{source}>: {message}");
        }

        private void LogOptionalServiceSummary()
        {
            if (_missingServiceSummaries.Count > 0)
            {
                SubsystemLog.Warn("Sandbox",
                    $"Thiếu data port cho {_missingServiceSummaries.Count} service auto-load; không auto-wire: " +
                    string.Join(", ", _missingServiceSummaries));
            }

            if (_unavailableServiceSummaries.Count > 0)
            {
                SubsystemLog.Warn("Sandbox",
                    $"{_unavailableServiceSummaries.Count} service auto-load chưa sẵn sàng: " +
                    string.Join(", ", _unavailableServiceSummaries));
            }
        }


        public SkillEffectVisualService SkillEffectVisual { get; private set; }

        private void BootstrapCombatRuntime()
        {
            string modSkillsPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/ModSkills.txt");
            if (System.IO.File.Exists(modSkillsPath))
            {
                CombatSkillCatalog = PcCombatCatalogFactory.CreateNoviceCoreSectAndModCatalog(modSkillsPath, AssetRegistry);
            }
            else
            {
                CombatSkillCatalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(AssetRegistry);
            }
            CombatRuntime = new CombatRuntimeService(CombatSkillCatalog);
            PlayerProgression ??= new PlayerProgressionState();
            // First-boot: default faction = Cái Bang (the PC sandbox convention).
            // Without this the player joins with faction=None, knownSkills empty,
            // and the skill panel shows 0 skills + slots are blank.
            if (PlayerProgression.faction == CombatFaction.None)
                PlayerProgression.GrantFactionSkillPanelProgression(CombatSkillCatalog, CombatFaction.CaiBang);
            SkillEffectVisual = new SkillEffectVisualService(new SprRuntimeService(), CombatSkillCatalog);
            // Wire skill cast sound → AudioService (PC missles.txt SoundPath)
            SkillEffectVisual.OnCastSound = (pcPath) => AudioService?.PlaySkillCast(pcPath);

            // Gameplay Loop: wire all subsystems together
            GameplayLoop = new GameplayLoopService(CombatSkillCatalog);
            var gp = GameplayLoop.RegisterPlayer(PlayerActorId, "Cái Bang Đệ Tử", PlayerProgression.level, Vector2.zero);
            gp.combat.knownSkills = PlayerProgression.knownSkills;
            gp.combat.skillLevels = PlayerProgression.skillLevels;

            // Auto-set all CaiBang skills to max level for testing.
            // Matches PC GM command behavior. Runs on every boot / domain reload.
            PlayerProgression.MaxAllSkillLevels(CombatSkillCatalog);
            gp.combat.knownSkills = PlayerProgression.knownSkills;
            gp.combat.skillLevels = PlayerProgression.skillLevels;

            // Auto-grant horse at level 30+ per PC horseres.txt progression.
            // Sandbox default: player joins at level 30 (CaiBang quest complete),
            // so unlock basic horse (id=1 = blue). Override here for sandbox demos.
            PlayerProgression.horseId = PlayerProgressionState.HorseIdForLevel(PlayerProgression.level);
            if (PlayerProgression.horseId <= 0) PlayerProgression.horseId = 1; // sandbox: always at least blue

            // Wire gameplay events to logs
            GameplayLoop.OnDeath += e =>
            {
                if (e.isPlayer)
                {
                    var mapId = MapManager?.ActiveMapId ?? defaultMapId;
                    var revivePos = RevivePosService?.GetDefaultRevivePosition(mapId);
                    string reviveMsg = revivePos != null ? $" Revive Map={revivePos.MapId} Pos={revivePos.PosX + ", " + revivePos.PosY}." : "";
                    SubsystemLog.Info("Gameplay", $"Player chết! Respawn sau 5s.{reviveMsg}");
                }
                else
                    SubsystemLog.Info("Gameplay", $"{e.victimNameVi} bị giết. +{e.expReward}EXP +{e.silverReward}Bạc");
            };
            GameplayLoop.OnLevelUp += e =>
                SubsystemLog.Info("Gameplay", $"LEVEL UP! {e.oldLevel} → {e.newLevel}");
            // GameplayLoop.OnDamage += e =>
            //     SubsystemLog.Info("Gameplay", $"DMG: {e.attackerId}→{e.targetId} -{e.damage} ({e.type})");
        }

        public void GrantFactionSkillPanelProgression(CombatFaction targetFaction)
        {
            if (CombatSkillCatalog == null)
                BootstrapCombatRuntime();
            PlayerProgression ??= new PlayerProgressionState();
            PlayerProgression.GrantFactionSkillPanelProgression(CombatSkillCatalog, targetFaction);
        }

        public void GrantCaiBangSkillPanelProgression()
        {
            GrantFactionSkillPanelProgression(CombatFaction.CaiBang);
        }
        private void EnsureSandboxCamera()
        {
            if (FindSandboxCamera() != null)
                return;

            var cameraGo = new GameObject("Main Camera");
            if (cameraRoot != null)
                cameraGo.transform.SetParent(cameraRoot, false);
            cameraGo.tag = "MainCamera";
            var cam = cameraGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 240f;
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.transform.position = new Vector3(0f, 0f, -100f);
            cam.transform.rotation = Quaternion.identity;
        }

        private void EnsureEnemyRuntime()
        {
            if (EnemyRuntime != null || worldRoot == null)
                return;
            var enemyGo = new GameObject("MapEnemyRuntime");
            enemyGo.transform.SetParent(worldRoot, false);
            EnemyRuntime = enemyGo.AddComponent<MapEnemySpawnRuntime>();
            EnemyNameplateOverlay = enemyGo.AddComponent<BaLangEnemyNameplateOverlay>();
            TrainingSpawner = enemyGo.AddComponent<TrainingNpcSpawner>();
        }

        private void EnsureObjectRuntime()
        {
            if (ObjectRuntime != null || worldRoot == null)
                return;
            var objectGo = new GameObject("MapInteractiveObjectRuntime");
            objectGo.transform.SetParent(worldRoot, false);
            ObjectRuntime = objectGo.AddComponent<MapInteractiveObjectRuntime>();
        }

        private void EnsureTrapRuntime()
        {
            if (TrapRuntime != null || worldRoot == null)
                return;
            var trapGo = new GameObject("MapTrapRuntime");
            trapGo.transform.SetParent(worldRoot, false);
            TrapRuntime = trapGo.AddComponent<MapTrapRuntime>();
        }

        private void SpawnEnemiesForActiveMap()
        {
            if (EnemyRuntime == null || MapManager?.ActiveMap == null)
                return;
            // Region_S folder contains server-side NPC spawn data with real PC coordinates.
            var regionSFolder = ResolveRegionSFolderForActiveMap();
            EnemyRuntime.SpawnForMap(MapManager.ActiveMapId, regionSFolder);

            // Bridge spawned enemies into GameplayLoop so combat/AI can interact with them.
            if (GameplayLoop != null)
            {
                int bridged = 0;
                foreach (var entry in EnemyRuntime.Entries)
                {
                    int templateId = entry.template?.templateId ?? 0;
                    string nameVi   = entry.template?.DisplayName ?? "Quái";
                    int level       = Mathf.Max(1, entry.level);
                    var pos         = entry.worldPosition;
                    // Use instanceId as actorId; offset by 10000 to avoid collision with player (id=1).
                    int actorId     = 10000 + entry.instanceId;
                    GameplayLoop.RegisterEnemy(actorId, nameVi, templateId, level, pos);
                    bridged++;
                }
                SubsystemLog.Info("MapEnemy", $"GameplayLoop: bridged {bridged} enemies từ EnemyRuntime.");
            }
        }

        private void RenderObjectsForActiveMap()
        {
            if (ObjectRuntime == null || MapManager?.ActiveMap == null)
                return;
            ObjectRuntime.RenderForMap(MapManager.ActiveMap);
        }

        private void BuildTrapsForActiveMap()
        {
            if (TrapRuntime == null || MapManager?.ActiveMap == null)
                return;
            TrapRuntime.BuildForMap(MapManager.ActiveMap);
        }

        private string ResolveRegionSFolderForActiveMap()
        {
            var mapId = MapManager.ActiveMapId;
            var entry = MapManager.ActiveMap?.catalogEntry;
            if (!string.IsNullOrEmpty(entry?.serverRegionFolder))
            {
                var generated = ResolveStreamingAssetsPath(entry.serverRegionFolder);
                if (HasRegionSFiles(generated))
                {
                    SubsystemLog.Info("MapEnemy", $"Map {mapId}: using generated PC server Region_S: {entry.serverRegionFolder}");
                    return generated;
                }

                SubsystemLog.Warn("MapEnemy", $"Map {mapId}: generated server Region_S missing on disk, falling back to legacy TestData");
            }

            var legacy = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", $"Map_{mapId}");
            if (HasRegionSFiles(legacy))
                SubsystemLog.Info("MapEnemy", $"Map {mapId}: using legacy TestData Region_S");
            else
                SubsystemLog.Info("MapEnemy", $"Map {mapId}: no static PC Region_S files found");
            return legacy;
        }

        private static string ResolveStreamingAssetsPath(string path)
        {
            return Path.IsPathRooted(path) ? path : Path.Combine(Application.streamingAssetsPath, path);
        }

        private static bool HasRegionSFiles(string folder)
        {
            try
            {
                return !string.IsNullOrEmpty(folder) &&
                       Directory.Exists(folder) &&
                       Directory.GetFiles(folder, "*_Region_S.dat").Length > 0;
            }
            catch
            {
                return false;
            }
        }

        private void SpawnTrainingNpcs()
        {
            if (TrainingSpawner == null) return;
            TrainingSpawner.Spawn();
        }

        private void EnsurePlayerController()
        {
            if (PlayerController != null)
                return;
            if (worldRoot == null)
                return;

            var playerGo = new GameObject("MalePlayer");
            playerGo.transform.SetParent(worldRoot, false);
            PlayerController = playerGo.AddComponent<SandboxPlayerController>();
            PlayerController.followCamera = FindSandboxCamera();

            // Equip Cái Bang Bổng Pháp staff weapon on boot for testing.
            // PC: Cái Bang Bổng Pháp requires 长棍类 weapon to cast staff skills.
            // 长棍类1 → PcWeaponType.LongWeapon → MA_RW_010_* SPRs.
            //
            // FastEditor guard: skip weapon equip on FastEditor boot to avoid loading 8×N
            // MA_RW_010 SPRs (which can hang Unity for 20+ minutes when the SPR decoder
            // stalls on shadow/aux frames). Re-enable manually by un-ticking FastEditor.
            if (ActiveBootProfile != SandboxBootProfile.FastEditor)
            {
                PlayerController.EquipWeapon(PcWeaponType.LongWeapon);
            }
            else
            {
                SubsystemLog.Info("SandboxBoot", "FastEditor: skipped player weapon equip (MA_RW_010_* SPRs).");
            }

            PlayerVisual = PlayerController.visual as MalePlayerVisual;
            PlayerJoystick = EnsureMobileJoystick();
            PlayerController.BindJoystick(PlayerJoystick);

            // Add position debug for testing spawn coordinates
            playerGo.AddComponent<PlayerPositionDebug>();

            SubsystemLog.Info("Sandbox", "Male player controller ready (8-way SPR parts + joystick)");
        }

        /// <summary>
        /// Spawn a female player visual at the same spawn point as the male one
        /// (training pentagon center). Used for ST-02.1 female visual parity tests.
        /// Idempotent: if a FemalePlayerVisual already exists, the existing one is kept.
        /// </summary>
        public FemalePlayerVisual SpawnFemaleVisual()
        {
            if (FemalePlayerVisual != null) return FemalePlayerVisual;
            if (worldRoot == null)
            {
                SubsystemLog.Warn("Sandbox", "Cannot spawn female visual: worldRoot is null");
                return null;
            }

            Vector2 spawn = new Vector2(53246f, -52041f);
            var femaleGo = new GameObject("FemalePlayer");
            femaleGo.transform.SetParent(worldRoot, false);
            femaleGo.transform.position = new Vector3(spawn.x + 40f, spawn.y, 0f);
            FemalePlayerVisual = femaleGo.AddComponent<FemalePlayerVisual>();
            FemalePlayerVisual.SetWeapon(PcWeaponType.EmptyHand);
            SubsystemLog.Info("Sandbox", $"Female player visual spawned at {spawn} (offset 40 units east of male)");
            return FemalePlayerVisual;
        }

        /// <summary>
        /// Immediately place player at the fixed training pentagon center (53493, 95313).
        /// PC source: Vo Su (tid=311) position from Region_S 104_093.
        /// Called before map load so player never starts at (0,0).
        /// </summary>
        private void PlacePlayerAtDefaultSpawn()
        {
            if (PlayerController == null) return;
            // Map-specific spawn point
            int mapId = defaultMapId;
            Vector2 spawn = MapEnemyDatabase.GetDefaultSpawnPoint(mapId);
            PlayerController.ResetMovementState();
            PlayerController.PlaceAt(spawn, snapCamera: false);
            SubsystemLog.Info("Sandbox", $"Player pre-placed at {spawn} for map {mapId}");
        }

        public int GetFightState() => CurrentFightState;

        public void SetFightState(int fightState)
        {
            CurrentFightState = Mathf.Clamp(fightState, 0, 1);
            SubsystemLog.Info("Sandbox", $"PC SetFightState({CurrentFightState}) applied");
        }

        public int GetCurCamp() => CurrentCamp;

        public int GetCamp() => OriginalCamp;

        public void SetCurCamp(int camp)
        {
            CurrentCamp = Mathf.Max(0, camp);
            SubsystemLog.Info("Sandbox", $"PC SetCurCamp({CurrentCamp}) applied");
        }

        public void SetOriginalCamp(int camp)
        {
            OriginalCamp = Mathf.Max(0, camp);
            SubsystemLog.Info("Sandbox", $"PC GetCamp source set to {OriginalCamp}");
        }

        public void SetLogoutRv(int value)
        {
            CurrentLogoutRv = value;
            SubsystemLog.Info("Sandbox", $"PC SetLogoutRV({CurrentLogoutRv}) applied");
        }

        public void SetPkFlag(int value)
        {
            CurrentPkFlag = Mathf.Max(0, value);
            SubsystemLog.Info("Sandbox", $"PC SetPKFlag({CurrentPkFlag}) applied");
        }

        public void ForbidChangePk(int value)
        {
            CurrentForbidChangePk = Mathf.Max(0, value);
            SubsystemLog.Info("Sandbox", $"PC ForbidChangePK({CurrentForbidChangePk}) applied");
        }

        public void SetPunish(int value)
        {
            CurrentPunish = Mathf.Max(0, value);
            SubsystemLog.Info("Sandbox", $"PC SetPunish({CurrentPunish}) applied");
        }

        public void SetCreateTeam(int value)
        {
            CurrentCreateTeam = Mathf.Max(0, value);
            SubsystemLog.Info("Sandbox", $"PC SetCreateTeam({CurrentCreateTeam}) applied");
        }

        public void SetTaskTemp(int taskId, int value)
        {
            if (taskId > 0)
            {
                if (value == 0) _taskTempValues.Remove(taskId);
                else _taskTempValues[taskId] = value;
            }
            SubsystemLog.Info("Sandbox", $"PC SetTaskTemp({taskId},{value}) applied");
        }

        public int GetTaskTemp(int taskId)
            => taskId > 0 && _taskTempValues.TryGetValue(taskId, out var value) ? value : 0;

        public int GetPcMissionValue(int missionVarId)
            => missionVarId > 0 && _pcMissionValues.TryGetValue(missionVarId, out var value) ? value : 0;

        public void SetPcMissionValue(int missionVarId, int value)
        {
            if (missionVarId > 0)
            {
                if (value == 0) _pcMissionValues.Remove(missionVarId);
                else _pcMissionValues[missionVarId] = value;
            }
            SubsystemLog.Info("Sandbox", $"PC SetMissionV({missionVarId},{value}) source recorded");
        }

        public int GetPcMissionPlayerGroup(int missionId)
            => missionId > 0 && _pcMissionPlayerGroups.TryGetValue(missionId, out var group) ? group : 0;

        public void SetPcMissionPlayerGroup(int missionId, int group)
        {
            if (missionId > 0)
            {
                if (group == 0) _pcMissionPlayerGroups.Remove(missionId);
                else _pcMissionPlayerGroups[missionId] = group;
            }
            SubsystemLog.Info("Sandbox", $"PC mission {missionId} player group recorded as {group}");
        }

        public bool HasPcSummonedPartner()
            => _pcHasSummonedPartner;

        public void SetPcSummonedPartner(bool hasSummonedPartner)
        {
            _pcHasSummonedPartner = hasSummonedPartner;
            SubsystemLog.Info("Sandbox", $"PC PARTNER_GetCurPartner summoned state recorded as {hasSummonedPartner}");
        }

        public int GetPcPartnerMasterTaskState(int masterTaskId)
            => masterTaskId > 0 && _pcPartnerMasterTaskStates.TryGetValue(masterTaskId, out var value) ? value : 0;

        public void SetPcPartnerMasterTaskState(int masterTaskId, int value)
        {
            if (masterTaskId > 0)
            {
                if (value == 0) _pcPartnerMasterTaskStates.Remove(masterTaskId);
                else _pcPartnerMasterTaskStates[masterTaskId] = value;
            }
            SubsystemLog.Info("Sandbox", $"PC PARTNER_SetTaskValue({masterTaskId},{value}) source recorded");
        }

        public void SetDeathScript(string scriptPath)
        {
            CurrentDeathScript = scriptPath ?? string.Empty;
            SubsystemLog.Info("Sandbox", $"PC SetDeathScript({CurrentDeathScript}) applied");
        }

        public void LeaveTeamForPcTrap()
        {
            SubsystemLog.Info("Sandbox", "PC LeaveTeam() recorded for trap action");
        }

        public void SetRevPos(int mapId, int reviveId)
        {
            CurrentReviveMapId = mapId;
            CurrentReviveId = reviveId;
            SubsystemLog.Info("Sandbox", $"PC SetRevPos({mapId},{reviveId}) applied");
        }

        private void ApplyActiveMapBoundsToPlayer()
        {
            var bounds = MapManager?.ActiveMap?.sourceBoundsRect;
            if (PlayerController == null || bounds == null)
                return;

            PlayerController.SetMapBounds(bounds);
            SubsystemLog.Info("Sandbox",
                $"Player map bounds set to x={bounds.x}..{bounds.x + bounds.width}, y={bounds.y}..{bounds.y + bounds.height}");
        }

        private void PlacePlayerOnActiveMap()
        {
            if (PlayerController == null)
                return;

            ApplyActiveMapBoundsToPlayer();

            // Map-specific spawn point
            int mapId = MapManager?.ActiveMapId ?? defaultMapId;
            Vector2 spawn = MapEnemyDatabase.GetDefaultSpawnPoint(mapId);

            PlayerController.ResetMovementState();
            PlayerController.PlaceAt(spawn, snapCamera: false);
            SubsystemLog.Info("Sandbox", $"Player spawn set to {spawn} on map {mapId}");

            var mapTab = FindObjectOfType<GMMapTab>(true);
            if (mapTab != null)
                mapTab.markerWorldOverride = spawn;
        }

        private Vector2? FindBaLangTrainerSpawn()
        {
            var folder = System.IO.Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", "Map_79");
            var spawns = BaLangEnemyRegionScanner.ScanRegionS(folder);
            foreach (var sp in spawns)
            {
                if (sp.templateId == 311 && sp.nameRaw == "武师")
                    return BaLangEnemyDatabase.MpsToWorld(sp.mpsX, sp.mpsY);
            }
            return null;
        }

        private MobileJoystick EnsureMobileJoystick()
        {
            if (uiRoot == null)
                return null;

            var existing = uiRoot.GetComponentInChildren<MobileJoystick>(true);
            if (existing != null)
            {
                // MobileJoystick already in scene (e.g. from Sandbox.unity) — still ensure the
                // mount toggle button is wired so testers can mount/dismount from the HUD.
                // EnsureMountToggleButton(existing.transform.parent as RectTransform);
                return existing;
            }

            if (FindObjectOfType<EventSystem>() == null)
            {
                var eventSystemGo = new GameObject("EventSystem");
                if (uiRoot != null)
                    eventSystemGo.transform.SetParent(uiRoot, false);
                eventSystemGo.AddComponent<EventSystem>();
                eventSystemGo.AddComponent<StandaloneInputModule>();
            }

            var canvasGo = new GameObject("MobileControlsCanvas");
            canvasGo.transform.SetParent(uiRoot, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Joystick phải nằm trên cùng: HUD UI Toolkit (PanelSettings sortingOrder=0),
            // PanelCanvas (sortingOrder=200) và mọi PcBottomBarBg/CombatActionCluster đè vùng
            // trái-dưới đều phải ở dưới joystick. 500 chừa headroom cho tooltip/dialogue popup
            // tương lai (sortingOrder 600..700) mà vẫn không che joystick.
            canvas.sortingOrder = 500;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            canvasGo.AddComponent<GraphicRaycaster>();

            var backgroundGo = new GameObject("MoveJoystick");
            backgroundGo.transform.SetParent(canvasGo.transform, false);
            var background = backgroundGo.AddComponent<RectTransform>();
            background.anchorMin = new Vector2(0f, 0f);
            background.anchorMax = new Vector2(0f, 0f);
            background.pivot = new Vector2(0.5f, 0.5f);
            background.anchoredPosition = new Vector2(120f, 120f);
            // Vòng đế joystick — sprite ngọc-bích thật (fallback procedural khi thiếu art).
            background.sizeDelta = new Vector2(220f, 220f);
            var bgImage = backgroundGo.AddComponent<Image>();
            var joystickBase = LoadJoystickArt("UI/VirtualJoystick/joystick_base");
            bgImage.sprite = joystickBase != null
                ? joystickBase
                : CreateUiDiscSprite(new Color(0.15f, 0.85f, 0.25f, 0.28f), new Color(0.70f, 1f, 0.70f, 0.65f));
            bgImage.preserveAspect = true;

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(backgroundGo.transform, false);
            var handle = handleGo.AddComponent<RectTransform>();
            handle.anchorMin = new Vector2(0.5f, 0.5f);
            handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.anchoredPosition = Vector2.zero;
            // Núm joystick — sprite ngọc-bích thật (fallback procedural khi thiếu art).
            handle.sizeDelta = new Vector2(110f, 110f);
            var handleImage = handleGo.AddComponent<Image>();
            var joystickHandle = LoadJoystickArt("UI/VirtualJoystick/joystick_handle");
            handleImage.sprite = joystickHandle != null
                ? joystickHandle
                : CreateUiDiscSprite(new Color(0.12f, 0.95f, 0.30f, 0.78f), new Color(0.85f, 1f, 0.85f, 0.95f));
            handleImage.preserveAspect = true;

            var joystick = backgroundGo.AddComponent<MobileJoystick>();
            joystick.background = background;
            joystick.handle = handle;
            // radius (bán kính di chuyển visible của handle) & inputRadius khớp vòng đế 220px.
            joystick.radius = 92f;
            joystick.inputRadius = 86f;
            joystick.deadZone = 0.08f;
            joystick.sensitivity = 1.35f;
            // EnsureMountToggleButton(canvasGo.GetComponent<RectTransform>());
            return joystick;
        }

        private void EnsureMountToggleButton(RectTransform canvasTransform)
        {
            if (canvasTransform == null || canvasTransform.Find("MountBtn") != null)
                return;

            // Nút lên/xuống ngựa — góc phải, ngang tầm giữa (khớp horse_btn spec 0.90,0.55).
            var btnGo = new GameObject("MountBtn");
            btnGo.transform.SetParent(canvasTransform, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-32f, 200f);
            rt.sizeDelta = new Vector2(150f, 60f);
            var img = btnGo.AddComponent<Image>();
            img.color = new Color(0.45f, 0.30f, 0.12f, 0.9f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var txt = lblGo.AddComponent<Text>();
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);

            var toggle = btnGo.AddComponent<MountToggleButton>();
            toggle.Bind(PlayerController, txt);
            btn.onClick.AddListener(toggle.OnClick);
        }

        // Cache sprite art joystick đã load từ Resources (load 1 lần).
        private static Sprite _joystickBaseArt;
        private static Sprite _joystickHandleArt;

        /// <summary>
        /// Load sprite joystick từ <c>Resources/UI/VirtualJoystick</c>. Trả về <c>null</c>
        /// khi thiếu art (caller sẽ fallback sang procedural disc). Cache tĩnh để
        /// không load lại nhiều lần khi build lại HUD.
        /// </summary>
        private static Sprite LoadJoystickArt(string resourcesPath)
        {
            if (string.IsNullOrEmpty(resourcesPath))
                return null;

            if (resourcesPath.EndsWith("base", System.StringComparison.Ordinal))
            {
                if (_joystickBaseArt == null)
                    _joystickBaseArt = Resources.Load<Sprite>(resourcesPath);
                return _joystickBaseArt;
            }

            if (resourcesPath.EndsWith("handle", System.StringComparison.Ordinal))
            {
                if (_joystickHandleArt == null)
                    _joystickHandleArt = Resources.Load<Sprite>(resourcesPath);
                return _joystickHandleArt;
            }

            return Resources.Load<Sprite>(resourcesPath);
        }

        private static Sprite CreateUiDiscSprite(Color fill, Color ring)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = (size - 1) * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / radius;
                    Color c = d <= 1f ? (d > 0.82f ? ring : fill) : Color.clear;
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect);
        }

        /// <summary>
        /// Build mobile UI panels (Quest, Inventory, Map Select) if not already present.
        /// Panels are children of the UI root canvas.
        /// </summary>
        private void EnsureMobileUiPanels()
        {
            if (uiRoot == null) return;

            try
            {
                EnsureMobileUiPanelsInternal();
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn("Sandbox", $"UI panel creation failed (non-critical): {ex.Message}");
            }
        }

        private void EnsureMobileUiPanelsInternal()
        {
            if (uiRoot == null) return;

            // Find or create the overlay canvas for panels
            var panelCanvas = uiRoot.Find("PanelCanvas");
            Canvas canvas;
            if (panelCanvas != null)
            {
                canvas = panelCanvas.GetComponent<Canvas>();
            }
            else
            {
                var canvasGo = new GameObject("PanelCanvas");
                canvasGo.transform.SetParent(uiRoot, false);
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 200;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280f, 720f);
                canvasGo.AddComponent<GraphicRaycaster>();
                panelCanvas = canvasGo.transform;
            }

            // Quest Tracker Panel
            if (QuestPanel == null)
            {
                var qpGo = new GameObject("QuestTrackerPanel");
                qpGo.transform.SetParent(panelCanvas, false);
                QuestPanel = qpGo.AddComponent<QuestTrackerPanel>();
                QuestPanel.Initialize(QuestService);
            }

            // Inventory Panel
            if (InventoryPanel == null)
            {
                var ipGo = new GameObject("InventoryPanel");
                ipGo.transform.SetParent(panelCanvas, false);
                InventoryPanel = ipGo.AddComponent<InventoryPanel>();
                InventoryPanel.Initialize(ItemDb, _inventoryService);
            }

            // Map Select Panel
            if (MapSelectPanel == null)
            {
                var mpGo = new GameObject("MapSelectPanel");
                mpGo.transform.SetParent(panelCanvas, false);
                MapSelectPanel = mpGo.AddComponent<MapSelectPanel>();
                MapSelectPanel.Initialize(MapManager, SwitchMap);
            }

            // Chat Panel
            if (ChatPanel == null && ChatService != null)
            {
                var cpGo = new GameObject("ChatPanel");
                cpGo.transform.SetParent(panelCanvas, false);
                ChatPanel = cpGo.AddComponent<ChatPanel>();
                ChatPanel.Initialize(ChatService);
            }

            // Party Panel
            if (PartyPanel == null && PartyService != null)
            {
                var ppGo = new GameObject("PartyPanel");
                ppGo.transform.SetParent(panelCanvas, false);
                PartyPanel = ppGo.AddComponent<PartyPanel>();
                PartyPanel.Initialize(PartyService);
            }

            // Faction Panel
            if (FactionPanel == null)
            {
                var fpGo = new GameObject("FactionPanel");
                fpGo.transform.SetParent(panelCanvas, false);
                FactionPanel = fpGo.AddComponent<FactionPanel>();
                FactionPanel.Initialize();
            }

            // Shop Panel
            if (ShopPanel == null && ShopService != null)
            {
                var spGo = new GameObject("ShopPanel");
                spGo.transform.SetParent(panelCanvas, false);
                ShopPanel = spGo.AddComponent<ShopPanel>();
                ShopPanel.Initialize(ShopService, _inventoryService, ItemDb);
            }

            // Minimap Panel (separate canvas since it uses a different layout)
            // DISABLED: uGUI MinimapPanel conflicts with the authentic PC UI Toolkit Minimap.
            /*
            if (MinimapPanel == null && uiRoot != null)
            {
                var minimapCanvas = new GameObject("MinimapCanvas");
                minimapCanvas.transform.SetParent(uiRoot, false);
                var mc = minimapCanvas.AddComponent<Canvas>();
                mc.renderMode = RenderMode.ScreenSpaceOverlay;
                mc.sortingOrder = 150; // below panels, above joystick
                var msc = minimapCanvas.AddComponent<CanvasScaler>();
                msc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                msc.referenceResolution = new Vector2(1280f, 720f);
                minimapCanvas.AddComponent<GraphicRaycaster>();
                MinimapPanel = minimapCanvas.AddComponent<MinimapPanel>();
                if (PlayerController != null)
                    MinimapPanel.Initialize(MapManager, PlayerController, EnemyRuntime);
            }
            */

            GmPanel = FindObjectOfType<GMPanelController>(true);

            // Wire scene GMButton to GMPanelController.Toggle() (button is placed in scene at top-right corner)
            if (GmPanel != null)
            {
                GmPanel.gameObject.SetActive(true);
                var gmButtonGO = GameObject.Find("GMButton");
                if (gmButtonGO != null)
                {
                    var gmBtn = gmButtonGO.GetComponent<UnityEngine.UI.Button>();
                    if (gmBtn != null && gmBtn.onClick.GetPersistentEventCount() == 0)
                    {
                        gmBtn.onClick.AddListener(GmPanel.Toggle);
                    }
                }
            }

            // Add HUD buttons for panels (on the joystick canvas)
            // EnsureHudButtons();
        }

        /// <summary>
        /// Switch to a different map. Unloads current, loads new map, re-spawns enemies.
        /// </summary>
        public void SwitchMap(int mapId)
        {
            if (MapManager == null) return;

            SubsystemLog.Info("Sandbox", $"Switching to map {mapId}...");

            // Update manifest status
            if (MapPortManifest.TryGet(mapId, out var entry))
            {
                SubsystemLog.Info("Sandbox", $"Loading: {entry.nameVi} (PC: {entry.pcNameHint})");
            }

            // If map not in catalog, add placeholder
            if (!MapManager.Catalog.ContainsKey(mapId))
            {
                SubsystemLog.Info("Sandbox", $"Map {mapId} not in catalog — using placeholder");
            }

            defaultMapId = mapId;
            MapManager.LoadMap(mapId);

            // Play BGM for map
            string bgmId = mapId switch
            {
                MapPortManifest.BaLangHuyenId => "bgm_balang",
                MapPortManifest.GiangTanThonId => "bgm_giangtan",
                MapPortManifest.TuongDuongId => "bgm_tuongduong",
                MapPortManifest.ThanhDoId => "bgm_thanhdo",
                MapPortManifest.DaiLyId => "bgm_daily",
                MapPortManifest.BienKinhId => "bgm_bienkinh",
                MapPortManifest.TinSuVuotAiPhongKy120Id => "bgm_balang",
                MapPortManifest.VuotAiNhiepThiTranId => "bgm_balang",
                _ => "bgm_balang",
            };
            AudioService?.PlayBGM(bgmId);
        }

        /// <summary>
        /// Add HUD buttons for Quest, Inventory, and Map Select panels.
        /// Placed on the right side of the screen above the mount button.
        /// </summary>
        private void EnsureHudButtons()
        {
            var joystickCanvas = uiRoot?.GetComponentInChildren<Canvas>();
            if (joystickCanvas == null) return;
            var canvasTransform = joystickCanvas.GetComponent<RectTransform>();

            // Column of buttons on right side
            // Quest button
            EnsurePanelButton(canvasTransform, "QuestBtn", "Nhiệm Vụ",
                new Vector2(-32f, 620f), new Color(0.6f, 0.4f, 0.1f, 0.85f),
                () => QuestPanel?.Toggle());

            // Inventory button
            EnsurePanelButton(canvasTransform, "InventoryBtn", "Túi Đồ",
                new Vector2(-32f, 540f), new Color(0.1f, 0.4f, 0.6f, 0.85f),
                () => InventoryPanel?.Toggle());

            // Map select button
            EnsurePanelButton(canvasTransform, "MapSelectBtn", "Bản Đồ",
                new Vector2(-32f, 460f), new Color(0.4f, 0.1f, 0.6f, 0.85f),
                () => MapSelectPanel?.Toggle());

            // Chat toggle button
            EnsurePanelButton(canvasTransform, "ChatBtn", "Chat",
                new Vector2(-32f, 380f), new Color(0.2f, 0.4f, 0.3f, 0.85f),
                () => ChatPanel?.Toggle());

            // Party button
            EnsurePanelButton(canvasTransform, "PartyBtn", "Đội",
                new Vector2(-32f, 300f), new Color(0.2f, 0.3f, 0.5f, 0.85f),
                () => PartyPanel?.Toggle());

            // Faction button
            EnsurePanelButton(canvasTransform, "FactionBtn", "Môn Phái",
                new Vector2(-175f, 620f), new Color(0.5f, 0.3f, 0.1f, 0.85f),
                () => FactionPanel?.Toggle());

            // Shop button
            EnsurePanelButton(canvasTransform, "ShopBtn", "Cửa Hàng",
                new Vector2(-175f, 540f), new Color(0.1f, 0.3f, 0.1f, 0.85f),
                () => ShopPanel?.Toggle());

            // GM button is in scene (GMButton at top-right corner), wired via FindObjectOfType in EnsureMobileUiPanels.
            // No need to add a duplicate GmBtn here.
        }

        private void EnsurePanelButton(RectTransform parent, string name, string label,
            Vector2 position, Color color, Action onClick)
        {
            if (parent.Find(name) != null) return;

            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = position;
            rt.sizeDelta = new Vector2(150f, 60f);
            var img = btnGo.AddComponent<Image>();
            img.color = color;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(btnGo.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var txt = lblGo.AddComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.fontSize = 24;
            txt.fontStyle = FontStyle.Bold;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
        }

        /// <summary>
        /// Configure the sandbox camera for PC-style isometric depth sorting. Player
        /// follow owns the camera position/zoom after spawn so movement and minimap
        /// navigation stay visible.
        /// </summary>
        public void ConfigureCameraForMap()
        {
            if (MapRenderer == null || !MapRenderer.HasContent) return;

            var cam = FindSandboxCamera();
            if (cam == null)
            {
                SubsystemLog.Warn("Sandbox", "No camera found to configure map");
                return;
            }

            cam.orthographic = true;
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 5000f);
            SubsystemLog.Info("Sandbox", "Camera configured for player-follow map view");
        }

        public void FrameCameraOnMap()
        {
            ConfigureCameraForMap();
            PlayerController?.SnapCamera();
        }

        private Camera FindSandboxCamera()
        {
            if (cameraRoot != null)
            {
                var c = cameraRoot.GetComponentInChildren<Camera>(true);
                if (c != null) return c;
            }
            return Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
        }

        private void InitSubsystem(SubsystemKind kind, string name, ref Transform root)
        {
            try
            {
                if (root == null)
                {
                    var go = new GameObject($"_{name}");
                    go.transform.SetParent(transform, false);
                    root = go.transform;
                }

                SubsystemLog.Info("Sandbox", $"{name} root ready");
                BootReport.Record(kind, true, $"{name} initialized");
            }
            catch (Exception ex)
            {
                SubsystemLog.Error("Sandbox", $"{name} failed: {ex.Message}");
                BootReport.Record(kind, false, $"{name} failed: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            SkillEffectVisual?.Update(Time.deltaTime);

            // PC uses SubWorld.m_dwCurrentTime incremented every game tick (~18fps).
            // Accumulate fractional ticks; at 60fps delta*18 < 1, direct int cast would stay 0 forever.
            if (CombatRuntime != null)
            {
                _combatTickAccumulator += Time.deltaTime * 18f;
                int ticks = Mathf.FloorToInt(_combatTickAccumulator);
                if (ticks > 0)
                {
                    CombatRuntime.AdvanceTime(ticks);
                    _combatTickAccumulator -= ticks;
                }
            }

            // Gameplay loop tick: mana regen, enemy AI, respawn timers
            // Sync player combat position from scene controller so range checks work correctly.
            if (GameplayLoop?.Player != null && PlayerController != null)
            {
                var wpos = (Vector2)PlayerController.transform.position;
                GameplayLoop.Player.worldPos = wpos;
                GameplayLoop.Player.combat.position = wpos;
            }

            // Sync live enemy scene positions → GameplayLoop so AI distance checks work.
            // Uses Entries (no alloc) with cached enemyBehaviour reference.
            if (GameplayLoop != null && EnemyRuntime != null)
            {
                foreach (var entry in EnemyRuntime.Entries)
                {
                    if (entry.enemyBehaviour == null) continue;
                    var glActor = GameplayLoop.GetActor(10000 + entry.instanceId);
                    if (glActor == null || glActor.isDead) continue;
                    var scenePos = (Vector2)entry.enemyBehaviour.transform.position;
                    glActor.worldPos = scenePos;
                    glActor.combat.position = scenePos;
                }
            }

            GameplayLoop?.Tick(Time.deltaTime);
        }

        // ── IMapTeleportHost implementation ──────────────────────────────────

        public bool HasMap(int mapId)
        {
            // For now, sandbox only has the default map loaded.
            // Real implementation would check MapManager or scene loading.
            return mapId == defaultMapId;
        }

        public void SwitchMapAndPlacePlayer(int mapId, Vector2 worldPosition)
        {
            Debug.Log($"[SandboxManager] Teleport to map {mapId} at ({worldPosition.x:F1}, {worldPosition.y:F1})");
            if (HasMap(mapId) && PlayerController != null)
            {
                PlayerController.PlaceAt(worldPosition, snapCamera: false);
            }
        }
    }
}
