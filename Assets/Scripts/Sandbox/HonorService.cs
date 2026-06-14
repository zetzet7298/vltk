// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.12 Honor Service (Vinh Danh runtime)
// Quản lý hệ thống vinh danh: 6 cấp bậc, danh hiệu thưởng, hào quang kích hoạt.
// PC source: settings/honor.txt (vinh danh).
// Vietnamese: "Vinh Danh", "Danh Hiệu", "Quang Huy", "Hào Quang".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý Vinh Danh (Honor System runtime).</summary>
    public class HonorService
    {
        public const string LogTag = "Honor";

        private PcHonorRegistry _registry;
        private IHonorHost _host;
        // Per-player achieved honors: playerId -> set of honorIds
        private readonly Dictionary<int, HashSet<int>> _playerAchieved = new();
        // Per-player current honor points: playerId -> points
        private readonly Dictionary<int, int> _playerPoints = new();

        public event Action OnHonorLoaded;
        public event Action<int, int> OnPlayerHonorAchieved; // (playerId, honorId)

        public int Count => _registry != null ? _registry.Count : 0;

        public HonorService() : this(null, null) { }
        public HonorService(PcHonorRegistry registry) : this(registry, null) { }
        public HonorService(PcHonorRegistry registry, IHonorHost host)
        {
            _host = host;
            AttachRegistry(registry);
        }

        public void AttachHost(IHonorHost host) { _host = host; }

        public void AttachRegistry(PcHonorRegistry registry)
        {
            _registry = registry ?? new PcHonorRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} vinh danh");
            OnHonorLoaded?.Invoke();
        }

        public PcHonorEntry GetHonor(int honorId)
            => _registry != null ? _registry.Get(honorId) : null;

        public PcHonorEntry GetByPoints(int points)
            => _registry != null ? _registry.GetByPoints(points) : null;

        /// <summary>Kiểm tra có đủ điểm để đạt vinh danh này không.</summary>
        public bool CanAchieve(int honorId, int points)
        {
            var entry = GetHonor(honorId);
            if (entry == null) return false;
            return points >= entry.requiredPoints;
        }

        /// <summary>Player cộng điểm vinh danh. Trả về honorId vừa đạt (hoặc 0 nếu chưa).</summary>
        public int AddPoints(int playerId, int deltaPoints)
        {
            if (!_playerPoints.ContainsKey(playerId)) _playerPoints[playerId] = 0;
            _playerPoints[playerId] += deltaPoints;
            int newTotal = _playerPoints[playerId];
            int achievedId = 0;
            if (_registry != null)
            {
                foreach (var entry in _registry.All)
                {
                    if (entry == null) continue;
                    if (newTotal >= entry.requiredPoints && !HasAchieved(playerId, entry.honorId))
                    {
                        if (AchieveHonor(playerId, entry.honorId)) achievedId = entry.honorId;
                    }
                }
            }
            return achievedId;
        }

        /// <summary>Đánh dấu player đạt vinh danh (dispatch host + set state).</summary>
        public bool AchieveHonor(int playerId, int honorId)
        {
            var entry = GetHonor(honorId);
            if (entry == null) return false;
            if (!_playerAchieved.TryGetValue(playerId, out var hset))
            {
                hset = new HashSet<int>();
                _playerAchieved[playerId] = hset;
            }
            if (hset.Contains(honorId)) return false; // already achieved
            hset.Add(honorId);
            OnPlayerHonorAchieved?.Invoke(playerId, honorId);
            if (_host != null)
            {
                if (entry.titleReward > 0)
                    _host.GrantTitle(playerId, honorId, entry.titleReward);
                if (entry.auraSkillId > 0)
                    _host.ActivateAura(playerId, honorId, entry.auraSkillId);
                _host.ShowHonorNotice(playerId, honorId, entry.honorName);
                _host.OnHonorAchieved(playerId, honorId, entry.honorName, entry.requiredPoints);
                _host.PlayHonorSFX(playerId, honorId);
                int points = _playerPoints.TryGetValue(playerId, out int p) ? p : 0;
                _host.LogHonorEvent(playerId, honorId, $"Đạt vinh danh {entry.honorName} (cần {entry.requiredPoints} điểm)");
                _host.SaveHonorProgress(playerId, honorId, points, true);
            }
            SubsystemLog.Info(LogTag, $"Player {playerId} đạt vinh danh {entry.honorName} (id={honorId})");
            return true;
        }

        /// <summary>Player đã đạt vinh danh chưa.</summary>
        public bool HasAchieved(int playerId, int honorId)
            => _playerAchieved.TryGetValue(playerId, out var hset) && hset.Contains(honorId);

        /// <summary>Lấy điểm vinh danh hiện tại của player.</summary>
        public int GetPlayerPoints(int playerId)
            => _playerPoints.TryGetValue(playerId, out int p) ? p : 0;

        /// <summary>Đếm số vinh danh player đã đạt.</summary>
        public int GetAchievedCount(int playerId)
            => _playerAchieved.TryGetValue(playerId, out var hset) ? hset.Count : 0;

        public IEnumerable<PcHonorEntry> GetAll()
            => _registry != null ? _registry.All : (IEnumerable<PcHonorEntry>)Array.Empty<PcHonorEntry>();

        public static HonorService LoadFromStreamingAssets(string subdir = "Reference/PcAttrib")
        {
            var svc = new HonorService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcHonorParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            // Fallback: tìm honor.txt ở root.
            if (svc.Count == 0)
            {
                var fallback = PcHonorParser.BuildRegistryFromRoot();
                if (fallback.Count > 0)
                {
                    svc.AttachRegistry(fallback);
                    return svc;
                }
            }
            if (svc.Count == 0)
                SubsystemLog.Warn(LogTag, "Honor: không tìm thấy honor.txt trong StreamingAssets");
            if (svc.Count == 0) svc.OnHonorLoaded?.Invoke();
            return svc;
        }
    }
}
