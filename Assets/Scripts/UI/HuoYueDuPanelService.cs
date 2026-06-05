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
            return new HuoYueDuPanelSnapshot { rows = System.Array.Empty<HuoYueDuPanelRow>() };
        }

        public static IReadOnlyList<HuoYueDuPanelRow> GetTodayTasks(HuoYueDuService service)
        {
            return System.Array.Empty<HuoYueDuPanelRow>();
        }

        public static IReadOnlyList<HuoYueDuPanelRow> GetByReward(HuoYueDuService service, int itemId)
        {
            return System.Array.Empty<HuoYueDuPanelRow>();
        }

        public static bool TryClaim(HuoYueDuService service, int taskId)
        {
            return false;
        }

    }
}
