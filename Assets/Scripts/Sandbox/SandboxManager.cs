using System;
using System.Collections.Generic;
using UnityEngine;
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
        // M1.2: Region catalog and report
        public RegionCatalogFile RegionCatalog { get; private set; }
        public RegionConversionReport RegionReport { get; private set; }

        public event Action<SandboxBootReport> OnBootComplete;

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
                        FrameCameraOnMap();
                    }
                };
                MapManager.OnMapUnloaded += (mapId) => {
                    MapRenderer.Clear();
                };

                if (loadDefaultMapOnBoot && MapManager.Catalog.ContainsKey(defaultMapId))
                    MapManager.LoadMap(defaultMapId);
            }

            SubsystemLog.Info("Sandbox",
                $"Initialized v{SandboxVersion.Version} ({SandboxVersion.Codename}) " +
                $"at {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            OnBootComplete?.Invoke(BootReport);
        }

        /// <summary>
        /// Frame the sandbox camera on the rendered map content. Switches the
        /// camera to orthographic and centers/zooms it so the whole map is visible.
        /// </summary>
        public void FrameCameraOnMap()
        {
            if (MapRenderer == null || !MapRenderer.HasContent) return;

            var cam = FindSandboxCamera();
            if (cam == null)
            {
                SubsystemLog.Warn("Sandbox", "No camera found to frame map");
                return;
            }

            var b = MapRenderer.ContentBounds;
            cam.orthographic = true;
            // Solid background so the skybox gradient doesn't bleed through the
            // semi-transparent overlay.
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.12f, 0.14f, 1f);

            float aspect = cam.aspect > 0.01f ? cam.aspect : 0.5625f;
            float halfH = b.size.y * 0.5f;
            float halfW = (b.size.x * 0.5f) / aspect;
            float size = Mathf.Max(halfH, halfW) * 1.05f; // 5% margin
            cam.orthographicSize = Mathf.Max(size, 1f);

            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 5000f);

            // Content sits on the XY plane; place the camera in front (-Z) looking +Z.
            cam.transform.position = new Vector3(b.center.x, b.center.y, -100f);
            cam.transform.rotation = Quaternion.identity;

            SubsystemLog.Info("Sandbox",
                $"Camera framed on map: center=({b.center.x:F0},{b.center.y:F0}) orthoSize={cam.orthographicSize:F0}");
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
