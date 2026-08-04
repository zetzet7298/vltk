using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Survivor
{
    /// <summary>
    /// Ticket 46 — Y-sort chung cho mọi actor visual (side-view XY, camera +Z):
    /// Y cao = xa camera = phải render TRƯỚC (sortingOrder thấp). Player + monster +
    /// proxy dùng CÙNG 1 công thức (tier không override thứ tự). Sandbox viết
    /// sortingOrder hardcode mỗi frame → bridge set sortingBaseOverride, Sandbox
    /// tự re-apply (MalePlayerVisual.ApplyFrame / PcNpcVisual.ApplyFrame mỗi tick).
    /// </summary>
    public static class ActorDepth
    {
        /// <summary>px/unit — khớp pixelsPerUnit bridge (JxPlayerVisual/JxNpcVisual = 40): 1 unit Y = 40 bậc order.</summary>
        public const float PixelsPerUnit = 40f;

        /// <summary>sortingOrder là field int16 (MapRenderer comment) — band hợp lệ.</summary>
        public const int ClampMin = short.MinValue; // -32768
        public const int ClampMax = short.MaxValue; // 32767

        /// <summary>
        /// Base order cho actor tại worldY: PlayerSortingOrder - worldY * ppu, clamp int16.
        /// Y cao → order thấp (render trước). Arena ±5.8 → ±232 bậc, không chạm clamp khi chơi.
        /// </summary>
        public static int BaseOrder(float worldY, float pixelsPerUnit = PixelsPerUnit)
        {
            return Mathf.Clamp(Mathf.RoundToInt(MapRenderer.PlayerSortingOrder - worldY * pixelsPerUnit), ClampMin, ClampMax);
        }
    }
}
