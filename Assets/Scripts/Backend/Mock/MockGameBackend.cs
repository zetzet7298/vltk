// -----------------------------------------------------------------------------
// VLTK.Backend.Mock — MockGameBackend
// Implementation mock (offline) của IGameBackend. KHÔNG import
// UnityEngine.Networking, KHÔNG gọi network. Mọi call trả về canned response
// với IsSuccess=true và data hợp lệ. Dùng cho:
//   - Runtime offline (SandboxManager không cần thay đổi khi useMock=true)
//   - EditMode test không cần server thật
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VLTK.Backend.Dto;

namespace VLTK.Backend.Mock
{
    /// <summary>
    /// Mock backend với dữ liệu cứng cho smoke test. Mọi Task trả về trạng thái
    /// thành công, code="200", message="Mock" để phân biệt với response thật.
    /// </summary>
    public sealed class MockGameBackend : IGameBackend
    {
        public BackendConfig Config { get; }
        public bool IsConfigured => Config != null;

        public MockGameBackend(BackendConfig config)
        {
            Config = config != null ? config : throw new ArgumentNullException(nameof(config));
        }

        public Task<BackendResponse<HealthResponse>> GetHealthAsync(CancellationToken ct = default)
        {
            var data = new HealthResponse
            {
                status = "ok",
                service = "mock",
                version = "0.0.0-mock",
                timestamp = DateTime.UtcNow.ToString("o"),
            };
            return Task.FromResult(new BackendResponse<HealthResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<MapListResponse>> ListMapsAsync(
            string mapType = null, CancellationToken ct = default)
        {
            var maps = new List<MapResponse>
            {
                new MapResponse
                {
                    mapId = 1,
                    name = "Phượng Tường Cổ Thành",
                    mapType = "city",
                    mapTypeName = "Thành phố",
                    posX = 1500,
                    posY = 1500,
                    newWorldScript = "NewWorld",
                    newWorldParam = "1 1500 1500",
                },
                new MapResponse
                {
                    mapId = 2,
                    name = "Tân Thủ Thôn",
                    mapType = "village",
                    mapTypeName = "Làng",
                    posX = 1800,
                    posY = 1200,
                    newWorldScript = "NewWorld",
                    newWorldParam = "2 1800 1200",
                },
            };
            if (!string.IsNullOrEmpty(mapType))
            {
                maps.RemoveAll(m => !string.Equals(m.mapType, mapType, StringComparison.OrdinalIgnoreCase));
            }
            var data = new MapListResponse
            {
                total = maps.Count,
                maps = maps,
            };
            return Task.FromResult(new BackendResponse<MapListResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }
    }
}
