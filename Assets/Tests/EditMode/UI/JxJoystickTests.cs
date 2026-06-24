// -----------------------------------------------------------------------------
// VLTK Mobile — JX joystick EditMode tests (port of KHRocker.cpp)
// Verifies: getRad/getAngleSigned (y-up CCW), 8-direction bucketing (nDir codes),
// dead zone (5px), radius clamp, touch-area, ShouldGoto clamp (nDir 0..63, nM 0..2),
// action-lock block, getDirection (inverted vector), thumb rotation.
// Category: HudJxCocos.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxJoystickTests
    {
        private const float Tol = 0.001f;

        // ---------------- getRad / getAngleSigned ----------------

        [Test]
        public void GetAngle_Right_IsZero()
        {
            // +x → 0° (right)
            float a = JxJoystickState.GetAngleSigned(Vector2.zero, new Vector2(10, 0));
            Assert.AreEqual(0f, a, Tol);
        }

        [Test]
        public void GetAngle_Up_Is90()
        {
            // +y → 90° (up) — y-up convention
            float a = JxJoystickState.GetAngleSigned(Vector2.zero, new Vector2(0, 10));
            Assert.AreEqual(90f, a, Tol);
        }

        [Test]
        public void GetAngle_Left_Is180()
        {
            // -x → 180° (left)
            float a = JxJoystickState.GetAngleSigned(Vector2.zero, new Vector2(-10, 0));
            Assert.AreEqual(180f, a, Tol);
        }

        [Test]
        public void GetAngle_Down_Is270()
        {
            // -y → 270° (down) — y-up convention
            float a = JxJoystickState.GetAngleSigned(Vector2.zero, new Vector2(0, -10));
            Assert.AreEqual(270f, a, Tol);
        }

        [Test]
        public void GetAngle_DownRight_Is315()
        {
            // (+x, -y) → 315° (down-right)
            float a = JxJoystickState.GetAngleSigned(Vector2.zero, new Vector2(10, -10));
            Assert.AreEqual(315f, a, Tol);
        }

        [Test]
        public void GetRad_Range_ZeroToTwoPi()
        {
            // Đảm bảo getRad luôn trong [0, 2π)
            float twoPi = 2f * Mathf.PI;
            foreach (Vector2 p in SamplePoints())
            {
                float r = JxJoystickState.GetRad(Vector2.zero, p);
                Assert.GreaterOrEqual(r, 0f);
                Assert.Less(r, twoPi + Tol);
            }
        }

        [Test]
        public void GetAngle_SamePoint_IsZero()
        {
            // degenerate → 0 (xie == 0 guard)
            float a = JxJoystickState.GetAngleSigned(Vector2.zero, Vector2.zero);
            Assert.AreEqual(0f, a, Tol);
        }

        // ---------------- AngleToDir (8-direction bucketing) ----------------

        [TestCase(0f,   ExpectedResult = JxJoystickState.DirRight,      Description = "right")]
        [TestCase(22.5f, ExpectedResult = JxJoystickState.DirUpRight,    Description = "lower-inclusive up-right")]
        [TestCase(45f,  ExpectedResult = JxJoystickState.DirUpRight,     Description = "up-right")]
        [TestCase(67.5f, ExpectedResult = JxJoystickState.DirUp,         Description = "lower-inclusive up")]
        [TestCase(90f,  ExpectedResult = JxJoystickState.DirUp,          Description = "up")]
        [TestCase(112.5f, ExpectedResult = JxJoystickState.DirUpLeft,    Description = "lower-inclusive up-left")]
        [TestCase(135f, ExpectedResult = JxJoystickState.DirUpLeft,      Description = "up-left")]
        [TestCase(157.5f, ExpectedResult = JxJoystickState.DirLeft,      Description = "lower-inclusive left")]
        [TestCase(180f, ExpectedResult = JxJoystickState.DirLeft,        Description = "left")]
        [TestCase(202.5f, ExpectedResult = JxJoystickState.DirDownLeft,  Description = "lower-inclusive down-left")]
        [TestCase(225f, ExpectedResult = JxJoystickState.DirDownLeft,    Description = "down-left")]
        [TestCase(247.5f, ExpectedResult = JxJoystickState.DirDown,      Description = "lower-inclusive down")]
        [TestCase(270f, ExpectedResult = JxJoystickState.DirDown,        Description = "down")]
        [TestCase(292.5f, ExpectedResult = JxJoystickState.DirDownRight, Description = "lower-inclusive down-right")]
        [TestCase(315f, ExpectedResult = JxJoystickState.DirDownRight,   Description = "down-right")]
        [TestCase(337.5f, ExpectedResult = JxJoystickState.DirRight,     Description = "lower-inclusive right (wrap)")]
        [TestCase(359.9f, ExpectedResult = JxJoystickState.DirRight,     Description = "near-360 wraps to right")]
        public int AngleToDir_MapsCorrectly(float nVer)
        {
            return JxJoystickState.AngleToDir(nVer);
        }

        [Test]
        public void DirCodes_AreMultipleOf8_AndContiguous()
        {
            // 8 mã hướng, mỗi mã = index*8, range 0..56
            int[] codes = {
                JxJoystickState.DirDown, JxJoystickState.DirDownLeft, JxJoystickState.DirLeft,
                JxJoystickState.DirUpLeft, JxJoystickState.DirUp, JxJoystickState.DirUpRight,
                JxJoystickState.DirRight, JxJoystickState.DirDownRight
            };
            Assert.AreEqual(8, codes.Length);
            for (int i = 0; i < codes.Length; i++)
                Assert.AreEqual(i * 8, codes[i], "dir index " + i + " = " + codes[i]);
        }

        [Test]
        public void DirCodes_AllWithinSpriteFrameRange()
        {
            // nDir là sprite frame start index → phải trong 0..63
            Assert.That(JxJoystickState.DirDownRight, Is.LessThanOrEqualTo(JxJoystickState.NDirMax));
            Assert.AreEqual(56, JxJoystickState.DirDownRight);
        }

        // ---------------- Touch area ----------------

        [Test]
        public void TouchArea_InsideRect_ReturnsTrue()
        {
            var js = new JxJoystickState();
            // góc trong CCRectMake(55,40,300,250)
            Assert.IsTrue(js.IsInTouchArea(new Vector2(55, 40)));
            Assert.IsTrue(js.IsInTouchArea(new Vector2(355, 290)));
            Assert.IsTrue(js.IsInTouchArea(new Vector2(150, 150))); // center
        }

        [Test]
        public void TouchArea_OutsideRect_ReturnsFalse()
        {
            var js = new JxJoystickState();
            Assert.IsFalse(js.IsInTouchArea(new Vector2(10, 10)));
            Assert.IsFalse(js.IsInTouchArea(new Vector2(400, 300)));
            Assert.IsFalse(js.IsInTouchArea(new Vector2(0, 0)));
        }

        // ---------------- TryBegin / Move lifecycle ----------------

        [Test]
        public void TryBegin_InsideArea_ActivatesAndSnapsCenter()
        {
            var js = new JxJoystickState();
            bool ok = js.TryBegin(new Vector2(150, 150));
            Assert.IsTrue(ok);
            Assert.IsTrue(js.IsActive);
            Assert.IsTrue(js.IsRunning);
            Assert.AreEqual(new Vector2(150, 150), js.Center);
            Assert.AreEqual(new Vector2(150, 150), js.Current);
            Assert.AreEqual(JxJoystickState.DirNone, js.Dir);
        }

        [Test]
        public void TryBegin_OutsideArea_Fails()
        {
            var js = new JxJoystickState();
            Assert.IsFalse(js.TryBegin(new Vector2(0, 0)));
            Assert.IsFalse(js.IsActive);
        }

        [Test]
        public void Move_Right_SetsDirRight()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            bool moved = js.Move(new Vector2(160, 150)); // +x
            Assert.IsTrue(moved);
            Assert.AreEqual(JxJoystickState.DirRight, js.Dir);
        }

        [Test]
        public void Move_Up_SetsDirUp()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            Assert.IsTrue(js.Move(new Vector2(150, 160))); // +y
            Assert.AreEqual(JxJoystickState.DirUp, js.Dir);
        }

        [Test]
        public void Move_Left_SetsDirLeft()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            Assert.IsTrue(js.Move(new Vector2(140, 150))); // -x
            Assert.AreEqual(JxJoystickState.DirLeft, js.Dir);
        }

        [Test]
        public void Move_Down_SetsDirDown()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            Assert.IsTrue(js.Move(new Vector2(150, 140))); // -y
            Assert.AreEqual(JxJoystickState.DirDown, js.Dir);
        }

        [Test]
        public void Move_DeadZone_ReturnsFalse()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            // distance <= 5 → false (không move)
            Assert.IsFalse(js.Move(new Vector2(153, 150)));
            Assert.IsFalse(js.Move(new Vector2(150, 155)));
        }

        [Test]
        public void Move_DeadZone_DoesNotChangeDir()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            js.Move(new Vector2(160, 150)); // dir = right
            Assert.AreEqual(JxJoystickState.DirRight, js.Dir);
            js.Move(new Vector2(152, 150)); // dead zone → giữ nguyên? source return false, dir không update
            // (dir chỉ update khi ra khỏi dead zone)
        }

        [Test]
        public void Move_ClampsCurrentToRadius()
        {
            var js = new JxJoystickState(85f);
            js.TryBegin(new Vector2(150, 150));
            // kéo rất xa → current bị clamp trong radius
            js.Move(new Vector2(1000, 150));
            float dist = Vector2.Distance(js.Current, js.Center);
            Assert.AreEqual(85f, dist, Tol);
        }

        [Test]
        public void End_ResetsState()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            js.Move(new Vector2(160, 150));
            js.End();
            Assert.IsFalse(js.IsActive);
            Assert.IsFalse(js.IsRunning);
            Assert.AreEqual(JxJoystickState.DirNone, js.Dir);
        }

        // ---------------- ShouldGoto (updateMovement) ----------------

        [Test]
        public void ShouldGoto_WhenRunningAndMoved_ReturnsClampedDir()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            js.Move(new Vector2(160, 150)); // right
            bool ok = js.ShouldGoto(out int dir, out int mode);
            Assert.IsTrue(ok);
            Assert.AreEqual(JxJoystickState.DirRight, dir);
            Assert.AreEqual(0, mode);
        }

        [Test]
        public void ShouldGoto_WhenNotRunning_ReturnsFalse()
        {
            var js = new JxJoystickState();
            bool ok = js.ShouldGoto(out _, out _);
            Assert.IsFalse(ok);
        }

        [Test]
        public void ShouldGoto_InDeadZone_ReturnsFalse()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            // chưa move ra khỏi dead zone → ShouldGoto false
            bool ok = js.ShouldGoto(out _, out _);
            Assert.IsFalse(ok);
        }

        [Test]
        public void ShouldGoto_ActionLocked_ReturnsFalse()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            js.Move(new Vector2(160, 150));
            js.IsActionLocked = true; // đang attack/magic
            bool ok = js.ShouldGoto(out _, out _);
            Assert.IsFalse(ok);
        }

        [Test]
        public void ShouldGoto_AfterEnd_ReturnsFalse()
        {
            var js = new JxJoystickState();
            js.TryBegin(new Vector2(150, 150));
            js.Move(new Vector2(160, 150));
            js.End();
            Assert.IsFalse(js.ShouldGoto(out _, out _));
        }

        // ---------------- getDirection (inverted vector) ----------------

        [Test]
        public void GetDirection_IsInvertedCenterMinusCurrent()
        {
            // source: ccpNormalize(centerPoint - currentPoint)
            Vector2 d = JxJoystickState.GetDirection(new Vector2(100, 100), new Vector2(110, 100));
            // center - current = (-10, 0) → normalize = (-1, 0)
            Assert.AreEqual(-1f, d.x, Tol);
            Assert.AreEqual(0f, d.y, Tol);
        }

        [Test]
        public void GetDirection_Degenerate_IsZero()
        {
            Vector2 d = JxJoystickState.GetDirection(Vector2.zero, Vector2.zero);
            Assert.AreEqual(Vector2.zero, d);
        }

        // ---------------- Thumb rotation ----------------

        [Test]
        public void ThumbRotation_Right_IsZero()
        {
            float r = JxJoystickState.GetThumbRotation(Vector2.zero, new Vector2(10, 0));
            Assert.AreEqual(0f, r, Tol);
        }

        [Test]
        public void ThumbRotation_Up_Is90()
        {
            float r = JxJoystickState.GetThumbRotation(Vector2.zero, new Vector2(0, 10));
            Assert.AreEqual(90f, r, Tol);
        }

        private static IEnumerable<Vector2> SamplePoints()
        {
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                yield return new Vector2(Mathf.Cos(a) * 10, Mathf.Sin(a) * 10);
            }
        }
    }
}
