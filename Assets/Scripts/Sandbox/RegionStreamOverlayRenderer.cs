using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.9 AC#3 — GM overlay that draws one colored quad per non-unloaded region so
    /// loaded/loading/failed regions are visible in world space. Colors come from the
    /// pure <see cref="RegionStreamingService.GetStateColor"/> so the overlay and the
    /// streaming logic never disagree. Toggled from the GM Map tab. Pure color/visibility
    /// state is queryable for tests; mesh building is Play Mode only.
    /// </summary>
    public class RegionStreamOverlayRenderer : MonoBehaviour
    {
        [Header("Settings")]
        public float regionWidth = 512f;
        public float regionHeight = 1024f;
        public Vector2 worldOrigin = Vector2.zero;
        [Tooltip("Inset so neighboring region quads stay visually separated.")]
        public float inset = 8f;

        private GameObject _overlayRoot;
        private Material _sharedMaterial;
        private bool _visible;

        public bool IsVisible => _visible;
        public int RenderedRegions { get; private set; }

        private void Awake()
        {
            _overlayRoot = new GameObject("RegionStreamOverlay");
            _overlayRoot.transform.SetParent(transform, false);
            _overlayRoot.SetActive(false);
            CreateSharedResources();
        }

        public void Show()
        {
            _visible = true;
            if (_overlayRoot != null) _overlayRoot.SetActive(true);
            SubsystemLog.Info("RegionStreamOverlay", "Shown");
        }

        public void Hide()
        {
            _visible = false;
            if (_overlayRoot != null) _overlayRoot.SetActive(false);
            SubsystemLog.Info("RegionStreamOverlay", "Hidden");
        }

        public void Toggle()
        {
            if (_visible) Hide();
            else Show();
        }

        public void Clear()
        {
            if (_overlayRoot == null) return;
            for (int i = _overlayRoot.transform.childCount - 1; i >= 0; i--)
                Destroy(_overlayRoot.transform.GetChild(i).gameObject);
            RenderedRegions = 0;
        }

        /// <summary>
        /// AC#3 — rebuild the overlay from the service's current per-region state.
        /// Each tracked region gets a quad tinted by its stream state color.
        /// </summary>
        public void Rebuild(RegionStreamingService service)
        {
            if (service == null) return;
            Clear();

            foreach (var kv in service.States)
            {
                var coord = kv.Key;
                var color = service.GetStateColor(coord);
                BuildRegionQuad(coord, color);
                RenderedRegions++;
            }
        }

        private void BuildRegionQuad(RegionCoord coord, Color color)
        {
            float baseX = worldOrigin.x + coord.x * regionWidth + inset;
            float baseY = worldOrigin.y + coord.y * regionHeight + inset;
            float w = regionWidth - inset * 2f;
            float h = regionHeight - inset * 2f;

            var go = new GameObject($"RegionState_{coord.x}_{coord.y}");
            go.transform.SetParent(_overlayRoot.transform, false);
            go.transform.position = new Vector3(baseX, baseY, -0.9f);

            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.material = _sharedMaterial;

            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(w, 0, 0),
                    new Vector3(w, h, 0),
                    new Vector3(0, h, 0),
                },
                triangles = new[] { 0, 1, 2, 0, 2, 3 },
                colors = new[] { color, color, color, color },
            };
            mf.mesh = mesh;
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
            if (_overlayRoot != null) Destroy(_overlayRoot);
            if (_sharedMaterial != null) Destroy(_sharedMaterial);
        }
    }
}
