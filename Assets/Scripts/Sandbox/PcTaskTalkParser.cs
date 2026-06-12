// -----------------------------------------------------------------------------
// VLTK Mobile — PC task/talk_*.txt parser (nhiệm vụ đối thoại liên kết)
// Source: server settings/task/talk/talk_{buygoods,findgoods,findmaps,showgoods,upground,worldmap}.txt
// Columns: TextID  When  Where  Who  Why1  Why2  What
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTaskTalkEntry
    {
        public int TextId { get; set; }
        public string When { get; set; } = string.Empty;
        public string Where { get; set; } = string.Empty;
        public string Who { get; set; } = string.Empty;
        public string Why1 { get; set; } = string.Empty;
        public string Why2 { get; set; } = string.Empty;
        public string What { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;    // buygoods/findgoods/findmaps/showgoods/upground/worldmap
    }

    public sealed class PcTaskTalkRegistry
    {
        private readonly List<PcTaskTalkEntry> _entries = new List<PcTaskTalkEntry>();
        private readonly Dictionary<int, PcTaskTalkEntry> _byId = new Dictionary<int, PcTaskTalkEntry>();
        private readonly Dictionary<string, List<PcTaskTalkEntry>> _bySource = new Dictionary<string, List<PcTaskTalkEntry>>();

        public int Count => _entries.Count;
        public IEnumerable<PcTaskTalkEntry> All => _entries;

        public void Add(PcTaskTalkEntry e)
        {
            if (e == null || e.TextId <= 0) return;
            _entries.Add(e);
            _byId[e.TextId] = e;
            string key = e.Source ?? string.Empty;
            if (!_bySource.TryGetValue(key, out var list))
            {
                list = new List<PcTaskTalkEntry>();
                _bySource[key] = list;
            }
            list.Add(e);
        }

        public PcTaskTalkEntry Get(int textId) => _byId.TryGetValue(textId, out var v) ? v : null;
        public IReadOnlyList<PcTaskTalkEntry> GetBySource(string source)
            => _bySource.TryGetValue(source ?? string.Empty, out var v) ? v : Array.Empty<PcTaskTalkEntry>();
    }

    public static class PcTaskTalkParser
    {
        private static string TryStr(string[] cols, int idx)
            => (idx < cols.Length) ? (cols[idx] ?? string.Empty).Trim() : string.Empty;

        public static PcTaskTalkRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTaskTalkRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            string[] fileNames = { "talk_buygoods.txt", "talk_findgoods.txt", "talk_findmaps.txt",
                                   "talk_showgoods.txt", "talk_upground.txt", "talk_worldmap.txt" };
            foreach (var fn in fileNames)
            {
                string source = fn.Replace("talk_", "").Replace(".txt", "");
                ParseFile(reg, Path.Combine(absoluteDir, fn), source);
            }

            if (reg.Count == 0)
                SubsystemLog.Warn("TaskTalk", $"PcTaskTalk registry rỗng ({absoluteDir})");
            return reg;
        }

        private static void ParseFile(PcTaskTalkRegistry reg, string path, string source)
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
                if (cols[0].IndexOf("Text", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0) continue;
                reg.Add(new PcTaskTalkEntry
                {
                    TextId = id,
                    When = TryStr(cols, 1),
                    Where = TryStr(cols, 2),
                    Who = TryStr(cols, 3),
                    Why1 = TryStr(cols, 4),
                    Why2 = TryStr(cols, 5),
                    What = TryStr(cols, 6),
                    Source = source
                });
            }
        }
    }
}
