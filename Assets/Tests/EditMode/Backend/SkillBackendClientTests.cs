// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — SkillBackendClientTests
// Test cho 5 facade method FS-03B trên BackendClient. Khi useMock=true, facade
// phải trả về đúng response từ MockGameBackend. Khi useMock=false, facade
// phải pass method xuống RestGameBackend (verified qua FakeHttpTransport).
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class SkillBackendClientTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig MockConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = true,
            defaultTimeoutSeconds = 5,
        };

        private static BackendConfig RestConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = false,
            defaultTimeoutSeconds = 5,
        };

        // ============================================================
        // Mock facade
        // ============================================================

        [Test]
        public async Task ListSkillsAsync_MockFacade_PassesThrough()
        {
            var client = new BackendClient(MockConfig());
            Assert.IsTrue(client.IsMock, "useMock=true phải trả MockGameBackend");

            var resp = await client.ListSkillsAsync(1);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, resp.data.skills.Count);
        }

        [Test]
        public async Task LearnSkillAsync_MockFacade_PassesThrough()
        {
            var client = new BackendClient(MockConfig());
            var resp = await client.LearnSkillAsync(new SkillLearnRequest(1, 22, 50, 0));

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(22, resp.data.skillId);
            Assert.AreEqual(1, resp.data.level);
        }

        [Test]
        public async Task LevelUpSkillAsync_MockFacade_PassesThrough()
        {
            var client = new BackendClient(MockConfig());
            var resp = await client.LevelUpSkillAsync(1, 22);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(2, resp.data.level);
        }

        [Test]
        public async Task CastSkillCheckAsync_MockFacade_PassesThrough()
        {
            var client = new BackendClient(MockConfig());
            var resp = await client.CastSkillCheckAsync(
                new SkillCastCheckRequest(1, 210, 300, 1000, 100, nowMs: 1000));

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(50, resp.data.costValue);
        }

        [Test]
        public async Task CastSkillAsync_MockFacade_PassesThrough()
        {
            var client = new BackendClient(MockConfig());
            var resp = await client.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(250, resp.data.currentMana);
        }

        [Test]
        public async Task ValidationError_PropagatesThroughFacade()
        {
            // Validation ở RestGameBackend/MockGameBackend phải propagate qua
            // facade — caller nhận code="validation_error".
            var client = new BackendClient(MockConfig());
            Assert.AreEqual("validation_error",
                (await client.ListSkillsAsync(0)).code);
            Assert.AreEqual("validation_error",
                (await client.CastSkillAsync(new SkillCastRequest(0, 210, 1000))).code);
            Assert.AreEqual("validation_error",
                (await client.CastSkillCheckAsync(null)).code);
        }

        // ============================================================
        // REST facade (verify IHttpTransport được inject đúng)
        // ============================================================

        [Test]
        public async Task ListSkillsAsync_RestFacade_PassesToRestGameBackend()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/skill/by-role/1", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"roleId\":1,\"skills\":[]}}");
            var client = new BackendClient(RestConfig(), fake);

            Assert.IsFalse(client.IsMock, "useMock=false phải trả RestGameBackend");
            var resp = await client.ListSkillsAsync(1);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(0, resp.data.skills.Count);
        }

        [Test]
        public async Task CastSkillAsync_RestFacade_PassesToRestGameBackend()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"skillId\":210,\"cast\":true,\"costType\":1,\"costPaid\":50," +
                "\"currentLife\":1000,\"currentMana\":250,\"currentStamina\":0," +
                "\"nextCastTime\":1000,\"effects\":[]}}");
            var client = new BackendClient(RestConfig(), fake);

            var resp = await client.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(250, resp.data.currentMana);
        }

        [Test]
        public async Task CastSkillCheckAsync_RestFacade_PropagatesServerDenial()
        {
            // Verify facade propagate đúng canCast=false từ server — caller biết
            // KHÔNG gửi /cast dù client predict OK.
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast/check", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"skillId\":210,\"canCast\":false," +
                "\"reason\":\"Khoảng cách quá xa\"," +
                "\"costType\":1,\"costValue\":50,\"delayPerCast\":0,\"nextCastTime\":0}}");
            var client = new BackendClient(RestConfig(), fake);

            var resp = await client.CastSkillCheckAsync(
                new SkillCastCheckRequest(1, 210, 300, 1000, 100, nowMs: 1000));

            Assert.IsTrue(resp.IsSuccess, "HTTP 200");
            Assert.IsFalse(resp.data.canCast, "server deny");
            Assert.AreEqual("Khoảng cách quá xa", resp.data.reason);
        }

        [Test]
        public async Task HttpError_5xx_PropagatesAsCode5xxThroughFacade()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/skill/cast", 500, "{\"detail\":\"oops\"}");
            var client = new BackendClient(RestConfig(), fake);

            var resp = await client.CastSkillAsync(new SkillCastRequest(1, 210, 1000));

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task TransportError_PropagatesThroughFacade()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("GET", "/v1/skill/by-role/1", new Exception("dns"));
            var client = new BackendClient(RestConfig(), fake);

            var resp = await client.ListSkillsAsync(1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }
    }
}
