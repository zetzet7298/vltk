// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/weather/weather.ini weather (thời tiết) parser
// Source: weather.ini (GB2312, INI sections like [Weather000]).
//   Each section: LifeTimeMin, ParticleNum, DownSpeed, WindSpeed, FlareTime, + flags.
//   We extract weather *templates* by section + record type hints. Mobile runtime
//   also accepts a tab-separated `weather.txt` fallback (MapId, WeatherType,
//   StartHour, EndHour, Probability, EffectId).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcWeatherParser
    {
        public const string WeatherTabFile = "weather.txt";

        public static List<PcWeatherEntry> ParseFile(string path)
        {
            var rows = new List<PcWeatherEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            string ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext == ".ini")
                return ParseIni(path);
            return ParseTab(path);
        }

        // weather.txt format: MapId \t WeatherType \t StartHour \t EndHour \t Probability \t EffectId
        private static List<PcWeatherEntry> ParseTab(string path)
        {
            var rows = new List<PcWeatherEntry>();
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcWeatherEntry
                {
                    mapId = PcItemCommon.Int(cols, 0),
                    weatherType = PcItemCommon.Int(cols, 1),
                    startHour = cols.Length > 2 ? PcItemCommon.Int(cols, 2) : 0,
                    endHour = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 23,
                    probability = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 100,
                    effectId = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 0,
                    nameRaw = cols.Length > 6 ? PcItemCommon.Str(cols, 6) : string.Empty,
                });
            }
            return rows;
        }

        // weather.ini: each [Weather###] section becomes one entry. weatherType derived
        // from the section index. We also pull LifeTimeMin as the duration hint.
        private static List<PcWeatherEntry> ParseIni(string path)
        {
            var rows = new List<PcWeatherEntry>();
            var lines = PcItemCommon.ReadServerLines(path);
            string currentSection = string.Empty;
            int lifeTimeMin = 0;
            int particleNum = 0;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("//")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (!string.IsNullOrEmpty(currentSection))
                    {
                        rows.Add(new PcWeatherEntry
                        {
                            mapId = 0,
                            weatherType = ExtractWeatherIndex(currentSection),
                            startHour = 0,
                            endHour = 23,
                            probability = 100,
                            effectId = particleNum,
                            nameRaw = currentSection,
                            lifeTimeMin = lifeTimeMin,
                        });
                    }
                    currentSection = line.Substring(1, line.Length - 2);
                    lifeTimeMin = 0;
                    particleNum = 0;
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                if (string.Equals(key, "LifeTimeMin", System.StringComparison.OrdinalIgnoreCase))
                    int.TryParse(val, out lifeTimeMin);
                else if (string.Equals(key, "ParticleNum", System.StringComparison.OrdinalIgnoreCase))
                    int.TryParse(val, out particleNum);
            }
            if (!string.IsNullOrEmpty(currentSection))
            {
                rows.Add(new PcWeatherEntry
                {
                    mapId = 0,
                    weatherType = ExtractWeatherIndex(currentSection),
                    startHour = 0,
                    endHour = 23,
                    probability = 100,
                    effectId = particleNum,
                    nameRaw = currentSection,
                    lifeTimeMin = lifeTimeMin,
                });
            }
            return rows;
        }

        private static int ExtractWeatherIndex(string section)
        {
            if (string.IsNullOrEmpty(section)) return 0;
            string tail = section;
            if (tail.StartsWith("Weather", System.StringComparison.OrdinalIgnoreCase))
                tail = tail.Substring(7);
            return int.TryParse(tail, out int n) ? n : 0;
        }

        public static PcWeatherRegistry BuildRegistry(string dir)
        {
            var reg = new PcWeatherRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Prefer tab-separated weather.txt at dir root; fall back to ini scan.
            string tab = Path.Combine(dir, WeatherTabFile);
            if (File.Exists(tab))
            {
                foreach (var s in ParseFile(tab)) reg.Register(s);
                return reg;
            }
            foreach (var f in Directory.GetFiles(dir, "*.ini", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcWeatherEntry
    {
        public int mapId;
        public int weatherType;       // 0=nắng, 1=mưa, 2=tuyết, 3=sương mù, 4=bão
        public int startHour;
        public int endHour;
        public int probability;       // 0-100
        public int effectId;          // particle/effect asset hint
        public int lifeTimeMin;       // ini: section duration
        public string nameRaw;
    }

    public sealed class PcWeatherRegistry
    {
        private readonly List<PcWeatherEntry> _all = new();
        public int Count => _all.Count;
        public IEnumerable<PcWeatherEntry> All => _all;
        public void Register(PcWeatherEntry e) { if (e == null) return; _all.Add(e); }

        public IReadOnlyList<PcWeatherEntry> GetForMap(int mapId)
        {
            var list = new List<PcWeatherEntry>();
            foreach (var e in _all)
                if (e != null && e.mapId == mapId) list.Add(e);
            return list;
        }

        public PcWeatherEntry Get(int mapId, int hour)
        {
            PcWeatherEntry fallback = null;
            foreach (var e in _all)
            {
                if (e == null || e.mapId != mapId) continue;
                if (hour >= e.startHour && hour <= e.endHour) return e;
                if (fallback == null) fallback = e;
            }
            return fallback;
        }
    }
}
