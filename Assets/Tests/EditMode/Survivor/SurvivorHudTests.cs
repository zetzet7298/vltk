// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorHudTests
// Ticket 37 self-check (pure logic, không scene — spec Testing Decisions):
//  - SurvivorBanner: wave change → "Đợt N"; boss transition → "BOSS" (ưu tiên);
//    hết window tự ẩn; waveIndex 0 → không banner (fail-closed)
//  - SurvivorHudLogic.BarFill: clamp 0..1, max ≤ 0 → 0 (fail-closed)
//  - FormatTime: mm:ss, âm → 00:00
//  - FormatGameOver: labels i18n (vi / en) + fallback lang (en→vi→VN map) +
//    kills 0 → ẩn dòng
// Logic thuần — không scene, không PlayMode (pattern SurvivorP1LogicTests).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorHudTests
    {
        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------

        private static SurvivorTextEntry E(string key, string text) => new SurvivorTextEntry { key = key, text = text };

        private static SurvivorText Bundled()
        {
            var t = new SurvivorText();
            t.RegisterBundle("vi", new List<SurvivorTextEntry>
            {
                E("survivor.gameover.result", "Kết quả"),
                E("survivor.hud.level", "Cấp"),
                E("survivor.hud.timer", "Thời gian"),
                E("survivor.hud.kills", "Tiêu diệt"),
                E("survivor.card.title", "LÊN CẤP"),
                E("survivor.only.vi", "chỉ có vi"),
            });
            t.RegisterBundle("en", new List<SurvivorTextEntry>
            {
                E("survivor.gameover.result", "Result"),
                E("survivor.hud.level", "Lv"),
                E("survivor.hud.timer", "Time"),
                E("survivor.hud.kills", "Kills"),
                E("survivor.card.title", "LEVEL UP"),
            });
            return t;
        }

        // ------------------------------------------------------------------
        // SurvivorBanner
        // ------------------------------------------------------------------

        [Test]
        public void Banner_WaveChange_ShowsWaveText()
        {
            var b = new SurvivorBanner();
            b.Poll(1, false, 0.016f);
            Assert.AreEqual("Đợt 1", b.Current);
        }

        [Test]
        public void Banner_BossTransition_ShowsBoss()
        {
            var b = new SurvivorBanner();
            b.Poll(0, false, 0.016f);
            b.Poll(0, true, 0.016f);
            Assert.AreEqual("BOSS", b.Current);
        }

        [Test]
        public void Banner_BossAlive_PriorityOverWaveNumber()
        {
            var b = new SurvivorBanner();
            b.Poll(3, true, 0.016f); // wave 3 + boss cùng lúc
            Assert.AreEqual("BOSS", b.Current);
        }

        [Test]
        public void Banner_NoChange_KeepsTextWithinWindow()
        {
            var b = new SurvivorBanner();
            b.Poll(2, false, 0f);
            b.Poll(2, false, 2f);  // age 2 < 2.5
            Assert.AreEqual("Đợt 2", b.Current);
        }

        [Test]
        public void Banner_ExpiresAfterShowSeconds()
        {
            var b = new SurvivorBanner();
            b.Poll(4, false, 0f);
            Assert.AreEqual("Đợt 4", b.Current);
            b.Poll(4, false, 2f);  // age 0 → 2
            b.Poll(4, false, 1f);  // age 2 → 3 (đã vượt 2.5 nhưng clear ở poll kế)
            b.Poll(4, false, 0f);  // incoming age 3 ≥ 2.5 → ẩn
            Assert.AreEqual("", b.Current);
        }

        [Test]
        public void Banner_WaveIndexZero_NoBanner()
        {
            // fail-closed: chưa có wave source → không bịa số
            var b = new SurvivorBanner();
            b.Poll(0, false, 0.016f);
            b.Poll(0, false, 0.016f);
            Assert.AreEqual("", b.Current);
        }

        [Test]
        public void Banner_Text_BossReplacesWaveNumber()
        {
            Assert.AreEqual("Đợt 7", SurvivorBanner.Text(7, false));
            Assert.AreEqual("BOSS", SurvivorBanner.Text(7, true));
        }

        // ------------------------------------------------------------------
        // SurvivorHudLogic.BarFill (HP/XP clamp)
        // ------------------------------------------------------------------

        [Test]
        public void BarFill_ClampsTo01()
        {
            Assert.AreEqual(0.5f, SurvivorHudLogic.BarFill(5f, 10f), 1e-4f);
            Assert.AreEqual(0f, SurvivorHudLogic.BarFill(-3f, 10f), 1e-4f);
            Assert.AreEqual(1f, SurvivorHudLogic.BarFill(99f, 10f), 1e-4f);
        }

        [Test]
        public void BarFill_MaxNonPositive_Zero()
        {
            // fail-closed: max 0/âm → 0, tránh NaN/inf
            Assert.AreEqual(0f, SurvivorHudLogic.BarFill(5f, 0f), 1e-4f);
            Assert.AreEqual(0f, SurvivorHudLogic.BarFill(5f, -2f), 1e-4f);
            Assert.AreEqual(0f, SurvivorHudLogic.BarFill(0f, 0f), 1e-4f);
        }

        // ------------------------------------------------------------------
        // FormatTime
        // ------------------------------------------------------------------

        [Test]
        public void FormatTime_Zero()
        {
            Assert.AreEqual("00:00", SurvivorHudLogic.FormatTime(0f));
        }

        [Test]
        public void FormatTime_RoundsDownSeconds()
        {
            Assert.AreEqual("00:59", SurvivorHudLogic.FormatTime(59.9f));
            Assert.AreEqual("01:05", SurvivorHudLogic.FormatTime(65.4f));
        }

        [Test]
        public void FormatTime_OverHour_KeepsMinutes()
        {
            Assert.AreEqual("60:00", SurvivorHudLogic.FormatTime(3600f));
        }

        [Test]
        public void FormatTime_Negative_Zero()
        {
            Assert.AreEqual("00:00", SurvivorHudLogic.FormatTime(-5f));
        }

        // ------------------------------------------------------------------
        // FormatGameOver — stats text + fallback lang
        // ------------------------------------------------------------------

        [Test]
        public void GameOverStats_DefaultLang_ViLabels()
        {
            var lines = SurvivorHudLogic.FormatGameOver(
                new SurvivorRunStats { Level = 3, TimeSurvived = 65f, Kills = 12 }, Bundled());
            Assert.AreEqual(new[] { "Kết quả", "Cấp 3", "Thời gian 01:05", "Tiêu diệt 12" }, lines.ToArray());
        }

        [Test]
        public void GameOverStats_EnLang_EnLabels_KillsZeroHidden()
        {
            var text = Bundled();
            text.SetLanguage("en");
            var lines = SurvivorHudLogic.FormatGameOver(
                new SurvivorRunStats { Level = 2, TimeSurvived = 0f, Kills = 0 }, text);
            Assert.AreEqual(new[] { "Result", "Lv 2", "Time 00:00" }, lines.ToArray());
        }

        [Test]
        public void GameOverStats_NoBundle_FallbackVnMap()
        {
            // không bundle: Get trả raw key → Locate đổi sang fallback VN, không hiện key
            var lines = SurvivorHudLogic.FormatGameOver(
                new SurvivorRunStats { Level = 5, TimeSurvived = 125f, Kills = 0 }, new SurvivorText());
            Assert.AreEqual(new[] { "Kết quả", "Cấp 5", "Thời gian 02:05" }, lines.ToArray());
        }

        [Test]
        public void GameOverStats_NullStats_FailSafeFallback()
        {
            var lines = SurvivorHudLogic.FormatGameOver(null, Bundled());
            Assert.AreEqual(new[] { "Kết quả", "Cấp 1", "Thời gian 00:00" }, lines.ToArray());
        }

        [Test]
        public void Locate_EnMissing_FallsBackToVi()
        {
            var text = Bundled();
            text.SetLanguage("en");
            // chỉ vi có key → fallback lang vi
            Assert.AreEqual("chỉ có vi", SurvivorHudLogic.Locate(text, "survivor.only.vi"));
        }

        [Test]
        public void Locate_UnknownKey_ReturnsRawKey()
        {
            Assert.AreEqual("survivor.x.y", SurvivorHudLogic.Locate(new SurvivorText(), "survivor.x.y"));
        }
    }
}