// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — ItemDto
// Mirror cho payload của GET /v1/item/by-role/{role_id}. Backend dùng
// CamelCaseModel nên JSON camelCase: id, roleId, genre, detail, particular,
// level, amount, slot, equipSlot, name (response) / roleId, items (list).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Backend.Dto
{
    /// <summary>
    /// Một vật phẩm trong túi đồ nhân vật. Field camelCase khớp alias của
    /// backend. `equipSlot` mặc định -1 = không trang bị. `name` có thể null
    /// khi backend không resolve được tên hiển thị.
    /// </summary>
    [Serializable]
    public sealed class ItemResponse
    {
        public int id;
        public int roleId;
        public int genre;
        public int detail;
        public int particular;
        public int level;
        public int amount;
        public int slot;
        public int equipSlot;
        public string name;
    }

    /// <summary>
    /// Danh sách vật phẩm của một nhân vật (kèm roleId để caller không phải
    /// truyền lại từ context). Field `items` camelCase khớp backend.
    /// </summary>
    [Serializable]
    public sealed class ItemListResponse
    {
        public int roleId;
        public List<ItemResponse> items;
    }
}
