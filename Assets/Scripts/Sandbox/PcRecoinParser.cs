// -----------------------------------------------------------------------------
// VLTK Mobile — PC recoin_goldenequip.txt parser (tái đúc trang bị vàng)
// Source: settings/item/recoin_goldenequip.txt (GB2312)
// Cols: DES_GOLDNAME, DES_GENRE, DES_DETAILTYPE, MAR_FIRST_DETAIL, MAR_COUNT, DES_BADVALUES, DES_STANDARDVALUES
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcRecoinEntry
    {
        public int Id { get; set; }
        public string GoldName { get; set; } = string.Empty;
        public int Genre { get; set; }
        public int DetailType { get; set; }
        public int MarFirstDetail { get; set; }
        public int MarCount { get; set; }
        public string BadValues { get; set; } = string.Empty;
        public string StandardValues { get; set; } = string.Empty;
    }

    public sealed class PcRecoinRegistry
    {
        private readonly Dictionary<int, PcRecoinEntry> _byId = new Dictionary<int, PcRecoinEntry>();
        public int Count => _byId.Count;
        public PcRecoinEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcRecoinEntry> All => _byId.Values;
        public void Add(PcRecoinEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcRecoinParser
    {
        public static PcRecoinRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcRecoinRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "recoin_goldenequip.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            bool headerSkipped = false;
            int seqId = 0;
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (!headerSkipped)
                {
                    headerSkipped = true;
                    if (line.StartsWith("DES_GOLDNAME", StringComparison.OrdinalIgnoreCase)) continue;
                }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                seqId++;
                int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int genre);
                int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int detail);
                int.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int firstDetail);
                int.TryParse(cols[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int count);
                reg.Add(new PcRecoinEntry
                {
                    Id = seqId,
                    GoldName = cols[0].Trim(),
                    Genre = genre,
                    DetailType = detail,
                    MarFirstDetail = firstDetail,
                    MarCount = count,
                    BadValues = cols.Length > 5 ? cols[5].Trim() : string.Empty,
                    StandardValues = cols.Length > 6 ? cols[6].Trim() : string.Empty
                });
            }
            return reg;
        }
    }
}
