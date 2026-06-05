// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/task/partner/partner_task_def.txt Partner Task parser
// Nhiệm vụ pet (đồng hành): thu thập / tiêu diệt theo pet + level.
// Source: settings/task/partner/partner_task_def.txt (GB2312, 7 tab cols).
//   TaskId  PartnerId  TaskType  TargetId  TargetCount  MinLevel  RewardItem
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcPartnerTaskParser
    {
        public const int TaskIdCol = 0;
        public const int PartnerIdCol = 1;
        public const int TaskTypeCol = 2;
        public const int TargetIdCol = 3;
        public const int TargetCountCol = 4;
        public const int MinLevelCol = 5;
        public const int RewardItemCol = 6;

        public static List<PcPartnerTaskEntry> ParseFile(string path)
        {
            var rows = new List<PcPartnerTaskEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcPartnerTaskEntry
                {
                    taskId = PcItemCommon.Int(cols, TaskIdCol),
                    partnerId = PcItemCommon.Int(cols, PartnerIdCol),
                    taskType = PcItemCommon.Int(cols, TaskTypeCol),
                    targetId = PcItemCommon.Int(cols, TargetIdCol),
                    targetCount = PcItemCommon.Int(cols, TargetCountCol),
                    minLevel = cols.Length > MinLevelCol ? PcItemCommon.Int(cols, MinLevelCol) : 0,
                    rewardItem = cols.Length > RewardItemCol ? PcItemCommon.Int(cols, RewardItemCol) : 0,
                });
            }
            return rows;
        }

        public static PcPartnerTaskRegistry BuildRegistry(string dir)
        {
            var reg = new PcPartnerTaskRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "partner_task_def.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcPartnerTaskEntry
    {
        public int taskId;
        public int partnerId;
        public int taskType;
        public int targetId;
        public int targetCount;
        public int minLevel;
        public int rewardItem;
    }

    public sealed class PcPartnerTaskRegistry
    {
        private readonly Dictionary<int, PcPartnerTaskEntry> _byId = new();
        private readonly Dictionary<int, List<PcPartnerTaskEntry>> _byPartner = new();
        private readonly List<PcPartnerTaskEntry> _all = new();
        public int Count => _byId.Count;
        public IEnumerable<PcPartnerTaskEntry> All => _all;

        public void Register(PcPartnerTaskEntry e)
        {
            if (e == null || e.taskId <= 0) return;
            _byId[e.taskId] = e;
            _all.Add(e);
            if (!_byPartner.TryGetValue(e.partnerId, out var list))
            {
                list = new List<PcPartnerTaskEntry>();
                _byPartner[e.partnerId] = list;
            }
            list.Add(e);
        }

        public PcPartnerTaskEntry Get(int taskId)
            => _byId.TryGetValue(taskId, out var v) ? v : null;

        public IReadOnlyList<PcPartnerTaskEntry> GetByPartner(int partnerId)
            => _byPartner.TryGetValue(partnerId, out var v) ? v : (IReadOnlyList<PcPartnerTaskEntry>)System.Array.Empty<PcPartnerTaskEntry>();

        public IReadOnlyList<PcPartnerTaskEntry> GetByLevel(int playerLevel)
        {
            var result = new List<PcPartnerTaskEntry>();
            foreach (var e in _all)
            {
                if (e == null) continue;
                if (e.minLevel > 0 && playerLevel < e.minLevel) continue;
                result.Add(e);
            }
            return result;
        }
    }
}
