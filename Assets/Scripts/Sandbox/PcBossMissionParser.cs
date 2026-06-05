// -----------------------------------------------------------------------------
// VLTK Mobile — PC missions/boss/bossmission.txt boss mission parser
// Source: server settings/missions/boss/bossmission.txt (nhiệm vụ boss).
//   MissionId  MapId  BossNpcId  MinLevel  MaxLevel  MinPartySize
//   RewardId  RewardCount  ResetHour
// ResetHour: giờ trong ngày (0-23) reset lại nhiệm vụ.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBossMissionParser
    {
        public const int MissionIdCol = 0;
        public const int MapIdCol = 1;
        public const int BossNpcIdCol = 2;
        public const int MinLevelCol = 3;
        public const int MaxLevelCol = 4;
        public const int MinPartySizeCol = 5;
        public const int RewardIdCol = 6;
        public const int RewardCountCol = 7;
        public const int ResetHourCol = 8;

        public static List<PcBossMissionEntry> ParseFile(string path)
        {
            var rows = new List<PcBossMissionEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                int missionId = PcItemCommon.Int(cols, MissionIdCol);
                if (missionId <= 0) continue;
                rows.Add(new PcBossMissionEntry
                {
                    missionId = missionId,
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    bossNpcId = PcItemCommon.Int(cols, BossNpcIdCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    minPartySize = PcItemCommon.Int(cols, MinPartySizeCol),
                    rewardId = PcItemCommon.Int(cols, RewardIdCol),
                    rewardCount = PcItemCommon.Int(cols, RewardCountCol),
                    resetHour = PcItemCommon.Int(cols, ResetHourCol),
                });
            }
            return rows;
        }

        public static PcBossMissionRegistry BuildRegistry(string dir)
        {
            var reg = new PcBossMissionRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "bossmission.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcBossMissionEntry
    {
        public int missionId;
        public int mapId;
        public int bossNpcId;
        public int minLevel;
        public int maxLevel;
        public int minPartySize;
        public int rewardId;
        public int rewardCount;
        public int resetHour;
    }

    public sealed class PcBossMissionRegistry
    {
        private readonly Dictionary<int, PcBossMissionEntry> _byId = new();
        private readonly Dictionary<int, List<PcBossMissionEntry>> _byMap = new();
        private readonly Dictionary<int, List<PcBossMissionEntry>> _byBoss = new();
        public int Count => _byId.Count;
        public IEnumerable<PcBossMissionEntry> All => _byId.Values;

        public void Register(PcBossMissionEntry e)
        {
            if (e == null || e.missionId <= 0) return;
            _byId[e.missionId] = e;
            if (!_byMap.TryGetValue(e.mapId, out var mlist))
            {
                mlist = new List<PcBossMissionEntry>();
                _byMap[e.mapId] = mlist;
            }
            mlist.Add(e);
            if (e.bossNpcId > 0)
            {
                if (!_byBoss.TryGetValue(e.bossNpcId, out var blist))
                {
                    blist = new List<PcBossMissionEntry>();
                    _byBoss[e.bossNpcId] = blist;
                }
                blist.Add(e);
            }
        }

        public PcBossMissionEntry Get(int missionId)
            => _byId.TryGetValue(missionId, out var v) ? v : null;

        public IReadOnlyList<PcBossMissionEntry> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v)
                ? (IReadOnlyList<PcBossMissionEntry>)v
                : System.Array.Empty<PcBossMissionEntry>();

        public IReadOnlyList<PcBossMissionEntry> GetByBoss(int npcId)
            => _byBoss.TryGetValue(npcId, out var v)
                ? (IReadOnlyList<PcBossMissionEntry>)v
                : System.Array.Empty<PcBossMissionEntry>();
    }
}
