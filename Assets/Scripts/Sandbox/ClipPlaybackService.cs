using System;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Playback diagnostics for a sprite clip (AC#4).</summary>
    public struct ClipPlaybackDiagnostics
    {
        public bool isComplete;
        public int expectedFrames;
        public int availableFrames;
        public int missingFrames;
        public SpriteValidationStatus status;
    }

    /// <summary>
    /// M2.5 — Character sprite clip playback logic. Pure C# (no MonoBehaviour) so it
    /// is fully EditMode-testable. Advances frames by deltaTime at the clip's frame
    /// rate, resolves the flattened atlas frame index for the current direction,
    /// exposes a stable pivot/offset per frame, and reports incomplete clips. A
    /// MonoBehaviour driver maps the resulting frame index to an atlas sprite.
    /// </summary>
    public class ClipPlaybackService
    {
        private SpriteClipDefinition _clip;
        private float _time;
        private int _direction;

        public SpriteClipDefinition Clip => _clip;
        public int CurrentDirection => _direction;

        /// <summary>Frames per direction (frameCount is the per-direction length).</summary>
        public int FramesPerDirection => _clip != null ? Mathf.Max(0, _clip.frameCount) : 0;
        public int DirectionCount => _clip != null ? Mathf.Max(1, _clip.directionCount) : 1;

        public void SetClip(SpriteClipDefinition clip)
        {
            _clip = clip;
            _time = 0f;
            _direction = 0;
        }

        /// <summary>AC#1 — current frame index within the active direction (0-based).</summary>
        public int CurrentFrameInDirection
        {
            get
            {
                if (_clip == null || FramesPerDirection <= 0) return 0;
                float rate = _clip.frameRate > 0 ? _clip.frameRate : 10f;
                int frame = Mathf.FloorToInt(_time * rate) % FramesPerDirection;
                return frame < 0 ? frame + FramesPerDirection : frame;
            }
        }

        /// <summary>
        /// AC#1 — flattened atlas frame index = direction * framesPerDirection +
        /// frameInDirection. Matches how directional clips are packed in the atlas.
        /// </summary>
        public int CurrentAtlasFrameIndex
        {
            get
            {
                if (_clip == null || FramesPerDirection <= 0) return 0;
                return _direction * FramesPerDirection + CurrentFrameInDirection;
            }
        }

        /// <summary>AC#1 — advance playback time; deterministic for a given deltaTime.</summary>
        public void Tick(float deltaTime)
        {
            if (_clip == null) return;
            _time += Mathf.Max(0f, deltaTime);
        }

        /// <summary>
        /// AC#2 — change the facing direction. Clamped to available directions; if the
        /// clip has only one direction the request is ignored (returns false).
        /// </summary>
        public bool SetDirection(int direction)
        {
            if (_clip == null) return false;
            int dirs = DirectionCount;
            if (dirs <= 1) { _direction = 0; return false; }
            _direction = ((direction % dirs) + dirs) % dirs;
            return true;
        }

        /// <summary>
        /// AC#3 — pivot + per-frame offset for the current frame, stable across the
        /// animation. Falls back to the clip pivot when no per-frame offset exists.
        /// </summary>
        public Vector2 CurrentPivotOffset()
        {
            if (_clip == null) return Vector2.zero;
            var pivot = _clip.pivot;
            var offsets = _clip.frameOffsets;
            int idx = CurrentAtlasFrameIndex;
            if (offsets != null && idx >= 0 && idx < offsets.Length)
                return pivot + offsets[idx];
            return pivot;
        }

        /// <summary>
        /// AC#4 — diagnostics: compares expected frames (framesPerDirection *
        /// directionCount) against the available frame offsets / atlas, flagging an
        /// incomplete clip.
        /// </summary>
        public ClipPlaybackDiagnostics Diagnose()
        {
            var d = new ClipPlaybackDiagnostics();
            if (_clip == null)
            {
                d.status = SpriteValidationStatus.Unknown;
                return d;
            }

            d.expectedFrames = FramesPerDirection * DirectionCount;
            d.availableFrames = _clip.frameOffsets?.Length ?? 0;
            d.missingFrames = Mathf.Max(0, d.expectedFrames - d.availableFrames);
            d.isComplete = d.expectedFrames > 0 && d.missingFrames == 0;
            d.status = d.isComplete
                ? SpriteValidationStatus.Valid
                : (d.availableFrames == 0 ? SpriteValidationStatus.MissingFrames : SpriteValidationStatus.Partial);

            if (!d.isComplete)
                SubsystemLog.Warn("ClipPlayback",
                    $"Incomplete clip '{_clip.actionName}': {d.availableFrames}/{d.expectedFrames} frames");
            return d;
        }
    }
}
