using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
    ///   3. StreamingAssets/Sprites/{JX path hash}.spr
    ///   4. Fallback to procedural sprite
    /// </summary>
    public class SprRuntimeService
    {
        private readonly string _spritesRoot;
        private readonly Dictionary<string, Sprite> _cache = new();
        private readonly Dictionary<string, Texture2D> _texCache = new();
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
        /// Resolve a sprite's decoded texture for a specific frame (cached). Returns null if
        /// the SPR cannot be found or decoded. Unlike <see cref="ResolveSprite"/>, this exposes
        /// the raw texture so callers can build sprites with their own pivot / pixelsPerUnit
        /// (the map renderer works in 1px = 1 world-unit screen space).
        /// </summary>
        public Texture2D ResolveTexture(string spriteName, int frameIndex = 0)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;

            string baseKey = SanitizeKey(spriteName);
            string texKey = frameIndex > 0 ? $"{baseKey}#{frameIndex}" : baseKey;
            if (_texCache.TryGetValue(texKey, out var cachedTex))
                return cachedTex;
            if (_missCache.Contains(texKey))
                return null;

            byte[] sprData = FindSprData(baseKey, spriteName);
            if (sprData == null)
            {
                _missCache.Add(texKey);
                return null;
            }

            var result = SprDecoder.Decode(sprData);
            if (!_diagnostics.ContainsKey(baseKey))
                _diagnostics[baseKey] = SprValidator.Validate(sprData, baseKey);
            if (!result.success || result.frames == null || result.frames.Length == 0)
            {
                _missCache.Add(texKey);
                return null;
            }

            SprFrame frame = null;
            if (frameIndex >= 0 && frameIndex < result.frames.Length &&
                result.frames[frameIndex] != null &&
                result.frames[frameIndex].width > 0 && result.frames[frameIndex].height > 0)
            {
                frame = result.frames[frameIndex];
            }
            else
            {
                for (int i = 0; i < result.frames.Length; i++)
                {
                    if (result.frames[i] != null && result.frames[i].width > 0 && result.frames[i].height > 0)
                    {
                        frame = result.frames[i];
                        break;
                    }
                }
            }
            if (frame == null)
            {
                _missCache.Add(texKey);
                return null;
            }

            var tex = SprDecoder.CreateTexture(frame);
            if (tex == null)
            {
                _missCache.Add(texKey);
                return null;
            }
            tex.name = $"SPRTEX_{texKey}";
            _texCache[texKey] = tex;
            return tex;
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
            foreach (var kvp in _texCache)
            {
                if (kvp.Value != null)
                    UnityEngine.Object.DestroyImmediate(kvp.Value);
            }
            _cache.Clear();
            _texCache.Clear();
            _missCache.Clear();
            _diagnostics.Clear();
        }

        // --- Internal ---

        private byte[] FindSprData(string sanitizedKey, string originalName)
        {
            originalName = originalName?.TrimEnd('\0') ?? "";
            foreach (var root in EnumerateSpriteRoots())
            {
                var data = FindSprDataInRoot(root, sanitizedKey, originalName);
                if (data != null)
                    return data;
            }

            return null;
        }

        private IEnumerable<string> EnumerateSpriteRoots()
        {
            if (!string.IsNullOrEmpty(_spritesRoot))
                yield return _spritesRoot;

            var streamingRoot = Path.GetDirectoryName(_spritesRoot);
            if (!string.IsNullOrEmpty(streamingRoot))
            {
                var generatedRoot = Path.Combine(streamingRoot, "Generated", "MapSprites");
                if (!string.Equals(generatedRoot, _spritesRoot, StringComparison.OrdinalIgnoreCase))
                    yield return generatedRoot;
                var generatedNpcRoot = Path.Combine(streamingRoot, "Generated", "NpcSprites");
                if (!string.Equals(generatedNpcRoot, _spritesRoot, StringComparison.OrdinalIgnoreCase))
                    yield return generatedNpcRoot;
            }
        }

        private static byte[] FindSprDataInRoot(string root, string sanitizedKey, string originalName)
        {
            if (string.IsNullOrEmpty(root))
                return null;

            var directPath = Path.Combine(root, $"{sanitizedKey}.spr");
            if (File.Exists(directPath))
                return File.ReadAllBytes(directPath);

            var nameKey = SanitizeKey(Path.GetFileNameWithoutExtension(originalName));
            if (nameKey != sanitizedKey)
            {
                var namePath = Path.Combine(root, $"{nameKey}.spr");
                if (File.Exists(namePath))
                    return File.ReadAllBytes(namePath);
            }

            string uidFromPath = ExtractUidFromPath(originalName);
            if (!string.IsNullOrEmpty(uidFromPath) && uidFromPath != sanitizedKey)
            {
                var uidPath = Path.Combine(root, $"{uidFromPath}.spr");
                if (File.Exists(uidPath))
                    return File.ReadAllBytes(uidPath);
            }

            string hashedUid = ComputePathUidHex(originalName);
            if (!string.IsNullOrEmpty(hashedUid) &&
                hashedUid != sanitizedKey &&
                hashedUid != uidFromPath)
            {
                var hashedPath = Path.Combine(root, $"{hashedUid}.spr");
                if (File.Exists(hashedPath))
                    return File.ReadAllBytes(hashedPath);
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
            var key = name.TrimEnd('\0').Replace('\\', '_').Replace('/', '_');
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

        public static string NormalizeResourcePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            var normalized = path.Trim().TrimEnd('\0').Replace('/', '\\');
            if (normalized.Length == 0) return "";
            if (!normalized.StartsWith("\\", StringComparison.Ordinal))
                normalized = "\\" + normalized;
            return normalized;
        }

        public static uint ComputePathUid(string path, string encodingName = "GB2312")
        {
            var normalized = NormalizeResourcePath(path);
            if (string.IsNullOrEmpty(normalized)) return 0;

            byte[] bytes;
            try
            {
                bytes = Encoding.GetEncoding(encodingName).GetBytes(normalized);
            }
            catch
            {
                bytes = Encoding.UTF8.GetBytes(normalized);
            }

            uint value = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                uint b = bytes[i];
                if (b >= 65 && b <= 90)
                    b += 32;

                uint index = (uint)(i + 1);
                value = ((value + index * b) % 0x8000000B) * 0xFFFFFFEF;
            }

            return value ^ 0x12345678;
        }

        public static string ComputePathUidHex(string path, string encodingName = "GB2312")
        {
            uint uid = ComputePathUid(path, encodingName);
            return uid == 0 ? null : uid.ToString("x8");
        }
    }
}
