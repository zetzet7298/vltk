// -----------------------------------------------------------------------------
// VLTK Mobile — PC progressconfig.txt parser (cấu hình tiến trình NPC)
// Source: settings/progressconfig.txt (GB2312). Tab-separated: Id, Title, Time, 15+ event flags, comment
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcProgressConfigEntry
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public float Time { get; set; }
        public string Comment { get; set; } = string.Empty;
        public int[] EventFlags { get; set; } = Array.Empty<int>();
    }

    public sealed class PcProgressConfigRegistry
    {
        private readonly Dictionary<int, PcProgressConfigEntry> _byId = new Dictionary<int, PcProgressConfigEntry>();
        public int Count => _byId.Count;
        public PcProgressConfigEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcProgressConfigEntry> All => _byId.Values;
        public void Add(PcProgressConfigEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcProgressConfigParser
    {
        public static PcProgressConfigRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcProgressConfigRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "progressconfig.txt");
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
                    if (line.StartsWith("Id", StringComparison.OrdinalIgnoreCase)) continue;
                }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                float.TryParse(cols[2].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float time);
                var flags = new List<int>();
                for (int i = 3; i < Math.Min(cols.Length, 18); i++)
                {
                    if (int.TryParse(cols[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int f)) flags.Add(f);
                }
                reg.Add(new PcProgressConfigEntry
                {
                    Id = id,
                    Title = cols[1].Trim(),
                    Time = time,
                    EventFlags = flags.ToArray(),
                    Comment = cols.Length > 18 ? cols[18].Trim() : string.Empty
                });
            }
            return reg;
        }
    }
}
