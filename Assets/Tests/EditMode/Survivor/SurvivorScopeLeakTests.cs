// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorScopeLeakTests
// Ticket 45: 2 scope-leak followup (dual review 44) — cùng class bug ticket 44
// (LevelUpScope không release → pause kẹt vô hạn):
//   P1 (44a): player chết khi modal levelup mở — SurvivorMonster.Update damage
//     check chạy mỗi frame không check Pause → OnPlayerDied phải release
//     LevelUpScope (no-op khi scope vắng) TRƯỚC ShowGameOver.
//   P2 (44b): auto-close race vs queue mới — Close → Pump trigger event MỚI
//     cùng roleId trong frame auto-close; poll phải so identity (ReferenceEquals
//     với event đã render) → fail-closed đóng modal + onClosed, KHÔNG giữ card cũ.
// Scene thật EditMode (boot qua OnInit như wiring tests — Awake không chạy).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorScopeLeakTests
    {
        private sealed class TestDirector : SurvivorGameDirector
        {
            public void Boot() => OnInit();
        }

        private GameObject _directorGo;
        private TestDirector _director;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            _directorGo = new GameObject("director_scopeleak");
            _director = _directorGo.AddComponent<TestDirector>();
            _director.Boot();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_directorGo != null) Object.DestroyImmediate(_directorGo);
            foreach (var o in Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (o is OverlayPanel || o is SurvivorHud || o is SupplyBar ||
                    o is SurvivorAudioSettingsPanel || o is SurvivorAudioMgr ||
                    o is UnityEngine.EventSystems.EventSystem)
                    Object.DestroyImmediate(o.gameObject);
            }
        }

        // ------------------------------------------------------------------
        // P1 (44a): player chết khi modal mở → LevelUpScope release
        // ------------------------------------------------------------------

        [Test]
        public void PlayerDied_WhileModalOpen_ReleasesLevelUpScope_GameOverShows()
        {
            var svc = _director.Overlay.SkillService;
            svc.Tick(0f);
            _director.OnLevelUp(null); // modal skill mở: LevelUp + CardChoice scope
            Assert.IsTrue(_director.Overlay.IsVisible, "modal skill đang mở");
            Assert.AreEqual(2, _director.Pause.Count, "LevelUp + CardChoice giữ pause");

            // player chết khi modal mở (monster damage path — Update không check Pause)
            _director.Player.TakeDamage(9999);

            Assert.IsTrue(_director.Player.Dead, "player chết");
            Assert.AreEqual(0, _director.Pause.ScopeCount(SurvivorPause.LevelUpScope),
                "ticket 45: OnPlayerDied release LevelUpScope — không kẹt");
            Assert.AreEqual(1, _director.Pause.ScopeCount(SurvivorPause.GameOverScope),
                "GameOver scope giữ pause — gameover hiển thị bình thường");
            // CardChoice scope còn giữ 1: service event của modal cũ vẫn "mở" —
            // UI đã bị gameover thay thế, scene reload (restart) dọn sạch. Mục tiêu
            // ticket 45 là LevelUpScope không kẹt — đã đạt; CardChoice không phải
            // leak vô hạn (không tăng theo thời gian, restart reset instance).
            Assert.AreEqual(2, _director.Pause.Count, "CardChoice (event modal cũ) + GameOver — restart dọn");
            Assert.IsTrue(_director.Overlay.IsVisible, "gameover canvas hiển thị");
            Assert.AreEqual(0f, Time.timeScale, "vẫn pause (GameOver scope) — chờ restart");
        }

        [Test]
        public void PlayerDied_NoModal_LevelUpReleaseNoOp_GameOverNormal()
        {
            var svc = _director.Overlay.SkillService;
            svc.Tick(0f);

            _director.Player.TakeDamage(9999); // chết khi không có modal

            Assert.AreEqual(0, _director.Pause.ScopeCount(SurvivorPause.LevelUpScope),
                "release no-op khi scope vắng — không phá path thường");
            Assert.AreEqual(1, _director.Pause.ScopeCount(SurvivorPause.GameOverScope));
            Assert.AreEqual(1, _director.Pause.Count);
            Assert.IsTrue(_director.Overlay.IsVisible, "gameover hiển thị bình thường");
        }

        // ------------------------------------------------------------------
        // P2 (44b): auto-close race vs queue mới → poll đóng fail-closed
        // ------------------------------------------------------------------

        [Test]
        public void AutoClose_QueueHasNewEvent_PollClosesFailsClosed_NoStaleCard()
        {
            var svc = _director.Overlay.SkillService;
            svc.Tick(0f);
            _director.OnLevelUp(null); // render event A (LevelUp): LevelUp + CardChoice
            var eventA = svc.Current(1);
            Assert.IsNotNull(eventA, "event A đang hiển thị");
            Assert.AreEqual(2, _director.Pause.Count);

            Assert.IsFalse(svc.Request(1, SkillChoiceMode.Box, 1), "đang chọn → enqueue event B (FIFO)");

            // auto-close frame: Tick → Close(A) → Pump → trigger B (cùng roleId)
            svc.Tick(31f);
            var eventB = svc.Current(1);
            Assert.IsNotNull(eventB, "queue pump ra event B");
            Assert.AreNotSame(eventA, eventB, "event B là event MỚI — poll phải phát hiện");
            Assert.AreEqual(1, _director.Pause.ScopeCount(SurvivorPause.CardChoiceScope),
                "B trigger → CardChoice re-acquire (service contract)");
            Assert.AreEqual(1, _director.Pause.ScopeCount(SurvivorPause.LevelUpScope),
                "LevelUp chưa release — chờ poll onClosed");

            // poll (Overlay.Update): Current != eventA → fail-closed đóng modal
            _director.Overlay.PollSkillChoiceAutoClose();
            Assert.IsFalse(_director.Overlay.IsVisible, "modal đóng — KHÔNG giữ card cũ");
            Assert.AreEqual(0, _director.Pause.ScopeCount(SurvivorPause.LevelUpScope),
                "onClosed fire → LevelUp release");
            Assert.AreEqual(0f, Time.timeScale, "vẫn pause (CardChoice của B) — không phải kẹt vô hạn");

            // B hết waiting window → service auto-close → CardChoice release, count 0
            svc.Tick(31f + 31f);
            Assert.AreEqual(0, _director.Pause.Count, "mọi scope release — không kẹt vô hạn");
            Assert.IsFalse(_director.Pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale, "timescale về 1");
        }

        [Test]
        public void Poll_ModalOpen_SameEvent_NoFalseClose_NoRegression()
        {
            var svc = _director.Overlay.SkillService;
            svc.Tick(0f);
            _director.OnLevelUp(null);
            Assert.AreEqual(2, _director.Pause.Count);

            // poll khi modal còn mở + event đúng → KHÔNG đóng (identity match)
            _director.Overlay.PollSkillChoiceAutoClose();
            Assert.IsTrue(_director.Overlay.IsVisible, "modal còn mở — poll không fire nhầm");
            Assert.AreEqual(2, _director.Pause.Count, "poll không đụng pause");
            Assert.AreEqual(0f, Time.timeScale);

            // pick bình thường → đóng + release sạch
            var card = svc.Current(1).Cards[0];
            Assert.IsTrue(svc.Select(1, card));
            _director.Overlay.PollSkillChoiceAutoClose();
            Assert.IsFalse(_director.Overlay.IsVisible, "modal đóng sau pick");
            Assert.AreEqual(0, _director.Pause.Count, "không leak sau pick");
            Assert.AreEqual(1f, Time.timeScale, "timescale về 1");
        }
    }
}
