// -----------------------------------------------------------------------------
// VLTK Mobile — PC task/task_event.txt + task_type.txt + task_id.txt parser
// Source: server settings/task/{task_event,task_type,task_id}.txt
// task_event.txt: EventID  EventName  EventText
// task_type.txt : TaskType  ConditionFile  EntityFile  AwardFile  TalkFile
// task_id.txt   : TaskID  TaskName  EventID  TaskType  CanCancel  TaskText
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTaskEventEntry
    {
        public int EventId { get; set; }
        public string EventName { get; set; } = string.Empty;
        public string EventText { get; set; } = string.Empty;
    }

    public class PcTaskTypeEntry
    {
        public string TaskType { get; set; } = string.Empty;
        public string ConditionFile { get; set; } = string.Empty;
        public string EntityFile { get; set; } = string.Empty;
        public string AwardFile { get; set; } = string.Empty;
        public string TalkFile { get; set; } = string.Empty;
    }

    public class PcTaskIdEntry
    {
        public int TaskId { get; set; }
        public string TaskName { get; set; } = string.Empty;
        public int EventId { get; set; }
        public string TaskType { get; set; } = string.Empty;
        public int CanCancel { get; set; }
        public string TaskText { get; set; } = string.Empty;
    }

    public sealed class PcTaskEventRegistry
    {
        // host field is in PcTaskEventParser (the static methods class) — see below.

        private readonly Dictionary<int, PcTaskEventEntry> _events = new Dictionary<int, PcTaskEventEntry>();
        private readonly Dictionary<string, PcTaskTypeEntry> _types = new Dictionary<string, PcTaskTypeEntry>();
        private readonly Dictionary<int, PcTaskIdEntry> _ids = new Dictionary<int, PcTaskIdEntry>();

        public int Count => _events.Count + _types.Count + _ids.Count;
        public int EventCount => _events.Count;
        public int TypeCount => _types.Count;
        public int IdCount => _ids.Count;

        public void AddEvent(PcTaskEventEntry e)
        {
            if (e == null || e.EventId <= 0) return;
            _events[e.EventId] = e;
        }
        public void AddType(PcTaskTypeEntry e)
        {
            if (e == null || string.IsNullOrWhiteSpace(e.TaskType)) return;
            _types[e.TaskType] = e;
        }
        public void AddId(PcTaskIdEntry e)
        {
            if (e == null || e.TaskId <= 0) return;
            _ids[e.TaskId] = e;
        }

        public PcTaskEventEntry GetEvent(int id) => _events.TryGetValue(id, out var v) ? v : null;
        public PcTaskTypeEntry GetType(string t) => _types.TryGetValue(t ?? string.Empty, out var v) ? v : null;
        public PcTaskIdEntry GetId(int id) => _ids.TryGetValue(id, out var v) ? v : null;

        public IEnumerable<PcTaskEventEntry> AllEvents => _events.Values;
        public IEnumerable<PcTaskTypeEntry> AllTypes => _types.Values;
        public IEnumerable<PcTaskIdEntry> AllIds => _ids.Values;
    }

    public static class PcTaskEventParser
    {
        private static IPcTaskEventHost _host;

        /// <summary>Set/clear host để dispatch side-effects (UI, SFX, log, save).</summary>
        public static void AttachHost(IPcTaskEventHost host) { _host = host; }

        private static string TryStr(string[] cols, int idx)
            => (idx < cols.Length) ? (cols[idx] ?? string.Empty).Trim() : string.Empty;

        public static PcTaskEventRegistry BuildRegistry(string absoluteDir)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var reg = new PcTaskEventRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir))
            {
                _host?.OnParseFailed(absoluteDir ?? "<null>", string.IsNullOrEmpty(absoluteDir) ? "empty dir" : "dir not found");
                _host?.OnRegistryBuilt(0, 0, 0, sw.ElapsedMilliseconds);
                return reg;
            }

            ParseEvents(reg, Path.Combine(absoluteDir, "task_event.txt"));
            ParseTypes(reg, Path.Combine(absoluteDir, "task_type.txt"));
            ParseIds(reg, Path.Combine(absoluteDir, "task_id.txt"));

            sw.Stop();
            if (reg.Count == 0)
                SubsystemLog.Warn("TaskEvent", $"PcTaskEvent registry rỗng ({absoluteDir})");
            _host?.OnRegistryBuilt(reg.EventCount, reg.TypeCount, reg.IdCount, sw.ElapsedMilliseconds);
            _host?.ShowTaskLogUI(reg.Count);
            _host?.SaveTaskLog(reg.EventCount, reg.TypeCount, reg.IdCount);
            return reg;
        }

        private static void ParseEvents(PcTaskEventRegistry reg, string path)
        {
            if (!File.Exists(path)) {
                _host?.OnParseFailed(System.IO.Path.GetFileName(path), "file not found");
                return;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int before = reg.EventCount;
            _host?.OnParseStart(System.IO.Path.GetFileName(path));
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                if (cols[0].IndexOf("Event", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || cols[0].IndexOf("ID", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0) continue;
                reg.AddEvent(new PcTaskEventEntry
                {
                    EventId = id,
                    EventName = TryStr(cols, 1),
                    EventText = TryStr(cols, 2)
                });
            }
            sw.Stop();
            _host?.OnParseComplete(System.IO.Path.GetFileName(path), reg.EventCount - before, sw.ElapsedMilliseconds);
        }

        private static void ParseTypes(PcTaskEventRegistry reg, string path)
        {
            if (!File.Exists(path)) {
                _host?.OnParseFailed(System.IO.Path.GetFileName(path), "file not found");
                return;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int before = reg.TypeCount;
            _host?.OnParseStart(System.IO.Path.GetFileName(path));
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                if (cols[0].IndexOf("Task", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                reg.AddType(new PcTaskTypeEntry
                {
                    TaskType = TryStr(cols, 0),
                    ConditionFile = TryStr(cols, 1),
                    EntityFile = TryStr(cols, 2),
                    AwardFile = TryStr(cols, 3),
                    TalkFile = TryStr(cols, 4)
                });
            }
            sw.Stop();
            _host?.OnParseComplete(System.IO.Path.GetFileName(path), reg.TypeCount - before, sw.ElapsedMilliseconds);
        }

        private static void ParseIds(PcTaskEventRegistry reg, string path)
        {
            if (!File.Exists(path)) {
                _host?.OnParseFailed(System.IO.Path.GetFileName(path), "file not found");
                return;
            }
            var sw = System.Diagnostics.Stopwatch.StartNew();
            int before = reg.IdCount;
            _host?.OnParseStart(System.IO.Path.GetFileName(path));
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (cols[0].IndexOf("Task", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || cols[0].IndexOf("ID", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0) continue;
                reg.AddId(new PcTaskIdEntry
                {
                    TaskId = id,
                    TaskName = TryStr(cols, 1),
                    EventId = cols.Length > 2
                        ? (int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int eid) ? eid : 0)
                        : 0,
                    TaskType = TryStr(cols, 3),
                    CanCancel = cols.Length > 4
                        ? (int.TryParse(cols[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int cc) ? cc : 0)
                        : 0,
                    TaskText = TryStr(cols, 5)
                });
            }
            sw.Stop();
            _host?.OnParseComplete(System.IO.Path.GetFileName(path), reg.IdCount - before, sw.ElapsedMilliseconds);
        }
    }
}
