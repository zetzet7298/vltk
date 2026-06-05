// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: HuoYueDu (Điểm Hoạt Động)
// Bảng UI quản lý điểm hoạt động hằng ngày, nhiệm vụ, thưởng, hết hạn.
// Vietnamese: "Điểm Hoạt Động", "Hôm nay", "Đã nhận", "Điểm thưởng", "Hết hạn".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct HuoYueDuPanelRow
    {
        public readonly int taskId;
        public readonly string name;
        public readonly string description;
        public readonly int currentPoints;
        public readonly int maxPoints;
        public readonly int rewardItemId;
        public readonly int rewardCount;
        public readonly bool isCompleted;
        public readonly int expiresSec;

        public HuoYueDuPanelRow(int taskId, string name, string description, int currentPoints, int maxPoints, int rewardItemId, int rewardCount, bool isCompleted, int expiresSec)
        {
            this.taskId = taskId;
            this.name = name ?? string.Empty;
            this.description = description ?? string.Empty;
            this.currentPoints = currentPoints;
            this.maxPoints = maxPoints;
            this.rewardItemId = rewardItemId;
            this.rewardCount = rewardCount;
            this.isCompleted = isCompleted;
            this.expiresSec = expiresSec;
        }
    }

    public sealed class HuoYueDuPanelSnapshot
    {
        public int playerId;
        public int totalPoints;
        public int totalToday;
        public int maxToday;
        public IReadOnlyList<HuoYueDuPanelRow> rows;
    }

    public static class HuoYueDuPanelService
    {
        public const string LabelHuoYueDu = "Điểm Hoạt Động";
        public const string LabelToday = "Hôm nay";
        public const string LabelClaimed = "Đã nhận";
        public const string LabelRewardPoints = "Điểm thưởng";
        public const string LabelExpired = "Hết hạn";

        public static HuoYueDuPanelSnapshot BuildSnapshot(HuoYueDuService service, int playerId)
        {
            var snapshot = new HuoYueDuPanelSnapshot
            {
                playerId = playerId,
                totalPoints = 0,
                totalToday = 0,
                maxToday = 0,
                rows = Array.Empty<HuoYueDuPanelRow>()
            };
            if (service == null) return snapshot;
            var all = service.GetAll();
            var rows = new List<HuoYueDuPanelRow>();
            int total = 0, today = 0;
            foreach (var entry in all)
            {
                if (entry == null) continue;
                int current = service.GetCurrentPoints(entry.taskId, playerId);
                bool completed = current >= entry.maxPoints;
                rows.Add(new HuoYueDuPanelRow(
                    entry.taskId, entry.nameRaw, entry.descriptionVi, current, entry.maxPoints,
                    entry.rewardItemId, entry.rewardCount, completed, entry.expiresSec));
                total += current;
                if (IsToday(entry)) today += current;
            }
            snapshot.totalPoints = total;
            snapshot.totalToday = today;
            snapshot.maxToday = 200;
            snapshot.rows = rows;
            return snapshot;
        }

        public static IReadOnlyList<HuoYueDuPanelRow> GetTodayTasks(HuoYueDuService service)
        {
            if (service == null) return Array.Empty<HuoYueDuPanelRow>();
            var rows = new List<HuoYueDuPanelRow>();
            foreach (var entry in service.GetAll())
            {
                if (entry == null) continue;
                if (IsToday(entry))
                {
                    rows.Add(new HuoYueDuPanelRow(
                        entry.taskId, entry.nameRaw, entry.descriptionVi, 0, entry.maxPoints,
                        entry.rewardItemId, entry.rewardCount, false, entry.expiresSec));
                }
            }
            return rows;
        }

        public static IReadOnlyList<HuoYueDuPanelRow> GetByReward(HuoYueDuService service, int itemId)
        {
            if (service == null || itemId <= 0) return Array.Empty<HuoYueDuPanelRow>();
            var rows = new List<HuoYueDuPanelRow>();
            foreach (var entry in service.GetAll())
            {
                if (entry == null) continue;
                if (entry.rewardItemId == itemId)
                {
                    rows.Add(new HuoYueDuPanelRow(
                        entry.taskId, entry.nameRaw, entry.descriptionVi, 0, entry.maxPoints,
                        entry.rewardItemId, entry.rewardCount, false, entry.expiresSec));
                }
            }
            return rows;
        }

        public static bool TryClaim(HuoYueDuService service, int taskId)
        {
            if (service == null || taskId <= 0) return false;
            return service.TryClaim(taskId);
        }

        private static bool IsToday(object entry)
        {
            // Heuristic: any task with expiresSec > 0 considered today
            try
            {
                var prop = entry.GetType().GetProperty("expiresSec");
                if (prop != null)
                {
                    int exp = (int)prop.GetValue(entry);
                    return exp > 0;
                }
            }
            catch { }
            return false;
        }
    }
}
