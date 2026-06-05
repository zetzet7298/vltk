// -----------------------------------------------------------------------------
// VLTK Mobile — PC droprate.ini parser (cấu hình drop rate NPC template)
// Source: settings/droprate.ini (GB2312). Grouped key/value.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcDropRateConfigEntry
    {
        public int NpcTemplateId { get; set; }
        public int ItemGenre { get; set; }
        public int ItemDetail { get; set; }
        public int ItemParticular { get; set; }
        public int DropRate { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
    }

    public sealed class PcDropRateConfigRegistry
    {
        private readonly Dictionary<int, PcDropRateConfigEntry> _byId = new Dictionary<int, PcDropRateConfigEntry>();
        public int Count => _byId.Count;
        public PcDropRateConfigEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcDropRateConfigEntry> All => _byId.Values;
        public IEnumerable<PcDropRateConfigEntry> GetByNpcTemplate(int tpl)
        {
            foreach (var e in _byId.Values) if (e.NpcTemplateId == tpl) yield return e;
        }
        public void Add(PcDropRateConfigEntry e) { if (e != null) _byId[e.NpcTemplateId] = e; }
    }

    public static class PcDropRateConfigParser
    {
        public static PcDropRateConfigRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcDropRateConfigRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "droprate.ini");
            if (!File.Exists(path))
            {
                path = Path.Combine(absoluteDir, "droprate", "droprate.ini");
                if (!File.Exists(path)) return reg;
            }
            var lines = PcMapListParser.ReadLines(path);
            var groups = new Dictionary<int, Dictionary<string, string>>();
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
                if (!int.TryParse(key.Substring(0, underscore), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tid)) continue;
                var subKey = key.Substring(underscore + 1);
                if (!groups.TryGetValue(tid, out var bag))
                {
                    bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    groups[tid] = bag;
                }
                bag[subKey] = value;
            }
            foreach (var kv in groups)
            {
                var b = kv.Value;
                var e = new PcDropRateConfigEntry
                {
                    NpcTemplateId = kv.Key,
                    ItemGenre = b.TryGetValue("Genre", out var g) && int.TryParse(g, NumberStyles.Integer, CultureInfo.InvariantCulture, out int gv) ? gv : 0,
                    ItemDetail = b.TryGetValue("Detail", out var d) && int.TryParse(d, NumberStyles.Integer, CultureInfo.InvariantCulture, out int dv) ? dv : 0,
                    ItemParticular = b.TryGetValue("Particular", out var p) && int.TryParse(p, NumberStyles.Integer, CultureInfo.InvariantCulture, out int pv) ? pv : 0,
                    DropRate = b.TryGetValue("Rate", out var r) && int.TryParse(r, NumberStyles.Integer, CultureInfo.InvariantCulture, out int rv) ? rv : 0,
                    MinCount = b.TryGetValue("MinCount", out var mn) && int.TryParse(mn, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mnv) ? mnv : 1,
                    MaxCount = b.TryGetValue("MaxCount", out var mx) && int.TryParse(mx, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mxv) ? mxv : 1
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
