// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — AuthBackendClientTests
// Test cho BackendClient facade với 3 method auth mới (FS-02B).
// Mục tiêu: facade phải route đến đúng backend impl (mock vs rest) và các
// method mới hoạt động giống hệt pass-through.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class AuthBackendClientTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";
        private readonly List<BackendConfig> _configs = new();

        private BackendConfig NewConfig(bool useMock)
        {
            var config = ScriptableObject.CreateInstance<BackendConfig>();
            config.baseUrl = BaseUrl;
            config.apiPrefix = "/v1";
            config.useMock = useMock;
            config.defaultTimeoutSeconds = 5;
            _configs.Add(config);
            return config;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (BackendConfig config in _configs)
                UnityEngine.Object.DestroyImmediate(config);
            _configs.Clear();
        }

        // ============================================================
        // Mock routing
        // ============================================================

        [Test]
        public async Task LoginAsync_UseMock_RoutesToMockGameBackend()
        {
            var client = new BackendClient(NewConfig(useMock: true));
            var resp = await client.LoginAsync("alice", "pw");

            Assert.IsTrue(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("Mock", resp.message);
        }

        [Test]
        public async Task ListRolesAsync_UseMock_RoutesToMockGameBackend()
        {
            var client = new BackendClient(NewConfig(useMock: true));
            var resp = await client.ListRolesAsync("alice");

            Assert.IsTrue(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("alice", resp.data.account);
        }

        [Test]
        public async Task GetPlayerStateAsync_UseMock_RoutesToMockGameBackend()
        {
            var client = new BackendClient(NewConfig(useMock: true));
            var resp = await client.GetPlayerStateAsync(1);

            Assert.IsTrue(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, resp.data.roleId);
        }

        // ============================================================
        // REST routing with injected FakeHttpTransport
        // ============================================================

        [Test]
        public async Task CreateAccountAsync_UseRest_PostsFastApiContract()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{" +
                "\"id\":7,\"accName\":\"alice\",\"isBanned\":false," +
                "\"isUseOtp\":false,\"serviceFlag\":0,\"extPoint\":0}}");
            var client = new BackendClient(NewConfig(useMock: false), fake);

            var resp = await client.CreateAccountAsync(new AccountCreateRequest
            {
                accName = "alice",
                password = "pw",
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("alice", resp.data.accName);
            Assert.That(fake.Sent[0].Body, Does.Contain("\"accName\":\"alice\""));
        }

        [Test]
        public async Task CreateRoleAsync_UseRest_PostsFastApiContract()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/role", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{" +
                "\"id\":42,\"roleName\":\"AliceRole\",\"account\":\"alice\"," +
                "\"faction\":-1,\"factionName\":null,\"level\":1}}");
            var client = new BackendClient(NewConfig(useMock: false), fake);

            var resp = await client.CreateRoleAsync(new RoleCreateRequest
            {
                account = "alice",
                roleName = "AliceRole",
                faction = -1,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(42, resp.data.id);
            Assert.That(fake.Sent[0].Body, Does.Contain("\"roleName\":\"AliceRole\""));
        }

        [Test]
        public async Task LoginAsync_UseRest_GoesThroughInjectedTransport()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"accName\":\"alice\",\"serviceFlag\":0,\"extPoint\":0}}");
            var client = new BackendClient(NewConfig(useMock: false), fake);

            var resp = await client.LoginAsync("alice", "pw");

            Assert.IsFalse(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
        }

        [Test]
        public async Task ListRolesAsync_UseRest_GoesThroughInjectedTransport()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/role/by-account/alice", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"account\":\"alice\",\"roles\":[]}}");
            var client = new BackendClient(NewConfig(useMock: false), fake);

            var resp = await client.ListRolesAsync("alice");

            Assert.IsFalse(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(0, resp.data.roles.Count);
        }

        [Test]
        public async Task GetPlayerStateAsync_UseRest_GoesThroughInjectedTransport()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/player/by-role/1", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"id\":1,\"roleId\":1,\"level\":1,\"exp\":0," +
                "\"transLife\":0,\"freePoint\":0,\"magicPoint\":0," +
                "\"strength\":35,\"dexterity\":25,\"vitality\":25,\"spirit\":15," +
                "\"series\":0,\"money\":0,\"repute\":0}}");
            var client = new BackendClient(NewConfig(useMock: false), fake);

            var resp = await client.GetPlayerStateAsync(1);

            Assert.IsFalse(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(35, resp.data.strength);
        }

        // ============================================================
        // End-to-end auth flow
        // ============================================================

        [Test]
        public async Task AuthFlow_MockBackend_LoginListGetPlayer_FullChain()
        {
            // Mô phỏng flow người chơi: login → list roles → get player state
            // cho role đầu tiên. Dùng mock để chạy offline.
            var client = new BackendClient(NewConfig(useMock: true));

            var login = await client.LoginAsync("alice", "pw");
            Assert.IsTrue(login.IsSuccess);

            var roles = await client.ListRolesAsync(login.data.accName);
            Assert.IsTrue(roles.IsSuccess);
            Assert.GreaterOrEqual(roles.data.roles.Count, 1);

            int roleId = roles.data.roles[0].id;
            var player = await client.GetPlayerStateAsync(roleId);
            Assert.IsTrue(player.IsSuccess);
            Assert.AreEqual(roleId, player.data.roleId);
        }

        [Test]
        public async Task AuthFlow_RestBackend_LoginListGetPlayer_FullChain()
        {
            // Cùng flow nhưng qua FakeHttpTransport, kiểm tra transport nhận
            // đúng 3 request theo thứ tự.
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"accName\":\"alice\",\"serviceFlag\":0,\"extPoint\":0}}");
            fake.QueueResponse("GET", "/v1/role/by-account/alice", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"account\":\"alice\",\"roles\":[" +
                "{\"id\":42,\"roleName\":\"Kiem_Khach\",\"account\":\"alice\"," +
                "\"faction\":0,\"factionName\":\"Thiếu Lâm\",\"level\":1}]}}");
            fake.QueueResponse("GET", "/v1/player/by-role/42", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"id\":1,\"roleId\":42,\"level\":1,\"exp\":0," +
                "\"transLife\":0,\"freePoint\":0,\"magicPoint\":0," +
                "\"strength\":35,\"dexterity\":25,\"vitality\":25,\"spirit\":15," +
                "\"series\":0,\"money\":0,\"repute\":0}}");
            var client = new BackendClient(NewConfig(useMock: false), fake);

            var login = await client.LoginAsync("alice", "pw");
            Assert.IsTrue(login.IsSuccess);

            var roles = await client.ListRolesAsync(login.data.accName);
            Assert.IsTrue(roles.IsSuccess);
            Assert.AreEqual(42, roles.data.roles[0].id);

            var player = await client.GetPlayerStateAsync(roles.data.roles[0].id);
            Assert.IsTrue(player.IsSuccess);
            Assert.AreEqual(42, player.data.roleId);

            // 3 request đã gửi đi theo đúng thứ tự.
            Assert.AreEqual(3, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
            Assert.AreEqual("GET", fake.Sent[1].Method);
            Assert.AreEqual("GET", fake.Sent[2].Method);
        }
    }
}
