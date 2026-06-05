// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Nhiệm Vụ Hằng Ngày (Daily Task Panel)
// Reference: PC daily quest system + DailyTaskService.
// Vietnamese: "Nhiệm Vụ Hằng Ngày", "Làm mới sau", "Phần thưởng".
// -----------------------------------------------------------------------------

using System;
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
            var snap = new DailyTaskPanelSnapshot
            {
                playerLevel = 1,
                dailyRefreshSec = DailyRefreshSecDefault,
                completedCount = 0,
                totalCount = 0,
                rows = System.Array.Empty<DailyTaskPanelRow>(),
            };
            if (svc == null) return snap;
            int nowSec = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            int midnight = (int)(DateTimeOffset.UtcNow.Date.AddDays(1).ToUnixTimeSeconds());
            snap.dailyRefreshSec = Math.Max(0, midnight - nowSec);
            snap.totalCount = svc.Count;
            var list = new List<DailyTaskPanelRow>();
            foreach (var t in svc.GetAllDailyTasks())
            {
                int prog = svc.GetProgress(playerId, t.taskId);
                int target = t.targetCount;
                bool completed = svc.IsCompleted(playerId, t.taskId);
                bool accepted = svc.IsAccepted(playerId, t.taskId);
                int timeLeft = snap.dailyRefreshSec;
                if (completed) snap.completedCount++;
                list.Add(new DailyTaskPanelRow(
                    t.taskId,
                    t.taskName ?? ("Nhiệm vụ " + t.taskId),
                    t.description ?? string.Empty,
                    prog,
                    target,
                    accepted,
                    completed,
                    t.rewardItemId,
                    t.rewardCount,
                    timeLeft));
            }
            snap.rows = list;
            return snap;
        }

        public static bool TryAccept(DailyTaskService svc, int playerId, int taskId)
        {
            if (svc == null || taskId <= 0) return false;
            return svc.Accept(playerId, taskId, 1);
        }

        public static bool TryComplete(DailyTaskService svc, int playerId, int taskId)
        {
            if (svc == null || taskId <= 0) return false;
            return svc.Complete(playerId, taskId);
        }

        public static int GetProgressPercent(int progress, int target)
        {
            if (target <= 0) return 0;
            return Math.Min(100, (progress * 100) / target);
        }

        public static string GetProgressPercent(string text, int progress, int target)
        {
            int p = GetProgressPercent(progress, target);
            return $"{text} {progress}/{target} ({p}%)";
        }
    }
}
