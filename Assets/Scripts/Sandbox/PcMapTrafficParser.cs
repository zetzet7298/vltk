// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings maptraffic.ini parser
// Source: maptraffic.ini (lưu lượng map).
// Columns: MapId  MaxPlayers  RecommendedLevel  MinLevel  MaxLevel  PkMode
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapTrafficParser
    {
        public const int MapIdCol = 0;
        public const int MaxPlayersCol = 1;
        public const int RecommendedLevelCol = 2;
        public const int MinLevelCol = 3;
        public const int MaxLevelCol = 4;
        public const int PkModeCol = 5;

        public static List<PcMapTrafficEntry> ParseFile(string path)
        {
            var rows = new List<PcMapTrafficEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, MapIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapTrafficEntry
                {
                    mapId = id,
                    maxPlayers = PcItemCommon.Int(cols, MaxPlayersCol),
                    recommendedLevel = PcItemCommon.Int(cols, RecommendedLevelCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    pkMode = PcItemCommon.Int(cols, PkModeCol),
                });
            }
            return rows;
        }

        public static PcMapTrafficRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapTrafficRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcMapTrafficEntry
    {
        public int mapId;
        public int maxPlayers;
        public int recommendedLevel;
        public int minLevel;
        public int maxLevel;
        public int pkMode;
    }

    public sealed class PcMapTrafficRegistry
    {
        private readonly Dictionary<int, PcMapTrafficEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMapTrafficEntry e) { if (e == null || e.mapId <= 0) return; _byId[e.mapId] = e; }
        public PcMapTrafficEntry Get(int mapId) => _byId.TryGetValue(mapId, out var v) ? v : null;
        public IReadOnlyList<PcMapTrafficEntry> All => new List<PcMapTrafficEntry>(_byId.Values);
    }
}
