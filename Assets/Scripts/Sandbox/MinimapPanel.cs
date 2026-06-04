// -----------------------------------------------------------------------------
// VLTK Mobile — Per-Map Minimap Panel
// Visual minimap overlay for the active map with player dot, enemy dots,
// click-to-move, and map label. Works for any loaded map.
// PC source: minimap UI from Ui3 INI files.
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
            var rect = mapDef.sourceBoundsRect;
            if (rect == null || rect.width <= 0 || rect.height <= 0)
                return new Vector2(0.5f, 0.5f);

            float nx = (worldPos.x - rect.x) / rect.width;
            float ny = 1f - ((worldPos.y - rect.y) / rect.height); // flip Y
            return new Vector2(
                Mathf.Clamp01(nx),
                Mathf.Clamp01(ny));
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
            containerRt.offsetMax = new Vector2(-8f, -8f);

            // Border/background
            var borderImg = containerGo.AddComponent<Image>();
            borderImg.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);

            // Map image (placeholder colored rect)
            var mapGo = new GameObject("MapImage");
            mapGo.transform.SetParent(containerGo.transform, false);
            var mapRt = mapGo.AddComponent<RectTransform>();
            mapRt.anchorMin = new Vector2(0.04f, 0.08f);
            mapRt.anchorMax = new Vector2(0.96f, 0.92f);
            _mapImage = mapGo.AddComponent<Image>();
            _mapImage.color = new Color(0.15f, 0.18f, 0.12f, 0.9f);

            // Grid overlay for terrain feel
            var gridGo = new GameObject("Grid");
            gridGo.transform.SetParent(mapGo.transform, false);
            var gridRt = gridGo.AddComponent<RectTransform>();
            gridRt.anchorMin = Vector2.zero;
            gridRt.anchorMax = Vector2.one;
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

            // Map name label
            var nameGo = new GameObject("MapName");
            nameGo.transform.SetParent(containerGo.transform, false);
            var nameRt = nameGo.AddComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0.05f, 0.93f);
            nameRt.anchorMax = new Vector2(0.95f, 1f);
            _mapNameText = nameGo.AddComponent<Text>();
            _mapNameText.font = _font;
            _mapNameText.fontSize = 18;
            _mapNameText.color = new Color(1f, 0.95f, 0.8f);
            _mapNameText.alignment = TextAnchor.MiddleCenter;

            // Click-to-move on minimap
            var btnGo = new GameObject("ClickArea");
            btnGo.transform.SetParent(mapGo.transform, false);
            var btnRt = btnGo.AddComponent<RectTransform>();
            btnRt.anchorMin = Vector2.zero;
            btnRt.anchorMax = Vector2.one;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = Color.clear;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(OnMinimapClick);
        }

        private void OnMinimapClick()
        {
            // Move player toward map center on minimap click
            if (_mapManager?.ActiveMap == null || _player == null) return;

            var mapDef = _mapManager.ActiveMap;
            var rect = mapDef.sourceBoundsRect;
            if (rect == null || rect.width <= 0) return;

            // Move player toward map center
            Vector2 center = new Vector2(
                rect.x + rect.width * 0.5f,
                rect.y + rect.height * 0.5f);
            _player.PlaceAt(center);
            SubsystemLog.Info("Minimap", $"Click-to-move → map center {center}");
        }
    }
}
