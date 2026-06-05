// -----------------------------------------------------------------------------
// VLTK Mobile — PC item suite_activate_count + ext_suite_activate_count parser
// Source: PC item/suite_activate_count.txt and ext_suite_activate_count.txt
// suite_activate_count: SuiteNo, ActivateCount
// ext_suite_activate_count: SuiteNo, ActivateCount1, ActivateCount2
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcSuiteCountEntry
    {
        public int Id { get; set; }
        public int Count { get; set; }
        public string Source { get; set; }
    }

    public sealed class PcSuiteCountRegistry
    {
        private readonly Dictionary<int, PcSuiteCountEntry> _byId = new Dictionary<int, PcSuiteCountEntry>();
        public int Count => _byId.Count;
        public PcSuiteCountEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcSuiteCountEntry> All => _byId.Values;
        public void Add(PcSuiteCountEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcSuiteCountParser
    {
        public static PcSuiteCountRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcSuiteCountRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            foreach (var fname in new[] { "suite_activate_count.txt", "ext_suite_activate_count.txt" })
            {
                var path = Path.Combine(absoluteDir, fname);
                if (!File.Exists(path)) continue;
                var lines = PcMapListParser.ReadLines(path);
                foreach (var raw in lines)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var line = raw.Trim();
                    if (line.Length == 0) continue;
                    var cols = line.Split('\t');
                    if (cols.Length < 2) continue;
                    // Skip header
                    if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int suiteId)) continue;
                    if (!int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)) continue;
                    reg.Add(new PcSuiteCountEntry
                    {
                        Id = suiteId,
                        Count = count,
                        Source = fname
                    });
                }
            }
            return reg;
        }
    }
}
