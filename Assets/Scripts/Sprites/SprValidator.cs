using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sprites
{
    public class SprDiagnostic
    {
        public string sourcePath;
        public bool isValid;
        public string error;
        public int frameCount;
        public int width;
        public int height;
        public int directions;
        public int paletteColors;
        public float intervalMs;
        public List<string> warnings = new();

        public override string ToString()
        {
            if (!isValid)
                return $"[INVALID] {sourcePath}: {error}";
            return $"[OK] {sourcePath}: {width}x{height}, {frameCount} frames, {directions} dirs, {paletteColors} colors";
        }
    }

    public static class SprValidator
    {
        public static SprDiagnostic Validate(byte[] data, string sourcePath = "")
        {
            var diag = new SprDiagnostic { sourcePath = sourcePath };

            if (data == null || data.Length < 32)
            {
                diag.error = "Data too small for SPR header (min 32 bytes)";
                return diag;
            }

            var result = SprDecoder.Decode(data);
            if (!result.success)
            {
                diag.error = result.error;
                return diag;
            }

            diag.isValid = true;
            diag.frameCount = result.header.frames;
            diag.width = result.header.width;
            diag.height = result.header.height;
            diag.directions = result.header.directions;
            diag.paletteColors = result.header.colors;
            diag.intervalMs = result.header.interval;

            if (result.frames != null)
            {
                int emptyFrames = 0;
                for (int i = 0; i < result.frames.Length; i++)
                {
                    if (result.frames[i] == null || result.frames[i].width == 0)
                    {
                        emptyFrames++;
                        diag.warnings.Add($"Frame {i} is empty or failed to decode");
                    }
                }

                if (emptyFrames > 0 && emptyFrames < diag.frameCount)
                    diag.warnings.Add($"{emptyFrames}/{diag.frameCount} frames empty");

                if (emptyFrames == diag.frameCount)
                {
                    diag.isValid = false;
                    diag.error = "All frames empty";
                }
            }

            if (diag.directions > 0 && diag.frameCount > 0 && diag.frameCount % diag.directions != 0)
                diag.warnings.Add($"Frame count ({diag.frameCount}) not evenly divisible by directions ({diag.directions})");

            return diag;
        }

        public static SprDiagnostic ValidateFile(string path)
        {
            try
            {
                var data = File.ReadAllBytes(path);
                return Validate(data, Path.GetFileName(path));
            }
            catch (Exception ex)
            {
                return new SprDiagnostic
                {
                    sourcePath = path,
                    error = $"File read error: {ex.Message}"
                };
            }
        }
    }
}
