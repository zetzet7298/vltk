// -----------------------------------------------------------------------------
// VLTK Mobile — Per-Map Minimap Panel
// Visual minimap overlay for the active map with player dot, enemy dots,
// click-to-move, and map label. Works for any loaded map.
// PC source: minimap UI from Ui3 INI files.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Per-map minimap panel rendered in screen corner.
    /// Shows player position dot, enemy dots, map name, and supports click-to-move.
    /// Uses MapDefinition source bounds for coordinate normalization.
    /// </summary>
    public class MinimapPanel : MonoBehaviour
    {
        private MapManager _mapManager;
        private SandboxPlayerController _player;
        private MapEnemySpawnRuntime _enemyRuntime;
        private Image _mapImage;
        private Image _playerDot;
        private Text _mapNameText;
        private GameObject _dotRoot;
        private Font _font;
        private readonly List<GameObject> _enemyDots = new();

        public void Initialize(MapManager mapManager, SandboxPlayerController player, MapEnemySpawnRuntime enemyRuntime)
        {
            _mapManager = mapManager;
            _player = player;
            _enemyRuntime = enemyRuntime;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Font.CreateDynamicFontFromOSFont("Arial", 14);
            BuildUI();
        }

        private void LateUpdate()
        {
            if (_playerDot == null || _player == null) return;
            UpdatePlayerDot();
            UpdateEnemyDots();
            UpdateMapName();
        }

        private void UpdatePlayerDot()
        {
            var mapDef = _mapManager?.ActiveMap;
            if (mapDef == null) return;

            var normalized = WorldToMinimap(mapDef, _player.transform.position);
            var rt = _playerDot.rectTransform;
            rt.anchorMin = new Vector2(normalized.x, normalized.y);
            rt.anchorMax = new Vector2(normalized.x, normalized.y);
            rt.anchoredPosition = Vector2.zero;
        }

        private void UpdateEnemyDots()
        {
            // Clear old dots
            foreach (var dot in _enemyDots)
            {
                if (dot != null) Destroy(dot);
            }
            _enemyDots.Clear();

            if (_enemyRuntime == null || _mapManager?.ActiveMap == null) return;

            var enemies = _enemyRuntime.GetActiveEnemies();
            var mapDef = _mapManager.ActiveMap;

            int maxDots = Mathf.Min(enemies.Count, 50); // limit for perf
            for (int i = 0; i < maxDots; i++)
            {
                var enemy = enemies[i];
                if (!enemy.alive) continue;

                var normalized = WorldToMinimap(mapDef, enemy.position);
                var dotGo = new GameObject($"EnemyDot_{i}");
                dotGo.transform.SetParent(_dotRoot.transform, false);
                var dotRt = dotGo.AddComponent<RectTransform>();
                dotRt.anchorMin = new Vector2(normalized.x, normalized.y);
                dotRt.anchorMax = new Vector2(normalized.x, normalized.y);
                dotRt.sizeDelta = new Vector2(4f, 4f);
                dotRt.anchoredPosition = Vector2.zero;
                var dotImg = dotGo.AddComponent<Image>();
                dotImg.color = new Color(1f, 0.3f, 0.2f, 0.8f);
                _enemyDots.Add(dotGo);
            }
        }

        private void UpdateMapName()
        {
            if (_mapNameText == null || _mapManager == null) return;
            int mapId = _mapManager.ActiveMapId;
            string name = MapPortManifest.GetNameVi(mapId);
            if (_mapNameText.text != name)
                _mapNameText.text = name;
        }

        private Vector2 WorldToMinimap(MapDefinition mapDef, Vector2 worldPos)
        {
            return MinimapCoordinateMapper.WorldToMinimapNormalized(mapDef, worldPos);
        }

        private void BuildUI()
        {
            // Minimap container — top-right corner
            var containerGo = new GameObject("MinimapPanel");
            containerGo.transform.SetParent(transform, false);

            var containerRt = containerGo.AddComponent<RectTransform>();
            containerRt.anchorMin = new Vector2(0.73f, 0.75f);
            containerRt.anchorMax = new Vector2(1f, 1f);
            containerRt.offsetMin = new Vector2(-8f, -8f);
            containerRt.offsetMax = new Vector2(-8f, 0f);

            // Border/background
            var borderImg = containerGo.AddComponent<Image>();
            borderImg.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);

            // Map image (placeholder colored rect)
            var mapGo = new GameObject("MapImage");
            mapGo.transform.SetParent(containerGo.transform, false);
            var mapRt = mapGo.AddComponent<RectTransform>();
            mapRt.anchorMin = new Vector2(0.04f, 0.08f);
            mapRt.anchorMax = new Vector2(0.96f, 0.92f);
            mapRt.offsetMin = Vector2.zero;
            mapRt.offsetMax = Vector2.zero;
            _mapImage = mapGo.AddComponent<Image>();
            _mapImage.color = new Color(0.15f, 0.18f, 0.12f, 0.9f);

            // Grid overlay for terrain feel
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(mapGo.transform, false);
            var gridRt = gridGo.AddComponent<RectTransform>();
            gridRt.anchorMin = Vector2.zero;
            gridRt.anchorMax = Vector2.one;
            gridRt.offsetMin = Vector2.zero;
            gridRt.offsetMax = Vector2.zero;
            var gridImg = gridGo.AddComponent<Image>();
            gridImg.color = new Color(0.2f, 0.25f, 0.15f, 0.3f);

            // Dot root (for enemy dots)
            _dotRoot = new GameObject("Dots");
            _dotRoot.transform.SetParent(mapGo.transform, false);
            var dotRt2 = _dotRoot.AddComponent<RectTransform>();
            dotRt2.anchorMin = Vector2.zero;
            dotRt2.anchorMax = Vector2.one;
            dotRt2.offsetMin = Vector2.zero;
            dotRt2.offsetMax = Vector2.zero;

            // Player dot
            var playerDotGo = new GameObject("PlayerDot");
            playerDotGo.transform.SetParent(mapGo.transform, false);
            var pdRt = playerDotGo.AddComponent<RectTransform>();
            pdRt.sizeDelta = new Vector2(10f, 10f);
            _playerDot = playerDotGo.AddComponent<Image>();
            _playerDot.color = new Color(0.2f, 0.9f, 1f, 1f);

            // Map name label — must use offsetMin/Max=0 so it stays inside container
            var nameGo = new GameObject("MapName");
            nameGo.transform.SetParent(containerGo.transform, false);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.82f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.offsetMin = new Vector2(4f, 0f);
            nameRt.offsetMax = new Vector2(-4f, 0f);
            _mapNameText = nameGo.AddComponent<Text>();
            _mapNameText.font = _font;
            _mapNameText.fontSize = 12;
            _mapNameText.color = new Color(1f, 0.95f, 0.8f);
            _mapNameText.alignment = TextAnchor.MiddleCenter;
            _mapNameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _mapNameText.verticalOverflow = VerticalWrapMode.Truncate;

            // Click-to-move on minimap
            var btnGo = new GameObject("ClickArea");
            btnGo.transform.SetParent(mapGo.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = Color.clear;
            btnImg.raycastTarget = true;
            var clickForwarder = btnGo.AddComponent<MinimapClickForwarder>();
            clickForwarder.Initialize(this, btnRt);
        }

        private void OnMinimapClick(PointerEventData eventData, RectTransform minimapRect)
        {
            if (_mapManager?.ActiveMap == null || _player == null || minimapRect == null || eventData == null)
                return;

            var eventCamera = eventData.pressEventCamera ?? eventData.enterEventCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    minimapRect,
                    eventData.position,
                    eventCamera,
                    out var localPointer))
                return;

            var activeMap = _mapManager.ActiveMap;
            var sourceRect = activeMap.sourceBoundsRect;
            if (sourceRect == null || sourceRect.width <= 0f || sourceRect.height <= 0f)
                return;

            var target = MinimapCoordinateMapper.MinimapLocalToWorld(activeMap, localPointer, minimapRect.rect);
            _player.MoveTo(target);
            SubsystemLog.Info("Minimap", $"Click-to-move → target {target}");
        }

        private sealed class MinimapClickForwarder : MonoBehaviour, IPointerClickHandler
        {
            private MinimapPanel _panel;
            private RectTransform _minimapRect;

            public void Initialize(MinimapPanel panel, RectTransform minimapRect)
            {
                _panel = panel;
                _minimapRect = minimapRect;
            }

            public void OnPointerClick(PointerEventData eventData)
            {
                _panel?.OnMinimapClick(eventData, _minimapRect);
            }
        }
    }
}
