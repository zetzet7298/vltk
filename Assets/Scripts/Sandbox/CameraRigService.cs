using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>Camera control mode for the sandbox rig.</summary>
    public enum CameraMode
    {
        Follow,  // locked to the follow target
        Free,    // GM unlocked: manual pan/inspect
    }

    /// <summary>
    /// M2.3 — Camera rig logic. Pure C# (no MonoBehaviour) so it is fully
    /// EditMode-testable. Owns the camera's 2D focus position and orthographic zoom,
    /// supports follow vs free (GM unlocked) modes, pan in free mode, zoom clamped to
    /// configured min/max, and a reset to the follow target. A MonoBehaviour driver
    /// applies <see cref="Focus"/>/<see cref="Zoom"/> to a Unity Camera each frame.
    /// </summary>
    public class CameraRigService
    {
        public float MinZoom { get; set; }
        public float MaxZoom { get; set; }

        public CameraMode Mode { get; private set; } = CameraMode.Follow;
        public Vector2 Focus { get; private set; }
        public float Zoom { get; private set; }
        public Vector2 FollowTarget { get; private set; }

        public CameraRigService(Vector2 initialTarget, float zoom = 5f, float minZoom = 2f, float maxZoom = 20f)
        {
            MinZoom = minZoom;
            MaxZoom = maxZoom > minZoom ? maxZoom : minZoom + 1f;
            FollowTarget = initialTarget;
            Focus = initialTarget;
            Zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
        }

        /// <summary>AC#1 — update the follow target; in Follow mode the focus tracks it.</summary>
        public void SetFollowTarget(Vector2 worldPos)
        {
            FollowTarget = worldPos;
            if (Mode == CameraMode.Follow)
                Focus = worldPos;
        }

        /// <summary>AC#1 — enable follow mode and snap focus to the target.</summary>
        public void EnableFollow()
        {
            Mode = CameraMode.Follow;
            Focus = FollowTarget;
        }

        /// <summary>AC#2 — GM unlock: switch to free mode so the camera can inspect any area.</summary>
        public void Unlock()
        {
            Mode = CameraMode.Free;
        }

        /// <summary>AC#2 — pan the camera in free mode by a world-space delta. Ignored in Follow mode.</summary>
        public bool Pan(Vector2 worldDelta)
        {
            if (Mode != CameraMode.Free) return false;
            Focus += worldDelta;
            return true;
        }

        /// <summary>
        /// AC#3 — change zoom by a delta (e.g. pinch / mouse wheel), clamped to
        /// [MinZoom, MaxZoom]. Positive delta zooms out (larger orthographic size).
        /// </summary>
        public float ZoomBy(float delta)
        {
            Zoom = Mathf.Clamp(Zoom + delta, MinZoom, MaxZoom);
            return Zoom;
        }

        /// <summary>AC#3 — set absolute zoom, clamped to range.</summary>
        public float SetZoom(float zoom)
        {
            Zoom = Mathf.Clamp(zoom, MinZoom, MaxZoom);
            return Zoom;
        }

        /// <summary>AC#4 — reset: return to Follow mode and snap focus back to the target.</summary>
        public void Reset()
        {
            Mode = CameraMode.Follow;
            Focus = FollowTarget;
        }
    }
}
