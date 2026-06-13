// -----------------------------------------------------------------------------
// VLTK.Backend — PredictState
//
// Client-side predictor cho skill cast gate. Mục tiêu: cho phép UI gate mượt
// (disable button khi không đủ mana / đang cooldown) mà KHÔNG cần gọi server,
// đồng thời đảm bảo "predict-then-reconcile" — khi server trả lời khác
// client dự đoán, server LUÔN thắng (parity FS-03A contract §5).
//
// Tham khảo parity:
//   - FS-03A contract §4.2 — gate chain CanCastSkill (relation/range/weapon/
//     eqt/horse/cooldown/resource). Ở client predict, chỉ predict được gate
//     dựa trên local state (resource + cooldown + onHorse). Gate context
//     (relation/distance/weaponType/equipState) phụ thuộc runtime entity
//     khác — client predict bỏ qua và để /cast/check xác nhận.
//
//   - FS-03A contract §4.3 — cooldown formula parity:
//       delay = max(TimePerCast, TimePerCastOnHorse khi on_horse, WaitTime)
//       next_ready = last_cast_ms + delay
//       sẵn_sàng = nowMs >= next_ready
//     Lần đầu (last_cast_ms <= 0) LUÔN sẵn sàng.
//
//   - FS-03A contract §5 — "client predict chỉ để hiển thị/animation;
//     số thì đợi server". PredictState là PURE (không gọi network) — chỉ
//     dùng snapshot client + last response. Khi gửi /cast phải reconcile
//     với /cast/check trước; nếu mismatch, KHÔNG gửi /cast.
//
// Lý do tách thành class pure:
//   1. EditMode test không cần network, không cần backend.
//   2. Cùng logic dùng cho UI (gate) VÀ cho batch bot/AI (predict path).
//   3. Cùng logic được mock dễ dàng cho PlayMode integration test.
// -----------------------------------------------------------------------------

using VLTK.Backend.Dto;

namespace VLTK.Backend
{
    /// <summary>
    /// Resource snapshot client-side. Mirror của <c>player_states.current_*</c>
    /// + <c>player_skills.last_cast_ms</c>. Cập nhật mỗi khi nhận response
    /// từ /cast (server-authoritative) — KHÔNG tự tăng/giảm ở client.
    /// </summary>
    [System.Serializable]
    public sealed class ClientResourceSnapshot
    {
        public int currentLife;
        public int currentMana;
        public int currentStamina;
        public long lastCastMs;     // 0 nếu chưa cast bao giờ
        public long nowMs;          // mốc "bây giờ" — caller truyền vào để có test deterministic

        public ClientResourceSnapshot() { }

        public ClientResourceSnapshot(
            int currentLife, int currentMana, int currentStamina,
            long lastCastMs = 0, long nowMs = 0)
        {
            this.currentLife = currentLife;
            this.currentMana = currentMana;
            this.currentStamina = currentStamina;
            this.lastCastMs = lastCastMs;
            this.nowMs = nowMs;
        }
    }

    /// <summary>
    /// Kết quả predict cục bộ (TRƯỚC khi reconcile với server).
    /// Một số gate client KHÔNG predict được (relation/distance — cần biết
    /// target entity ở runtime) nên trả <see cref="needsServerCheck"/>=true
    /// để caller quyết định gọi /cast/check.
    /// </summary>
    public readonly struct PredictResult
    {
        /// <summary>True nếu predict tất cả gate local đều pass.</summary>
        public readonly bool canCastLocally;

        /// <summary>True nếu cần gọi /cast/check để xác nhận gate context.</summary>
        public readonly bool needsServerCheck;

        /// <summary>Lý do fail (resource/cooldown); null khi OK.</summary>
        public readonly string reason;

        /// <summary>Mốc sẵn sàng nếu đang cooldown (server /cast check sẽ trả giá trị chính thức).</summary>
        public readonly long predictedNextCastTime;

        public PredictResult(
            bool canCastLocally,
            bool needsServerCheck,
            string reason = null,
            long predictedNextCastTime = 0)
        {
            this.canCastLocally = canCastLocally;
            this.needsServerCheck = needsServerCheck;
            this.reason = reason;
            this.predictedNextCastTime = predictedNextCastTime;
        }
    }

    /// <summary>
    /// Kết quả reconcile sau khi đã có response từ /cast/check.
    /// </summary>
    public readonly struct ReconcileResult
    {
        /// <summary>True nếu server xác nhận cast OK.</summary>
        public readonly bool canCast;

        /// <summary>Lý do fail (server trả); null khi OK.</summary>
        public readonly string reason;

        /// <summary>Mốc sẵn sàng (server-authoritative).</summary>
        public readonly long serverNextCastTime;

        /// <summary>
        /// True nếu server và client predict CÙNG kết luận (canCastLocally ==
        /// server.canCast). Khi false, server thắng; UI nên tắt cooldown bar
        /// nếu server báo sẵn sàng sớm hơn client dự đoán (vd 2 nhân vật share
        /// cooldown).
        /// </summary>
        public readonly bool predictionMatched;

        public ReconcileResult(
            bool canCast,
            string reason,
            long serverNextCastTime,
            bool predictionMatched)
        {
            this.canCast = canCast;
            this.reason = reason;
            this.serverNextCastTime = serverNextCastTime;
            this.predictionMatched = predictionMatched;
        }
    }

