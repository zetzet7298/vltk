// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/tong/tong_task.txt Bang (GUILD) task parser
// Source: server settings/tong/tong_task.txt (GB2312, tab-separated).
//   TaskId  TaskType  TargetId  TargetCount  RequiredLevel  RewardContribution  RewardFunds
// Task types: 0=Kill Boss, 1=Gather, 2=Donate, 3=Defend City, 4=PvP Arena.
// Vietnamese: "Nhiệm Vụ Bang", "Tiêu Diệt Boss", "Thu Thập", "Cống Hiến".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGuildTaskParser
    {
        public const int TaskIdCol = 0;
        public const int TaskTypeCol = 1;
        public const int TargetIdCol = 2;
        public const int TargetCountCol = 3;
        public const int RequiredLevelCol = 4;
        public const int RewardContributionCol = 5;
        public const int RewardFundsCol = 6;

        public static List<PcGuildTaskEntry> ParseFile(string path)
        {
            var rows = new List<PcGuildTaskEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int taskId = PcItemCommon.Int(cols, TaskIdCol);
                if (taskId <= 0) continue;
                rows.Add(new PcGuildTaskEntry
                {
                    taskId = taskId,
                    taskType = PcItemCommon.Int(cols, TaskTypeCol),
                    targetId = PcItemCommon.Int(cols, TargetIdCol),
                    targetCount = cols.Length > TargetCountCol ? PcItemCommon.Int(cols, TargetCountCol) : 0,
                    requiredLevel = cols.Length > RequiredLevelCol ? PcItemCommon.Int(cols, RequiredLevelCol) : 0,
                    rewardContribution = cols.Length > RewardContributionCol ? PcItemCommon.Int(cols, RewardContributionCol) : 0,
                    rewardFunds = cols.Length > RewardFundsCol ? PcItemCommon.Int(cols, RewardFundsCol) : 0,
                });
            }
            return rows;
        }

        public static PcGuildTaskRegistry BuildRegistry(string dir)
        {
            var reg = new PcGuildTaskRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "tong_task.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcGuildTaskEntry
    {
        public int taskId;
        public int taskType;          // 0=Kill Boss, 1=Gather, 2=Donate, 3=Defend, 4=PvP
        public int targetId;          // BossId / ItemId / NpcId
        public int targetCount;
        public int requiredLevel;     // Cấp bang tối thiểu
        public int rewardContribution;
        public int rewardFunds;
    }

    public sealed class PcGuildTaskRegistry
    {
        private readonly Dictionary<int, PcGuildTaskEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcGuildTaskEntry e)
        {
            if (e == null || e.taskId <= 0) return;
            _byId[e.taskId] = e;
        }
        public PcGuildTaskEntry Get(int taskId)
            => _byId.TryGetValue(taskId, out var v) ? v : null;

        /// <summary>Lọc nhiệm vụ theo cấp bang hiện tại.</summary>
        public IReadOnlyList<PcGuildTaskEntry> GetByLevel(int guildLevel)
        {
            var result = new List<PcGuildTaskEntry>();
            foreach (var e in _byId.Values)
            {
                if (e == null) continue;
                if (guildLevel >= e.requiredLevel) result.Add(e);
            }
            return result;
        }

        public IEnumerable<PcGuildTaskEntry> All => _byId.Values;
    }
}
