// -----------------------------------------------------------------------------
// VLTK Mobile — ST-4.5 NPC Shop Item runtime service
// Quản lý vật phẩm bày bán trong 165 shop NPC.
// PC source: settings/npcshopitem.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class NpcShopItemService
    {
        public const string DefaultStreamingDir = "Reference/PcShop";
        public const string LogTag = "NpcShop";

        private readonly PcNpcShopItemRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public NpcShopItemService() { }
        public NpcShopItemService(PcNpcShopItemRegistry registry) { _registry = registry ?? new PcNpcShopItemRegistry(); }

        public static NpcShopItemService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcNpcShopItemParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} vật phẩm shop");
            return new NpcShopItemService(reg);
        }

        public PcNpcShopItemEntry GetItem(int shopNpcId, int slotIdx)
        {
            if (_registry == null) return null;
            foreach (var e in _registry.GetByShop(shopNpcId))
                if (e.slotIdx == slotIdx) return e;
            return null;
        }

        public IReadOnlyList<PcNpcShopItemEntry> GetByShop(int shopNpcId)
            => _registry != null ? _registry.GetByShop(shopNpcId) : Array.Empty<PcNpcShopItemEntry>();
        public IReadOnlyList<PcNpcShopItemEntry> GetByItem(int itemId)
            => _registry != null ? _registry.GetByItem(itemId) : Array.Empty<PcNpcShopItemEntry>();

        public IReadOnlyList<PcNpcShopItemEntry> GetShopItems(int shopNpcId)
            => GetByShop(shopNpcId);

        public bool CanBuy(int shopNpcId, int slotIdx, int playerReputation)
        {
            var e = GetItem(shopNpcId, slotIdx);
            if (e == null) return false;
            return playerReputation >= e.requiredReputation;
        }

        public int GetEffectivePrice(int shopNpcId, int slotIdx, int playerVipLevel)
        {
            var e = GetItem(shopNpcId, slotIdx);
            if (e == null || e.price <= 0) return 0;
            float discount = 1f - Math.Min(0.5f, Math.Max(0f, playerVipLevel) * 0.05f);
            return (int)(e.price * discount);
        }
    }
}
