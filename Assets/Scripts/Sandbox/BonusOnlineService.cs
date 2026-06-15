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
        private IBonusOnlineServiceHost _host;

        /// <summary>Sự kiện khi nhận thưởng online thành công.</summary>
        public event Action<PcBonusOnlineEntry> OnBonusClaimed;

        public int Count => _registry?.Count ?? 0;
        public int ClaimedCount => _claimedBonuses.Count;

        public BonusOnlineService() { }
        public BonusOnlineService(PcBonusOnlineRegistry registry)
        {
            _registry = registry ?? new PcBonusOnlineRegistry();
        }

        public void AttachHost(IBonusOnlineServiceHost host) { _host = host; }

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
        {
            var e = _registry.Get(bonusId);
            if (_host != null)
            {
                if (e != null)
                    _host.OnBonusResolved(e.bonusId, e.requiredMinutes, e.rewardId, e.rewardCount, e.vipRequired);
                else
                    _host.LogBonusEvent("query_missing", bonusId, "Bonus not found in registry");
            }
            return e;
        }

        public IReadOnlyList<PcBonusOnlineEntry> GetBonusForMinutes(int minutes)
        {
            var list = _registry.GetForMinutes(minutes);
            if (_host != null) _host.OnBonusForMinutesQueried(minutes, list.Count);
            return list;
        }

        public IReadOnlyList<PcBonusOnlineEntry> GetBonusByVip(int vipLevel)
        {
            var list = _registry.GetByVip(vipLevel);
            if (_host != null) _host.OnBonusByVipQueried(vipLevel, list.Count);
            return list;
        }

        public IEnumerable<PcBonusOnlineEntry> GetAll()
        {
            int n = 0;
            foreach (var e in _registry.All) n++;
            if (_host != null) _host.OnAllBonusQueried(n);
            return _registry.All;
        }

        /// <summary>
        /// Kiểm tra có thể nhận bonus không (đủ phút + đủ VIP + chưa claim).
        /// </summary>
        public bool CanClaim(int bonusId, int minutes, int vipLevel)
        {
            var e = _registry.Get(bonusId);
            if (e == null)
            {
                if (_host != null) _host.OnCanClaimEvaluated(bonusId, false, minutes, vipLevel);
                return false;
            }
            bool result;
            if (e.requiredMinutes > minutes) result = false;
            else if (e.vipRequired > vipLevel) result = false;
            else if (_claimedBonuses.Contains(bonusId)) result = false;
            else result = true;
            if (_host != null)
            {
                _host.OnCanClaimEvaluated(bonusId, result, minutes, vipLevel);
                _host.LogBonusEvent(result ? "can_claim" : "cannot_claim", bonusId, result ? "ok" : "blocked");
            }
            return result;
        }

        /// <summary>Đánh dấu đã nhận thưởng. Trả về true nếu mới nhận.</summary>
        public bool MarkClaimed(int bonusId)
        {
            var e = _registry.Get(bonusId);
            if (e == null)
            {
                if (_host != null)
                {
                    _host.OnBonusClaimDispatched(bonusId, false, "Bonus not found in registry");
                    _host.LogBonusEvent("claim_missing", bonusId, "Bonus not found");
                }
                return false;
            }
            if (!_claimedBonuses.Add(bonusId))
            {
                if (_host != null)
                {
                    _host.OnBonusClaimDispatched(bonusId, false, "Bonus already claimed");
                    _host.LogBonusEvent("claim_already", bonusId, "Already claimed");
                }
                return false;
            }
            SubsystemLog.Info(LogTag, $"Đã nhận thưởng online #{bonusId}");
            OnBonusClaimed?.Invoke(e);
            if (_host != null)
            {
                _host.OnBonusClaimDispatched(bonusId, true, $"Claimed bonus {bonusId}");
                _host.ShowBonusUI(bonusId, e.requiredMinutes, e.rewardId);
                _host.LogBonusEvent("claim", bonusId, $"Claimed reward {e.rewardId} x{e.rewardCount}");
                _host.PlayBonusSFX("claim", bonusId);
                _host.SaveBonusState(bonusId, e.requiredMinutes, e.vipRequired);
            }
            return true;
        }

        public void ResetClaims()
        {
            _claimedBonuses.Clear();
            if (_host != null) _host.LogBonusEvent("reset", 0, "All bonus claims cleared");
        }

        public void Tick(int currentMinutes, int vipLevel)
        {
            if (_host != null) _host.OnOnlineTick(currentMinutes, vipLevel);
        }
    }
}
