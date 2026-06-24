// -----------------------------------------------------------------------------
// VLTK Mobile — JX immedicy box state (port of KuiItemImmediaBoxVN.cpp)
//
// Nguồn: client/Classes/vn/gameui/KuiItemImmediaBoxVN.cpp (331 L).
//  - 3 ô item dùng ngay (BoxIndex 0,1,2), thanh dọc bên phải màn hình.
//  - HoldObject_(nType, nBoxIndex, nameID, isAdd): add/remove item theo ô.
//    Remove: nBoxIndex 0..2 → removeChildByTag. Add với nameID==0 → skip (trống).
//  - Stack count = m_ItemList.GetCountWithNameID(nameID). <=0 → ApplyRemoveItemRef
//    (xóa ref ô — slot trống). Label vàng (Color3B::YELLOW), căn phải.
//  - ImmediaCallback: click ô → dùng item.
//  - Ô 52x52, layout dọc (ô0 trên, spacing ~8px).
//
// Thuần C# (không MonoBehaviour) — EditMode-testable.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.UI.JxCocos
{
    /// <summary>Một ô item dùng ngay (immedicy box slot).</summary>
    public sealed class JxImmedicySlot
    {
        /// <summary>Vị trí ô 0..2.</summary>
        public int BoxIndex;
        /// <summary>ID item (nameID = g_FileName2Id(GetName())). 0 = trống.</summary>
        public ulong NameId;
        /// <summary>Số lượng stack (GetCountWithNameID).</summary>
        public int StackCount;
        /// <summary>Đường dẫn SPR icon.</summary>
        public string IconPath = string.Empty;
        /// <summary>Genre item (uGenre).</summary>
        public uint Genre;
    }

    /// <summary>State thuần cho 3 ô item dùng ngay. Verify được trong EditMode.</summary>
    public sealed class JxImmedicyBoxState
    {
        /// <summary>3 ô (nguồn: BoxIndex 0..2).</summary>
        public const int SlotCount = 3;

        /// <summary>Kích thước ô (nguồn: colorsize 52x52).</summary>
        public const int SlotSize = 52;

        /// <summary>Màu stack count (nguồn: Color3B::YELLOW).</summary>
        public static readonly Color StackColor = Color.yellow;

        private readonly JxImmedicySlot[] _slots = new JxImmedicySlot[SlotCount];

        public JxImmedicyBoxState()
        {
            for (int i = 0; i < SlotCount; i++) _slots[i] = new JxImmedicySlot { BoxIndex = i };
        }

        /// <summary>Truy cập ô theo index [0..2].</summary>
        public JxImmedicySlot Slot(int index)
        {
            if (index < 0 || index >= SlotCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _slots[index];
        }

        /// <summary>Tất cả ô (chỉ-đọc snapshot).</summary>
        public System.Collections.Generic.IReadOnlyList<JxImmedicySlot> Slots => _slots;

        /// <summary>Ô có item không (NameId != 0 &&amp; StackCount &gt; 0)?</summary>
        public static bool IsOccupied(JxImmedicySlot slot) =>
            slot != null && slot.NameId != 0 && slot.StackCount > 0;

        // ---- API port ----

        /// <summary>
        /// HoldObject_(isAdd=true): đặt item vào ô. nameID==0 → no-op (trống).
        /// Trả về false nếu index sai hoặc nameId==0. Stack count đặt riêng qua
        /// SetStackCount (vì nguồn tách lookup GetCountWithNameID).
        /// </summary>
        public bool SetItem(int boxIndex, ulong nameId, string iconPath, uint genre = 0)
        {
            if (boxIndex < 0 || boxIndex >= SlotCount) return false;
            if (nameId == 0) return false;
            _slots[boxIndex].NameId = nameId;
            _slots[boxIndex].IconPath = iconPath ?? string.Empty;
            _slots[boxIndex].Genre = genre;
            return true;
        }

        /// <summary>HoldObject_(isAdd=false): xóa ô.</summary>
        public bool ClearItem(int boxIndex)
        {
            if (boxIndex < 0 || boxIndex >= SlotCount) return false;
            _slots[boxIndex].NameId = 0;
            _slots[boxIndex].StackCount = 0;
            _slots[boxIndex].IconPath = string.Empty;
            _slots[boxIndex].Genre = 0;
            return true;
        }

        /// <summary>
        /// Đặt stack count. &lt;=0 → ApplyRemoveItemRef tương đương (xóa item khỏi ô).
        /// Nguồn: nAllstackCount = GetCountWithNameID(nameID); if (&lt;=0) ApplyRemoveItemRef.
        /// </summary>
        public void SetStackCount(int boxIndex, int count)
        {
            if (boxIndex < 0 || boxIndex >= SlotCount) return;
            if (count <= 0)
            {
                // Stack cạn → xóa item ref (source: ApplyRemoveItemRef).
                ClearItem(boxIndex);
                return;
            }
            _slots[boxIndex].StackCount = count;
        }

        /// <summary>
        /// ImmediaCallback: dùng item ô. Trả về true nếu ô có item &amp; stack&gt;0
        /// (sẵn sàng dùng). Controller nhận lệnh dùng qua command bus (hook riêng).
        /// </summary>
        public bool UseItem(int boxIndex)
        {
            if (boxIndex < 0 || boxIndex >= SlotCount) return false;
            if (!IsOccupied(_slots[boxIndex])) return false;
            // Giảm stack (dùng 1); về 0 → tự clear.
            SetStackCount(boxIndex, _slots[boxIndex].StackCount - 1);
            return true;
        }

        /// <summary>UpdateImmediaItem: cập nhật ô (genre/nameID).</summary>
        public void UpdateItem(int boxIndex, uint genre, ulong nameId)
        {
            if (boxIndex < 0 || boxIndex >= SlotCount) return;
            _slots[boxIndex].Genre = genre;
            if (nameId != 0) _slots[boxIndex].NameId = nameId;
        }

        /// <summary>clearItemData: xóa toàn bộ ô.</summary>
        public void ClearAll()
        {
            for (int i = 0; i < SlotCount; i++) ClearItem(i);
        }
    }
}
