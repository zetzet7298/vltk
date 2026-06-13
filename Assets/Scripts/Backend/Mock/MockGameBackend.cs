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

        public Task<BackendResponse<SceneResponse>> EnterMapAsync(
            EnterMapRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", "EnterMapRequest is null"));
            }
            // Mock: trả về SceneResponse với id giả lập (hash từ roleId+mapId để
            // ổn định giữa các call). posX/posY echo từ request để caller thấy
            // dữ liệu đã được server "xác nhận".
            int fakeId = unchecked(request.roleId * 1000 + request.mapId);
            var data = new SceneResponse
            {
                id = fakeId,
                roleId = request.roleId,
                mapId = request.mapId,
                posX = request.posX,
                posY = request.posY,
            };
            return Task.FromResult(new BackendResponse<SceneResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<SceneResponse>> GetMapPositionAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {roleId}"));
            }
            // Mock: vị trí mặc định thành Phượng Tường Cổ Thành (mapId=1).
            var data = new SceneResponse
            {
                id = roleId, // scene id tạm bằng roleId cho đơn giản
                roleId = roleId,
                mapId = 1,
                posX = 1500,
                posY = 1500,
            };
            return Task.FromResult(new BackendResponse<SceneResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }

        public Task<BackendResponse<ItemListResponse>> ListItemsAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<ItemListResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {roleId}"));
            }
            // Mock: 2 vật phẩm cứng trong túi — 1 hồi máu (genre=2, detail=1)
            // và 1 vũ khí chưa trang bị (genre=0).
            var items = new List<ItemResponse>
            {
                new ItemResponse
                {
                    id = roleId * 100 + 1,
                    roleId = roleId,
                    genre = 2,
                    detail = 1,
                    particular = 1,
                    level = 1,
                    amount = 5,
                    slot = 0,
                    equipSlot = -1,
                    name = "Hồi Huyết Đan (nhỏ)",
                },
                new ItemResponse
                {
                    id = roleId * 100 + 2,
                    roleId = roleId,
                    genre = 0,
                    detail = 1,
                    particular = 12,
                    level = 10,
                    amount = 1,
                    slot = 1,
                    equipSlot = -1,
                    name = "Kiếm Phổ Thông",
                },
            };
            var data = new ItemListResponse
            {
                roleId = roleId,
                items = items,
            };
            return Task.FromResult(new BackendResponse<ItemListResponse>
            {
                code = "200",
                message = "Mock",
                data = data,
            });
        }
    }
}
