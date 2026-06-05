// -----------------------------------------------------------------------------
// VLTK Mobile — ST-7.x Daily Reward runtime service
// Wraps PcDailyRewardRegistry. PC source: settings/event/dailyreward.txt.
// Quản lý phần thưởng đăng nhập hằng ngày (30 ngày).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Phần Thưởng Hằng Ngày: lookup theo ngày, kiểm tra điều kiện VIP.
    /// </summary>
    public class DailyRewardService
    {
        public const string LogTag = "DailyReward";
        public const string DefaultStreamingDir = "Reference/PcEvent";

        private PcDailyRewardRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public DailyRewardService() { }
        public DailyRewardService(PcDailyRewardRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcDailyRewardRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "DailyReward registry rỗng");
        }

        public PcDailyRewardEntry GetReward(int dayIdx) => _reg != null ? _reg.Get(dayIdx) : null;

        public IReadOnlyList<PcDailyRewardEntry> GetForVip(int vipLevel)
            => _reg != null ? _reg.GetForVip(vipLevel) : Array.Empty<PcDailyRewardEntry>();

        public IReadOnlyList<PcDailyRewardEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcDailyRewardEntry>();

        /// <summary>
        /// Trả về phần thưởng theo chuỗi ngày liên tục đăng nhập. Tìm ngày cao nhất ≤ consecutiveDays.
        /// </summary>
        public PcDailyRewardEntry GetStreakReward(int consecutiveDays, int vipLevel)
        {
            if (_reg == null) return null;
            PcDailyRewardEntry match = null;
            foreach (var e in _reg.All)
            {
                if (e.dayIdx > consecutiveDays) continue;
                if (e.requiredVipLevel > vipLevel) continue;
                if (match == null || e.dayIdx > match.dayIdx) match = e;
            }
            return match;
        }

        public bool CanClaim(int dayIdx, int lastClaimDay, int vipLevel)
        {
            if (dayIdx <= 0) return false;
            if (dayIdx <= lastClaimDay) return false;
            var reward = GetReward(dayIdx);
            if (reward == null) return false;
            return reward.requiredVipLevel <= vipLevel;
        }

        public int GetTotalDays() => _reg != null ? _reg.Count : 0;

        public static DailyRewardService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcDailyRewardParser.BuildRegistry(dir);
            return new DailyRewardService(reg);
        }
    }
}
