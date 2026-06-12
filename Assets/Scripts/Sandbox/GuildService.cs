// -----------------------------------------------------------------------------
// VLTK Mobile — ST-09.x Guild (Bang Hội) Service
// Quản lý cấp bang, tài chính bang, công trình bang theo PC tong_level_data.txt.
// PC source: settings/tong/tong_level_data.txt (33 levels).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum GuildUpgradeResult
    {
        Success,           // Nâng cấp thành công
        NotEnoughFunds,    // Không đủ tài chính
        NotEnoughBuild,    // Không đủ công trình
        MaxLevel,          // Đã đạt cấp tối đa
        InvalidLevel,      // Cấp không hợp lệ
    }

    /// <summary>Service quản lý bang hội (Tài Chính Bang, Công Trình Bang, Nâng Cấp).</summary>
    public class GuildService
    {
        public const string LogTag = "Guild";

        private readonly PcTongLevelRegistry _registry;

        private int _guildLevel = 1;
        private int _guildFunds;
        private int _guildBuild;
        private string _guildName = string.Empty;

        /// <summary>Event kích hoạt khi bang nâng cấp cấp (oldLevel, newLevel).</summary>
        public event Action<int, int> OnGuildUpgraded;
        /// <summary>Event kích hoạt khi tài chính thay đổi (oldFunds, newFunds).</summary>
        public event Action<int, int> OnFundsChanged;

        public int GuildLevel { get => _guildLevel; set => _guildLevel = Math.Max(1, value); }
        public int GuildFunds => _guildFunds;
        public int GuildBuild => _guildBuild;
        public string GuildName { get => _guildName ?? string.Empty; set => _guildName = value ?? string.Empty; }

        public int Count => _registry.Count;
        public int MaxLevel => _registry.MaxLevel;

        public GuildService() : this(null) { }

        public GuildService(PcTongLevelRegistry registry)
        {
            _registry = registry ?? new PcTongLevelRegistry();
        }

        /// <summary>Tra cứu dữ liệu cấp bang (cấp 1 → 33).</summary>
        public PcTongLevelEntry GetLevelData(int level) => _registry.Get(level);

        /// <summary>Dữ liệu cấp bang hiện tại.</summary>
        public PcTongLevelEntry GetCurrentLevelData() => _registry.Get(_guildLevel);

        /// <summary>Chi phí tài chính để nâng cấp lên cấp mục tiêu.</summary>
        public int GetUpgradeCost(int targetLevel)
        {
            var entry = _registry.Get(targetLevel);
            return entry?.requiredFunds ?? 0;
        }

        /// <summary>Chi phí công trình để nâng cấp lên cấp mục tiêu.</summary>
        public int GetBuildCost(int targetLevel)
        {
            var entry = _registry.Get(targetLevel);
            return entry?.requiredBuild ?? 0;
        }

        /// <summary>Có thể nâng cấp lên cấp mục tiêu với tài chính hiện có không.</summary>
        public bool CanUpgrade(int targetLevel, int availableFunds)
        {
            if (targetLevel <= 0 || targetLevel > MaxLevel) return false;
            if (targetLevel <= _guildLevel) return false;
            return availableFunds >= GetUpgradeCost(targetLevel);
        }

        /// <summary>Thử nâng cấp bang lên cấp mục tiêu.</summary>
        public GuildUpgradeResult TryUpgrade(int targetLevel, int availableFunds)
        {
            if (targetLevel <= 0) return GuildUpgradeResult.InvalidLevel;
            if (targetLevel > MaxLevel) return GuildUpgradeResult.MaxLevel;
            if (targetLevel <= _guildLevel) return GuildUpgradeResult.InvalidLevel;

            int cost = GetUpgradeCost(targetLevel);
            if (availableFunds < cost) return GuildUpgradeResult.NotEnoughFunds;

            int oldLevel = _guildLevel;
            int oldFunds = _guildFunds;
            _guildLevel = targetLevel;
            _guildFunds = Math.Max(0, _guildFunds + availableFunds - cost);
            SubsystemLog.Info(LogTag, $"Nâng cấp bang: cấp {oldLevel} → {_guildLevel} (phí {cost})");
            OnGuildUpgraded?.Invoke(oldLevel, _guildLevel);
            OnFundsChanged?.Invoke(oldFunds, _guildFunds);
            return GuildUpgradeResult.Success;
        }

        /// <summary>Cấp cao nhất có thể đạt được với tài chính hiện có.</summary>
        public int GetMaxAffordableLevel(int availableFunds)
        {
            int best = _guildLevel;
            for (int lvl = _guildLevel + 1; lvl <= MaxLevel; lvl++)
            {
                if (GetUpgradeCost(lvl) <= availableFunds) best = lvl;
                else break;
            }
            return best;
        }

        /// <summary>Đóng góp vào tài chính bang.</summary>
        public int Donate(int amount)
        {
            if (amount <= 0) return _guildFunds;
            int old = _guildFunds;
            _guildFunds += amount;
            SubsystemLog.Info(LogTag, $"Đóng góp {amount} vào tài chính bang");
            OnFundsChanged?.Invoke(old, _guildFunds);
            return _guildFunds;
        }

        /// <summary>Chi tiêu tài chính để xây công trình.</summary>
        public bool SpendOnBuild(int amount)
        {
            if (amount <= 0) return true;
            if (_guildFunds < amount) return false;
            int oldFunds = _guildFunds;
            _guildFunds -= amount;
            _guildBuild += amount;
            SubsystemLog.Info(LogTag, $"Chi {amount} cho công trình bang");
            OnFundsChanged?.Invoke(oldFunds, _guildFunds);
            return true;
        }

        /// <summary>Load từ StreamingAssets/Reference/PcTong.</summary>
        public static GuildService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcTong");
            var reg = PcTongLevelParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} cấp bang (tối đa {reg.MaxLevel}) từ {dir}");
            return new GuildService(reg);
        }
    }
}
