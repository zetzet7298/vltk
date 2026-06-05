// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.x VIP Level runtime service (12 cấp VIP)
// Quản lý đặc quyền VIP theo cấp nạp.
// PC source: settings/viplevel.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class VipLevelService
    {
        public const string DefaultStreamingDir = "Reference/PcVip";
        public const string LogTag = "VipLevel";

        private readonly PcVipLevelRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public VipLevelService() { }
        public VipLevelService(PcVipLevelRegistry registry) { _registry = registry ?? new PcVipLevelRegistry(); }

        public static VipLevelService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcVipLevelParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} cấp VIP");
            return new VipLevelService(reg);
        }

        public PcVipLevelEntry GetVipLevel(int vipLevel) => _registry != null ? _registry.GetVipLevel(vipLevel) : null;

        public int GetVipForRecharge(long rechargeAmount)
        {
            if (_registry == null) return 0;
            int best = 0;
            foreach (var e in _registry.All)
                if (e != null && rechargeAmount >= e.requiredRecharge && e.vipLevel > best) best = e.vipLevel;
            return best;
        }

        public float GetShopDiscount(int vipLevel)
        {
            var e = GetVipLevel(vipLevel);
            return e?.shopDiscount ?? 0f;
        }

        public int GetDailyGoldBonus(int vipLevel)
        {
            var e = GetVipLevel(vipLevel);
            return e?.dailyGoldBonus ?? 0;
        }

        public int GetDailyExpBonus(int vipLevel)
        {
            var e = GetVipLevel(vipLevel);
            return e?.dailyExpBonus ?? 0;
        }

        public int GetMaxBuyPerDay(int vipLevel)
        {
            var e = GetVipLevel(vipLevel);
            return e?.maxBuyPerDay ?? 0;
        }

        public bool HasMallAccess(int vipLevel)
        {
            var e = GetVipLevel(vipLevel);
            return e?.mallAccess ?? false;
        }
    }
}
