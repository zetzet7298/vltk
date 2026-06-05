// -----------------------------------------------------------------------------
// VLTK Mobile — PC task tollgate killer parser (trạm kiểm tra - killer)
// Source: settings/task/tollgate/killer/killer.txt
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcTollgateKillerEntry
    {
        public int Id { get; set; }
        public string BossName { get; set; }
        public string BossInfo { get; set; }
        public int MapId { get; set; }
        public int PosX { get; set; }
        public int PosY { get; set; }
    }

    public sealed class PcTollgateKillerRegistry
    {
        private readonly Dictionary<int, PcTollgateKillerEntry> _byId = new Dictionary<int, PcTollgateKillerEntry>();
        public int Count => _byId.Count;
        public PcTollgateKillerEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcTollgateKillerEntry> All => _byId.Values;
        public void Add(PcTollgateKillerEntry e) { if (e != null) _byId[e.Id] = e; }
    }

    public static class PcTollgateKillerParser
    {
        public static PcTollgateKillerRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcTollgateKillerRegistry();
            if (string.IsNullOrEmpty(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "killer.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";") || line.StartsWith("#")) continue;
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                reg.Add(new PcTollgateKillerEntry
                {
                    Id = id,
                    BossName = cols.Length > 1 ? cols[1].Trim() : "",
                    BossInfo = cols.Length > 2 ? cols[2].Trim() : "",
                    MapId = cols.Length > 3 && int.TryParse(cols[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int m) ? m : 0,
                    PosX = cols.Length > 4 && int.TryParse(cols[4].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int x) ? x : 0,
                    PosY = cols.Length > 5 && int.TryParse(cols[5].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int y) ? y : 0
                });
            }
            return reg;
        }
    }
}
