// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — EnterMapAsyncTests
// EditMode test cho slice FS-02C endpoint EnterMapAsync:
//   - Rest:  POST /v1/map/enter với body=EnterMapRequest → DataResponse[SceneResponse]
//   - Mock:  trả về SceneResponse echo từ request
//
// Phủ các case: URL build, body JSON serialize (camelCase), parse envelope,
// error path (HTTP 500, transport error, invalid_arg khi request=null).
// -----------------------------------------------------------------------------

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Dto;
using VLTK.Backend.Mock;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class EnterMapAsyncTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig NewRestConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = false,
            defaultTimeoutSeconds = 5,
        };

        private static BackendConfig NewMockConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = true,
        };

        private const string EnvelopeOk = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{\"id\":42,\"roleId\":7,\"mapId\":15,\"posX\":1500,\"posY\":1800}" +
            "}";

        // -------- RestGameBackend --------

        [Test]
        public async Task Rest_BuildsPostUrlAndSendsJsonBody()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/map/enter", 200, EnvelopeOk);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new EnterMapRequest
            {
                roleId = 7,
                mapId = 15,
                posX = 1500,
                posY = 1800,
            };
            var resp = await backend.EnterMapAsync(req);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual(42, resp.data.id);
            Assert.AreEqual(7, resp.data.roleId);
            Assert.AreEqual(15, resp.data.mapId);
            Assert.AreEqual(1500, resp.data.posX);
            Assert.AreEqual(1800, resp.data.posY);

            // URL đã gửi: POST tới base + /v1/map/enter
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
            Assert.AreEqual($"{BaseUrl}/v1/map/enter", fake.Sent[0].Url);

            // Body JSON: camelCase, đủ 4 field, đúng giá trị
            string body = fake.Sent[0].Body;
            StringAssert.Contains("\"roleId\":7", body);
            StringAssert.Contains("\"mapId\":15", body);
            StringAssert.Contains("\"posX\":1500", body);
            StringAssert.Contains("\"posY\":1800", body);
        }

        [Test]
        public async Task Rest_NullRequest_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.EnterMapAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            // Không gửi request gì cả khi request=null
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_HttpError500_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/map/enter", 500, "{\"detail\":\"internal\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.EnterMapAsync(new EnterMapRequest
            {
                roleId = 1, mapId = 1, posX = 0, posY = 0,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task Rest_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("POST", "/v1/map/enter", new Exception("dns fail"));
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.EnterMapAsync(new EnterMapRequest
            {
                roleId = 1, mapId = 1, posX = 0, posY = 0,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task Rest_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/map/enter", 200, "not json at all");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.EnterMapAsync(new EnterMapRequest
            {
                roleId = 1, mapId = 1, posX = 0, posY = 0,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        // -------- MockGameBackend --------

        [Test]
        public async Task Mock_ReturnsSceneResponseEchoingRequest()
        {
            var backend = new MockGameBackend(NewMockConfig());

            var req = new EnterMapRequest
            {
                roleId = 7,
                mapId = 15,
                posX = 1500,
                posY = 1800,
            };
            var resp = await backend.EnterMapAsync(req);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual("Mock", resp.message);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(7, resp.data.roleId);
            Assert.AreEqual(15, resp.data.mapId);
            Assert.AreEqual(1500, resp.data.posX);
            Assert.AreEqual(1800, resp.data.posY);
        }

        [Test]
        public async Task Mock_NullRequest_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.EnterMapAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }
    }
}
