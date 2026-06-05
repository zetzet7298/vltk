// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/task/random/randomtask.txt Random Task parser
// Nhiệm vụ ngẫu nhiên: kill, collect, talk theo cấp + cooldown.
// Source: settings/task/random/*.txt (GB2312, 10 tab cols).
//   TaskId  TaskType  TargetId  TargetCount  MinLevel  MaxLevel
//   RewardExp  RewardSilver  CooldownSec  [MapId]
// TaskType: 0=giết quái, 1=thu thập, 2=trò chuyện, 3=tiêu diệt boss.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcRandomTaskParser
    {
        public const int TaskIdCol = 0;
        public const int TaskTypeCol = 1;
        public const int TargetIdCol = 2;
        public const int TargetCountCol = 3;
        public const int MinLevelCol = 4;
        public const int MaxLevelCol = 5;
        public const int RewardExpCol = 6;
        public const int RewardSilverCol = 7;
        public const int CooldownSecCol = 8;
        public const int MapIdCol = 9;

        public static List<PcRandomTaskEntry> ParseFile(string path)
        {
            var rows = new List<PcRandomTaskEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                rows.Add(new PcRandomTaskEntry
                {
                    taskId = PcItemCommon.Int(cols, TaskIdCol),
                    taskType = PcItemCommon.Int(cols, TaskTypeCol),
                    targetId = PcItemCommon.Int(cols, TargetIdCol),
                    targetCount = PcItemCommon.Int(cols, TargetCountCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = cols.Length > MaxLevelCol ? PcItemCommon.Int(cols, MaxLevelCol) : 0,
                    rewardExp = cols.Length > RewardExpCol ? PcItemCommon.Int(cols, RewardExpCol) : 0,
                    rewardSilver = cols.Length > RewardSilverCol ? PcItemCommon.Int(cols, RewardSilverCol) : 0,
                    cooldownSec = cols.Length > CooldownSecCol ? PcItemCommon.Int(cols, CooldownSecCol) : 0,
                    mapId = cols.Length > MapIdCol ? PcItemCommon.Int(cols, MapIdCol) : 0,
                });
            }
            return rows;
        }

        public static PcRandomTaskRegistry BuildRegistry(string dir)
        {
            var reg = new PcRandomTaskRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcRandomTaskEntry
    {
        public int taskId;
        public int taskType;
        public int targetId;
        public int targetCount;
        public int minLevel;
        public int maxLevel;
        public int rewardExp;
        public int rewardSilver;
        public int cooldownSec;
        public int mapId;
    }

    public sealed class PcRandomTaskRegistry
    {
        private readonly Dictionary<int, PcRandomTaskEntry> _byId = new();
        private readonly List<PcRandomTaskEntry> _all = new();
        public int Count => _byId.Count;
        public IEnumerable<PcRandomTaskEntry> All => _all;

        public void Register(PcRandomTaskEntry e)
        {
            if (e == null || e.taskId <= 0) return;
            _byId[e.taskId] = e;
            _all.Add(e);
        }

        public PcRandomTaskEntry Get(int taskId)
            => _byId.TryGetValue(taskId, out var v) ? v : null;

        public IReadOnlyList<PcRandomTaskEntry> GetByLevel(int playerLevel)
        {
            var result = new List<PcRandomTaskEntry>();
            foreach (var e in _all)
            {
                if (e == null) continue;
                if (e.minLevel <= 0 && e.maxLevel <= 0) { result.Add(e); continue; }
                if (e.minLevel > 0 && playerLevel < e.minLevel) continue;
                if (e.maxLevel > 0 && playerLevel > e.maxLevel) continue;
                result.Add(e);
            }
            return result;
        }

        public IReadOnlyList<PcRandomTaskEntry> GetByType(int type)
        {
            var result = new List<PcRandomTaskEntry>();
            foreach (var e in _all)
            {
                if (e != null && e.taskType == type) result.Add(e);
            }
            return result;
        }
    }
}