    /// <summary>
    /// Predictor pure cho skill cast gate. KHÔNG gọi network, KHÔNG mutate
    /// state — chỉ nhận input, trả output. Tất cả method static.
    ///
    /// Hợp đồng parity FS-03A:
    ///   - Resource gate: costType=1 (mana) → currentMana &gt;= costValue;
    ///                    costType=2 (life) → currentLife &gt;= costValue;
    ///                    costType=3 (stamina) → currentStamina &gt;= costValue;
    ///                    costType=0 (none) hoặc costValue &lt;= 0 → luôn pass.
    ///   - Cooldown gate: lần đầu (lastCastMs &lt;= 0) → luôn pass;
    ///                    nowMs &gt;= lastCastMs + delayPerCast → pass;
    ///                    else → fail với reason "cooldown".
    ///   - Gate context (relation/range/weapon/eqt/horse) KHÔNG predict — cần
    ///     /cast/check xác nhận.
    /// </summary>
    public static class PredictState
    {
        /// <summary>
        /// Predict xem client-side có thể cast được không (chỉ dựa trên resource
        /// + cooldown + onHorse). KHÔNG kiểm tra relation/range/weapon/eqt —
        /// cần server xác nhận qua /cast/check.
        /// </summary>
        /// <param name="snapshot">Resource snapshot client-side.</param>
        /// <param name="costType">0=none, 1=mana, 2=life, 3=stamina (parity server).</param>
        /// <param name="costValue">Lượng tài nguyên phải trả.</param>
        /// <param name="delayPerCast">ms — max(TimePerCast, TimePerCastOnHorse, WaitTime).</param>
        /// <returns>
        /// <see cref="PredictResult"/> với canCastLocally + needsServerCheck.
        /// needsServerCheck LUÔN true vì gate context (relation/range/...) phụ
        /// thuộc runtime, không predict được ở local.
        /// </returns>
        public static PredictResult Predict(
            ClientResourceSnapshot snapshot,
            int costType,
            int costValue,
            int delayPerCast)
        {
            if (snapshot == null)
            {
                return new PredictResult(
                    canCastLocally: false,
                    needsServerCheck: true,
                    reason: "snapshot null");
            }

            // 1. Cooldown gate (parity FS-03A §4.3).
            // Lần đầu (lastCastMs <= 0) luôn sẵn sàng; sau đó phải nowMs >=
            // lastCastMs + delayPerCast. Tính predictedNextCastTime để UI
            // hiển thị cooldown bar mượt (server có thể trả khác nếu có
            // cast khác xen vào).
            long predictedNextCastTime = snapshot.lastCastMs + delayPerCast;
            if (snapshot.lastCastMs > 0 && snapshot.nowMs < predictedNextCastTime)
            {
                return new PredictResult(
                    canCastLocally: false,
                    needsServerCheck: true,
                    reason: "cooldown",
                    predictedNextCastTime: predictedNextCastTime);
            }

            // 2. Resource gate (parity FS-03A cost table §3).
            if (costValue > 0)
            {
                switch (costType)
                {
                    case 1: // mana
                        if (snapshot.currentMana < costValue)
                        {
                            return new PredictResult(
                                canCastLocally: false,
                                needsServerCheck: true,
                                reason: "not_enough_mana");
                        }
                        break;
                    case 2: // life
                        if (snapshot.currentLife < costValue)
                        {
                            return new PredictResult(
                                canCastLocally: false,
                                needsServerCheck: true,
                                reason: "not_enough_life");
                        }
                        break;
                    case 3: // stamina
                        if (snapshot.currentStamina < costValue)
                        {
                            return new PredictResult(
                                canCastLocally: false,
                                needsServerCheck: true,
                                reason: "not_enough_stamina");
                        }
                        break;
                    case 0:
                    default:
                        // costType=0 (không tiêu hao) hoặc lạ → bỏ qua resource
                        // gate (engine PC bỏ qua loại không xác định — parity
                        // skill_cast.py:74-75).
                        break;
                }
            }

            // 3. Cooldown + resource đều pass. Gate context (relation/range/
            // weapon/eqt/horse) cần server xác nhận → needsServerCheck=true.
            return new PredictResult(
                canCastLocally: true,
                needsServerCheck: true,
                reason: null,
                predictedNextCastTime: predictedNextCastTime);
        }

        /// <summary>
        /// Predict với input đã có sẵn cost/delay/delayPerCast ở <paramref name="lastCheck"/>
        /// (response từ /cast/check hoặc /cast trước). Đây là shortcut khi
        /// caller đã cache last response và chỉ cần re-evaluate với snapshot
        /// hiện tại (vd sau khi regen mana).
        /// </summary>
        public static PredictResult Predict(
            ClientResourceSnapshot snapshot,
            SkillCastCheckResponse lastCheck)
        {
            if (lastCheck == null)
            {
                return Predict(snapshot, 0, 0, 0);
            }
            return Predict(snapshot, lastCheck.costType, lastCheck.costValue, lastCheck.delayPerCast);
        }

        /// <summary>
        /// Reconcile kết quả client predict với response từ /cast/check. Server
        /// LUÔN thắng — nếu mismatch, <see cref="ReconcileResult.canCast"/>
        /// lấy theo server và predictionMatched=false để caller log/UI biết.
        /// </summary>
        public static ReconcileResult Reconcile(
            PredictResult prediction,
            SkillCastCheckResponse serverCheck)
        {
            if (serverCheck == null)
            {
                // Không có server check → caller KHÔNG được phép gửi /cast.
                return new ReconcileResult(
                    canCast: false,
                    reason: "no_server_check",
                    serverNextCastTime: 0,
                    predictionMatched: false);
            }

            bool serverCanCast = serverCheck.canCast;
            bool matched = prediction.canCastLocally == serverCanCast;
            return new ReconcileResult(
                canCast: serverCanCast,
                reason: serverCheck.reason,
                serverNextCastTime: serverCheck.nextCastTime,
                predictionMatched: matched);
        }
    }
}
