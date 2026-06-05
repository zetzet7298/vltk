// -----------------------------------------------------------------------------
// VLTK Mobile — WeatherConfigService: runtime service cho PC weather/weather.ini
// Bổ sung cho WeatherService: lookup config thời tiết chi tiết (lifeTime, particles, etc.)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class WeatherConfigService
    {
        private readonly PcWeatherConfigRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public WeatherConfigService() { _reg = new PcWeatherConfigRegistry(); }
        public WeatherConfigService(PcWeatherConfigRegistry reg) { _reg = reg ?? new PcWeatherConfigRegistry(); }

        public static WeatherConfigService LoadFromStreamingAssets(string subDir = "Reference/PcWeather")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new WeatherConfigService(PcWeatherConfigParser.BuildRegistry(path));
        }

        public PcWeatherConfigEntry Get(string weatherId) => _reg.Get(weatherId);
        public IReadOnlyList<PcWeatherConfigEntry> All => _reg.All;
    }
}
