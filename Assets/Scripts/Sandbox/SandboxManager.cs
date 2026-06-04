using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Sprites;

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
        public BaLangEnemySpawnRuntime EnemyRuntime { get; private set; }
        public BaLangEnemyNameplateOverlay EnemyNameplateOverlay { get; private set; }
        public TrainingNpcSpawner TrainingSpawner { get; private set; }
        public FemalePlayerVisual FemalePlayerVisual { get; private set; }
        public SkillCatalog CombatSkillCatalog { get; private set; }
        public CombatRuntimeService CombatRuntime { get; private set; }
        public GameplayLoopService GameplayLoop { get; private set; }
        public PlayerProgressionState PlayerProgression { get; private set; }
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
            CombatSkillCatalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog(AssetRegistry);
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

        public void GrantCaiBangSkillPanelProgression()
        {
            if (CombatSkillCatalog == null)
                BootstrapCombatRuntime();
            PlayerProgression ??= new PlayerProgressionState();
            PlayerProgression.GrantCaiBangSkillPanelProgression(CombatSkillCatalog);
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
            var enemyGo = new GameObject("BaLangEnemyRuntime");
            enemyGo.transform.SetParent(worldRoot, false);
            EnemyRuntime = enemyGo.AddComponent<BaLangEnemySpawnRuntime>();
            EnemyNameplateOverlay = enemyGo.AddComponent<BaLangEnemyNameplateOverlay>();
            TrainingSpawner = enemyGo.AddComponent<TrainingNpcSpawner>();
        }

        private void SpawnEnemiesForActiveMap()
        {
            if (EnemyRuntime == null || MapManager?.ActiveMap == null)
                return;
            // Region_S folder contains server-side NPC spawn data with real PC coordinates.
            var regionSFolder = System.IO.Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", $"Map_{MapManager.ActiveMapId}");
            EnemyRuntime.SpawnFromRegionS(regionSFolder);
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

            PlayerVisual = PlayerController.visual;
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
            // Training area center from PC Region_S data, verified in-game.
            // World (53246, -52041) = MPS (53246, 104082)
            Vector2 spawn = new Vector2(53246f, -52041f);
            PlayerController.ResetMovementState();
            PlayerController.PlaceAt(spawn, snapCamera: false);
            SubsystemLog.Info("Sandbox", $"Player pre-placed at {spawn} (MPS 53493,95313) before map load");
        }

        private void PlacePlayerOnActiveMap()
        {
            if (PlayerController == null)
                return;

            // Training area center: world (53246, -52041)
            Vector2 spawn = new Vector2(53246f, -52041f);

            PlayerController.ResetMovementState();
            PlayerController.PlaceAt(spawn, snapCamera: false);
            SubsystemLog.Info("Sandbox", $"Default player spawn set to {spawn} (training center) on map {MapManager?.ActiveMapId}");

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
            EnsureMountToggleButton(canvasGo.transform);
            return joystick;
        }

        private void EnsureMountToggleButton(RectTransform canvasTransform)
        {
            if (canvasTransform == null) return;
            var existing = canvasTransform.Find("MountToggleButton");
            if (existing != null) return;
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
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
