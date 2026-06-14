using System;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.8 — Minimap / world-map navigation logic. Pure C# (no MonoBehaviour) so
    /// it is fully EditMode-testable. Maps a world position to a normalized minimap
    /// coordinate using the map's source bounds, resolves/registers the minimap
    /// artifact through the asset registry, and exposes a missing state with its
    /// source id when the artifact is absent.
    /// </summary>
    public class MinimapService
    {
        private readonly IAssetRegistry _registry;
        private IMinimapHost _host;

        public MinimapService(IAssetRegistry registry) : this(registry, null) { }
        public MinimapService(IAssetRegistry registry, IMinimapHost host)
        {
            _registry = registry;
            _host = host;
        }

        public void AttachHost(IMinimapHost host) { _host = host; }

        /// <summary>
        /// AC#1/AC#4 — resolve the minimap artifact for a map definition through the
        /// asset registry and stamp the resulting status onto its <see cref="MinimapRef"/>.
        /// Returns the (possibly newly created) ref. A null minimapRef or one whose
        /// artifact cannot be resolved is marked Missing (still carrying its source id).
        /// </summary>
        public MinimapRef ResolveArtifact(MapDefinition map)
        {
            if (map == null)
            {
                SubsystemLog.Warn("Minimap", "ResolveArtifact called with null map");
                return null;
            }
            int mapId = map.catalogEntry?.mapId ?? 0;
            string settingSourceId = map.catalogEntry?.settingSourceId?.ToKey();

            var minimap = map.minimapRef;
            if (minimap == null)
            {
                // No minimap declared for this map → represent as an explicit
                // missing ref so navigation UI can surface the absence.
                minimap = new MinimapRef
                {
                    sourceId = map.catalogEntry?.settingSourceId,
                    status = MinimapArtifactStatus.Missing,
                };
                map.minimapRef = minimap;
                SubsystemLog.Warn("Minimap",
                    $"Map {map.catalogEntry?.mapId} has no minimap ref; marked missing");
                _host?.OnMapNoMinimapRef(mapId, settingSourceId);
                _host?.OnMinimapMissing(mapId, settingSourceId, "no minimap ref");
                _host?.ShowMinimapUI(mapId, null, true);
                _host?.LogMinimapEvent(mapId, $"Map {mapId} không có minimap ref");
                _host?.PlayMinimapSFX(mapId, "missing");
                _host?.SaveMinimapState(mapId, settingSourceId, null);
                return minimap;
            }

            var entry = minimap.sourceId != null ? _registry?.Resolve(minimap.sourceId) : null;
            if (entry != null && entry.status == AssetStatus.Available)
            {
                minimap.status = MinimapArtifactStatus.Registered;
                if (string.IsNullOrEmpty(minimap.artifactPath))
                    minimap.artifactPath = entry.unityAssetPath;
                _host?.OnMinimapResolved(mapId, minimap.sourceId?.ToKey(), minimap.artifactPath);
                _host?.ShowMinimapUI(mapId, minimap.artifactPath, false);
                _host?.LogMinimapEvent(mapId, $"Minimap đã tải: {minimap.artifactPath}");
                _host?.PlayMinimapSFX(mapId, "load");
                _host?.SaveMinimapState(mapId, minimap.sourceId?.ToKey(), minimap.artifactPath);
            }
            else
            {
                minimap.status = MinimapArtifactStatus.Missing;
                SubsystemLog.Warn("Minimap",
                    $"Minimap artifact missing for source {minimap.sourceId?.ToKey() ?? "<null>"}");
                _host?.OnMinimapMissing(mapId, minimap.sourceId?.ToKey(), "asset not found");
                _host?.ShowMinimapUI(mapId, null, true);
                _host?.LogMinimapEvent(mapId, $"Minimap artifact missing");
                _host?.PlayMinimapSFX(mapId, "missing");
                _host?.SaveMinimapState(mapId, minimap.sourceId?.ToKey(), null);
            }

            return minimap;
        }

        /// <summary>
        /// AC#3 — convert a world position to a normalized minimap coordinate
        /// (0..1 on each axis) using the map's source bounds rect. Y is flipped so
        /// the top of the world maps to the top of the minimap UV. Positions outside
        /// the bounds are clamped to [0,1].
        /// </summary>
        public Vector2 WorldToMinimapNormalized(MapDefinition map, Vector2 worldPos)
        {
            var rect = map?.sourceBoundsRect;
            if (rect == null || rect.width <= 0f || rect.height <= 0f)
                return new Vector2(0.5f, 0.5f);

            float u = (worldPos.x - rect.x) / rect.width;
            float v = (worldPos.y - rect.y) / rect.height;

            u = Mathf.Clamp01(u);
            v = Mathf.Clamp01(v);

            // Flip Y: world bottom (v=0) → minimap bottom; world top → minimap top.
            // UV origin for most UI RawImages is bottom-left, so no flip needed; but
            // marker RectTransforms anchor top-left, so callers can use (u, 1-v).
            int mapId = map?.catalogEntry?.mapId ?? 0;
            _host?.OnWorldToMinimap(mapId, worldPos.x, worldPos.y, u, v);
            return new Vector2(u, v);
        }

        /// <summary>
        /// AC#3 — marker pixel position inside a minimap of the given pixel size,
        /// using a top-left origin (Y inverted) which matches a RectTransform whose
        /// pivot/anchor is top-left.
        /// </summary>
        public Vector2 WorldToMinimapPixel(MapDefinition map, Vector2 worldPos, Vector2 minimapSize)
        {
            var n = WorldToMinimapNormalized(map, worldPos);
            return new Vector2(n.x * minimapSize.x, (1f - n.y) * minimapSize.y);
        }

        /// <summary>
        /// Convert a top-left-origin UI pixel inside the minimap/world-preview rect to
        /// a clamped world coordinate. This is the inverse of WorldToMinimapPixel and
        /// matches PC map UI hit-testing where y grows downward in the widget.
        /// </summary>
        public Vector2 MinimapPixelToWorld(MapDefinition map, Vector2 pixel, Vector2 minimapSize)
        {
            var rect = map?.sourceBoundsRect;
            if (rect == null || rect.width <= 0f || rect.height <= 0f || minimapSize.x <= 0f || minimapSize.y <= 0f)
                return Vector2.zero;

            float u = Mathf.Clamp01(pixel.x / minimapSize.x);
            float v = Mathf.Clamp01(1f - (pixel.y / minimapSize.y));
            int mapId = map?.catalogEntry?.mapId ?? 0;
            var world = new Vector2(rect.x + rect.width * u, rect.y + rect.height * v);
            _host?.OnMinimapToWorld(mapId, pixel.x, pixel.y, world.x, world.y);
            return world;
        }

        /// <summary>
        /// Convert a normalized top-left-origin UI point (0..1) into world coords.
        /// Useful for UI Toolkit pointer callbacks whose size is measured separately.
        /// </summary>
        public Vector2 MinimapNormalizedToWorld(MapDefinition map, Vector2 normalizedTopLeft)
        {
            var rect = map?.sourceBoundsRect;
            if (rect == null || rect.width <= 0f || rect.height <= 0f)
                return Vector2.zero;

            float u = Mathf.Clamp01(normalizedTopLeft.x);
            float v = Mathf.Clamp01(1f - normalizedTopLeft.y);
            return new Vector2(rect.x + rect.width * u, rect.y + rect.height * v);
        }

        /// <summary>AC#4 — true when the map's minimap artifact is absent.</summary>
        public bool IsMissing(MapDefinition map)
        {
            if (map?.minimapRef == null) return true;
            return map.minimapRef.status != MinimapArtifactStatus.Registered;
        }

        /// <summary>AC#4 — the source id to surface when reporting a missing minimap.</summary>
        public SourceAssetId GetMissingSourceId(MapDefinition map)
        {
            if (map?.minimapRef?.sourceId != null) return map.minimapRef.sourceId;
            return map?.catalogEntry?.settingSourceId;
        }
    }
}
