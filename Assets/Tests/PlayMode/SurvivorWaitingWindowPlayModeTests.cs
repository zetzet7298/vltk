// -----------------------------------------------------------------------------
// VLTK.Tests.PlayMode — SurvivorWaitingWindowPlayModeTests
// Ticket 44: waiting-window auto-close (O6) — regression path EditMode không
// cover được: director.Update THẬT với pause held (modal mở ⇔ IsPaused) —
// Tick phải chạy trước early-return, auto-close → Overlay.Update poll hide
// modal + fire onClosed (release LevelUpScope) → timescale về 1, không leak.
// Timeout rút ngắn (WaitingLearnWindow instance field, ticket 44 sanction) —
// không đợi 30s thật.
// -----------------------------------------------------------------------------

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Survivor;

namespace VLTK.Tests.PlayMode
{
    public class SurvivorWaitingWindowPlayModeTests
    {
        private GameObject _directorGo;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_directorGo != null) Object.DestroyImmediate(_directorGo);
            // dọn singleton/UI boot tạo — tránh rò rỉ sang test kế
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
        public IEnumerator LevelUp_WaitingWindow_AutoCloses_Unpauses_NoLeak()
        {
            _directorGo = new GameObject("director_pm");
            _directorGo.AddComponent<SurvivorGameDirector>(); // PlayMode: Awake → OnInit (boot thật)
            var director = SurvivorGameDirector.Instance;
            Assert.IsNotNull(director, "director boot");
            var svc = director.Overlay.SkillService;
            Assert.IsNotNull(svc, "SkillChoiceService wire (ticket 43)");

            svc.WaitingLearnWindow = 0.2f; // rút timeout — không đợi 30s thật
            svc.Tick(Time.time);
            director.OnLevelUp(null); // levelup service path: LevelUp + CardChoice scope
            Assert.IsTrue(director.Overlay.IsVisible, "modal skill mở");
            Assert.IsTrue(director.Pause.IsPaused, "modal mở ⇔ pause");
            Assert.AreEqual(2, director.Pause.Count, "LevelUp + CardChoice scope");
            Assert.AreEqual(0f, Time.timeScale, "timescale 0 khi modal mở");
            yield return null; // director.Update chạy 1 frame với pause held — Tick không bị early-return chặn

            // chờ auto-close thật (director.Update Tick → service Close → Overlay.Update poll)
            float deadline = Time.realtimeSinceStartup + 3f;
            while (director.Pause.IsPaused && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.IsFalse(director.Pause.IsPaused, "auto-close → hết pause (không leak scope)");
            Assert.AreEqual(0, director.Pause.ScopeCount(SurvivorPause.CardChoiceScope), "CardChoice scope release");
            Assert.AreEqual(0, director.Pause.ScopeCount(SurvivorPause.LevelUpScope), "LevelUp scope release qua onClosed hook");
            Assert.IsFalse(director.Overlay.IsVisible, "modal tự đóng (canvas hide)");
            Assert.AreEqual(1f, Time.timeScale, "timescale về 1");
        }
    }
}
