using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M2.3 — Camera Controller tests. Follow tracking, GM unlock + pan, zoom
    /// clamped to min/max, and reset to the follow target (AC#1–AC#4).
    /// </summary>
    public class CameraRigTests
    {
        private CameraRigService MakeRig()
            => new CameraRigService(initialTarget: new Vector2(5, 5), zoom: 5f, minZoom: 2f, maxZoom: 20f);

        // --- AC#1: follow tracks the target ---

        [Test]
        public void Follow_FocusTracksTarget()
        {
            var rig = MakeRig();
            Assert.AreEqual(CameraMode.Follow, rig.Mode);
            rig.SetFollowTarget(new Vector2(8, 9));
            Assert.AreEqual(new Vector2(8, 9), rig.Focus);
        }

        [Test]
        public void Follow_AfterUnlock_TargetDoesNotMoveFocus()
        {
            var rig = MakeRig();
            rig.Unlock();
            rig.SetFollowTarget(new Vector2(8, 9));
            // In free mode the focus stays where it was, not snapped to target.
            Assert.AreEqual(new Vector2(5, 5), rig.Focus);
            Assert.AreEqual(new Vector2(8, 9), rig.FollowTarget);
        }

        // --- AC#2: GM unlock + pan ---

        [Test]
        public void Pan_InFreeMode_MovesFocus()
        {
            var rig = MakeRig();
            rig.Unlock();
            Assert.AreEqual(CameraMode.Free, rig.Mode);
            Assert.IsTrue(rig.Pan(new Vector2(3, -2)));
            Assert.AreEqual(new Vector2(8, 3), rig.Focus);
        }

        [Test]
        public void Pan_InFollowMode_IsIgnored()
        {
            var rig = MakeRig();
            Assert.IsFalse(rig.Pan(new Vector2(3, -2)));
            Assert.AreEqual(new Vector2(5, 5), rig.Focus);
        }

        // --- AC#3: zoom clamped to min/max ---

        [Test]
        public void ZoomBy_ClampsToRange()
        {
            var rig = MakeRig();
            rig.ZoomBy(100f);
            Assert.AreEqual(20f, rig.Zoom);   // clamped to max
            rig.ZoomBy(-1000f);
            Assert.AreEqual(2f, rig.Zoom);    // clamped to min
        }

        [Test]
        public void ZoomBy_WithinRange_Applies()
        {
            var rig = MakeRig();
            Assert.AreEqual(8f, rig.ZoomBy(3f));
            Assert.AreEqual(6f, rig.ZoomBy(-2f));
        }

        [Test]
        public void SetZoom_ClampsToRange()
        {
            var rig = MakeRig();
            Assert.AreEqual(2f, rig.SetZoom(-5f));
            Assert.AreEqual(20f, rig.SetZoom(999f));
            Assert.AreEqual(10f, rig.SetZoom(10f));
        }

        // --- AC#4: reset returns to follow target ---

        [Test]
        public void Reset_ReturnsToFollowTargetAndMode()
        {
            var rig = MakeRig();
            rig.Unlock();
            rig.Pan(new Vector2(20, 20));
            rig.SetFollowTarget(new Vector2(7, 7));

            rig.Reset();
            Assert.AreEqual(CameraMode.Follow, rig.Mode);
            Assert.AreEqual(new Vector2(7, 7), rig.Focus);
        }

        [Test]
        public void EnableFollow_SnapsFocusToTarget()
        {
            var rig = MakeRig();
            rig.Unlock();
            rig.Pan(new Vector2(10, 10));
            rig.EnableFollow();
            Assert.AreEqual(rig.FollowTarget, rig.Focus);
        }

        [Test]
        public void Constructor_ClampsInitialZoom()
        {
            var rig = new CameraRigService(Vector2.zero, zoom: 100f, minZoom: 2f, maxZoom: 15f);
            Assert.AreEqual(15f, rig.Zoom);
        }
    }
}
