// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Nhiệm Vụ Hằng Ngày (Daily Task Panel)
// PC source: task/dailytask/dailytask.txt.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct DailyTaskPanelRow
    {
        public readonly int taskId;
        public readonly string taskName;
        public readonly string taskDesc;
        public readonly int progress;
        public readonly int target;
        public readonly bool isAccepted;
        public readonly bool isCompleted;
        public readonly int rewardItemId;
        public readonly int rewardCount;
        public readonly int timeLeftSec;

        public DailyTaskPanelRow(int taskId, string taskName, string taskDesc, int progress, int target, bool isAccepted, bool isCompleted, int rewardItemId, int rewardCount, int timeLeftSec)
        {
            this.taskId = taskId;
            this.taskName = taskName;
            this.taskDesc = taskDesc;
            this.progress = progress;
            this.target = target;
            this.isAccepted = isAccepted;
            this.isCompleted = isCompleted;
            this.rewardItemId = rewardItemId;
            this.rewardCount = rewardCount;
            this.timeLeftSec = timeLeftSec;
        }
    }

    public sealed class DailyTaskPanelSnapshot
    {
        public int playerLevel;
        public int dailyRefreshSec;
        public int completedCount;
        public int totalCount;
        public IReadOnlyList<DailyTaskPanelRow> rows;
    }

    public static class DailyTaskPanelService
    {
        public const int DailyRefreshSecDefault = 86400; // 24h

        public static DailyTaskPanelSnapshot BuildSnapshot(DailyTaskService svc, int playerId)
        {
            if (svc == null)
                return new DailyTaskPanelSnapshot { dailyRefreshSec = DailyRefreshSecDefault, rows = System.Array.Empty<DailyTaskPanelRow>() };

            var entries = svc.GetAllDailyTasks();
            var rows = new List<DailyTaskPanelRow>(entries.Count);
            foreach (var e in entries)
            {
                int target = e.targetCount > 0 ? e.targetCount : 1;
                rows.Add(new DailyTaskPanelRow(
                    e.taskId,
                    $"Nhiệm vụ #{e.taskId}",
                    BuildDescription(e),
                    0,
                    target,
                    false,
                    false,
                    e.rewardItem,
                    e.rewardItem > 0 ? 1 : 0,
                    DailyRefreshSecDefault));
            }

            return new DailyTaskPanelSnapshot
            {
                dailyRefreshSec = DailyRefreshSecDefault,
                completedCount = 0,
                totalCount = rows.Count,
                rows = rows
            };
        }

        public static bool TryAccept(DailyTaskService svc, int playerId, int taskId)
        {
            if (svc == null || taskId <= 0)
                return false;
            // PC dailytask.txt mỗi nhiệm vụ có dải cấp [minLevel, maxLevel]. Panel chỉ
            // cầm playerId nên dùng cấp mở khóa của chính nhiệm vụ (minLevel) — nằm trong
            // dải hợp lệ — thay vì int.MaxValue (sẽ vượt maxLevel và bị từ chối oan).
            var entry = svc.GetDailyTask(taskId);
            if (entry == null)
                return false;
            int acceptLevel = entry.minLevel > 0 ? entry.minLevel : 1;
            if (entry.maxLevel > 0 && acceptLevel > entry.maxLevel)
                acceptLevel = entry.maxLevel;
            return svc.Accept(taskId, acceptLevel);
        }

        public static bool TryComplete(DailyTaskService svc, int playerId, int taskId)
        {
            if (svc == null || taskId <= 0)
                return false;
            return svc.Complete(taskId);
        }

        public static int GetProgressPercent(int progress, int target)
        {
            if (target <= 0 || progress <= 0)
                return 0;
            if (progress >= target)
                return 100;
            return UnityEngine.Mathf.Clamp(progress * 100 / target, 0, 100);
        }

        public static string GetProgressPercent(string text, int progress, int target)
            => string.IsNullOrEmpty(text) ? $"{GetProgressPercent(progress, target)}%" : text;

        private static string BuildDescription(PcDailyTaskEntry e)
        {
            string type = e.taskType switch
            {
                0 => "Diệt quái",
                1 => "Thu thập",
                2 => "Đối thoại",
                3 => "Đến địa điểm",
                _ => "Nhiệm vụ",
            };
            return $"{type}: mục tiêu {e.targetId} x{(e.targetCount > 0 ? e.targetCount : 1)} — thưởng EXP {e.rewardExp}, bạc {e.rewardSilver}";
        }
    }
}
