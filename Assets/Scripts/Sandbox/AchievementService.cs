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
        private IAchievementHost _host;
        // per-player achievement progress: (playerId, achievementId) -> progress
        private readonly Dictionary<int, Dictionary<int, long>> _progress = new();
        // per-player completed set: playerId -> set of achievementIds
        private readonly Dictionary<int, HashSet<int>> _completed = new();

        public event Action<int, int> OnProgressUpdated; // (playerId, achievementId)
        public event Action<int, int> OnCompleted;        // (playerId, achievementId)

        public int Count => _reg?.Count ?? 0;

        public AchievementService() : this(null, null) { }
        public AchievementService(PcAchievementRegistry reg) : this(reg, null) { }
        public AchievementService(PcAchievementRegistry reg, IAchievementHost host) { _reg = reg; _host = host; }

        public void AttachHost(IAchievementHost host) { _host = host; }

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

        /// <summary>Player cập nhật tiến độ thành tựu. Trả về true nếu vừa hoàn thành.</summary>
        public bool TrackProgress(int playerId, int achievementId, long deltaProgress)
        {
            var ach = GetAchievement(achievementId);
            if (ach == null || ach.conditionValue <= 0) return false;
            if (!_progress.TryGetValue(playerId, out var pmap))
            {
                pmap = new Dictionary<int, long>();
                _progress[playerId] = pmap;
            }
            if (!_completed.TryGetValue(playerId, out var cset))
            {
                cset = new HashSet<int>();
                _completed[playerId] = cset;
            }
            if (cset.Contains(achievementId)) return false; // already completed
            long current = pmap.TryGetValue(achievementId, out long v) ? v : 0L;
            long newProgress = System.Math.Min(current + deltaProgress, ach.conditionValue);
            pmap[achievementId] = newProgress;
            OnProgressUpdated?.Invoke(playerId, achievementId);
            if (_host != null) _host.ShowAchievementIcon(playerId, achievementId, false);
            if (newProgress >= ach.conditionValue)
            {
                cset.Add(achievementId);
                OnCompleted?.Invoke(playerId, achievementId);
                if (_host != null)
                {
                    _host.ShowAchievementIcon(playerId, achievementId, true);
                    _host.OnAchievementCompleted(playerId, achievementId, ach.nameRaw);
                    _host.PlayAchievementSFX(playerId, achievementId);
                    if (ach.rewardItemId > 0 && ach.rewardCount > 0)
                        _host.GrantAchievementItem(playerId, ach.rewardItemId, ach.rewardCount);
                    if (ach.rewardExp > 0)
                        _host.GrantAchievementExp(playerId, ach.rewardExp);
                    if (ach.points > 0)
                        _host.AddAchievementPoints(playerId, ach.points);
                    _host.SaveProgress(playerId, achievementId, newProgress, true);
                }
                SubsystemLog.Info(LogTag, $"Player {playerId} hoàn thành thành tựu {ach.nameRaw} (id={achievementId})");
                return true;
            }
            if (_host != null) _host.SaveProgress(playerId, achievementId, newProgress, false);
            return false;
        }

        /// <summary>Player lấy tiến độ hiện tại cho 1 thành tựu.</summary>
        public long GetPlayerProgress(int playerId, int achievementId)
        {
            if (_progress.TryGetValue(playerId, out var pmap) && pmap.TryGetValue(achievementId, out var v))
                return v;
            return 0L;
        }

        /// <summary>Kiểm tra player đã hoàn thành thành tựu chưa.</summary>
        public bool IsPlayerCompleted(int playerId, int achievementId)
            => _completed.TryGetValue(playerId, out var cset) && cset.Contains(achievementId);

        /// <summary>Đếm số thành tựu player đã hoàn thành.</summary>
        public int GetPlayerCompletedCount(int playerId)
            => _completed.TryGetValue(playerId, out var cset) ? cset.Count : 0;

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
