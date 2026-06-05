// -----------------------------------------------------------------------------
// VLTK Mobile — PC partner_bag.ini parser (túi đồ đồng hành)
// Source: settings/partner/partner_bag.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcPartnerBagEntry
    {
        public string SectionName { get; set; }
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    public sealed class PcPartnerBagRegistry
    {
        private readonly Dictionary<string, PcPartnerBagEntry> _bySection = new Dictionary<string, PcPartnerBagEntry>();
        public int Count => _bySection.Count;
        public PcPartnerBagEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcPartnerBagEntry> All => _bySection.Values;
        public void Add(PcPartnerBagEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcPartnerBagParser
    {
        public static PcPartnerBagRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcPartnerBagRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "partner_bag.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcPartnerBagEntry current = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var section = line.Substring(1, line.Length - 2).Trim();
                    current = new PcPartnerBagEntry { SectionName = section };
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
