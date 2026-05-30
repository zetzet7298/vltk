using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class MapRenderer : MonoBehaviour
    {
        private ObstacleOverlayRenderer _obstacleOverlay;
        private Transform _mapRoot;
        private int _loadedMapId = -1;

        public int LoadedMapId => _loadedMapId;
        public ObstacleOverlayRenderer ObstacleOverlay => _obstacleOverlay;

        private void Awake()
        {
            _obstacleOverlay = gameObject.AddComponent<ObstacleOverlayRenderer>();
            _mapRoot = new GameObject("MapContent").transform;
            _mapRoot.SetParent(transform, false);
        }

        public void LoadMapRegions(MapDefinition mapDef)
        {
            if (mapDef == null) return;

            Clear();
            _loadedMapId = mapDef.catalogEntry.mapId;

            var catalogEntry = mapDef.catalogEntry;
            SubsystemLog.Info("MapRenderer",
                $"Loading map {catalogEntry.displayNameNormalized} " +
                $"({mapDef.regionCountX}x{mapDef.regionCountY} regions)");

            // Load sample region files from StreamingAssets
            LoadSampleRegions(mapDef);
        }

        public void Clear()
        {
            _obstacleOverlay.Clear();
            for (int i = _mapRoot.childCount - 1; i >= 0; i--)
                Destroy(_mapRoot.GetChild(i).gameObject);
            _loadedMapId = -1;
        }

        private void LoadSampleRegions(MapDefinition mapDef)
        {
            var regionsDir = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions");
            if (!Directory.Exists(regionsDir))
            {
                SubsystemLog.Warn("MapRenderer", "No test region data available");
                return;
            }

            var files = Directory.GetFiles(regionsDir, "*.dat");
            SubsystemLog.Info("MapRenderer", $"Found {files.Length} test region files");

            int rendered = 0;
            foreach (var f in files)
            {
                var data = File.ReadAllBytes(f);
                var region = RegionParser.Parse(data);
                if (!region.success) continue;

                // Place regions in a grid layout for visualization
                int rx = rendered % 5;
                int ry = rendered / 5;
                int pixelX = rx * 512;
                int pixelY = ry * 1024;

                if (region.HasObstacle)
                {
                    var grid = RegionParser.ExtractObstacle(region, _loadedMapId, rx, ry);
                    if (grid != null)
                    {
                        _obstacleOverlay.RenderRegion(grid, pixelX, pixelY);
                        rendered++;
                    }
                }

                // Parse ground tiles and render them procedurally
                if (region.HasGround)
                {
                    var ground = GroundLayerParser.ExtractFromRegion(region);
                    if (ground != null)
                    {
                        RenderGroundLayer(ground, rx, ry, pixelX, pixelY);
                    }
                }

                // Parse built-in objects and render them procedurally
                if (region.HasBuiltin)
                {
                    var builtin = BuildinObjParser.ExtractFromRegion(region);
                    if (builtin != null)
                    {
                        RenderBuiltinObjects(builtin, rx, ry, pixelX, pixelY);
                    }
                }

                // Show ground info in scene
                if (region.HasGround)
                {
                    var ground = GroundLayerParser.ExtractFromRegion(region);
                    if (ground != null)
                    {
                        CreateRegionLabel(rx, ry, pixelX, pixelY,
                            $"{ground.numTiles} tiles, {ground.numObjects} objs");
                    }
                }
            }

            SubsystemLog.Info("MapRenderer",
                $"Rendered {rendered} regions with obstacle overlay");
        }

        private void RenderGroundLayer(GroundLayerData ground, int rx, int ry, float pixelX, float pixelY)
        {
            var groundRoot = new GameObject($"Ground_{rx}_{ry}").transform;
            groundRoot.SetParent(_mapRoot, false);

            // Generate tiles
            foreach (var tile in ground.tiles)
            {
                // Calculate position relative to region base
                // CellWidth = 32, CellHeight = 32
                float tx = pixelX + (tile.h * 32f);
                float ty = pixelY + (tile.v * 32f);

                var tileGo = new GameObject($"Tile_{tile.h}_{tile.v}");
                tileGo.transform.SetParent(groundRoot, false);
                tileGo.transform.position = new Vector3(tx + 16f, ty + 16f, 0f);

                var sr = tileGo.AddComponent<SpriteRenderer>();
                sr.sprite = CreateProceduralTileSprite(tile.spriteName, 32, 32);
                sr.sortingOrder = -10;
            }

            // Generate overlay/cover objects
            foreach (var obj in ground.objects)
            {
                // Absolute positions are stored in nPositionX, nPositionY.
                // In JX, we draw them relative to current region corner:
                // px = obj.positionX - regionScenePosX
                // py = (obj.positionY - regionScenePosY) / 2 -> wait, our obstacle scale uses direct coordinate scale,
                // so we can just place it at (obj.positionX, obj.positionY) relative or absolute!
                // Let's place it at pixel base + relative position
                // Wait! In JX, obj.relateRegion is the region index (or relative region)
                // Let's draw it relative to this region base:
                // For ground objects, the coordinates in file are absolute map scene positions!
                // To keep it simple and visual, let's map them to this region's local pixel coordinates:
                float ox = pixelX + (obj.positionX % 512);
                float oy = pixelY + (obj.positionY % 1024);

                var objGo = new GameObject($"GroundObj_{obj.imageName}");
                objGo.transform.SetParent(groundRoot, false);
                objGo.transform.position = new Vector3(ox, oy, -0.5f);

                var sr = objGo.AddComponent<SpriteRenderer>();
                sr.sprite = CreateProceduralTileSprite(obj.imageName, obj.width > 0 ? obj.width : 64, obj.height > 0 ? obj.height : 64);
                sr.sortingOrder = obj.layer;
            }
        }

        private void RenderBuiltinObjects(BuildinObjData builtin, int rx, int ry, float pixelX, float pixelY)
        {
            var builtinRoot = new GameObject($"Builtin_{rx}_{ry}").transform;
            builtinRoot.SetParent(_mapRoot, false);

            foreach (var obj in builtin.objects)
            {
                // Builtin object absolute map positions are stored in imgX1, imgY1.
                // Let's map them to this region's local pixel coordinates:
                float ox = pixelX + (obj.imgX1 % 512);
                float oy = pixelY + (obj.imgY1 % 1024);

                var objGo = new GameObject($"BuiltinObj_{obj.imageName}");
                objGo.transform.SetParent(builtinRoot, false);
                objGo.transform.position = new Vector3(ox, oy, -1f);

                var sr = objGo.AddComponent<SpriteRenderer>();
                sr.sprite = CreateProceduralTileSprite(obj.imageName, obj.imgWidth > 0 ? obj.imgWidth : 64, obj.imgHeight > 0 ? obj.imgHeight : 64);
                sr.sortingOrder = obj.order != 65535 ? (int)obj.order : 0;
            }
        }

        private Sprite CreateProceduralTileSprite(string name, int w, int h)
        {
            // Fallback texture generation based on name hash to give consistent colors
            int hash = string.IsNullOrEmpty(name) ? 0 : name.GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            Color col = Color.HSVToRGB(hue, 0.4f, 0.8f);

            var tex = new Texture2D(w, h);
            var cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++)
            {
                int x = i % w;
                int y = i / w;
                // Add a border so individual tiles are visible
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                {
                    cols[i] = new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 0.8f);
                }
                else
                {
                    cols[i] = new Color(col.r, col.g, col.b, 0.8f);
                }
            }
            tex.SetPixels(cols);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
        }

        private void CreateRegionLabel(int rx, int ry, float px, float py, string text)
        {
            var go = new GameObject($"Label_{rx}_{ry}");
            go.transform.SetParent(_mapRoot, false);
            go.transform.position = new Vector3(px + 256, py + 512, -2f);

            var canvasGo = new GameObject("Canvas_" + rx + "_" + ry);
            canvasGo.transform.SetParent(go.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = canvasGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 100);
            rt.localScale = new Vector3(0.5f, 0.5f, 1f);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(canvasGo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;
            var txt = txtGo.AddComponent<UnityEngine.UI.Text>();
            txt.text = $"Region ({rx},{ry})\n{text}";
            txt.font = UnityEngine.Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 24;
            txt.color = Color.cyan;
            txt.alignment = TextAnchor.MiddleCenter;
        }
    }
}
