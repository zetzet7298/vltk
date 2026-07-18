// -----------------------------------------------------------------------------
// VLTK Mobile — PC treasurehunt.txt parser
// Source: settings/activity/treasurehunt.txt (Săn Kho Báu).
// Columns: TreasureId MapId PosX PosY ItemId ItemCount RequiredLevel
//          RespawnSec DetectionRange
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTreasureHuntParser
    {
        public const int TreasureIdCol = 0;
        public const int MapIdCol = 1;
        public const int PosXCol = 2;
        public const int PosYCol = 3;
        public const int ItemIdCol = 4;
        public const int ItemCountCol = 5;
        public const int RequiredLevelCol = 6;
        public const int RespawnSecCol = 7;
        public const int DetectionRangeCol = 8;

        public static List<PcTreasureHuntEntry> ParseFile(string path)
        {
            var rows = new List<PcTreasureHuntEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TreasureIdCol);
                if (id <= 0) continue;
                rows.Add(new PcTreasureHuntEntry
                {
                    treasureId = id,
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    itemId = PcItemCommon.Int(cols, ItemIdCol),
                    itemCount = PcItemCommon.Int(cols, ItemCountCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    respawnSec = PcItemCommon.Int(cols, RespawnSecCol),
                    detectionRange = PcItemCommon.Int(cols, DetectionRangeCol),
                });
            }
            return rows;
        }

        public static PcTreasureHuntRegistry BuildRegistry(string dir)
        {
            var reg = new PcTreasureHuntRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("treasure"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcTreasureHuntEntry
    {
        public int treasureId;
        public int mapId;
        public int posX;
        public int posY;
        public int itemId;
        public int itemCount;
        public int requiredLevel;
        public int respawnSec;
        public int detectionRange;
    }

    public sealed class PcTreasureHuntRegistry
    {
        private readonly Dictionary<int, PcTreasureHuntEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcTreasureHuntEntry e) { if (e == null || e.treasureId <= 0) return; _byId[e.treasureId] = e; }
        public PcTreasureHuntEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcTreasureHuntEntry> GetByMap(int mapId)
        {
            var list = new List<PcTreasureHuntEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcTreasureHuntEntry> All => new List<PcTreasureHuntEntry>(_byId.Values);
    }
}
