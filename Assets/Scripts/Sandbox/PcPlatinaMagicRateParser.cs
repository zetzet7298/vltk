// -----------------------------------------------------------------------------
// VLTK Mobile — PC platina_magicrate.txt parser (tỉ lệ thuộc tính bạch kim)
// Source: settings/item/platina_magicrate.txt (GB2312)
// Cols: PlatinaItem, Level, SkillNo, ActiveRate, Rate1, MagicIdx1, Rate2, MagicIdx2, Rate3, MagicIdx3, Comment
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcPlatinaMagicRateEntry
    {
        public int PlatinaItem { get; set; }
        public int Level { get; set; }
        public int SkillNo { get; set; }
        public int ActiveRate { get; set; }
        public int[] Rates { get; set; } = Array.Empty<int>();
        public int[] MagicIdxs { get; set; } = Array.Empty<int>();
        public string Comment { get; set; } = string.Empty;
    }

    public sealed class PcPlatinaMagicRateRegistry
    {
        private readonly List<PcPlatinaMagicRateEntry> _all = new List<PcPlatinaMagicRateEntry>();
        public int Count => _all.Count;
        public IEnumerable<PcPlatinaMagicRateEntry> All => _all;
        public void Add(PcPlatinaMagicRateEntry e) { if (e != null) _all.Add(e); }
        public IEnumerable<PcPlatinaMagicRateEntry> GetByItem(int platinaItem)
        {
            foreach (var e in _all) if (e.PlatinaItem == platinaItem) yield return e;
        }
    }

    public static class PcPlatinaMagicRateParser
    {
        public static PcPlatinaMagicRateRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcPlatinaMagicRateRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "platina_magicrate.txt");
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
                    if (line.StartsWith("PlatinaItem", StringComparison.OrdinalIgnoreCase)) continue;
                }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int item)) continue;
                int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int lvl);
                int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int skillNo);
                int.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int active);
                var rates = new List<int>();
                var idxs = new List<int>();
                for (int p = 0; p < 3; p++)
                {
                    int rateIdx = 4 + p * 2;
                    int idxIdx = 5 + p * 2;
                    if (rateIdx < cols.Length && int.TryParse(cols[rateIdx].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int r)) rates.Add(r);
                    if (idxIdx < cols.Length && int.TryParse(cols[idxIdx].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)) idxs.Add(i);
                }
                reg.Add(new PcPlatinaMagicRateEntry
                {
                    PlatinaItem = item,
                    Level = lvl,
                    SkillNo = skillNo,
                    ActiveRate = active,
                    Rates = rates.ToArray(),
                    MagicIdxs = idxs.ToArray(),
                    Comment = cols.Length > 10 ? cols[10].Trim() : string.Empty
                });
            }
            return reg;
        }
    }
}
