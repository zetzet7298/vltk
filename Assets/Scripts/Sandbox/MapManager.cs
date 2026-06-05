using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class MapManager
    {
        private readonly Dictionary<int, MapCatalogEntry> _catalog = new();
        private readonly Dictionary<int, MapDefinition> _definitions = new();
        private readonly IAssetRegistry _registry;
        private int _activeMapId = -1;
        private MapDefinition _activeMap;

        public event Action<int> OnMapLoaded;
        public event Action<int> OnMapUnloaded;
        public event Action<string> OnMapError;

        public int ActiveMapId => _activeMapId;
        public MapDefinition ActiveMap => _activeMap;
        public IReadOnlyDictionary<int, MapCatalogEntry> Catalog => _catalog;
        public PcMapRuntimeDataRegistry TravelData { get; private set; }
        /// <summary>Discovery report from the map catalog tool (AC4). Null when using placeholder catalog.</summary>
        public MapDiscoveryReport DiscoveryReport { get; private set; }

        /// <summary>M1.12: Get full MapDefinition for a loaded map (null if not loaded/converted).</summary>
        public MapDefinition GetDefinition(int mapId)
        {
            _definitions.TryGetValue(mapId, out var def);
            return def;
        }

        /// <summary>
        /// Create MapManager without an asset registry (placeholder/test use).
        /// </summary>
        public MapManager() { }

        /// <summary>
        /// Create MapManager with an asset registry (M0.6: all resource access goes through registry).
        /// </summary>
        public MapManager(IAssetRegistry registry)
        {
            _registry = registry;
        }

        /// <summary>
        /// Load map catalog. Tries StreamingAssets/MapCatalog.json first,
        /// then merges full PC settings/maplist.ini data when present;
        /// falls back to built-in placeholders for development.
        /// </summary>
        public void LoadCatalog()
        {
            // Try real catalog first
            var catalogFile = MapCatalogLoader.LoadFromStreamingAssets();
            if (catalogFile != null && catalogFile.totalMaps > 0)
            {
                var entries = MapCatalogLoader.ToModelEntries(catalogFile);
                foreach (var entry in entries)
                    _catalog[entry.mapId] = entry;

                // AC4: parse and expose discovery report
                var report = MapCatalogLoader.ToDiscoveryReport(catalogFile);
                DiscoveryReport = report;
                if (report != null)
                {
                    SubsystemLog.Info("MapManager",
                        $"Discovery: {report.totalDiscovered} total, {report.available} available, {report.missing} missing");
                }

                SubsystemLog.Info("MapManager", $"Real catalog loaded: {_catalog.Count} maps");
            }

            // Merge full PC map settings (maplist/cavelist/tong/waypoint/scroll/wharf/revivepos).
            MergePcMapData();
            if (_catalog.Count > 0)
            {
                SubsystemLog.Info("MapManager", $"Catalog ready: {_catalog.Count} maps");
                return;
            }

            // Fallback to placeholder
            AddPlaceholder(1, "Bach Duong Son", false);
            AddPlaceholder(2, "Thieu Lam Tu", true);
            AddPlaceholder(3, "Thien Vuong Bang", false);
            AddPlaceholder(4, "Duong Mon", true);
            AddPlaceholder(5, "Nga My", false);
            AddPlaceholder(6, "Thuy Yen Mon", true);
            AddPlaceholder(7, "Cai Bang", false);
            AddPlaceholder(8, "Con Lon", true);
            AddPlaceholder(9, "To Vu Son", false);
            AddPlaceholder(10, "Thanh Do", true);
            AddPlaceholder(11, "Phuong Tuong", true);
            AddPlaceholder(12, "Tuong Duong", false);
            AddPlaceholder(13, "Kiem Ge", false);
            AddPlaceholder(14, "Dai Ly", true);
            AddPlaceholder(15, "Lam Tuyet Son", false);

            SubsystemLog.Info("MapManager", $"Placeholder catalog loaded: {_catalog.Count} maps");
        }

        /// <summary>Kept for backward compat — delegates to LoadCatalog.</summary>
        public void LoadPlaceholderCatalog() => LoadCatalog();

        private void MergePcMapData()
        {
            var pcMapDir = Path.Combine(Application.streamingAssetsPath, "Reference/PcMap");
            if (!Directory.Exists(pcMapDir)) return;

            var batch = PcMapDataBatchLoader.Load(pcMapDir, pcMapDir);
            TravelData = PcMapRuntimeDataRegistry.FromBatch(batch);
            var runtimeEntries = PcMapDataBatchLoader.BuildRuntimeCatalog(batch);
            int added = 0;
            foreach (var entry in runtimeEntries)
            {
                if (entry == null || entry.mapId <= 0) continue;
                if (_catalog.ContainsKey(entry.mapId)) continue;
                _catalog[entry.mapId] = entry;
                added++;
            }

            SubsystemLog.Info("MapManager",
                $"PC map data merged: +{added} maps, caves={batch.caves.Count}, tongs={batch.tongs.Count}, " +
                $"waypoints={batch.waypoints.Count}, scrolls={batch.scrolls.Count}, wharves={batch.wharves.Count}, revive={batch.revivePositions.Count}");
        }

        public void LoadMap(int mapId)
        {
            if (!_catalog.TryGetValue(mapId, out var entry))
            {
                var msg = $"Map {mapId} not found in catalog";
                OnMapError?.Invoke(msg);
                SubsystemLog.Error("MapManager", msg);
                return;
            }

            if (_activeMapId == mapId)
            {
                SubsystemLog.Warn("MapManager", $"Map {mapId} already loaded");
                return;
            }

            UnloadCurrentMap();

            var def = BuildRuntimeDefinition(entry);

            _activeMapId = mapId;
            _activeMap = def;
            _definitions[mapId] = def;

            // M0.6: register the loaded map definition in the asset registry
            if (_registry != null)
            {
                var regEntry = new AssetRegistryEntry
                {
                    sourceId = new SourceAssetId
                    {
                        sourcePath = entry.sourceMapPath ?? $"maps/{mapId}",
                        packageName = "maps_pak",
                        uid = mapId,
                        resourceKind = ResourceKind.Map,
                        discoveryTool = DiscoveryTool.Runtime,
                        evidenceNote = $"Loaded via MapManager at runtime",
                    },
                    artifactType = ArtifactType.MapDefinition,
                    unityAssetPath = entry.sourceMapPath ?? $"maps/{mapId}",
                    loadMode = LoadMode.StreamingAssets,
                    status = entry.conversionStatus == ConversionStatus.Complete
                        ? AssetStatus.Available
                        : AssetStatus.Pending,
                };
                _registry.Register(regEntry);
                SubsystemLog.Info("MapManager", $"Registered map id={mapId} in asset registry (status={regEntry.status})");
            }

            SubsystemLog.Info("MapManager", $"Loaded map: {entry.displayNameNormalized} (id={mapId})");
            OnMapLoaded?.Invoke(mapId);
        }

        private MapDefinition BuildRuntimeDefinition(MapCatalogEntry entry)
        {
            float sourceX = (entry.rect?.x ?? 0f) * 512f;
            float sourceY = (entry.rect?.y ?? 0f) * 512f;
            float sourceW = Mathf.Max(1f, (entry.rect?.width ?? 1f) * 512f);
            float sourceH = Mathf.Max(1f, (entry.rect?.height ?? 1f) * 512f);

            return new MapDefinition
            {
                catalogEntry = entry,
                regionCountX = (int)(entry.rect?.width ?? 0),
                regionCountY = (int)(entry.rect?.height ?? 0),
                regionWidthPixels = 512,
                regionHeightPixels = 1024,
                cellWidth = 32,
                cellHeight = 32,
                sourceBoundsRect = new RectDef
                {
                    x = sourceX,
                    y = -sourceY - sourceH,
                    width = sourceW,
                    height = sourceH,
                },
                mapLtRegionIndex = entry.mapLeftTopRegionIndex,
                environmentProfile = new EnvironmentProfile
                {
                    brightness = entry.defaultBrightness,
                    tint = entry.defaultColor,
                },
                conversionStatus = ConversionStatus.NotStarted,
            };
        }

        public void UnloadCurrentMap()
        {
            if (_activeMapId < 0) return;

            var oldId = _activeMapId;
            _activeMapId = -1;
            _activeMap = null;

            SubsystemLog.Info("MapManager", $"Unloaded map id={oldId}");
            OnMapUnloaded?.Invoke(oldId);
        }

        public List<MapCatalogEntry> GetAllEntries()
        {
            var result = new List<MapCatalogEntry>(_catalog.Values);
            result.Sort((a, b) => a.mapId.CompareTo(b.mapId));
            return result;
        }

        public List<MapCatalogEntry> Search(string query)
        {
            var result = new List<MapCatalogEntry>();
            if (string.IsNullOrEmpty(query))
            {
                result.AddRange(_catalog.Values);
                result.Sort((a, b) => a.mapId.CompareTo(b.mapId));
                return result;
            }

            var q = query.ToLowerInvariant();
            foreach (var entry in _catalog.Values)
            {
                if (entry.mapId.ToString().Contains(q) ||
                    (entry.displayNameNormalized != null &&
                     entry.displayNameNormalized.ToLowerInvariant().Contains(q)))
                {
                    result.Add(entry);
                }
            }
            result.Sort((a, b) => a.mapId.CompareTo(b.mapId));
            return result;
        }

        private void AddPlaceholder(int id, string name, bool isIndoor)
        {
            _catalog[id] = new MapCatalogEntry
            {
                mapId = id,
                displayNameRaw = name,
                displayNameNormalized = name,
                isIndoor = isIndoor,
                defaultBrightness = 1f,
                defaultColor = Color.white,
                conversionStatus = ConversionStatus.NotStarted,
            };
        }
    }
}
