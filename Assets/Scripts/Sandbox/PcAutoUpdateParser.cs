// -----------------------------------------------------------------------------
// VLTK Mobile — PC autoupdate.ini parser
// Source: settings/autoupdate.ini (GB2312). INI key=value pairs grouped by
// [SectionName]. Mỗi section lưu các FTP site URL cho auto-update.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcAutoUpdateEntry
    {
        public string SectionName { get; set; }
        public Dictionary<string, string> KeyValues { get; set; } = new Dictionary<string, string>();
    }

    public sealed class PcAutoUpdateRegistry
    {
        private readonly Dictionary<string, PcAutoUpdateEntry> _bySection = new Dictionary<string, PcAutoUpdateEntry>();
        public int Count => _bySection.Count;
        public PcAutoUpdateEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcAutoUpdateEntry> All => _bySection.Values;
        public void Add(PcAutoUpdateEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcAutoUpdateParser
    {
        public static PcAutoUpdateRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcAutoUpdateRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "autoupdate.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcAutoUpdateEntry current = null;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var name = line.Substring(1, line.Length - 2).Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    current = new PcAutoUpdateEntry { SectionName = name };
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
