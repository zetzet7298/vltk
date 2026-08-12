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

                // Check if this is a size table (per-frame compressed or raw SPR)
                bool isSizeTable = false;
                if (header.frames > 0)
                {
                    if (result.offsets[0].offset != 0)
                    {
                        isSizeTable = true;
                    }
                    else
                    {
                        for (int i = 1; i < header.frames; i++)
                        {
                            if (result.offsets[i].offset < result.offsets[i - 1].offset)
                            {
                                isSizeTable = true;
                                break;
                            }
                        }
                    }
                }

                if (isSizeTable)
                {
                    uint currentOffset = 0;
                    for (int i = 0; i < header.frames; i++)
                    {
                        uint size = result.offsets[i].offset;
                        result.offsets[i].offset = currentOffset;
                        result.offsets[i].length = size;
                        currentOffset += size;
                    }
                }

                // Decode each frame
                result.frames = new SprFrame[header.frames];
                for (int i = 0; i < header.frames; i++)
                {
                    var off = result.offsets[i];
                    long frameStart = frameDataBase + off.offset;
                    long frameEnd = frameStart + off.length;

                    if (frameEnd > data.Length)
                    {
                        // Fallback for malformed PC SPR offset tables (e.g. training dummies
                        // enemy178/179/180/181_st.spr): the `offset` field actually stores the
                        // frame payload length (+4 bias for the per-frame header), and the
                        // `length` field is garbage. PC JX engine reads the rest of the file as
                        // a single contiguous frame. Recover it instead of dropping the frame.
                        long payloadAvail = data.Length - frameDataBase;
                        long guessLength = (long)off.offset - 4;
                        if (guessLength > 0 && guessLength <= payloadAvail)
                        {
                            SubsystemLog.Warn("SPR",
                                $"Frame {i} offset table malformed (offset={off.offset}, length={off.length}); "
                                + $"using contiguous fallback (start={frameDataBase}, length={guessLength})");
                            frameStart = frameDataBase;
                            frameEnd = frameStart + guessLength;
                        }
                        else
                        {
                            SubsystemLog.Warn("SPR", $"Frame {i} out of bounds, skipping");
                            result.frames[i] = new SprFrame { width = 0, height = 0 };
                            continue;
                        }
                    }

                    ms.Position = frameStart;
                    var frameBlob = br.ReadBytes((int)(frameEnd - frameStart));
                    result.frames[i] = DecodeFrame(frameBlob, result.palette, header.colors);
                }
                result.success = true;
                return result;
            }
            catch (Exception ex)
            {
                string msg = !string.IsNullOrEmpty(ex.Message) ? ex.Message : ex.GetType().Name;
                result.error = $"Decode error: {msg}";
                return result;
            }
        }

        private static SprFrame DecodeFrame(byte[] blob, byte[] palette, int colorCount)
        {
            var frame = new SprFrame();

            if (blob == null || blob.Length < 8)
                return frame;

            int startPos = 0;
            ushort w = (ushort)(blob[0] | (blob[1] << 8));
            ushort h = (ushort)(blob[2] | (blob[3] << 8));

            // Support 1-byte shift for per-frame raw/compressed SPRs
            if ((w == 0 || w > 2048 || h == 0 || h > 2048) && blob.Length > 8)
            {
                ushort wShift = (ushort)(blob[1] | (blob[2] << 8));
                ushort hShift = (ushort)(blob[3] | (blob[4] << 8));
                if (wShift > 0 && wShift <= 2048 && hShift > 0 && hShift <= 2048)
                {
                    startPos = 1;
                    w = wShift;
                    h = hShift;
                }
            }

            frame.width = w;
            frame.height = h;

            if (frame.width == 0 || frame.height == 0)
                return frame;

            frame.offsetX = (short)(blob[startPos + 4] | (blob[startPos + 5] << 8));
            frame.offsetY = (short)(blob[startPos + 6] | (blob[startPos + 7] << 8));

            // fwidth and fheight are ushort (max 65535 each) — multiplying as int can overflow.
            // Skip pathological frames (>4M pixels) instead of crashing the whole decode.
            long pixelCount = (long)frame.width * (long)frame.height;
            if (pixelCount > 0xFFFFFFL)
            {
                frame.width = 0; frame.height = 0;
                return frame;
            }
            frame.rgbaPixels = new Color32[(int)pixelCount];

            // Decode RLE-compressed rows
            int srcPos = startPos + 8;
            int totalPixels = frame.width * frame.height;

            // Top-down row order (matching PC source payload ordering decoded to Unity bottom-up SetPixels32 format)
            for (int row = 0; row < frame.height; row++)
            {
                int rowBase = (frame.height - 1 - row) * frame.width;
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
                        // PC SPR palettes use 256 colors; SPR colorIndex is a byte (0-255) but
                        // header.colors can be < 256 — clamp and mask to prevent IndexOutOfRangeException
                        // (which surfaces as an empty ex.Message in the outer catch).
                        byte red = 0, green = 0, blue = 0;
                        if (colorIndex < colorCount && colorCount > 0)
                        {
                            int palOff = colorIndex * 3;
                            if (palOff + 2 < palette.Length)
                            {
                                red = palette[palOff + 0];
                                green = palette[palOff + 1];
                                blue = palette[palOff + 2];
                            }
                        }

                        frame.rgbaPixels[rowBase + col] = new Color32(red, green, blue, alpha);
                        col++;
                    }
                }
            }

            return frame;
        }

        // Max texture dimension Unity allows on the active graphics device.
        // SPR frames can declare huge dimensions (e.g. 49k×65k) which fail Texture2D ctor.
        // Skip such frames entirely — they are usually "shadow" or "ambient" effect textures
        // with no visible pixel data, never rendered as part of an NPC sprite.
        // Unity 6's default max texture dimension is 16384 on desktop GPUs.
        // PC JX engine SPRs often declare very large dims (49k×65k) for shadow/ambient tiles;
        // we skip those but still load any frame ≤ 16384 — needed so all 8 NPC directions
        // resolve to a usable sprite (not just one frame in each direction).
        private const int MAX_SPR_TEXTURE_DIM = 16384;

        public static Texture2D CreateTexture(SprFrame frame, bool linear = false)
        {
            if (frame == null || frame.width == 0 || frame.height == 0)
                return null;
            if (frame.width > MAX_SPR_TEXTURE_DIM || frame.height > MAX_SPR_TEXTURE_DIM)
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
