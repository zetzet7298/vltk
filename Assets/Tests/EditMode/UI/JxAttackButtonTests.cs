// -----------------------------------------------------------------------------
// VLTK Mobile — JX attack button EditMode tests (port of KgameWorldVN.cpp)
// Verifies: swing animation config (6 frame, 0.05s delay, loops=1, restore=true),
// swing duration (0.30s), frame stepping, tick lifecycle, press/release texture,
// attack frame path pattern. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxAttackButtonTests
    {
        // ---------------- Animation config ----------------

        [Test]
        public void AnimationConfig_MatchesSource()
        {
            // Nguồn: KgameWorldVN.cpp — attack_0..attack_5 (6 frame)
            Assert.AreEqual(6, JxAttackButtonState.FrameCount);
            Assert.AreEqual(0.05f, JxAttackButtonState.DelayPerUnit);
            Assert.AreEqual(1, JxAttackButtonState.Loops);
            Assert.IsTrue(JxAttackButtonState.RestoreOriginalFrame);
        }

        [Test]
        public void SwingDuration_IsFrameCountTimesDelay()
        {
            // 6 * 0.05 = 0.30s
            Assert.AreEqual(0.30f, JxAttackButtonState.SwingDuration, 0.0001f);
        }

        // ---------------- Textures ----------------

        [Test]
        public void MainButtonTextures_MatchSource()
        {
            Assert.AreEqual("KgameWorld/mr-1_new.png", JxAttackButtonState.TextureIdle);
            Assert.AreEqual("KgameWorld/mr-2_new.png", JxAttackButtonState.TexturePressed);
        }

        [Test]
        public void CurrentTexture_IdleWhenNotPressed()
        {
            var btn = new JxAttackButtonState();
            Assert.AreEqual(JxAttackButtonState.TextureIdle, btn.CurrentTexture);
        }

        [Test]
        public void CurrentTexture_PressedWhenHolding()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            Assert.AreEqual(JxAttackButtonState.TexturePressed, btn.CurrentTexture);
        }

        // ---------------- Frame path ----------------

        [Test]
        public void GetAttackFramePath_MatchesPattern()
        {
            Assert.AreEqual("KgameWorld/attack_0.png", JxAttackButtonState.GetAttackFramePath(0));
            Assert.AreEqual("KgameWorld/attack_5.png", JxAttackButtonState.GetAttackFramePath(5));
        }

        [Test]
        public void GetAttackFramePath_ClampsOutOfRange()
        {
            Assert.AreEqual("KgameWorld/attack_0.png", JxAttackButtonState.GetAttackFramePath(-1));
            Assert.AreEqual("KgameWorld/attack_5.png", JxAttackButtonState.GetAttackFramePath(99));
        }

        // ---------------- Press / Tick lifecycle ----------------

        [Test]
        public void Press_StartsSwingAtFrameZero()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            Assert.IsTrue(btn.IsPressed);
            Assert.IsTrue(btn.IsSwinging);
            Assert.AreEqual(0f, btn.SwingElapsed, 0.0001f);
            Assert.AreEqual(0, btn.CurrentFrame);
        }

        [Test]
        public void Tick_AdvancesFrame()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            btn.Tick(0.12f); // 0.12 / 0.05 = 2.4 → frame 2
            Assert.AreEqual(2, btn.CurrentFrame);
            Assert.IsTrue(btn.IsSwinging);
        }

        [Test]
        public void Tick_CompletesSwing_ReturnsTrue()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            bool done = btn.Tick(0.30f); // = duration
            Assert.IsTrue(done);
            Assert.IsFalse(btn.IsSwinging);
            Assert.AreEqual(-1, btn.CurrentFrame);
        }

        [Test]
        public void Tick_OverDuration_RestoresAndStops()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            btn.Tick(0.50f); // vượt duration
            // restore original frame → IsSwinging false, elapsed reset
            Assert.IsFalse(btn.IsSwinging);
            Assert.AreEqual(0f, btn.SwingElapsed, 0.0001f);
        }

        [Test]
        public void Tick_BeforeDuration_NotDone()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            bool done = btn.Tick(0.10f); // 2 frame, chưa xong
            Assert.IsFalse(done);
            Assert.IsTrue(btn.IsSwinging);
        }

        [Test]
        public void Tick_WhenNotSwinging_ReturnsFalse()
        {
            var btn = new JxAttackButtonState();
            Assert.IsFalse(btn.Tick(0.5f));
        }

        [Test]
        public void CurrentFrame_Idle_ReturnsNegativeOne()
        {
            var btn = new JxAttackButtonState();
            Assert.AreEqual(-1, btn.CurrentFrame);
        }

        [Test]
        public void CurrentFrame_LastFrame_BeforeCompletion()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            btn.Tick(0.25f); // 0.25/0.05 = 5 → clamp về frame 5 (cuối)
            Assert.AreEqual(5, btn.CurrentFrame);
            Assert.IsTrue(btn.IsSwinging); // 0.25 < 0.30 chưa xong
        }

        [Test]
        public void Release_ClearsPressed_ButSwingContinues()
        {
            var btn = new JxAttackButtonState();
            btn.Press();
            btn.Release();
            Assert.IsFalse(btn.IsPressed);
            // swing animation vẫn chạy đến hết (loops=1, không cancel)
            Assert.IsTrue(btn.IsSwinging);
            Assert.AreEqual(JxAttackButtonState.TextureIdle, btn.CurrentTexture);
        }
    }
}
