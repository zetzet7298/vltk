using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class ObstacleOverlayRenderer : MonoBehaviour
    {
        [Header("Settings")]
        public float cellWorldSize = 32f;
        public Color blockedColor = new Color(0.85f, 0.12f, 0.12f, 0.75f);
        public Color walkableColor = new Color(0.20f, 0.55f, 0.25f, 0.55f);
        public bool showWalkable = true;
        public bool showBlocked = true;

        private GameObject _overlayRoot;
        private bool _visible;
        private Mesh _sharedQuadMesh;
        private Material _sharedMaterial;

        public bool IsVisible => _visible;
        public int RenderedRegions { get; private set; }
        public int RenderedCells { get; private set; }

        private void Awake()
        {
            _overlayRoot = new GameObject("ObstacleOverlay");
            // Hidden by default — overlay is a debug/visualisation aid.
            _overlayRoot.SetActive(false);
            _visible = false;
            CreateSharedResources();
        }

        public void Show()
        {
            _visible = true;
            _overlayRoot.SetActive(true);
            SubsystemLog.Info("ObstacleOverlay", "Shown");
        }

        public void Hide()
        {
            _visible = false;
            _overlayRoot.SetActive(false);
            SubsystemLog.Info("ObstacleOverlay", "Hidden");
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }

        public void Clear()
        {
            for (int i = _overlayRoot.transform.childCount - 1; i >= 0; i--)
                Destroy(_overlayRoot.transform.GetChild(i).gameObject);
            RenderedRegions = 0;
            RenderedCells = 0;
        }

        /// <summary>
        /// Render one region's obstacle grid. <paramref name="regionWorldX"/> and
        /// <paramref name="regionWorldY"/> are the region's bottom-left corner in
        /// world units (already scaled — do not multiply by cellToWorldScale).
        /// </summary>
        public void RenderRegion(ObstacleGrid grid, float regionWorldX, float regionWorldY)
        {
            if (grid == null) return;
            if (!showBlocked && !showWalkable) return;

            float worldX = regionWorldX;
            float worldY = regionWorldY;
            float z = -1f;

            var go = new GameObject($"Obstacle_{grid.regionX}_{grid.regionY}");
            go.transform.SetParent(_overlayRoot.transform, false);
            go.transform.position = new Vector3(worldX, worldY, z);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _sharedMaterial;

            int cellCount = 0;
            for (int cy = 0; cy < grid.height; cy++)
            {
                for (int cx = 0; cx < grid.width; cx++)
                {
                    bool blocked = grid.GetRawFlags(cx, cy) != 0;
                    if ((blocked && showBlocked) || (!blocked && showWalkable))
                        cellCount++;
                }
            }

            if (cellCount == 0)
            {
                Destroy(go);
                return;
            }

            var vertices = new List<Vector3>(cellCount * 4);
            var triangles = new List<int>(cellCount * 6);
            var colors = new List<Color>(cellCount * 4);

            int vertIdx = 0;
            float cellSize = cellWorldSize;

            for (int cy = 0; cy < grid.height; cy++)
            {
                for (int cx = 0; cx < grid.width; cx++)
                {
                    byte flags = grid.GetRawFlags(cx, cy);
                    bool isBlocked = flags != 0;

                    if (isBlocked && !showBlocked) continue;
                    if (!isBlocked && !showWalkable) continue;

                    float px = cx * cellSize;
                    float py = cy * cellSize;

                    Color col = isBlocked ? blockedColor : walkableColor;

                    vertices.Add(new Vector3(px, py, 0));
                    vertices.Add(new Vector3(px + cellSize, py, 0));
                    vertices.Add(new Vector3(px + cellSize, py + cellSize, 0));
                    vertices.Add(new Vector3(px, py + cellSize, 0));

                    colors.Add(col); colors.Add(col); colors.Add(col); colors.Add(col);

                    triangles.Add(vertIdx);
                    triangles.Add(vertIdx + 1);
                    triangles.Add(vertIdx + 2);
                    triangles.Add(vertIdx);
                    triangles.Add(vertIdx + 2);
                    triangles.Add(vertIdx + 3);

                    vertIdx += 4;
                }
            }

            var mesh = new Mesh
            {
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                colors = colors.ToArray(),
            };
            mf.mesh = mesh;

            RenderedRegions++;
            RenderedCells += cellCount;
        }

        private void CreateSharedResources()
        {
            _sharedMaterial = new Material(Shader.Find("Sprites/Default"))
            {
                color = Color.white,
            };
            _sharedMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            _sharedMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            _sharedMaterial.SetInt("_ZWrite", 0);
            _sharedMaterial.renderQueue = 3000;
        }

        private void OnDestroy()
        {
            if (_overlayRoot != null)
                Destroy(_overlayRoot);
            if (_sharedMaterial != null)
                Destroy(_sharedMaterial);
        }
    }
}
