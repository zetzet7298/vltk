// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/weather/weather.ini parser
// Source: settings/weather/weather.ini (GBK). INI format.
// Mỗi section [WeatherNNN] = 1 loại thời tiết với các key=LifeTimeMin, ParticleNum, ...
// WeatherService đã có runtime. Parser này bổ sung lookup config chi tiết.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [Serializable]
    public class PcWeatherConfigEntry
    {
        public string weatherId;
        public Dictionary<string, string> properties = new(StringComparer.OrdinalIgnoreCase);
        public string Get(string key) => properties.TryGetValue(key, out var v) ? v : string.Empty;
        public int GetInt(string key, int defaultVal = 0)
        {
            var s = Get(key);
            return int.TryParse(s, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int v) ? v : defaultVal;
        }
    }

    public sealed class PcWeatherConfigRegistry
    {
        private readonly Dictionary<string, PcWeatherConfigEntry> _byId = new(StringComparer.OrdinalIgnoreCase);
        public int Count => _byId.Count;
        public void Register(PcWeatherConfigEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.weatherId)) return;
            _byId[e.weatherId] = e;
        }
        public PcWeatherConfigEntry Get(string weatherId)
            => _byId.TryGetValue(weatherId ?? string.Empty, out var v) ? v : null;
        public IReadOnlyList<PcWeatherConfigEntry> All => new List<PcWeatherConfigEntry>(_byId.Values);
    }

    public static class PcWeatherConfigParser
    {
        public static PcWeatherConfigRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcWeatherConfigRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "weather.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcWeatherConfigEntry current = null;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '#' || line.StartsWith("//")) continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var section = line.Substring(1, line.Length - 2).Trim();
                    current = new PcWeatherConfigEntry { weatherId = section };
                    reg.Register(current);
                    continue;
                }
                if (current == null) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                if (key.Length > 0) current.properties[key] = val;
            }
            return reg;
        }
    }
}
