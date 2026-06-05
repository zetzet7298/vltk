// -----------------------------------------------------------------------------
// VLTK Mobile — PC Guild Workshop Level parser (Công trình bang theo cấp)
// Source: settings/tong/workshop/workshops.txt + *_level_data.txt
// workshops.txt: TYPE, NAME, DESC, COEFFICIENT, OPEN_ICON, CLOSE_ICON, UNFOUNDED_ICON, SCRIPT
// Level files: LEVEL, OUTPUT_COEF, SCALE, STONE_VALUE, LINGPAI_PRICE
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcGuildWorkshopLevelData
    {
        public int Level { get; set; }
        public int OutputCoef { get; set; }
        public int Scale { get; set; }
        public int StoneValue { get; set; }
        public int LingpaiPrice { get; set; }
    }

    public class PcGuildWorkshopLevelEntry
    {
        public int WorkshopType { get; set; }
        public string Name { get; set; }
        public string Desc { get; set; }
        public float Coefficient { get; set; }
        public string OpenIcon { get; set; }
        public string CloseIcon { get; set; }
        public string UnfoundedIcon { get; set; }
        public string Script { get; set; }
        public List<PcGuildWorkshopLevelData> Levels { get; set; } = new List<PcGuildWorkshopLevelData>();
    }

    public sealed class PcGuildWorkshopLevelRegistry
    {
        private readonly Dictionary<int, PcGuildWorkshopLevelEntry> _byType = new Dictionary<int, PcGuildWorkshopLevelEntry>();
        public int Count => _byType.Count;
        public PcGuildWorkshopLevelEntry Get(int type) => _byType.TryGetValue(type, out var v) ? v : null;
        public IEnumerable<PcGuildWorkshopLevelEntry> All => _byType.Values;
        public void Add(PcGuildWorkshopLevelEntry e) { if (e != null) _byType[e.WorkshopType] = e; }
    }

    public static class PcGuildWorkshopLevelParser
    {
        /// <summary>Map workshop type → level data filename (without .txt extension).</summary>
        private static readonly Dictionary<int, string> TypeToFile = new Dictionary<int, string>
        {
            { 1, "bingjia" },
            { 2, "tiangong" },
            { 3, "mianju" },
            { 4, "shilian" },
            { 5, "tianyi" },
            { 6, "liwu" },
            { 7, "huodong" },
        };

        public static PcGuildWorkshopLevelRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcGuildWorkshopLevelRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;

            // Parse workshops.txt for metadata
            var metaPath = Path.Combine(absoluteDir, "workshops.txt");
            if (!File.Exists(metaPath)) return reg;

            var lines = PcMapListParser.ReadLines(metaPath);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)) continue;

                var e = new PcGuildWorkshopLevelEntry
                {
                    WorkshopType = type,
                    Name = cols.Length > 1 ? cols[1].Trim() : string.Empty,
                    Desc = cols.Length > 2 ? cols[2].Trim() : string.Empty,
                    Coefficient = cols.Length > 3 && float.TryParse(cols[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float c) ? c : 1f,
                    OpenIcon = cols.Length > 4 ? cols[4].Trim() : string.Empty,
                    CloseIcon = cols.Length > 5 ? cols[5].Trim() : string.Empty,
                    UnfoundedIcon = cols.Length > 6 ? cols[6].Trim() : string.Empty,
                    Script = cols.Length > 7 ? cols[7].Trim() : string.Empty,
                };
                reg.Add(e);
            }

            // Parse level data files
            foreach (var kvp in TypeToFile)
            {
                var entry = reg.Get(kvp.Key);
                if (entry == null) continue;
                var levelPath = Path.Combine(absoluteDir, $"{kvp.Value}_level_data.txt");
                if (!File.Exists(levelPath)) continue;

                var levelLines = PcMapListParser.ReadLines(levelPath);
                foreach (var raw in levelLines)
                {
                    if (string.IsNullOrWhiteSpace(raw)) continue;
                    var line = raw.Trim();
                    if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                    var cols = line.Split('\t');
                    if (cols.Length < 2) continue;
                    if (!int.TryParse(cols[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int lvl)) continue;

                    entry.Levels.Add(new PcGuildWorkshopLevelData
                    {
                        Level = lvl,
                        OutputCoef = cols.Length > 1 && int.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int oc) ? oc : 0,
                        Scale = cols.Length > 2 && int.TryParse(cols[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sc) ? sc : 0,
                        StoneValue = cols.Length > 3 && int.TryParse(cols[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sv) ? sv : 0,
                        LingpaiPrice = cols.Length > 4 && int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int lp) ? lp : 0,
                    });
                }
            }

            return reg;
        }
    }
}
