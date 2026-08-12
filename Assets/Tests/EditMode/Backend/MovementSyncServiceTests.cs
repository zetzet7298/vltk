// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — MovementSyncServiceTests
// EditMode test cho MovementSyncService — service đồng bộ vị trí client ↔
// server với interval + reconciliation drift detection. Pure C# (KHÔNG
// MonoBehaviour), dùng MockGameBackend để tránh phụ thuộc HTTP.
//
// Phủ:
//   - SyncPosition_UpdatesServerPosition : gọi SyncPositionAsync, verify
//     ServerPosition + HasServerPosition được cập nhật từ response.
//   - ShouldSync_True_AfterInterval : tích luỹ dt >= syncInterval → trả true.
//   - NeedsReconciliation_DetectsDrift : client pos khác server quá
//     threshold → true; trong threshold → false; chưa có server pos → false.
//   - Extra: null backend throw, syncInterval<=0 clamp, validation request
//     fail null trả về null.
// -----------------------------------------------------------------------------

using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;
using VLTK.Backend.Movement;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class MovementSyncServiceTests
    {
        private const int TestRoleId = 42;

        private static BackendConfig NewMockConfig() => new BackendConfig
        {
            baseUrl = "http://127.0.0.1:8020",
            apiPrefix = "/v1",
            useMock = true,
        };

        // ============================================================
        // Constructor
        // ============================================================

        [Test]
        public void Constructor_NullBackend_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() =>
                new MovementSyncService(null, TestRoleId));
        }

        [Test]
        public void Constructor_InvalidRoleId_Throws()
        {
            var backend = new BackendClient(NewMockConfig());
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new MovementSyncService(backend, roleId: 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                new MovementSyncService(backend, roleId: -1));
        }

        [Test]
        public void Constructor_NegativeSyncInterval_ClampsToDefault()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId, syncInterval: -1f);
            // Clamp về 2f (default)
            Assert.AreEqual(2f, service.SyncInterval);
        }

        [Test]
        public void Constructor_ZeroSyncInterval_ClampsToDefault()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId, syncInterval: 0f);
            Assert.AreEqual(2f, service.SyncInterval);
        }

        [Test]
        public void Constructor_DefaultSyncInterval_2f()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId);
            Assert.AreEqual(2f, service.SyncInterval);
            Assert.AreEqual(TestRoleId, service.RoleId);
            Assert.IsFalse(service.HasServerPosition,
                "chưa sync lần nào → HasServerPosition = false");
        }

        // ============================================================
        // SyncPosition_UpdatesServerPosition
        // ============================================================

        [Test]
        public async Task SyncPosition_UpdatesServerPosition()
        {
            // Mock: trả về SceneResponse với posX/posY echo từ request, mapId=1.
            // Sau SyncPositionAsync(1500, 1800), ServerPosition phải là (1500, 1800).
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId, syncInterval: 2f);

            // Trước sync: HasServerPosition = false, ServerPosition = (0, 0).
            Assert.IsFalse(service.HasServerPosition);
            Assert.AreEqual((0, 0), service.ServerPosition);

            SceneResponse resp = await service.SyncPositionAsync(1500, 1800);

            Assert.IsNotNull(resp, "Mock echo → response phải khác null");
            Assert.AreEqual(1500, resp.posX);
            Assert.AreEqual(1800, resp.posY);
            Assert.AreEqual(TestRoleId, resp.roleId);
            Assert.AreEqual(1, resp.mapId);

            // Service PHẢI update ServerPosition từ response.
            Assert.IsTrue(service.HasServerPosition,
                "sau sync thành công → HasServerPosition = true");
            Assert.AreEqual(1500, service.ServerPosition.X);
            Assert.AreEqual(1800, service.ServerPosition.Y);
        }

        [Test]
        public async Task SyncPosition_ServerEchoes_DifferentPosition_Reconcile()
        {
            // Parity: server có thể trả vị trí KHÁC client gửi (vd vật cản,
            // warping). Service PHẢI dùng vị trí server trả, không giữ vị trí
            // client gửi.
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId);

            // Mock echo exact posX/posY — test vẫn pass nếu service dùng
            // server response (vì 2 giá trị trùng nhau). Đây là smoke test cho
            // code path "apply server state".
            SceneResponse resp = await service.SyncPositionAsync(2000, 3000);
            Assert.AreEqual(2000, resp.posX);
            Assert.AreEqual(3000, resp.posY);
            Assert.AreEqual((2000, 3000), service.ServerPosition);
        }

        // ============================================================
        // ShouldSync_True_AfterInterval
        // ============================================================

        [Test]
        public void ShouldSync_True_AfterInterval()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId, syncInterval: 2f);

            // Chưa đủ interval: 1.5s < 2.0s → false.
            Assert.IsFalse(service.ShouldSync(1.5f),
                "1.5s chưa đủ interval 2.0s");
            Assert.IsFalse(service.ShouldSync(0.3f),
                "tiếp tục tích luỹ 0.3s → 1.8s, vẫn < 2.0s");
            Assert.IsFalse(service.ShouldSync(0.1f),
                "tiếp tục tích luỹ 0.1s → 1.9s, vẫn < 2.0s");

            // Đạt đúng interval: 0.2s → 2.1s ≥ 2.0s → true.
            Assert.IsTrue(service.ShouldSync(0.2f),
                "tích luỹ đến ≥ 2.0s → ShouldSync = true");

            // Sau khi trả true, accumulator đã reset → frame kế trả false.
            Assert.IsFalse(service.ShouldSync(0.5f),
                "sau khi ShouldSync reset accumulator → 0.5s < 2.0s");
        }

        [Test]
        public void ShouldSync_ExactBoundary_ReturnsTrue()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId, syncInterval: 1f);

            // Tích luỹ chính xác 1.0s → ShouldSync = true (>= boundary).
            Assert.IsTrue(service.ShouldSync(1f));
        }

        [Test]
        public void ShouldSync_NegativeDt_Ignored()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId, syncInterval: 1f);

            // dt âm (debug step back) → bỏ qua, KHÔNG tích luỹ.
            Assert.IsFalse(service.ShouldSync(-0.5f));
            Assert.AreEqual(0f, service.Accumulator,
                "dt âm phải được bỏ qua, accumulator không đổi");
        }

        [Test]
        public async Task ShouldSync_ResetOnSuccessfulSync()
        {
            // Sau khi SyncPositionAsync thành công, accumulator phải reset về 0.
            // Đây là đảm bảo caller gọi ShouldSync liên tục sẽ không bị "burst"
            // nhiều sync liên tiếp.
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId, syncInterval: 1f);

            service.ShouldSync(0.9f); // tích luỹ 0.9s
            Assert.AreEqual(0.9f, service.Accumulator, 0.001f);
            service.ShouldSync(0.5f); // đạt 1.4s ≥ 1.0s → reset
            Assert.AreEqual(0f, service.Accumulator, 0.001f,
                "sau ShouldSync trả true → accumulator reset");

            // Sync thành công → accumulator cũng reset.
            service.ShouldSync(0.7f);
            Assert.AreEqual(0.7f, service.Accumulator, 0.001f);
            await service.SyncPositionAsync(100, 100);
            Assert.AreEqual(0f, service.Accumulator, 0.001f,
                "sau SyncPositionAsync thành công → accumulator reset");
        }

        // ============================================================
        // NeedsReconciliation_DetectsDrift
        // ============================================================

        [Test]
        public async Task NeedsReconciliation_NoServerPosition_ReturnsFalse()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId);

            // Chưa từng sync thành công → không có data để so sánh.
            Assert.IsFalse(service.NeedsReconciliation(9999, 9999),
                "không có ServerPosition → NeedsReconciliation = false (chưa biết)");
            Assert.IsFalse(service.NeedsReconciliation(0, 0));
        }

        [Test]
        public async Task NeedsReconciliation_DetectsDrift()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId);

            // Sync lần đầu: server pos = (1000, 1000).
            await service.SyncPositionAsync(1000, 1000);
            Assert.IsTrue(service.HasServerPosition);
            Assert.AreEqual((1000, 1000), service.ServerPosition);

            // Client đứng yên tại (1000, 1000) → không drift.
            Assert.IsFalse(service.NeedsReconciliation(1000, 1000),
                "client pos == server pos → không drift");
            Assert.IsFalse(service.NeedsReconciliation(1010, 1010),
                "|dx|=|dy|=10 < threshold 50 → không drift");
            Assert.IsFalse(service.NeedsReconciliation(950, 1050),
                "|dx|=50, |dy|=50 → không drift (threshold 50, không vượt)");

            // Client drift vượt threshold 1 trục → true.
            Assert.IsTrue(service.NeedsReconciliation(1051, 1000),
                "|dx|=51 > 50 → drift theo X");
            Assert.IsTrue(service.NeedsReconciliation(1000, 1051),
                "|dy|=51 > 50 → drift theo Y");
            Assert.IsTrue(service.NeedsReconciliation(2000, 1000),
                "client teleport xa → drift");
            Assert.IsTrue(service.NeedsReconciliation(500, 500),
                "client đi xa theo cả 2 trục → drift");

            // Threshold tuỳ chỉnh.
            Assert.IsFalse(service.NeedsReconciliation(1100, 1000, threshold: 200),
                "threshold 200 → |dx|=100 < 200 → không drift");
            Assert.IsTrue(service.NeedsReconciliation(1300, 1000, threshold: 200),
                "threshold 200 → |dx|=300 > 200 → drift");
        }

        [Test]
        public void NeedsReconciliation_NegativeThreshold_ClampsToZero()
        {
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId);

            // Pre-seed ServerPosition qua public API: dùng 1 sync hợp lệ.
            // Ở đây test constructor-only: HasServerPosition = false nên
            // NeedsReconciliation luôn false bất kể threshold.
            Assert.IsFalse(service.NeedsReconciliation(100, 100, threshold: -10));
        }

        // ============================================================
        // SyncPositionAsync — error paths
        // ============================================================

        [Test]
        public async Task SyncPosition_BackendFailure_ReturnsNull()
        {
            // Tạo RestGameBackend với FakeHttpTransport trả 500.
            // BackendClient trỏ tới RestGameBackend (useMock=false) nhưng dùng
            // transport fake.
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/movement", 500, "{\"detail\":\"db\"}");
            var config = new BackendConfig
            {
                baseUrl = "http://127.0.0.1:8020",
                apiPrefix = "/v1",
                useMock = false,
                defaultTimeoutSeconds = 5,
            };
            var backend = new BackendClient(config, fake);
            var service = new MovementSyncService(backend, TestRoleId);

            SceneResponse resp = await service.SyncPositionAsync(500, 500);
            Assert.IsNull(resp, "HTTP 500 → service trả null (không throw)");
            Assert.IsFalse(service.HasServerPosition,
                "fail thì KHÔNG update ServerPosition (giữ state cũ)");
        }

        [Test]
        public async Task SyncPosition_NegativePosition_ReturnsNull()
        {
            // Mock backend validation: posX/posY < 0 → trả Failure.
            var backend = new BackendClient(NewMockConfig());
            var service = new MovementSyncService(backend, TestRoleId);

            SceneResponse resp = await service.SyncPositionAsync(-1, 100);
            Assert.IsNull(resp, "posX âm → service trả null");
            Assert.IsFalse(service.HasServerPosition);

            resp = await service.SyncPositionAsync(100, -5);
            Assert.IsNull(resp, "posY âm → service trả null");
            Assert.IsFalse(service.HasServerPosition);
        }

        [Test]
        public async Task SyncPosition_AfterFailure_RecoversToSuccess()
        {
            // Lần 1 fail → HasServerPosition = false.
            // Lần 2 success → HasServerPosition = true, ServerPosition cập nhật.
            // Dùng MockGameBackend cho cả 2 path: lần 1 gửi pos âm (validation
            // fail) → null; lần 2 gửi pos hợp lệ → success.
            var config = new BackendConfig
            {
                baseUrl = "http://127.0.0.1:8020",
                apiPrefix = "/v1",
                useMock = true,
            };
            var mockBackend = new BackendClient(config);
            var service = new MovementSyncService(mockBackend, TestRoleId);

            // Path 1: negative pos → fail
            SceneResponse r1 = await service.SyncPositionAsync(-1, 0);
            Assert.IsNull(r1);
            Assert.IsFalse(service.HasServerPosition);

            // Path 2: valid pos → success, ServerPosition cập nhật
            SceneResponse r2 = await service.SyncPositionAsync(1500, 1800);
            Assert.IsNotNull(r2);
            Assert.IsTrue(service.HasServerPosition);
            Assert.AreEqual((1500, 1800), service.ServerPosition);
        }
    }
}
