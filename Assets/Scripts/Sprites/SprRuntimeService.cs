using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sprites
{
    /// <summary>
    /// Runtime SPR sprite loader: finds SPR files by name, decodes them,
    /// caches the resulting Unity sprites, and serves lookups for MapRenderer.
    ///
    /// Search order for a requested sprite name (e.g. "image\tree\abc" or "00002d56"):
    ///   1. StreamingAssets/Sprites/{uid}.spr  (pre-converted flat files)
    ///   2. StreamingAssets/Sprites/{sanitizedName}.spr
    ///   3. Fallback to procedural sprite
    /// </summary>
    public class SprRuntimeService
    {
        private readonly string _spritesRoot;
        private readonly Dictionary<string, Sprite> _cache = new();
        private readonly Dictionary<string, SprDiagnostic> _diagnostics = new();
        private readonly HashSet<string> _missCache = new();

        public int CacheCount => _cache.Count;
        public int MissCount => _missCache.Count;
        public int DiagnosticCount => _diagnostics.Count;

        public SprRuntimeService(string streamingAssetsRoot = null)
        {
            _spritesRoot = streamingAssetsRoot
                ?? Path.Combine(Application.streamingAssetsPath, "Sprites");
        }

        /// <summary>
        /// Resolve a sprite by its source name (imageName from ground/builtin data)
        /// or SPR UID. Returns a Unity Sprite if found and decoded, null otherwise.
        /// Results are cached; subsequent calls for the same name are O(1).
        /// </summary>
        public Sprite ResolveSprite(string spriteName, int fallbackWidth = 32, int fallbackHeight = 32)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;

            string key = SanitizeKey(spriteName);

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            if (_missCache.Contains(key))
                return null;

            // Try to find the SPR file on disk
            byte[] sprData = FindSprData(key, spriteName);
            if (sprData == null)
            {
                _missCache.Add(key);
                SubsystemLog.Warn("SprRuntime", $"SPR not found for '{spriteName}' (key={key})");
                return null;
            }

            // Decode and create sprite
            var sprite = DecodeToSprite(sprData, key);
            if (sprite != null)
            {
                _cache[key] = sprite;
                SubsystemLog.Info("SprRuntime", $"Loaded SPR '{key}' ({sprite.texture.width}x{sprite.texture.height})");
                return sprite;
            }

            _missCache.Add(key);
            return null;
        }

        /// <summary>
        /// Resolve with fallback: returns real SPR sprite if available,
        /// otherwise returns a procedural colored sprite.
        /// </summary>
        public Sprite ResolveSpriteOrDefault(string spriteName, int width = 32, int height = 32)
        {
            var sprite = ResolveSprite(spriteName, width, height);
            if (sprite != null) return sprite;
            return CreateFallbackSprite(spriteName, width, height);
        }

        /// <summary>
        /// Preload a batch of SPR files and warm the cache.
        /// </summary>
        public int PreloadAll()
        {
            if (!Directory.Exists(_spritesRoot))
            {
                SubsystemLog.Warn("SprRuntime", $"Sprites directory not found: {_spritesRoot}");
                return 0;
            }

            int loaded = 0;
            foreach (var file in Directory.GetFiles(_spritesRoot, "*.spr"))
            {
                string key = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
                if (_cache.ContainsKey(key)) continue;

                try
                {
                    var data = File.ReadAllBytes(file);
                    var sprite = DecodeToSprite(data, key);
                    if (sprite != null)
                    {
                        _cache[key] = sprite;
                        loaded++;
                    }
                }
                catch (Exception ex)
                {
                    SubsystemLog.Warn("SprRuntime", $"Failed to preload {file}: {ex.Message}");
                }
            }

            SubsystemLog.Info("SprRuntime", $"Preloaded {loaded} SPR sprites from {_spritesRoot}");
            return loaded;
        }

        /// <summary>
        /// Get diagnostic info for a previously resolved sprite.
        /// </summary>
        public SprDiagnostic GetDiagnostic(string spriteName)
        {
            var key = SanitizeKey(spriteName);
            _diagnostics.TryGetValue(key, out var diag);
            return diag;
        }

        /// <summary>
        /// Get all diagnostics collected so far.
        /// </summary>
        public IReadOnlyDictionary<string, SprDiagnostic> GetAllDiagnostics() => _diagnostics;

        /// <summary>
        /// Clear cache (useful when switching maps to free memory).
        /// </summary>
        public void ClearCache()
        {
            foreach (var kvp in _cache)
            {
                if (kvp.Value != null && kvp.Value.texture != null)
                    UnityEngine.Object.DestroyImmediate(kvp.Value.texture);
            }
            _cache.Clear();
            _missCache.Clear();
            _diagnostics.Clear();
        }

        // --- Internal ---

        private byte[] FindSprData(string sanitizedKey, string originalName)
        {
            // Strategy 1: exact UID match (e.g. "00002d56")
            var directPath = Path.Combine(_spritesRoot, $"{sanitizedKey}.spr");
            if (File.Exists(directPath))
                return File.ReadAllBytes(directPath);

            // Strategy 2: try the original name sanitized (e.g. "image_tree_abc" from "image\tree\abc")
            var nameKey = SanitizeKey(Path.GetFileNameWithoutExtension(originalName));
            if (nameKey != sanitizedKey)
            {
                var namePath = Path.Combine(_spritesRoot, $"{nameKey}.spr");
                if (File.Exists(namePath))
                    return File.ReadAllBytes(namePath);
            }

            // Strategy 3: try matching by UID portion at end of path
            // Many sprite names are like "image\effect\00002d56.spr"
            string uidFromPath = ExtractUidFromPath(originalName);
            if (!string.IsNullOrEmpty(uidFromPath) && uidFromPath != sanitizedKey)
            {
                var uidPath = Path.Combine(_spritesRoot, $"{uidFromPath}.spr");
                if (File.Exists(uidPath))
                    return File.ReadAllBytes(uidPath);
            }

            return null;
        }

        private Sprite DecodeToSprite(byte[] sprData, string key)
        {
            var result = SprDecoder.Decode(sprData);

            var diag = SprValidator.Validate(sprData, key);
            _diagnostics[key] = diag;

            if (!result.success || result.frames == null || result.frames.Length == 0)
            {
                SubsystemLog.Warn("SprRuntime", $"Decode failed for '{key}': {result.error}");
                return null;
            }

            // Find first valid frame
            SprFrame bestFrame = null;
            for (int i = 0; i < result.frames.Length; i++)
            {
                if (result.frames[i] != null && result.frames[i].width > 0 && result.frames[i].height > 0)
                {
                    bestFrame = result.frames[i];
                    break;
                }
            }

            if (bestFrame == null)
            {
                SubsystemLog.Warn("SprRuntime", $"No valid frames in '{key}'");
                return null;
            }

            var tex = SprDecoder.CreateTexture(bestFrame);
            if (tex == null) return null;

            tex.name = $"SPR_{key}";

            // Use the SPR header center as pivot (character anchor point)
            float pivotX = result.header.width > 0 ? (float)result.header.centerX / result.header.width : 0.5f;
            float pivotY = result.header.height > 0 ? (float)result.header.centerY / result.header.height : 0.5f;
            var pivot = new Vector2(pivotX, pivotY);

            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 100f);
            sprite.name = $"SPR_{key}";
            return sprite;
        }

        private Sprite CreateFallbackSprite(string name, int w, int h)
        {
            int hash = string.IsNullOrEmpty(name) ? 0 : name.GetHashCode();
            float hue = Mathf.Abs(hash % 360) / 360f;
            Color col = Color.HSVToRGB(hue, 0.4f, 0.8f);

            var tex = new Texture2D(w, h);
            var cols = new Color[w * h];
            for (int i = 0; i < cols.Length; i++)
            {
                int x = i % w;
                int y = i / w;
                if (x == 0 || x == w - 1 || y == 0 || y == h - 1)
                    cols[i] = new Color(col.r * 0.5f, col.g * 0.5f, col.b * 0.5f, 0.8f);
                else
                    cols[i] = new Color(col.r, col.g, col.b, 0.8f);
            }
            tex.SetPixels(cols);
            tex.Apply();
            tex.filterMode = FilterMode.Point;

            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
        }

        private static string SanitizeKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            // Normalize to lowercase, strip path separators and extensions
            var key = name.Replace('\\', '_').Replace('/', '_');
            // Remove file extension if present
            int dotIdx = key.LastIndexOf('.');
            if (dotIdx > 0)
                key = key.Substring(0, dotIdx);
            key = key.TrimStart('_');
            return key.ToLowerInvariant();
        }

        private static string ExtractUidFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            // Normalize backslashes to forward slashes for cross-platform
            var normalized = path.Replace('\\', '/');
            string filename = Path.GetFileNameWithoutExtension(normalized);
            // Check if it looks like a hex UID (8 chars)
            if (filename.Length == 8)
            {
                bool allHex = true;
                foreach (char c in filename)
                {
                    if (!Uri.IsHexDigit(c)) { allHex = false; break; }
                }
                if (allHex) return filename.ToLowerInvariant();
            }
            return null;
        }
    }
}
