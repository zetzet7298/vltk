// -----------------------------------------------------------------------------
// VLTK Mobile — PC tongstunt_setting.txt parser (võ công bang hội)
// Source: settings/tongstunt_setting.txt (GB2312). Cột phẳng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTongStuntEntry
    {
        public int StuntId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int RequiredLevel { get; set; }
        public int RequiredContribution { get; set; }
        public int EffectId { get; set; }
        public int CostSilver { get; set; }
    }

    public sealed class PcTongStuntRegistry
    {
        private readonly Dictionary<int, PcTongStuntEntry> _byId = new Dictionary<int, PcTongStuntEntry>();
        public int Count => _byId.Count;
        public PcTongStuntEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcTongStuntEntry> All => _byId.Values;
        public IEnumerable<PcTongStuntEntry> GetForLevel(int level)
        {
            foreach (var e in _byId.Values) if (e.RequiredLevel <= level) yield return e;
        }
        public void Add(PcTongStuntEntry e) { if (e != null) _byId[e.StuntId] = e; }
    }

    public static class PcTongStuntParser
    {
        public static PcTongStuntRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTongStuntRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "tongstunt_setting.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 4) cols = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (cols.Length < 4) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                var e = new PcTongStuntEntry
                {
                    StuntId = id,
                    Name = cols.Length > 1 ? cols[1] : string.Empty,
                    RequiredLevel = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int r) ? r : 0,
                    RequiredContribution = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int c) ? c : 0,
                    EffectId = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ef) ? ef : 0,
                    CostSilver = cols.Length > 5 && int.TryParse(cols[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int cs) ? cs : 0
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
