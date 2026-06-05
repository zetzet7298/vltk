// -----------------------------------------------------------------------------
// VLTK Mobile — PC product_config.ini parser
// Source: settings/product_config.ini (GB2312). INI key=value: cấu hình vùng
// và ngôn ngữ của sản phẩm (Region=4 cho VN, Language=5 cho VIETNAM).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcProductConfigEntry
    {
        public string SectionName { get; set; }
        public Dictionary<string, string> KeyValues { get; set; } = new Dictionary<string, string>();
    }

    public sealed class PcProductConfigRegistry
    {
        private readonly Dictionary<string, PcProductConfigEntry> _bySection = new Dictionary<string, PcProductConfigEntry>();
        public int Count => _bySection.Count;
        public PcProductConfigEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcProductConfigEntry> All => _bySection.Values;
        public void Add(PcProductConfigEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcProductConfigParser
    {
        public static PcProductConfigRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcProductConfigRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "product_config.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcProductConfigEntry current = null;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    var name = line.Substring(1, line.Length - 2).Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    current = new PcProductConfigEntry { SectionName = name };
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
