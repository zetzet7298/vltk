// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/missions/arena/arena.txt Võ Đài parser
// Nhiệm vụ võ đài PvP: map + min/max level + min/max rating + phần thưởng.
// Source: settings/missions/arena/arena.txt (GB2312, 9 tab cols).
//   ArenaId  MapId  MinLevel  MaxLevel  MinRating  MaxRating
//   RewardId  RewardCount  ResetHour
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcArenaParser
    {
        public const int ArenaIdCol = 0;
        public const int MapIdCol = 1;
        public const int MinLevelCol = 2;
        public const int MaxLevelCol = 3;
        public const int MinRatingCol = 4;
        public const int MaxRatingCol = 5;
        public const int RewardIdCol = 6;
        public const int RewardCountCol = 7;
        public const int ResetHourCol = 8;

        public static List<PcArenaEntry> ParseFile(string path)
        {
            var rows = new List<PcArenaEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                rows.Add(new PcArenaEntry
                {
                    arenaId = PcItemCommon.Int(cols, ArenaIdCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    minRating = PcItemCommon.Int(cols, MinRatingCol),
                    maxRating = PcItemCommon.Int(cols, MaxRatingCol),
                    rewardId = cols.Length > RewardIdCol ? PcItemCommon.Int(cols, RewardIdCol) : 0,
                    rewardCount = cols.Length > RewardCountCol ? PcItemCommon.Int(cols, RewardCountCol) : 0,
                    resetHour = cols.Length > ResetHourCol ? PcItemCommon.Int(cols, ResetHourCol) : 0,
                });
            }
            return rows;
        }

        public static PcArenaRegistry BuildRegistry(string dir)
        {
            var reg = new PcArenaRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "arena.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcArenaEntry
    {
        public int arenaId;
        public int mapId;
        public int minLevel;
        public int maxLevel;
        public int minRating;
        public int maxRating;
        public int rewardId;
        public int rewardCount;
        public int resetHour;
    }

    public sealed class PcArenaRegistry
    {
        private readonly Dictionary<int, PcArenaEntry> _byId = new();
        private readonly Dictionary<int, List<PcArenaEntry>> _byMap = new();
        private readonly List<PcArenaEntry> _all = new();
        public int Count => _byId.Count;
        public IEnumerable<PcArenaEntry> All => _all;

        public void Register(PcArenaEntry e)
        {
            if (e == null || e.arenaId <= 0) return;
            _byId[e.arenaId] = e;
            _all.Add(e);
            if (!_byMap.TryGetValue(e.mapId, out var list))
            {
                list = new List<PcArenaEntry>();
                _byMap[e.mapId] = list;
            }
            list.Add(e);
        }

        public PcArenaEntry Get(int arenaId)
            => _byId.TryGetValue(arenaId, out var v) ? v : null;

        public IReadOnlyList<PcArenaEntry> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v) ? v : (IReadOnlyList<PcArenaEntry>)System.Array.Empty<PcArenaEntry>();

        public IReadOnlyList<PcArenaEntry> GetForLevel(int playerLevel)
        {
            var result = new List<PcArenaEntry>();
            foreach (var e in _all)
            {
                if (e == null) continue;
                if (e.minLevel > 0 && playerLevel < e.minLevel) continue;
                if (e.maxLevel > 0 && playerLevel > e.maxLevel) continue;
                result.Add(e);
            }
            return result;
        }
    }
}
