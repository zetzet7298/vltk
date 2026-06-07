// -----------------------------------------------------------------------------
// VLTK Mobile — Inventory Panel Service (Túi đồ)
// UI service: dựng snapshot các ô vật phẩm trong túi đồ nhân vật.
// PC reference: UID 05ea8560 item grid 6×10, money, equipment, consumables.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using VLTK.Sandbox;
using VLTK.Model;

namespace VLTK.UI
{
    /// <summary>Một dòng trong panel túi đồ.</summary>
    public readonly struct InventoryPanelRow
    {
        public readonly int slotIdx;
        public readonly int itemId;
        public readonly int count;
        public readonly int itemGenre;   // 0=Trang bị, 1=Tiêu hao, 2=Nhiệm vụ, 3=Nguyên liệu
        public readonly int itemDetail;
        public readonly int itemParticular;
        public readonly bool isLocked;
        public readonly bool isEquipped;
        public readonly string itemName;
        public readonly int itemQuality; // 0=trắng, 1=xanh, 2=tím, 3=vàng, 4=đỏ

        public InventoryPanelRow(int slotIdx, int itemId, int count, int itemGenre, int itemDetail, int itemParticular, bool isLocked, bool isEquipped, string itemName, int itemQuality)
        {
            this.slotIdx = slotIdx;
            this.itemId = itemId;
            this.count = count;
            this.itemGenre = itemGenre;
            this.itemDetail = itemDetail;
            this.itemParticular = itemParticular;
            this.isLocked = isLocked;
            this.isEquipped = isEquipped;
            this.itemName = itemName ?? string.Empty;
            this.itemQuality = itemQuality;
        }
    }

    /// <summary>Snapshot toàn bộ panel túi đồ.</summary>
    public sealed class InventoryPanelSnapshot
    {
        public int playerId;
        public int totalSlots;
        public int usedSlots;
        public int gold;
        public int silver;
        public IReadOnlyList<InventoryPanelRow> rows;
    }

    /// <summary>Dịch vụ UI: panel túi đồ nhân vật.</summary>
    public static class InventoryPanelService
    {
        public const string Title = "Túi Đồ";
        public const string LabelItem = "Vật phẩm";
        public const string LabelEquip = "Trang bị";
        public const string LabelConsumable = "Tiêu hao";
        public const string LabelQuest = "Nhiệm vụ";
        public const string LabelLocked = "Khóa";
        public const string LabelSort = "Sắp xếp";
        public const int DefaultSlotCount = InventoryWindowPcSpec.SlotCount;

        /// <summary>PC backpack grid (Hành Trang) — 6 columns × 10 rows = 60 slots.</summary>
        public const int GridColumns = InventoryWindowPcSpec.GridColumns;
        public const int GridRows = InventoryWindowPcSpec.GridRows;
        public const int GridSlotCount = InventoryWindowPcSpec.SlotCount;

        private static readonly int[] _defaultSlotOrder = Enumerable.Range(0, DefaultSlotCount).ToArray();

        /// <summary>Thứ tự ô mặc định theo grid PC 6×10.</summary>
        public static IReadOnlyList<int> GetPcInventoryOrder() => _defaultSlotOrder;

        /// <summary>
        /// Map a runtime InventoryService item to its quality tier (PC 7bfc9072.ini).
        /// 0=white,1=blue,2=purple,3=gold/platina,4=red(broken). Derived from refine
        /// level + set membership since the contract bundle has no explicit tier field.
        /// </summary>
        public static int ResolveQuality(ItemDefinition def)
        {
            if (def == null) return 0;
            if (def.setId > 0) return 3;            // set piece -> gold/platina tier
            if (def.refineLevel >= 7) return 2;     // heavily refined -> purple
            if (def.refineLevel >= 1) return 1;     // refined -> blue
            return 0;                               // white
        }

