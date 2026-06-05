// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Goods Catalog Service (Danh mục vật phẩm bán)
// Wraps PcGoodsRegistry. PC source: settings/goods.txt (1,521 entries).
// Shop link is by itemId in the shop's slot list (buysell.txt cross-ref).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý danh mục Vật Phẩm Bán (Cửa Hàng, Hàng Hóa).
    /// PC source: settings/goods.txt — 1,521 vật phẩm NPC bán.
    /// </summary>
    public class GoodsCatalogService
    {
        public const string LogTag = "GoodsCatalog";

        private PcGoodsRegistry _registry;
        private readonly Dictionary<int, int> _shopGoodsByShop = new();
        private readonly List<PcGoodsEntry> _allGoods = new();

        public event Action<int> OnGoodsLoaded; // (goodsCount)

        public int Count => _registry != null ? _registry.Count : 0;

        public GoodsCatalogService() : this(null) { }

        public GoodsCatalogService(PcGoodsRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcGoodsRegistry registry)
        {
            _registry = registry;
            _allGoods.Clear();
            _shopGoodsByShop.Clear();
            if (_registry == null) return;
            foreach (var e in _registry.All())
            {
                if (e == null) continue;
                _allGoods.Add(e);
                // 1-1 mapping: good.id ↔ shop slot. Aggregate shopId from entry fields.
                int shopId = e.itemGenre * 1000 + e.detailType;
                if (!_shopGoodsByShop.ContainsKey(shopId))
                    _shopGoodsByShop[shopId] = 0;
                _shopGoodsByShop[shopId] = _shopGoodsByShop[shopId] + 1;
            }
            SubsystemLog.Info(LogTag,
                $"Vật Phẩm Bán loaded: {_allGoods.Count} Hàng Hóa, {_shopGoodsByShop.Count} Cửa Hàng");
            OnGoodsLoaded?.Invoke(_allGoods.Count);
        }

        public PcGoodsEntry GetGood(int goodId)
            => _registry != null ? _registry.Get(goodId) : null;

        public IEnumerable<PcGoodsEntry> GetAllGoods() => _allGoods;

        /// <summary>Lọc vật phẩm theo shopId (heuristic theo genre/detail).</summary>
        public List<PcGoodsEntry> GetGoodsForShop(int shopId)
        {
            int shopGenre = shopId / 1000;
            int shopDetail = shopId % 1000;
            var result = new List<PcGoodsEntry>();
            foreach (var e in _allGoods)
            {
                if (e == null) continue;
                if (e.itemGenre == shopGenre && e.detailType == shopDetail)
                    result.Add(e);
            }
            return result;
        }

        public int GetShopGoodsCount(int shopId)
            => _shopGoodsByShop.TryGetValue(shopId, out var n) ? n : 0;

        /// <summary>Load từ StreamingAssets/Reference/PcShop/goods.txt.</summary>
        public static GoodsCatalogService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcShop");
            var reg = PcGoodsParser.BuildRegistry(root);
            return new GoodsCatalogService(reg);
        }
    }
}
