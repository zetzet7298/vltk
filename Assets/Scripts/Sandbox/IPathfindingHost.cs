// -----------------------------------------------------------------------------
// VLTK Mobile — IPathfindingHost: giao diện host cho PathfindingService.
// Cho phép runtime dispatch các side-effect khi A* tìm đường
// (UI debug overlay, log, SFX, NPC nav, broadcast).
// PC source: A* algorithm + lua path_event.
// PC surfaces: ShowPathOverlay, PlayPathSFX, Msg2Player, OnPathComplete.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho PathfindingService. Implement bởi UI/Debug/Nav/Chat.
    /// </summary>
    public interface IPathfindingHost
    {
        /// <summary>Hiển thị path overlay trên minimap / debug view (PC ShowPathOverlay).</summary>
        void ShowPathOverlay(Vector2Int[] cells, Vector2Int start, Vector2Int goal);

        /// <summary>Thông báo khi tìm thấy đường đi (PC OnPathFound).</summary>
        void OnPathFound(Vector2Int start, Vector2Int goal, int cellCount, int expandedNodes);

        /// <summary>Thông báo khi không tìm thấy đường đi (PC OnPathFailed).</summary>
        void OnPathFailed(Vector2Int start, Vector2Int goal, string failureReason, int expandedNodes);

        /// <summary>Phát SFX khi path bắt đầu tính toán (PC PlayPathSFX).</summary>
        void PlayPathSFX(Vector2Int start, Vector2Int goal);

        /// <summary>Log pathfinding event lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogPathEvent(Vector2Int start, Vector2Int goal, string message);

        /// <summary>Dispatch NPC nav: cập nhật path cho NPC di chuyển (PC NPCSetPath).</summary>
        void DispatchNpcNav(int npcId, Vector2Int[] cells);

        /// <summary>Phát thưởng cho player nếu path dài / nhanh (PC AddMoney / AddExp).</summary>
        void GrantPathReward(int playerId, int cellCount, int expandedNodes);

        /// <summary>Lưu path history vào DB player (PC SavePathHistory).</summary>
        void SavePathHistory(int playerId, Vector2Int start, Vector2Int goal, int cellCount);
    }
}
