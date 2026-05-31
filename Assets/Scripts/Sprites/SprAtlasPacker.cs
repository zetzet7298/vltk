using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Sprites
{
    /// <summary>
    /// M0.8 AC#4 — SPR atlas packing pipeline.
    /// Packs decoded SPR frames into a Texture2D atlas and registers the result
    /// in the asset registry so frames can be loaded through the Asset Registry.
    ///
    /// Design constraints (from spec):
    /// - Must NOT bake gameplay rules.
    /// - Atlas entries must be retrievable via IAssetRegistry.Resolve().
    /// - Works at edit/conversion time, not in the gameplay loop.
    /// </summary>
    public static class SprAtlasPacker
    {
        public const int MAX_ATLAS_SIZE = 4096;

        /// <summary>
        /// Result from packing one SPR file into an atlas.
        /// </summary>
        public class AtlasPackResult
        {
            public bool success;
            public string error;
            public Texture2D atlas;
            public Rect[] frameRects;      // UV rects in atlas for each frame
            public Vector2[] framePivots;  // Normalized pivot per frame
            public SpriteClipDefinition clipDefinition;
            public string atlasKey;        // Key used to register in AssetRegistry
        }

        /// <summary>
        /// Pack all frames of a decoded SPR into a single atlas Texture2D and
        /// build a SpriteClipDefinition. Registers the result in the registry.
        /// </summary>
        public static AtlasPackResult Pack(
            SprDecodeResult decoded,
            SourceAssetId sourceId,
            IAssetRegistry registry = null)
        {
            var result = new AtlasPackResult();

            if (decoded == null || !decoded.success)
            {
                result.error = "Cannot pack: decode result is null or failed";
                return result;
            }

            if (decoded.frames == null || decoded.frames.Length == 0)
            {
                result.error = "No frames to pack";
                return result;
            }

            try
            {
                // Collect non-empty frames
                var validFrames = new List<(int index, SprFrame frame)>();
                for (int i = 0; i < decoded.frames.Length; i++)
                {
                    var f = decoded.frames[i];
                    if (f != null && f.width > 0 && f.height > 0)
                        validFrames.Add((i, f));
                }

                if (validFrames.Count == 0)
                {
                    result.error = "All frames are empty";
                    return result;
                }

                // Determine atlas grid size (square packing, power-of-2 aligned)
                int frameW = decoded.header.width;
                int frameH = decoded.header.height;
                int totalFrames = decoded.frames.Length;

                int cols = Mathf.CeilToInt(Mathf.Sqrt(totalFrames));
                int rows = Mathf.CeilToInt((float)totalFrames / cols);

                int atlasW = NextPow2(cols * frameW);
                int atlasH = NextPow2(rows * frameH);

                // Clamp to max atlas size
                while (atlasW > MAX_ATLAS_SIZE) { cols--; rows = Mathf.CeilToInt((float)totalFrames / cols); atlasW = NextPow2(cols * frameW); }
                while (atlasH > MAX_ATLAS_SIZE) { rows--; atlasH = NextPow2(rows * frameH); }

                var atlas = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);
                atlas.filterMode = FilterMode.Point;

                // Fill with transparent
                var clearPixels = new Color32[atlasW * atlasH];
                atlas.SetPixels32(clearPixels);

                result.frameRects = new Rect[totalFrames];
                result.framePivots = new Vector2[totalFrames];

                for (int i = 0; i < totalFrames; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    int x = col * frameW;
                    int y = atlasH - (row + 1) * frameH; // top-down to bottom-up

                    var frame = decoded.frames[i];
                    if (frame != null && frame.width > 0 && frame.rgbaPixels != null)
                    {
                        // Blit frame pixels into atlas
                        int blitW = Mathf.Min(frame.width, frameW);
                        int blitH = Mathf.Min(frame.height, frameH);
                        atlas.SetPixels32(x, y, blitW, blitH, TrimPixels(frame.rgbaPixels, frame.width, blitW, blitH));

                        result.framePivots[i] = new Vector2(
                            frameW > 0 ? (float)decoded.header.centerX / frameW : 0.5f,
                            frameH > 0 ? (float)decoded.header.centerY / frameH : 0.5f
                        );
                    }

                    result.frameRects[i] = new Rect(
                        (float)x / atlasW,
                        (float)y / atlasH,
                        (float)frameW / atlasW,
                        (float)frameH / atlasH
                    );
                }

                atlas.Apply();
                result.atlas = atlas;

                // Build SpriteClipDefinition (M0.8 AC#3 — offsets preserved)
                var frameOffsets = new Vector2[totalFrames];
                for (int i = 0; i < totalFrames; i++)
                {
                    var f = decoded.frames[i];
                    if (f != null)
                        frameOffsets[i] = new Vector2(f.offsetX, f.offsetY);
                }

                var clipDef = new SpriteClipDefinition
                {
                    sourceSpriteId = sourceId,
                    frameCount = totalFrames,
                    frameRate = decoded.header.interval > 0 ? 1000f / decoded.header.interval : 10f,
                    directionCount = decoded.header.directions,
                    pivot = decoded.header.centerX > 0 || decoded.header.centerY > 0
                        ? new Vector2(decoded.header.centerX, decoded.header.centerY)
                        : new Vector2(frameW * 0.5f, frameH * 0.5f),
                    frameOffsets = frameOffsets,
                    paletteInfo = $"indexed256,colors={decoded.header.colors}",
                    alphaMode = "rle",
                    renderStyle = "spriteRenderer",
                    validationStatus = SpriteValidationStatus.Valid,
                };

                string atlasKey = sourceId?.ToKey() ?? $"atlas_{Guid.NewGuid():N}";
                clipDef.atlasRef = atlasKey;
                result.clipDefinition = clipDef;
                result.atlasKey = atlasKey;
                result.success = true;

                // M0.8 AC#4: Register in asset registry so frames can be loaded through it
                if (registry != null && sourceId != null)
                {
                    var entry = new AssetRegistryEntry
                    {
                        sourceId = sourceId,
                        artifactType = ArtifactType.SpriteAtlas,
                        unityAssetPath = atlasKey,
                        loadMode = LoadMode.TestFixture,
                        status = AssetStatus.Available,
                    };
                    registry.Register(entry);
                    SubsystemLog.Info("SprAtlas", $"Registered atlas '{atlasKey}' in registry ({totalFrames} frames)");
                }

                SubsystemLog.Info("SprAtlas",
                    $"Packed {validFrames.Count}/{totalFrames} frames into {atlasW}x{atlasH} atlas ({cols}x{rows} grid)");

                return result;
            }
            catch (Exception ex)
            {
                result.error = $"Atlas pack error: {ex.Message}";
                SubsystemLog.Error("SprAtlas", result.error);
                return result;
            }
        }

        private static Color32[] TrimPixels(Color32[] src, int srcWidth, int dstWidth, int dstHeight)
        {
            var dst = new Color32[dstWidth * dstHeight];
            for (int row = 0; row < dstHeight; row++)
            {
                int srcBase = row * srcWidth;
                int dstBase = row * dstWidth;
                for (int col = 0; col < dstWidth; col++)
                {
                    int si = srcBase + col;
                    dst[dstBase + col] = si < src.Length ? src[si] : new Color32(0, 0, 0, 0);
                }
            }
            return dst;
        }

        private static int NextPow2(int v)
        {
            if (v <= 0) return 1;
            int p = 1;
            while (p < v) p <<= 1;
            return p;
        }
    }
}
