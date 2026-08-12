// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — AuthMockGameBackendTests
// Test cho 3 endpoint FS-02B trên MockGameBackend (offline, không cần network).
// Mock phải trả về shape giống backend thật để UI/SandboxManager có thể chạy
// mà không cần server.
// -----------------------------------------------------------------------------

using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;

namespace VLTK.Tests.Backend
{
    public class AuthMockGameBackendTests
    {
        private BackendConfig _config;

        private BackendConfig NewConfig()
        {
            _config = ScriptableObject.CreateInstance<BackendConfig>();
            _config.baseUrl = "http://test:8020";
            _config.apiPrefix = "/v1";
            _config.useMock = true;
            return _config;
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
                UnityEngine.Object.DestroyImmediate(_config);
            _config = null;
        }

        // ============================================================
        // LoginAsync
        // ============================================================

        [Test]
        public async Task CreateAccountAsync_ReturnsSafeAccountShape()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.CreateAccountAsync(new AccountCreateRequest
            {
                accName = "alice",
                password = "plaintext",
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("alice", resp.data.accName);
            Assert.IsFalse(resp.data.isBanned);
        }

        [Test]
        public async Task CreateRoleAsync_ReturnsRequestedIdentity()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.CreateRoleAsync(new RoleCreateRequest
            {
                account = "alice",
                roleName = "AliceRole",
                faction = -1,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("AliceRole", resp.data.roleName);
            Assert.AreEqual("alice", resp.data.account);
        }

        [Test]
        public async Task LoginAsync_ReturnsEchoedAccName()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.LoginAsync("alice", "hunter2");

            Assert.IsTrue(resp.IsSuccess, $"mock phải trả IsSuccess=true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual("Mock", resp.message);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual("alice", resp.data.accName);
            Assert.AreEqual(0, resp.data.serviceFlag);
            Assert.AreEqual(0, resp.data.extPoint);
        }

        [Test]
        public async Task LoginAsync_DoesNotHashPassword()
        {
            // Mock chỉ echo password plaintext — caller chịu trách nhiệm gửi
            // plaintext. Test pin hành vi này để mock không âm thầm hash.
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.LoginAsync("alice", "plain_password_42");

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("alice", resp.data.accName);
        }

        [Test]
        public async Task LoginAsync_EmptyAccName_ValidationError()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.LoginAsync("", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task LoginAsync_EmptyPassword_ValidationError()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.LoginAsync("alice", "");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        // ============================================================
        // ListRolesAsync
        // ============================================================

        [Test]
        public async Task ListRolesAsync_ReturnsSeededRole()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.ListRolesAsync("alice");

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual("alice", resp.data.account);
            Assert.IsNotNull(resp.data.roles);
            Assert.AreEqual(1, resp.data.roles.Count, "mock seed 1 role");
            var role = resp.data.roles[0];
            Assert.AreEqual(1, role.id);
            Assert.AreEqual("alice", role.account);
            Assert.AreEqual(0, role.faction);
            Assert.AreEqual("Thiếu Lâm", role.factionName);
            Assert.AreEqual(1, role.level);
        }

        [Test]
        public async Task ListRolesAsync_AccNameEchoesInResponse()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.ListRolesAsync("bob");

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("bob", resp.data.account);
            Assert.AreEqual("bob", resp.data.roles[0].account);
        }

        [Test]
        public async Task ListRolesAsync_EmptyAccName_ValidationError()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.ListRolesAsync("");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        // ============================================================
        // GetPlayerStateAsync
        // ============================================================

        [Test]
        public async Task GetPlayerStateAsync_ReturnsKimDefaultStats()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.GetPlayerStateAsync(1);

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(1, resp.data.id);
            Assert.AreEqual(1, resp.data.roleId);
            Assert.AreEqual(1, resp.data.level);
            // Stat Kim mặc định 35/25/25/15 — parity với backend task_head.lua:79-82.
            Assert.AreEqual(35, resp.data.strength);
            Assert.AreEqual(25, resp.data.dexterity);
            Assert.AreEqual(25, resp.data.vitality);
            Assert.AreEqual(15, resp.data.spirit);
            Assert.AreEqual(0, resp.data.series);
        }

        [Test]
        public async Task GetPlayerStateAsync_DifferentRoleIds_AllReturnValidResponse()
        {
            var backend = new MockGameBackend(NewConfig());
            foreach (int roleId in new[] { 1, 42, 1000 })
            {
                var resp = await backend.GetPlayerStateAsync(roleId);
                Assert.IsTrue(resp.IsSuccess, $"roleId={roleId} phải success");
                Assert.AreEqual(roleId, resp.data.roleId, $"roleId phải echo input");
            }
        }

        [Test]
        public async Task GetPlayerStateAsync_ZeroRoleId_ValidationError()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.GetPlayerStateAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        [Test]
        public async Task GetPlayerStateAsync_NegativeRoleId_ValidationError()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.GetPlayerStateAsync(-1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
        }

        // ============================================================
        // Cross-method consistency
        // ============================================================

        [Test]
        public async Task AuthFlow_LoginThenListThenGetPlayer_AllReturnSuccess()
        {
            // End-to-end happy path: Login → ListRoles → GetPlayerState, dùng
            // accName/roleId từ response trước. Mock giữ role cố định id=1, nên
            // lấy thẳng role đó.
            var backend = new MockGameBackend(NewConfig());

            var login = await backend.LoginAsync("alice", "pw");
            Assert.IsTrue(login.IsSuccess);
            Assert.AreEqual("alice", login.data.accName);

            var roles = await backend.ListRolesAsync(login.data.accName);
            Assert.IsTrue(roles.IsSuccess);
            Assert.GreaterOrEqual(roles.data.roles.Count, 1);
            int roleId = roles.data.roles[0].id;

            var player = await backend.GetPlayerStateAsync(roleId);
            Assert.IsTrue(player.IsSuccess);
            Assert.AreEqual(roleId, player.data.roleId);
        }
    }
}
