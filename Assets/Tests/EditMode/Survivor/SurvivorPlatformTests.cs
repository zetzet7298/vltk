// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorPlatformTests (ticket 42 self-check)
// Pure logic, KHÔNG scene/PlayMode (spec Testing Decisions):
//  - MonsterCapPolicy: EffectiveCap fail-closed (≤0→default, >MaxCap→trần),
//    CanSpawn boundary (at cap → false), Excess math, PickTrimIndices
//    front-first + boss exempt + không vượt excess.
//  - PerfBudget: inject dt trực tiếp (dt = clock), report đúng interval
//    boundary, avg/min/max đúng, reset window, interval ≤0 → fallback 5s,
//    dt âm clamp 0.
//  - SafeAreaUtil: full-screen → 0, notch → đúng cạnh, fail-closed zero screen.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorPlatformTests
    {
        // ------------------------------------------------------------------
        // MonsterCapPolicy — EffectiveCap (fail-closed)
        // ------------------------------------------------------------------

        [Test]
        public void EffectiveCap_Invalid_ReturnsDefault()
        {
            Assert.AreEqual(MonsterCapPolicy.DefaultCap, MonsterCapPolicy.EffectiveCap(0));
            Assert.AreEqual(MonsterCapPolicy.DefaultCap, MonsterCapPolicy.EffectiveCap(-5));
        }

        [Test]
        public void EffectiveCap_AboveMax_CappedToCeiling()
        {
            Assert.AreEqual(MonsterCapPolicy.MaxCap, MonsterCapPolicy.EffectiveCap(99999));
        }

        [Test]
        public void EffectiveCap_Valid_Passthrough()
        {
            Assert.AreEqual(50, MonsterCapPolicy.EffectiveCap(50));
        }

        // ------------------------------------------------------------------
        // MonsterCapPolicy — CanSpawn / Excess
        // ------------------------------------------------------------------

        [Test]
        public void CanSpawn_BelowCap_True()
        {
            Assert.IsTrue(MonsterCapPolicy.CanSpawn(79, 80));
        }

        [Test]
        public void CanSpawn_AtCap_False()
        {
            Assert.IsFalse(MonsterCapPolicy.CanSpawn(80, 80));
            Assert.IsFalse(MonsterCapPolicy.CanSpawn(120, 80));
        }

        [Test]
        public void CanSpawn_InvalidCap_FailClosed_DefaultBoundary()
        {
            // cap ≤ 0 → default 80: 80 active → không spawn
            Assert.IsFalse(MonsterCapPolicy.CanSpawn(80, 0));
            Assert.IsTrue(MonsterCapPolicy.CanSpawn(79, 0));
        }

        [Test]
        public void Excess_UnderCap_Zero()
        {
            Assert.AreEqual(0, MonsterCapPolicy.Excess(30, 80));
            Assert.AreEqual(0, MonsterCapPolicy.Excess(80, 80));
        }

        [Test]
        public void Excess_OverCap_Positive()
        {
            Assert.AreEqual(20, MonsterCapPolicy.Excess(100, 80));
        }

        // ------------------------------------------------------------------
        // MonsterCapPolicy — PickTrimIndices
        // ------------------------------------------------------------------

        [Test]
        public void PickTrim_UnderCap_Empty()
        {
            var r = MonsterCapPolicy.PickTrimIndices(30, 80);
            Assert.AreEqual(0, r.Count);
        }

        [Test]
        public void PickTrim_FrontFirst_StopsAtExcess()
        {
            // 5 active, cap 3 → trim 2: index 0,1 (kẻ sống lâu nhất trước)
            var r = MonsterCapPolicy.PickTrimIndices(5, 3);
            CollectionAssert.AreEqual(new List<int> { 0, 1 }, r);
        }

        [Test]
        public void PickTrim_SkipsExemptBoss()
        {
            // cap 3, 5 active, index 0 = boss → trim 1,2; không đụng boss
            var r = MonsterCapPolicy.PickTrimIndices(5, 3, i => i == 0);
            CollectionAssert.AreEqual(new List<int> { 1, 2 }, r);
        }

        [Test]
        public void PickTrim_NotEnoughNonExempt_TrimsFewer_FailClosed()
        {
            // cap 2, 4 active, 3 exempt → chỉ trim 1 (kẻ duy nhất), không vượt excess, không đụng exempt
            var r = MonsterCapPolicy.PickTrimIndices(4, 2, i => i != 1);
            CollectionAssert.AreEqual(new List<int> { 1 }, r);
        }

        // ------------------------------------------------------------------
        // PerfBudget — interval / boundary
        // ------------------------------------------------------------------

        [Test]
        public void Budget_NoReport_BeforeInterval()
        {
            var b = new PerfBudget(1f);
            Assert.IsNull(b.Tick(0.3f));
            Assert.IsNull(b.Tick(0.3f));
            Assert.IsNull(b.Tick(0.3f)); // 0.9 < 1.0
        }

        [Test]
        public void Budget_Report_AtBoundary()
        {
            var b = new PerfBudget(1f);
            for (int i = 0; i < 3; i++) Assert.IsNull(b.Tick(0.25f)); // 0.75 < 1.0
            var r = b.Tick(0.25f); // acc = 1.0 ≥ 1.0 → report ngay tại boundary
            Assert.IsNotNull(r);
            Assert.AreEqual(4, r.Value.Frames);
        }

        [Test]
        public void Budget_AvgMinMax_Values()
        {
            var b = new PerfBudget(0.05f);
            b.Tick(0.01f);
            b.Tick(0.02f);
            var r = b.Tick(0.03f); // acc 0.06 ≥ 0.05 → report 3 frames
            Assert.IsNotNull(r);
            Assert.AreEqual(3, r.Value.Frames);
            // dt là float (0.01f*1000 = 9.99999978) → tolerance 1e-5 cho giá trị ms
            Assert.AreEqual(20.0, r.Value.AvgMs, 1e-5);   // 60ms / 3
            Assert.AreEqual(10.0, r.Value.MinMs, 1e-5);
            Assert.AreEqual(30.0, r.Value.MaxMs, 1e-5);
        }

        [Test]
        public void Budget_ResetsAfterReport()
        {
            var b = new PerfBudget(0.05f);
            b.Tick(0.06f); // report 1 frame
            Assert.IsNull(b.Tick(0.04f)); // window mới, chưa đủ
            var r = b.Tick(0.04f); // 0.08 ≥ 0.05 → report 2 frames mới
            Assert.IsNotNull(r);
            Assert.AreEqual(2, r.Value.Frames, "window reset — không gộp frame cũ");
        }

        [Test]
        public void Budget_Callback_InvokedPerWindow()
        {
            int reports = 0;
            var b = new PerfBudget(0.05f, _ => reports++);
            for (int i = 0; i < 3; i++) b.Tick(0.06f); // 3 window
            Assert.AreEqual(3, reports);
        }

        [Test]
        public void Budget_InvalidInterval_Fallback5s()
        {
            var b = new PerfBudget(-1f);
            Assert.IsNull(b.Tick(1f));
            Assert.IsNull(b.Tick(1f));
            Assert.IsNull(b.Tick(1f));
            Assert.IsNull(b.Tick(1f));
            Assert.IsNotNull(b.Tick(1f)); // 5.0 ≥ 5 → report
        }

        [Test]
        public void Budget_NegativeDt_ClampedNoThrow()
        {
            var b = new PerfBudget(0.05f);
            Assert.IsNull(b.Tick(-0.5f)); // clamp 0, vẫn đếm frame
            Assert.IsNull(b.Tick(0.0f));
            var r = b.Tick(0.06f); // acc 0.06 ≥ 0.05 → report 3 frames (dt âm = 0)
            Assert.IsNotNull(r);
            Assert.AreEqual(3, r.Value.Frames);
            Assert.AreEqual(20.0, r.Value.AvgMs, 1e-5); // 60ms / 3 frames
            Assert.AreEqual(0.0, r.Value.MinMs, 1e-5);
            Assert.AreEqual(60.0, r.Value.MaxMs, 1e-5);
        }

        // ------------------------------------------------------------------
        // SafeAreaUtil — padding math
        // ------------------------------------------------------------------

        [Test]
        public void SafeArea_FullScreen_ZeroPadding()
        {
            var p = SafeAreaUtil.ComputePadding(new Rect(0, 0, 1080, 1920), new Vector2(1080, 1920));
            Assert.IsTrue(p.IsZero);
        }

        [Test]
        public void SafeArea_NotchTop_OnlyTopInset()
        {
            // screen 1080×1920, safeArea chừa 132px trên (notch)
            var p = SafeAreaUtil.ComputePadding(new Rect(0, 0, 1080, 1788), new Vector2(1080, 1920));
            Assert.AreEqual(0f, p.Left, 1e-6f);
            Assert.AreEqual(0f, p.Bottom, 1e-6f);
            Assert.AreEqual(0f, p.Right, 1e-6f);
            Assert.AreEqual(132f / 1920f, p.Top, 1e-6f);
        }

        [Test]
        public void SafeArea_LeftRightCutout_Insets()
        {
            // landscape-ish cutout 2 bên (test generic)
            var p = SafeAreaUtil.ComputePadding(new Rect(54, 0, 972, 1920), new Vector2(1080, 1920));
            Assert.AreEqual(54f / 1080f, p.Left, 1e-6f);
            Assert.AreEqual(54f / 1080f, p.Right, 1e-6f);
            Assert.AreEqual(0f, p.Bottom, 1e-6f);
            Assert.AreEqual(0f, p.Top, 1e-6f);
        }

        [Test]
        public void SafeArea_ZeroScreen_FailClosed_ZeroPadding()
        {
            var p = SafeAreaUtil.ComputePadding(new Rect(10, 10, 100, 100), Vector2.zero);
            Assert.IsTrue(p.IsZero);
        }

        [Test]
        public void SafeArea_OutOfRange_Clamped01()
        {
            // safeArea vượt hẳn screen (lỗi runtime) → inset âm clamp về 0
            var p = SafeAreaUtil.ComputePadding(new Rect(-100, -100, 3000, 3000), new Vector2(1080, 1920));
            Assert.IsTrue(p.IsZero);
        }

        [Test]
        public void SafeArea_PartiallyOverflow_ClampsOnlyNegative()
        {
            // xMax 1900 > 1080 → Right âm → 0; yMax 1900 < 1920 → Top = 20px hợp lệ
            var p = SafeAreaUtil.ComputePadding(new Rect(-100, -100, 2000, 2000), new Vector2(1080, 1920));
            Assert.AreEqual(0f, p.Left, 1e-6f);
            Assert.AreEqual(0f, p.Bottom, 1e-6f);
            Assert.AreEqual(0f, p.Right, 1e-6f);
            Assert.AreEqual(20f / 1920f, p.Top, 1e-6f);
        }
    }
}
