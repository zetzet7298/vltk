using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    public sealed class SprFramePlaybackTests
    {
        private static SprFrame VisibleFrame() => new SprFrame
        {
            width = 2,
            height = 2,
            rgbaPixels = new[] { new Color32(1, 1, 1, 255), new Color32(), new Color32(), new Color32() },
        };

        [Test]
        public void MissilePlayback_UsesEveryDirectionBlockFromPcMetadata()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7 },
                SprFramePlayback.UsedFrameIndices(SprPlaybackMode.Missile, 8, 4));
        }

        [Test]
        public void MissilePlayback_MapsDirectionAndTickIntoPcFrameBlock()
        {
            Assert.AreEqual(7, SprFramePlayback.FrameIndex(SprPlaybackMode.Missile, 3, 1, 8, 4, 1));
            Assert.AreEqual(6, SprFramePlayback.FrameIndex(SprPlaybackMode.Missile, -1, 0, 8, 4, 1));
            Assert.AreEqual(1, SprFramePlayback.FrameIndex(SprPlaybackMode.Stationary, 3, 3, 8, 4, 2));
        }

        [Test]
        public void StationaryPlayback_UsesOnlyMetadataFrameRange()
        {
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 },
                SprFramePlayback.UsedFrameIndices(SprPlaybackMode.Stationary, 19, 1));
        }

        [Test]
        public void UsedFrameValidation_RequiresExplicitCanonicalEmptyAllowlist()
        {
            var frames = new SprFrame[2];
            frames[0] = new SprFrame { width = 1, height = 1, rgbaPixels = new Color32[1] };
            frames[1] = VisibleFrame();

            Assert.IsFalse(SprFramePlayback.TryValidateUsedFrames(frames, SprPlaybackMode.Stationary, 2, 1,
                null, out var error));
            StringAssert.Contains("not allowlisted", error);

            Assert.IsTrue(SprFramePlayback.TryValidateUsedFrames(frames, SprPlaybackMode.Stationary, 2, 1,
                new HashSet<int> { 0 }, out error), error);
        }

        [Test]
        public void UsedFrameValidation_RejectsDecodedRangeOverrunAndMalformedUsedFrame()
        {
            var oneFrame = new[] { VisibleFrame() };
            Assert.IsFalse(SprFramePlayback.TryValidateUsedFrames(oneFrame, SprPlaybackMode.Stationary, 2, 1,
                null, out var rangeError));
            StringAssert.Contains("outside decoded range", rangeError);

            var malformed = new[] { new SprFrame { width = 2, height = 2, rgbaPixels = new Color32[3] } };
            Assert.IsFalse(SprFramePlayback.TryValidateUsedFrames(malformed, SprPlaybackMode.Stationary, 1, 1,
                null, out var frameError));
            StringAssert.Contains("not playable", frameError);
        }
    }
}
