// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/task/metempsychosis/metempsychosistask.txt parser
// Nhiệm vụ chuyển sinh: yêu cầu cấp + số lần chuyển sinh, thưởng skill/title.
// Source: settings/task/metempsychosis/metempsychosistask.txt (GB2312, 8 cols).
//   TaskId  RequiredLevel  RequiredTranslifeCount  TaskType  TargetId
//   TargetCount  RewardSkillId  RewardTitle
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcMetempsychosisTaskParser
    {
        public const int TaskIdCol = 0;
        public const int RequiredLevelCol = 1;
        public const int RequiredTranslifeCountCol = 2;
        public const int TaskTypeCol = 3;
        public const int TargetIdCol = 4;
        public const int TargetCountCol = 5;
        public const int RewardSkillIdCol = 6;
        public const int RewardTitleCol = 7;

        public static List<PcMetempsychosisTaskEntry> ParseFile(string path)
        {
            var rows = new List<PcMetempsychosisTaskEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                rows.Add(new PcMetempsychosisTaskEntry
                {
                    taskId = PcItemCommon.Int(cols, TaskIdCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    requiredTranslifeCount = PcItemCommon.Int(cols, RequiredTranslifeCountCol),
                    taskType = PcItemCommon.Int(cols, TaskTypeCol),
                    targetId = PcItemCommon.Int(cols, TargetIdCol),
                    targetCount = PcItemCommon.Int(cols, TargetCountCol),
                    rewardSkillId = cols.Length > RewardSkillIdCol ? PcItemCommon.Int(cols, RewardSkillIdCol) : 0,
                    rewardTitle = cols.Length > RewardTitleCol ? PcItemCommon.Str(cols, RewardTitleCol) : string.Empty,
                });
            }
            return rows;
        }

        public static PcMetempsychosisTaskRegistry BuildRegistry(string dir)
        {
            var reg = new PcMetempsychosisTaskRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcMetempsychosisTaskEntry
    {
        public int taskId;
        public int requiredLevel;
        public int requiredTranslifeCount;
        public int taskType;
        public int targetId;
        public int targetCount;
        public int rewardSkillId;
        public string rewardTitle = string.Empty;
    }

    public sealed class PcMetempsychosisTaskRegistry
    {
        private readonly Dictionary<int, PcMetempsychosisTaskEntry> _byId = new();
        private readonly List<PcMetempsychosisTaskEntry> _all = new();
        public int Count => _byId.Count;
        public IEnumerable<PcMetempsychosisTaskEntry> All => _all;

        public void Register(PcMetempsychosisTaskEntry e)
        {
            if (e == null || e.taskId <= 0) return;
            _byId[e.taskId] = e;
            _all.Add(e);
        }

        public PcMetempsychosisTaskEntry Get(int taskId)
            => _byId.TryGetValue(taskId, out var v) ? v : null;

        public IReadOnlyList<PcMetempsychosisTaskEntry> GetByLevel(int playerLevel)
        {
            var result = new List<PcMetempsychosisTaskEntry>();
            foreach (var e in _all)
            {
                if (e != null && e.requiredLevel <= playerLevel) result.Add(e);
            }
            return result;
        }
    }
}
