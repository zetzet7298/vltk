// -----------------------------------------------------------------------------
// VLTK Mobile — PC huashan.txt (Hoa Sơn Luận Kiếm) PvP tournament parser
// Source: server settings/event/huashan.txt hoặc Reference/PcEvent root.
// Cols: RoundIdx, MapId, PosX, PosY, RequiredLevel, MaxParticipants,
//       RewardItemId, RewardCount, IsFinalRound
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcHuaShanLuanJianParser
    {
        public const int RoundIdxCol = 0;
        public const int MapIdCol = 1;
        public const int PosXCol = 2;
        public const int PosYCol = 3;
        public const int RequiredLevelCol = 4;
        public const int MaxParticipantsCol = 5;
        public const int RewardItemIdCol = 6;
        public const int RewardCountCol = 7;
        public const int IsFinalRoundCol = 8;

        public static List<PcHuaShanLuanJianEntry> ParseFile(string path)
        {
            var rows = new List<PcHuaShanLuanJianEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                autoId++;
                rows.Add(new PcHuaShanLuanJianEntry
                {
                    id = autoId,
                    roundIdx = PcItemCommon.Int(cols, RoundIdxCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    maxParticipants = PcItemCommon.Int(cols, MaxParticipantsCol),
                    rewardItemId = PcItemCommon.Int(cols, RewardItemIdCol),
                    rewardCount = PcItemCommon.Int(cols, RewardCountCol),
                    isFinalRound = PcItemCommon.Int(cols, IsFinalRoundCol) != 0,
                });
            }
            return rows;
        }

        public static PcHuaShanLuanJianRegistry BuildRegistry(string dir)
        {
            var reg = new PcHuaShanLuanJianRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcHuaShanLuanJianEntry
    {
        public int id;
        public int roundIdx;
        public int mapId;
        public int posX;
        public int posY;
        public int requiredLevel;
        public int maxParticipants;
        public int rewardItemId;
        public int rewardCount;
        public bool isFinalRound;
    }

    public sealed class PcHuaShanLuanJianRegistry
    {
        private readonly Dictionary<int, PcHuaShanLuanJianEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcHuaShanLuanJianEntry e) { if (e == null || e.id <= 0) return; _byId[e.id] = e; }
        public PcHuaShanLuanJianEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcHuaShanLuanJianEntry> GetByMap(int mapId)
        {
            var list = new List<PcHuaShanLuanJianEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcHuaShanLuanJianEntry> GetByRound(int roundIdx)
        {
            var list = new List<PcHuaShanLuanJianEntry>();
            foreach (var e in _byId.Values)
                if (e.roundIdx == roundIdx) list.Add(e);
            return list;
        }
        public PcHuaShanLuanJianEntry GetFinalRound()
        {
            foreach (var e in _byId.Values)
                if (e.isFinalRound) return e;
            return null;
        }
        public int GetTotalRounds()
        {
            int max = 0;
            foreach (var e in _byId.Values)
                if (e.roundIdx > max) max = e.roundIdx;
            return max;
        }
        public IReadOnlyList<PcHuaShanLuanJianEntry> All => new List<PcHuaShanLuanJianEntry>(_byId.Values);
    }
}
