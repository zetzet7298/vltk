// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/citywar.ini city war (Bang chiến) parser
// Source: citywar.ini (GB2312). Section-based INI with [CityArea] etc.
//   AreaName01=...  AreaIncludes01=1,2,3 ...
//   + other sections (battle rules, schedules, owner scores).
// Mobile runtime only needs the area names + map lists for city-warp lookup.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcCityWarParser
    {
        public static List<PcCityWarArea> ParseFile(string path)
        {
            var rows = new List<PcCityWarArea>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            string currentSection = string.Empty;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2);
                    continue;
                }
                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                if (key.StartsWith("AreaName", System.StringComparison.OrdinalIgnoreCase))
                {
                    rows.Add(new PcCityWarArea
                    {
                        section = currentSection,
                        name = val,
                        key = key,
                    });
                }
                else if (key.StartsWith("AreaIncludes", System.StringComparison.OrdinalIgnoreCase))
                {
                    var last = rows.Count > 0 ? rows[rows.Count - 1] : null;
                    if (last != null && last.key == "AreaName" + key.Substring("AreaIncludes".Length))
                    {
                        foreach (var p in val.Split(','))
                        {
                            if (int.TryParse(p.Trim(), out int m)) last.mapIds.Add(m);
                        }
                    }
                }
                else
                {
                    rows.Add(new PcCityWarArea
                    {
                        section = currentSection,
                        name = key,
                        key = key,
                    });
                    if (int.TryParse(val, out int m)) rows[rows.Count - 1].mapIds.Add(m);
                }
            }
            return rows;
        }

        public static PcCityWarRegistry BuildRegistry(string dir)
        {
            var reg = new PcCityWarRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "citywar.ini");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcCityWarArea
    {
        public string section;
        public string name;
        public string key;
        public List<int> mapIds = new();
    }

    public sealed class PcCityWarRegistry
    {
        private readonly List<PcCityWarArea> _areas = new();
        public int Count => _areas.Count;
        public void Register(PcCityWarArea a) { if (a == null) return; _areas.Add(a); }
        public IEnumerable<PcCityWarArea> All => _areas;
    }
}
