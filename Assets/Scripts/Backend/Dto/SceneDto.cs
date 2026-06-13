// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — SceneDto
// Mirror cho payload của các endpoint map/position/movement:
//   POST /v1/map/enter   → EnterMapRequest  (roleId, mapId, posX, posY)
//   GET  /v1/map/position/{role_id} → SceneResponse
//   POST /v1/movement    → MoveRequest      (roleId, posX, posY — KHÔNG có mapId)
//   Cả 3 endpoint đều trả về SceneResponse.
//
// Backend (map/application/schemas.py) dùng CamelCaseModel, nên JSON trả về
// camelCase: id, roleId, mapId, posX, posY (response) / roleId, mapId, posX,
// posY (request). Field C# dùng camelCase để JsonUtility + Newtonsoft map đúng.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Backend.Dto
{
    /// <summary>
    /// Yêu cầu vào/đổi bản đồ (POST /v1/map/enter). Backend Pydantic có
    /// extra="forbid", nên các field C# phải khớp 1-1; nếu client gửi thừa
    /// field, backend sẽ từ chối 422.
    /// </summary>
    [Serializable]
    public sealed class EnterMapRequest
    {
        public int roleId;
        public int mapId;
        public int posX;
        public int posY;
    }

    /// <summary>
    /// Yêu cầu cập nhật toạ độ runtime mà KHÔNG đổi bản đồ
    /// (POST /v1/movement, parity PC KNpc::SetPos nX nY — KNpc.cpp:5496).
    ///
    /// Khác EnterMapRequest ở chỗ không mang mapId: PC server chỉ ghi lại
    /// m_MapX/m_MapY khi nhân vật di chuyển trong cùng bản đồ. Backend sẽ
    /// báo 404 nếu role chưa có scene (chưa gọi enter_map) vì movement không
    /// thể tự tạo scene mới.
    ///
    /// Backend Pydantic (MovementRequest) yêu cầu:
    ///   roleId: int  (ge=1)
    ///   posX:   int  (ge=0, default 0)
    ///   posY:   int  (ge=0, default 0)
    /// </summary>
    [Serializable]
    public sealed class MoveRequest
    {
        public int roleId;
        public int posX;
        public int posY;
    }

    /// <summary>
    /// Vị trí hiện tại của nhân vật trên bản đồ (EnterMap, GetMapPosition và
    /// Move đều trả về cùng shape). Field camelCase khớp alias generator: id,
    /// roleId, mapId, posX, posY.
    /// </summary>
    [Serializable]
    public sealed class SceneResponse
    {
        public int id;
        public int roleId;
        public int mapId;
        public int posX;
        public int posY;
    }
}
