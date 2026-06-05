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
            return new DailyTaskPanelSnapshot { rows = System.Array.Empty<DailyTaskPanelRow>() };
        }

        public static bool TryAccept(DailyTaskService svc, int playerId, int taskId)
        {
            return false;
        }

        public static bool TryComplete(DailyTaskService svc, int playerId, int taskId)
        {
            return false;
        }

        public static int GetProgressPercent(int progress, int target)
        {
            return 0;
        }

        public static string GetProgressPercent(string text, int progress, int target)
        {
            return string.Empty;
        }

    }
}
