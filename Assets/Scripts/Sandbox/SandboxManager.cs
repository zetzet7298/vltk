using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
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

    public sealed class SandboxBootReport
    {
        public readonly List<(SubsystemKind kind, bool ok, string message)> Entries
            = new();

        public void Record(SubsystemKind kind, bool ok, string message)
        {
            Entries.Add((kind, ok, message));
        }
    }

    public enum SandboxServiceDataStatus
    {
        Unknown,
        Loaded,
        MissingData,
        Unavailable,
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
    }

    public class SandboxManager : MonoBehaviour
    {
        public const int BaLangHuyenMapId = 79;
        public const int PlayerActorId = 1;

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

        [Header("Boot")]
        public int defaultMapId = BaLangHuyenMapId;
        public bool loadDefaultMapOnBoot = true;

        public static SandboxManager Instance { get; private set; }
        public SandboxBootReport BootReport { get; private set; }
        public bool IsInitialized { get; private set; }
        public AssetRegistry AssetRegistry { get; private set; }
        public MapManager MapManager { get; private set; }
        public MapRenderer MapRenderer { get; private set; }
        public SandboxPlayerController PlayerController { get; private set; }
        public MalePlayerVisual PlayerVisual { get; private set; }
        public MobileJoystick PlayerJoystick { get; private set; }
        public MapEnemySpawnRuntime EnemyRuntime { get; private set; }
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
        private InventoryService _inventoryService;

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
        public ShopConfigService ShopConfigService { get; private set; }
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
        public RareSpawnService RareSpawnService { get; private set; }
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
        private float _combatTickAccumulator;
        // M1.2: Region catalog and report
        public RegionCatalogFile RegionCatalog { get; private set; }
        public RegionConversionReport RegionReport { get; private set; }

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
            InitializeSubsystems();
        }

        public void BootstrapCombatForTests(AssetRegistry registry = null)
        {
            AssetRegistry = registry ?? new AssetRegistry();
            BootstrapCombatRuntime();
        }

        private void InitializeSubsystems()
        {
            InitSubsystem(SubsystemKind.Game, "Game", ref gameRoot);
            InitSubsystem(SubsystemKind.Camera, "Camera", ref cameraRoot);
            InitSubsystem(SubsystemKind.UI, "UI", ref uiRoot);
            InitSubsystem(SubsystemKind.World, "World", ref worldRoot);
            InitSubsystem(SubsystemKind.Debug, "Debug", ref debugRoot);
            InitSubsystem(SubsystemKind.Services, "Services", ref servicesRoot);

            IsInitialized = true;

            EnsureSandboxCamera();

            // M0.6: create shared registry, pass to all systems that need resource lookup
            AssetRegistry = new AssetRegistry();
            BootstrapCombatRuntime();

            MapManager = new MapManager(AssetRegistry);
            MapManager.LoadCatalog();

            // M1.2: Load region catalog
            var regionCat = RegionCatalogLoader.LoadFromStreamingAssets();
            if (regionCat != null)
            {
                RegionCatalog = regionCat;
                RegionReport = RegionCatalogLoader.ToConversionReport(regionCat);
                SubsystemLog.Info("Sandbox", $"Regions: {regionCat.totalRegions} loaded");
            }

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
                        MapRenderer.LoadMapRegions(MapManager.ActiveMap);
                        EnsurePlayerController();
                        EnsureEnemyRuntime();
                        PlacePlayerOnActiveMap();
                        SpawnEnemiesForActiveMap();
                        SpawnTrainingNpcs();
                        ConfigureCameraForMap();
                        PlayerController?.SnapCamera();
                    }
                };
                MapManager.OnMapUnloaded += (mapId) => {
                    EnemyRuntime?.Clear();
                    MapRenderer.Clear();
                };

                EnsurePlayerController();

                // ── New Subsystems ──────────────────────────────────
                QuestService = new QuestService();
                // Load PC item data via batch loader (14 categories, ~10k+ items)
                var importer = PcItemBatchLoader.ImportInto(
                    System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcItem"));
                ItemDb = new ItemDatabase(importer);
                LootService = new LootDropService(ItemDb);
                var dropRegistry = new DropRateRegistry();
                dropRegistry.LoadDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcDropRate"));
                LootService.AttachRegistry(dropRegistry);
                PcSkillsFull = PcSkillRegistry.LoadFromDirectory(System.IO.Path.Combine(Application.streamingAssetsPath, "Reference/PcSkill"));
                AudioService = new AudioService();
                if (servicesRoot != null)
                    AudioService.Initialize(servicesRoot);

                // Wire quest events to combat loot
                if (GameplayLoop != null)
                    GameplayLoop.OnDeath += e =>
                    {
                        if (!e.isPlayer && e.victimTemplateId != null)
                            QuestService?.UpdateKillObjective(e.victimTemplateId.Value);
                    };

                // Initialize item inventory
                var itemImporter = new ItemContractImporter();
                _inventoryService = new InventoryService(itemImporter, null);

                // Initialize Chat system
                ChatService = new ChatService();
                ChatService.PostSystemMessage("Chào mừng đến Võ Lâm Truyền Kỳ Mobile!");

                // Initialize Party system
                PartyService = new PartyService();

                // Initialize Shop system
                ShopService = new ShopService(ItemDb, initialSilver: 5000);

                // ── PC-parity runtime services (meridian, partner, title, …) ────────
                _serviceLoadStatuses.Clear();
                _missingServiceSummaries.Clear();
                _unavailableServiceSummaries.Clear();

                MeridianService = MeridianService.LoadFromStreamingAssets();
                PartnerService = PartnerService.LoadFromStreamingAssets();
                PetSkillService = PetSkillService.LoadFromStreamingAssets();
                TitleService = TitleService.LoadFromStreamingAssets();
                LotteryService = LotteryService.LoadFromStreamingAssets();
                CompoundRecipeService = CompoundRecipeService.LoadFromStreamingAssets();
                QuestItemService = QuestItemService.LoadFromStreamingAssets();
                AdventureService = AdventureService.LoadFromStreamingAssets();
                GuildService = GuildService.LoadFromStreamingAssets();
                AttribConstService = AttribConstService.LoadFromStreamingAssets();
                MissleCatalogService = MissleCatalogService.LoadFromStreamingAssets();
                EventBonusService = EventBonusService.LoadFromStreamingAssets();
                CityWarService = CityWarService.LoadFromStreamingAssets();
                AuctionService = AuctionService.LoadFromStreamingAssets();
                GoodsCatalogService = GoodsCatalogService.LoadFromStreamingAssets();
                ShopConfigService = ShopConfigService.LoadFromStreamingAssets();

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

                // ── Batch 5: Final client systems (faction maps, awards, double exp, sim city, client skill scripts) ─
                FactionMapService = LoadOptionalStreamingService(nameof(FactionMapService), () => FactionMapService.LoadFromStreamingAssets());
                BattleAwardService = LoadOptionalStreamingService(nameof(BattleAwardService), () => BattleAwardService.LoadFromStreamingAssets());
                DoubleExpService = LoadOptionalStreamingService(nameof(DoubleExpService), () => DoubleExpService.LoadFromStreamingAssets());
                SimCityPluginService = LoadOptionalStreamingService(nameof(SimCityPluginService), () => SimCityPluginService.LoadFromStreamingAssets());
                ClientSkillScriptService = LoadOptionalStreamingService(nameof(ClientSkillScriptService), () => ClientSkillScriptService.LoadFromStreamingAssets());
                TongJinBattleService = LoadOptionalStreamingService(nameof(TongJinBattleService), () => TongJinBattleService.LoadFromStreamingAssets());
                BangChienService = LoadOptionalStreamingService(nameof(BangChienService), () => BangChienService.LoadFromStreamingAssets());
                BossHoangKimService = LoadOptionalStreamingService(nameof(BossHoangKimService), () => BossHoangKimService.LoadFromStreamingAssets());
                TaskFlagService = new TaskFlagService();
                // ── Batch 8: Save/Load + Mail + Mount + Ranking + Friend + Pet + Shop + Missile + HudArt + FactionRuntime + BattleScript + TaskFlagRegistry ───────────
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
                // ── Batch 9: Map data + Skill data + World boss + Achievement + Mall + Fashion + Sign-in + Treasure + Encounter + Friend gift + Text + Animation ───────────
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
                // ── Batch 10: Faction skill tree + bonus + relation + Guild scripts + Battle map config + reward config + honor + Sơ/Trung/Cao Jin ───────────
                FactionSkillTreeService = LoadOptionalStreamingService(nameof(FactionSkillTreeService), () => FactionSkillTreeService.LoadFromStreamingAssets());
                FactionBonusService = LoadOptionalStreamingService(nameof(FactionBonusService), () => FactionBonusService.LoadFromStreamingAssets());
                FactionRelationService = LoadOptionalStreamingService(nameof(FactionRelationService), () => FactionRelationService.LoadFromStreamingAssets());
                GuildScriptService = LoadOptionalStreamingService(nameof(GuildScriptService), () => GuildScriptService.LoadFromStreamingAssets());
                BattleMapConfigService = LoadOptionalStreamingService(nameof(BattleMapConfigService), () => BattleMapConfigService.LoadFromStreamingAssets());
                BattleRewardConfigService = LoadOptionalStreamingService(nameof(BattleRewardConfigService), () => BattleRewardConfigService.LoadFromStreamingAssets());
                BattleHonorService = LoadOptionalStreamingService(nameof(BattleHonorService), () => BattleHonorService.LoadFromStreamingAssets());
                SjBattleService = LoadOptionalStreamingService(nameof(SjBattleService), () => SjBattleService.LoadFromStreamingAssets());
                // ── Batch 11: Hoa Sơn + Sprite asset + Sound effect + Map connection + NPC shop item + Reputation + Title effect + VIP level ───────────
                HuaShanLuanJianService = LoadOptionalStreamingService(nameof(HuaShanLuanJianService), () => HuaShanLuanJianService.LoadFromStreamingAssets());
                SpriteAssetService = LoadOptionalStreamingService(nameof(SpriteAssetService), () => SpriteAssetService.LoadFromStreamingAssets());
                SoundEffectService = LoadOptionalStreamingService(nameof(SoundEffectService), () => SoundEffectService.LoadFromStreamingAssets());
                MapConnectionService = LoadOptionalStreamingService(nameof(MapConnectionService), () => MapConnectionService.LoadFromStreamingAssets());
                NpcShopItemService = LoadOptionalStreamingService(nameof(NpcShopItemService), () => NpcShopItemService.LoadFromStreamingAssets());
                ReputationService = LoadOptionalStreamingService(nameof(ReputationService), () => ReputationService.LoadFromStreamingAssets());
                TitleEffectService = LoadOptionalStreamingService(nameof(TitleEffectService), () => TitleEffectService.LoadFromStreamingAssets());
                VipLevelService = LoadOptionalStreamingService(nameof(VipLevelService), () => VipLevelService.LoadFromStreamingAssets());
                // ── Batch 12: Guild city war + Script registries (mission 985, skill 2,486, item 635, event 455, task 316, global 579, library 44) ───────────
                GuildCityWarService = new GuildCityWarService(CityWarService);
                GuildCityWarLogService = LoadOptionalStreamingService(nameof(GuildCityWarLogService), () => GuildCityWarLogService.LoadFromStreamingAssets());
                MissionScriptService = LoadOptionalStreamingService(nameof(MissionScriptService), () => MissionScriptService.LoadFromStreamingAssets());
                SkillScriptService = LoadOptionalStreamingService(nameof(SkillScriptService), () => SkillScriptService.LoadFromStreamingAssets());
                ItemScriptService = LoadOptionalStreamingService(nameof(ItemScriptService), () => ItemScriptService.LoadFromStreamingAssets());
                EventScriptService = LoadOptionalStreamingService(nameof(EventScriptService), () => EventScriptService.LoadFromStreamingAssets());
                TaskScriptService = LoadOptionalStreamingService(nameof(TaskScriptService), () => TaskScriptService.LoadFromStreamingAssets());
                GlobalScriptService = LoadOptionalStreamingService(nameof(GlobalScriptService), () => GlobalScriptService.LoadFromStreamingAssets());
                LibraryScriptService = LoadOptionalStreamingService(nameof(LibraryScriptService), () => LibraryScriptService.LoadFromStreamingAssets());
                // ── Batch 13: Area script registries (14.x GBK areas, faction quest, town, gbk trigger, tong battle) ───────────
                AreaScriptService = LoadOptionalStreamingService(nameof(AreaScriptService), () => AreaScriptService.LoadFromStreamingAssets());
                GbkMapScriptService = LoadOptionalStreamingService(nameof(GbkMapScriptService), () => GbkMapScriptService.LoadFromStreamingAssets());
                FactionQuestAreaService = LoadOptionalStreamingService(nameof(FactionQuestAreaService), () => FactionQuestAreaService.LoadFromStreamingAssets());
                TownScriptService = LoadOptionalStreamingService(nameof(TownScriptService), () => TownScriptService.LoadFromStreamingAssets());
                GbkTriggerService = LoadOptionalStreamingService(nameof(GbkTriggerService), () => GbkTriggerService.LoadFromStreamingAssets());
                TongBattleScriptService = LoadOptionalStreamingService(nameof(TongBattleScriptService), () => TongBattleScriptService.LoadFromStreamingAssets());

                // ── Batch 6: Client settings, items, maps (37 more services) ───────────
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
                RareSpawnService = LoadOptionalStreamingService(nameof(RareSpawnService), () => RareSpawnService.LoadFromStreamingAssets());
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

                LogOptionalServiceSummary();

                // Wire combat events to chat log
                if (GameplayLoop != null)
                    GameplayLoop.OnDeath += e =>
                    {
                        if (!e.isPlayer)
                            ChatService?.PostCombatLog($"{e.victimNameVi} bị giết. +{e.expReward}EXP");
                    };

                // Build mobile UI panels
                EnsureMobileUiPanels();

                // Place player at training pentagon center immediately so it never
                // appears at (0,0) before the map finishes loading.
                PlacePlayerAtDefaultSpawn();

                if (loadDefaultMapOnBoot && MapManager.Catalog.ContainsKey(defaultMapId))
                    MapManager.LoadMap(defaultMapId);
            }

            SubsystemLog.Info("Sandbox",
                $"Initialized v{SandboxVersion.Version} ({SandboxVersion.Codename}) " +
                $"at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            OnBootComplete?.Invoke(BootReport);
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
            SkillEffectVisual = new SkillEffectVisualService(new SprRuntimeService(), CombatSkillCatalog);

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
                    SubsystemLog.Info("Gameplay", $"Player chết! Respawn sau 5s.");
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
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
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

        private void SpawnEnemiesForActiveMap()
        {
            if (EnemyRuntime == null || MapManager?.ActiveMap == null)
                return;
            // Region_S folder contains server-side NPC spawn data with real PC coordinates.
            var regionSFolder = System.IO.Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", $"Map_{MapManager.ActiveMapId}");
            EnemyRuntime.SpawnForMap(MapManager.ActiveMapId, regionSFolder);
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
            PlayerController.EquipWeapon(PcWeaponType.LongWeapon);

            // Auto-equip horse from PlayerProgression. PC source: level 30+ unlocks horse
            // (see horseres.txt). Sandbox defaults: level 200 = red horse (id=5).
            int horseId = PlayerProgression?.horseId ?? 0;
            if (horseId > 0) PlayerController.SetHorseId(horseId);

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

        private void PlacePlayerOnActiveMap()
        {
            if (PlayerController == null)
                return;

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
                EnsureMountToggleButton(existing.transform.parent as RectTransform);
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
            canvas.sortingOrder = 100;
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
            background.sizeDelta = new Vector2(150f, 150f);
            var bgImage = backgroundGo.AddComponent<Image>();
            bgImage.sprite = CreateUiDiscSprite(new Color(0.15f, 0.85f, 0.25f, 0.28f), new Color(0.70f, 1f, 0.70f, 0.65f));

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(backgroundGo.transform, false);
            var handle = handleGo.AddComponent<RectTransform>();
            handle.anchorMin = new Vector2(0.5f, 0.5f);
            handle.anchorMax = new Vector2(0.5f, 0.5f);
            handle.pivot = new Vector2(0.5f, 0.5f);
            handle.anchoredPosition = Vector2.zero;
            handle.sizeDelta = new Vector2(74f, 74f);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.sprite = CreateUiDiscSprite(new Color(0.12f, 0.95f, 0.30f, 0.78f), new Color(0.85f, 1f, 0.85f, 0.95f));

            var joystick = backgroundGo.AddComponent<MobileJoystick>();
            joystick.background = background;
            joystick.handle = handle;
            joystick.radius = 58f;
            joystick.inputRadius = 55f;
            joystick.deadZone = 0.08f;
            joystick.sensitivity = 1.35f;
            EnsureMountToggleButton(canvasGo.GetComponent<RectTransform>());
            return joystick;
        }

        private void EnsureMountToggleButton(RectTransform canvasTransform)
        {
            if (canvasTransform == null) return;
            var existing = canvasTransform.Find("MountToggleButton");
            if (existing != null) return;

            var buttonGo = new GameObject("MountToggleButton");
            buttonGo.transform.SetParent(canvasTransform, false);
            var rt = buttonGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-32f, 220f);
            rt.sizeDelta = new Vector2(220f, 86f);
            var img = buttonGo.AddComponent<Image>();
            img.sprite = CreateUiDiscSprite(new Color(0.18f, 0.42f, 0.75f, 0.85f), new Color(0.85f, 0.95f, 1f, 0.95f));
            var btn = buttonGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() =>
            {
                if (PlayerController != null) PlayerController.ToggleMount();
            });

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(buttonGo.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var txt = labelGo.AddComponent<Text>();
            txt.text = "Ngựa";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 1f, 1f, 1f);
            txt.fontSize = 36;
            txt.fontStyle = FontStyle.Bold;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
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

            // Add HUD buttons for panels (on the joystick canvas)
            EnsureHudButtons();
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
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);
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
            GameplayLoop?.Tick(Time.deltaTime);
        }
    }
}
