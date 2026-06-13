// -----------------------------------------------------------------------------
// VLTK.Backend.Combat — CameraShake
// MonoBehaviour subscribe CombatFeedbackBus.OnFeedback để rung camera khi
// có feedback combat. Áp dụng khi:
//   - Normal hit: micro-rumble (magnitude * baseMagnitude)
//   - Crit: full shake (magnitude * critMultiplier * baseMagnitude)
//   - Miss: không shake (server trả damage=0 → không có cảm giác đau)
//   - Heal: shake nhẹ (positive feedback)
//
// Magnitude tỉ lệ với damage (value lớn → shake mạnh, nhưng capped bởi
// maxMagnitude để tránh giật hình khi đánh boss).
//
// Design:
//   - Lưu localPosition gốc trong Awake, restore sau khi shake xong.
//   - Shake áp dụng offset lên transform.localPosition (không phá camera
//     pose). KHÔNG dùng Cinemachine ImpulseSource để giữ zero external
//     dependency (FS-03D layer purely visual).
//   - Subscribe CombatFeedbackBus.OnFeedback trong OnEnable.
// -----------------------------------------------------------------------------

using System.Collections;
using UnityEngine;

namespace VLTK.Backend.Combat
{
    /// <summary>
    /// Rung camera theo cường độ damage. Attach lên Camera (hoặc GameObject
    /// cha của Camera) để mỗi Normal/Crit/Heal feedback tạo một cơn rung
    /// có thời hạn.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CameraShake : MonoBehaviour
    {
        // ----- Inspector -----

        [Tooltip("Magnitude gốc (world unit). Magnitude hiệu dụng = " +
                 "value * baseMagnitude * (1 nếu Normal, critMultiplier nếu Crit, " +
                 "healMultiplier nếu Heal).")]
        [Min(0f)]
        public float baseMagnitude = 0.02f;

        [Tooltip("Multiplier cho đòn chí mạng.")]
        [Min(0f)]
        public float critMultiplier = 3.0f;

        [Tooltip("Multiplier cho heal (thường &lt; 1 để rung nhẹ).")]
        [Min(0f)]
        public float healMultiplier = 0.5f;

        [Tooltip("Tỉ lệ magnitude theo damage (value=10 → 10 * damageScale * base).")]
        [Min(0f)]
        public float damageScale = 0.005f;

        [Tooltip("Magnitude tối đa (cap) — tránh giật hình khi đánh boss.")]
        [Min(0f)]
        public float maxMagnitude = 0.3f;

        [Tooltip("Thời gian rung (giây).")]
        [Min(0.05f)]
        public float shakeDuration = 0.2f;

        [Tooltip("Tần số rung (Hz) — random offset mỗi 1/freq giây.")]
        [Min(1f)]
        public float shakeFrequency = 30f;

        // ----- Runtime -----

        private Vector3 _baseLocalPosition;
        private Coroutine _shakeRoutine;
        private bool _hasBasePos;

        // ----- Lifecycle -----

        private void Awake()
        {
            _baseLocalPosition = transform.localPosition;
            _hasBasePos = true;
        }

        private void OnEnable()
        {
            CombatFeedbackBus.OnFeedback += HandleFeedback;
        }

        private void OnDisable()
        {
            CombatFeedbackBus.OnFeedback -= HandleFeedback;
            StopShake();
        }

        // ----- Event handler -----

        private void HandleFeedback(CombatFeedbackEvent evt)
        {
            if (evt.IsMiss) return; // miss: không rung
            float multiplier = evt.IsCritical ? critMultiplier
                              : evt.IsHeal ? healMultiplier
                              : 1f;
            float magnitude = evt.Value * damageScale * baseMagnitude * multiplier;
            magnitude = Mathf.Min(magnitude, maxMagnitude);
            TriggerShake(magnitude);
        }

        /// <summary>
        /// Public API để các hệ thống khác (Cinematic, Boss phase change) có
        /// thể trigger shake trực tiếp mà không qua bus.
        /// </summary>
        public void TriggerShake(float magnitude)
        {
            if (magnitude <= 0f) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(magnitude));
        }

        private IEnumerator ShakeRoutine(float magnitude)
        {
            float t = 0f;
            float interval = 1f / shakeFrequency;
            float nextSample = 0f;
            while (t < shakeDuration)
            {
                if (Time.time >= nextSample)
                {
                    Vector3 offset = new(
                        Random.Range(-magnitude, magnitude),
                        Random.Range(-magnitude, magnitude),
                        0f);
                    transform.localPosition = _baseLocalPosition + offset;
                    nextSample = Time.time + interval;
                }
                t += Time.deltaTime;
                yield return null;
            }
            transform.localPosition = _baseLocalPosition;
            _shakeRoutine = null;
        }

        private void StopShake()
        {
            if (_shakeRoutine != null)
            {
                StopCoroutine(_shakeRoutine);
                _shakeRoutine = null;
            }
            if (_hasBasePos) transform.localPosition = _baseLocalPosition;
        }

        // ----- Test helpers -----

        /// <summary>True khi đang shake (test inspection).</summary>
        public bool IsShaking => _shakeRoutine != null;

        /// <summary>Override base position (test setup). Gọi SAU khi attach.</summary>
        public void SetBasePosition(Vector3 baseLocalPos)
        {
            _baseLocalPosition = baseLocalPos;
            _hasBasePos = true;
            transform.localPosition = baseLocalPos;
        }
    }
}
