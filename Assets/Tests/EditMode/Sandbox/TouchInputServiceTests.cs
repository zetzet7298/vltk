using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M6.1 — Touch Controls tests. Tap-to-move (AC#1), joystick move vector (AC#2),
    /// skill button intent (AC#3), pinch zoom clamp (AC#4), and touch-target scaling
    /// (AC#5).
    /// </summary>
    public class TouchInputServiceTests
    {
        // --- AC#1: tap to move ---

        [Test]
        public void Tap_OnGround_RequestsMoveToWorld()
        {
            var svc = new TouchInputService { ScreenToWorld = p => p * 0.1f };
            var result = svc.Tap(new Vector2(100, 200), overUi: false);
            Assert.IsTrue(result.moveRequested);
            Assert.IsFalse(result.isUiHit);
            Assert.AreEqual(new Vector2(10, 20), result.worldTarget);
        }

        [Test]
        public void Tap_OnUi_Consumed_NoMove()
        {
            var svc = new TouchInputService();
            var result = svc.Tap(new Vector2(50, 50), overUi: true);
            Assert.IsTrue(result.isUiHit);
            Assert.IsFalse(result.moveRequested);
        }

        // --- AC#2: virtual joystick ---

        [Test]
        public void Joystick_InsideDeadZone_NoMove()
        {
            var svc = new TouchInputService { JoystickDeadZone = 0.2f };
            Assert.AreEqual(Vector2.zero, svc.JoystickToMove(new Vector2(0.1f, 0f)));
        }

        [Test]
        public void Joystick_FullDeflection_ReturnsUnitDirection()
        {
            var svc = new TouchInputService { JoystickDeadZone = 0.15f };
            var move = svc.JoystickToMove(new Vector2(1f, 0f));
            Assert.AreEqual(1f, move.magnitude, 0.001f); // full deflection → magnitude 1
            Assert.AreEqual(1f, move.x, 0.001f);
        }

        [Test]
        public void Joystick_PartialDeflection_ScalesPastDeadZone()
        {
            var svc = new TouchInputService { JoystickDeadZone = 0.15f };
            // magnitude 0.575 = midpoint between deadzone 0.15 and 1.0 → scaled ~0.5.
            var move = svc.JoystickToMove(new Vector2(0.575f, 0f));
            Assert.AreEqual(0.5f, move.magnitude, 0.01f);
        }

        [Test]
        public void Joystick_PreservesDirection()
        {
            var svc = new TouchInputService { JoystickDeadZone = 0.1f };
            var move = svc.JoystickToMove(new Vector2(0f, -1f));
            Assert.Less(move.y, 0f);
            Assert.AreEqual(0f, move.x, 0.001f);
        }

        // --- AC#3: skill button ---

        [Test]
        public void SkillButton_ProducesCastIntent()
        {
            var svc = new TouchInputService();
            var intent = svc.SkillButton(2);
            Assert.IsTrue(intent.requested);
            Assert.AreEqual(2, intent.slot);
        }

        // --- AC#4: pinch zoom ---

        [Test]
        public void PinchZoom_ClampsToRange()
        {
            var svc = new TouchInputService();
            // Huge pinch-out (cur >> prev) → zoom decreases, clamps at min.
            float zoom = svc.PinchZoom(prevDistance: 10f, curDistance: 1000f, currentZoom: 5f,
                minZoom: 2f, maxZoom: 20f, sensitivity: 0.1f);
            Assert.AreEqual(2f, zoom);
        }

        [Test]
        public void PinchZoom_PinchIn_IncreasesZoomValue()
        {
            var svc = new TouchInputService();
            // prev > cur (fingers move together) → positive delta → larger zoom value.
            float zoom = svc.PinchZoom(prevDistance: 200f, curDistance: 100f, currentZoom: 5f,
                minZoom: 2f, maxZoom: 20f, sensitivity: 0.01f);
            Assert.AreEqual(6f, zoom, 0.001f); // 5 + (200-100)*0.01
        }

        // --- AC#5: touch target scaling ---

        [Test]
        public void TouchTarget_ScalesWithDpi()
        {
            var svc = new TouchInputService { ReferenceDpi = 160f, MinTouchTargetPoints = 44f };
            // At reference DPI, target equals min.
            Assert.AreEqual(44f, svc.TouchTargetPixels(160f), 0.01f);
            // At 2x DPI (320), pixel size doubles.
            Assert.AreEqual(88f, svc.TouchTargetPixels(320f), 0.01f);
        }

        [Test]
        public void TouchTarget_NeverBelowMinimum()
        {
            var svc = new TouchInputService { ReferenceDpi = 160f, MinTouchTargetPoints = 44f };
            // Very low DPI still respects the minimum.
            Assert.GreaterOrEqual(svc.TouchTargetPixels(80f), 44f);
        }
    }
}
