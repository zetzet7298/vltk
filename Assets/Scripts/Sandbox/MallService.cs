// -----------------------------------------------------------------------------
// VLTK Mobile — ST-5.x Mall runtime service
// Wraps PcMallRegistry. PC source: settings/shop/mall.txt.
// Quản lý cửa hàng: giá, giảm giá, tồn kho, giới hạn mua.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Cửa Hàng Mall: kiểm tra điều kiện mua, giá hiệu lực, thời gian khuyến mãi.
    /// </summary>
    public class MallService
    {
        public const string LogTag = "Mall";
        public const string DefaultStreamingDir = "Reference/PcShop";

        public const int CurrencyCoin = 0;       // Đồng
        public const int CurrencyGold = 1;       // Vàng
        public const int CurrencyBind = 2;       // Đồng khóa
        public const int CurrencyVND = 3;        // VND

        private PcMallRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public MallService() { }
        public MallService(PcMallRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcMallRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Mall registry rỗng");
        }

        public PcMallEntry GetMallItem(int id) => _reg != null ? _reg.Get(id) : null;

        public IReadOnlyList<PcMallEntry> GetByItem(int itemId)
            => _reg != null ? _reg.GetByItem(itemId) : Array.Empty<PcMallEntry>();

        public IReadOnlyList<PcMallEntry> GetForVip(int vipLevel)
            => _reg != null ? _reg.GetForVip(vipLevel) : Array.Empty<PcMallEntry>();

        public IReadOnlyList<PcMallEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcMallEntry>();

        public bool CanBuy(int mallItemId, int vipLevel, int alreadyBoughtToday)
        {
            var entry = GetMallItem(mallItemId);
            if (entry == null) return false;
            if (entry.requiredVipLevel > vipLevel) return false;
            if (entry.stock == 0) return false; // hết hàng
            if (entry.maxBuyPerDay > 0 && alreadyBoughtToday >= entry.maxBuyPerDay) return false;
            return true;
        }

        public int GetEffectivePrice(int mallItemId, int vipLevel)
        {
            var entry = GetMallItem(mallItemId);
            if (entry == null) return -1;
            if (entry.requiredVipLevel > vipLevel) return -1;
            if (entry.discount <= 0) return entry.price;
            // discount là phần trăm (0-100); 0 = không giảm
            if (entry.discount >= 100) return 0;
            long price = (long)entry.price * (100 - entry.discount) / 100L;
            return price < 0 ? 0 : (int)price;
        }

        public bool IsOnSale(int mallItemId, long nowUnix)
        {
            var entry = GetMallItem(mallItemId);
            if (entry == null) return false;
            if (entry.startTimeUnix > 0 && nowUnix < entry.startTimeUnix) return false;
            if (entry.endTimeUnix > 0 && nowUnix > entry.endTimeUnix) return false;
            return true;
        }

        public static MallService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcMallParser.BuildRegistry(dir);
            return new MallService(reg);
        }
    }
}
