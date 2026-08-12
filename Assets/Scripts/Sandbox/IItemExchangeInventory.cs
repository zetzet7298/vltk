// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Item Exchange Inventory Interface
// Abstraction cho InventoryService mutation khi apply ItemExchangePlan.
// PC source: Server 6.0/script/misc/itemexchangevalue/itemexchangevalue.lua
// + Server 6.0/server/.../itemexchange_setting/{normal,rare,level_exp,rolevalue}.*
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side abstraction để apply <see cref="ItemExchangePlan.Commands"/>.
    /// Implementations: real <c>InventoryService</c>, in-memory fake cho tests/GM preview.
    /// Mỗi method tương ứng 1 ApiName trong <see cref="ItemExchangeHostCommand"/>.
    /// </summary>
    public interface IItemExchangeInventory
    {
        /// <summary>HasItem theo bag index.</summary>
        bool HasItem(int itemIndex, int count = 1);

        /// <summary>RemoveItemByIndex: trừ item theo index. Trả false nếu thiếu.</summary>
        bool TakeItem(int itemIndex, int count = 1);

        /// <summary>
        /// AddItem/AddItemEx/AddGoldItem: tạo item (genre/detail/particular) với
        /// level + count + magic level. Trả false nếu túi đầy hoặc không tạo được.
        /// </summary>
        bool GiveItem(int genre, int detail, int particular, int level, int count, int magicLevel = 0);

        /// <summary>GiveGold: cộng tiền vàng.</summary>
        bool GiveGold(int amount);

        /// <summary>FreeBagCells: số ô trống còn lại trong túi.</summary>
        int FreeBagCells();

        /// <summary>WriteLog: ghi log server-side (PC WriteLog lua function).</summary>
        void WriteLog(string message);

        // --- Jinglian extras (PC PutIn/SyncItem/SetItemMagicLevel/SetItemBindState) ---

        /// <summary>ConsumeItem: trừ số lượng item tại index (jinglian energy).</summary>
        bool ConsumeItem(int itemIndex, int count);

        /// <summary>SetItemMagicLevel: set cấp độ ma thuật cho item tại index.</summary>
        bool SetItemMagicLevel(int itemIndex, int newMagicLevel);

        /// <summary>SyncItem: đồng bộ dữ liệu item (server-side sync message).</summary>
        bool SyncItem(int itemIndex);

        /// <summary>SetItemBindState: thay đổi trạng thái khóa của item.</summary>
        bool SetItemBindState(int itemIndex, int bindState);
    }
}
