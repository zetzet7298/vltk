// -----------------------------------------------------------------------------
// VLTK Mobile — UI VIP Panel Service (Bảng cấp VIP)
// Hiển thị quyền lợi VIP: nạp, thưởng hằng ngày, giảm giá shop, mall access.
// Vietnamese: "VIP", "Cấp VIP", "Quyền lợi", "Giảm giá".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct VipPanelRow
    {
        public readonly int vipLevel;
        public readonly long requiredRecharge;       // VND hoặc gold
        public readonly long dailyGoldBonus;
        public readonly long dailyExpBonus;
        public readonly int shopDiscount;            // % (0-100)
        public readonly int maxBuyPerDay;
        public readonly bool mallAccess;
        public readonly bool isCurrent;
        public readonly bool isAchieved;

        public VipPanelRow(int vipLevel, long requiredRecharge, long dailyGoldBonus, long dailyExpBonus,
            int shopDiscount, int maxBuyPerDay, bool mallAccess, bool isCurrent, bool isAchieved)
        {
            this.vipLevel = vipLevel;
            this.requiredRecharge = requiredRecharge;
            this.dailyGoldBonus = dailyGoldBonus;
            this.dailyExpBonus = dailyExpBonus;
            this.shopDiscount = shopDiscount;
            this.maxBuyPerDay = maxBuyPerDay;
            this.mallAccess = mallAccess;
            this.isCurrent = isCurrent;
            this.isAchieved = isAchieved;
        }
    }

    public sealed class VipPanelSnapshot
    {
        public int playerId;
        public int currentVip;
        public long rechargeAmount;
        public int nextVip;
        public long rechargeToNext;
        public IReadOnlyList<VipPanelRow> rows;
    }

    /// <summary>
    /// Panel service VIP — hiển thị cấp, quyền lợi, tính toán nạp còn thiếu.
    /// </summary>
    public static class VipPanelService
    {
        public const int MaxVipLevel = 12;

        public static VipPanelSnapshot BuildSnapshot(VipLevelService svc, int playerId, long rechargeAmount)
        {
            int currentVip = GetCurrentVip(svc, rechargeAmount);
            int nextVip = GetNextVip(svc, currentVip);
            long toNext = ComputeRechargeToNext(svc, currentVip, rechargeAmount);

            var snap = new VipPanelSnapshot
            {
                playerId = playerId,
                currentVip = currentVip,
                rechargeAmount = rechargeAmount,
                nextVip = nextVip,
                rechargeToNext = toNext,
                rows = new List<VipPanelRow>(),
            };

            if (svc == null) return snap;

            try
            {
                var list = new List<VipPanelRow>(MaxVipLevel);
                for (int lvl = 0; lvl < MaxVipLevel; lvl++)
                {
                    var row = new VipPanelRow(
                        vipLevel: lvl,
                        requiredRecharge: 100000L * (lvl + 1) * (lvl + 1),
                        dailyGoldBonus: 1000L * (lvl + 1),
                        dailyExpBonus: 500L * (lvl + 1),
                        shopDiscount: System.Math.Min(50, lvl * 5),
                        maxBuyPerDay: 10 + lvl * 5,
                        mallAccess: lvl >= 3,
                        isCurrent: lvl == currentVip,
                        isAchieved: lvl <= currentVip
                    );
                    list.Add(row);
                }
                snap.rows = list;
            }
            catch { }
            return snap;
        }

        public static int GetCurrentVip(VipLevelService svc, long rechargeAmount)
        {
            if (svc == null) return 0;
            if (rechargeAmount <= 0) return 0;
            int lvl = 0;
            for (int i = 0; i < MaxVipLevel; i++)
            {
                long required = 100000L * (i + 1) * (i + 1);
                if (rechargeAmount >= required) lvl = i;
                else break;
            }
            return lvl;
        }

        public static int GetNextVip(VipLevelService svc, int currentVip)
        {
            if (svc == null) return 0;
            if (currentVip < 0) return 0;
            if (currentVip >= MaxVipLevel - 1) return 0;
            return currentVip + 1;
        }

        public static string GetVipBenefits(VipLevelService svc, int vipLevel)
        {
            if (svc == null) return string.Empty;
            if (vipLevel < 0 || vipLevel >= MaxVipLevel) return string.Empty;
            return $"VIP {vipLevel}: Giảm giá {vipLevel * 5}%, mua tối đa {10 + vipLevel * 5} lần/ngày"
                + (vipLevel >= 3 ? ", truy cập Mall" : "");
        }

        public static long ComputeRechargeToNext(VipLevelService svc, int currentVip, long currentRecharge)
        {
            if (svc == null) return 0;
            if (currentVip >= MaxVipLevel - 1) return 0;
            long required = 100000L * (currentVip + 2) * (currentVip + 2);
            long diff = required - currentRecharge;
            return diff > 0 ? diff : 0;
        }
    }
}
