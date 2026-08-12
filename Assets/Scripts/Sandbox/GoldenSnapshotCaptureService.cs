using System;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Temporary, isolated camera for one caller-selected SkillFx layer.</summary>
    public static class GoldenSnapshotCaptureService
    {
        public const int CaptureSize = 256;
        public const int GridSize = 16;
        public const float OrthographicSize = 32f;
        public static readonly Bounds FixedBounds = new Bounds(Vector3.zero, new Vector3(64f, 64f, 1f));

        public static GoldenSnapshot Capture(
            string mapId, string caseId, int skillFxLayer,
            int skillId = -1, string faction = null, int frame = -1, long tick = -1)
        {
            return Capture(mapId, caseId, skillFxLayer, Vector2.zero, skillId, faction, frame, tick);
        }

        /// <summary>Captures fixed-size bounds centered on focus; no scene camera/UI/terrain is read.</summary>
        public static GoldenSnapshot Capture(
            string mapId, string caseId, int skillFxLayer, Vector2 focus,
            int skillId = -1, string faction = null, int frame = -1, long tick = -1)
        {
            if (string.IsNullOrWhiteSpace(mapId)) throw new ArgumentException("mapId is required", nameof(mapId));
            if (string.IsNullOrWhiteSpace(caseId)) throw new ArgumentException("caseId is required", nameof(caseId));
            if (skillId < 0) throw new ArgumentOutOfRangeException(nameof(skillId));
            if (string.IsNullOrWhiteSpace(faction)) throw new ArgumentException("faction is required", nameof(faction));
            if (frame < 0) throw new ArgumentOutOfRangeException(nameof(frame));
            if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
            if (skillFxLayer < 0 || skillFxLayer > 31) throw new ArgumentOutOfRangeException(nameof(skillFxLayer));
            string layerName = LayerMask.LayerToName(skillFxLayer);
            if (string.IsNullOrWhiteSpace(layerName)) throw new ArgumentException("skillFxLayer must name a layer", nameof(skillFxLayer));

            var cameraObject = new GameObject("GoldenSnapshotCaptureCamera") { hideFlags = HideFlags.HideAndDontSave };
            var camera = cameraObject.AddComponent<Camera>();
            var target = new RenderTexture(CaptureSize, CaptureSize, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 1,
                filterMode = FilterMode.Point,
            };
            Texture2D pixels = null;
            var previous = RenderTexture.active;
            try
            {
                camera.enabled = false;
                camera.orthographic = true;
                camera.orthographicSize = OrthographicSize;
                camera.aspect = 1f;
                camera.transform.position = new Vector3(focus.x, focus.y, -100f);
                camera.transform.rotation = Quaternion.identity;
                camera.nearClipPlane = 0.1f;
                camera.farClipPlane = 200f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear;
                camera.cullingMask = 1 << skillFxLayer;
                camera.targetTexture = target;
                camera.Render();

                RenderTexture.active = target;
                pixels = new Texture2D(CaptureSize, CaptureSize, TextureFormat.RGBA32, false, false);
                pixels.ReadPixels(new Rect(0, 0, CaptureSize, CaptureSize), 0, 0, false);
                pixels.Apply(false, false);
                var snapshot = GoldenSnapshotComparer.Build(
                    mapId, CaptureSize, CaptureSize, pixels.GetRawTextureData<byte>().ToArray(),
                    GridSize, GridSize, caseId: caseId, skillId: skillId, faction: faction, frame: frame, tick: tick);
                snapshot.skillFxLayer = skillFxLayer;
                snapshot.skillFxLayerName = layerName;
                if (!GoldenSnapshotComparer.TryValidate(snapshot, out var error))
                    throw new InvalidOperationException($"Golden capture rejected: {error}");
                return snapshot;
            }
            finally
            {
                RenderTexture.active = previous;
                if (pixels != null) UnityEngine.Object.Destroy(pixels);
                camera.targetTexture = null;
                target.Release();
                UnityEngine.Object.Destroy(target);
                UnityEngine.Object.Destroy(cameraObject);
            }
        }
    }
}
