// -----------------------------------------------------------------------------
// VLTK Mobile — ICityWarHost: giao diện host cho CityWarService.
// Cho phép runtime dispatch các side-effect khi thành chiến (capture,
// defender update, reset, broadcast) — UI minimap, NPC spawn, log, SFX.
// PC source: settings/event/citywar.ini + lua citywar_event.
// PC surfaces: Msg2Faction, CreateNpc, broadcast, ShowMapMarker, SetCityOwner.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side callbacks cho CityWarService. Implement bởi UI/Map/NPC/Chat/Audio.
    /// </summary>
    public interface ICityWarHost
    {
        /// <summary>Thông báo thành bị chiếm / đổi chủ (PC broadcast + Msg2Faction).</summary>
        void OnCityOwnerChanged(int cityId, int oldOwnerFaction, int newOwnerFaction, string cityName);

        /// <summary>Spawn / xóa NPC trấn thủ tương ứng (PC CreateNpc / RemoveNpc).</summary>
        void UpdateDefenderNpcs(int cityId, int factionId, int defenderCount);

        /// <summary>Hiển thị marker thành chiến trên bản đồ minimap (PC ShowMapMarker).</summary>
        void ShowCityMarker(int cityId, int ownerFaction, string cityName);

        /// <summary>Phát SFX khi thành bị chiếm (PC PlayCaptureSFX).</summary>
        void PlayCaptureSFX(int cityId, int newOwnerFaction);

        /// <summary>Phát thưởng cho faction chiếm thành thành công (PC AddItemEx / AddMoney).</summary>
        void GrantCaptureReward(int cityId, int factionId, int rewardItem, int rewardCount);

        /// <summary>Log thông báo thành chiến lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogCityWarEvent(int cityId, int oldOwner, int newOwner, string message);

        /// <summary>Cập nhật UI leaderboard thành chiến (PC SetCityWarBoard).</summary>
        void UpdateLeaderboard(int cityId, int ownerFaction, int defenderCount, long captureTimestamp);

        /// <summary>Reset toàn bộ trạng thái thành chiến về ban đầu (PC ResetCityWar).</summary>
        void OnCityWarReset(int totalCities, int neutralCount);
    }
}
