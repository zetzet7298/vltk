// -----------------------------------------------------------------------------
// VLTK Mobile — PC ObjData SPR renderer for Region_S Obj_S.dat placements.
// Uses exact \spr\obj\... assets staged from PC package1.ini order.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    [DisallowMultipleComponent]
    public sealed class PcMapObjectVisual : MonoBehaviour
    {
        public string sourcePath;
        public int direction;
        public int startFrame;
        public int intervalMs;
        public bool loopAnimation;
        public float pixelsPerUnit = 1f;
        public Vector2 referencePixel;

        private SpriteRenderer _renderer;
        private Transform _spriteRoot;
        private Clip _clip;
        private float _elapsedMs;

        private sealed class Clip
        {
            public int directionCount;
            public int framesPerDirection;
            public Sprite[] sprites;
            public Vector2[] offsets;
        }

        private static readonly Dictionary<string, Clip> Cache = new();
        private static readonly HashSet<string> MissingLog = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetCache()
        {
            Cache.Clear();
            MissingLog.Clear();
        }

        public bool HasVisual => IsAlive(_clip);

        public void Configure(MapInteractiveObject obj)
        {
            if (obj == null) return;
            sourcePath = obj.imageName;
            direction = obj.direction != 0 ? obj.direction : obj.imageCurDir;
            startFrame = Math.Max(0, obj.imageCurFrame);
            intervalMs = Math.Max(0, obj.imageInterval);
            loopAnimation = obj.loopAnimation != 0;
            referencePixel = new Vector2(obj.imageCgXpos, obj.imageCgYpos);
            EnsureRenderer();
            _clip = LoadClip();
            ApplyFrame(0f);
        }

        private void Awake()
        {
            EnsureRenderer();
            _clip = LoadClip();
            ApplyFrame(0f);
        }

        private void Update()
        {
            if (!loopAnimation || intervalMs <= 0) return;
            _elapsedMs += Time.deltaTime * 1000f;
            ApplyFrame(_elapsedMs);
        }

        private void EnsureRenderer()
        {
            if (_spriteRoot != null && _renderer != null) return;
            var child = transform.Find("ObjectSprite");
            if (child == null)
            {
                var go = new GameObject("ObjectSprite");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            _spriteRoot = child;
            _renderer = child.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = child.gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingLayerName = "Default";
            _renderer.sortingOrder = MapRenderer.BuiltinSortingOrder + 500;
        }

        private void ApplyFrame(float elapsedMs)
        {
            if (_renderer == null || !IsAlive(_clip)) return;
            int frameInDir = startFrame;
            if (loopAnimation && intervalMs > 0 && _clip.framesPerDirection > 1)
                frameInDir = Mathf.FloorToInt(elapsedMs / intervalMs) % _clip.framesPerDirection;
            int dirs = Math.Max(1, _clip.directionCount);
            int dir = dirs > 1 ? Mathf.Clamp(direction, 0, dirs - 1) : 0;
            int idx = Mathf.Clamp(dir * _clip.framesPerDirection + frameInDir, 0, _clip.sprites.Length - 1);
            _renderer.sprite = _clip.sprites[idx];
            _spriteRoot.localPosition = _clip.offsets[idx];
        }

        private Clip LoadClip()
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) return null;
            string key = $"{sourcePath}|ppu={pixelsPerUnit:F3}|ref={referencePixel.x:F1},{referencePixel.y:F1}";
            if (Cache.TryGetValue(key, out var cached) && IsAlive(cached)) return cached;

            byte[] data = ReadSprData(sourcePath);
            if (data == null)
            {
                LogMissing(sourcePath, "object SPR missing");
                return null;
            }
            var decoded = SprDecoder.Decode(data);
            if (!decoded.success || decoded.header == null || decoded.frames == null || decoded.frames.Length == 0)
            {
                LogMissing(sourcePath, decoded.error ?? "object SPR decode failed");
                return null;
            }

            int dirs = Math.Max(1, (int)decoded.header.directions);
            int total = decoded.frames.Length;
            int framesPerDir = Math.Max(1, total / dirs);
            if (total % dirs != 0) dirs = 1;
            var clip = new Clip
            {
                directionCount = dirs,
                framesPerDirection = framesPerDir,
                sprites = new Sprite[total],
                offsets = new Vector2[total],
            };
            var refPixel = referencePixel.sqrMagnitude > 0.01f
                ? referencePixel
                : new Vector2(decoded.header.centerX, decoded.header.centerY);

            for (int i = 0; i < total; i++)
            {
                var frame = decoded.frames[i];
                if (frame == null || frame.width == 0 || frame.height == 0) continue;
                var tex = SprDecoder.CreateTexture(frame);
                if (tex == null) continue;
                tex.name = $"PcMapObj_{Path.GetFileNameWithoutExtension(sourcePath)}_{i:000}";
                clip.sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                clip.offsets[i] = new Vector2((frame.offsetX - refPixel.x) / pixelsPerUnit, (refPixel.y - frame.offsetY) / pixelsPerUnit);
            }

            Cache[key] = clip;
            return clip;
        }

        private static bool IsAlive(Clip clip)
        {
            if (clip == null || clip.sprites == null) return false;
            foreach (var sprite in clip.sprites)
                if (sprite != null) return true;
            return false;
        }

        private static byte[] ReadSprData(string sourcePath)
        {
            string root = Application.streamingAssetsPath;
            string uid = SprRuntimeService.ComputePathUidHex(sourcePath);
            foreach (var dir in EnumerateRoots(root))
            {
                if (!string.IsNullOrEmpty(uid))
                {
                    string hashed = Path.Combine(dir, uid + ".spr");
                    if (File.Exists(hashed)) return File.ReadAllBytes(hashed);
                }
                string direct = Path.Combine(dir, Path.GetFileName(sourcePath.Replace('\\', Path.DirectorySeparatorChar)));
                if (File.Exists(direct)) return File.ReadAllBytes(direct);
            }
            return null;
        }

        private static IEnumerable<string> EnumerateRoots(string streamingRoot)
        {
            yield return Path.Combine(streamingRoot, "Generated", "ObjectSprites");
            yield return Path.Combine(streamingRoot, "Sprites");
            yield return Path.Combine(streamingRoot, "Generated", "MapSprites");
        }

        private static void LogMissing(string sourcePath, string reason)
        {
            string key = sourcePath + "|" + reason;
            if (!MissingLog.Add(key)) return;
            SubsystemLog.Warn("PcMapObjectVisual", $"{reason}: {sourcePath}");
        }
    }
}
