// -----------------------------------------------------------------------------
// VLTK Mobile — PC mission/maze config parser (Ngọc Long Sơn Trang task info)
// Source: settings/missions/maze/taskinfo.txt (GB2312).
// Format: TaskID, TaskName, TaskInfo (tab-separated, header row skipped).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>One maze task entry.</summary>
    public class PcMissionMazeEntry
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public string TaskInfo { get; set; }
    }

    public sealed class PcMissionMazeRegistry
    {
        private readonly Dictionary<int, PcMissionMazeEntry> _byId = new Dictionary<int, PcMissionMazeEntry>();
        public int Count => _byId.Count;
        public PcMissionMazeEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMissionMazeEntry> All => _byId.Values;
        public void Add(PcMissionMazeEntry e) { if (e != null) _byId[e.TaskId] = e; }
    }

    public static class PcMissionMazeParser
    {
        public static PcMissionMazeRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcMissionMazeRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "taskinfo.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 3) cols = line.Split(',');
                if (cols.Length < 3) continue;
                // Skip header row
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                reg.Add(new PcMissionMazeEntry
                {
                    TaskId = id,
                    TaskName = cols[1].Trim(),
                    TaskInfo = cols[2].Trim()
                });
            }
            return reg;
        }
    }
}
