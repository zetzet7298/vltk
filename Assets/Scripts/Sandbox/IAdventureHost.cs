// -----------------------------------------------------------------------------
// VLTK Mobile — IAdventureHost: giao diện host cho AdventureService.
// Cho phép runtime dispatch các side-effect khi mục mạo hiểm hoàn thành
// (UI minimap marker, log, broadcast, phần thưởng, save).
// PC source: settings/adventure.txt + lua adventure_event.
// PC surfaces: Msg2Player, AddItemEx, broadcast, ShowAdventureMapPin.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho AdventureService. Implement bởi UI/Map/Chat/Notify.
    /// </summary>
    public interface IAdventureHost
    {
        /// <summary>Hiển thị marker hoàn thành trên bản đồ mạo hiểm (PC ShowAdventureMapPin).</summary>
        void ShowMapPin(int advId, int mapId, bool isCompleted);

        /// <summary>Thông báo khi player hoàn thành mục mạo hiểm (PC Msg2Player).</summary>
        void OnAdventureCompleted(int playerId, int advId, string adventureName, int mapId);

        /// <summary>Phát thưởng khi hoàn thành mục (PC AddItemEx / AddMoney / AddExp).</summary>
        void GrantAdventureReward(int playerId, int advId, int rewardItem, int rewardCount);

        /// <summary>Cập nhật progress bar UI (PC SetAdventureProgress).</summary>
        void UpdateProgress(int playerId, int completed, int total, float ratio);

        /// <summary>Log thông báo mạo hiểm lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogAdventureEvent(int playerId, int advId, string message);

        /// <summary>Broadcast khi player hoàn thành 100% tất cả mục (PC broadcast).</summary>
        void OnAllAdventuresCompleted(int playerId, int totalCount);

        /// <summary>Lưu tiến độ mạo hiểm vào DB (PC SaveAdventureProgress).</summary>
        void SaveAdventureProgress(int playerId, int advId, bool completed);
    }
}
