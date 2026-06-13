// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — SceneDto
// Mirror cho payload của POST /v1/map/enter và GET /v1/map/position/{role_id}.
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
    /// Vị trí hiện tại của nhân vật trên bản đồ (cả EnterMap và GetMapPosition
    /// đều trả về cùng shape). Field camelCase khớp alias generator: id, roleId,
    /// mapId, posX, posY.
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
