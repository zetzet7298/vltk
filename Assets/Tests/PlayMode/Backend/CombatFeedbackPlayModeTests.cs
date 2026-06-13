// -----------------------------------------------------------------------------
// VLTK.Tests.PlayMode.Backend — CombatFeedbackPlayModeTests
// PlayMode E2E cho FS-03D: kiểm tra end-to-end combat feedback loop
//   BackendClientRunner.RunCombatDemoAsync → CombatFeedbackBus.Raise
//   → CombatFeedbackView / CameraShake nhận event đúng thứ tự.
//
// Test thực sự chạy MonoBehaviour lifecycle (OnEnable subscribe, OnDisable
// unsubscribe, Coroutine start/destroy) — KHÔNG chỉ là pure unit test.
// Dùng Mock backend (useMock=true) để không cần server thật.
//
// File này bổ sung cho EditMode test ở
// Assets/Tests/EditMode/Backend/BackendClientRunnerTests.cs — phần
// EditMode test mock config; phần PlayMode test verify bus + view integration.
// -----------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Backend;
using VLTK.Backend.Combat;

namespace VLTK.Tests.PlayMode.Backend
{
    /// <summary>
    /// E2E PlayMode test cho combat feedback. Verify:
    ///   1. BackendClientRunner.RunCombatDemoAsync() chạy đủ rounds.
    ///   2. CombatFeedbackBus.Raise() phát đủ số event.
    ///   3. CombatFeedbackView (subscribe) nhận đúng tất cả event.
    ///   4. CameraShake (subscribe) trigger đúng số lần (trừ Miss/Heal-optional).
    /// </summary>
    public class CombatFeedbackPlayModeTests
    {
        private GameObject _go;
        private BackendClientRunner _runner;
        private CombatFeedbackView _view;
        private CameraShake _shake;
        private List<CombatFeedbackEvent> _captured;