        /// <summary>
        /// Build the PC backpack snapshot (6×10 grid) bound to the live
        /// InventoryService entries. PC behavior: Open([[items]]) lists held items
        /// into the grid; empty trailing slots stay blank.
        /// </summary>
        public static InventoryPanelSnapshot BuildGridSnapshot(InventoryService inventory, int playerId, int gold = 0, int silver = 0)
        {
            var rows = new List<InventoryPanelRow>(GridSlotCount);
            var entries = inventory?.Inventory;
            int used = 0;
            for (int i = 0; i < GridSlotCount; i++)
            {
                if (entries != null && i < entries.Count)
                {
                    var e = entries[i];
                    var def = e.item;
                    int quality = ResolveQuality(def);
                    rows.Add(new InventoryPanelRow(
                        slotIdx: i,
                        itemId: def != null ? def.itemId : 0,
                        count: e.count,
                        itemGenre: 0,
                        itemDetail: 0,
                        itemParticular: 0,
                        isLocked: false,
                        isEquipped: false,
                        itemName: def != null ? def.DisplayName : string.Empty,
                        itemQuality: quality));
                    if (def != null) used++;
                }
                else
                {
                    rows.Add(new InventoryPanelRow(i, 0, 0, 0, 0, 0, false, false, string.Empty, 0));
                }
            }
            return new InventoryPanelSnapshot
            {
                playerId = playerId,
                totalSlots = GridSlotCount,
                usedSlots = used,
                gold = gold,
                silver = silver,
                rows = rows,
            };
        }

        /// <summary>Dựng snapshot dựa trên ItemDatabase và danh sách vật phẩm của player.</summary>
        public static InventoryPanelSnapshot BuildSnapshot(ItemDatabase db, int playerId, int pageIndex = 0)
        {
            var rows = new List<InventoryPanelRow>();
            for (int i = 0; i < DefaultSlotCount; i++)
            {
                rows.Add(new InventoryPanelRow(
                    slotIdx: i + pageIndex * DefaultSlotCount,
                    itemId: 0,
                    count: 0,
                    itemGenre: 0,
                    itemDetail: 0,
                    itemParticular: 0,
                    isLocked: false,
                    isEquipped: false,
                    itemName: string.Empty,
                    itemQuality: 0));
            }
            return new InventoryPanelSnapshot
            {
                playerId = playerId,
                totalSlots = DefaultSlotCount,
                usedSlots = 0,
                gold = 0,
                silver = 0,
                rows = rows,
            };
        }

        /// <summary>Lấy tên vật phẩm từ database.</summary>
        public static string GetItemName(ItemDatabase db, int itemId)
        {
            if (db == null) return string.Empty;
            var def = db.Resolve(itemId);
            return def != null ? (def.DisplayName ?? def.nameRaw ?? string.Empty) : string.Empty;
        }

        /// <summary>Sử dụng vật phẩm: luôn trả về false ở runtime stub (cần kết nối gameplay state).</summary>
        public static bool TryUseItem(int playerId, int slot, int count)
        {
            if (playerId <= 0 || slot < 0 || count <= 0) return false;
            return false;
        }

        /// <summary>Vứt vật phẩm ra đất.</summary>
        public static bool TryDropItem(int playerId, int slot, int count)
        {
            if (playerId <= 0 || slot < 0 || count <= 0) return false;
            return false;
        }

        /// <summary>Mặc vật phẩm vào ô trang bị.</summary>
        public static bool TryEquipItem(int playerId, int slot)
        {
            if (playerId <= 0 || slot < 0) return false;
            return false;
        }

        /// <summary>Tháo vật phẩm khỏi ô trang bị về túi.</summary>
        public static bool TryUnequipItem(int playerId, int slot)
        {
            if (playerId <= 0 || slot < 0) return false;
            return false;
        }

        /// <summary>Sắp xếp vật phẩm trong túi theo chất lượng (cao→thấp), rồi theo tên.</summary>
        public static IReadOnlyList<InventoryPanelRow> SortItems(IReadOnlyList<InventoryPanelRow> rows)
        {
            if (rows == null) return System.Array.Empty<InventoryPanelRow>();
            return rows.OrderByDescending(r => r.itemQuality)
                       .ThenBy(r => r.itemName, System.StringComparer.Ordinal)
                       .ToList();
        }
    }
}
