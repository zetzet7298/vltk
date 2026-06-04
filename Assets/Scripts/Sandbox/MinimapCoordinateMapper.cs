using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Pure minimap coordinate conversion helper. Uses Unity UI local rect space
    /// (bottom-left normalized) and maps it directly to MapDefinition source bounds.
    /// </summary>
    public static class MinimapCoordinateMapper
    {
        public static Vector2 WorldToMinimapNormalized(MapDefinition map, Vector2 worldPos)
        {
            var rect = map?.sourceBoundsRect;
            if (rect == null || rect.width <= 0f || rect.height <= 0f)
                return new Vector2(0.5f, 0.5f);

            float u = Mathf.Clamp01((worldPos.x - rect.x) / rect.width);
            float v = Mathf.Clamp01((worldPos.y - rect.y) / rect.height);
            return new Vector2(u, v);
        }

        public static Vector2 MinimapLocalToWorld(MapDefinition map, Vector2 localPointer, Rect minimapRect)
        {
            var source = map?.sourceBoundsRect;
            if (source == null || source.width <= 0f || source.height <= 0f || minimapRect.width <= 0f || minimapRect.height <= 0f)
                return Vector2.zero;

            float u = Mathf.Clamp01((localPointer.x - minimapRect.xMin) / minimapRect.width);
            float v = Mathf.Clamp01((localPointer.y - minimapRect.yMin) / minimapRect.height);

            return new Vector2(
                source.x + source.width * u,
                source.y + source.height * v);
        }
    }
}
