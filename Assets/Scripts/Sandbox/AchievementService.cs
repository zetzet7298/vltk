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
        private IAchievementServiceHost _host;

        public int Count => _reg?.Count ?? 0;

        public AchievementService() { }
        public AchievementService(PcAchievementRegistry reg) { _reg = reg; }

        public void AttachHost(IAchievementServiceHost host) { _host = host; }

        public void RegisterRegistry(PcAchievementRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
            {
                SubsystemLog.Warn(LogTag, "Achievement registry rỗng");
            }
            else if (_host != null)
            {
                _host.OnAchievementRegistryAttached(_reg.Count);
                _host.LogAchievementEvent("load", 0, $"Loaded {_reg.Count} achievements");
                _host.PlayAchievementSFX("load", 0);
            }
        }

        public PcAchievementEntry GetAchievement(int id)
        {
            var a = _reg != null ? _reg.Get(id) : null;
            if (_host != null)
            {
                if (a != null)
                    _host.OnAchievementResolved(a.achievementId, a.category, a.nameRaw);
                else
                    _host.LogAchievementEvent("query_missing", id, "Achievement not found in registry");
            }
            return a;
        }

        public IReadOnlyList<PcAchievementEntry> GetByCategory(int category)
        {
            var list = _reg != null ? _reg.GetByCategory(category) : Array.Empty<PcAchievementEntry>();
            if (_host != null)
                _host.OnAchievementsByCategoryQueried(category, list.Count, GetCategoryName(category));
            return list;
        }

        public IReadOnlyList<PcAchievementEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcAchievementEntry>();

        /// <summary>
        /// Kiểm tra xem người chơi đủ điều kiện nhận thành tựu (cấp + tiến độ).
        /// </summary>
        public bool CanEarn(int achievementId, int playerLevel, long progress)
        {
            var ach = GetAchievement(achievementId);
            if (ach == null)
            {
                if (_host != null) _host.OnCanEarnEvaluated(achievementId, false, playerLevel, progress);
                return false;
            }
            bool result;
            // conditionType=0 là yêu cầu level
            if (ach.conditionType == 0 && playerLevel < ach.conditionValue) result = false;
            // các loại khác dùng progress (so với conditionValue)
            else if (ach.conditionType != 0 && progress < ach.conditionValue) result = false;
            else result = true;
            if (_host != null)
            {
                _host.OnCanEarnEvaluated(achievementId, result, playerLevel, progress);
                _host.LogAchievementEvent(result ? "can_earn" : "cannot_earn", achievementId, result ? "ok" : "blocked");
                _host.PlayAchievementSFX(result ? "unlock" : "block", achievementId);
            }
            return result;
        }

        /// <summary>
        /// Thử hoàn thành thành tựu: tăng progress nếu chưa đạt. Trả về true nếu vừa đạt max.
        /// </summary>
        public bool TryComplete(int achievementId, ref long progress)
        {
            var ach = GetAchievement(achievementId);
            if (ach == null || ach.conditionValue <= 0)
            {
                if (_host != null) _host.OnTryCompleteDispatched(achievementId, false, progress);
                return false;
            }
            bool result;
            if (progress < ach.conditionValue)
            {
                progress = ach.conditionValue;
                result = true;
            }
            else result = false;
            if (_host != null)
            {
                _host.OnTryCompleteDispatched(achievementId, result, progress);
                _host.LogAchievementEvent(result ? "complete" : "already_complete", achievementId, result ? $"Progress set to {progress}" : $"Already at {progress}");
                _host.PlayAchievementSFX(result ? "complete" : "progress", achievementId);
                _host.SaveAchievementState(achievementId, progress, ach.category);
                if (result) _host.ShowAchievementUI(achievementId, ach.nameRaw, ach.category);
            }
            return result;
        }

        /// <summary>
        /// Tính phần trăm hoàn thành (0..100).
        /// </summary>
        public float GetProgressPercent(int achievementId, long progress)
        {
            var ach = GetAchievement(achievementId);
            float pct = 0f;
            if (ach != null && ach.conditionValue > 0)
            {
                pct = (float)progress / ach.conditionValue * 100f;
                if (pct > 100f) pct = 100f;
            }
            if (_host != null) _host.OnProgressQueried(achievementId, pct, progress);
            return pct;
        }

        public string GetCategoryName(int category)
        {
            string name = category switch
            {
                CategoryCombat => "Chiến đấu",
                CategoryQuest => "Nhiệm vụ",
                CategorySkill => "Kỹ năng",
                CategoryInteraction => "Tương tác",
                CategoryCollection => "Sưu tầm",
                _ => "Khác",
            };
            return name;
        }

        public static AchievementService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcAchievementParser.BuildRegistry(dir);
            return new AchievementService(reg);
        }
    }
}
