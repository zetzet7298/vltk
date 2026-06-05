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

    public class SandboxManager : MonoBehaviour
    {
        public const int BaLangHuyenMapId = 79;
        public const int PlayerActorId = 1;

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
                try { BattlefieldService = BattlefieldService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BattlefieldService: " + e.Message); }
                try { InstanceMapService = InstanceMapService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "InstanceMapService: " + e.Message); }
                try { HongbaoService = HongbaoService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "HongbaoService: " + e.Message); }
                try { ItemExchangeService = ItemExchangeService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ItemExchangeService: " + e.Message); }
                try { SpecialSkillService = SpecialSkillService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SpecialSkillService: " + e.Message); }
                try { NpcSkillService = NpcSkillService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "NpcSkillService: " + e.Message); }
                try { TranslifeSkillService = TranslifeSkillService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TranslifeSkillService: " + e.Message); }
                try { SkillTemplateService = SkillTemplateService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SkillTemplateService: " + e.Message); }
                try { NpcLevelScriptService = NpcLevelScriptService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "NpcLevelScriptService: " + e.Message); }
                try { NpcDeathScriptService = NpcDeathScriptService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "NpcDeathScriptService: " + e.Message); }
                try { DailyTaskService = DailyTaskService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "DailyTaskService: " + e.Message); }
                try { BossMissionService = BossMissionService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BossMissionService: " + e.Message); }

                // ── Batch 3: Events, weather, music, tasks, arena, trip, bonus ───────
                try { ServerEventService = ServerEventService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ServerEventService: " + e.Message); }
                try { VngEventService = VngEventService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "VngEventService: " + e.Message); }
                try { BattleScriptService = BattleScriptService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BattleScriptService: " + e.Message); }
                try { WeatherService = WeatherService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "WeatherService: " + e.Message); }
                try { MusicService = MusicService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MusicService: " + e.Message); }
                try { GuildWorkshopService = GuildWorkshopService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "GuildWorkshopService: " + e.Message); }
                try { HuoYueDuService = HuoYueDuService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "HuoYueDuService: " + e.Message); }
                try { CityDefenceService = CityDefenceService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "CityDefenceService: " + e.Message); }
                try { ActivityService = ActivityService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ActivityService: " + e.Message); }
                try { RandomTaskService = RandomTaskService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "RandomTaskService: " + e.Message); }
                try { PartnerTaskService = PartnerTaskService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "PartnerTaskService: " + e.Message); }
                try { MetempsychosisTaskService = MetempsychosisTaskService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MetempsychosisTaskService: " + e.Message); }
                try { ArenaService = ArenaService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ArenaService: " + e.Message); }
                try { TripService = TripService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TripService: " + e.Message); }
                try { BonusOnlineService = BonusOnlineService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BonusOnlineService: " + e.Message); }

                // ── Batch 4: Guild extras, honor, shitu, foundry, world rank, misc ──
                try { GuildRankService = GuildRankService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "GuildRankService: " + e.Message); }
                try { GuildStuntService = GuildStuntService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "GuildStuntService: " + e.Message); }
                try { GuildTaskService = GuildTaskService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "GuildTaskService: " + e.Message); }
                try { HonorService = HonorService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "HonorService: " + e.Message); }
                try { ShituService = ShituService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ShituService: " + e.Message); }
                try { FoundryService = FoundryService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "FoundryService: " + e.Message); }
                try { WorldRankService = WorldRankService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "WorldRankService: " + e.Message); }
                try { NewPlayerGuideService = NewPlayerGuideService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "NewPlayerGuideService: " + e.Message); }
                try { ChangeFeatureService = ChangeFeatureService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ChangeFeatureService: " + e.Message); }
                try { StallService = StallService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "StallService: " + e.Message); }
                try { FlipCardService = FlipCardService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "FlipCardService: " + e.Message); }
                try { BaoRuongThanBiService = BaoRuongThanBiService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BaoRuongThanBiService: " + e.Message); }
                try { SeasonalEventService = SeasonalEventService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SeasonalEventService: " + e.Message); }
                try { CompensationService = CompensationService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "CompensationService: " + e.Message); }

                // ── Batch 5: Final client systems (faction maps, awards, double exp, sim city, client skill scripts) ─
                try { FactionMapService = FactionMapService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "FactionMapService: " + e.Message); }
                try { BattleAwardService = BattleAwardService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BattleAwardService: " + e.Message); }
                try { DoubleExpService = DoubleExpService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "DoubleExpService: " + e.Message); }
                try { SimCityPluginService = SimCityPluginService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SimCityPluginService: " + e.Message); }
                try { ClientSkillScriptService = ClientSkillScriptService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ClientSkillScriptService: " + e.Message); }
                try { TongJinBattleService = TongJinBattleService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TongJinBattleService: " + e.Message); }
                try { BangChienService = BangChienService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BangChienService: " + e.Message); }
                try { BossHoangKimService = BossHoangKimService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BossHoangKimService: " + e.Message); }
                TaskFlagService = new TaskFlagService();
                // ── Batch 8: Save/Load + Mail + Mount + Ranking + Friend + Pet + Shop + Missile + HudArt + FactionRuntime + BattleScript + TaskFlagRegistry ───────────
                try { SaveSlotService = SaveSlotService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SaveSlotService: " + e.Message); }
                try { MailService = MailService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MailService: " + e.Message); }
                try { MountService = MountService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MountService: " + e.Message); }
                try { RankingService = RankingService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "RankingService: " + e.Message); }
                try { FriendService = FriendService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "FriendService: " + e.Message); }
                try { PetService = PetService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "PetService: " + e.Message); }
                try { ShopConfigService = ShopConfigService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ShopConfigService: " + e.Message); }
                try { MissileEffectService = MissileEffectService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MissileEffectService: " + e.Message); }
                try { HudArtCatalogService = HudArtCatalogService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "HudArtCatalogService: " + e.Message); }
                FactionMapRuntimeService = FactionMapRuntimeService != null ? FactionMapRuntimeService : new FactionMapRuntimeService(FactionMapService);
                BattleScriptRuntimeService = BattleScriptRuntimeService != null ? BattleScriptRuntimeService : new BattleScriptRuntimeService(BattleScriptService);
                try { TaskFlagRegistryService = TaskFlagRegistryService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TaskFlagRegistryService: " + e.Message); }
                // ── Batch 9: Map data + Skill data + World boss + Achievement + Mall + Fashion + Sign-in + Treasure + Encounter + Friend gift + Text + Animation ───────────
                try { MapListFullService = MapListFullService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapListFullService: " + e.Message); }
                try { MapElementService = MapElementService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapElementService: " + e.Message); }
                try { MapRespawnService = MapRespawnService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapRespawnService: " + e.Message); }
                try { MapBlockService = MapBlockService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapBlockService: " + e.Message); }
                try { MapNpcRespawnService = MapNpcRespawnService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapNpcRespawnService: " + e.Message); }
                try { MapMusicService = MapMusicService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapMusicService: " + e.Message); }
                try { SkillLevelDataService = SkillLevelDataService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SkillLevelDataService: " + e.Message); }
                try { SkillUpgradeService = SkillUpgradeService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SkillUpgradeService: " + e.Message); }
                try { SkillBookService = SkillBookService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SkillBookService: " + e.Message); }
                try { SkillComboService = SkillComboService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SkillComboService: " + e.Message); }
                try { SkillStateService = SkillStateService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SkillStateService: " + e.Message); }
                try { SkillMasteryService = SkillMasteryService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SkillMasteryService: " + e.Message); }
                try { WorldBossService = WorldBossService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "WorldBossService: " + e.Message); }
                try { AchievementService = AchievementService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "AchievementService: " + e.Message); }
                try { DailyRewardService = DailyRewardService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "DailyRewardService: " + e.Message); }
                try { MallService = MallService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MallService: " + e.Message); }
                try { FashionService = FashionService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "FashionService: " + e.Message); }
                try { SignInService = SignInService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SignInService: " + e.Message); }
                try { TreasureHuntService = TreasureHuntService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TreasureHuntService: " + e.Message); }
                try { EncounterService = EncounterService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "EncounterService: " + e.Message); }
                try { FriendGiftService = FriendGiftService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "FriendGiftService: " + e.Message); }
                try { TextResourceService = TextResourceService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TextResourceService: " + e.Message); }
                try { AnimationBankService = AnimationBankService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "AnimationBankService: " + e.Message); }

                // ── Batch 6: Client settings, items, maps (37 more services) ───────────
                try { PortraitService = PortraitService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "PortraitService: " + e.Message); }
                try { SoundListService = SoundListService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "SoundListService: " + e.Message); }
                try { KillerService = KillerService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "KillerService: " + e.Message); }
                try { ItemDetailService = ItemDetailService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ItemDetailService: " + e.Message); }
                try { ItemTypeService = ItemTypeService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ItemTypeService: " + e.Message); }
                try { MapTrafficService = MapTrafficService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapTrafficService: " + e.Message); }
                try { MapTypeService = MapTypeService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapTypeService: " + e.Message); }
                try { AdjustColorService = AdjustColorService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "AdjustColorService: " + e.Message); }
                try { ClientWeaponSkillService = ClientWeaponSkillService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ClientWeaponSkillService: " + e.Message); }
                try { GoldEquipService = GoldEquipService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "GoldEquipService: " + e.Message); }
                try { PlatinaEquipService = PlatinaEquipService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "PlatinaEquipService: " + e.Message); }
                try { HorseService = HorseService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "HorseService: " + e.Message); }
                try { PotionService = PotionService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "PotionService: " + e.Message); }
                try { MagicScriptService = MagicScriptService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MagicScriptService: " + e.Message); }
                try { MagicAttribService = MagicAttribService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MagicAttribService: " + e.Message); }
                try { ScrollService = ScrollService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ScrollService: " + e.Message); }
                try { CaveListFullService = CaveListFullService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "CaveListFullService: " + e.Message); }
                try { GoldBossService = GoldBossService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "GoldBossService: " + e.Message); }
                try { ChangeFeatureDataService = ChangeFeatureDataService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "ChangeFeatureDataService: " + e.Message); }
                try { GlobalConfigService = GlobalConfigService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "GlobalConfigService: " + e.Message); }
                try { NormalSpawnService = NormalSpawnService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "NormalSpawnService: " + e.Message); }
                try { RareSpawnService = RareSpawnService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "RareSpawnService: " + e.Message); }
                try { WharfService = WharfService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "WharfService: " + e.Message); }
                try { WaypointService = WaypointService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "WaypointService: " + e.Message); }
                try { AutoPathRouteService = AutoPathRouteService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "AutoPathRouteService: " + e.Message); }
                try { RevivePosService = RevivePosService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "RevivePosService: " + e.Message); }
                try { FactionConfigService = FactionConfigService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "FactionConfigService: " + e.Message); }
                try { NpcResService = NpcResService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "NpcResService: " + e.Message); }
                try { NpcSFullService = NpcSFullService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "NpcSFullService: " + e.Message); }
                try { TongStuntService = TongStuntService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TongStuntService: " + e.Message); }
                try { TongSettingService = TongSettingService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TongSettingService: " + e.Message); }
                try { TongNpcPosService = TongNpcPosService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "TongNpcPosService: " + e.Message); }
                try { MapListService = MapListService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapListService: " + e.Message); }
                try { MapDescService = MapDescService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "MapDescService: " + e.Message); }
                try { BossSpawnService = BossSpawnService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "BossSpawnService: " + e.Message); }
                try { DropRateConfigService = DropRateConfigService.LoadFromStreamingAssets(); } catch (Exception e) { SubsystemLog.Warn("Sandbox", "DropRateConfigService: " + e.Message); }

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
