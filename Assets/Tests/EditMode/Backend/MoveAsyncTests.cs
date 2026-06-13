// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — MoveAsyncTests
// EditMode test cho slice FS-04B endpoint MoveAsync (POST /v1/movement):
//   - Rest:  POST /v1/movement với body=MoveRequest (roleId/posX/posY) → DataResponse[SceneResponse]
//   - Mock:  trả về SceneResponse echo posX/posY từ request, mapId=1 mặc định
//
// Phủ: URL build, body JSON serialize (camelCase KHÔNG có mapId), parse
// envelope, error path (HTTP 500, transport error, invalid_arg khi
// request=null, roleId<=0, posX<0, posY<0), và BackendClient facade.
//
// PC REF: KNpc.cpp:5496 — int KNpc::SetPos(int nX, int nY); chỉ ghi
// m_MapX/m_MapY, không gọi NewWorld → KHÔNG đổi mapId (test này KHÔNG
// assert mapId thay đổi — rest thật sẽ giữ mapId cũ, mock hardcode 1).
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
    public class MoveAsyncTests
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

        // Envelope mẫu — server giữ nguyên mapId=1 (role đã ở Phượng Tường từ
        // trước), chỉ đổi posX/posY. Đây là parity với test backend
        // test_movement_update_position (FS-04A).
        private const string EnvelopeOk = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{\"id\":42,\"roleId\":7,\"mapId\":1,\"posX\":320,\"posY\":450}" +
            "}";

        // -------- RestGameBackend --------

        [Test]
        public async Task Rest_BuildsPostUrlAndSendsJsonBodyWithoutMapId()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/movement", 200, EnvelopeOk);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var req = new MoveRequest
            {
                roleId = 7,
                posX = 320,
                posY = 450,
            };
            var resp = await backend.MoveAsync(req);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual(7, resp.data.roleId);
            Assert.AreEqual(1, resp.data.mapId); // server giữ mapId cũ
            Assert.AreEqual(320, resp.data.posX);
            Assert.AreEqual(450, resp.data.posY);

            // URL đã gửi: POST tới base + /v1/movement
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("POST", fake.Sent[0].Method);
            Assert.AreEqual($"{BaseUrl}/v1/movement", fake.Sent[0].Url);

            // Body JSON: camelCase, đủ 3 field (roleId/posX/posY), KHÔNG có mapId
            string body = fake.Sent[0].Body;
            StringAssert.Contains("\"roleId\":7", body);
            StringAssert.Contains("\"posX\":320", body);
            StringAssert.Contains("\"posY\":450", body);
            StringAssert.DoesNotContain("\"mapId\"", body,
                "MoveAsync phải KHÔNG gửi mapId — endpoint movement cập nhật runtime, không đổi map");
        }

        [Test]
        public async Task Rest_NullRequest_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            // Không gửi request gì cả khi request=null
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_RoleIdZero_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 0,
                posX = 100,
                posY = 100,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count, "Không gửi request khi roleId=0");
        }

        [Test]
        public async Task Rest_NegativeRoleId_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = -3,
                posX = 100,
                posY = 100,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task Rest_NegativePosX_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 7,
                posX = -1,
                posY = 100,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count, "Không gửi request khi posX<0");
        }

        [Test]
        public async Task Rest_NegativePosY_ReturnsInvalidArgFailure()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 7,
                posX = 100,
                posY = -5,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count, "Không gửi request khi posY<0");
        }

        [Test]
        public async Task Rest_ZeroPosXPosY_IsValidRequest()
        {
            // posX/posY=0 hợp lệ (mặc định backend Pydantic). Edge case để pin
            // rằng validation chỉ chặn âm, không chặn 0.
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/movement", 200, EnvelopeOk);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 7,
                posX = 0,
                posY = 0,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, fake.Sent.Count, "posX/posY=0 phải gửi request");
        }

        [Test]
        public async Task Rest_HttpError404_ReturnsFailure()
        {
            // Movement 404 — role chưa có scene (chưa enter_map).
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/movement", 404,
                "{\"code\":\"err_invalid_scene_id\",\"message\":\"role chưa có scene\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 999, posX = 100, posY = 100,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task Rest_HttpError500_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/movement", 500, "{\"detail\":\"internal\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 1, posX = 0, posY = 0,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task Rest_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("POST", "/v1/movement", new Exception("dns fail"));
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 1, posX = 0, posY = 0,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task Rest_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/movement", 200, "not json at all");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 1, posX = 0, posY = 0,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        // -------- MockGameBackend --------

        [Test]
        public async Task Mock_ReturnsSceneResponseEchoingPosFromRequest()
        {
            var backend = new MockGameBackend(NewMockConfig());

            var req = new MoveRequest
            {
                roleId = 7,
                posX = 320,
                posY = 450,
            };
            var resp = await backend.MoveAsync(req);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual("Mock", resp.message);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(7, resp.data.roleId);
            // Mock hardcode mapId=1 (Phượng Tường, parity với GetMapPosition mock).
            // Rest mới giữ mapId cũ từ server — chỉ test này pin mock behavior.
            Assert.AreEqual(1, resp.data.mapId);
            Assert.AreEqual(320, resp.data.posX);
            Assert.AreEqual(450, resp.data.posY);
        }

        [Test]
        public async Task Mock_ZeroPosition_IsValidRequest()
        {
            var backend = new MockGameBackend(NewMockConfig());

            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 5,
                posX = 0,
                posY = 0,
            });

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(0, resp.data.posX);
            Assert.AreEqual(0, resp.data.posY);
        }

        [Test]
        public async Task Mock_NullRequest_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.MoveAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_RoleIdZero_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 0, posX = 100, posY = 100,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_NegativeRoleId_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = -1, posX = 100, posY = 100,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_NegativePosX_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 1, posX = -1, posY = 100,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_NegativePosY_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.MoveAsync(new MoveRequest
            {
                roleId = 1, posX = 100, posY = -10,
            });

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        // -------- BackendClient facade --------

        [Test]
        public async Task BackendClient_Mock_DelegatesMoveAsyncToMock()
        {
            var client = new BackendClient(NewMockConfig());

            var resp = await client.MoveAsync(new MoveRequest
            {
                roleId = 7,
                posX = 1500,
                posY = 1800,
            });

            Assert.IsTrue(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("Mock", resp.message);
            Assert.AreEqual(7, resp.data.roleId);
            Assert.AreEqual(1500, resp.data.posX);
            Assert.AreEqual(1800, resp.data.posY);
        }

        [Test]
        public async Task BackendClient_Rest_DelegatesMoveAsyncToRest()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/movement", 200, EnvelopeOk);
            var client = new BackendClient(NewRestConfig(), fake);

            var resp = await client.MoveAsync(new MoveRequest
            {
                roleId = 7,
                posX = 320,
                posY = 450,
            });

            Assert.IsFalse(client.IsMock);
            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, fake.Sent.Count, "BackendClient.Rest phải gửi 1 request");
            Assert.AreEqual($"{BaseUrl}/v1/movement", fake.Sent[0].Url);
        }

        [Test]
        public async Task BackendClient_NullRequest_ReturnsInvalidArg()
        {
            var client = new BackendClient(NewMockConfig());
            var resp = await client.MoveAsync(null);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }
    }
}
