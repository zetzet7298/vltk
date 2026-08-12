using System;
using System.Collections.Generic;

namespace VLTK.Sprites
{
    public enum SprPlaybackMode
    {
        Missile,
        Stationary,
    }

    /// <summary>
    /// PC SPR frame-range contract for data-driven skill playback.
    /// KMissleRes::Draw uses metadata totalFrames/directions, not SPR header frame count.
    /// </summary>
    public static class SprFramePlayback
    {
        public const int MaxTextureDimension = 16384;

        public static int FrameIndex(SprPlaybackMode mode, int spriteDirection, int lifeTick,
            int totalFrames, int directions, int intervalTicks)
        {
            int safeDirections = Math.Max(1, directions);
            int framesPerDirection = FramesPerDirection(totalFrames, safeDirections);
            int localFrame = (Math.Max(0, lifeTick) / Math.Max(1, intervalTicks)) % framesPerDirection;
            return mode == SprPlaybackMode.Missile
                ? Mod(spriteDirection, safeDirections) * framesPerDirection + localFrame
                : localFrame;
        }

        public static int[] UsedFrameIndices(SprPlaybackMode mode, int totalFrames, int directions)
        {
            int safeDirections = Math.Max(1, directions);
            int framesPerDirection = FramesPerDirection(totalFrames, safeDirections);
            int count = mode == SprPlaybackMode.Missile
                ? framesPerDirection * safeDirections
                : framesPerDirection;
            var indices = new int[count];
            for (int i = 0; i < indices.Length; i++) indices[i] = i;
            return indices;
        }

        public static bool TryValidateUsedFrames(SprFrame[] frames, SprPlaybackMode mode,
            int totalFrames, int directions, ISet<int> canonicalEmptyFrames, out string error)
        {
            foreach (int index in UsedFrameIndices(mode, totalFrames, directions))
            {
                if (frames == null || index >= frames.Length)
                {
                    error = $"used frame {index} outside decoded range";
                    return false;
                }

                var frame = frames[index];
                if (IsCanonicalEmpty(frame))
                {
                    if (canonicalEmptyFrames != null && canonicalEmptyFrames.Contains(index)) continue;
                    error = $"used frame {index} is canonical-empty but not allowlisted";
                    return false;
                }

                if (!IsPlayable(frame))
                {
                    error = $"used frame {index} is not playable";
                    return false;
                }
            }

            error = null;
            return true;
        }

        public static bool IsCanonicalEmpty(SprFrame frame)
        {
            return frame != null && frame.width == 1 && frame.height == 1 && !HasVisiblePixels(frame);
        }

        public static bool IsPlayable(SprFrame frame)
        {
            if (frame == null || frame.width == 0 || frame.height == 0 ||
                frame.width > MaxTextureDimension || frame.height > MaxTextureDimension)
                return false;
            long pixelCount = (long)frame.width * frame.height;
            return frame.rgbaPixels != null && frame.rgbaPixels.Length == pixelCount && HasVisiblePixels(frame);
        }

        private static int FramesPerDirection(int totalFrames, int directions)
        {
            if (totalFrames <= 0) throw new ArgumentOutOfRangeException(nameof(totalFrames));
            return Math.Max(1, totalFrames / Math.Max(1, directions));
        }

        private static int Mod(int value, int divisor)
        {
            int result = value % divisor;
            return result < 0 ? result + divisor : result;
        }

        private static bool HasVisiblePixels(SprFrame frame)
        {
            if (frame.rgbaPixels == null) return false;
            for (int i = 0; i < frame.rgbaPixels.Length; i++)
                if (frame.rgbaPixels[i].a != 0) return true;
            return false;
        }
    }
}
