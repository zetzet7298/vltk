// -----------------------------------------------------------------------------
// VLTK Mobile — PC partner_setting.ini parser (cấu hình đồng hành)
// Source: settings/partner/partner_setting.ini
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcPartnerSettingEntry
    {
        public string SectionName { get; set; }
        public readonly Dictionary<string, string> Values = new Dictionary<string, string>();
    }

    public sealed class PcPartnerSettingRegistry
    {
        private readonly Dictionary<string, PcPartnerSettingEntry> _bySection = new Dictionary<string, PcPartnerSettingEntry>();
        public int Count => _bySection.Count;
        public PcPartnerSettingEntry Get(string section) => _bySection.TryGetValue(section, out var v) ? v : null;
        public IEnumerable<PcPartnerSettingEntry> All => _bySection.Values;
        public void Add(PcPartnerSettingEntry e) { if (e != null) _bySection[e.SectionName] = e; }
    }

    public static class PcPartnerSettingParser
    {
        public static PcPartnerSettingRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcPartnerSettingRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "partner_setting.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            PcPartnerSettingEntry current = null;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    var section = line.Substring(1, line.Length - 2).Trim();
                    current = new PcPartnerSettingEntry { SectionName = section };
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
