// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — MovementSyncMonoBehaviourTests
// EditMode test cho MovementSyncMonoBehaviour: driver layer gắn MovementSyncService
// (FS-04C) vào MonoBehaviour để mỗi FixedUpdate kiểm tra ShouldSync(dt) và gọi
// SyncPositionAsync khi đạt interval. Verify:
//   1. BindBackend: tạo MovementSyncService, IsBound=true.
//   2. CallsSyncPeriodically: gọi FixedUpdate đủ số lần → service.SyncPositionAsync
//      được trigger, ServerPosition được cập nhật.
//   3. UnbindBackend: trở về no-op, FixedUpdate không làm gì.
//   4. FixedUpdate trước khi BindBackend: no-op, không ném exception.
//
// Vì EditMode Editor KHÔNG tự gọi MonoBehaviour.FixedUpdate, test phải gọi
// private FixedUpdate bằng reflection. Đây là pattern test MonoBehaviour
// driver trong EditMode — PlayMode test runner mới chạy loop tự động.
// -----------------------------------------------------------------------------

using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Movement;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class MovementSyncMonoBehaviourTests
    {
        private const int TestRoleId = 42;
        private const float TestSyncInterval = 0.1f;

        private GameObject _go;
        private MovementSyncMonoBehaviour _driver;
        private MethodInfo _fixedUpdateMethod;

        private static BackendConfig NewMockConfig() => new BackendConfig
        {
            baseUrl = "http://127.0.0.1:8020",
            apiPrefix = "/v1",
            useMock = true,
        };

        [SetUp]
        public void SetUp()
        {
            // Tạo GameObject mới với MovementSyncMonoBehaviour. KHÔNG active
            // lúc đầu để tránh Unity gọi OnEnable trong Editor. Test sẽ active
            // thủ công khi cần FixedUpdate.
            _go = new GameObject("MovementSyncMonoBehaviour_Test_GO");
            _go.SetActive(false);
            _driver = _go.AddComponent<MovementSyncMonoBehaviour>();
            _driver.syncInterval = TestSyncInterval;
            _driver.reconciliationThreshold = 50;
            _driver.verbose = false;

            // Reflection cache cho private FixedUpdate — MonoBehaviour message
            // method không public, không thể gọi trực tiếp trong test.
            _fixedUpdateMethod = typeof(MovementSyncMonoBehaviour).GetMethod(
                "FixedUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(_fixedUpdateMethod,
                "MovementSyncMonoBehaviour.FixedUpdate không tồn tại — kiểm tra lại code.");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        // ============================================================
        // BindBackend / IsBound / UnbindBackend
        // ============================================================

        [Test]
        public void BindBackend_CreatesService_SetsIsBoundTrue()
        {
            Assert.IsFalse(_driver.IsBound,
                "Trước BindBackend phải IsBound=false.");

            var backend = new BackendClient(NewMockConfig());
            _driver.BindBackend(backend, TestRoleId);

            Assert.IsTrue(_driver.IsBound,
                "Sau BindBackend phải IsBound=true.");
            Assert.IsNotNull(_driver.Service,
                "Sau BindBackend phải có service.");
            Assert.AreEqual(TestRoleId, _driver.Service.RoleId,
                "Service.RoleId phải khớp roleId inject.");
            Assert.AreEqual(TestSyncInterval, _driver.Service.SyncInterval,
                "Service.SyncInterval phải lấy từ MonoBehaviour.syncInterval.");
        }

        [Test]
        public void BindBackend_NullBackend_Unbinds()
        {
            var backend = new BackendClient(NewMockConfig());
            _driver.BindBackend(backend, TestRoleId);
            Assert.IsTrue(_driver.IsBound);

            _driver.BindBackend(null, TestRoleId);
            Assert.IsFalse(_driver.IsBound,
                "BindBackend(null) phải fallback về unbind.");
        }

        [Test]
        public void BindBackend_InvalidRoleId_Unbinds()
        {
            var backend = new BackendClient(NewMockConfig());
            _driver.BindBackend(backend, 0);
            Assert.IsFalse(_driver.IsBound, "roleId=0 phải unbind.");
            _driver.BindBackend(backend, -1);
            Assert.IsFalse(_driver.IsBound, "roleId=-1 phải unbind.");
        }

        [Test]
        public void UnbindBackend_ResetsState()
        {
            var backend = new BackendClient(NewMockConfig());
            _driver.BindBackend(backend, TestRoleId);
            Assert.IsTrue(_driver.IsBound);

            _driver.UnbindBackend();
            Assert.IsFalse(_driver.IsBound);
            Assert.IsNull(_driver.Service,
                "Sau UnbindBackend phải Service=null.");
        }

        // ============================================================
        // FixedUpdate: no-op khi chưa BindBackend
        // ============================================================

        [Test]
        public void FixedUpdate_BeforeBind_NoOpNoException()
        {
            // Gọi FixedUpdate mà chưa BindBackend — phải no-op, không throw.
            Assert.DoesNotThrow(() => InvokeFixedUpdate());
        }

        // ============================================================
        // FixedUpdate: gọi sync định kỳ
        // ============================================================

        [Test]
        public void FixedUpdate_CallsSyncPeriodically()
        {
            // Mock: trả SceneResponse với posX/posY echo từ request, mapId=1.
            // Sau khi sync, service.ServerPosition phải khớp vị trí đã gửi.
            var backend = new BackendClient(NewMockConfig());
            _driver.BindBackend(backend, TestRoleId);
            Assert.IsFalse(_driver.Service.HasServerPosition,
                "Trước sync phải HasServerPosition=false.");

            // Đặt transform.position ở world (1500 * 512, 1800 * 512) = ô
            // lưới (1500, 1800). WorldGridMapper.WorldToGridX sẽ trả 1500
            // (Mathf.FloorToInt(768000 / 512) = 1500).
            const int expectedGridX = 1500;
            const int expectedGridY = 1800;
            _go.transform.position = new Vector3(
                expectedGridX * WorldGridMapper.DefaultTileSize,
                expectedGridY * WorldGridMapper.DefaultTileSize,
                0f);

            // Mock backend UpdatePositionAsync là async; sync MonoBehaviour
            // fire-and-forget rồi check ở Update. EditMode không chạy Update
            // loop tự động, nên phải pump thủ công bằng cách gọi Update
            // (cũng private). Nhưng trước tiên cần sync đã hoàn tất.
            //
            // Cách verify: gọi FixedUpdate đủ nhiều lần để trigger sync, rồi
            // chờ một lúc để async task hoàn tất (mock trả gần như instant),
            // sau đó check service.ServerPosition.
            //
            // syncInterval=0.1f, fixedDeltaTime=0.02f (default). Mỗi
            // FixedUpdate cộng 0.02f vào accumulator; cần 5 lần để đạt 0.1f.
            // Gọi 6 lần cho chắc.
            for (int i = 0; i < 6; i++)
            {
                InvokeFixedUpdate();
            }

            // Mock backend trả về SceneResponse echo pos từ request, nên
            // ServerPosition phải là (expectedGridX, expectedGridY) sau khi
            // task chạy xong. Pump thêm vài FixedUpdate để chắc task complete.
            // (Mock backend UpdatePositionAsync chỉ await Task.Yield 1 lần —
            // cần nhiều pump hơn task thật.)
            //
            // Cách đơn giản: chờ synchronous bằng cách gọi lại FixedUpdate
            // cho tới khi HasServerPosition=true, với max retry.
            int maxRetries = 50;
            while (maxRetries-- > 0 && !_driver.Service.HasServerPosition)
            {
                // Editor update tick — yield cho async task chạy.
                System.Threading.Thread.Sleep(10);
            }

            Assert.IsTrue(_driver.Service.HasServerPosition,
                $"Sau {50 - maxRetries} retries, sync phải hoàn tất " +
                $"(Service.HasServerPosition=true).");
            var (srvX, srvY) = _driver.Service.ServerPosition;
            Assert.AreEqual(expectedGridX, srvX,
                $"ServerPosition.X phải là {expectedGridX} " +
                $"(transform.position X = {expectedGridX * WorldGridMapper.DefaultTileSize} → grid {expectedGridX}); " +
                $"got: {srvX}");
            Assert.AreEqual(expectedGridY, srvY,
                $"ServerPosition.Y phải là {expectedGridY} (grid); got: {srvY}");
        }

        [Test]
        public void FixedUpdate_AfterUnbind_NoOp()
        {
            var backend = new BackendClient(NewMockConfig());
            _driver.BindBackend(backend, TestRoleId);
            _driver.UnbindBackend();

            // Sau unbind, gọi FixedUpdate phải no-op, không throw.
            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 3; i++)
                {
                    InvokeFixedUpdate();
                }
            });
            // Service đã null, không có gì để verify thêm.
        }

        // ============================================================
        // Helper
        // ============================================================

        private void InvokeFixedUpdate()
        {
            _fixedUpdateMethod.Invoke(_driver, null);
        }
    }
}
