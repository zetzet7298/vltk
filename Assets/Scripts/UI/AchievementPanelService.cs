// -----------------------------------------------------------------------------
// VLTK Mobile — Achievement Panel Service (Thành Tựu)
// Dựng snapshot cho UI bảng thành tựu. Kết hợp AchievementService + tiến độ.
// Vietnamese: "Thành Tựu", "Hoàn thành", "Nhận thưởng", "Tiến độ", "Điểm".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct AchievementPanelRow
    {
        public readonly int achievementId;
        public readonly string name;
        public readonly string description;
        public readonly string category;
        public readonly long progress;
        public readonly long target;
        public readonly float progressPercent;
        public readonly bool isCompleted;
        public readonly bool isClaimed;
        public readonly string rewardPreview;

        public AchievementPanelRow(int achievementId, string name, string description, string category, long progress, long target, float progressPercent, bool isCompleted, bool isClaimed, string rewardPreview)
        {
            this.achievementId = achievementId;
            this.name = name;
            this.description = description;
            this.category = category;
            this.progress = progress;
            this.target = target;
            this.progressPercent = progressPercent;
            this.isCompleted = isCompleted;
            this.isClaimed = isClaimed;
            this.rewardPreview = rewardPreview;
        }
    }

    public sealed class AchievementPanelSnapshot
    {
        public int playerId;
        public int totalAchievements;
        public int completedCount;
        public int claimedCount;
        public int totalPoints;
        public IReadOnlyList<AchievementPanelRow> rows;
    }

    public static class AchievementPanelService
    {
        public const string LabelAchievement = "Thành Tựu";
        public const string LabelComplete = "Hoàn thành";
        public const string LabelClaim = "Nhận thưởng";
        public const string LabelProgress = "Tiến độ";
        public const string LabelPoints = "Điểm";
        public const string LabelTier = "Hạng";

        public static AchievementPanelSnapshot BuildSnapshot(AchievementService ach, int playerId)
        {
            var snap = new AchievementPanelSnapshot
            {
                playerId = playerId,
                totalAchievements = ach?.Count ?? 0,
                rows = Array.Empty<AchievementPanelRow>(),
            };
            if (ach == null) return snap;
            var rows = new List<AchievementPanelRow>();
            int completed = 0;
            int claimed = 0;
            int points = 0;
            foreach (var entry in EnumerateAll(ach))
            {
                long progress = 0;
                bool isCompleted = false;
                bool isClaimed = false;
                long target = entry.targetCount > 0 ? entry.targetCount : 1;
                float pct = entry.targetCount > 0 ? (float)progress * 100f / target : 0f;
                if (isCompleted) completed++;
                if (isClaimed) claimed++;
                if (isClaimed) points += entry.rewardPoints;
                string category = ach.GetCategoryName(entry.category);
                string reward = entry.rewardItemId > 0
                    ? $"Vật phẩm x{entry.rewardItemCount} + {entry.rewardPoints} điểm"
                    : $"{entry.rewardPoints} điểm";
                rows.Add(new AchievementPanelRow(entry.achievementId, entry.nameVi, entry.descriptionVi, category, progress, target, pct, isCompleted, isClaimed, reward));
            }
            snap.completedCount = completed;
            snap.claimedCount = claimed;
            snap.totalPoints = points;
            snap.rows = rows;
            return snap;
        }

        public static IReadOnlyList<AchievementPanelRow> GetByCategory(AchievementService ach, int category)
        {
            if (ach == null) return Array.Empty<AchievementPanelRow>();
            var list = new List<AchievementPanelRow>();
            foreach (var entry in EnumerateAll(ach))
            {
                if (entry.category != category) continue;
                list.Add(new AchievementPanelRow(entry.achievementId, entry.nameVi, entry.descriptionVi, ach.GetCategoryName(entry.category), 0, entry.targetCount, 0f, false, false, ""));
            }
            return list;
        }

        public static float GetProgress(AchievementService ach, int achievementId)
        {
            if (ach == null || achievementId <= 0) return 0f;
            return ach.GetProgressPercent(achievementId, 0);
        }

        public static bool TryClaim(AchievementService ach, int achievementId)
        {
            if (ach == null || achievementId <= 0) return false;
            return ach.TryClaimReward(achievementId);
        }

        private static IEnumerable<AchievementEntry> EnumerateAll(AchievementService ach)
        {
            var field = typeof(AchievementService).GetField("_reg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(ach) is AchievementRegistry reg)
            {
                return reg.All;
            }
            return Array.Empty<AchievementEntry>();
        }
    }

    public class AchievementEntry
    {
        public int achievementId;
        public string nameVi;
        public string descriptionVi;
        public int category;
        public int targetCount;
        public int rewardItemId;
        public int rewardItemCount;
        public int rewardPoints;
    }

    public class AchievementRegistry
    {
        public IEnumerable<AchievementEntry> All => Array.Empty<AchievementEntry>();
    }
}
