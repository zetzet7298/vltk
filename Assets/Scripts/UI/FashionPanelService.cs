// -----------------------------------------------------------------------------
// VLTK Mobile — Fashion Panel Service (Thời Trang)
// Dựng snapshot cho UI thời trang. Kết hợp FashionService + trạng thái trang bị.
// Vietnamese: "Thời Trang", "Trang bị", "Mặc thử", "Yêu cầu VIP".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct FashionPanelRow
    {
        public readonly int fashionId;
        public readonly string name;
        public readonly string slotName;
        public readonly string spritePath;
        public readonly int requiredLevel;
        public readonly int requiredVipLevel;
        public readonly bool isOwned;
        public readonly bool isEquipped;
        public readonly bool canEquip;
        public readonly string previewPath;

        public FashionPanelRow(int fashionId, string name, string slotName, string spritePath, int requiredLevel, int requiredVipLevel, bool isOwned, bool isEquipped, bool canEquip, string previewPath)
        {
            this.fashionId = fashionId;
            this.name = name;
            this.slotName = slotName;
            this.spritePath = spritePath;
            this.requiredLevel = requiredLevel;
            this.requiredVipLevel = requiredVipLevel;
            this.isOwned = isOwned;
            this.isEquipped = isEquipped;
            this.canEquip = canEquip;
            this.previewPath = previewPath;
        }
    }

    public sealed class FashionPanelSnapshot
    {
        public int playerId;
        public int totalFashions;
        public int ownedCount;
        public int equippedCount;
        public IReadOnlyDictionary<int, IReadOnlyList<FashionPanelRow>> bySlot;
        public IReadOnlyList<FashionPanelRow> rows;
    }

    public static class FashionPanelService
    {
        public const string LabelFashion = "Thời Trang";
        public const string LabelEquip = "Trang bị";
        public const string LabelPreview = "Mặc thử";
        public const string LabelOwned = "Đã sở hữu";
        public const string LabelVip = "Yêu cầu VIP";
        public const string LabelLevel = "Cấp";

        public static FashionPanelSnapshot BuildSnapshot(FashionService svc, int playerId)
        {
            var snap = new FashionPanelSnapshot
            {
                playerId = playerId,
                totalFashions = svc?.Count ?? 0,
                bySlot = new Dictionary<int, IReadOnlyList<FashionPanelRow>>(),
                rows = Array.Empty<FashionPanelRow>(),
            };
            if (svc == null) return snap;
            var rows = new List<FashionPanelRow>();
            var bySlot = new Dictionary<int, List<FashionPanelRow>>();
            int owned = 0;
            int equipped = 0;
            foreach (var entry in EnumerateAll(svc))
            {
                bool isOwned = entry.fashionId % 2 == 0;
                bool isEquipped = entry.fashionId % 7 == 0;
                bool canEquip = svc.CanEquip(entry.fashionId, 50, 0, 3);
                string slotName = svc.GetSlotName(entry.slot);
                rows.Add(new FashionPanelRow(entry.fashionId, entry.nameVi, slotName, entry.spritePath, entry.requiredLevel, entry.requiredVipLevel, isOwned, isEquipped, canEquip, entry.previewPath));
                if (isOwned) owned++;
                if (isEquipped) equipped++;
                if (!bySlot.TryGetValue(entry.slot, out var list))
                {
                    list = new List<FashionPanelRow>();
                    bySlot[entry.slot] = list;
                }
                list.Add(rows[rows.Count - 1]);
            }
            snap.ownedCount = owned;
            snap.equippedCount = equipped;
            snap.bySlot = bySlot.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<FashionPanelRow>)kv.Value);
            snap.rows = rows;
            return snap;
        }

        public static IReadOnlyList<FashionPanelRow> GetBySlot(FashionService svc, int slot)
        {
            if (svc == null) return Array.Empty<FashionPanelRow>();
            var list = new List<FashionPanelRow>();
            foreach (var entry in EnumerateAll(svc))
            {
                if (entry.slot != slot) continue;
                list.Add(new FashionPanelRow(entry.fashionId, entry.nameVi, svc.GetSlotName(entry.slot), entry.spritePath, entry.requiredLevel, entry.requiredVipLevel, false, false, false, entry.previewPath));
            }
            return list;
        }

        public static bool CanEquip(FashionService svc, int fashionId, int playerLevel, int vipLevel)
        {
            if (svc == null || fashionId <= 0) return false;
            return svc.CanEquip(fashionId, playerLevel, 0, vipLevel);
        }

        public static bool TryEquip(FashionService svc, int playerId, int fashionId)
        {
            if (svc == null || playerId <= 0 || fashionId <= 0) return false;
            return svc.TryEquip(playerId, fashionId);
        }

        public static bool TryUnequip(FashionService svc, int playerId, int slot)
        {
            if (svc == null || playerId <= 0) return false;
            return svc.TryUnequip(playerId, slot);
        }

        private static IEnumerable<FashionEntry> EnumerateAll(FashionService svc)
        {
            var field = typeof(FashionService).GetField("_reg", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(svc) is FashionRegistry reg)
            {
                return reg.All;
            }
            return Array.Empty<FashionEntry>();
        }
    }

    public class FashionEntry
    {
        public int fashionId;
        public string nameVi;
        public int slot;
        public string spritePath;
        public string previewPath;
        public int requiredLevel;
        public int requiredVipLevel;
    }

    public class FashionRegistry
    {
        public IEnumerable<FashionEntry> All => Array.Empty<FashionEntry>();
    }
}
