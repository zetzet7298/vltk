// -----------------------------------------------------------------------------
// VLTK Mobile — PC taxrates.ini parser (thuế kinh tế)
// Source: settings/taxrates.ini (GB2312). INI format with [Main], [CityRates] sections
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTaxRateEntry
    {
        public string SectionName { get; set; }
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    public sealed class PcTaxRateRegistry
    {
        private readonly Dictionary<string, PcTaxRateEntry> _bySection = new Dictionary<string, PcTaxRateEntry>();
        public int Count => _bySection.Count;
        public PcTaxRateEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcTaxRateEntry> All => _bySection.Values;
        public void Add(PcTaxRateEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcTaxRateParser
    {
        public static PcTaxRateRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTaxRateRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "taxrates.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcTaxRateEntry current = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line)) continue;
                if (line.StartsWith(";") || line.StartsWith("#")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var section = line.Substring(1, line.Length - 2).Trim();
                    current = new PcTaxRateEntry { SectionName = section };
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
