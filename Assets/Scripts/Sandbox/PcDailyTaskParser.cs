// -----------------------------------------------------------------------------
// VLTK Mobile — PC task/dailytask/dailytask.txt daily task parser
// Source: server settings/task/dailytask/dailytask.txt (nhiệm vụ hàng ngày).
//   TaskId  TaskType  TargetId  TargetCount  MinLevel  MaxLevel  RewardExp
//   RewardSilver  RewardItem
// TaskType: 0=kill, 1=collect, 2=talk, 3=visit.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcDailyTaskParser
    {
        public const int TaskIdCol = 0;
        public const int TaskTypeCol = 1;
        public const int TargetIdCol = 2;
        public const int TargetCountCol = 3;
        public const int MinLevelCol = 4;
        public const int MaxLevelCol = 5;
        public const int RewardExpCol = 6;
        public const int RewardSilverCol = 7;
        public const int RewardItemCol = 8;

        public static List<PcDailyTaskEntry> ParseFile(string path)
        {
            var rows = new List<PcDailyTaskEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int taskId = PcItemCommon.Int(cols, TaskIdCol);
                if (taskId <= 0) continue;
                rows.Add(new PcDailyTaskEntry
                {
                    taskId = taskId,
                    taskType = PcItemCommon.Int(cols, TaskTypeCol),
                    targetId = PcItemCommon.Int(cols, TargetIdCol),
                    targetCount = PcItemCommon.Int(cols, TargetCountCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    rewardExp = PcItemCommon.Int(cols, RewardExpCol),
                    rewardSilver = PcItemCommon.Int(cols, RewardSilverCol),
                    rewardItem = PcItemCommon.Int(cols, RewardItemCol),
                });
            }
            return rows;
        }

        public static PcDailyTaskRegistry BuildRegistry(string dir)
        {
            var reg = new PcDailyTaskRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "dailytask.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcDailyTaskEntry
    {
        public int taskId;
        public int taskType;        // 0=kill, 1=collect, 2=talk, 3=visit
        public int targetId;
        public int targetCount;
        public int minLevel;
        public int maxLevel;
        public int rewardExp;
        public int rewardSilver;
        public int rewardItem;
    }

    public sealed class PcDailyTaskRegistry
    {
        private readonly Dictionary<int, PcDailyTaskEntry> _byId = new();
        private readonly Dictionary<int, List<PcDailyTaskEntry>> _byType = new();
        public int Count => _byId.Count;
        public IEnumerable<PcDailyTaskEntry> All => _byId.Values;

        public void Register(PcDailyTaskEntry e)
        {
            if (e == null || e.taskId <= 0) return;
            _byId[e.taskId] = e;
            if (!_byType.TryGetValue(e.taskType, out var list))
            {
                list = new List<PcDailyTaskEntry>();
                _byType[e.taskType] = list;
            }
            list.Add(e);
        }

        public PcDailyTaskEntry Get(int taskId)
            => _byId.TryGetValue(taskId, out var v) ? v : null;

        public IReadOnlyList<PcDailyTaskEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v)
                ? (IReadOnlyList<PcDailyTaskEntry>)v
                : System.Array.Empty<PcDailyTaskEntry>();

        public IReadOnlyList<PcDailyTaskEntry> GetByLevel(int playerLevel)
        {
            var result = new List<PcDailyTaskEntry>();
            foreach (var e in _byId.Values)
            {
                if (playerLevel >= e.minLevel && playerLevel <= e.maxLevel) result.Add(e);
            }
            return result;
        }
    }
}
