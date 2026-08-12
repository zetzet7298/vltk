using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Own-design tuning cho collect: magnet radius/speed + pickup + gem lifetime.
    /// P1 defaults khớp giá trị cũ (PickupRadius 1.6 / speed 8 / 0.4) để không phá feel;
    /// lifetime = chống tích tụ gem (parity-shape ActorWaveCmpt monster lifetime).
    /// </summary>
    [System.Serializable]
    public struct CollectSettings
    {
        public float MagnetRadius;   // bán kính bắt đầu hút (units)
        public float MagnetSpeed;    // tốc độ hút (units/s)
        public float PickupDistance; // khoảng cách <= này là nhặt được
        public float GemLifetime;    // giây; hết hạn gem tự hủy

        public static CollectSettings Default()
        {
            return new CollectSettings
            {
                MagnetRadius = 1.6f,
                MagnetSpeed = 8f,
                PickupDistance = 0.4f,
                GemLifetime = 10f,
            };
        }
    }

    /// <summary>
    /// Pure magnet math — testable EditMode, không scene.
    /// </summary>
    public static class MagnetMath
    {
        /// <summary>
        /// Một bước hút gem về player. Trả true = pickup (gem về đúng vị trí player).
        /// Ngoài radius: đứng yên. Trong radius: bước = min(speed*dt, dist - pickupDistance)
        /// → không overshoot, frame kế tiếp rơi vào pickup.
        /// </summary>
        public static bool Pull(Vector2 gemPos, Vector2 playerPos, CollectSettings s, float dt, out Vector2 newPos)
        {
            Vector2 d = playerPos - gemPos;
            float dist = d.magnitude;
            if (dist > s.MagnetRadius)
            {
                newPos = gemPos;
                return false;
            }
            // epsilon chống float drift (vd 5−4.6 = 0.4000001 > 0.4 → miss pickup)
            if (dist <= s.PickupDistance + 1e-4f)
            {
                newPos = playerPos;
                return true;
            }
            float step = Mathf.Min(s.MagnetSpeed * dt, dist - s.PickupDistance);
            newPos = gemPos + d.normalized * step;
            return false;
        }
    }
}
