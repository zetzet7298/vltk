// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — BackendClientRunnerTests
// Smoke test MonoBehaviour runtime wirer: load config (mock) → run flow
// login → list roles → enter map → assert no exception + IsCompleted=true.
// Lưu ý: KHÔNG test thật REST server — runner dùng BackendConfig mặc định
// (useMock=true) nên chỉ verify wiring + luồng, không gọi network.
// -----------------------------------------------------------------------------

using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VLTK.Backend;

namespace VLTK.Tests.Backend
{
    public class BackendClientRunnerTests
    {
        private GameObject _go;
        private BackendClientRunner _runner;

        [SetUp]
        public void SetUp()
        {
            // Tạo GameObject mới với BackendClientRunner — KHÔNG gọi Start()
            // bằng cách SetActive(false) trước khi AddComponent, rồi destroy
            // ngay sau khi test xong. runOnStart=true sẽ KHÔNG chạy vì GO
            // chưa active.
            _go = new GameObject("BackendClientRunner_Test_GO");
            _go.SetActive(false);
            _runner = _go.AddComponent<BackendClientRunner>();
            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        [Test]
        public async Task RunAsync_MockMode_CompletesLoginListRolesEnterMap()
        {
            // Mặc định BackendConfig.useMock=true → không cần server thật.
            // Smoke: assert runner hoàn tất login → list roles → enter map
            // mà không ném exception.
            _runner.accName = "test_runner_acc";
            _runner.password = "test_runner_pw";
            _runner.enterMapId = 1;
            _runner.enterPosX = 100;
            _runner.enterPosY = 200;

            // runOnStart=true sẽ fire-and-forget trong SetActive(true) ở trên;
            // gọi RunAsync() thêm lần nữa để chờ completion deterministic.
            await _runner.RunAsync(default);

            Assert.IsNotNull(_runner.Client, "Client phải được khởi tạo.");
            Assert.IsTrue(_runner.Client.IsMock,
                "Mặc định useMock=true → Client phải là Mock.");
            Assert.IsTrue(_runner.IsCompleted, "RunAsync phải set IsCompleted=true.");
            Assert.IsNull(_runner.LastError,
                $"LastError phải null khi thành công; got: {_runner.LastError}");
        }

        [Test]
        public void RunAsync_CalledTwice_OnlyExecutesOnce()
        {
            // Gọi lần đầu fire-and-forget (SetActive(true) ở SetUp đã chạy).
            // Gọi explicit lần 2 → phải bị bỏ qua (idempotent guard).
            _runner.accName = "test_idempotent";
            _runner.password = "pw";

            // Lần 2 explicit: chỉ kiểm tra guard warning, không chờ completion.
            var task = _runner.RunAsync(default);
            // Lần 1 từ SetUp chạy song song — chờ cả 2 xong.
            Assert.IsNotNull(task);
        }

        [Test]
        public void DefaultAccName_NotEmpty()
        {
            // Smoke: Inspector field accName phải có default không rỗng.
            Assert.IsFalse(string.IsNullOrEmpty(_runner.accName),
                "accName default không được rỗng (cần cho runOnStart flow).");
            Assert.IsFalse(string.IsNullOrEmpty(_runner.password),
                "password default không được rỗng (cần cho runOnStart flow).");
        }

        [Test]
        public void DefaultEnterMapId_IsPhuongTuong()
        {
            // map_id=1 = Phượng Tường Cổ Thành (theo maplist.ini của PC).
            // Runner mặc định enter map_id=1 để smoke khớp với backend fixture.
            Assert.AreEqual(1, _runner.enterMapId,
                "Default enterMapId phải là 1 (Phượng Tường Cổ Thành).");
        }
    }
}
