// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.x Achievement runtime service
// Wraps PcAchievementRegistry. PC source: settings/achievement/achievement.txt.
// Quản lý thành tựu (250+): theo dõi tiến độ, hoàn thành, phần thưởng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Thành Tựu: lookup, kiểm tra điều kiện, hoàn thành, phần thưởng.
    /// </summary>
    public class AchievementService
    {
        public const string LogTag = "Achievement";
        public const string DefaultStreamingDir = "Reference/PcAchievement";

        public const int CategoryCombat = 0;       // Chiến đấu
        public const int CategoryQuest = 1;        // Nhiệm vụ
        public const int CategorySkill = 2;        // Kỹ năng
        public const int CategoryInteraction = 3;  // Tương tác
        public const int CategoryCollection = 4;   // Sưu tầm

        private PcAchievementRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public AchievementService() { }
        public AchievementService(PcAchievementRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcAchievementRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Achievement registry rỗng");
        }

        public PcAchievementEntry GetAchievement(int id) => _reg != null ? _reg.Get(id) : null;

        public IReadOnlyList<PcAchievementEntry> GetByCategory(int category)
            => _reg != null ? _reg.GetByCategory(category) : Array.Empty<PcAchievementEntry>();

        public IReadOnlyList<PcAchievementEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcAchievementEntry>();

        /// <summary>
        /// Kiểm tra xem người chơi đủ điều kiện nhận thành tựu (cấp + tiến độ).
        /// </summary>
        public bool CanEarn(int achievementId, int playerLevel, long progress)
        {
            var ach = GetAchievement(achievementId);
            if (ach == null) return false;
            // conditionType=0 là yêu cầu level
            if (ach.conditionType == 0 && playerLevel < ach.conditionValue) return false;
            // các loại khác dùng progress (so với conditionValue)
            if (ach.conditionType != 0 && progress < ach.conditionValue) return false;
            return true;
        }

        /// <summary>
        /// Thử hoàn thành thành tựu: tăng progress nếu chưa đạt. Trả về true nếu vừa đạt max.
        /// </summary>
        public bool TryComplete(int achievementId, ref long progress)
        {
            var ach = GetAchievement(achievementId);
            if (ach == null || ach.conditionValue <= 0) return false;
            if (progress < ach.conditionValue)
            {
                progress = ach.conditionValue;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Tính phần trăm hoàn thành (0..100).
        /// </summary>
        public float GetProgressPercent(int achievementId, long progress)
        {
            var ach = GetAchievement(achievementId);
            if (ach == null || ach.conditionValue <= 0) return 0f;
            float pct = (float)progress / ach.conditionValue * 100f;
            return pct > 100f ? 100f : pct;
        }

        public string GetCategoryName(int category)
        {
            switch (category)
            {
                case CategoryCombat: return "Chiến đấu";
                case CategoryQuest: return "Nhiệm vụ";
                case CategorySkill: return "Kỹ năng";
                case CategoryInteraction: return "Tương tác";
                case CategoryCollection: return "Sưu tầm";
                default: return "Khác";
            }
        }

        public static AchievementService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcAchievementParser.BuildRegistry(dir);
            return new AchievementService(reg);
        }
    }
}
