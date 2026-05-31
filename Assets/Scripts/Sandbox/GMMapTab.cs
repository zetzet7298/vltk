using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class GMMapTab : MonoBehaviour
    {
        [Header("UI References")]
        public Transform listContent;
        public InputField searchInput;
        public Button searchButton;
        public Button loadButton;
        public Button unloadButton;
        public Text statusText;
        public Text mapInfoText;
        public Text errorText;
        public Toggle obstacleToggle;
        // M1.10 AC#1: status filter dropdown (All/Available/Missing/Failed)
        public Dropdown statusFilterDropdown;
        // M1.10 AC#4: random map button
        public Button randomMapButton;
        // M1.10 AC#5: batch audit button + log text
        public Button batchAuditButton;
        public Text batchAuditLogText;
        // M1.8: minimap preview wiring
        public Toggle minimapToggle;
        public RawImage minimapImage;
        public RectTransform minimapMarker;
        public Text minimapMissingText;

        private readonly List<MapCatalogEntry> _displayed = new();
        private int _selectedMapId = -1;
        private MapManager _mapManager;
        // M1.8: minimap service
        private MinimapService _minimapService;
        // M1.10 AC#5: batch audit state
        private bool _batchRunning;
        private int _batchIndex;
        private List<string> _batchLog = new();

        private void Start()
        {
            _mapManager = SandboxManager.Instance?.MapManager;

            if (searchButton != null)
                searchButton.onClick.AddListener(DoSearch);

            if (loadButton != null)
                loadButton.onClick.AddListener(LoadSelectedMap);

            if (unloadButton != null)
                unloadButton.onClick.AddListener(UnloadCurrentMap);

            if (searchInput != null)
                searchInput.onEndEdit.AddListener(_ => DoSearch());

            if (obstacleToggle != null)
            {
                obstacleToggle.onValueChanged.AddListener(OnObstacleToggleChanged);
                var r = SandboxManager.Instance?.MapRenderer?.ObstacleOverlay;
                if (r != null)
                    obstacleToggle.isOn = r.IsVisible;
            }

            // M1.10 AC#1: status filter
            if (statusFilterDropdown != null)
                statusFilterDropdown.onValueChanged.AddListener(_ => RefreshList());

            // M1.10 AC#4: random map
            if (randomMapButton != null)
                randomMapButton.onClick.AddListener(LoadRandomMap);

            // M1.10 AC#5: batch audit
            if (batchAuditButton != null)
                batchAuditButton.onClick.AddListener(StartBatchAudit);

            // M1.8: minimap toggle
            var registry = SandboxManager.Instance?.AssetRegistry;
            if (registry != null)
                _minimapService = new MinimapService(registry);
            if (minimapToggle != null)
                minimapToggle.onValueChanged.AddListener(OnMinimapToggleChanged);
            SetMinimapVisible(false);

            // M0.10 AC#4: Subscribe to error events
            if (_mapManager != null)
            {
                _mapManager.OnMapError += ShowError;
                _mapManager.OnMapLoaded += _ => { ClearError(); RefreshList(); RefreshMinimap(); };
                _mapManager.OnMapUnloaded += _ => RefreshList();
            }

            RefreshList();
        }

        // M1.8 AC#2/AC#4: toggle minimap preview, resolve artifact, show missing state.
        private void OnMinimapToggleChanged(bool show)
        {
            SetMinimapVisible(show);
            if (show) RefreshMinimap();
        }

        private void SetMinimapVisible(bool show)
        {
            if (minimapImage != null) minimapImage.gameObject.SetActive(show);
            if (minimapMarker != null) minimapMarker.gameObject.SetActive(show);
            if (minimapMissingText != null && !show) minimapMissingText.gameObject.SetActive(false);
        }

        /// <summary>
        /// M1.8 AC#2/AC#3/AC#4: resolve the active map's minimap artifact, show the
        /// preview when registered, and surface a missing state (with source id)
        /// when absent.
        /// </summary>
        public void RefreshMinimap()
        {
            if (_minimapService == null || minimapToggle == null || !minimapToggle.isOn) return;
            var map = SandboxManager.Instance?.MapManager?.ActiveMap;
            if (map == null)
            {
                if (minimapMissingText != null)
                {
                    minimapMissingText.text = "Minimap: no map loaded";
                    minimapMissingText.gameObject.SetActive(true);
                }
                return;
            }

            var minimap = _minimapService.ResolveArtifact(map);
            bool missing = _minimapService.IsMissing(map);
            if (minimapMissingText != null)
            {
                if (missing)
                {
                    var src = _minimapService.GetMissingSourceId(map)?.ToKey() ?? "<unknown>";
                    minimapMissingText.text = $"Minimap missing (source: {src})";
                    minimapMissingText.gameObject.SetActive(true);
                }
                else
                {
                    minimapMissingText.gameObject.SetActive(false);
                }
            }
            if (minimapImage != null) minimapImage.enabled = !missing;
            UpdateMinimapMarker(map);
        }

        /// <summary>M1.8 AC#3: position the player marker in correct minimap scale.</summary>
        public void UpdateMinimapMarker(MapDefinition map)
        {
            if (_minimapService == null || minimapMarker == null || minimapImage == null) return;
            var rt = minimapImage.rectTransform;
            var size = rt.rect.size;
            var pixel = _minimapService.WorldToMinimapPixel(map, MarkerWorldPosition(map), size);
            // RawImage anchored top-left; marker uses top-left origin pixel.
            minimapMarker.anchoredPosition = new Vector2(pixel.x, -pixel.y);
        }

        /// <summary>
        /// World position used for the minimap marker. No player entity exists in
        /// the sandbox yet, so this defaults to the map bounds center; assign
        /// <see cref="markerWorldOverride"/> to drive it from a player/camera.
        /// </summary>
        public Vector2? markerWorldOverride;

        private Vector2 MarkerWorldPosition(MapDefinition map)
        {
            if (markerWorldOverride.HasValue) return markerWorldOverride.Value;
            var r = map?.sourceBoundsRect;
            if (r == null) return Vector2.zero;
            return new Vector2(r.x + r.width * 0.5f, r.y + r.height * 0.5f);
        }

        private void OnObstacleToggleChanged(bool show)
        {
            var r = SandboxManager.Instance?.MapRenderer?.ObstacleOverlay;
            if (r != null)
            {
                if (show) r.Show();
                else r.Hide();
            }
        }

        public void RefreshList()
        {
            var mgr = SandboxManager.Instance?.MapManager;
            if (mgr == null) return;

            var query = searchInput != null ? searchInput.text : "";

            // M1.10 AC#1: status filter
            ConversionStatus? statusFilter = null;
            if (statusFilterDropdown != null)
            {
                statusFilter = statusFilterDropdown.value switch
                {
                    1 => ConversionStatus.NotStarted,
                    2 => ConversionStatus.Complete,
                    3 => ConversionStatus.Failed,
                    4 => ConversionStatus.Partial,
                    _ => null,
                };
            }

            _displayed.Clear();
            var results = mgr.Search(query);
            foreach (var e in results)
            {
                if (statusFilter == null || e.conversionStatus == statusFilter)
                    _displayed.Add(e);
            }
            RebuildListUI();
            UpdateStatus();
        }

        public void DoSearch()
        {
            RefreshList();
        }

        public void SelectMap(int mapId)
        {
            _selectedMapId = mapId;
            UpdateMapInfo();
        }

        public void LoadSelectedMap()
        {
            if (_selectedMapId < 0) return;
            var mgr = SandboxManager.Instance?.MapManager;
            if (mgr == null) return;

            ClearError();
            mgr.LoadMap(_selectedMapId);
            UpdateStatus();
        }

        /// <summary>M1.10 AC#4: Load a random map from the currently filtered list.</summary>
        public void LoadRandomMap()
        {
            if (_displayed.Count == 0) return;
            var pick = _displayed[UnityEngine.Random.Range(0, _displayed.Count)];
            SelectMap(pick.mapId);
            LoadSelectedMap();
            SubsystemLog.Info("GMMapTab", $"Random map selected: {pick.mapId} ({pick.displayNameNormalized})");
        }

        /// <summary>M1.10 AC#5: Start batch audit — cycles through filtered maps, records load success/failure.</summary>
        public void StartBatchAudit()
        {
            if (_batchRunning) return;
            _batchRunning = true;
            _batchIndex = 0;
            _batchLog.Clear();
            if (batchAuditLogText != null) batchAuditLogText.text = "Batch audit starting...";
            SubsystemLog.Info("GMMapTab", $"Batch audit started: {_displayed.Count} maps");

            // Subscribe to map loaded event to advance to next map
            if (_mapManager != null)
            {
                _mapManager.OnMapLoaded -= OnBatchMapLoaded;
                _mapManager.OnMapError -= OnBatchMapError;
                _mapManager.OnMapLoaded += OnBatchMapLoaded;
                _mapManager.OnMapError += OnBatchMapError;
            }
            AdvanceBatchAudit();
        }

        private void AdvanceBatchAudit()
        {
            if (_batchIndex >= _displayed.Count)
            {
                _batchRunning = false;
                var summary = $"Batch audit complete: {_displayed.Count} maps. " +
                              $"Errors: {_batchLog.FindAll(l => l.StartsWith("[ERR]")).Count}";
                SubsystemLog.Info("GMMapTab", summary);
                if (batchAuditLogText != null) batchAuditLogText.text = summary;
                if (_mapManager != null)
                {
                    _mapManager.OnMapLoaded -= OnBatchMapLoaded;
                    _mapManager.OnMapError -= OnBatchMapError;
                }
                return;
            }

            var entry = _displayed[_batchIndex];
            SelectMap(entry.mapId);
            _mapManager?.LoadMap(entry.mapId);
        }

        private void OnBatchMapLoaded(int mapId)
        {
            _batchLog.Add($"[OK ] Map {mapId}: loaded");
            if (batchAuditLogText != null)
                batchAuditLogText.text = $"Auditing {_batchIndex + 1}/{_displayed.Count}: map {mapId} OK";
            _batchIndex++;
            AdvanceBatchAudit();
        }

        private void OnBatchMapError(string error)
        {
            int failId = _displayed.Count > _batchIndex ? _displayed[_batchIndex].mapId : -1;
            _batchLog.Add($"[ERR] Map {failId}: {error}");
            if (batchAuditLogText != null)
                batchAuditLogText.text = $"Auditing {_batchIndex + 1}/{_displayed.Count}: map {failId} ERROR";
            _batchIndex++;
            AdvanceBatchAudit();
        }

        public void UnloadCurrentMap()
        {
            var mgr = SandboxManager.Instance?.MapManager;
            if (mgr == null) return;

            mgr.UnloadCurrentMap();
            UpdateStatus();
        }

        // M0.10 AC#4: Show error in GM Panel
        private void ShowError(string error)
        {
            if (errorText != null)
            {
                errorText.text = $"Error: {error}";
                errorText.gameObject.SetActive(true);
            }
            SubsystemLog.Warn("GMMapTab", $"Map error displayed: {error}");
        }

        private void ClearError()
        {
            if (errorText != null)
            {
                errorText.text = "";
                errorText.gameObject.SetActive(false);
            }
        }

        private void RebuildListUI()
        {
            if (listContent == null) return;

            for (int i = listContent.childCount - 1; i >= 0; i--)
                Destroy(listContent.GetChild(i).gameObject);

            var activeId = SandboxManager.Instance?.MapManager?.ActiveMapId ?? -1;

            for (int idx = 0; idx < _displayed.Count; idx++)
            {
                var entry = _displayed[idx];
                var row = new GameObject($"Map_{entry.mapId}");
                row.transform.SetParent(listContent, false);
                var rt = row.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0, 30);
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(0, -idx * 32f);

                // M0.10 AC#1: status badge color
                var img = row.AddComponent<Image>();
                img.color = entry.mapId == _selectedMapId
                    ? new Color(0.3f, 0.6f, 1f, 0.5f)
                    : entry.mapId == activeId
                        ? new Color(0.2f, 0.8f, 0.3f, 0.4f)
                        : entry.conversionStatus == ConversionStatus.Failed
                            ? new Color(0.6f, 0.1f, 0.1f, 0.4f)
                            : new Color(0.2f, 0.2f, 0.25f, 0.5f);

                var btn = row.AddComponent<Button>();
                var capturedId = entry.mapId;
                btn.onClick.AddListener(() => SelectMap(capturedId));

                // Double-click handler via double-tap timing tracked in SelectMap
                var txt = new GameObject("Label");
                txt.transform.SetParent(rt, false);
                var tr = txt.AddComponent<RectTransform>();
                tr.anchorMin = Vector2.zero;
                tr.anchorMax = Vector2.one;
                tr.sizeDelta = Vector2.zero;
                var t = txt.AddComponent<Text>();

                // M0.10 AC#1: status badge prefix
                string prefix = entry.isIndoor ? "[IN] " : "[OUT] ";
                string statusBadge = entry.conversionStatus switch
                {
                    ConversionStatus.Complete => "✓ ",
                    ConversionStatus.Failed   => "✗ ",
                    ConversionStatus.Partial  => "~ ",
                    _                         => "· ",
                };
                string activeMarker = entry.mapId == activeId ? " ◄" : "";
                t.text = $"{statusBadge}{prefix}{entry.mapId}: {entry.displayNameNormalized}{activeMarker}";
                t.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 12;
                t.color = Color.white;
                t.alignment = TextAnchor.MiddleLeft;
            }
        }

        private void UpdateStatus()
        {
            if (statusText == null) return;
            var mgr = SandboxManager.Instance?.MapManager;
            if (mgr == null) return;

            if (mgr.ActiveMapId >= 0)
            {
                var name = mgr.ActiveMap?.catalogEntry?.displayNameNormalized ?? "?";
                statusText.text = $"Active: {name} (id={mgr.ActiveMapId})";

                // Discovery report summary if available
                if (mgr.DiscoveryReport != null)
                {
                    var r = mgr.DiscoveryReport;
                    statusText.text += $"\nCatalog: {r.available}/{r.totalDiscovered} available";
                }
            }
            else
            {
                statusText.text = "No map loaded";
                if (mgr.DiscoveryReport != null)
                {
                    var r = mgr.DiscoveryReport;
                    statusText.text += $"\n{r.totalDiscovered} maps discovered ({r.available} available, {r.missing} missing)";
                }
            }
        }

        private void UpdateMapInfo()
        {
            if (mapInfoText == null) return;
            var mgr = SandboxManager.Instance?.MapManager;
            if (mgr == null || _selectedMapId < 0) return;

            var entry = mgr.Catalog.ContainsKey(_selectedMapId)
                ? mgr.Catalog[_selectedMapId]
                : null;
            if (entry == null)
            {
                mapInfoText.text = $"Map {_selectedMapId}: not found";
                return;
            }

            mapInfoText.text = $"ID: {entry.mapId}\n" +
                               $"Name: {entry.displayNameNormalized}\n" +
                               $"Indoor: {entry.isIndoor}\n" +
                               $"Status: {entry.conversionStatus}";
            RebuildListUI();
        }
    }
}
