// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — PredictStateTests
// Test cho PredictState (pure predictor) — verify resource gate + cooldown gate
// + cost types + costType=0 bypass + costValue<=0 bypass + predict mismatch
// reconciliation. KHÔNG cần network, KHÔNG cần backend.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;

namespace VLTK.Tests.Backend
{
    public class PredictStateTests
    {
        // ============================================================
        // Cooldown gate
        // ============================================================

        [Test]
        public void Predict_FirstCast_AlwaysPassesCooldown()
        {
            // lastCastMs=0 → lần đầu luôn sẵn sàng.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 100,
                lastCastMs: 0, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 5);

            Assert.IsTrue(result.canCastLocally);
            // needsServerCheck LUÔN true (gate context cần server xác nhận).
            Assert.IsTrue(result.needsServerCheck);
            Assert.IsNull(result.reason);
        }

        [Test]
        public void Predict_CooldownNotElapsed_FailsWithCooldownReason()
        {
            // lastCastMs=1000, delay=5, nowMs=1001 → chưa sẵn sàng.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 100,
                lastCastMs: 1000, nowMs: 1001);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 5);

            Assert.IsFalse(result.canCastLocally);
            Assert.AreEqual("cooldown", result.reason);
            // Predicted next ready = 1000+5 = 1005.
            Assert.AreEqual(1005L, result.predictedNextCastTime);
        }

        [Test]
        public void Predict_CooldownElapsed_PassesCooldown()
        {
            // lastCastMs=1000, delay=5, nowMs=1005 → vừa sẵn sàng.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 100,
                lastCastMs: 1000, nowMs: 1005);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 5);

            Assert.IsTrue(result.canCastLocally);
            Assert.IsNull(result.reason);
        }

        [Test]
        public void Predict_CooldownFarPassed_Passes()
        {
            // lastCastMs=1000, delay=5, nowMs=10000 → đã qua lâu.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 100,
                lastCastMs: 1000, nowMs: 10000);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 5);

            Assert.IsTrue(result.canCastLocally);
        }

        [Test]
        public void Predict_NegativeLastCast_TreatedAsFirstCast()
        {
            // lastCastMs=-1 → như lần đầu, luôn pass.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 100,
                lastCastMs: -1, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 5);

            Assert.IsTrue(result.canCastLocally);
        }

        [Test]
        public void Predict_ZeroDelay_PassesCooldownAfterSameMs()
        {
            // delay=0 → luôn pass cooldown trừ khi nowMs < lastCastMs.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 100,
                lastCastMs: 1000, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 0);

            Assert.IsTrue(result.canCastLocally);
        }

        // ============================================================
        // Resource gate: costType=1 (mana)
        // ============================================================

        [Test]
        public void Predict_NotEnoughMana_Fails()
        {
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 30, currentStamina: 100, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 0);

            Assert.IsFalse(result.canCastLocally);
            Assert.AreEqual("not_enough_mana", result.reason);
        }

        [Test]
        public void Predict_ExactMana_Passes()
        {
            // currentMana == costValue → vẫn pass.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 50, currentStamina: 100, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 0);

            Assert.IsTrue(result.canCastLocally);
        }

        // ============================================================
        // Resource gate: costType=2 (life)
        // ============================================================

        [Test]
        public void Predict_NotEnoughLife_Fails()
        {
            var snap = new ClientResourceSnapshot(
                currentLife: 10, currentMana: 300, currentStamina: 100, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 2, costValue: 50, delayPerCast: 0);

            Assert.IsFalse(result.canCastLocally);
            Assert.AreEqual("not_enough_life", result.reason);
        }

        [Test]
        public void Predict_ExactLife_Passes()
        {
            var snap = new ClientResourceSnapshot(
                currentLife: 50, currentMana: 300, currentStamina: 100, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 2, costValue: 50, delayPerCast: 0);

            Assert.IsTrue(result.canCastLocally);
        }

        // ============================================================
        // Resource gate: costType=3 (stamina)
        // ============================================================

        [Test]
        public void Predict_NotEnoughStamina_Fails()
        {
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 10, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 3, costValue: 50, delayPerCast: 0);

            Assert.IsFalse(result.canCastLocally);
            Assert.AreEqual("not_enough_stamina", result.reason);
        }

        // ============================================================
        // Resource gate: costType=0 (none) hoặc costValue<=0
        // ============================================================

        [Test]
        public void Predict_CostTypeNone_BypassesResourceGate()
        {
            // costType=0 (Kim Ba) → không trừ resource, pass.
            var snap = new ClientResourceSnapshot(
                currentLife: 1, currentMana: 0, currentStamina: 0, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 0, costValue: 0, delayPerCast: 0);

            Assert.IsTrue(result.canCastLocally);
        }

        [Test]
        public void Predict_ZeroCost_BypassesResourceGate()
        {
            // costValue=0 với costType=1 → vẫn bypass.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 0, currentStamina: 100, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 1, costValue: 0, delayPerCast: 0);

            Assert.IsTrue(result.canCastLocally);
        }

        [Test]
        public void Predict_UnknownCostType_BypassesResourceGate()
        {
            // costType=99 (lạ) → parity skill_cast.py:74-75 bỏ qua loại không
            // xác định. Vẫn pass resource gate.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 0, currentStamina: 0, nowMs: 1000);
            var result = PredictState.Predict(snap, costType: 99, costValue: 50, delayPerCast: 0);

            Assert.IsTrue(result.canCastLocally,
                "costType lạ → bỏ qua resource gate (parity engine PC)");
        }

        // ============================================================
        // Gate order: cooldown check TRƯỚC resource check
        // ============================================================

        [Test]
        public void Predict_CooldownFailsBeforeResourceCheck()
        {
            // Khi cả cooldown và resource đều fail, reason PHẢI là "cooldown"
            // (gate order parity KSkill::CanCastSkill — cooldown 8 trước resource 9).
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 0, currentStamina: 100,
                lastCastMs: 1000, nowMs: 1001);
            var result = PredictState.Predict(snap, costType: 1, costValue: 50, delayPerCast: 5);

            Assert.IsFalse(result.canCastLocally);
            Assert.AreEqual("cooldown", result.reason,
                "cooldown check chạy trước resource check (parity KSkill::CanCastSkill)");
        }

        // ============================================================
        // Null snapshot
        // ============================================================

        [Test]
        public void Predict_NullSnapshot_ReturnsFailure()
        {
            var result = PredictState.Predict(null, costType: 1, costValue: 50, delayPerCast: 0);

            Assert.IsFalse(result.canCastLocally);
            Assert.AreEqual("snapshot null", result.reason);
        }

        // ============================================================
        // Overload với SkillCastCheckResponse
        // ============================================================

        [Test]
        public void Predict_WithLastCheckResponse_UsesCheckFields()
        {
            var lastCheck = new SkillCastCheckResponse
            {
                skillId = 210,
                canCast = true,
                costType = 1,
                costValue = 50,
                delayPerCast = 0,
                nextCastTime = 0,
            };
            // currentMana < costValue → fail.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 10, currentStamina: 100, nowMs: 1000);
            var result = PredictState.Predict(snap, lastCheck);

            Assert.IsFalse(result.canCastLocally);
            Assert.AreEqual("not_enough_mana", result.reason);
        }

        [Test]
        public void Predict_WithNullLastCheck_DefaultsToFreeCast()
        {
            // null lastCheck → costType=0, costValue=0, delay=0 → luôn pass.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 0, currentStamina: 0, nowMs: 1000);
            var result = PredictState.Predict(snap, (SkillCastCheckResponse)null);

            Assert.IsTrue(result.canCastLocally);
        }

        // ============================================================
        // Reconcile
        // ============================================================

        [Test]
        public void Reconcile_ServerConfirms_PassesThrough()
        {
            var predict = new PredictResult(true, true, null, 0);
            var server = new SkillCastCheckResponse
            {
                canCast = true,
                reason = null,
                nextCastTime = 1000,
            };
            var r = PredictState.Reconcile(predict, server);

            Assert.IsTrue(r.canCast);
            Assert.IsNull(r.reason);
            Assert.AreEqual(1000L, r.serverNextCastTime);
            Assert.IsTrue(r.predictionMatched, "client OK + server OK = matched");
        }

        [Test]
        public void Reconcile_ServerDenies_PredictOK_Mismatch()
        {
            // Client predict OK (resource + cooldown pass) nhưng server deny
            // (gate context fail). Server thắng, predictionMatched=false.
            var predict = new PredictResult(true, true, null, 0);
            var server = new SkillCastCheckResponse
            {
                canCast = false,
                reason = "Khoảng cách quá xa",
                nextCastTime = 0,
            };
            var r = PredictState.Reconcile(predict, server);

            Assert.IsFalse(r.canCast, "server thắng");
            Assert.AreEqual("Khoảng cách quá xa", r.reason);
            Assert.IsFalse(r.predictionMatched, "predict OK + server fail = mismatch");
        }

        [Test]
        public void Reconcile_ServerConfirms_PredictFails_Mismatch()
        {
            // Client predict fail (cooldown) nhưng server OK (vd 2 nhân vật
            // share cooldown, server đã expire rồi). Server thắng, caller bật
            // lại UI.
            var predict = new PredictResult(false, true, "cooldown", 1005);
            var server = new SkillCastCheckResponse
            {
                canCast = true,
                reason = null,
                nextCastTime = 0,
            };
            var r = PredictState.Reconcile(predict, server);

            Assert.IsTrue(r.canCast, "server thắng — caller mở UI");
            Assert.IsTrue(r.serverNextCastTime == 0);
            Assert.IsFalse(r.predictionMatched);
        }

        [Test]
        public void Reconcile_ServerFails_PredictFails_Matched()
        {
            // Cả hai fail → matched.
            var predict = new PredictResult(false, true, "not_enough_mana", 0);
            var server = new SkillCastCheckResponse
            {
                canCast = false,
                reason = "Không đủ tài nguyên",
                nextCastTime = 0,
            };
            var r = PredictState.Reconcile(predict, server);

            Assert.IsFalse(r.canCast);
            Assert.IsFalse(r.predictionMatched, "cùng kết luận fail");
        }

        [Test]
        public void Reconcile_NullServerCheck_FailsClosed()
        {
            // Không có server check → caller KHÔNG được gửi /cast.
            var predict = new PredictResult(true, true, null, 0);
            var r = PredictState.Reconcile(predict, null);

            Assert.IsFalse(r.canCast);
            Assert.AreEqual("no_server_check", r.reason);
            Assert.IsFalse(r.predictionMatched);
        }

        // ============================================================
        // PredictState client-state pin (parity FS-03A)
        // ============================================================

        [Test]
        public void ClientResourceSnapshot_FieldsPersist()
        {
            // Sanity: snapshot giữ đúng các field parity player_states.
            var snap = new ClientResourceSnapshot(
                currentLife: 1000, currentMana: 300, currentStamina: 50,
                lastCastMs: 1234, nowMs: 5678);
            Assert.AreEqual(1000, snap.currentLife);
            Assert.AreEqual(300, snap.currentMana);
            Assert.AreEqual(50, snap.currentStamina);
            Assert.AreEqual(1234L, snap.lastCastMs);
            Assert.AreEqual(5678L, snap.nowMs);
        }
    }
}
