// -----------------------------------------------------------------------------
// VLTK Mobile — PC timertask.txt + systemtimetask.txt parser (định thời)
// Source: settings/timertask.txt, settings/systemtimetask.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTimerTaskEntry
    {
        public int TaskId { get; set; }
        public string Script { get; set; }
        public string Source { get; set; }
    }

    public sealed class PcTimerTaskRegistry
    {
        private readonly Dictionary<int, PcTimerTaskEntry> _byId = new Dictionary<int, PcTimerTaskEntry>();
        public int Count => _byId.Count;
        public PcTimerTaskEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcTimerTaskEntry> All => _byId.Values;
        public void Add(PcTimerTaskEntry e) { if (e != null) _byId[e.TaskId] = e; }
    }

    public static class PcTimerTaskParser
    {
        public static PcTimerTaskRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTimerTaskRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            foreach (var fname in new[] { "timertask.txt", "systemtimetask.txt" })
            {
                var path = Path.Combine(absoluteDir, fname);
                if (!File.Exists(path)) continue;
                var lines = PcMapListParser.ReadLines(path);
                foreach (var raw in lines)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
                    var cols = line.Split('\t');
                    if (cols.Length < 2) continue;
                    if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                    reg.Add(new PcTimerTaskEntry
                    {
                        TaskId = id,
                        Script = cols.Length > 1 ? cols[1].Trim() : "",
                        Source = fname
                    });
                }
            }
            return reg;
        }
    }
}
