// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.22 Weather Service (Thời tiết runtime)
// Wraps PcWeatherRegistry. PC source: settings/weather/* (weather.ini + weather.txt).
// Vietnamese: "Thời Tiết", "Nắng", "Mưa", "Tuyết", "Sương Mù", "Bão".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class WeatherService
    {
        public const string LogTag = "Weather";
        public const string DefaultStreamingDir = "Reference/PcWeather";

        private PcWeatherRegistry _registry;

        /// <summary>Event khi load xong toàn bộ catalog thời tiết.</summary>
        public event Action OnWeatherLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public WeatherService() { }
        public WeatherService(PcWeatherRegistry registry) { AttachRegistry(registry); }

        public void AttachRegistry(PcWeatherRegistry registry)
        {
            _registry = registry ?? new PcWeatherRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} mục thời tiết");
            OnWeatherLoaded?.Invoke();
        }

        public PcWeatherEntry GetWeatherForMap(int mapId, int hour)
            => _registry != null ? _registry.Get(mapId, hour) : null;

        public IReadOnlyList<PcWeatherEntry> GetAllWeatherForMap(int mapId)
            => _registry != null
                ? _registry.GetForMap(mapId)
                : (IReadOnlyList<PcWeatherEntry>)Array.Empty<PcWeatherEntry>();

        public IEnumerable<PcWeatherEntry> GetAllWeather()
            => _registry != null ? _registry.All : (IEnumerable<PcWeatherEntry>)Array.Empty<PcWeatherEntry>();

        public static WeatherService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new WeatherService();
            if (Directory.Exists(dir))
            {
                var reg = PcWeatherParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Weather: directory không tồn tại {dir}");
                svc.OnWeatherLoaded?.Invoke();
            }
            return svc;
        }
    }
}
