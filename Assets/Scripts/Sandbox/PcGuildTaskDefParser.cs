// -----------------------------------------------------------------------------
// VLTK Mobile — PC Guild Task Definition parser (Định nghĩa nhiệm vụ bang)
// Source: settings/tong/task/{tong,member,controlhelp,workshop}_task_def.txt
// Format: TASK_ID_FIRST, TASK_ID_LAST, TASK_NAME, SYNC_FLAG, TASK_DESCRIBE
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcGuildTaskDefEntry
    {
        public int TaskIdFirst { get; set; }
        public int TaskIdLast { get; set; }
        public string TaskName { get; set; }
        public int SyncFlag { get; set; }
        public string TaskDescribe { get; set; }
        public string Source { get; set; }
    }

    public sealed class PcGuildTaskDefRegistry
    {
        private readonly List<PcGuildTaskDefEntry> _entries = new List<PcGuildTaskDefEntry>();
        public int Count => _entries.Count;
        public IEnumerable<PcGuildTaskDefEntry> All => _entries;
        public void Add(PcGuildTaskDefEntry e) { if (e != null) _entries.Add(e); }

        public IEnumerable<PcGuildTaskDefEntry> FindById(int taskId)
        {
            foreach (var e in _entries)
            {
                if (taskId >= e.TaskIdFirst && taskId <= e.TaskIdLast) yield return e;
            }
        }
    }

    public static class PcGuildTaskDefParser
    {
        private static readonly string[] SourceFiles = new[]
        {
            "tong_task_def.txt",
            "member_task_def.txt",
            "controlhelp_task_def.txt",
            "workshop_task_def.txt",
        };

        public static PcGuildTaskDefRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcGuildTaskDefRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            foreach (var fileName in SourceFiles)
            {
                var path = Path.Combine(absoluteDir, fileName);
                if (!File.Exists(path)) continue;

                var lines = PcMapListParser.ReadLines(path);
                foreach (var raw in lines)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                    var cols = line.Split('\t');
                    if (cols.Length < 2) continue;
                    if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int first)) continue;

                    var e = new PcGuildTaskDefEntry
                    {
                        TaskIdFirst = first,
                        TaskIdLast = cols.Length > 1 && int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int last) ? last : first,
                        TaskName = cols.Length > 2 ? cols[2].Trim() : string.Empty,
                        SyncFlag = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sf) ? sf : 0,
                        TaskDescribe = cols.Length > 4 ? cols[4].Trim() : string.Empty,
                        Source = fileName,
                    };
                    reg.Add(e);
                }
            }
            return reg;
        }
    }
}
