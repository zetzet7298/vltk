// -----------------------------------------------------------------------------
// VLTK Mobile — PC ranksetting.txt parser (cài đặt xếp hạng)
// Source: settings/ranksetting.txt (GB2312). Tab-separated: RANKID, RANKSTR
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcRankSettingEntry
    {
        public int RankId { get; set; }
        public string RankName { get; set; } = string.Empty;
    }

    public sealed class PcRankSettingRegistry
    {
        private readonly Dictionary<int, PcRankSettingEntry> _byId = new Dictionary<int, PcRankSettingEntry>();
        public int Count => _byId.Count;
        public PcRankSettingEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcRankSettingEntry> All => _byId.Values;
        public void Add(PcRankSettingEntry e) { if (e != null) _byId[e.RankId] = e; }
    }

    public static class PcRankSettingParser
    {
        public static PcRankSettingRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcRankSettingRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "ranksetting.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            bool headerSkipped = false;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    if (line.StartsWith("RANKID", StringComparison.OrdinalIgnoreCase)) continue;
                }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                reg.Add(new PcRankSettingEntry
                {
                    RankId = id,
                    RankName = cols[1].Trim()
                });
            }
            return reg;
        }
    }
}
