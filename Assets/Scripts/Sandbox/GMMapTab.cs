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

        // Infinite scroll and double-click state
        private ScrollRect _scrollRect;
        private float _lastScrollTime;
        private int _visibleCount = 30;
        private const int PageSize = 30;
        private float _lastSelectTime;
        private int _lastSelectedMapIdForDoubleClick = -1;
        private int _customFilterState = 0; // 0: All, 1: Complete, 2: Failed, 3: Partial
        private Text _filterBtnText;

        private void Start()
        {
            _mapManager = SandboxManager.Instance?.MapManager;

            // Setup ScrollView runtime for MapList to make it scrollable and prevent it from overflowing the panel
            if (listContent != null && listContent.name == "MapList")
            {
                var parentRt = listContent.GetComponent<RectTransform>();
                if (parentRt != null)
                {
                    // Adjust anchor and offsets to stretch properly and remain BELOW TopBar (which is 35px height)
                    // Bottom anchor is set to 0.3f to stay above the MapInfo area.
                    parentRt.anchorMin = new Vector2(0f, 0.3f);
                    parentRt.anchorMax = new Vector2(1f, 1f);
                    parentRt.offsetMin = new Vector2(5f, 5f);
                    parentRt.offsetMax = new Vector2(-5f, -40f); // -40f shifts it below the 35px TopBar
                    // Do NOT set parentRt.sizeDelta.y here as it overrides offsetMax and offsetMin.
                }

                var scrollRect = listContent.gameObject.AddComponent<ScrollRect>();
                
                // Use robust standard Mask + transparent Image for reliable runtime clipping
                var maskImg = listContent.gameObject.AddComponent<Image>();
                maskImg.color = new Color(0f, 0f, 0f, 0.01f);
                var mask = listContent.gameObject.AddComponent<Mask>();
                mask.showMaskGraphic = false;

                var contentGo = new GameObject("Content");
                contentGo.transform.SetParent(listContent, false);
                var contentRt = contentGo.AddComponent<RectTransform>();
                contentRt.anchorMin = new Vector2(0f, 1f);
                contentRt.anchorMax = new Vector2(1f, 1f);
                contentRt.pivot = new Vector2(0.5f, 1f);
                contentRt.anchoredPosition = Vector2.zero;
                contentRt.sizeDelta = new Vector2(0f, 0f);

                scrollRect.content = contentRt;
                scrollRect.viewport = parentRt; // Wire viewport reference!
                scrollRect.horizontal = false;
                scrollRect.vertical = true;
                scrollRect.movementType = ScrollRect.MovementType.Clamped;

                // Redirect listContent to the new Content so items are added there
                listContent = contentRt;
                _scrollRect = scrollRect;
            }

            if (searchButton != null)
                searchButton.onClick.AddListener(DoSearch);

            // Dynamically resolve load/unload buttons if they are null in the inspector
            if (loadButton == null)
            {
                var btnTrans = transform.Find("TopBar/LoadBtn");
                if (btnTrans != null) loadButton = btnTrans.GetComponent<Button>();
            }
            if (unloadButton == null)
            {
                var btnTrans = transform.Find("TopBar/UnloadBtn");
                if (btnTrans != null) unloadButton = btnTrans.GetComponent<Button>();
            }

            // Re-align and size Load / Unload buttons on TopBar to make space for the Filter button
            if (loadButton != null)
            {
                loadButton.onClick.RemoveAllListeners();
                loadButton.onClick.AddListener(LoadSelectedMap);
                var loadRt = loadButton.GetComponent<RectTransform>();
                if (loadRt != null)
                {
                    loadRt.anchorMin = new Vector2(0.65f, 0f);
                    loadRt.anchorMax = new Vector2(0.82f, 1f);
                    loadRt.offsetMin = new Vector2(2f, 2f);
                    loadRt.offsetMax = new Vector2(-2f, -2f);
                }
                var loadTxt = loadButton.GetComponentInChildren<Text>();
                if (loadTxt != null)
                {
                    loadTxt.text = "Tải Map";
                    loadTxt.fontSize = 12;
                    loadTxt.alignment = TextAnchor.MiddleCenter;
                }
            }

            if (unloadButton != null)
            {
                unloadButton.onClick.RemoveAllListeners();
                unloadButton.onClick.AddListener(UnloadCurrentMap);
                var unloadRt = unloadButton.GetComponent<RectTransform>();
                if (unloadRt != null)
                {
                    unloadRt.anchorMin = new Vector2(0.82f, 0f);
                    unloadRt.anchorMax = new Vector2(1.00f, 1f);
                    unloadRt.offsetMin = new Vector2(2f, 2f);
                    unloadRt.offsetMax = new Vector2(-2f, -2f);
                }
                var unloadTxt = unloadButton.GetComponentInChildren<Text>();
                if (unloadTxt != null)
                {
                    unloadTxt.text = "Rời Map";
                    unloadTxt.fontSize = 12;
                    unloadTxt.alignment = TextAnchor.MiddleCenter;
                }
            }

            if (searchInput != null)
            {
                // Align text nicely to MiddleLeft to prevent it from offseting to the top
                if (searchInput.textComponent != null)
                {
                    searchInput.textComponent.alignment = TextAnchor.MiddleLeft;
                    searchInput.textComponent.fontSize = 14;
                }

                // SearchInput takes 0% to 45% width of TopBar
                var searchRt = searchInput.GetComponent<RectTransform>();
                if (searchRt != null)
                {
                    searchRt.anchorMin = new Vector2(0f, 0f);
                    searchRt.anchorMax = new Vector2(0.45f, 1f);
                    searchRt.offsetMin = new Vector2(2f, 2f);
                    searchRt.offsetMax = new Vector2(-2f, -2f);
                }

                // Add search placeholder dynamically since it's missing in the prefab
                if (searchInput.placeholder == null)
                {
                    var placeholderGo = new GameObject("Placeholder");
                    placeholderGo.transform.SetParent(searchInput.transform, false);
                    
                    var placeholderText = placeholderGo.AddComponent<Text>();
                    placeholderText.text = "Tìm kiếm bản đồ...";
                    if (searchInput.textComponent != null)
                    {
                        placeholderText.font = searchInput.textComponent.font;
                        placeholderText.fontSize = 14;
                        placeholderText.alignment = TextAnchor.MiddleLeft;
                    }
                    placeholderText.fontStyle = FontStyle.Italic;
                    placeholderText.color = new Color(0.6f, 0.6f, 0.6f, 0.6f);
                    
                    var pRt = placeholderGo.GetComponent<RectTransform>();
                    if (pRt != null)
                    {
                        pRt.anchorMin = Vector2.zero;
                        pRt.anchorMax = Vector2.one;
                        pRt.offsetMin = new Vector2(5f, 0f);
                        pRt.offsetMax = new Vector2(-5f, 0f);
                    }
                    searchInput.placeholder = placeholderText;
                }

                // Create StatusFilterButton dynamically at runtime (takes 45% to 65% width of TopBar)
                var topBarTrans = searchInput.transform.parent;
                if (topBarTrans != null)
                {
                    var filterBtnTrans = topBarTrans.Find("StatusFilterButton");
                    GameObject filterBtnGo;
                    if (filterBtnTrans != null)
                    {
                        filterBtnGo = filterBtnTrans.gameObject;
                    }
                    else
                    {
                        filterBtnGo = new GameObject("StatusFilterButton");
                        filterBtnGo.transform.SetParent(topBarTrans, false);
                    }

                    var filterRt = filterBtnGo.GetComponent<RectTransform>();
                    if (filterRt == null) filterRt = filterBtnGo.AddComponent<RectTransform>();
                    filterRt.anchorMin = new Vector2(0.45f, 0f);
                    filterRt.anchorMax = new Vector2(0.65f, 1f);
                    filterRt.offsetMin = new Vector2(2f, 2f);
                    filterRt.offsetMax = new Vector2(-2f, -2f);

                    var filterImg = filterBtnGo.GetComponent<Image>();
                    if (filterImg == null) filterImg = filterBtnGo.AddComponent<Image>();
                    filterImg.color = new Color(0.2f, 0.2f, 0.25f, 1f); // Dark blue-grey/slate color
                    
                    var filterBtn = filterBtnGo.GetComponent<Button>();
                    if (filterBtn == null) filterBtn = filterBtnGo.AddComponent<Button>();
                    filterBtn.targetGraphic = filterImg;
                    filterBtn.onClick.RemoveAllListeners();
                    filterBtn.onClick.AddListener(CycleFilterState);

                    Transform lblTrans = filterBtnGo.transform.Find("Label");
                    GameObject lblGo;
                    if (lblTrans != null)
                    {
                        lblGo = lblTrans.gameObject;
                    }
                    else
                    {
                        lblGo = new GameObject("Label");
                        lblGo.transform.SetParent(filterBtnGo.transform, false);
                    }

                    var lrt = lblGo.GetComponent<RectTransform>();
                    if (lrt == null) lrt = lblGo.AddComponent<RectTransform>();
                    lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
                    lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

                    _filterBtnText = lblGo.GetComponent<Text>();
                    if (_filterBtnText == null) _filterBtnText = lblGo.AddComponent<Text>();
                    _filterBtnText.text = "Lọc: Tất cả";
                    _filterBtnText.alignment = TextAnchor.MiddleCenter;
                    _filterBtnText.fontSize = 11;
                    _filterBtnText.color = Color.white;
                    if (searchInput.textComponent != null)
                        _filterBtnText.font = searchInput.textComponent.font;
                }

                searchInput.onEndEdit.AddListener(_ => DoSearch());
                // Auto-filter: search dynamically as the user types
                searchInput.onValueChanged.AddListener(_ => DoSearch());
            }

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

            _scrollRect = listContent != null ? listContent.GetComponentInParent<ScrollRect>() : null;
            if (_scrollRect != null)
            {
                _scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
            }

            RefreshList();
        }

        private void OnScrollValueChanged(Vector2 scrollPos)
        {
            if (scrollPos.y < 0.1f && _visibleCount < _displayed.Count)
            {
                if (Time.time - _lastScrollTime > 0.2f)
                {
                    _lastScrollTime = Time.time;
                    _visibleCount += PageSize;
                    RebuildListUI();
                    SubsystemLog.Info("GMMapTab", $"Infinite scroll: loaded more maps (visible: {_visibleCount}/{_displayed.Count})");
                }
            }
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

        private void CycleFilterState()
        {
            _customFilterState = (_customFilterState + 1) % 4;
            if (_filterBtnText != null)
            {
                _filterBtnText.text = _customFilterState switch
                {
                    1 => "Lọc: Hoàn thành",
                    2 => "Lọc: Lỗi",
                    3 => "Lọc: Đang làm",
                    _ => "Lọc: Tất cả"
                };
            }
            RefreshList();
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
            else
            {
                // Fallback to our custom filter button state
                statusFilter = _customFilterState switch
                {
                    1 => ConversionStatus.Complete,
                    2 => ConversionStatus.Failed,
                    3 => ConversionStatus.Partial,
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
            _visibleCount = PageSize; // reset visible count on refresh
            RebuildListUI();
            UpdateStatus();
        }

        public void DoSearch()
        {
            RefreshList();
        }

        public void SelectMap(int mapId)
        {
            if (_selectedMapId == mapId && _lastSelectedMapIdForDoubleClick == mapId && Time.time - _lastSelectTime < 0.35f)
            {
                // Double click detected -> Teleport immediately!
                _selectedMapId = mapId;
                UpdateMapInfo();
                LoadSelectedMap();
                SubsystemLog.Info("GMMapTab", $"Double-click teleport to map {mapId}");
                return;
            }

            _selectedMapId = mapId;
            _lastSelectedMapIdForDoubleClick = mapId;
            _lastSelectTime = Time.time;
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
            int limit = Mathf.Min(_visibleCount, _displayed.Count);

            var contentRt = listContent.GetComponent<RectTransform>();
            if (contentRt != null)
            {
                contentRt.sizeDelta = new Vector2(contentRt.sizeDelta.x, limit * 32f);
            }

            for (int idx = 0; idx < limit; idx++)
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
                tr.offsetMin = new Vector2(10f, 0f); // 10px indentation padding
                tr.offsetMax = new Vector2(-10f, 0f); // 10px indentation padding
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
