// -----------------------------------------------------------------------------
// VLTK Mobile — PC chengshidahongbao.txt parser (hồng bao thành thị đại)
// Source: settings/item/chengshidahongbao.txt (GB2312)
// Cols: Name, Type, Genre, Detail, Particular, Serise, Level, Param1-6, Proba, Costly, Msg, Log
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcCityHongbaoEntry
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public int Genre { get; set; }
        public int Detail { get; set; }
        public int Level { get; set; }
        public int Proba { get; set; }
        public int Costly { get; set; }
        public string Msg { get; set; } = string.Empty;
    }

    public sealed class PcCityHongbaoRegistry
    {
        private readonly Dictionary<int, PcCityHongbaoEntry> _byId = new Dictionary<int, PcCityHongbaoEntry>();
        public int Count => _byId.Count;
        public PcCityHongbaoEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcCityHongbaoEntry> All => _byId.Values;
        public void Add(PcCityHongbaoEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcCityHongbaoParser
    {
        public static PcCityHongbaoRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcCityHongbaoRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "chengshidahongbao.txt");
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
                    if (line.StartsWith("Name", StringComparison.OrdinalIgnoreCase)) continue;
                }
                var cols = line.Split('\t');
                if (cols.Length < 15) continue;
                seqId++;
                int.TryParse(cols[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int type);
                int.TryParse(cols[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int genre);
                int.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int detail);
                int.TryParse(cols[6].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level);
                int.TryParse(cols[13].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int proba);
                int.TryParse(cols[14].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int costly);
                reg.Add(new PcCityHongbaoEntry
                {
                    Id = seqId,
                    Name = cols[0].Trim(),
                    Type = type,
                    Genre = genre,
                    Detail = detail,
                    Level = level,
                    Proba = proba,
                    Costly = costly,
                    Msg = cols.Length > 15 ? cols[15].Trim() : string.Empty
                });
            }
            return reg;
        }
    }
}
