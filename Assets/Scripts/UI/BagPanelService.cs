// -----------------------------------------------------------------------------
// VLTK Mobile — Bag Panel Service (Rương đồ)
// UI service: các ô rương mở rộng, mở khóa rương, đếm slot trống.
// PC reference: túi rương mở rộng, có thể mua thêm rương 2/3/4.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace VLTK.UI
{
    /// <summary>Một dòng trong panel rương.</summary>
    public readonly struct BagPanelRow
    {
        public readonly int bagId;
        public readonly string name;
        public readonly int slots;
        public readonly bool isFull;
        public readonly int itemCount;
        public readonly long lastOpenTimeUnix;

        public BagPanelRow(int bagId, string name, int slots, bool isFull, int itemCount, long lastOpenTimeUnix)
        {
            this.bagId = bagId;
            this.name = name ?? string.Empty;
            this.slots = slots;
            this.isFull = isFull;
            this.itemCount = itemCount;
            this.lastOpenTimeUnix = lastOpenTimeUnix;
        }
    }

    /// <summary>Snapshot toàn bộ panel rương.</summary>
    public sealed class BagPanelSnapshot
    {
        public int playerId;
        public int totalBags;
        public int totalSlots;
        public int usedSlots;
        public IReadOnlyList<BagPanelRow> rows;
    }

    /// <summary>Dịch vụ UI: panel rương đồ.</summary>
    public static class BagPanelService
    {
        public const string Title = "Rương";
        public const string LabelOpen = "Mở rương";
        public const string LabelEmptySlot = "Ô trống";
        public const string LabelFull = "Đã đầy";
        public const string LabelLocked = "Đã khóa";
        public const int DefaultSlotsPerBag = 20;
        public const int MaxBags = 4;

        /// <summary>4 rương mặc định (1 mặc định, 3 mở rộng).</summary>
        private static readonly string[] _bagNames =
        {
            "Rương 1 (mặc định)",
            "Rương 2",
            "Rương 3",
            "Rương 4",
        };

        /// <summary>Dựng snapshot rương cho player.</summary>
        public static BagPanelSnapshot BuildSnapshot(int playerId)
        {
            if (playerId <= 0) playerId = 0;
            var rows = new List<BagPanelRow>();
            for (int i = 0; i < MaxBags; i++)
            {
                rows.Add(new BagPanelRow(
                    bagId: i + 1,
                    name: _bagNames[i],
                    slots: DefaultSlotsPerBag,
                    isFull: false,
                    itemCount: 0,
                    lastOpenTimeUnix: 0));
            }
            return new BagPanelSnapshot
            {
                playerId = playerId,
                totalBags = MaxBags,
                totalSlots = MaxBags * DefaultSlotsPerBag,
                usedSlots = 0,
                rows = rows,
            };
        }

        /// <summary>Lấy thông tin rương theo ID.</summary>
        public static BagPanelRow? GetBag(int bagId)
        {
            if (bagId < 1 || bagId > MaxBags) return null;
            return new BagPanelRow(
                bagId: bagId,
                name: _bagNames[bagId - 1],
                slots: DefaultSlotsPerBag,
                isFull: false,
                itemCount: 0,
                lastOpenTimeUnix: 0);
        }

        /// <summary>Thử mở khóa rương mở rộng (luôn false ở stub — cần kết nối gameplay state).</summary>
        public static bool TryUnlock(int playerId, int bagId)
        {
            if (playerId <= 0 || bagId < 1 || bagId > MaxBags) return false;
            return false;
        }

        /// <summary>Lấy toàn bộ danh sách rương.</summary>
        public static IReadOnlyList<BagPanelRow> GetAllBags(int playerId)
        {
            return BuildSnapshot(playerId).rows;
        }

        /// <summary>Tổng ô trống còn lại của player (giả định chưa dùng ô nào).</summary>
        public static int GetRemainingSlots(int playerId)
        {
            if (playerId <= 0) return 0;
            return MaxBags * DefaultSlotsPerBag;
        }
    }
}
