// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — MovementDto (FS-04C contract)
//
// Pin từ backend FS-04A (commit 54e2055, branch backend/fs04a-movement-endpoint,
// repo vltk-server). Endpoint:
//
//   POST /v1/movement   ↔  KNpc::SetPos (KNpc.cpp:5496) — cập nhật toạ độ runtime.
//
// Body request chỉ mang roleId + posX + posY (KHÔNG có mapId — movement
// không đổi bản đồ, chỉ teleport/reconcile trong scene hiện tại). Server trả
// về SceneResponse (id/roleId/mapId/posX/posY) — parity GetMapPositionAsync
// shape để client reuse code.
//
// Mã lỗi phổ biến (FS-04A evidence):
//   200 → success, data != null
//   404 "..." → role chưa có scene (chưa gọi POST /v1/map/enter)
//   422 → posX/posY âm hoặc thiếu field bắt buộc
//
// Quy tắc field camelCase khớp alias generator (to_camel) của backend
// CamelCaseModel — Unity serialize bằng Newtonsoft mặc định camelCase.
// -----------------------------------------------------------------------------

using System;
using Newtonsoft.Json;

namespace VLTK.Backend.Dto
{
    /// <summary>
    /// Request body cho POST /v1/movement. Cập nhật vị trí runtime của nhân
    /// vật (KHÔNG đổi map). Backend Pydantic có extra="forbid", nên các field
    /// C# phải khớp 1-1; nếu client gửi thừa field, backend sẽ từ chối 422.
    ///
    ///   roleId   : int&gt;=1, role cần update vị trí
    ///   posX     : int&gt;=0, toạ độ X theo grid PC (KNpc.cpp nMpsX, đơn vị ô)
    ///   posY     : int&gt;=0, toạ độ Y theo grid PC (KNpc.cpp nMpsY, đơn vị ô)
    /// </summary>
    [Serializable]
    public sealed class UpdatePositionRequest
    {
        public int roleId;
        public int posX;
        public int posY;

        public UpdatePositionRequest() { }

        public UpdatePositionRequest(int roleId, int posX, int posY)
        {
            this.roleId = roleId;
            this.posX = posX;
            this.posY = posY;
        }

        /// <summary>
        /// Serialize thành JSON camelCase để gửi qua HTTP body. Dùng
        /// Newtonsoft mặc định (đã có trong VLTK.Backend) thay vì JsonUtility
        /// để khớp alias generator phía backend.
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
