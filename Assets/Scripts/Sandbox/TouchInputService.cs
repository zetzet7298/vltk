using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>Result of interpreting a screen tap.</summary>
    public struct TapResult
    {
        public bool isUiHit;        // tap landed on a UI control (consumed)
        public bool moveRequested;  // tap should drive a move-to
        public Vector2 worldTarget; // resolved world target when moveRequested
    }

    /// <summary>
    /// M6.1 — Touch control logic for mobile. Pure C# (no MonoBehaviour) so it is
    /// fully EditMode-testable. Resolves tap-to-move targets (AC#1), converts a
    /// virtual-joystick drag into a continuous move vector (AC#2), produces a
    /// skill-button cast intent (AC#3), turns pinch deltas into clamped zoom (AC#4),
    /// and scales UI touch targets to stay readable across screen sizes (AC#5). A
    /// MonoBehaviour input driver feeds raw touch data in and applies the results.
    /// </summary>
    public class TouchInputService
    {
        /// <summary>Pixels-to-world scale for resolving a screen tap to a ground point.</summary>
        public Func<Vector2, Vector2> ScreenToWorld { get; set; } = p => p;

        /// <summary>Joystick dead zone (0..1) below which no movement is produced.</summary>
        public float JoystickDeadZone { get; set; } = 0.15f;

        /// <summary>Reference DPI used to scale touch targets (mdpi = 160).</summary>
        public float ReferenceDpi { get; set; } = 160f;

        /// <summary>Minimum touch-target size in points (Apple/Google ~44pt).</summary>
        public float MinTouchTargetPoints { get; set; } = 44f;

        /// <summary>
        /// AC#1 — interpret a tap. If it hits UI it is consumed (no move); otherwise
        /// the tap resolves to a world target for tap-to-move.
        /// </summary>
        public TapResult Tap(Vector2 screenPos, bool overUi)
        {
            if (overUi)
                return new TapResult { isUiHit = true, moveRequested = false };
            return new TapResult
            {
                isUiHit = false,
                moveRequested = true,
                worldTarget = ScreenToWorld != null ? ScreenToWorld(screenPos) : screenPos,
            };
        }

        /// <summary>
        /// AC#2 — virtual joystick: convert a drag vector (in joystick-local units,
        /// expected roughly within the unit circle) to a normalized move direction
        /// scaled by magnitude. Returns Vector2.zero inside the dead zone.
        /// </summary>
        public Vector2 JoystickToMove(Vector2 drag)
        {
            float mag = drag.magnitude;
            if (mag <= JoystickDeadZone) return Vector2.zero;
            float clamped = Mathf.Min(mag, 1f);
            // Rescale so the dead-zone edge maps to 0 and full deflection maps to 1.
            float scaled = (clamped - JoystickDeadZone) / (1f - JoystickDeadZone);
            return (drag / mag) * scaled;
        }

        /// <summary>AC#3 — skill button tap produces a cast intent for the given slot.</summary>
        public SkillCastIntent SkillButton(int slot)
            => new SkillCastIntent { requested = true, slot = slot };

        /// <summary>
        /// AC#4 — pinch gesture: change in distance between two touches maps to a zoom
        /// delta; the resulting zoom is clamped to [min, max].
        /// </summary>
        public float PinchZoom(float prevDistance, float curDistance, float currentZoom,
            float minZoom, float maxZoom, float sensitivity = 0.01f)
        {
            float delta = (prevDistance - curDistance) * sensitivity; // pinch in → zoom in (smaller)
            return Mathf.Clamp(currentZoom + delta, minZoom, maxZoom);
        }

        /// <summary>
        /// AC#5 — compute a UI touch-target size (in pixels) that stays at least
        /// MinTouchTargetPoints physically, scaling with the device DPI so the target
        /// remains readable/usable as screen size/density changes.
        /// </summary>
        public float TouchTargetPixels(float screenDpi)
        {
            float dpi = screenDpi > 0 ? screenDpi : ReferenceDpi;
            // points → pixels at this DPI (1 point = 1/72 inch is print; for UI we use
            // the Android dp convention: px = dp * dpi/160).
            float px = MinTouchTargetPoints * (dpi / ReferenceDpi);
            return Mathf.Max(MinTouchTargetPoints, px);
        }
    }

    /// <summary>Intent emitted when a skill button is tapped (consumed by cast flow).</summary>
    public struct SkillCastIntent
    {
        public bool requested;
        public int slot;
    }
}
