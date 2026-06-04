// -----------------------------------------------------------------------------
// VLTK Mobile — Mount horse body visual.
// PC npcres/item/equip/horse/horse{NNN}.spr is a single-frame 50x76 sprite
// representing the horse body that the rider sits on. Decoded at runtime
// from StreamingAssets/Sprites/{uid}.spr via SprRuntimeService.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime renderer cho thân ngựa (horse body) khi nhân vật cưỡi.
    /// Single-frame 50x76 sprite, no direction matrix — PC horse SPRs chỉ có 1 frame.
    /// Driven by the rider's <see cref="MalePlayerVisual"/> / <see cref="FemalePlayerVisual"/>
    /// direction so the horse always faces the same way as the rider.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HorseVisual : MonoBehaviour
    {
        [Header("SPR Source")]
        [Tooltip("Source path PC (back-slash form) the runtime hashes to find the staged uid.spr.")]
        public string sourcePath = @"spr\item\equip\horse\horse001.spr";
        [Tooltip("PC horse id (1,3,5,7,9 = blue, yellow, red, white, black). 0 means no horse.")]
        public int horseId = 1;

        [Header("Placement")]
        [Tooltip("Offset from the rider GameObject origin to where the horse body anchor sits.")]
        public Vector2 anchorOffset = new Vector2(0f, -20f);
        [Tooltip("PPU for the 50x76 horse body. 1 keeps it pixel-true to PC.")]
        public float pixelsPerUnit = 1f;
        public string spritesRootOverride;

        [Header("Diagnostics")]
        public bool logMissing = true;

        private SpriteRenderer _renderer;
        private Sprite _sprite;
        private Vector2 _spriteSize;

        private static readonly Dictionary<string, Sprite> SpriteCache = new();
        private static readonly HashSet<string> MissingLogCache = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCaches()
        {
            SpriteCache.Clear();
            MissingLogCache.Clear();
        }

        public bool HasSprite => _sprite != null;
        public Vector2 SpriteSize => _spriteSize;
        // PC horseres.txt maps 1/3/5/7/9 to the 5 common horse models.
        public static readonly int[] AvailableHorseIds = { 1, 3, 5, 7, 9 };

        /// <summary>Map PC horse id (1/3/5/7/9) to the corresponding SPR file.</summary>
        public static string SourcePathForHorseId(int horseId)
        {
            int id = horseId;
            if (id <= 0) id = 1;
            // Snap to one of the 5 known ids (1,3,5,7,9). 1→1, 3→3, 5→5, 7→7, 9→9, 2→3, 4→5, 6→7, 8→9, 10→1...
            int index = ((id - 1) / 2) % AvailableHorseIds.Length;
            int snap = AvailableHorseIds[Mathf.Abs(index)];
            return $@"spr\item\equip\horse\horse{snap:D3}.spr";
        }

        private void Awake()
        {
            try
            {
                _renderer = gameObject.GetComponent<SpriteRenderer>();
                if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
                if (_renderer == null)
                {
                    SubsystemLog.Warn("HorseVisual", "Could not get/add SpriteRenderer on " + name);
                    return;
                }
                _renderer.sortingLayerName = "Default";
                // Resolve sourcePath from horseId if explicitly set non-default.
                if (horseId > 0 && !string.IsNullOrEmpty(sourcePath) && sourcePath.EndsWith("horse001.spr"))
                    sourcePath = SourcePathForHorseId(horseId);
                LoadAndApply();
            }
            catch (System.Exception ex)
            {
                SubsystemLog.Warn("HorseVisual", $"Awake failed: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Re-decode the horse SPR. Call after changing <see cref="sourcePath"/>
        /// (e.g. switching from horse001 to horse003) or after a domain reload.
        /// </summary>
        public void LoadAndApply()
        {
            try
            {
                if (_renderer == null)
                {
                    _renderer = gameObject.GetComponent<SpriteRenderer>();
                    if (_renderer == null) _renderer = gameObject.AddComponent<SpriteRenderer>();
                }
                // Re-resolve from horseId if set.
                if (horseId > 0)
                    sourcePath = SourcePathForHorseId(horseId);
                _sprite = LoadSprite(sourcePath);
                if (_renderer != null)
                {
                    _renderer.sprite = _sprite;
                    if (_sprite != null)
                    {
                        _spriteSize = new Vector2(_sprite.rect.width, _sprite.rect.height);
                        _renderer.transform.localPosition = anchorOffset;
                    }
                }
            }
            catch (System.Exception ex)
            {
                SubsystemLog.Warn("HorseVisual", $"LoadAndApply failed: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Switch the horse color/breed. PC horseres.txt maps 1/3/5/7/9 to
        /// the 5 common horse models (blue, yellow, red, white, black).
        /// </summary>
        public void SetHorseId(int newHorseId)
        {
            horseId = newHorseId;
            sourcePath = SourcePathForHorseId(newHorseId);
            LoadAndApply();
        }

        /// <summary>
        /// Update the horse facing direction. PC horse SPRs are single-frame, so
        /// we just flip the X scale for the west-facing variants (dir 1-3).
        /// </summary>
        public void SetDirection(int dir)
        {
            bool faceWest = dir == 1 || dir == 2 || dir == 3;
            var s = _renderer.transform.localScale;
            s.x = faceWest ? -Mathf.Abs(s.x == 0 ? 1f : s.x) : Mathf.Abs(s.x == 0 ? 1f : s.x);
            _renderer.transform.localScale = s;
            // Horse sits behind the rider visually (sortingOrder is set by caller).
        }

        public void SetSortingOrder(int order)
        {
            if (_renderer != null) _renderer.sortingOrder = order;
        }

        private Sprite LoadSprite(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath)) return null;
            string root = string.IsNullOrEmpty(spritesRootOverride)
                ? Path.Combine(Application.streamingAssetsPath, "Sprites")
                : spritesRootOverride;
            string cacheKey = $"{root}|{sourcePath}|ppu={pixelsPerUnit:F3}";
            if (SpriteCache.TryGetValue(cacheKey, out var cached) && cached != null)
                return cached;

            byte[] data = ReadSprData(root, sourcePath);
            if (data == null)
            {
                if (logMissing) SubsystemLog.Warn("HorseVisual", $"SPR file not staged: {sourcePath}");
                return null;
            }

            var decoded = SprDecoder.Decode(data);
            if (!decoded.success || decoded.frames == null || decoded.frames.Length == 0)
            {
                if (logMissing) SubsystemLog.Warn("HorseVisual", $"Decode failed: {decoded.error ?? sourcePath}");
                return null;
            }

            var frame = decoded.frames[0];
            if (frame == null || frame.width == 0 || frame.height == 0) return null;
            var tex = SprDecoder.CreateTexture(frame);
            if (tex == null) return null;
            tex.name = $"HorseVisual_{Path.GetFileNameWithoutExtension(sourcePath)}";
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), pixelsPerUnit, 0, SpriteMeshType.FullRect);
            sprite.name = tex.name;
            SpriteCache[cacheKey] = sprite;
            SubsystemLog.Info("HorseVisual", $"Loaded {sourcePath}: {tex.width}x{tex.height}");
            return sprite;
        }

        private static byte[] ReadSprData(string spritesRoot, string sourcePath)
        {
            if (string.IsNullOrEmpty(spritesRoot) || string.IsNullOrEmpty(sourcePath)) return null;
            string uid = SprRuntimeService.ComputePathUidHex(sourcePath);
            if (!string.IsNullOrEmpty(uid))
            {
                string hashedPath = Path.Combine(spritesRoot, uid + ".spr");
                if (File.Exists(hashedPath)) return File.ReadAllBytes(hashedPath);
            }
            string fileName = Path.GetFileName(sourcePath.Replace('\\', Path.DirectorySeparatorChar));
            if (!string.IsNullOrEmpty(fileName))
            {
                string directPath = Path.Combine(spritesRoot, fileName);
                if (File.Exists(directPath)) return File.ReadAllBytes(directPath);
            }
            return null;
        }
    }
}
