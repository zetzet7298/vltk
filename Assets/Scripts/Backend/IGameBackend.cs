// -----------------------------------------------------------------------------
// VLTK.Backend — IGameBackend
// Hợp đồng cho client giao tiếp với FastAPI game server.
//
// Slice FS-01D (smoke):
//   - GET  /health              (root, không thuộc /v1)
//   - GET  /v1/map              (danh mục bản đồ thế giới)
//
// Slice FS-02B (auth):
//   - POST /v1/account/login
//   - GET  /v1/role/by-account/{account_id}
//   - GET  /v1/player/{role_id}
//
// Slice FS-02C (expand):
//   - POST /v1/map/enter        (vào/đổi bản đồ, body=EnterMapRequest)
//   - GET  /v1/map/position/{id} (lấy vị trí nhân vật)
//   - GET  /v1/item/by-role/{id} (liệt kê túi đồ)
//
// Slice FS-03C (combat — server-authoritative):
//   - POST /v1/combat/damage/calc  (KNpc::CalcDamage parity — server mutate target)
//   - POST /v1/combat/status/tick  (KNpc::ProcessState parity — 1 frame server tick)
//   - POST /v1/combat/pk/check     (PK hợp lệ server-side; client KHÔNG tự quyết)
//
// Mọi method đều trả về BackendResponse<T> để khi backend trả 4xx/5xx với body
// JSON hợp lệ, caller vẫn nhận được code/message thay vì exception.
// -----------------------------------------------------------------------------

using System.Threading;
using System.Threading.Tasks;
using VLTK.Backend.Dto;

namespace VLTK.Backend
{
    /// <summary>
    /// Hợp đồng backend tối thiểu cho FS-01D. Mở rộng dần trong các slice sau.
    /// </summary>
    public interface IGameBackend
    {
        /// <summary>True khi backend đã được cấu hình (config hợp lệ).</summary>
        bool IsConfigured { get; }

        /// <summary>Config đang dùng (cho diagnostics/UI).</summary>
        BackendConfig Config { get; }

        /// <summary>
        /// Gọi GET /health. Trả về dict thuần từ backend (KHÔNG bọc trong
        /// DataResponse), nên trả về BackendResponse&lt;HealthResponse&gt; với
        /// IsSuccess dựa trên HTTP 2xx + parse JSON thành công.
        /// </summary>
        Task<BackendResponse<HealthResponse>> GetHealthAsync(CancellationToken ct = default);

        /// <summary>
        /// Gọi GET /v1/map (tùy chọn lọc theo map_type). Trả về
        /// BackendResponse&lt;MapListResponse&gt; với envelope code/message.
        /// </summary>
        Task<BackendResponse<MapListResponse>> ListMapsAsync(
            string mapType = null, CancellationToken ct = default);

        /// <summary>
        /// Gọi POST /v1/map/enter (body=EnterMapRequest). Server đổi nhân vật
        /// sang bản đồ mới và trả về SceneResponse (id/roleId/mapId/posX/posY).
        /// </summary>
        Task<BackendResponse<SceneResponse>> EnterMapAsync(
            EnterMapRequest request, CancellationToken ct = default);

        /// <summary>
        /// Gọi GET /v1/map/position/{roleId}. Trả về SceneResponse vị trí hiện
        /// tại của nhân vật; null/empty roleId → trả Failure("invalid_arg").
        /// </summary>
        Task<BackendResponse<SceneResponse>> GetMapPositionAsync(
            int roleId, CancellationToken ct = default);

        /// <summary>
        /// Gọi GET /v1/item/by-role/{roleId}. Trả về ItemListResponse (roleId +
        /// danh sách ItemResponse). roleId phải &gt; 0; nếu không → Failure.
        /// </summary>
        Task<BackendResponse<ItemListResponse>> ListItemsAsync(
            int roleId, CancellationToken ct = default);

        // ----------------------------------------------------------------
        // FS-03C — Combat (server-authoritative)
        // ----------------------------------------------------------------

        /// <summary>
        /// Gọi POST /v1/combat/damage/calc (body=DamageCalcRequest). Server
        /// là NGUỒN CHÂN LÝ DUY NHẤT cho damage — KHÔNG tự tính local.
        /// Server MUTATE target tại chỗ, trả về damage + state sau.
        /// Caller dùng <see cref="ServerAuthorityEnforcer.ApplyServerState"/>
        /// để thay thế state local bằng state server trả.
        /// </summary>
        Task<BackendResponse<DamageCalcResponse>> CalcDamageAsync(
            DamageCalcRequest request, CancellationToken ct = default);

        /// <summary>
        /// Gọi POST /v1/combat/status/tick (body=StatusTickRequest). Server
        /// tiến 1 frame ProcessState (parity KNpc.cpp:612-863), mutate target
        /// + status tại chỗ, trả về control flags + dotResults + state sau.
        /// Client KHÔNG tự tick status local.
        /// </summary>
        Task<BackendResponse<StatusTickResponse>> StatusTickAsync(
            StatusTickRequest request, CancellationToken ct = default);

        /// <summary>
        /// Gọi POST /v1/combat/pk/check (body=PkCheckRequest). Server quyết
        /// định có được phép đánh player khác hay không (vùng an toàn / khác
        /// phe / battle). Client LUÔN phải gọi trước khi damage.
        /// </summary>
        Task<BackendResponse<PkCheckResponse>> CheckPkAsync(
            PkCheckRequest request, CancellationToken ct = default);
    }
}
