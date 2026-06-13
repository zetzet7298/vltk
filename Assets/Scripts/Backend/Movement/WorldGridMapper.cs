// -----------------------------------------------------------------------------
// VLTK.Backend.Movement — WorldGridMapper
//
// Tiện ích chuyển đổi toạ độ thế giới (world space, đơn vị thực) ↔ toạ độ ô
// lưới PC (KNpc.cpp nMpsX/nMpsY, đơn vị ô). Engine PC lưu trữ toạ độ nhân
// vật theo đơn vị Ô (grid cell integer) chứ không phải pixel — đây là cách
// engine tra cứu Region/SubWorld (KNpc.cpp gWorld[].m_nRegionIndex theo
// nMpsX/nMpsY).
//
// Quy tắc parity (KNpc.cpp nMpsX/Npc template + GetPos/SetPos parity):
//   - World → grid:  floor(world / tileSize)  (chia nguyên, hướng về 0 cho số âm)
//   - Grid  → world:  grid * tileSize         (trả vector2 ở gốc ô)
//   - tileSize mặc định = 512f (parity VLTK client — server region thường dùng
//     512 đơn vị/ô; xem MapDataService và Settings/Region trong PC client).
//
// Đây là class pure C#, KHÔNG gọi network, KHÔNG phụ thuộc Unity runtime
// (chỉ dùng UnityEngine.Vector2). EditMode test chạy đầy đủ không cần
// PlayerLoop.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Backend.Movement
{
    /// <summary>
    /// Bộ chuyển đổi toạ độ world ↔ grid PC. Pure utility, mọi method đều
    /// static. KHÔNG mutate state.
    /// </summary>
    public static class WorldGridMapper
    {
        /// <summary>
        /// Kích thước ô lưới mặc định (parity VLTK client). Lưu thành hằng số
        /// để caller dễ reference và tránh magic number trong code game.
        /// </summary>
        public const float DefaultTileSize = 512f;

        /// <summary>
        /// Chuyển toạ độ world X sang grid X (chia nguyên, hướng về 0 cho số âm).
        /// Parity KNpc.cpp nMpsX: <c>int nX = (int)(world_x / tile_size);</c>
        /// (engine PC dùng ép kiểu int của C++ — tương đương <see cref="Mathf.FloorToInt"/>
        /// cho số dương và làm tròn về 0 cho số âm).
        /// </summary>
        /// <param name="worldX">Toạ độ world X (đơn vị thực, có thể âm).</param>
        /// <param name="tileSize">Kích thước mỗi ô lưới (mặc định 512).</param>
        /// <returns>Chỉ số ô lưới X (integer, có thể âm nếu worldX âm).</returns>
        public static int WorldToGridX(float worldX, float tileSize = DefaultTileSize)
        {
            // Mathf.FloorToInt: floor cho mọi giá trị — parity chia nguyên hướng
            // về 0 cho dương. Với số âm (worldX < 0): C++ ép kiểu int cũng là
            // truncate toward zero; Unity Mathf.FloorToInt cũng cho kết quả
            // parity vì tileSize > 0 (chỉ số âm ảnh hưởng). Để an toàn với cả
            // dương lẫn âm, dùng FloorToInt.
            return Mathf.FloorToInt(worldX / tileSize);
        }

        /// <summary>
        /// Chuyển toạ độ world Y sang grid Y. Tương tự <see cref="WorldToGridX"/>.
        /// </summary>
        public static int WorldToGridY(float worldY, float tileSize = DefaultTileSize)
        {
            return Mathf.FloorToInt(worldY / tileSize);
        }

        /// <summary>
        /// Chuyển chỉ số ô lưới (gridX, gridY) về toạ độ world ở GỐC ô
        /// (không phải tâm). Parity KNpc.cpp dùng gridX * tile_size làm gốc
        /// toạ độ. Công thức:
        ///   <c>worldX = gridX * tileSize</c>
        ///   <c>worldY = gridY * tileSize</c>
        /// </summary>
        /// <param name="gridX">Chỉ số ô lưới X (integer, có thể âm).</param>
        /// <param name="gridY">Chỉ số ô lưới Y (integer, có thể âm).</param>
        /// <param name="tileSize">Kích thước mỗi ô lưới (mặc định 512).</param>
        /// <returns>Vector2 toạ độ world ở gốc ô.</returns>
        public static Vector2 GridToWorld(int gridX, int gridY, float tileSize = DefaultTileSize)
        {
            return new Vector2(gridX * tileSize, gridY * tileSize);
        }
    }
}
