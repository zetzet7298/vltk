// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — Role DTOs (FS-02A contract)
//
// Pin từ backend contract (FS-02A, smoke step 6-8):
//   GET  /v1/role/by-account/{accName}  →  { account, roles: [RoleResponse] }
//   POST /v1/role                       →  { account, roleName, faction }  (FS-02B chưa cần)
//   GET  /v1/role/{role_id}             →  RoleResponse
//
// RoleResponse fields (CamelCaseModel → JSON camelCase):
//   id          (int)        — primary key
//   roleName    (string)     — tên nhân vật, unique toàn server
//   account     (string)     — FK logic đến accounts.acc_name
//   faction     (int)        — -1..9: 0..4 = Kim/Mộc/Thủy/Hỏa/Thổ, 5..9 = bonus
//                              môn phái, -1 = chưa nhập môn phái
//   factionName (string)     — tên tiếng Việt ("Thiếu Lâm", "Võ Đang"…) — UTF-8
//   level       (int)        — level hiện tại (mặc định 1)
//
// Field name = `account` (KHÔNG phải `accName`) cho FK; `accName` chỉ dùng cho
// session id ở login. Hai field khác nhau — đừng nhầm.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Backend.Dto
{
    /// <summary>Request body cho POST /v1/role.</summary>
    [Serializable]
    public sealed class RoleCreateRequest
    {
        public string account;
        public string roleName;
        public int faction = -1;
    }

    /// <summary>Một nhân vật trong account. Field camelCase khớp backend.</summary>
    [Serializable]
    public sealed class RoleResponse
    {
        public int id;
        public string roleName;
        public string account;
        public int faction;
        public string factionName;
        public int level;
    }

    /// <summary>
    /// Phản hồi data từ GET /v1/role/by-account/{accName}.
    /// `account` ở đây là echo lại accName query path; `roles` có thể rỗng.
    /// </summary>
    [Serializable]
    public sealed class RoleListResponse
    {
        public string account;

        /// <summary>Danh sách role. Có thể rỗng cho account mới tạo chưa có role.</summary>
        public List<RoleResponse> roles;
    }
}
