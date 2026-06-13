// -----------------------------------------------------------------------------
// VLTK.Backend.Combat — CombatFeedbackEvent
// Event payload + static event hub (CombatFeedbackBus) cho combat feedback
// lớp visual (FS-03D). Lớp này nằm trên top của FS-03B (SkillCast) + FS-03C
// (Combat damage) — KHÔNG tự tính damage, chỉ mang thông tin đã được server
// xác nhận để view/spawner/camera render hiệu ứng.
//
// Luồng:
//   1. BackendClientRunner.RunCombatDemoAsync() gọi /v1/skill/cast →
//      SkillCastResponse. Response có currentLife/Mana/Stamina SAU cast.
//   2. Runner gọi /v1/combat/damage/calc → DamageCalcResponse (nếu cần).
//   3. Runner tính delta (trước/sau cast hoặc damage) → publish
//      CombatFeedbackEvent lên CombatFeedbackBus.
//   4. CombatFeedbackView / HitEffectSpawner / CameraShake (subscriber
//      MonoBehaviour) nhận event qua OnEnable subscribe, render feedback.
//
// Quy tắc server-authoritative (parity FS-03A §5):
//   - Event KHÔNG chứa damage tự tính — chỉ mang delta do server trả.
//   - Crit/miss/heal do runner quyết định (dựa vào policy đã pin trong
//     contract) — view chỉ render theo kind/color đã chỉ định.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Backend.Combat
{
    /// <summary>
    /// Phân loại feedback để view chọn màu + animation.
    ///   Normal = đòn thường trúng (trắng)
    ///   Crit   = đòn chí mạng (vàng)
    ///   Miss   = đánh trượt (đỏ)
    ///   Heal   = hồi máu/mana (xanh lá)
    /// </summary>
    public enum CombatFeedbackKind
    {
        Normal = 0,
        Crit = 1,
        Miss = 2,
        Heal = 3,
    }

    /// <summary>
    /// Một sự kiện feedback đơn lẻ (1 damage number, 1 hit effect, 1 camera
    /// shake). Value là số hiển thị (damage dương = trừ máu, delta âm = heal
    /// tùy kind). Position là vị trí world nơi spawn text/effect.
    /// </summary>
    [Serializable]
    public struct CombatFeedbackEvent
    {
        /// <summary>Phân loại (Normal/Crit/Miss/Heal) — quyết định màu + animation.</summary>
        public CombatFeedbackKind Kind;

        /// <summary>Số hiển thị (damage, heal, ...). Luôn &gt;= 0 khi Normal/Crit.</summary>
        public int Value;

        /// <summary>Vị trí world nơi spawn text/effect (Vector3).</summary>
        public Vector3 Position;

        /// <summary>True nếu là đòn chí mạng (camera shake mạnh hơn).</summary>
        public bool IsCritical => Kind == CombatFeedbackKind.Crit;

        /// <summary>True nếu đánh trượt (không spawn hit effect, chỉ text "Miss").</summary>
        public bool IsMiss => Kind == CombatFeedbackKind.Miss;

        /// <summary>True nếu là heal (màu xanh, không trigger camera shake).</summary>
        public bool IsHeal => Kind == CombatFeedbackKind.Heal;

        public CombatFeedbackEvent(CombatFeedbackKind kind, int value, Vector3 position)
        {
            Kind = kind;
            Value = value;
            Position = position;
        }

        public override string ToString()
            => $"CombatFeedbackEvent({Kind}, value={Value}, pos={Position})";
    }

    /// <summary>
    /// Static event hub cho combat feedback. Cho phép nhiều subscriber
    /// (CombatFeedbackView, HitEffectSpawner, CameraShake) cùng nhận event
    /// từ runner mà không cần runner giữ reference trực tiếp.
    ///
    /// Lưu ý: thread-safety. Các call Raise() chỉ an toàn từ main thread
    /// (Unity convention). BackendClientRunner.RunAsync() chạy trên main
    /// thread (Unity default cho async/await không có ConfigureAwait), nên
    /// Raise() ở đó cũng main thread. Nếu cần cross-thread, bọc Raise bằng
    /// một main-thread dispatcher (chưa cần trong FS-03D).
    /// </summary>
    public static class CombatFeedbackBus
    {
        /// <summary>Event chính — subscribe trong OnEnable, unsubscribe trong OnDisable.</summary>
        public static event Action<CombatFeedbackEvent> OnFeedback;

        /// <summary>
        /// Publish một event lên bus. Idempotent — gọi nhiều lần Raise cùng
        /// payload sẽ fire subscriber nhiều lần (đúng kỳ vọng cho view spawn
        /// nhiều số damage).
        /// </summary>
        public static void Raise(CombatFeedbackEvent evt)
        {
            // Copy delegate để tránh race condition nếu subscriber unsubscribe
            // giữa chừng khi đang invoke.
            var handler = OnFeedback;
            if (handler != null)
            {
                try
                {
                    handler.Invoke(evt);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[CombatFeedbackBus] subscriber threw: {ex}");
                }
            }
        }

        /// <summary>
        /// Clear tất cả subscribers. Dùng cho test teardown để tránh leak
        /// giữa các test case. KHÔNG gọi trong production code.
        /// </summary>
        public static void ClearAllSubscribers()
        {
            OnFeedback = null;
        }
    }
}
