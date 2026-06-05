// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/adventure.txt adventure reward parser
// Source: server settings/adventure.txt (1,037 entries, GB2312, tab-separated).
// Catalog of dã tẩu/truyền tống/phiêu lưu reward tables.
// Columns: MapId  PosX  PosY (+ optional extras)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcAdventureParser
    {
        public const int MapIdCol = 0;
        public const int PosXCol = 1;
        public const int PosYCol = 2;

        public static List<PcAdventureEntry> ParseFile(string path)
        {
            var rows = new List<PcAdventureEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                autoId++;
                rows.Add(new PcAdventureEntry
                {
                    id = autoId,
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    nameRaw = PcItemCommon.Str(cols, PosXCol) + "," + PcItemCommon.Str(cols, PosYCol),
                    description = string.Empty,
                    extra0 = cols.Length > 3 ? cols[3] : string.Empty,
                    extra1 = cols.Length > 4 ? cols[4] : string.Empty,
                });
            }
            return rows;
        }

        public static PcAdventureRegistry BuildRegistry(string dir)
        {
            var reg = new PcAdventureRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcAdventureEntry
    {
        public int id;
        public int mapId;
        public string nameRaw;
        public string description;
        public string extra0;
        public string extra1;
    }

    public sealed class PcAdventureRegistry
    {
        private readonly Dictionary<int, PcAdventureEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcAdventureEntry e) { if (e == null || e.id <= 0) return; _byId[e.id] = e; }
        public PcAdventureEntry Resolve(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcAdventureEntry> All => _byId.Values;
    }
}
