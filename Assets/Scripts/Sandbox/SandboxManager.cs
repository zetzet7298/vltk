using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VLTK.Core;

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
                        ConfigureCameraForMap();
                        PlayerController?.SnapCamera();
                    }
                };
                MapManager.OnMapUnloaded += (mapId) => {
                    EnemyRuntime?.Clear();
                    MapRenderer.Clear();
                };

                EnsurePlayerController();

                if (loadDefaultMapOnBoot && MapManager.Catalog.ContainsKey(defaultMapId))
                    MapManager.LoadMap(defaultMapId);
            }

            SubsystemLog.Info("Sandbox",
                $"Initialized v{SandboxVersion.Version} ({SandboxVersion.Codename}) " +
                $"at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            OnBootComplete?.Invoke(BootReport);
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
        }

        private void SpawnEnemiesForActiveMap()
        {
            if (EnemyRuntime == null || MapManager?.ActiveMap == null)
                return;
            // Region_S folder contains server-side NPC spawn data with real PC coordinates.
            var regionSFolder = System.IO.Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", $"Map_{MapManager.ActiveMapId}");
            EnemyRuntime.SpawnFromRegionS(regionSFolder);
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

            PlayerVisual = PlayerController.visual;
            PlayerJoystick = EnsureMobileJoystick();
            PlayerController.BindJoystick(PlayerJoystick);

            SubsystemLog.Info("Sandbox", "Male player controller ready (8-way SPR parts + joystick)");
        }

        private void PlacePlayerOnActiveMap()
        {
            if (PlayerController == null)
                return;

            Vector2 spawn = Vector2.zero;
            if (MapRenderer != null && MapRenderer.HasContent)
                spawn = new Vector2(MapRenderer.ContentBounds.center.x, MapRenderer.ContentBounds.center.y);

            if (MapManager != null && MapManager.ActiveMapId == BaLangHuyenMapId)
            {
                var trainer = FindBaLangTrainerSpawn();
                if (trainer.HasValue)
                    spawn = trainer.Value;
            }

            PlayerController.ResetMovementState();
            PlayerController.PlaceAt(spawn, snapCamera: false);
            SubsystemLog.Info("Sandbox", $"Default player spawn set to {spawn} on map {MapManager?.ActiveMapId}");

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
                return existing;

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
            return joystick;
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
    }
}
