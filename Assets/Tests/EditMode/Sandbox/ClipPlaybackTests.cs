using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M2.5 — Character Sprite Clip playback tests. Frame advance from decoded
    /// clip (AC#1), direction switching (AC#2), stable pivot/offset (AC#3), and
    /// incomplete-clip diagnostics (AC#4).
    /// </summary>
    public class ClipPlaybackTests
    {
        private SpriteClipDefinition MakeClip(int framesPerDir, int directions, float rate = 10f,
            bool fullOffsets = true)
        {
            int total = framesPerDir * directions;
            Vector2[] offsets = null;
            if (fullOffsets)
            {
                offsets = new Vector2[total];
                for (int i = 0; i < total; i++) offsets[i] = new Vector2(i, -i);
            }
            return new SpriteClipDefinition
            {
                frameCount = framesPerDir,
                directionCount = directions,
                frameRate = rate,
                actionName = "walk",
                pivot = new Vector2(0.5f, 0f),
                frameOffsets = offsets,
            };
        }

        // --- AC#1: frame advance from decoded frames ---

        [Test]
        public void Tick_AdvancesFrameAtFrameRate()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 1, rate: 10f));

            Assert.AreEqual(0, svc.CurrentFrameInDirection);
            svc.Tick(0.1f); // 1 frame at 10fps
            Assert.AreEqual(1, svc.CurrentFrameInDirection);
            svc.Tick(0.2f); // +2 frames
            Assert.AreEqual(3, svc.CurrentFrameInDirection);
        }

        [Test]
        public void Frame_LoopsWithinDirection()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 3, directions: 1, rate: 10f));
            svc.Tick(0.3f); // exactly 3 frames → wraps to 0
            Assert.AreEqual(0, svc.CurrentFrameInDirection);
            svc.Tick(0.1f);
            Assert.AreEqual(1, svc.CurrentFrameInDirection);
        }

        [Test]
        public void AtlasFrameIndex_AccountsForDirection()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 4, rate: 10f));
            svc.SetDirection(2);
            svc.Tick(0.1f); // frame 1 in direction
            // direction 2 * 4 frames + frame 1 = 9
            Assert.AreEqual(9, svc.CurrentAtlasFrameIndex);
        }

        // --- AC#2: direction switching ---

        [Test]
        public void SetDirection_ChangesDirection_WhenMultiDirectional()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 8, rate: 10f));
            Assert.IsTrue(svc.SetDirection(5));
            Assert.AreEqual(5, svc.CurrentDirection);
        }

        [Test]
        public void SetDirection_Wraps()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 8, rate: 10f));
            svc.SetDirection(10); // 10 % 8 = 2
            Assert.AreEqual(2, svc.CurrentDirection);
            svc.SetDirection(-1); // wraps to 7
            Assert.AreEqual(7, svc.CurrentDirection);
        }

        [Test]
        public void SetDirection_SingleDirectionClip_Ignored()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 1, rate: 10f));
            Assert.IsFalse(svc.SetDirection(3));
            Assert.AreEqual(0, svc.CurrentDirection);
        }

        // --- AC#3: stable pivot/offset ---

        [Test]
        public void CurrentPivotOffset_CombinesPivotAndFrameOffset()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 1, rate: 10f));
            // frame 0 offset = (0,0) → pivot (0.5,0)
            Assert.AreEqual(new Vector2(0.5f, 0f), svc.CurrentPivotOffset());
            svc.Tick(0.2f); // frame 2 → offset (2,-2)
            Assert.AreEqual(new Vector2(2.5f, -2f), svc.CurrentPivotOffset());
        }

        [Test]
        public void CurrentPivotOffset_NoOffsets_FallsBackToPivot()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 1, rate: 10f, fullOffsets: false));
            Assert.AreEqual(new Vector2(0.5f, 0f), svc.CurrentPivotOffset());
        }

        // --- AC#4: incomplete clip diagnostics ---

        [Test]
        public void Diagnose_CompleteClip_IsValid()
        {
            var svc = new ClipPlaybackService();
            svc.SetClip(MakeClip(framesPerDir: 4, directions: 4, rate: 10f));
            var d = svc.Diagnose();
            Assert.IsTrue(d.isComplete);
            Assert.AreEqual(16, d.expectedFrames);
            Assert.AreEqual(16, d.availableFrames);
            Assert.AreEqual(0, d.missingFrames);
            Assert.AreEqual(SpriteValidationStatus.Valid, d.status);
        }

        [Test]
        public void Diagnose_IncompleteClip_FlagsMissingFrames()
        {
            var svc = new ClipPlaybackService();
            var clip = MakeClip(framesPerDir: 4, directions: 4, rate: 10f);
            // Drop frames: only 10 of 16 present.
            clip.frameOffsets = new Vector2[10];
            svc.SetClip(clip);

            LogAssert.Expect(LogType.Warning, "[ClipPlayback] Incomplete clip 'walk': 10/16 frames");
            var d = svc.Diagnose();
            Assert.IsFalse(d.isComplete);
            Assert.AreEqual(6, d.missingFrames);
            Assert.AreEqual(SpriteValidationStatus.Partial, d.status);
        }

        [Test]
        public void Diagnose_NoFrames_StatusMissingFrames()
        {
            var svc = new ClipPlaybackService();
            var clip = MakeClip(framesPerDir: 4, directions: 2, rate: 10f, fullOffsets: false);
            svc.SetClip(clip);

            LogAssert.Expect(LogType.Warning, "[ClipPlayback] Incomplete clip 'walk': 0/8 frames");
            var d = svc.Diagnose();
            Assert.AreEqual(SpriteValidationStatus.MissingFrames, d.status);
            Assert.AreEqual(8, d.missingFrames);
        }

        [Test]
        public void NoClip_SafeDefaults()
        {
            var svc = new ClipPlaybackService();
            Assert.AreEqual(0, svc.CurrentFrameInDirection);
            Assert.AreEqual(0, svc.CurrentAtlasFrameIndex);
            Assert.AreEqual(Vector2.zero, svc.CurrentPivotOffset());
            Assert.DoesNotThrow(() => svc.Tick(1f));
            Assert.AreEqual(SpriteValidationStatus.Unknown, svc.Diagnose().status);
        }
    }
}
