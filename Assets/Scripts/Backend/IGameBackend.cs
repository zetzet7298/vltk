// -----------------------------------------------------------------------------
// VLTK.Backend — IGameBackend
// Hợp đồng cho client giao tiếp với FastAPI game server.
//
// Slice FS-01D gồm hai endpoint smoke (health, list maps).
// Slice FS-02B mở rộng với auth flow:
//   - LoginAsync(account, password)   → LoginResponse  (POST /v1/account/login)
//   - ListRolesAsync(account)         → RoleListResponse (GET /v1/role/by-account/{acc})
//   - GetPlayerStateAsync(roleId)     → PlayerStateResponse (GET /v1/player/by-role/{id})
// Slice FS-02C mở rộng với enter map + position + items:
//   - EnterMapAsync(EnterMapRequest)  → SceneResponse (POST /v1/map/enter)
//   - GetMapPositionAsync(roleId)     → SceneResponse (GET /v1/map/position/{id})
//   - ListItemsAsync(roleId)          → ItemListResponse (GET /v1/item/by-role/{id})
// Slice FS-04B bổ sung movement (KNpc::SetPos parity):
//   - MoveAsync(MoveRequest)          → SceneResponse (POST /v1/movement)
//
// Slice FS-04C mở rộng với movement runtime sync (KNpc::SetPos parity):
//   - UpdatePositionAsync(req)        → SceneResponse (POST /v1/movement)
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
    /// Hợp đồng backend. Mở rộng dần qua các slice:
    ///   FS-01D: GetHealthAsync, ListMapsAsync
    ///   FS-02B: LoginAsync, ListRolesAsync, GetPlayerStateAsync
    ///   FS-02C: EnterMapAsync, GetMapPositionAsync, ListItemsAsync
    ///   FS-03B: ListSkillsAsync, LearnSkillAsync, LevelUpSkillAsync,
    ///           CastSkillCheckAsync, CastSkillAsync
    ///   FS-03C: CalcDamageAsync, StatusTickAsync, CheckPkAsync
    ///   FS-04B: MoveAsync
    /// </summary>
    public interface IGameBackend
    {
        /// <summary>True khi backend đã được cấu hình (config hợp lệ).</summary>
        bool IsConfigured { get; }

        /// <summary>Config đang dùng (cho diagnostics/UI).</summary>
        BackendConfig Config { get; }

        // ---- FS-01D (smoke) ----

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

        // ---- FS-02B (auth → role → player) ----

        /// <summary>
        /// Gọi POST /v1/account/login với body JSON
        /// <c>{accName, password, otp?, clientIp?}</c>. Password gửi PLAINTEXT —
        /// KHÔNG hash trước (server tự hash MD5-IN-HOA để so sánh).
        ///
        /// Khi thành công: data.accName là session id dùng cho các call sau
        /// (ListRolesAsync, GetPlayerStateAsync). KHÔNG có bearer/JWT trong FS-02.
        ///
        /// Mã lỗi phổ biến:
        ///   200 → success, data != null
        ///   401 → sai tên HOẶC sai mật khẩu (cùng message, không lộ tồn tại)
        ///   403 → account bị banned
        ///   422 → body thiếu field bắt buộc hoặc có field lạ
        ///   429 → vượt LimitAccountPerIP
        ///   501 → account bật OTP mà OTP engine chưa cấu hình
        /// </summary>
        Task<BackendResponse<LoginResponse>> LoginAsync(
            string accName,
            string password,
            string otp = null,
            string clientIp = null,
            CancellationToken ct = default);

        /// <summary>
        /// Gọi GET /v1/role/by-account/{accName}. Trả về danh sách nhân vật
        /// (roles[]) thuộc account. Có thể rỗng cho account mới chưa tạo role.
        /// </summary>
        Task<BackendResponse<RoleListResponse>> ListRolesAsync(
            string accName, CancellationToken ct = default);

        /// <summary>
        /// Gọi GET /v1/player/by-role/{roleId}. Trả về trạng thái nhân vật
        /// (level, exp, chỉ số, tiền, danh vọng). 404 nếu role chưa có player
        /// state (chưa gọi POST /v1/player).
        /// </summary>
        Task<BackendResponse<PlayerStateResponse>> GetPlayerStateAsync(
            int roleId, CancellationToken ct = default);

        // ---- FS-02C (enter map + position + items) ----

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

        // ---- FS-04B (movement — runtime position update) ----

        /// <summary>
        /// Gọi POST /v1/movement với body JSON <c>{roleId, posX, posY}</c>.
        /// Server cập nhật toạ độ runtime của nhân vật (KNpc::SetPos parity —
        /// KNpc.cpp:5496) mà KHÔNG đổi mapId. Khác EnterMapAsync ở chỗ
        /// movement không mang mapId: nếu role chưa có scene (chưa gọi
        /// EnterMapAsync), server trả 404 vì không thể tự tạo scene mới.
        ///
        /// Mã lỗi phổ biến:
        ///   200 → success, data chứa vị trí mới (giữ nguyên mapId cũ)
        ///   404 → role chưa có scene (chưa enter_map)
        ///   422 → body thiếu field hoặc vi phạm ràng buộc
        ///         (roleId&lt;1, posX/posY&lt;0)
        /// </summary>
        Task<BackendResponse<SceneResponse>> MoveAsync(
            MoveRequest request, CancellationToken ct = default);

        // ---- FS-03B (skill read + cast) ----

        /// <summary>
        /// Gọi GET /v1/skill/by-role/{roleId}. Trả về danh sách skill đã học
        /// của role (mảng rỗng nếu roleId chưa học skill nào).
        /// </summary>
        Task<BackendResponse<PlayerSkillListResponse>> ListSkillsAsync(
            int roleId, CancellationToken ct = default);

        /// <summary>
        /// Gọi POST /v1/skill/learn với body JSON
        /// <c>{roleId, skillId, charLevel, faction}</c>. Mã lỗi phổ biến:
        ///   404 "Kỹ năng không có trong bảng định nghĩa" — skillId lạ
        ///   409 "Nhân vật đã học kỹ năng này" — duplicate
        ///   422 "Chưa đủ cấp độ yêu cầu" — charLevel &lt; template.req_level
        /// </summary>
        Task<BackendResponse<PlayerSkillResponse>> LearnSkillAsync(
            SkillLearnRequest req, CancellationToken ct = default);

        /// <summary>
        /// Gọi POST /v1/skill/by-role/{roleId}/level-up/{skillId}. Nâng cấp
        /// skill đã học (+1 level). Mã lỗi phổ biến:
        ///   404 "Nhân vật chưa học kỹ năng này"
        ///   422 "Kỹ năng đã đạt cấp tối đa"
        /// </summary>
        Task<BackendResponse<PlayerSkillResponse>> LevelUpSkillAsync(
            int roleId, int skillId, CancellationToken ct = default);

        /// <summary>
        /// Gọi POST /v1/skill/cast/check (STATELESS pre-flight). Server KHÔNG
        /// đụng DB — dùng current* + gate fields từ client để validate. Dùng
        /// trước khi gọi CastSkillAsync để UI gate mượt, nhưng vẫn phải
        /// reconcile với /cast vì server-authoritative lấy resource/cooldown
        /// thật từ DB (parity FS-03A contract §4.1, H-SK2/H-SK3).
        /// </summary>
        Task<BackendResponse<SkillCastCheckResponse>> CastSkillCheckAsync(
            SkillCastCheckRequest req, CancellationToken ct = default);

        /// <summary>
        /// Gọi POST /v1/skill/cast (SERVER-AUTHORITATIVE). Server đọc
        /// currentMana/Life/Stamina + last_cast_ms từ DB, KHÔNG nhận từ client
        /// (chống spoof). Client gửi gate context (onHorse/relation/distance/
        /// weaponType/equipState/nowMs) + skillId. Response chứa currentLife/
        /// Mana/Stamina SAU cast + effects[] nội suy — client PHẢI dùng số
        /// server trả, không tự tính (parity FS-03A contract §5 "Predict-reconcile").
        /// </summary>
        Task<BackendResponse<SkillCastResponse>> CastSkillAsync(
            SkillCastRequest req, CancellationToken ct = default);

        // ---- FS-03C (combat: damage calc + status tick + pk check) ----

        /// <summary>Gọi POST /v1/combat/damage/calc (SERVER-AUTHORITATIVE).</summary>
        Task<BackendResponse<DamageCalcResponse>> CalcDamageAsync(
            DamageCalcRequest request, CancellationToken ct = default);

        /// <summary>Gọi POST /v1/combat/status/tick — tiến 1 frame trạng thái.</summary>
        Task<BackendResponse<StatusTickResponse>> StatusTickAsync(
            StatusTickRequest request, CancellationToken ct = default);

        /// <summary>Gọi POST /v1/combat/pk/check — kiểm tra PK hợp lệ.</summary>
        Task<BackendResponse<PkCheckResponse>> CheckPkAsync(
            PkCheckRequest request, CancellationToken ct = default);

        // ---- FS-04C (movement runtime sync) ----

        /// <summary>
        /// Gọi POST /v1/movement với body JSON <c>{roleId, posX, posY}</c>.
        /// Server cập nhật toạ độ runtime của nhân vật (parity KNpc::SetPos
        /// trong KNpc.cpp:5496) và trả về SceneResponse với vị trí SAU
        /// update. KHÔNG đổi mapId — movement chỉ reconcile trong scene hiện
        /// tại.
        ///
        /// Mã lỗi phổ biến (FS-04A evidence):
        ///   200 → success, data != null
        ///   404 → role chưa có scene (chưa gọi POST /v1/map/enter)
        ///   422 → posX/posY âm hoặc thiếu field bắt buộc
        /// </summary>
        Task<BackendResponse<SceneResponse>> UpdatePositionAsync(
            UpdatePositionRequest request, CancellationToken ct = default);
    }
}
