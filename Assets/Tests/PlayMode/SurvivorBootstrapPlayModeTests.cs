// -----------------------------------------------------------------------------
// VLTK.Tests.PlayMode — SurvivorBootstrapPlayModeTests
// Phase 2 (PORT_CAIBANG §3 Gap C) run-start bootstrap — smoke qua director
// THẬT (Awake → OnInit → Start → OnGameStart → TriggerBootstrap):
//  - t=0: modal 2 card (128/125) hiện ngay, KHÔNG 1073/1074, timescale 0
//    (CardChoiceScope), IsBootstrap, RerollsLeft=0.
//  - Pick 1 → Learn roster + event đóng + CardChoice release + Overlay poll
//    hide modal → game chạy.
//  - Levelup SAU bootstrap → vẫn 3 card + reroll (parity giữ nguyên).
//  - Close() từ chối bootstrap (bắt pick) + timeout re-trigger giữ event.
// -----------------------------------------------------------------------------

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Survivor;

namespace VLTK.Tests.PlayMode
{
    public class SurvivorBootstrapPlayModeTests
    {
        private GameObject _directorGo;

        [SetUp]
        public void SetUp() { Time.timeScale = 1f; }

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

        [UnityTest]
        public IEnumerator Bootstrap_ModalAtStart_PickResumes_LevelUpStill3Cards()
        {
            _directorGo = new GameObject("director_bootstrap_pm");
            _directorGo.AddComponent<SurvivorGameDirector>();
            var director = SurvivorGameDirector.Instance;
            Assert.IsNotNull(director, "director boot");
            var svc = director.Overlay.SkillService;
            Assert.IsNotNull(svc, "SkillChoiceService wire");

            yield return null; // Start → OnGameStart → TriggerBootstrap đã chạy

            // --- t=0: modal 2 card ép sẵn (128/125), KHÔNG 1073/1074 ---
            var ev = svc.Current(1u);
            Assert.IsNotNull(ev, "bootstrap event ngay sau game start");
            Assert.IsTrue(ev.IsBootstrap, "flag bootstrap");
            Assert.AreEqual(2, ev.Cards.Length, "2 card, KHÔNG phải 3 (parity draw khác)");
            Assert.AreEqual(128, ev.Cards[0].Def.Id, "card 0 = 128 Kháng Long Hữu Hối");
            Assert.AreEqual(125, ev.Cards[1].Def.Id, "card 1 = 125 Bổng Đả Ác Cẩu");
            Assert.AreEqual(0, ev.RerollsLeft, "bootstrap KHÔNG reroll");
            Assert.IsTrue(director.Overlay.IsVisible, "modal hiện ngay t=0");
            Assert.AreEqual(1, director.Pause.ScopeCount(SurvivorPause.CardChoiceScope), "CardChoice pause giữ");
            Assert.AreEqual(0f, Time.timeScale, "timescale 0 tới click (bắt pick)");

            // --- Close() từ chối bootstrap (không skip được card đầu) ---
            svc.Close(1u);
            Assert.IsNotNull(svc.Current(1u), "Close TỪ CHỐI khi IsBootstrap");
            Assert.AreEqual(1, director.Pause.ScopeCount(SurvivorPause.CardChoiceScope), "pause giữ");

            // --- timeout → re-trigger CÙNG event (không auto-close) ---
            svc.WaitingLearnWindow = 0.1f; // rút window — không đợi 30s
            float deadline = Time.realtimeSinceStartup + 3f;
            while (Time.realtimeSinceStartup < deadline &&
                   svc.IsWaiting(1u) && !ReferenceEquals(svc.Current(1u), ev))
                yield return null; // chờ 1 window trôi qua (IsWaiting false) rồi Tick re-trigger
            yield return null; // 1 frame cho director.Update Tick xử lý timeout
            Assert.IsNotNull(svc.Current(1u), "timeout KHÔNG auto-close");
            Assert.IsTrue(svc.IsWaiting(1u), "window reset — modal vẫn chờ pick");
            Assert.AreEqual(0, director.Player.Cast.GetLevel(128), "fail-closed: KHÔNG auto-learn");
            Assert.IsTrue(director.Overlay.IsVisible, "modal vẫn hiển thị sau timeout");

            // --- pick 1 → learn + đóng + release pause → game chạy ---
            var pick = svc.Current(1u).Cards[0];
            Assert.IsTrue(svc.Select(1u, pick), "pick card");
            Assert.AreEqual(1, director.Player.Cast.GetLevel(pick.Def.Id), "pick → Learn roster");
            Assert.IsNull(svc.Current(1u), "event đóng");
            Assert.AreEqual(0, director.Pause.ScopeCount(SurvivorPause.CardChoiceScope), "CardChoice release");
            deadline = Time.realtimeSinceStartup + 3f;
            while (director.Overlay.IsVisible && Time.realtimeSinceStartup < deadline)
                yield return null; // Overlay.Update poll hide modal
            Assert.IsFalse(director.Overlay.IsVisible, "modal hide sau pick (poll)");
            Assert.AreEqual(1f, Time.timeScale, "timescale về 1 — game chạy tiếp");

            // --- levelup SAU bootstrap: parity giữ nguyên (depend chặn 1073/1074 khi 128 chưa Lv5) ---
            director.OnLevelUp(null);
            var ev2 = svc.Current(1u);
            Assert.IsNotNull(ev2, "levelup thường trigger");
            Assert.IsFalse(ev2.IsBootstrap, "KHÔNG phải bootstrap");
            Assert.AreEqual(2, ev2.Cards.Length, "128 mới lv1 → 1073/1074 depend chưa thỏa → 2 card (depend chặn đúng, parity giữ nguyên)");
            Assert.AreEqual(2, ev2.RerollsLeft, "reroll levelup vẫn có (parity giữ nguyên)");
            for (int i = 0; i < ev2.Cards.Length; i++)
            {
                Assert.AreNotEqual(1073, ev2.Cards[i].Def.Id, "1073 chưa mở (128 < Lv5)");
                Assert.AreNotEqual(1074, ev2.Cards[i].Def.Id, "1074 chưa mở (125 < Lv5)");
            }
            Assert.IsTrue(svc.RerollLevelUp(1u), "reroll hoạt động");
            Assert.AreEqual(2, svc.Current(1u).Cards.Length, "draw lại (2 card sẵn)");
            // đóng qua pick để test không kẹt scope
            svc.Select(1u, svc.Current(1u).Cards[0]);
            deadline = Time.realtimeSinceStartup + 3f;
            while (director.Pause.IsPaused && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.AreEqual(0, director.Pause.ScopeCount(SurvivorPause.CardChoiceScope), "không leak CardChoice");
            Assert.AreEqual(0, director.Pause.ScopeCount(SurvivorPause.LevelUpScope), "không leak LevelUp");
        }
    }
}
