// -----------------------------------------------------------------------------
// VLTK Mobile — PC task/levellink.txt parser (liên kết cấp độ nhiệm vụ)
// Source: server settings/task/levellink.txt
// Columns: TaskLevel  TaskStart
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTaskLevelLinkEntry
    {
        public int TaskLevel { get; set; }
        public int TaskStart { get; set; }
    }

    public sealed class PcTaskLevelLinkRegistry
    {
        private readonly List<PcTaskLevelLinkEntry> _entries = new List<PcTaskLevelLinkEntry>();
        private readonly Dictionary<int, PcTaskLevelLinkEntry> _byLevel = new Dictionary<int, PcTaskLevelLinkEntry>();

        public int Count => _entries.Count;
        public IEnumerable<PcTaskLevelLinkEntry> All => _entries;

        public void Add(PcTaskLevelLinkEntry e)
        {
            if (e == null || e.TaskLevel <= 0) return;
            _entries.Add(e);
            _byLevel[e.TaskLevel] = e;
        }

        public PcTaskLevelLinkEntry GetByLevel(int level)
            => _byLevel.TryGetValue(level, out var v) ? v : null;

        /// <summary>Trả về TaskStart cho cấp độ lớn nhất không vượt quá playerLevel.</summary>
        public int GetTaskStartForLevel(int playerLevel)
        {
            int best = 0;
            foreach (var e in _entries)
            {
                if (e.TaskLevel <= playerLevel && e.TaskStart > best) best = e.TaskStart;
            }
            return best;
        }
    }

    public static class PcTaskLevelLinkParser
    {
        public static PcTaskLevelLinkRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTaskLevelLinkRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            string path = Path.Combine(absoluteDir, "levellink.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                if (cols[0].IndexOf("Task", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lvl)) continue;
                int start = cols.Length > 1
                    ? (int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : 0)
                    : 0;
                reg.Add(new PcTaskLevelLinkEntry { TaskLevel = lvl, TaskStart = start });
            }
            if (reg.Count == 0)
                SubsystemLog.Warn("TaskLevelLink", "PcTaskLevelLink registry rỗng");
            return reg;
        }
    }
}
