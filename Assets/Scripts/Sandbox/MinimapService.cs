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

        public MinimapService(IAssetRegistry registry)
        {
            _registry = registry;
        }

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
                return minimap;
            }

            var entry = minimap.sourceId != null ? _registry?.Resolve(minimap.sourceId) : null;
            if (entry != null && entry.status == AssetStatus.Available)
            {
                minimap.status = MinimapArtifactStatus.Registered;
                if (string.IsNullOrEmpty(minimap.artifactPath))
                    minimap.artifactPath = entry.unityAssetPath;
            }
            else
            {
                minimap.status = MinimapArtifactStatus.Missing;
                SubsystemLog.Warn("Minimap",
                    $"Minimap artifact missing for source {minimap.sourceId?.ToKey() ?? "<null>"}");
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
