// -----------------------------------------------------------------------------
// VLTK Mobile — PC partner_event.ini parser (sự kiện đồng hành)
// Source: settings/partner/partner_event.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcPartnerEventEntry
    {
        public string SectionName { get; set; }
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    public sealed class PcPartnerEventRegistry
    {
        private readonly Dictionary<string, PcPartnerEventEntry> _bySection = new Dictionary<string, PcPartnerEventEntry>();
        public int Count => _bySection.Count;
        public PcPartnerEventEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcPartnerEventEntry> All => _bySection.Values;
        public void Add(PcPartnerEventEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcPartnerEventParser
    {
        public static PcPartnerEventRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcPartnerEventRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "partner_event.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcText.ReadLinesTcvn3(path);
            PcPartnerEventEntry current = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var section = line.Substring(1, line.Length - 2).Trim();
                    current = new PcPartnerEventEntry { SectionName = section };
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
