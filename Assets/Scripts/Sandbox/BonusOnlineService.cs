// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.15 Bonus Online Time Service
// Thưởng online runtime: tra cứu bonus theo phút + kiểm tra điều kiện claim.
// PC source: settings/bonus_onlinetime/bonus_online.txt
// Vietnamese: "Thưởng Online", "Phần Thưởng Đăng Nhập", "Tích Lũy Phút".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class BonusOnlineService
    {
        public const string LogTag = "BonusOnline";
        public const string DefaultStreamingDir = "Reference/PcBonusOnline";

        private readonly PcBonusOnlineRegistry _registry;
        private readonly HashSet<int> _claimedBonuses = new();

        /// <summary>Sự kiện khi nhận thưởng online thành công.</summary>
        public event Action<PcBonusOnlineEntry> OnBonusClaimed;

        public int Count => _registry?.Count ?? 0;
        public int ClaimedCount => _claimedBonuses.Count;

        public BonusOnlineService(PcBonusOnlineRegistry registry)
        {
            _registry = registry ?? new PcBonusOnlineRegistry();
        }

        public static BonusOnlineService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = string.IsNullOrEmpty(subDir)
                ? Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir)
                : Path.Combine(Application.streamingAssetsPath, subDir);
            var reg = PcBonusOnlineParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} thưởng online từ {dir}");
            return new BonusOnlineService(reg);
        }

        public PcBonusOnlineEntry GetBonus(int bonusId)
            => _registry.Get(bonusId);

        public IReadOnlyList<PcBonusOnlineEntry> GetBonusForMinutes(int minutes)
            => _registry.GetForMinutes(minutes);

        public IReadOnlyList<PcBonusOnlineEntry> GetBonusByVip(int vipLevel)
            => _registry.GetByVip(vipLevel);

        public IEnumerable<PcBonusOnlineEntry> GetAll() => _registry.All;

        /// <summary>
        /// Kiểm tra có thể nhận bonus không (đủ phút + đủ VIP + chưa claim).
        /// </summary>
        public bool CanClaim(int bonusId, int minutes, int vipLevel)
        {
            var e = _registry.Get(bonusId);
            if (e == null) return false;
            if (e.requiredMinutes > minutes) return false;
            if (e.vipRequired > vipLevel) return false;
            if (_claimedBonuses.Contains(bonusId)) return false;
            return true;
        }

        /// <summary>Đánh dấu đã nhận thưởng. Trả về true nếu mới nhận.</summary>
        public bool MarkClaimed(int bonusId)
        {
            var e = _registry.Get(bonusId);
            if (e == null) return false;
            if (!_claimedBonuses.Add(bonusId)) return false;
            SubsystemLog.Info(LogTag, $"Đã nhận thưởng online #{bonusId}");
            OnBonusClaimed?.Invoke(e);
            return true;
        }

        public void ResetClaims() => _claimedBonuses.Clear();
    }
}
