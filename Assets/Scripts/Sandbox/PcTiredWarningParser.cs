// -----------------------------------------------------------------------------
// VLTK Mobile — PC tiredwarning.ini parser
// Source: settings/tiredwarning.ini (GB2312). INI key=value under [Config]:
// thời gian cảnh báo mệt mỏi của hệ thống phòng chống nghiện game.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTiredWarningEntry
    {
        public string SectionName { get; set; }
        public Dictionary<string, string> KeyValues { get; set; } = new Dictionary<string, string>();
    }

    public sealed class PcTiredWarningRegistry
    {
        private readonly Dictionary<string, PcTiredWarningEntry> _bySection = new Dictionary<string, PcTiredWarningEntry>();
        public int Count => _bySection.Count;
        public PcTiredWarningEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcTiredWarningEntry> All => _bySection.Values;
        public void Add(PcTiredWarningEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcTiredWarningParser
    {
        public static PcTiredWarningRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTiredWarningRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "tiredwarning.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcTiredWarningEntry current = null;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var name = line.Substring(1, line.Length - 2).Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    current = new PcTiredWarningEntry { SectionName = name };
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
