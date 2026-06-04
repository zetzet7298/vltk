// -----------------------------------------------------------------------------
// VLTK Mobile — Mobile UI Screens (Quest Panel, Inventory Panel, Map Selector)
// UI panels for quest tracking, inventory management, and map switching.
// Uses UnityEngine.UI (legacy) for broad mobile compatibility.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// In-game quest tracker panel. Shows active quests and their progress.
    /// Toggled via HUD button. All text in Vietnamese.
    /// </summary>
    public class QuestTrackerPanel : MonoBehaviour
    {
        private QuestService _questService;
        private GameObject _panelRoot;
        private Text _titleText;
        private Transform _questListRoot;
        private Font _font;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        public void Initialize(QuestService questService)
        {
            _questService = questService;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panelRoot != null)
                _panelRoot.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        public void Refresh()
        {
            if (_questService == null || _questListRoot == null) return;

            // Clear existing entries
            for (int i = _questListRoot.childCount - 1; i >= 0; i--)
                Destroy(_questListRoot.GetChild(i).gameObject);

            var active = _questService.ActiveQuests;
            if (active.Count == 0)
            {
                AddLabel(_questListRoot, "  Không có nhiệm vụ đang thực hiện", 24, new Color(0.7f, 0.7f, 0.7f));
                return;
            }

            foreach (var kv in active)
            {
                var qi = kv.Value;
                var def = _questService.GetDefinition(qi.questId);
                if (def == null) continue;

                string stateLabel = qi.state == QuestState.Complete ? "✓ " :
                                    qi.state == QuestState.Active ? "● " : "○ ";
                string questLabel = $"{stateLabel}{def.nameVi}";

                var entryGo = AddLabel(_questListRoot, questLabel, 26, Color.white);
                AddObjectiveEntries(entryGo.transform, qi, def);
            }
        }

        private GameObject AddLabel(Transform parent, string text, int fontSize, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(0f, fontSize + 12);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = _font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            return go;
        }

        private void AddObjectiveEntries(Transform parent, QuestInstance qi, QuestDefinition def)
        {
            foreach (var obj in qi.objectives)
            {
                string color = obj.IsComplete ? "#88ff88" : "#ffdd88";
                string objText = $"    {obj.descriptionVi}";
                AddLabel(parent, objText, 22, obj.IsComplete ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.85f, 0.5f));
            }
        }

        private void BuildUI()
        {
            _panelRoot = new GameObject("QuestTrackerPanel");
            _panelRoot.transform.SetParent(transform, false);
            _panelRoot.SetActive(false);

            var canvasRt = _panelRoot.AddComponent<RectTransform>();
            canvasRt.anchorMin = new Vector2(0.15f, 0.2f);
            canvasRt.anchorMax = new Vector2(0.85f, 0.85f);

            // Background
            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.12f, 0.94f);

            // Title bar
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panelRoot.transform, false);
            var titleRt = titleBar.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.92f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.15f, 0.25f, 0.45f, 0.95f);

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar.transform, false);
            var textRt = titleTextGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            var titleTxt = titleTextGo.AddComponent<Text>();
            titleTxt.text = "Nhiệm Vụ";
            titleTxt.font = _font;
            titleTxt.fontSize = 32;
            titleTxt.color = new Color(1f, 0.95f, 0.8f);
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Close button
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBar.transform, false);
            var closeRt = closeGo.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.9f, 0f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() => Toggle());

            var closeTextGo = new GameObject("CloseText");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTxtRt = closeTextGo.AddComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero; closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = Vector2.zero; closeTxtRt.offsetMax = Vector2.zero;
            var closeTxt = closeTextGo.AddComponent<Text>();
            closeTxt.text = "✕";
            closeTxt.font = _font;
            closeTxt.fontSize = 28;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;

            // Scrollable quest list
            var scrollGo = new GameObject("QuestList");
            scrollGo.transform.SetParent(_panelRoot.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.03f, 0.02f);
            scrollRt.anchorMax = new Vector2(0.97f, 0.91f);

            // Content with vertical layout
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            _questListRoot = contentGo.transform;
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0f, 1f);
            var vLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.spacing = 8f;
            vLayout.padding = new RectOffset(10, 10, 10, 10);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    /// <summary>
    /// Inventory panel for viewing/using items and equipment.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        private ItemDatabase _itemDb;
        private InventoryService _inventory;
        private GameObject _panelRoot;
        private Transform _itemListRoot;
        private Text _statsText;
        private Font _font;
        private bool _isOpen;

        public bool IsOpen => _isOpen;

        public void Initialize(ItemDatabase itemDb, InventoryService inventory)
        {
            _itemDb = itemDb;
            _inventory = inventory;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panelRoot != null)
                _panelRoot.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        public void Refresh()
        {
            if (_inventory == null || _itemListRoot == null) return;

            for (int i = _itemListRoot.childCount - 1; i >= 0; i--)
                Destroy(_itemListRoot.GetChild(i).gameObject);

            var items = _inventory.Inventory;
            if (items.Count == 0)
            {
                AddLabel(_itemListRoot, "  Túi đồ trống", 24, new Color(0.7f, 0.7f, 0.7f));
                return;
            }

            foreach (var entry in items)
            {
                string itemText = $"  {entry.item.DisplayName} x{entry.count}";
                var label = AddLabel(_itemListRoot, itemText, 24, Color.white);
            }

            // Update stats summary
            if (_statsText != null)
            {
                var preview = _inventory.StatPreview();
                string stats = "Tổng chỉ số: ";
                foreach (var kv in preview)
                    stats += $"[{kv.Key}]+{kv.Value} ";
                _statsText.text = stats;
            }
        }

        private GameObject AddLabel(Transform parent, string text, int fontSize, Color color)
        {
            var go = new GameObject("Item");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, fontSize + 10);
            var txt = go.AddComponent<Text>();
            txt.text = text;
            txt.font = _font;
            txt.fontSize = fontSize;
            txt.color = color;
            txt.alignment = TextAnchor.MiddleLeft;
            return go;
        }

        private void BuildUI()
        {
            _panelRoot = new GameObject("InventoryPanel");
            _panelRoot.transform.SetParent(transform, false);
            _panelRoot.SetActive(false);

            var canvasRt = _panelRoot.AddComponent<RectTransform>();
            canvasRt.anchorMin = new Vector2(0.1f, 0.15f);
            canvasRt.anchorMax = new Vector2(0.9f, 0.9f);

            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.06f, 0.04f, 0.94f);

            // Title
            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panelRoot.transform, false);
            var titleRt = titleBar.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.92f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.3f, 0.2f, 0.1f, 0.95f);

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar.transform, false);
            var textRt = titleTextGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            var titleTxt = titleTextGo.AddComponent<Text>();
            titleTxt.text = "Túi Đồ";
            titleTxt.font = _font;
            titleTxt.fontSize = 32;
            titleTxt.color = new Color(1f, 0.9f, 0.7f);
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Close button
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBar.transform, false);
            var closeRt = closeGo.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.9f, 0f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() => Toggle());

            var closeTextGo = new GameObject("CloseText");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTxtRt = closeTextGo.AddComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero; closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = Vector2.zero; closeTxtRt.offsetMax = Vector2.zero;
            var closeTxt = closeTextGo.AddComponent<Text>();
            closeTxt.text = "✕";
            closeTxt.font = _font;
            closeTxt.fontSize = 28;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;

            // Stats bar
            var statsGo = new GameObject("Stats");
            statsGo.transform.SetParent(_panelRoot.transform, false);
            var statsRt = statsGo.AddComponent<RectTransform>();
            statsRt.anchorMin = new Vector2(0.02f, 0.85f);
            statsRt.anchorMax = new Vector2(0.98f, 0.92f);
            _statsText = statsGo.AddComponent<Text>();
            _statsText.font = _font;
            _statsText.fontSize = 20;
            _statsText.color = new Color(0.8f, 0.9f, 1f);
            _statsText.alignment = TextAnchor.MiddleLeft;

            // Item list
            var scrollGo = new GameObject("ItemList");
            scrollGo.transform.SetParent(_panelRoot.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.02f, 0.02f);
            scrollRt.anchorMax = new Vector2(0.98f, 0.85f);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            _itemListRoot = contentGo.transform;
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0f, 1f);
            var vLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.spacing = 4f;
            vLayout.padding = new RectOffset(8, 8, 8, 8);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    /// <summary>
    /// Map selection panel. Shows available maps from MapPortManifest,
    /// allows switching between maps.
    /// </summary>
    public class MapSelectPanel : MonoBehaviour
    {
        private MapManager _mapManager;
        private GameObject _panelRoot;
        private Transform _mapListRoot;
        private Font _font;
        private bool _isOpen;
        private Action<int> _onMapSelected;

        public bool IsOpen => _isOpen;

        public void Initialize(MapManager mapManager, Action<int> onMapSelected)
        {
            _mapManager = mapManager;
            _onMapSelected = onMapSelected;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildUI();
        }

        public void Toggle()
        {
            _isOpen = !_isOpen;
            if (_panelRoot != null)
                _panelRoot.SetActive(_isOpen);
            if (_isOpen) Refresh();
        }

        public void Refresh()
        {
            if (_mapListRoot == null) return;

            for (int i = _mapListRoot.childCount - 1; i >= 0; i--)
                Destroy(_mapListRoot.GetChild(i).gameObject);

            foreach (var kv in MapPortManifest.Entries)
            {
                var entry = kv.Value;
                var btnGo = new GameObject($"Map_{entry.mapId}");
                btnGo.transform.SetParent(_mapListRoot, false);
                var rt = btnGo.AddComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 48f);

                var img = btnGo.AddComponent<Image>();
                bool isActive = _mapManager?.ActiveMapId == entry.mapId;
                img.color = isActive
                    ? new Color(0.2f, 0.5f, 0.3f, 0.9f)
                    : new Color(0.15f, 0.15f, 0.2f, 0.9f);

                var btn = btnGo.AddComponent<Button>();
                btn.targetGraphic = img;
                int mapId = entry.mapId;
                btn.onClick.AddListener(() => SelectMap(mapId));

                var txtGo = new GameObject("Label");
                txtGo.transform.SetParent(btnGo.transform, false);
                var txtRt = txtGo.AddComponent<RectTransform>();
                txtRt.anchorMin = Vector2.zero;
                txtRt.anchorMax = Vector2.one;
                txtRt.offsetMin = Vector2.zero;
                txtRt.offsetMax = Vector2.zero;
                var txt = txtGo.AddComponent<Text>();
                txt.text = $"{entry.nameVi} ({entry.mapId}){(isActive ? " ◀" : "")}";
                txt.font = _font;
                txt.fontSize = 24;
                txt.color = isActive ? new Color(0.8f, 1f, 0.8f) : Color.white;
                txt.alignment = TextAnchor.MiddleLeft;
            }
        }

        private void SelectMap(int mapId)
        {
            _onMapSelected?.Invoke(mapId);
            Toggle(); // close after selection
        }

        private void BuildUI()
        {
            _panelRoot = new GameObject("MapSelectPanel");
            _panelRoot.transform.SetParent(transform, false);
            _panelRoot.SetActive(false);

            var canvasRt = _panelRoot.AddComponent<RectTransform>();
            canvasRt.anchorMin = new Vector2(0.2f, 0.15f);
            canvasRt.anchorMax = new Vector2(0.8f, 0.9f);

            var bg = _panelRoot.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

            var titleBar = new GameObject("TitleBar");
            titleBar.transform.SetParent(_panelRoot.transform, false);
            var titleRt = titleBar.AddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.92f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            var titleBg = titleBar.AddComponent<Image>();
            titleBg.color = new Color(0.15f, 0.2f, 0.4f, 0.95f);

            var titleTextGo = new GameObject("TitleText");
            titleTextGo.transform.SetParent(titleBar.transform, false);
            var textRt = titleTextGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            var titleTxt = titleTextGo.AddComponent<Text>();
            titleTxt.text = "Chọn Bản Đồ";
            titleTxt.font = _font;
            titleTxt.fontSize = 32;
            titleTxt.color = new Color(0.9f, 0.95f, 1f);
            titleTxt.alignment = TextAnchor.MiddleCenter;

            // Close
            var closeGo = new GameObject("CloseBtn");
            closeGo.transform.SetParent(titleBar.transform, false);
            var closeRt = closeGo.AddComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.9f, 0f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            var closeImg = closeGo.AddComponent<Image>();
            closeImg.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
            var closeBtn = closeGo.AddComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            closeBtn.onClick.AddListener(() => Toggle());

            var closeTextGo = new GameObject("CloseText");
            closeTextGo.transform.SetParent(closeGo.transform, false);
            var closeTxtRt = closeTextGo.AddComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero; closeTxtRt.anchorMax = Vector2.one;
            closeTxtRt.offsetMin = Vector2.zero; closeTxtRt.offsetMax = Vector2.zero;
            var closeTxt = closeTextGo.AddComponent<Text>();
            closeTxt.text = "✕";
            closeTxt.font = _font;
            closeTxt.fontSize = 28;
            closeTxt.color = Color.white;
            closeTxt.alignment = TextAnchor.MiddleCenter;

            // Map list
            var scrollGo = new GameObject("MapList");
            scrollGo.transform.SetParent(_panelRoot.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.03f, 0.02f);
            scrollRt.anchorMax = new Vector2(0.97f, 0.91f);

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            _mapListRoot = contentGo.transform;
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = Vector2.zero;
            contentRt.anchorMax = Vector2.one;
            contentRt.pivot = new Vector2(0f, 1f);
            var vLayout = contentGo.AddComponent<VerticalLayoutGroup>();
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.spacing = 4f;
            vLayout.padding = new RectOffset(8, 8, 8, 8);
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }
}
