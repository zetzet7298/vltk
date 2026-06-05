// -----------------------------------------------------------------------------
// VLTK Mobile — PC forbitheart.txt parser
// Source: settings/forbitheart.txt (GB2312). Mỗi dòng tab-separated: MapId,
// Description. Danh sách map cấm sử dụng "tâm pháp" (heart skill).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class PcForbitHeartEntry
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; }
    }

    public sealed class PcForbitHeartRegistry
    {
        private readonly Dictionary<int, PcForbitHeartEntry> _byId = new Dictionary<int, PcForbitHeartEntry>();
        public int Count => _byId.Count;
        public PcForbitHeartEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcForbitHeartEntry> All => _byId.Values;
        public void Add(PcForbitHeartEntry e) { if (e != null) _byId[e.ItemId] = e; }
    }

    public static class PcForbitHeartParser
    {
        public static PcForbitHeartRegistry BuildRegistry(string absoluteDir)
        {
            var reg = new PcForbitHeartRegistry();
            if (string.IsNullOrEmpty(absoluteDir) || !Directory.Exists(absoluteDir)) return reg;
            var path = Path.Combine(absoluteDir, "forbitheart.txt");
            if (!File.Exists(path)) return reg;
            var lines = PcMapListParser.ReadLines(path);
            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == ';' || line[0] == '#') continue;
                var cols = line.Split('\t');
                if (cols.Length < 1) continue;
                if (!int.TryParse(cols[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                var e = new PcForbitHeartEntry
                {
                    ItemId = id,
                    ItemName = cols.Length > 1 ? cols[1].Trim() : string.Empty
                };
                reg.Add(e);
            }
            return reg;
        }
    }
}
