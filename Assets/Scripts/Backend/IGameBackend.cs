// -----------------------------------------------------------------------------
// VLTK.Backend — IGameBackend
// Hợp đồng cho client giao tiếp với FastAPI game server.
//
// Slice FS-01D gồm hai endpoint smoke (health, list maps).
// Slice FS-02B mở rộng với auth flow:
//   - LoginAsync(account, password)   → LoginResponse  (POST /v1/account/login)
//   - ListRolesAsync(account)         → RoleListResponse (GET /v1/role/by-account/{acc})
//   - GetPlayerStateAsync(roleId)     → PlayerStateResponse (GET /v1/player/by-role/{id})
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
    ///   (FS-02C+ sẽ bổ sung: CreateRoleAsync, AddExpAsync, TransLifeAsync, …)
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
    }
}
