using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sprites
{
    public class SprHeader
    {
        public const uint SPR_SIGNATURE = 0x00525053; // "SPR\0"

        public uint signature;
        public ushort width;
        public ushort height;
        public ushort centerX;
        public ushort centerY;
        public ushort frames;
        public ushort colors;
        public ushort directions;
        public ushort interval;
        public ushort[] reserved = new ushort[6];

        public bool IsValid => (signature & 0x00FFFFFF) == SPR_SIGNATURE;
    }

    public class SprFrameOffset
    {
        public uint offset;
        public uint length;
    }

    public class SprFrame
    {
        public ushort width;
        public ushort height;
        public short offsetX;
        public short offsetY;
        public byte[] pixelData;
        public Color32[] rgbaPixels;
    }

    public class SprDecodeResult
    {
        public SprHeader header;
        public byte[] palette;        // RGB, 3 bytes per color
        public SprFrameOffset[] offsets;
        public SprFrame[] frames;
        public bool success;
        public string error;
    }

    public static class SprDecoder
    {
        public static SprDecodeResult Decode(byte[] data)
        {
            var result = new SprDecodeResult();

            if (data == null || data.Length < 32)
            {
                result.error = "Data too small for SPR header";
                return result;
            }

            try
            {
                using var ms = new MemoryStream(data);
                using var br = new BinaryReader(ms);

                // Parse header (32 bytes)
                var header = new SprHeader
                {
                    signature = br.ReadUInt32(),
                    width = br.ReadUInt16(),
                    height = br.ReadUInt16(),
                    centerX = br.ReadUInt16(),
                    centerY = br.ReadUInt16(),
                    frames = br.ReadUInt16(),
                    colors = br.ReadUInt16(),
                    directions = br.ReadUInt16(),
                    interval = br.ReadUInt16(),
                };
                for (int i = 0; i < 6; i++)
                    header.reserved[i] = br.ReadUInt16();

                if (!header.IsValid)
                {
                    result.error = $"Invalid SPR signature: 0x{header.signature:X8}";
                    return result;
                }

                result.header = header;

                // Read palette (3 bytes per color)
                int paletteSize = header.colors * 3;
                if (ms.Position + paletteSize > data.Length)
                {
                    result.error = "Palette exceeds data bounds";
                    return result;
                }

                result.palette = br.ReadBytes(paletteSize);

                // Read frame offset table (8 bytes per frame)
                int offsetTableSize = header.frames * 8;
                if (ms.Position + offsetTableSize > data.Length)
                {
                    result.error = "Offset table exceeds data bounds";
                    return result;
                }

                result.offsets = new SprFrameOffset[header.frames];
                long frameDataBase = ms.Position + offsetTableSize;

                for (int i = 0; i < header.frames; i++)
                {
                    result.offsets[i] = new SprFrameOffset
                    {
                        offset = br.ReadUInt32(),
                        length = br.ReadUInt32(),
                    };
                }

                // Decode each frame
                result.frames = new SprFrame[header.frames];
                for (int i = 0; i < header.frames; i++)
                {
                    var off = result.offsets[i];
                    long frameStart = frameDataBase + off.offset;

                    if (frameStart + off.length > data.Length)
                    {
                        SubsystemLog.Warn("SPR", $"Frame {i} out of bounds, skipping");
                        result.frames[i] = new SprFrame { width = 0, height = 0 };
                        continue;
                    }

                    ms.Position = frameStart;
                    var frameBlob = br.ReadBytes((int)off.length);
                    result.frames[i] = DecodeFrame(frameBlob, result.palette, header.colors);
                }

                result.success = true;
                return result;
            }
            catch (Exception ex)
            {
                result.error = $"Decode error: {ex.Message}";
                return result;
            }
        }

        private static SprFrame DecodeFrame(byte[] blob, byte[] palette, int colorCount)
        {
            var frame = new SprFrame();

            if (blob == null || blob.Length < 8)
                return frame;

            frame.width = (ushort)(blob[0] | (blob[1] << 8));
            frame.height = (ushort)(blob[2] | (blob[3] << 8));

            if (frame.width == 0 || frame.height == 0)
                return frame;

            frame.offsetX = (short)(blob[4] | (blob[5] << 8));
            frame.offsetY = (short)(blob[6] | (blob[7] << 8));

            int pixelCount = frame.width * frame.height;
            frame.rgbaPixels = new Color32[pixelCount];

            // Decode RLE-compressed rows
            int srcPos = 8;
            int totalPixels = frame.width * frame.height;

            // Bottom-up row order (matching PC source)
            for (int row = frame.height - 1; row >= 0; row--)
            {
                int rowBase = row * frame.width;
                int col = 0;

                while (col < frame.width && srcPos + 1 < blob.Length)
                {
                    byte runLength = blob[srcPos++];
                    byte alpha = blob[srcPos++];

                    if (alpha == 0)
                    {
                        col += runLength;
                        continue;
                    }

                    for (int r = 0; r < runLength && col < frame.width; r++)
                    {
                        if (srcPos >= blob.Length) break;

                        byte colorIndex = blob[srcPos++];
                        int palOff = colorIndex * 3;
                        byte red = palOff + 0 < palette.Length ? palette[palOff + 0] : (byte)0;
                        byte green = palOff + 1 < palette.Length ? palette[palOff + 1] : (byte)0;
                        byte blue = palOff + 2 < palette.Length ? palette[palOff + 2] : (byte)0;

                        frame.rgbaPixels[rowBase + col] = new Color32(red, green, blue, alpha);
                        col++;
                    }
                }
            }

            return frame;
        }

        public static Texture2D CreateTexture(SprFrame frame, bool linear = false)
        {
            if (frame == null || frame.width == 0 || frame.height == 0)
                return null;

            var tex = new Texture2D(frame.width, frame.height, TextureFormat.RGBA32, false, linear);
            tex.filterMode = FilterMode.Point;

            if (frame.rgbaPixels != null && frame.rgbaPixels.Length > 0)
                tex.SetPixels32(frame.rgbaPixels);

            tex.Apply();
            return tex;
        }

        public static Sprite CreateSprite(Texture2D tex, SprFrame frame, float pixelsPerUnit = 100f)
        {
            if (tex == null) return null;

            var pivot = new Vector2(
                frame.width > 0 ? 0.5f : 0f,
                frame.height > 0 ? 0.5f : 0f
            );

            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit);
        }
    }
}
