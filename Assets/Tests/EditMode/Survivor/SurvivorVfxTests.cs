// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorVfxTests
// Ticket 34 self-check (pure logic, không scene/PlayMode — spec Testing Decisions):
//  - Fail-closed staged gate: uid rỗng → KHÔNG resolve, không bịa path; resolve
//    miss → proxy (false); staged → sprite (true).
//  - Flash lifecycle: add/tick/expire, progress 1→0, multiple slots độc lập.
//  - Burst particles (death/levelup) = scene glue + feel → manual checklist,
//    không test PlayMode (pattern SurvivorP1LogicTests).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorVfxTests
    {
        // ------------------------------------------------------------------
        // fail-closed staged-sprite gate (VfxStagedSprite)
        // ------------------------------------------------------------------

        [Test]
        public void StagedSprite_EmptyUid_ReturnsFalse_NoResolve()
        {
            bool resolverCalled = false;
            bool ok = VfxStagedSprite.TryResolve("", _ =>
            {
                resolverCalled = true;
                return null;
            }, out var sprite);

            Assert.IsFalse(ok, "uid rỗng → không render (fail-closed)");
            Assert.IsNull(sprite);
            Assert.IsFalse(resolverCalled, "không được gọi resolver khi uid rỗng — không bịa path");
        }

        [Test]
        public void StagedSprite_NullUid_ReturnsFalse()
        {
            bool ok = VfxStagedSprite.TryResolve(null, _ => null, out var sprite);
            Assert.IsFalse(ok);
            Assert.IsNull(sprite);
        }

        [Test]
        public void StagedSprite_ResolveMiss_ReturnsFalse_ProxyFallback()
        {
            // uid hợp lệ nhưng runtime file thiếu → resolve trả null → proxy (fail-closed, không crash)
            bool ok = VfxStagedSprite.TryResolve("deadbeef", _ => null, out var sprite);
            Assert.IsFalse(ok, "resolve miss → không render SPR");
            Assert.IsNull(sprite);
        }

        [Test]
        public void StagedSprite_Staged_ReturnsSprite()
        {
            var sp = MakeSprite();
            bool ok = VfxStagedSprite.TryResolve("deadbeef", _ => sp, out var sprite);
            Assert.IsTrue(ok, "uid staged → render SPR");
            Assert.AreSame(sp, sprite);
        }

        [Test]
        public void StagedSprite_NullResolver_ReturnsFalse()
        {
            bool ok = VfxStagedSprite.TryResolve("deadbeef", null, out var sprite);
            Assert.IsFalse(ok);
            Assert.IsNull(sprite);
        }

        // ------------------------------------------------------------------
        // flash lifecycle (VfxFlashTimeline — pure)
        // ------------------------------------------------------------------

        [Test]
        public void FlashTimeline_Add_Active_ThenExpire()
        {
            var tl = new VfxFlashTimeline();
            int token = tl.Add(0.5f);

            Assert.AreEqual(1, tl.ActiveCount);
            Assert.IsTrue(tl.IsActive(token));

            tl.Tick(0.2f);
            Assert.IsTrue(tl.IsActive(token), "chưa hết hạn vẫn active");
            Assert.AreEqual(0.6f, tl.Progress(token), 1e-3f, "progress giảm theo thời gian");

            tl.Tick(0.4f); // tổng 0.6 > 0.5 → hết hạn
            Assert.IsFalse(tl.IsActive(token));
            Assert.AreEqual(0, tl.ActiveCount);
            Assert.AreEqual(0f, tl.Progress(token), "token đã drop → 0");
        }

        [Test]
        public void FlashTimeline_Progress_MonotonicDown()
        {
            var tl = new VfxFlashTimeline();
            int token = tl.Add(1f);
            float prev = tl.Progress(token);
            for (int i = 0; i < 5; i++)
            {
                tl.Tick(0.1f);
                float cur = tl.Progress(token);
                Assert.Less(cur, prev, "progress phải giảm dần");
                prev = cur;
            }
            Assert.IsTrue(tl.Progress(token) > 0f, "chưa hết hạn");
        }

        [Test]
        public void FlashTimeline_MultipleSlots_Independent()
        {
            var tl = new VfxFlashTimeline();
            int short_ = tl.Add(0.2f);
            int long_ = tl.Add(1f);

            tl.Tick(0.5f);
            Assert.IsFalse(tl.IsActive(short_), "slot ngắn hết hạn");
            Assert.IsTrue(tl.IsActive(long_), "slot dài vẫn active");
            Assert.AreEqual(1, tl.ActiveCount);
        }

        [Test]
        public void FlashTimeline_ZeroDuration_ClampedActive()
        {
            var tl = new VfxFlashTimeline();
            int token = tl.Add(0f);
            Assert.IsTrue(tl.IsActive(token), "duration 0 → clamp 0.01s, không crash");
            tl.Tick(0.05f);
            Assert.IsFalse(tl.IsActive(token));
        }

        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private static Sprite MakeSprite()
        {
            var tx = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            var sp = Sprite.Create(tx, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 4f);
            return sp;
        }
    }
}
