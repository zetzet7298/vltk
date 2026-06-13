// -----------------------------------------------------------------------------
// VLTK.Backend — BackendClient
// Facade đơn giản: chọn MockGameBackend hoặc RestGameBackend theo Config.useMock.
// Caller (SandboxManager hoặc UI) chỉ cần khởi tạo BackendClient một lần và
/// gọi các method trên đó — không phải biết backend đang ở chế độ nào.
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;
using VLTK.Backend.Rest;

namespace VLTK.Backend
{
    /// <summary>
    /// Facade cho IGameBackend. Quyết định mock vs rest tại thời điểm khởi tạo.
    /// </summary>
    public sealed class BackendClient
    {
        public IGameBackend Backend { get; }
        public BackendConfig Config => Backend.Config;
        public bool IsMock => Backend is MockGameBackend;

        /// <summary>
        /// Khởi tạo BackendClient. Nếu config.useMock=true, dùng MockGameBackend;
        /// ngược lại dùng RestGameBackend với UnityWebRequestHttpTransport.
        /// </summary>
        public BackendClient(BackendConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            config.ApplyStreamingAssetsOverrideIfPresent();
            Backend = config.useMock
                ? (IGameBackend)new MockGameBackend(config)
                : new RestGameBackend(config, new UnityWebRequestHttpTransport());
        }

        /// <summary>
        /// Khởi tạo với transport chỉ định (dùng cho test inject FakeHttpTransport).
        /// </summary>
        public BackendClient(BackendConfig config, IHttpTransport transport)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            config.ApplyStreamingAssetsOverrideIfPresent();
            Backend = config.useMock
                ? (IGameBackend)new MockGameBackend(config)
                : new RestGameBackend(config, transport);
        }

        public Task<BackendResponse<HealthResponse>> GetHealthAsync(CancellationToken ct = default)
            => Backend.GetHealthAsync(ct);

        public Task<BackendResponse<MapListResponse>> ListMapsAsync(
            string mapType = null, CancellationToken ct = default)
            => Backend.ListMapsAsync(mapType, ct);
    }
}
