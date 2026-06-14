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
        private IWeatherHost _host;
        private int _lastAppliedMapId = -1;
        private int _lastAppliedWeather = -1;

        /// <summary>Event khi load xong toàn bộ catalog thời tiết.</summary>
        public event Action OnWeatherLoaded;

        /// <summary>Event khi đổi thời tiết runtime cho map (mapId, weatherType).</summary>
        public event Action<int, int> OnWeatherChanged;

        public int Count => _registry != null ? _registry.Count : 0;
        public int LastAppliedMapId => _lastAppliedMapId;
        public int LastAppliedWeather => _lastAppliedWeather;

        public WeatherService() : this(null, null) { }
        public WeatherService(PcWeatherRegistry registry) : this(registry, null) { }
        public WeatherService(PcWeatherRegistry registry, IWeatherHost host)
        {
            _host = host;
            AttachRegistry(registry);
        }

        public void AttachHost(IWeatherHost host) { _host = host; }

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

        /// <summary>Resolve + dispatch thời tiết cho map tại giờ hiện tại. Trả về entry áp dụng (hoặc null).</summary>
        public PcWeatherEntry ResolveAndApply(int mapId, int hour)
        {
            var entry = GetWeatherForMap(mapId, hour);
            if (entry == null) return null;
            int oldWeather = _lastAppliedWeather;
            _lastAppliedMapId = mapId;
            _lastAppliedWeather = entry.weatherType;
            OnWeatherChanged?.Invoke(mapId, entry.weatherType);
            if (_host != null)
            {
                _host.ApplyWeatherEffect(mapId, entry.weatherType, entry.effectId, entry.probability);
                _host.PlayAmbientSFX(mapId, entry.weatherType);
                _host.SetFogColor(mapId, entry.weatherType);
                _host.SetSkyColor(mapId, entry.weatherType);
                _host.ShowWeatherNotice(mapId, entry.weatherType);
                _host.LogWeatherChange(mapId, oldWeather, entry.weatherType);
            }
            return entry;
        }

        /// <summary>Clear thời tiết hiện tại (khi rời map hoặc reset).</summary>
        public void ClearWeather(int mapId)
        {
            _lastAppliedMapId = -1;
            _lastAppliedWeather = -1;
            if (_host != null) _host.ClearWeatherEffect(mapId);
        }

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
