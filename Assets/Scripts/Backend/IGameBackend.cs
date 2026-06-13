// -----------------------------------------------------------------------------
// VLTK.Backend — IGameBackend
// Hợp đồng cho client giao tiếp với FastAPI game server. Slice tối thiểu FS-01D
// gồm hai endpoint smoke:
//   - GET /health  (root, không thuộc /v1)
//   - GET /v1/map  (danh mục bản đồ thế giới)
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
    /// Hợp đồng backend tối thiểu cho FS-01D. Mở rộng dần trong các slice sau
    /// (login, role list, player state, item list, skill cast).
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
    }
}
