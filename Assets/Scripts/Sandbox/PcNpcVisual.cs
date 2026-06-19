// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Single-body PC NPC SPR renderer. Mirrors the player SPR decode/runtime-cache path:
    /// staged SPR -> uid lookup -> SprDecoder -> 8-direction frame selection.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PcNpcVisual : MonoBehaviour
    {
        public string standSourcePath;
        public string walkSourcePath;
        public int direction;
        public float frameRate = 8f;
        public float pixelsPerUnit = 1f;
        public Vector2 referencePixel = new Vector2(160f, 192f);
        public bool moving;
        public bool logMissing = true;

        [Header("Shadow")]
        public bool renderShadow = true;
        public string standShadowSourcePath = @"spr\npcres\man\MA_YY_999_ST01.spr";
        public string walkShadowSourcePath = @"spr\npcres\man\MA_YY_999_RN01.spr";

        private SpriteRenderer _renderer;
        private Transform _spriteRoot;
        private SpriteRenderer _shadowRenderer;
        private Transform _shadowRoot;
        private ClipRuntime _stand;
        private ClipRuntime _walk;
        private ClipRuntime _shadowStand;
        private ClipRuntime _shadowWalk;
        private float _time;
        private Vector2 _lastPosition;
        private string _loadedStand;
        private string _loadedWalk;
        private string _loadedShadowStand;
        private string _loadedShadowWalk;

        private sealed class ClipRuntime
        {
            public int totalFrames;
            public int directionCount;
            public int framesPerDirection;
            public Sprite[] sprites;
            public Vector2[] offsets;
        }

        private static readonly Dictionary<string, ClipRuntime> ClipCache = new();
        private static readonly HashSet<string> MissingLogCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCaches()
        {
            ClipCache.Clear();
            MissingLogCache.Clear();
        }

        public bool HasWalkClip => IsClipAlive(_walk);
        public bool HasAnyClip => IsClipAlive(_walk) || IsClipAlive(_stand);
        public bool HasShadow => renderShadow && _shadowRenderer != null && _shadowRenderer.sprite != null;
        public int FramesPerDirection => (_walk ?? _stand)?.framesPerDirection ?? 0;
        public int DirectionCount => (_walk ?? _stand)?.directionCount ?? 0;

        private void Awake()
        {
            EnsureRenderer();
            EnsureShadowRenderer();
            _lastPosition = transform.position;
            ReloadIfNeeded(true);
            ApplyFrame(0f);
        }

        private void Update()
        {
            var pos = (Vector2)transform.position;
            var delta = pos - _lastPosition;
            if (delta.sqrMagnitude > 0.01f)
                SetMoveInput(delta);
            else
                moving = false;
            _lastPosition = pos;
            _time += Time.deltaTime;
            ApplyFrame(_time);
        }

        public void Configure(string standPath, string walkPath, Vector2? refPixel = null)
        {
            standSourcePath = standPath;
            walkSourcePath = walkPath;
            if (refPixel.HasValue) referencePixel = refPixel.Value;
            EnsureRenderer();
            EnsureShadowRenderer();
            ReloadIfNeeded(true);
            ApplyFrame(0f);
        }

        public void SetMoveInput(Vector2 move)
        {
            int next = MalePlayerSpriteCatalog.DirectionFromMove(move);
            if (next >= 0)
            {
                direction = next;
                moving = true;
            }
            else
            {
                moving = false;
            }
        }

        public void Tick(float deltaTime)
        {
            _time += Mathf.Max(0f, deltaTime);
            ApplyFrame(_time);
        }

        private void ReloadIfNeeded(bool force)
        {
            if (!force && _loadedStand == standSourcePath && _loadedWalk == walkSourcePath &&
                _loadedShadowStand == standShadowSourcePath && _loadedShadowWalk == walkShadowSourcePath)
                return;
            _stand = LoadClip(standSourcePath);
            _walk = LoadClip(walkSourcePath);
            _shadowStand = renderShadow ? LoadClip(standShadowSourcePath) : null;
            _shadowWalk = renderShadow ? LoadClip(walkShadowSourcePath) : null;
            _loadedStand = standSourcePath;
            _loadedWalk = walkSourcePath;
            _loadedShadowStand = standShadowSourcePath;
            _loadedShadowWalk = walkShadowSourcePath;
        }

        private void ApplyFrame(float time)
        {
            ReloadIfNeeded(false);
            var clip = moving && IsClipAlive(_walk) ? _walk : (IsClipAlive(_stand) ? _stand : _walk);
            if (!IsClipAlive(clip))
            {
                if (_renderer != null) _renderer.sprite = null;
                return;
            }

            int dirs = Mathf.Max(1, clip.directionCount);
            int dir = dirs > 1 ? Mathf.Clamp(direction, 0, dirs - 1) : 0;
            int frameInDir = moving ? Mathf.FloorToInt(time * frameRate) % clip.framesPerDirection : 0;
            int idx = Mathf.Clamp(dir * clip.framesPerDirection + frameInDir, 0, clip.sprites.Length - 1);
            // PC SPR frames are often shadow/ambient tiles (very wide or very tall) — the actual
            // character sprite lives in one direction and frame position. When the natural index
            // hits a null or pathological frame, search nearby for a "real" sprite (50–512 px).
            var sprite = clip.sprites[idx];
            if (NeedsSpriteFallback(sprite))
            {
                int pick = FindBestSpriteInDirection(clip, dir);
                if (pick < 0)
                {
                    if (_renderer != null) _renderer.sprite = null;
                    return;
                }
                sprite = clip.sprites[pick];
                idx = pick;
            }
            _renderer.sprite = sprite;
            _spriteRoot.localPosition = clip.offsets[idx];
            _renderer.sortingOrder = MapRenderer.PlayerSortingOrder - 10;
            ApplyShadowFrame(time);
        }

        private void ApplyShadowFrame(float time)
        {
            EnsureShadowRenderer();
            if (!renderShadow || _shadowRenderer == null)
            {
                if (_shadowRenderer != null) _shadowRenderer.sprite = null;
                return;
            }

            var clip = moving && IsClipAlive(_shadowWalk) ? _shadowWalk : (IsClipAlive(_shadowStand) ? _shadowStand : _shadowWalk);
            if (!IsClipAlive(clip))
            {
                _shadowRenderer.sprite = null;
                return;
            }

            int dirs = Mathf.Max(1, clip.directionCount);
            int dir = dirs > 1 ? Mathf.Clamp(direction, 0, dirs - 1) : 0;
            int frameInDir = moving ? Mathf.FloorToInt(time * frameRate) % clip.framesPerDirection : 0;
            int idx = Mathf.Clamp(dir * clip.framesPerDirection + frameInDir, 0, clip.sprites.Length - 1);
            _shadowRenderer.sprite = clip.sprites[idx];
            _spriteRoot.localPosition = clip.offsets[idx];
            _shadowRenderer.sortingOrder = MapRenderer.PlayerSortingOrder - 20;
        }

        // Returns true if the natural frame at this index is unusable (null or a wide shadow tile
        // instead of the actual character sprite). Width > 1024 or height > 1024 is treated as
        // an ambient/shadow tile that we should NOT render directly — PC engine packs shadow
        // tiles into the same SPR file as the character frames.
        private static bool NeedsSpriteFallback(Sprite s)
        {
            if (s == null) return true;
            if (s.rect.width > 1024f || s.rect.height > 1024f) return true;
            if (s.rect.width < 16f || s.rect.height < 16f) return true;
            return false;
        }

        // Scan every sprite in the given direction for the one closest to a real character
        // sprite (smallest "reasonable" pixel area). Returns global sprite index, or -1.
        // Scan every sprite in the given direction. Prefer the one that looks like a character
        // body (16–512 px, aspect ratio close to 1:1 — shadows/aux tiles are very wide or very tall).
        // Tiebreaker: smallest |offsetX| - so the character's design pivot wins over shadow
        // tiles that share similar area.
        private static int FindBestSpriteInDirection(ClipRuntime clip, int dir)
        {
            if (clip == null || clip.sprites == null || clip.framesPerDirection <= 0) return -1;
            int dirs = Mathf.Max(1, clip.directionCount);
            int dirBase = Mathf.Clamp(dir, 0, dirs - 1) * clip.framesPerDirection;
            int bestIdx = -1;
            float bestScore = float.MaxValue;
            int end = Mathf.Min(clip.sprites.Length, dirBase + clip.framesPerDirection);
            for (int i = dirBase; i < end; i++)
            {
                var s = clip.sprites[i];
                if (s == null) continue;
                int w = (int)s.rect.width;
                int h = (int)s.rect.height;
                if (w < 16 || h < 16 || w > 512 || h > 512) continue;
                // Aspect ratio: 1.0 = perfect square. Score = |w/h - 1| + small bonus for being closer to design pivot.
                float aspect = (float)w / (float)h;
                float aspectScore = Mathf.Abs(aspect - 1f);
                var off = clip.offsets[i];
                float pivotScore = Mathf.Abs(off.x) * 0.001f;
                float score = aspectScore + pivotScore;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIdx = i;
                }
            }
            return bestIdx;
        }

        private void EnsureRenderer()
        {
            if (_spriteRoot != null && _renderer != null) return;

            var child = transform.Find("NpcSprite");
            if (child == null)
            {
                var go = new GameObject("NpcSprite");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            _spriteRoot = child;
            _renderer = child.GetComponent<SpriteRenderer>();
            if (_renderer == null) _renderer = child.gameObject.AddComponent<SpriteRenderer>();
            _renderer.sortingLayerName = "Default";
            _renderer.sortingOrder = MapRenderer.PlayerSortingOrder - 10;
        }

        private void EnsureShadowRenderer()
        {
            if (_shadowRoot != null && _shadowRenderer != null) return;

            var child = transform.Find("NpcShadow");
            if (child == null)
            {
                var go = new GameObject("NpcShadow");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            _shadowRoot = child;
            _shadowRenderer = child.GetComponent<SpriteRenderer>();
            if (_shadowRenderer == null) _shadowRenderer = child.gameObject.AddComponent<SpriteRenderer>();
            _shadowRenderer.sortingLayerName = "Default";
            _shadowRenderer.sortingOrder = MapRenderer.PlayerSortingOrder - 20;
        }

        private ClipRuntime LoadClip(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath)) return null;
            string root = Path.Combine(Application.streamingAssetsPath, "Sprites");
            string key = $"{root}|{sourcePath}|ppu={pixelsPerUnit:F3}|ref={referencePixel.x:F1},{referencePixel.y:F1}";
            if (ClipCache.TryGetValue(key, out var cached))
            {
                if (IsClipAlive(cached)) return cached;
                ClipCache.Remove(key);
            }

            byte[] data = ReadSprData(root, sourcePath);
            if (data == null)
            {
                LogMissing(sourcePath, "NPC SPR missing");
                return null;
            }

            var decoded = SprDecoder.Decode(data);
            if (!decoded.success || decoded.header == null || decoded.frames == null || decoded.frames.Length == 0)
            {
                LogMissing(sourcePath, decoded.error ?? "NPC SPR decode failed");
                return null;
            }

            int dirs = Mathf.Max(1, decoded.header.directions);
            int total = decoded.frames.Length;
            int framesPerDir = Mathf.Max(1, total / dirs);
            if (total % dirs != 0) dirs = 1;

            var clip = new ClipRuntime
            {
                totalFrames = total,
                directionCount = dirs,
                framesPerDirection = framesPerDir,
                sprites = new Sprite[total],
                offsets = new Vector2[total],
            };

            for (int i = 0; i < total; i++)
            {
                var frame = decoded.frames[i];
                if (frame == null || frame.width == 0 || frame.height == 0) continue;

                var tex = SprDecoder.CreateTexture(frame);
                if (tex == null) continue;
                tex.name = $"PcNpc_{Path.GetFileNameWithoutExtension(sourcePath)}_{i:000}";
                // [PcNpcVisual-TrainingNPC 2026-06-19] Removed buggy shouldFlipY for enemy178/179/180.
                //   Trước fix: decoder đã đặt PC row 0 (top) ở Unity texture top (bottom-up storage),
                //   rồi flip lại khiến PC row 0 xuống Unity bottom → training NPCs hiển thị UPSIDE DOWN.
                //   Sau fix: dùng pivot (0,1) top-left chuẩn, không flip pixel — giống mọi NPC khác.
                clip.sprites[i] = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
                // PC frame offsets are stored in 1/64 PC-pixel units (engine tile grid).
                // referencePixel is in raw PC pixels; normalize offsetY through the same divisor
                // so the resulting world-space offset lands near the sprite's own foot
                // regardless of how big frame.offsetY happens to be in PC internal coords.
                clip.offsets[i] = new Vector2((frame.offsetX - referencePixel.x) / pixelsPerUnit, (referencePixel.y - frame.offsetY) / pixelsPerUnit);
            }

            ClipCache[key] = clip;
            SubsystemLog.Info("PcNpcVisual", $"Loaded {sourcePath}: {total} frames, {dirs} dirs");
            return clip;
        }

        private static bool IsClipAlive(ClipRuntime clip)
        {
            if (clip == null || clip.sprites == null) return false;
            foreach (var sprite in clip.sprites)
                if (sprite != null) return true;
            return false;
        }

        private static byte[] ReadSprData(string root, string sourcePath)
        {
            foreach (var candidateRoot in EnumerateNpcSpriteRoots(root))
            {
                string uid = SprRuntimeService.ComputePathUidHex(sourcePath);
                if (!string.IsNullOrEmpty(uid))
                {
                    string hashedPath = Path.Combine(candidateRoot, uid + ".spr");
                    if (File.Exists(hashedPath)) return File.ReadAllBytes(hashedPath);
                }

                string fileName = Path.GetFileName(sourcePath.Replace('\\', Path.DirectorySeparatorChar));
                string direct = Path.Combine(candidateRoot, fileName);
                if (File.Exists(direct)) return File.ReadAllBytes(direct);
            }
            return null;
        }

        private static IEnumerable<string> EnumerateNpcSpriteRoots(string spritesRoot)
        {
            if (!string.IsNullOrEmpty(spritesRoot))
                yield return spritesRoot;

            var streamingRoot = Path.GetDirectoryName(spritesRoot);
            if (!string.IsNullOrEmpty(streamingRoot))
                yield return Path.Combine(streamingRoot, "Generated", "NpcSprites");
        }

        private void LogMissing(string sourcePath, string reason)
        {
            if (!logMissing) return;
            string key = sourcePath + "|" + reason;
            if (!MissingLogCache.Add(key)) return;
            SubsystemLog.Warn("PcNpcVisual", $"{reason}: {sourcePath}");
        }
    }
}
