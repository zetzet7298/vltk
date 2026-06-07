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

        [Serializable]
        private sealed class RegionManifestJson
        {
            public RegionManifestCell[] regions;
        }

        [Serializable]
        private sealed class RegionManifestCell
        {
            public int col;
            public int row;
        }

        [Serializable]
        private sealed class GeneratedAliasCatalogJson
        {
            public GeneratedAliasEntry[] aliases;
        }

        [Serializable]
        private sealed class GeneratedAliasEntry
        {
            public int mapId;
            public string nameVi;
            public string pcMapPath;
            public string geometryKey;
            public string mapType;
        }

        [Serializable]
        private sealed class GeneratedGeometryCatalogJson
        {
            public GeneratedGeometryEntry[] geometries;
        }

        [Serializable]
        private sealed class GeneratedGeometryEntry
        {
            public string geometryKey;
            public string regionFolder;
            public string spriteFolder;
            public RectDef bounds;
            public int regionCount;
            public string status;
        }

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

            // Merge generated visual-map aliases/geometries before PC travel data.
            MergeGeneratedBulkCatalogs();

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

        private void MergeGeneratedBulkCatalogs()
        {
            var root = Application.streamingAssetsPath;
            var aliasCatalog = LoadJson<GeneratedAliasCatalogJson>(Path.Combine(root, "MapAliasCatalog.json"));
            var geometryCatalog = LoadJson<GeneratedGeometryCatalogJson>(Path.Combine(root, "MapGeometryCatalog.json"));
            if (aliasCatalog?.aliases == null || aliasCatalog.aliases.Length == 0)
                return;

            var geometries = new Dictionary<string, GeneratedGeometryEntry>(StringComparer.OrdinalIgnoreCase);
            if (geometryCatalog?.geometries != null)
            {
                foreach (var geometry in geometryCatalog.geometries)
                {
                    if (geometry == null || string.IsNullOrEmpty(geometry.geometryKey)) continue;
                    if (!IsGeometryAvailable(geometry)) continue;
                    geometries[geometry.geometryKey] = geometry;
                }
            }

            int added = 0, updated = 0;
            foreach (var alias in aliasCatalog.aliases)
            {
                if (alias == null || alias.mapId <= 0) continue;
                bool exists = _catalog.TryGetValue(alias.mapId, out var entry);
                if (!exists)
                {
                    entry = new MapCatalogEntry
                    {
                        mapId = alias.mapId,
                        defaultBrightness = 1f,
                        defaultColor = Color.white,
                        conversionStatus = ConversionStatus.Partial,
                    };
                    _catalog[alias.mapId] = entry;
                    added++;
                }
                else
                {
                    updated++;
                }

                string nameVi = alias.nameVi;
                if (MapPortManifest.TryGet(alias.mapId, out var manifestEntry) &&
                    !string.IsNullOrEmpty(manifestEntry.pcNameHint) &&
                    !string.IsNullOrEmpty(alias.pcMapPath) &&
                    alias.pcMapPath.Contains(manifestEntry.pcNameHint))
                {
                    nameVi = manifestEntry.nameVi;
                }
                if (!string.IsNullOrEmpty(nameVi))
                {
                    entry.displayNameRaw = nameVi;
                    entry.displayNameNormalized = nameVi;
                }
                if (!string.IsNullOrEmpty(alias.pcMapPath))
                    entry.sourceMapPath = alias.pcMapPath;
                if (!string.IsNullOrEmpty(alias.mapType))
                    entry.worldSetMembership = alias.mapType;
                if (!string.IsNullOrEmpty(alias.geometryKey))
                    entry.geometryKey = alias.geometryKey;

                if (!string.IsNullOrEmpty(alias.geometryKey) &&
                    geometries.TryGetValue(alias.geometryKey, out var geometry))
                {
                    entry.regionFolder = geometry.regionFolder;
                    entry.spriteFolder = geometry.spriteFolder;
                    entry.geometryBounds = geometry.bounds;
                    entry.conversionStatus = ConversionStatus.Partial;
                }
            }

            SubsystemLog.Info("MapManager",
                $"Generated visual map catalogs merged: +{added} maps, updated={updated}, geometries={geometries.Count}");
        }

        private static bool IsGeometryAvailable(GeneratedGeometryEntry geometry)
        {
            return geometry != null &&
                   geometry.regionCount > 0 &&
                   !string.IsNullOrEmpty(geometry.regionFolder) &&
                   geometry.bounds != null &&
                   geometry.bounds.width > 0f &&
                   geometry.bounds.height > 0f;
        }

        private static T LoadJson<T>(string path) where T : class
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
            catch (Exception ex)
            {
                SubsystemLog.Warn("MapManager", $"Failed to load {Path.GetFileName(path)}: {ex.Message}");
                return null;
            }
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
            bool hasGeometryBounds = entry.geometryBounds != null &&
                entry.geometryBounds.width > 0f && entry.geometryBounds.height > 0f;
            bool hasCatalogRect = entry.rect != null && entry.rect.width > 0f && entry.rect.height > 0f;
            bool hasManifestBounds = TryLoadRegionManifestBounds(entry, out var manifestBounds,
                out int manifestCountX, out int manifestCountY);

            float sourceX = (entry.rect?.x ?? 0f) * 512f;
            float sourceY = (entry.rect?.y ?? 0f) * 512f;
            float sourceW = Mathf.Max(1f, (entry.rect?.width ?? 1f) * 512f);
            float sourceH = Mathf.Max(1f, (entry.rect?.height ?? 1f) * 512f);
            RectDef sourceBounds;
            int regionCountX;
            int regionCountY;
            if (hasGeometryBounds)
            {
                sourceBounds = entry.geometryBounds;
                regionCountX = Mathf.Max(1, Mathf.CeilToInt(entry.geometryBounds.width / 512f));
                regionCountY = Mathf.Max(1, Mathf.CeilToInt(entry.geometryBounds.height / 512f));
            }
            else if (hasCatalogRect)
            {
                sourceBounds = new RectDef { x = sourceX, y = -sourceY - sourceH, width = sourceW, height = sourceH };
                regionCountX = (int)entry.rect.width;
                regionCountY = (int)entry.rect.height;
            }
            else if (hasManifestBounds)
            {
                sourceBounds = manifestBounds;
                regionCountX = manifestCountX;
                regionCountY = manifestCountY;
            }
            else
            {
                sourceBounds = new RectDef { x = sourceX, y = -sourceY - sourceH, width = sourceW, height = sourceH };
                regionCountX = 0;
                regionCountY = 0;
            }

            return new MapDefinition
            {
                catalogEntry = entry,
                regionCountX = regionCountX,
                regionCountY = regionCountY,
                regionWidthPixels = 512,
                regionHeightPixels = 1024,
                cellWidth = 32,
                cellHeight = 32,
                sourceBoundsRect = sourceBounds,
                mapLtRegionIndex = entry.mapLeftTopRegionIndex,
                environmentProfile = new EnvironmentProfile
                {
                    brightness = entry.defaultBrightness,
                    tint = entry.defaultColor,
                },
                conversionStatus = ConversionStatus.NotStarted,
            };
        }

        private static bool TryLoadRegionManifestBounds(MapCatalogEntry entry, out RectDef bounds, out int countX, out int countY)
        {
            bounds = null;
            countX = 0;
            countY = 0;
            var path = ResolveRegionManifestPath(entry);
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;

            RegionManifestJson manifest;
            try { manifest = JsonUtility.FromJson<RegionManifestJson>(File.ReadAllText(path)); }
            catch { return false; }
            if (manifest?.regions == null || manifest.regions.Length == 0) return false;

            int minCol = int.MaxValue, maxCol = int.MinValue, minRow = int.MaxValue, maxRow = int.MinValue;
            foreach (var region in manifest.regions)
            {
                minCol = Mathf.Min(minCol, region.col);
                maxCol = Mathf.Max(maxCol, region.col);
                minRow = Mathf.Min(minRow, region.row);
                maxRow = Mathf.Max(maxRow, region.row);
            }
            if (minCol == int.MaxValue || minRow == int.MaxValue) return false;

            countX = maxCol - minCol + 1;
            countY = maxRow - minRow + 1;
            float width = countX * 512f;
            float height = countY * 512f;
            bounds = new RectDef { x = minCol * 512f, y = -(minRow * 512f) - height, width = width, height = height };
            return true;
        }

        private static string ResolveRegionManifestPath(MapCatalogEntry entry)
        {
            if (entry == null) return null;
            if (!string.IsNullOrEmpty(entry.regionFolder))
            {
                var folder = Path.IsPathRooted(entry.regionFolder)
                    ? entry.regionFolder
                    : Path.Combine(Application.streamingAssetsPath, entry.regionFolder);
                var manifest = Path.Combine(folder, "manifest.json");
                if (File.Exists(manifest))
                    return manifest;
            }

            return Path.Combine(Application.streamingAssetsPath, "TestData", "Regions",
                $"Map_{entry.mapId}_C", "manifest.json");
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