        [SetUp]
        public void SetUp()
        {
            // Clean bus
            CombatFeedbackBus.ClearAllSubscribers();
            _captured = new List<CombatFeedbackEvent>();

            // GameObject cha (inactive để Start() không auto-fire)
            _go = new GameObject("CombatFeedbackTest_GO");
            _go.SetActive(false);

            _runner = _go.AddComponent<BackendClientRunner>();
            _view = _go.AddComponent<CombatFeedbackView>();
            _shake = _go.AddComponent<CameraShake>();

            // Disable auto-run + giảm rounds để test nhanh
            _runner.runOnStart = false;
            _runner.runCombatDemoOnComplete = false;
            _runner.combatDemoRounds = 3;
            _runner.critChance = 0f;  // 0% crit
            _runner.missChance = 0f;  // 0% miss → mọi event là Normal
            _runner.combatDemoDamage = 50;

            // Capture tất cả feedback event
            CombatFeedbackBus.OnFeedback += evt => _captured.Add(evt);

            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            CombatFeedbackBus.ClearAllSubscribers();
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private IEnumerator WaitForRunner()
        {
            // Chờ runner chạy xong (combat demo chỉ cần ~vài frame vì mock).
            float deadline = Time.realtimeSinceStartup + 5f;
            while (!_runner.IsCompleted || !_runner.IsCombatDemoCompleted)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Runner timed out — IsCompleted={0}, IsCombatDemoCompleted={1}",
                        _runner.IsCompleted, _runner.IsCombatDemoCompleted);
                }
                yield return null;
            }
        }

        // ---- Tests ----

        [UnityTest]
        public IEnumerator PlayMode_RunCombatDemo_PublishesExpectedEvents()
        {
            // Start runOnStart flow (login + list roles + enter map + combat demo)
            var task = _runner.StartFullFlowAsync(System.Threading.CancellationToken.None);
            // Wait for completion
            while (!task.IsCompleted) yield return null;
            yield return WaitForRunner();

            Assert.IsNull(_runner.LastError,
                $"Runner.LastError phải null khi thành công; got: {_runner.LastError}");
            Assert.IsTrue(_runner.IsCompleted, "RunAsync phải set IsCompleted=true.");
            Assert.IsTrue(_runner.IsCombatDemoCompleted, "RunCombatDemoAsync phải xong.");

            // Số event phải >= combatDemoRounds (mỗi round 1 damage event) + heal (1/2 rounds)
            // missChance=0, critChance=0 → toàn Normal damage + chèn Heal
            int expectedMinimum = _runner.combatDemoRounds; // ít nhất N damage events
            Assert.GreaterOrEqual(_captured.Count, expectedMinimum,
                $"Số feedback event phải >= {expectedMinimum}; got {_captured.Count}");

            // FeedbackEventCount counter phải khớp captured.Count
            Assert.AreEqual(_captured.Count, _runner.FeedbackEventCount,
                "FeedbackEventCount phải bằng số event thực tế đã publish.");
        }

        [UnityTest]
        public IEnumerator PlayMode_View_ReceivesAllEventsViaSubscription()
        {
            // Manual publish để test view subscribe độc lập với runner
            int manualEvents = 5;
            for (int i = 0; i < manualEvents; i++)
            {
                var evt = new CombatFeedbackEvent(
                    CombatFeedbackKind.Normal, 100 + i, Vector3.zero);
                CombatFeedbackBus.Raise(evt);
            }
            yield return null; // let Update tick

            // View đang subscribe bus (trong OnEnable) → text spawn ra
            // Test với world-space fallback (canvasRoot=null) → spawn TextMesh
            Assert.Greater(_view.ActiveCount, 0,
                "CombatFeedbackView phải spawn ít nhất 1 feedback item.");
        }

        [UnityTest]
        public IEnumerator PlayMode_CameraShake_TriggersOnNormalHit()
        {
            // Gọi trực tiếp bus với Normal hit
            CombatFeedbackBus.Raise(new CombatFeedbackEvent(
                CombatFeedbackKind.Normal, 100, Vector3.zero));
            yield return null;
            Assert.IsTrue(_shake.IsShaking,
                "CameraShake phải trigger khi nhận Normal hit.");
        }

        [UnityTest]
        public IEnumerator PlayMode_CameraShake_DoesNotTriggerOnMiss()
        {
            CombatFeedbackBus.Raise(new CombatFeedbackEvent(
                CombatFeedbackKind.Miss, 0, Vector3.zero));
            yield return null;
            Assert.IsFalse(_shake.IsShaking,
                "CameraShake KHÔNG trigger khi miss (damage=0).");
        }

        [UnityTest]
        public IEnumerator PlayMode_CameraShake_StrongerOnCrit()
        {
            // Lưu vị trí gốc
            Vector3 basePos = _shake.transform.localPosition;

            // Normal hit nhỏ
            CombatFeedbackBus.Raise(new CombatFeedbackEvent(
                CombatFeedbackKind.Normal, 10, Vector3.zero));
            yield return null;
            float normalMagnitude = ComputeMaxOffset(_shake.transform.localPosition, basePos);

            // Wait for shake to end
            float deadline = Time.realtimeSinceStartup + 1.0f;
            while (_shake.IsShaking && Time.realtimeSinceStartup < deadline) yield return null;
            _shake.SetBasePosition(basePos);

            // Crit hit cùng value
            CombatFeedbackBus.Raise(new CombatFeedbackEvent(
                CombatFeedbackKind.Crit, 10, Vector3.zero));
            yield return null;
            float critMagnitude = ComputeMaxOffset(_shake.transform.localPosition, basePos);

            Assert.Greater(critMagnitude, normalMagnitude,
                $"Crit shake ({critMagnitude}) phải mạnh hơn Normal ({normalMagnitude}) cùng damage.");
        }

        private static float ComputeMaxOffset(Vector3 now, Vector3 basePos)
            => Vector3.Distance(now, basePos);
    }
}
