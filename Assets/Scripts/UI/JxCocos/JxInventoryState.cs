// -----------------------------------------------------------------------------
// VLTK Mobile — JX inventory state (port of KuiItemVN.cpp + KuiItem.cpp + KItem.h)
//
// Nguồn:
//  - KuiItemVN.cpp (1684 L): addDialogData (grid origin 26,54), UpdateData,
//    AddObject (item placement), onTouchItem (click/drag). ITEM_CELL_SIZE=52, space=6.
//  - KuiItem.cpp AddObject: item model + pixel placement + quality color.
//  - KItem.h ITEMGENRE enum (item_equip=0..item_number=7), MAX_ITEM=1024.
//
// Grid placement math (port, Y-flip như cocos):
//   nCurX = m_StartPos.x + GridX*ITEM_CELL_SIZE + Width*ITEM_CELL_SIZE/2
//   nCurY = m_StartPos.y + GridY*ITEM_CELL_SIZE + Height*ITEM_CELL_SIZE/2
//   pixel = (nCurX, m_size.height - nCurY)
//
// Quality color cho equip (KuiEffect nTempColor): 0=normal, 1=purple, 2=gold, 3=platinum.
// Broken equip (Durability 0 hoặc 1) → brokenequip icon.
// Stack label (vàng) cho item stackable KHÔNG phải equip.
//
// Thuần C# (không MonoBehaviour) — EditMode-testable. Grid + swap + collision là
// phần verify được; icon texture (SPR) là asset layer riêng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.UI.JxCocos
{
    /// <summary>ITEMGENRE (nguồn KItem.h). Thứ tự quan trọng.</summary>
    public enum JxItemGenre
    {
        Equip = 0,       // item_equip — trang bị
        Medicine = 1,    // item_medicine — thuốc
        Mine = 2,        // item_mine — khoáng thạch
        Materials = 3,   // item_materials — dược liệu
        Task = 4,        // item_task — nhiệm vụ
        TownPortal = 5,  // item_townportal — châu dẹp
        Fusion = 6,      // item_fusion — phép phù
        Number = 7,      // item_number — đếm số mục
    }

    /// <summary>EQUIPDETAILTYPE (nguồn KItem.h) — vị trí trang bị. Cho panel trang bị.</summary>
    public enum JxEquipSlot
    {
        MeleeWeapon = 0,   // equip_meleeweapon
        RangeWeapon = 1,   // equip_rangeweapon
        Armor = 2,         // equip_armor
        Ring = 3,          // equip_ring
        Amulet = 4,        // equip_amulet
        Boots = 5,         // equip_boots
        Belt = 6,          // equip_belt
        Helm = 7,          // equip_helm
        Cuff = 8,          // equip_cuff
        Pendant = 9,       // equip_pendant
        Horse = 10,        // equip_horse
        Mask = 11,         // equip_mask — mặt nạ
        Pifeng = 12,       // equip_pifeng
        Yinjian = 13,      // equip_yinjian
        Shiping = 14,      // equip_shiping
    }

    /// <summary>Chất lượng trang bị (KuiEffect nTempColor).</summary>
    public enum JxItemQuality
    {
        Normal = 0,    // ccc4(0, 93, 57, 120) — xanh
        Purple = 1,    //紫装 — tím
        Gold = 2,      //黄金 — vàng (GetGoldId>0)
        Platinum = 3,  //白金 — bạch kim (IsPlatina)
    }

    /// <summary>1 item trong túi đồ (KUiDraggedObject + Item[uId] fields).</summary>
    public sealed class JxInventoryItem
    {
        /// <summary>ID item (uId). Nguồn: pObject-&gt;uId. Phải &gt; 0 &amp;&amp; &lt; MAX_ITEM.</summary>
        public int ItemId;
        /// <summary>Genre (uGenre). Nguồn: pObject-&gt;uGenre.</summary>
        public uint Genre;
        /// <summary>Vị trí ô X (DataX). Nguồn: "包袱中的起点X位置 第几个格子".</summary>
        public int GridX;
        /// <summary>Vị trí ô Y (DataY).</summary>
        public int GridY;
        /// <summary>Số ô chiếm ngang (DataW). Mặc định 1.</summary>
        public int Width = 1;
        /// <summary>Số ô chiếm dọc (DataH). Mặc định 1.</summary>
        public int Height = 1;
        /// <summary>Số lượng stack (GetStackNum). Chỉ có ý nghĩa với stackable non-equip.</summary>
        public int Stack;
        /// <summary>Độ bền (GetDurability). 0 hoặc 1 = hỏng → brokenequip icon.</summary>
        public int Durability = -1;
        /// <summary>Đường dẫn SPR icon (GetImagePath). Null/empty → hỏi.spr fallback.</summary>
        public string IconPath = string.Empty;
        /// <summary>Tên item (dịch).</summary>
        public string Name = string.Empty;
        /// <summary>Đã khóa không (getLock).</summary>
        public bool Locked;
        /// <summary>Chi tiết equip (GetEquipDetailType) — cho trang bị.</summary>
        public int EquipDetailType = -1;
        /// <summary>Có thể xếp chồng không (IsStack).</summary>
        public bool Stackable;
        /// <summary>Chất lượng (tính từ goldId/IsPlatina/IsPurple).</summary>
        public JxItemQuality Quality = JxItemQuality.Normal;
    }

    /// <summary>State thuần cho túi đồ grid. Verify được trong EditMode.</summary>
    public sealed class JxInventoryState
    {
        /// <summary>MAX_ITEM (nguồn KItem.h).</summary>
        public const int MaxItem = 1024;

        /// <summary>ITEM_CELL_SIZE (nguồn KuiItemVN.h).</summary>
        public const int CellSize = 52;

        /// <summary>Khoảng cách ô (nguồn: space = 6).</summary>
        public const int CellSpacing = 6;

        /// <summary>Gốc grid X (nguồn: m_StartPos.x = 26).</summary>
        public const float StartX = 26f;

        /// <summary>Gốc grid Y (nguồn: m_StartPos.y = 54).</summary>
        public const float StartY = 54f;

        private readonly int _cols;
        private readonly int _rows;
        private readonly Dictionary<int, JxInventoryItem> _items = new();

        /// <summary>Tạo state với grid cols×rows (mặc định 8×6 — JX bag điển hình).</summary>
        public JxInventoryState(int cols = 8, int rows = 6)
        {
            if (cols <= 0 || rows <= 0)
                throw new ArgumentException("Grid dimensions must be positive");
            _cols = cols;
            _rows = rows;
        }

        public int Columns => _cols;
        public int Rows => _rows;
        public IReadOnlyDictionary<int, JxInventoryItem> Items => _items;

        // ---- Validation (port) ----

        /// <summary>Validation nguồn: uId &gt; 0 &amp;&amp; uId &lt; MAX_ITEM.</summary>
        public static bool IsValidItemId(int itemId) => itemId > 0 && itemId < MaxItem;

        /// <summary>Vị trí ô (gridX, gridY) + kích thước (w,h) có nằm trong grid không?</summary>
        public bool FitsInGrid(int gridX, int gridY, int w, int h)
        {
            if (w <= 0 || h <= 0) return false;
            return gridX >= 0 && gridY >= 0 && gridX + w <= _cols && gridY + h <= _rows;
        }

        /// <summary>
        /// Kiểm tra vùng [gridX..gridX+w, gridY..gridY+h] có bị item khác chiếm không
        /// (loại trừ excludeItemId). Dùng cho placement/swap collision.
        /// </summary>
        public bool IsRegionFree(int gridX, int gridY, int w, int h, int excludeItemId = 0)
        {
            int x2 = gridX + w, y2 = gridY + h;
            foreach (var kv in _items)
            {
                if (kv.Key == excludeItemId) continue;
                var it = kv.Value;
                int ix2 = it.GridX + it.Width, iy2 = it.GridY + it.Height;
                // AABB overlap test.
                if (gridX < ix2 && x2 > it.GridX && gridY < iy2 && y2 > it.GridY)
                    return false;
            }
            return true;
        }

        // ---- Add/Remove (port AddObject) ----

        /// <summary>
        /// AddObject: thêm item vào grid. Validation nguồn (uId hợp lệ) + kiểm
        /// grid fit + collision. Trả về false nếu item sai/trùng/vượt ô/va chạm.
        /// </summary>
        public bool AddItem(JxInventoryItem item)
        {
            if (item == null || !IsValidItemId(item.ItemId)) return false;
            if (_items.ContainsKey(item.ItemId)) return false;
            if (!FitsInGrid(item.GridX, item.GridY, item.Width, item.Height)) return false;
            if (!IsRegionFree(item.GridX, item.GridY, item.Width, item.Height, item.ItemId)) return false;
            _items[item.ItemId] = item;
            return true;
        }

        /// <summary>Xóa item theo ItemId.</summary>
        public bool RemoveItem(int itemId)
        {
            return _items.Remove(itemId);
        }

        public bool TryGetItem(int itemId, out JxInventoryItem item) => _items.TryGetValue(itemId, out item);

        public void Clear() => _items.Clear();

        // ---- Swap / move (port onTouchItem drag) ----

        /// <summary>
        /// Di chuyển item đến ô mới (newGridX, newGridY). Validate fit + collision
        /// (loại trừ chính item). Trả về false nếu không hợp lệ/va chạm.
        /// </summary>
        public bool MoveItem(int itemId, int newGridX, int newGridY)
        {
            if (!_items.TryGetValue(itemId, out var item)) return false;
            if (!FitsInGrid(newGridX, newGridY, item.Width, item.Height)) return false;
            if (!IsRegionFree(newGridX, newGridY, item.Width, item.Height, itemId)) return false;
            item.GridX = newGridX;
            item.GridY = newGridY;
            return true;
        }

        /// <summary>
        /// Swap 2 item vị trí (drag item A lên item B). Chỉ swap được nếu mỗi item
        /// vừa khít ô kia (1×1) HOẶC đổi chỗ không va chạm. Trả về false nếu không
        /// đổi được.
        /// </summary>
        public bool SwapItems(int itemIdA, int itemIdB)
        {
            if (!_items.TryGetValue(itemIdA, out var a)) return false;
            if (!_items.TryGetValue(itemIdB, out var b)) return false;
            if (itemIdA == itemIdB) return true;
            // Kiểm A có đặt vào ô của B không va chạm (loại trừ cả A và B).
            if (!FitsInGrid(b.GridX, b.GridY, a.Width, a.Height)) return false;
            if (!IsRegionFree(b.GridX, b.GridY, a.Width, a.Height, itemIdA == 0 ? 0 : itemIdB)) return false;
            if (!FitsInGrid(a.GridX, a.GridY, b.Width, b.Height)) return false;
            if (!IsRegionFree(a.GridX, a.GridY, b.Width, b.Height, itemIdA)) return false;
            (a.GridX, b.GridX) = (b.GridX, a.GridX);
            (a.GridY, b.GridY) = (b.GridY, a.GridY);
            return true;
        }

        // ---- Pixel placement (port) ----

        /// <summary>
        /// Pixel center của item trong grid (cocos, Y chưa lật). Nguồn AddObject:
        /// nCurX = StartX + GridX*CellSize + Width*CellSize/2;
        /// nCurY = StartY + GridY*CellSize + Height*CellSize/2.
        /// </summary>
        public static (float x, float y) GridToPixelLocal(JxInventoryItem item)
        {
            float x = StartX + item.GridX * CellSize + item.Width * CellSize / 2f;
            float y = StartY + item.GridY * CellSize + item.Height * CellSize / 2f;
            return (x, y);
        }

        /// <summary>
        /// Pixel center Y sau khi lật (cocos Y-up trong parent height).
        /// pixelY = parentHeight - nCurY.
        /// </summary>
        public static (float x, float y) GridToPixelParent(JxInventoryItem item, float parentHeight)
        {
            var (x, y) = GridToPixelLocal(item);
            return (x, parentHeight - y);
        }

        // ---- Quality / broken-equip (port) ----

        /// <summary>
        /// Durability 0 hoặc 1 → item hỏng (nguồn: GetDurability()==0||==1 → brokenequip).
        /// Durability = -1 nghĩa là không có độ bền (non-equip).
        /// </summary>
        public static bool IsBrokenEquip(JxInventoryItem item) =>
            item != null && item.Genre == (uint)JxItemGenre.Equip
            && (item.Durability == 0 || item.Durability == 1);

        /// <summary>
        /// Có hiển thị stack label không? Nguồn: Genre != item_equip &amp;&amp; IsStack().
        /// </summary>
        public static bool ShowStackLabel(JxInventoryItem item) =>
            item != null && item.Genre != (uint)JxItemGenre.Equip && item.Stackable;

        /// <summary>
        /// Icon path hiệu lực, với fallback brokenequip (hỏng) / hỏi.spr (null).
        /// Nguồn: GetImagePath null → hỏi.spr; durability 0/1 → brokenequip.spr.
        /// </summary>
        public static string EffectiveIconPath(JxInventoryItem item)
        {
            if (item == null) return "\\spr\\others\\问号.spr";
            if (IsBrokenEquip(item)) return "\\spr\\item\\equip\\brokenequip.spr";
            return string.IsNullOrEmpty(item.IconPath) ? "\\spr\\others\\问号.spr" : item.IconPath;
        }

        /// <summary>
        /// Tính chất lượng equip từ flag (port nTempColor). Purple ưu tiên thấp hơn
        /// Gold/Platinum theo nguồn (IsPurple set 1, GoldId>0 set 2 hoặc Platina 3).
        /// </summary>
        public static JxItemQuality ComputeEquipQuality(bool isPurple, bool hasGoldId, bool isPlatina)
        {
            if (isPlatina) return JxItemQuality.Platinum;
            if (hasGoldId) return JxItemQuality.Gold;
            if (isPurple) return JxItemQuality.Purple;
            return JxItemQuality.Normal;
        }
    }
}
