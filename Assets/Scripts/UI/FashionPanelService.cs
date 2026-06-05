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
            return new FashionPanelSnapshot { rows = System.Array.Empty<FashionPanelRow>(), bySlot = new System.Collections.Generic.Dictionary<int, IReadOnlyList<FashionPanelRow>>() };
        }

        public static IReadOnlyList<FashionPanelRow> GetBySlot(FashionService svc, int slot)
        {
            return System.Array.Empty<FashionPanelRow>();
        }

        public static bool CanEquip(FashionService svc, int fashionId, int playerLevel, int vipLevel)
        {
            return false;
        }

        public static bool TryEquip(FashionService svc, int playerId, int fashionId)
        {
            return false;
        }

        public static bool TryUnequip(FashionService svc, int playerId, int slot)
        {
            return false;
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
