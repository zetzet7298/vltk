// -----------------------------------------------------------------------------
// VLTK Mobile — IWeatherHost: giao diện host cho WeatherService.
// Cho phép runtime dispatch các side-effect khi đổi thời tiết theo map/giờ
// (particle system, sky tint, fog, ambient sound, UI indicator).
// PC source: settings/weather/weather.ini + weather.txt + lua weather_cycle.
// PC surfaces: SetWeatherEffect, SetFogColor, SetSkyColor, SetAmbientSFX.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    public enum WeatherType
    {
        Sunny = 0,    // Nắng
        Rain = 1,     // Mưa
        Snow = 2,     // Tuyết
        Fog = 3,      // Sương Mù
        Storm = 4,    // Bão
    }

    /// <summary>
    /// Host-side callbacks cho WeatherService. Implement bởi Rendering/Audio/UI.
    /// </summary>
    public interface IWeatherHost
    {
        /// <summary>Áp dụng hiệu ứng thời tiết (particle, sky tint, fog).</summary>
        void ApplyWeatherEffect(int mapId, int weatherType, int effectId, float probability);

        /// <summary>Phát âm thanh ambient theo thời tiết (PC SetAmbientSFX).</summary>
        void PlayAmbientSFX(int mapId, int weatherType);

        /// <summary>Dừng hiệu ứng thời tiết hiện tại (khi đổi map hoặc clear weather).</summary>
        void ClearWeatherEffect(int mapId);

        /// <summary>Đặt màu sương mù theo weather (PC SetFogColor).</summary>
        void SetFogColor(int mapId, int weatherType);

        /// <summary>Đặt màu bầu trời theo weather (PC SetSkyColor).</summary>
        void SetSkyColor(int mapId, int weatherType);

        /// <summary>Thông báo thời tiết lên UI minimap/notification (PC ShowWeatherNotice).</summary>
        void ShowWeatherNotice(int mapId, int weatherType);

        /// <summary>Log thời tiết đã chọn lên kênh chat hệ thống (PC Msg2Player).</summary>
        void LogWeatherChange(int mapId, int oldWeather, int newWeather);
    }
}
