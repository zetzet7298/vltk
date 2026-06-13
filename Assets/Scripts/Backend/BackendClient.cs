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
        ///
        /// Lưu ý: KHÔNG tự động apply StreamingAssets override tại đây — để test có
        /// thể truyền config cụ thể mà không bị runtime JSON ghi đè. Caller muốn
        /// dùng override phải gọi <see cref="BackendConfig.ApplyStreamingAssetsOverrideIfPresent"/>
        /// trước khi tạo BackendClient (ví dụ sau khi <see cref="BackendConfig.LoadOrDefault"/>).
        /// </summary>
        public BackendClient(BackendConfig config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Backend = config.useMock
                ? (IGameBackend)new MockGameBackend(config)
                : new RestGameBackend(config, new UnityWebRequestHttpTransport());
        }

        /// <summary>
        /// Khởi tạo với transport chỉ định (dùng cho test inject FakeHttpTransport).
        /// Cũng KHÔNG auto-apply StreamingAssets override (xem ctor 1-arg).
        /// </summary>
        public BackendClient(BackendConfig config, IHttpTransport transport)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            Backend = config.useMock
                ? (IGameBackend)new MockGameBackend(config)
                : new RestGameBackend(config, transport);
        }

        public Task<BackendResponse<HealthResponse>> GetHealthAsync(CancellationToken ct = default)
            => Backend.GetHealthAsync(ct);

        public Task<BackendResponse<MapListResponse>> ListMapsAsync(
            string mapType = null, CancellationToken ct = default)
            => Backend.ListMapsAsync(mapType, ct);

        public Task<BackendResponse<SceneResponse>> EnterMapAsync(
            EnterMapRequest request, CancellationToken ct = default)
            => Backend.EnterMapAsync(request, ct);

        public Task<BackendResponse<SceneResponse>> GetMapPositionAsync(
            int roleId, CancellationToken ct = default)
            => Backend.GetMapPositionAsync(roleId, ct);

        public Task<BackendResponse<ItemListResponse>> ListItemsAsync(
            int roleId, CancellationToken ct = default)
            => Backend.ListItemsAsync(roleId, ct);

        public Task<BackendResponse<DamageCalcResponse>> CalcDamageAsync(
            DamageCalcRequest request, CancellationToken ct = default)
            => Backend.CalcDamageAsync(request, ct);

        public Task<BackendResponse<StatusTickResponse>> StatusTickAsync(
            StatusTickRequest request, CancellationToken ct = default)
            => Backend.StatusTickAsync(request, ct);

        public Task<BackendResponse<PkCheckResponse>> CheckPkAsync(
            PkCheckRequest request, CancellationToken ct = default)
            => Backend.CheckPkAsync(request, ct);
    }
}
