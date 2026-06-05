// -----------------------------------------------------------------------------
// VLTK Mobile — PC forbititem.ini parser (cấm vật phẩm)
// Source: settings/forbititem.ini (GB2312). Format: [Item_X] section, key=value
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcForbitItemEntry
    {
        public string SectionName { get; set; }
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    public sealed class PcForbitItemRegistry
    {
        private readonly Dictionary<string, PcForbitItemEntry> _bySection = new Dictionary<string, PcForbitItemEntry>();
        public int Count => _bySection.Count;
        public PcForbitItemEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcForbitItemEntry> All => _bySection.Values;
        public void Add(PcForbitItemEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcForbitItemParser
    {
        public static PcForbitItemRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcForbitItemRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "forbititem.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcForbitItemEntry current = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var section = line.Substring(1, line.Length - 2).Trim();
                    if (section.Equals("Info", System.StringComparison.OrdinalIgnoreCase))
                    {
                        current = null;
                        continue;
                    }
                    current = new PcForbitItemEntry { SectionName = section };
                    reg.Add(current);
                    continue;
                }
                var eqIdx = line.IndexOf('=');
                if (eqIdx > 0 && current != null)
                {
                    var key = line.Substring(0, eqIdx).Trim();
                    var val = line.Substring(eqIdx + 1).Trim();
                    current.Values[key] = val;
                }
            }
            return reg;
        }
    }
}
