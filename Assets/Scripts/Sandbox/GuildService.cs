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
        private readonly IGuildHost _host;
        private readonly Dictionary<int, GuildMemberRole> _members = new();

        public const int MinGuildNameLength = 3;
        public const int MaxGuildNameLength = 12;

        private int _guildLevel = 1;
        private int _guildFunds;
        private int _guildBuild;
        private string _guildName = string.Empty;
        private string _founderName = string.Empty;
        private bool _isCreated;

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

        public bool IsCreated => _isCreated;
        public string FounderName => _founderName ?? string.Empty;
        public int MemberCount => _members.Count;
        public IReadOnlyDictionary<int, GuildMemberRole> Members => _members;
        public const int CreateCost = 1000; // PC: tạo bang tốn 1000 lượng

        public event Action<string, string> OnGuildCreatedEvent; // (guildName, founderName)
        public event Action<string, string> OnGuildDisbandedEvent; // (guildName, leaderName)
        public event Action<int, GuildMemberRole> OnMemberJoinedEvent; // (playerId, role)
        public event Action<int, GuildMemberRole> OnMemberLeftEvent; // (playerId, previousRole)

        public GuildService() : this(null, null) { }

        public GuildService(PcTongLevelRegistry registry) : this(registry, null) { }

        public GuildService(PcTongLevelRegistry registry, IGuildHost host)
        {
            _registry = registry ?? new PcTongLevelRegistry();
            _host = host;
        }

        public void AttachHost(IGuildHost host) { /* host is fixed at ctor; kept for API symmetry */ }

        // ── Lifecycle ────────────────────────────────────────────────────────

        public enum GuildCreationResult
        {
            Success,
            AlreadyCreated,
            InvalidName,
            InsufficientFunds,
        }

        /// <summary>Tạo bang mới với tên và bang chủ. PC: tong_apply.lua + Pay 1000.</summary>
        public GuildCreationResult CreateGuild(string guildName, string founderName, int founderId, int availableMoney)
        {
            if (_isCreated) return GuildCreationResult.AlreadyCreated;
            if (string.IsNullOrEmpty(guildName) || guildName.Length < MinGuildNameLength
                || guildName.Length > MaxGuildNameLength)
                return GuildCreationResult.InvalidName;
            if (availableMoney < CreateCost) return GuildCreationResult.InsufficientFunds;

            // Trừ tiền player qua host (PC Pay)
            if (_host != null && !_host.TryDeductPlayerMoney(founderName, CreateCost))
                return GuildCreationResult.InsufficientFunds;

            _guildName = guildName;
            _founderName = founderName ?? string.Empty;
            _guildLevel = 1;
            _guildFunds = 0;
            _guildBuild = 0;
            _members.Clear();
            _members[founderId] = GuildMemberRole.Leader;
            _isCreated = true;

            SubsystemLog.Info(LogTag, $"Tạo bang '{guildName}' bởi {founderName}");
            OnGuildCreatedEvent?.Invoke(_guildName, _founderName);
            if (_host != null)
            {
                _host.OnGuildCreated(_guildName, _founderName);
                _host.OnMemberJoined(_guildName, _founderName, GuildMemberRole.Leader);
            }
            return GuildCreationResult.Success;
        }

        /// <summary>Giải tán bang (chỉ bang chủ mới có quyền). PC: tong_disband.lua.</summary>
        public bool DisbandGuild(int leaderId)
        {
            if (!_isCreated) return false;
            if (!_members.TryGetValue(leaderId, out var role) || role != GuildMemberRole.Leader)
                return false;
            string oldName = _guildName;
            string oldLeader = _founderName;
            _isCreated = false;
            _guildName = string.Empty;
            _founderName = string.Empty;
            _members.Clear();
            SubsystemLog.Info(LogTag, $"Giải tán bang '{oldName}' bởi {oldLeader}");
            OnGuildDisbandedEvent?.Invoke(oldName, oldLeader);
            if (_host != null) _host.OnGuildDisbanded(oldName, oldLeader);
            return true;
        }

        /// <summary>Thêm thành viên vào bang (chỉ Leader/ Elder mới có quyền).</summary>
        public bool AddMember(int playerId, string playerName, GuildMemberRole role, int inviterId)
        {
            if (!_isCreated) return false;
            if (_members.ContainsKey(playerId)) return false;
            if (!_members.TryGetValue(inviterId, out var inviterRole)) return false;
            if (inviterRole != GuildMemberRole.Leader && inviterRole != GuildMemberRole.Elder)
                return false;

            _members[playerId] = role;
            SubsystemLog.Info(LogTag, $"{playerName} gia nhập bang '{_guildName}'");
            OnMemberJoinedEvent?.Invoke(playerId, role);
            if (_host != null) _host.OnMemberJoined(_guildName, playerName, role);
            return true;
        }

        /// <summary>Rời bang hoặc bị kick. PC: tong_leave.lua / tong_kick.lua.</summary>
        public bool RemoveMember(int playerId, string playerName)
        {
            if (!_isCreated) return false;
            if (!_members.TryGetValue(playerId, out var role)) return false;
            // Bang chủ không thể tự rời; phải disband
            if (role == GuildMemberRole.Leader) return false;

            _members.Remove(playerId);
            SubsystemLog.Info(LogTag, $"{playerName} rời bang '{_guildName}'");
            OnMemberLeftEvent?.Invoke(playerId, role);
            if (_host != null) _host.OnMemberLeft(_guildName, playerName, role);
            return true;
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
