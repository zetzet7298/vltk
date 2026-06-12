// -----------------------------------------------------------------------------
// VLTK Mobile — PC task/random/* entity parser (nhiệm vụ ngẫu nhiên)
// Source: server settings/task/random/{kill,coll,talk,next}/entity.txt
// Columns: TaskName  TaskType  Genre  Detail  Particular  Level  GoodsFive
//   Quality  GoodsNum  DelGoods  RecordSeed  Money  KillNpcName  DropRate
//   TalkNpcName  TalkNpcMap  TaskText
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTaskRandomEntry
    {
        public string TaskName { get; set; } = string.Empty;
        public string TaskType { get; set; } = string.Empty;       // sát quái / thu thập / đối thoại / mật đà
        public string Genre { get; set; } = string.Empty;          // Tổng loại
        public string Detail { get; set; } = string.Empty;         // Phân loại
        public int Level { get; set; }
        public int GoodsFive { get; set; }
        public int Quality { get; set; }
        public int GoodsNum { get; set; }
        public int DelGoods { get; set; }
        public int RecordSeed { get; set; }
        public int Money { get; set; }
        public string KillNpcName { get; set; } = string.Empty;
        public int DropRate { get; set; }
        public string TalkNpcName { get; set; } = string.Empty;
        public string TalkNpcMap { get; set; } = string.Empty;
        public string TaskText { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;         // kill / coll / talk / next
    }

    public sealed class PcTaskRandomRegistry
    {
        private readonly List<PcTaskRandomEntry> _entries = new List<PcTaskRandomEntry>();
        private readonly Dictionary<string, List<PcTaskRandomEntry>> _bySource = new Dictionary<string, List<PcTaskRandomEntry>>();

        public int Count => _entries.Count;
        public IEnumerable<PcTaskRandomEntry> All => _entries;

        public void Add(PcTaskRandomEntry e)
        {
            if (e == null) return;
            _entries.Add(e);
            string key = e.Source ?? string.Empty;
            if (!_bySource.TryGetValue(key, out var list))
            {
                list = new List<PcTaskRandomEntry>();
                _bySource[key] = list;
            }
            list.Add(e);
        }

        public IReadOnlyList<PcTaskRandomEntry> GetBySource(string source)
            => _bySource.TryGetValue(source ?? string.Empty, out var v) ? v : Array.Empty<PcTaskRandomEntry>();
    }

    public static class PcTaskRandomParser
    {
        private static int TryInt(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return 0;
            return int.TryParse(s.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        // Bounds-safe column access: rows with fewer columns than the full schema
        // (common in coll/talk/next entity.txt) must not throw IndexOutOfRange.
        private static int TryInt(string[] cols, int idx)
            => (idx >= 0 && idx < cols.Length) ? TryInt(cols[idx]) : 0;

        private static string TryStr(string[] cols, int idx)
            => (idx < cols.Length) ? (cols[idx] ?? string.Empty).Trim() : string.Empty;

        public static PcTaskRandomRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTaskRandomRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            ParseEntityFile(reg, Path.Combine(absoluteDir, "kill", "entity.txt"), "kill");
            ParseEntityFile(reg, Path.Combine(absoluteDir, "coll", "entity.txt"), "coll");
            ParseEntityFile(reg, Path.Combine(absoluteDir, "talk", "entity.txt"), "talk");
            ParseEntityFile(reg, Path.Combine(absoluteDir, "next", "entity.txt"), "next");

            if (reg.Count == 0)
                SubsystemLog.Warn("TaskRandom", $"PcTaskRandom registry rỗng ({absoluteDir})");
            return reg;
        }

        private static void ParseEntityFile(PcTaskRandomRegistry reg, string path, string source)
        {
            if (!File.Exists(path)) return;
            var lines = PcText.ReadLinesTcvn3(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                string first = cols[0] ?? string.Empty;
                // skip header lines
                if (first.IndexOf("Task", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (first.IndexOf("Tên", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;

                reg.Add(new PcTaskRandomEntry
                {
                    TaskName = TryStr(cols, 0),
                    TaskType = TryStr(cols, 1),
                    Genre = TryStr(cols, 2),
                    Detail = TryStr(cols, 3),
                    Level = TryInt(cols, 5),
                    GoodsFive = TryInt(cols, 6),
                    Quality = TryInt(cols, 7),
                    GoodsNum = TryInt(cols, 8),
                    DelGoods = TryInt(cols, 9),
                    RecordSeed = source == "kill" ? TryInt(cols, 10) : 0,
                    Money = TryInt(cols, source == "kill" ? 11 : 10),
                    KillNpcName = TryStr(cols, source == "kill" ? 12 : 11),
                    DropRate = source == "kill" ? TryInt(cols, 13) : 0,
                    TalkNpcName = TryStr(cols, source == "kill" ? 14 : 12),
                    TalkNpcMap = TryStr(cols, source == "kill" ? 15 : 13),
                    TaskText = TryStr(cols, source == "kill" ? 16 : 14),
                    Source = source
                });
            }
        }
    }
}
