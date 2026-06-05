// -----------------------------------------------------------------------------
// VLTK Mobile — PC player_limittime.ini parser
// Source: settings/player_limittime.ini (GB2312). INI key=value grouped by
// [Config] và [LimitTime]: cấu hình giới hạn thời gian chơi của người chơi.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcPlayerLimitEntry
    {
        public string SectionName { get; set; }
        public Dictionary<string, string> KeyValues { get; set; } = new Dictionary<string, string>();
    }

    public sealed class PcPlayerLimitRegistry
    {
        private readonly Dictionary<string, PcPlayerLimitEntry> _bySection = new Dictionary<string, PcPlayerLimitEntry>();
        public int Count => _bySection.Count;
        public PcPlayerLimitEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcPlayerLimitEntry> All => _bySection.Values;
        public void Add(PcPlayerLimitEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcPlayerLimitParser
    {
        public static PcPlayerLimitRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcPlayerLimitRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "player_limittime.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcPlayerLimitEntry current = null;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var name = line.Substring(1, line.Length - 2).Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    current = new PcPlayerLimitEntry { SectionName = name };
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
