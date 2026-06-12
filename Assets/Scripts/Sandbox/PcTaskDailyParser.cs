// -----------------------------------------------------------------------------
// VLTK Mobile — PC task/dailytask/* parser (nhiệm vụ hàng ngày chi tiết)
// Source: server settings/task/dailytask/{gather,killmonster,talk,gather_pos,talk_pos}.txt
// Mỗi file có format khác nhau nhưng đều bắt đầu bằng TaskId.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTaskDailyEntry
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public int MapId { get; set; }
        public string MapName { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;   // gather / kill / talk
        public string TargetName { get; set; } = string.Empty;  // GatherName / MonsterName / NpcName
        public int Count { get; set; }
        public int G { get; set; }
        public int D { get; set; }
        public int P { get; set; }
        public int NpcGender { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int NpcRes { get; set; }
        public string NpcScript { get; set; } = string.Empty;
    }

    public sealed class PcTaskDailyRegistry
    {
        private readonly Dictionary<int, PcTaskDailyEntry> _byId = new Dictionary<int, PcTaskDailyEntry>();
        private readonly Dictionary<string, List<PcTaskDailyEntry>> _byType = new Dictionary<string, List<PcTaskDailyEntry>>();

        public int Count => _byId.Count;
        public IEnumerable<PcTaskDailyEntry> All => _byId.Values;

        public void Add(PcTaskDailyEntry e)
        {
            if (e == null || e.TaskId <= 0) return;
            _byId[e.TaskId] = e;
            string typeKey = e.TaskType ?? string.Empty;
            if (!_byType.TryGetValue(typeKey, out var list))
            {
                list = new List<PcTaskDailyEntry>();
                _byType[typeKey] = list;
            }
            list.Add(e);
        }

        public PcTaskDailyEntry Get(int taskId) => _byId.TryGetValue(taskId, out var v) ? v : null;
        public IReadOnlyList<PcTaskDailyEntry> GetByType(string type)
            => _byType.TryGetValue(type ?? string.Empty, out var v) ? v : System.Array.Empty<PcTaskDailyEntry>();
    }

    public static class PcTaskDailyParser
    {
        private static int TryInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string TryStr(string[] cols, int idx)
            => (idx < cols.Length) ? (cols[idx] ?? string.Empty).Trim() : string.Empty;

        private static void SkipHeader(string[] lines, int headerLines)
        {
            // first `headerLines` non-empty lines are skipped by caller iteration
        }

        public static PcTaskDailyRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTaskDailyRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            // gather.txt: TaskId  TaskName  MapId  MapName  GatherName  G  D  P  GatherCount
            ParseGather(reg, Path.Combine(absoluteDir, "gather.txt"));
            // killmonster.txt: TaskId  TaskName  MapId  MapName  MonsterName  KillCount
            ParseKill(reg, Path.Combine(absoluteDir, "killmonster.txt"));
            // talk.txt / talk_old.txt: TaskId  TaskName  MapName  NpcName  NpcGender
            ParseTalk(reg, Path.Combine(absoluteDir, "talk.txt"));
            ParseTalk(reg, Path.Combine(absoluteDir, "talk_old.txt"));
            // gather_pos.txt / talk_pos.txt: MapId  X  Y  NpcRes  NpcName  NpcScript  TaskId
            ParsePosition(reg, Path.Combine(absoluteDir, "gather_pos.txt"));
            ParsePosition(reg, Path.Combine(absoluteDir, "talk_pos.txt"));

            if (reg.Count == 0)
                SubsystemLog.Warn("TaskDaily", $"PcTaskDaily registry rỗng ({absoluteDir})");
            return reg;
        }

        private static void ParseGather(PcTaskDailyRegistry reg, string path)
        {
            if (!File.Exists(path)) return;
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                int id = TryInt(cols[0]);
                if (id <= 0) continue;
                if (cols[0].IndexOf("Task", System.StringComparison.OrdinalIgnoreCase) >= 0) continue; // header
                reg.Add(new PcTaskDailyEntry
                {
                    TaskId = id,
                    TaskName = TryStr(cols, 1),
                    MapId = TryInt(cols[2]),
                    MapName = TryStr(cols, 3),
                    TaskType = "gather",
                    TargetName = TryStr(cols, 4),
                    G = TryInt(cols[5]),
                    D = TryInt(cols[6]),
                    P = TryInt(cols[7]),
                    Count = TryInt(cols[8])
                });
            }
        }

        private static void ParseKill(PcTaskDailyRegistry reg, string path)
        {
            if (!File.Exists(path)) return;
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = TryInt(cols[0]);
                if (id <= 0) continue;
                if (cols[0].IndexOf("Task", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                reg.Add(new PcTaskDailyEntry
                {
                    TaskId = id,
                    TaskName = TryStr(cols, 1),
                    MapId = TryInt(cols[2]),
                    MapName = TryStr(cols, 3),
                    TaskType = "kill",
                    TargetName = TryStr(cols, 4),
                    Count = TryInt(cols[5])
                });
            }
        }

        private static void ParseTalk(PcTaskDailyRegistry reg, string path)
        {
            if (!File.Exists(path)) return;
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = TryInt(cols[0]);
                if (id <= 0) continue;
                if (cols[0].IndexOf("Task", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                reg.Add(new PcTaskDailyEntry
                {
                    TaskId = id,
                    TaskName = TryStr(cols, 1),
                    MapName = TryStr(cols, 2),
                    TaskType = "talk",
                    TargetName = TryStr(cols, 3),
                    NpcGender = TryInt(cols[4])
                });
            }
        }

        private static void ParsePosition(PcTaskDailyRegistry reg, string path)
        {
            if (!File.Exists(path)) return;
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 7) continue;
                if (cols[0].IndexOf("Map", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                int taskId = TryInt(cols[6]);
                if (taskId <= 0) continue;
                var existing = reg.Get(taskId);
                if (existing == null)
                {
                    reg.Add(new PcTaskDailyEntry
                    {
                        TaskId = taskId,
                        TaskType = "position",
                        MapId = TryInt(cols[0]),
                        X = TryInt(cols[1]),
                        Y = TryInt(cols[2]),
                        NpcRes = TryInt(cols[3]),
                        TargetName = TryStr(cols, 4),
                        NpcScript = TryStr(cols, 5)
                    });
                }
            }
        }
    }
}
