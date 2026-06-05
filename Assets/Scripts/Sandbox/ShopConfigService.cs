// -----------------------------------------------------------------------------
// VLTK Mobile — ST-5.18 Shop Config Service (Cấu Hình + Mua Bán Cửa Hàng)
// Wraps ShopConfigRegistry. PC source: settings/shops/shop_xxx.txt.
// Vietnamese: "Cửa Hàng", "Mua", "Bán", "Tồn Kho", "Bổ Sung Hàng",
//             "Bạc", "Bạc Khóa", "Đồng", "Danh Vọng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service runtime cấu hình cửa hàng: lookup giá, mua, restock, danh sách.
    /// </summary>
    public class ShopConfigService
    {
        public const string LogTag = "ShopConfig";
        public const string DefaultStreamingDir = "Reference/PcShop";

        private ShopConfigRegistry _registry;
        private readonly Dictionary<int, int> _lastRestockUnix = new();

        public int Count => _registry != null ? _registry.Count : 0;

        public ShopConfigService() { }
        public ShopConfigService(ShopConfigRegistry reg) { _registry = reg; }

        public void AttachRegistry(ShopConfigRegistry reg)
        {
            _registry = reg ?? new ShopConfigRegistry();
            SubsystemLog.Info(LogTag, $"ShopConfig loaded: {Count} entries");
        }

        public ShopConfigEntry GetItem(int shopId, int itemId)
            => _registry != null ? _registry.Get(shopId, itemId) : null;

        public IReadOnlyList<ShopConfigEntry> GetItemsForShop(int shopId)
            => _registry != null
                ? _registry.GetByShop(shopId)
                : (IReadOnlyList<ShopConfigEntry>)System.Array.Empty<ShopConfigEntry>();

        public IReadOnlyList<ShopConfigEntry> GetShopsForItem(int itemId)
            => _registry != null
                ? _registry.GetByItem(itemId)
                : (IReadOnlyList<ShopConfigEntry>)System.Array.Empty<ShopConfigEntry>();

        public IReadOnlyList<ShopConfigEntry> All
            => _registry != null
                ? (IReadOnlyList<ShopConfigEntry>)new List<ShopConfigEntry>(_registry.All)
                : (IReadOnlyList<ShopConfigEntry>)System.Array.Empty<ShopConfigEntry>();

        /// <summary>
        /// Thử mua item: kiểm tra tồn tại + stock + level + fame.
        /// Không trừ tiền (chỉ validate); caller xử lý inventory.
        /// </summary>
        public bool TryBuy(int shopId, int itemId, int count, int playerLevel = 0, int playerFame = 0)
        {
            var entry = GetItem(shopId, itemId);
            if (entry == null) return false;
            if (count <= 0) return false;
            if (entry.requiredLevel > 0 && playerLevel < entry.requiredLevel) return false;
            if (entry.requiredFame > 0 && playerFame < entry.requiredFame) return false;
            if (entry.stock > 0 && entry.stock < count) return false;
            return true;
        }

        /// <summary>
        /// Tính giá tiền phải trả cho count sản phẩm.
        /// </summary>
        public int GetTotalPrice(int shopId, int itemId, int count)
        {
            var entry = GetItem(shopId, itemId);
            if (entry == null) return 0;
            return entry.price * Mathf.Max(1, count);
        }

        /// <summary>
        /// Restock tất cả item trong shop về maxStock.
        /// </summary>
        public int TryRestock(int shopId)
        {
            if (_registry == null) return 0;
            int count = 0;
            var items = _registry.GetByShop(shopId);
            foreach (var e in items)
            {
                if (e.maxStock > 0 && e.stock < e.maxStock)
                {
                    e.stock = e.maxStock;
                    count++;
                }
            }
            _lastRestockUnix[shopId] = (int)System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (count > 0)
                SubsystemLog.Info(LogTag, $"Restock shop {shopId}: {count} item");
            return count;
        }

        /// <summary>
        /// Số giây còn lại cho lần restock tiếp theo của shop.
        /// </summary>
        public int GetRestockTime(int shopId)
        {
            if (_registry == null) return 0;
            int longest = 0;
            var items = _registry.GetByShop(shopId);
            foreach (var e in items)
            {
                if (e.restockSec <= 0) continue;
                if (e.restockSec > longest) longest = e.restockSec;
            }
            return longest;
        }

        public static ShopConfigService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new ShopConfigService();
            if (Directory.Exists(dir))
            {
                var reg = PcShopConfigParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"ShopConfig dir không tồn tại {dir}");
            }
            return svc;
        }
    }
}
