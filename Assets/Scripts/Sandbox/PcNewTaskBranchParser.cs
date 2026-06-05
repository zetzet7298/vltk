// -----------------------------------------------------------------------------
// VLTK Mobile — PC newtask branch parser (nhiệm vụ nhánh tân thủ)
// Source: settings/task/newtask/branch/auxpasstask.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcNewTaskBranchEntry
    {
        public int ParentTaskId { get; set; }
        public int TaskId { get; set; }
        public int MinLevel { get; set; }
        public int MaxLevel { get; set; }
        public int Value { get; set; }
        public string Text { get; set; }
    }

    public sealed class PcNewTaskBranchRegistry
    {
        private readonly List<PcNewTaskBranchEntry> _entries = new List<PcNewTaskBranchEntry>();
        public int Count => _entries.Count;
        public IEnumerable<PcNewTaskBranchEntry> All => _entries;
        public void Add(PcNewTaskBranchEntry e) { if (e != null) _entries.Add(e); }
        public IEnumerable<PcNewTaskBranchEntry> GetByTaskId(int taskId)
        {
            foreach (var e in _entries) if (e.TaskId == taskId) yield return e;
        }
    }

    public static class PcNewTaskBranchParser
    {
        public static PcNewTaskBranchRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcNewTaskBranchRegistry();
            if (string.IsNullOrEmpty(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "auxpasstask.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parentTaskId)) continue;
                reg.Add(new PcNewTaskBranchEntry
                {
                    ParentTaskId = parentTaskId,
                    TaskId = cols.Length > 1 && int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tid) ? tid : 0,
                    MinLevel = cols.Length > 2 && int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int min) ? min : 0,
                    MaxLevel = cols.Length > 3 && int.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int max) ? max : 0,
                    Value = cols.Length > 4 && int.TryParse(cols[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0,
                    Text = cols.Length > 5 ? cols[5].Trim() : ""
                });
            }
            return reg;
        }
    }
}
