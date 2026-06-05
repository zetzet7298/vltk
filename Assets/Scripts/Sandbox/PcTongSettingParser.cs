// -----------------------------------------------------------------------------
// VLTK Mobile — PC tong_setting.ini parser (cấu hình bang hội)
// Source: settings/tong_setting.ini (GB2312). Grouped key/value.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTongSettingEntry
    {
        public int SettingId { get; set; }
        public int MaxMember { get; set; }
        public int MaxElder { get; set; }
        public int MaxStunt { get; set; }
        public int FoundingCost { get; set; }
        public int UpgradeCost { get; set; }
    }

    public sealed class PcTongSettingRegistry
    {
        private readonly Dictionary<int, PcTongSettingEntry> _byId = new Dictionary<int, PcTongSettingEntry>();
        public int Count => _byId.Count;
        public PcTongSettingEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcTongSettingEntry> All => _byId.Values;
        public void Add(PcTongSettingEntry e) { if (e != null) _byId[e.SettingId] = e; }
    }

    public static class PcTongSettingParser
    {
        public static PcTongSettingRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTongSettingRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "tong_setting.ini");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            var groups = new Dictionary<int, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']') continue;
                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                int underscore = key.IndexOf('_');
                if (underscore <= 0) continue;
                if (!int.TryParse(key.Substring(0, underscore), NumberStyles.Integer, CultureInfo.InvariantCulture, out int sid)) continue;
                var subKey = key.Substring(underscore + 1);
                if (!groups.TryGetValue(sid, out var bag))
                {
                    bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    groups[sid] = bag;
                }
                bag[subKey] = value;
            }
            foreach (var kv in groups)
            {
                var b = kv.Value;
                var e = new PcTongSettingEntry
                {
                    SettingId = kv.Key,
                    MaxMember = b.TryGetValue("MaxMember", out var mm) && int.TryParse(mm, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mmv) ? mmv : 50,
                    MaxElder = b.TryGetValue("MaxElder", out var me) && int.TryParse(me, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mev) ? mev : 5,
                    MaxStunt = b.TryGetValue("MaxStunt", out var ms) && int.TryParse(ms, NumberStyles.Integer, CultureInfo.InvariantCulture, out int msv) ? msv : 5,
                    FoundingCost = b.TryGetValue("FoundingCost", out var fc) && int.TryParse(fc, NumberStyles.Integer, CultureInfo.InvariantCulture, out int fcv) ? fcv : 100000,
                    UpgradeCost = b.TryGetValue("UpgradeCost", out var uc) && int.TryParse(uc, NumberStyles.Integer, CultureInfo.InvariantCulture, out int ucv) ? ucv : 1000000
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
