// -----------------------------------------------------------------------------
// VLTK Mobile — PC utilities.ini parser
// Source: settings/utilities.ini (GB2312). INI key=value grouped by section
// (DisguiseMask...): cấu hình tiện ích hệ thống (ngụy trang, cấm tính năng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcUtilitiesEntry
    {
        public string SectionName { get; set; }
        public Dictionary<string, string> KeyValues { get; set; } = new Dictionary<string, string>();
    }

    public sealed class PcUtilitiesRegistry
    {
        private readonly Dictionary<string, PcUtilitiesEntry> _bySection = new Dictionary<string, PcUtilitiesEntry>();
        public int Count => _bySection.Count;
        public PcUtilitiesEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcUtilitiesEntry> All => _bySection.Values;
        public void Add(PcUtilitiesEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcUtilitiesParser
    {
        public static PcUtilitiesRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcUtilitiesRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "utilities.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcUtilitiesEntry current = null;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var name = line.Substring(1, line.Length - 2).Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    current = new PcUtilitiesEntry { SectionName = name };
                    reg.Add(current);
                    continue;
                }
                if (current == null) continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var val = line.Substring(eq + 1).Trim();
                if (string.IsNullOrEmpty(key)) continue;
                current.KeyValues[key] = val;
            }
            return reg;
        }
    }
}
