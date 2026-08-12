// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — GetMapPositionAsyncTests
// EditMode test cho slice FS-02C endpoint GetMapPositionAsync:
//   - Rest:  GET /v1/map/position/{role_id} → DataResponse[SceneResponse]
//   - Mock:  trả về SceneResponse mặc định (Phượng Tường, 1500,1500)
//
// Phủ: URL build với roleId trong path, parse envelope, validation
// roleId <= 0 → invalid_arg không gửi request, error paths.
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
    public class GetMapPositionAsyncTests
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
            "\"data\":{\"id\":1,\"roleId\":42,\"mapId\":7,\"posX\":1200,\"posY\":3400}" +
            "}";

        // -------- RestGameBackend --------

        [Test]
        public async Task Rest_BuildsGetUrlWithRoleIdInPath()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/map/position/42", 200, EnvelopeOk);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.GetMapPositionAsync(42);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(42, resp.data.roleId);
            Assert.AreEqual(7, resp.data.mapId);
            Assert.AreEqual(1200, resp.data.posX);
            Assert.AreEqual(3400, resp.data.posY);

            // URL: GET base + /v1/map/position/42 (không query, không body)
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("GET", fake.Sent[0].Method);
            Assert.AreEqual($"{BaseUrl}/v1/map/position/42", fake.Sent[0].Url);
            Assert.IsTrue(string.IsNullOrEmpty(fake.Sent[0].Body),
                "GET không được có body");
        }

        [Test]
        public async Task Rest_RoleIdZero_ReturnsInvalidArg()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.GetMapPositionAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count, "Không gửi request khi roleId=0");
        }

        [Test]
        public async Task Rest_RoleIdNegative_ReturnsInvalidArg()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.GetMapPositionAsync(-5);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_HttpError404_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/map/position/999", 404,
                "{\"detail\":\"role not found\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.GetMapPositionAsync(999);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task Rest_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("GET", "/v1/map/position/", new Exception("connection reset"));
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.GetMapPositionAsync(1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        // -------- MockGameBackend --------

        [Test]
        public async Task Mock_ReturnsDefaultSceneAtPhuongTuong()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.GetMapPositionAsync(42);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("Mock", resp.message);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(42, resp.data.roleId);
            Assert.AreEqual(1, resp.data.mapId);
            Assert.AreEqual(1500, resp.data.posX);
            Assert.AreEqual(1500, resp.data.posY);
        }

        [Test]
        public async Task Mock_RoleIdZero_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.GetMapPositionAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_NegativeRoleId_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.GetMapPositionAsync(-1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }
    }
}
