// -----------------------------------------------------------------------------
// VLTK Mobile — ST-8.x Battle Reward Config Service
// Quản lý phần thưởng chiến trường. Reference: battlereward.txt.
// Vietnamese: "Phần Thưởng Chiến Trường", "Thắng", "Thua", "Hạng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý phần thưởng chiến trường.
    /// </summary>
    public class BattleRewardConfigService
    {
        public const string LogTag = "BattleRewardConfig";
        public const string DefaultStreamingDir = "Reference/PcBattlefield";

        private PcBattleRewardConfigRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public BattleRewardConfigService() { }
        public BattleRewardConfigService(PcBattleRewardConfigRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcBattleRewardConfigRegistry reg)
        {
            _registry = reg ?? new PcBattleRewardConfigRegistry();
            if (_registry.Count == 0) SubsystemLog.Warn(LogTag, "Phần thưởng chiến trường rỗng");
        }

        public static BattleRewardConfigService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new BattleRewardConfigService();
            var reg = PcBattleRewardConfigParser.BuildRegistry(dir);
            svc.RegisterRegistry(reg);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} mục phần thưởng");
            return svc;
        }

        public PcBattleRewardConfigEntry GetReward(int rewardId)
            => _registry != null ? _registry.Get(rewardId) : null;

        public IReadOnlyList<PcBattleRewardConfigEntry> GetByBattleType(int battleType)
            => _registry != null ? _registry.GetByBattleType(battleType) : Array.Empty<PcBattleRewardConfigEntry>();

        public IReadOnlyList<PcBattleRewardConfigEntry> GetForRank(int rank)
            => _registry != null ? _registry.GetForRank(rank) : Array.Empty<PcBattleRewardConfigEntry>();

        /// <summary>Lấy phần thưởng thắng cho battle type + rank cụ thể.</summary>
        public PcBattleRewardConfigEntry GetWinReward(int battleType, int rank)
        {
            if (_registry == null) return null;
            PcBattleRewardConfigEntry best = null;
            foreach (var e in _registry.GetByBattleType(battleType))
            {
                if (e.requiredRank > rank) continue;
                if (best == null || e.requiredRank > best.requiredRank) best = e;
            }
            return best;
        }

        /// <summary>Lấy phần thưởng thua cho battle type + rank.</summary>
        public PcBattleRewardConfigEntry GetLossReward(int battleType, int rank)
        {
            // Cùng entry thưởng thắng nhưng lấy cột loss
            return GetWinReward(battleType, rank);
        }
    }
}
