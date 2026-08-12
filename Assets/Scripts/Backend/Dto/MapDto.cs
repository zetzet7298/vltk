// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — MapListResponse, MapResponse
// Mirror cho payload của GET /v1/map (xem backend/app/modules/map/application/schemas.py).
// Backend dùng CamelCaseModel (to_camel alias) → JSON camelCase. Vì vậy các
// field C# dùng camelCase public field để JsonUtility + Newtonsoft map đúng.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Backend.Dto
{
    /// <summary>
    /// Một bản đồ trong danh mục thế giới (từ maplist.ini). Field camelCase
    /// khớp alias generator của backend: mapId, mapType, mapTypeName, posX,
    /// posY, newWorldScript, newWorldParam.
    /// </summary>
    [Serializable]
    public sealed class MapResponse
    {
        public int mapId;
        public string name;
        public string mapType;
        public string mapTypeName;
        public int posX;
        public int posY;
        public string newWorldScript;
        public string newWorldParam;
    }

    /// <summary>
    /// Danh sách bản đồ (kèm tổng số). Field `total` là số nguyên; `maps` là
    /// danh sách MapResponse.
    /// </summary>
    [Serializable]
    public sealed class MapListResponse
    {
        public int total;

        /// <summary>
        /// Tên field JSON là `maps` (camelCase). Dùng List&lt;MapResponse&gt;
        /// để Newtonsoft.Json + JsonUtility đều map được.
        /// </summary>
        public List<MapResponse> maps;
    }
}
