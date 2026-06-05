// -----------------------------------------------------------------------------
// VLTK Mobile — PC newtask mainpasstask parser (nhiệm vụ chính tuyến tân thủ)
// Source: settings/task/newtask/mastertask/mainpasstask.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcMainPassTaskEntry
    {
        public int TaskId { get; set; }
        public int Level { get; set; }
        public int Value { get; set; }
        public string Text { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }
    }

    public sealed class PcMainPassTaskRegistry
    {
        private readonly List<PcMainPassTaskEntry> _entries = new List<PcMainPassTaskEntry>();
        public int Count => _entries.Count;
        public IEnumerable<PcMainPassTaskEntry> All => _entries;
        public void Add(PcMainPassTaskEntry e) { if (e != null) _entries.Add(e); }
        public IEnumerable<PcMainPassTaskEntry> GetByTaskId(int taskId)
        {
            foreach (var e in _entries) if (e.TaskId == taskId) yield return e;
        }
    }

    public static class PcMainPassTaskParser
    {
        public static PcMainPassTaskRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMainPassTaskRegistry();
            if (string.IsNullOrEmpty(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "mainpasstask.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int taskId)) continue;
                reg.Add(new PcMainPassTaskEntry
                {
                    TaskId = taskId,
                    Level = cols.Length > 1 && int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lv) ? lv : 0,
                    Value = cols.Length > 2 && int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0,
                    Text = cols.Length > 3 ? cols[3].Trim() : "",
                    Title = cols.Length > 4 ? cols[4].Trim() : "",
                    Desc = cols.Length > 5 ? cols[5].Trim() : ""
                });
            }
            return reg;
        }
    }
}
