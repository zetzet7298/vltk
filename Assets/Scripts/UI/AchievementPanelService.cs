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
            return new AchievementPanelSnapshot { rows = System.Array.Empty<AchievementPanelRow>() };
        }

        public static IReadOnlyList<AchievementPanelRow> GetByCategory(AchievementService ach, int category)
        {
            return System.Array.Empty<AchievementPanelRow>();
        }

        public static float GetProgress(AchievementService ach, int achievementId)
        {
            return 0f;
        }

        public static bool TryClaim(AchievementService ach, int achievementId)
        {
            return false;
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
