// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — AuthRestGameBackendTests
// Test cho 3 endpoint FS-02B trên RestGameBackend (dùng FakeHttpTransport).
// Mỗi endpoint cover: happy + 4xx + 5xx + malformed JSON.
//
// Pin contract FS-02A (commit 1625566 backend, branch main):
//   POST /v1/account/login body = { accName, password PLAINTEXT, otp?, clientIp? }
//     → { code, message, data: { accName, serviceFlag, extPoint } }
//   GET  /v1/role/by-account/{accName} → { account, roles: [...] }
//   GET  /v1/player/by-role/{roleId}   → { id, roleId, level, exp, ... }
// -----------------------------------------------------------------------------

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class AuthRestGameBackendTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig NewConfig() => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = false,
            defaultTimeoutSeconds = 5,
        };

        // ============================================================
        // LoginAsync
        // ============================================================

        [Test]
        public async Task LoginAsync_HappyPath_SendsPostWithPlaintextPassword()
        {
            var fake = new FakeHttpTransport();
            // Match response 03_login_success.json trong FS-02A evidence.
            const string body = "{\"code\":\"200\",\"message\":\"Success\"," +
                                "\"data\":{\"accName\":\"alice\",\"serviceFlag\":0,\"extPoint\":0}}";
            fake.QueueResponse("POST", "/v1/account/login", 200, body);
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "hunter2");

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual("Success", resp.message);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual("alice", resp.data.accName);
            Assert.AreEqual(0, resp.data.serviceFlag);
            Assert.AreEqual(0, resp.data.extPoint);

            // Verify request shape: POST, đúng URL, body JSON có accName+password
            // PLAINTEXT (không MD5), KHÔNG có Authorization header.
            Assert.AreEqual(1, fake.Sent.Count);
            var sent = fake.Sent[0];
            Assert.AreEqual("POST", sent.Method);
            StringAssert.Contains("/v1/account/login", sent.Url);
            Assert.IsNotNull(sent.Body, "POST body phải có JSON");
            Assert.IsTrue(sent.Body.Contains("\"accName\":\"alice\""),
                $"body phải chứa accName; got {sent.Body}");
            Assert.IsTrue(sent.Body.Contains("\"password\":\"hunter2\""),
                $"body phải chứa password PLAINTEXT; got {sent.Body}");
            // Server lưu MD5-IN-HOA, client KHÔNG gửi hash trước → assert
            // KHÔNG có field hash-y kiểu md5/MD5 trong body.
            Assert.IsFalse(sent.Body.Contains("md5") || sent.Body.Contains("MD5"),
                $"password phải plaintext, KHÔNG MD5; got {sent.Body}");
        }

        [Test]
        public async Task LoginAsync_OmitsOtpAndClientIpWhenNull()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"accName\":\"alice\",\"serviceFlag\":0,\"extPoint\":0}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "pw");

            Assert.IsTrue(resp.IsSuccess);
            var body = fake.Sent[0].Body;
            // extra=forbid ở backend → KHÔNG gửi field optional khi null.
            Assert.IsFalse(body.Contains("\"otp\""), $"otp phải vắng khi null; got {body}");
            Assert.IsFalse(body.Contains("\"clientIp\""), $"clientIp phải vắng khi null; got {body}");
        }

        [Test]
        public async Task LoginAsync_SendsOtpWhenProvided()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"accName\":\"alice\",\"serviceFlag\":0,\"extPoint\":0}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            await backend.LoginAsync("alice", "pw", otp: "123456");

            Assert.IsTrue(fake.Sent[0].Body.Contains("\"otp\":\"123456\""));
        }

        [Test]
        public async Task LoginAsync_401_ReturnsFailureWithCode()
        {
            var fake = new FakeHttpTransport();
            // Match response 04_login_wrong_pw.json: backend trả bare
            // `{"detail": "..."}` (FastAPI HTTPException default shape), không
            // có envelope DataResponse. Caller vẫn phải đọc được code="401".
            fake.QueueResponse("POST", "/v1/account/login", 401,
                "{\"detail\":\"Tên đăng nhập hoặc mật khẩu không đúng\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "wrong");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("401", resp.code);
            // Message phải chứa text backend trả (hoặc fallback "http 401").
            Assert.IsTrue(resp.message.Contains("401") || resp.message.Contains("Tên"));
        }

        [Test]
        public async Task LoginAsync_422_ValidationError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 422,
                "{\"detail\":[{\"loc\":[\"body\",\"accName\"],\"msg\":\"field required\"}]}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("422", resp.code);
        }

        [Test]
        public async Task LoginAsync_500_ServerError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 500,
                "{\"detail\":\"internal server error\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task LoginAsync_429_RateLimit()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 429, "{\"detail\":\"too many\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("429", resp.code);
        }

        [Test]
        public async Task LoginAsync_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 200, "<html>not json</html>");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        [Test]
        public async Task LoginAsync_EmptyBody_ReturnsEmptyBody()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 200, "");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("empty_body", resp.code);
        }

        [Test]
        public async Task LoginAsync_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("POST", "/v1/account/login", new Exception("dns"));
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task LoginAsync_EmptyAccName_ValidationErrorBeforeSend()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("", "pw");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count, "không được gửi request khi validation fail");
        }

        [Test]
        public async Task LoginAsync_EmptyPassword_ValidationErrorBeforeSend()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.LoginAsync("alice", "");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        // ============================================================
        // ListRolesAsync
        // ============================================================

        [Test]
        public async Task ListRolesAsync_HappyPath_EmptyRoles()
        {
            var fake = new FakeHttpTransport();
            // Match response 06_list_roles_empty.json.
            const string body = "{\"code\":\"200\",\"message\":\"Success\"," +
                                "\"data\":{\"account\":\"alice\",\"roles\":[]}}";
            fake.QueueResponse("GET", "/v1/role/by-account/alice", 200, body);
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListRolesAsync("alice");

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual("alice", resp.data.account);
            Assert.IsNotNull(resp.data.roles);
            Assert.AreEqual(0, resp.data.roles.Count, "account mới chưa tạo role");

            // URL đã gửi: base + /v1/role/by-account/alice (không encode, đã là ASCII).
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("GET", fake.Sent[0].Method);
            StringAssert.Contains("/v1/role/by-account/alice", fake.Sent[0].Url);
        }

        [Test]
        public async Task ListRolesAsync_HappyPath_OneRoleWithFactionName()
        {
            var fake = new FakeHttpTransport();
            // Match response 08_list_roles_one.json — factionName tiếng Việt
            // (UTF-8) phải deserialize đúng.
            const string body = "{\"code\":\"200\",\"message\":\"Success\"," +
                                "\"data\":{\"account\":\"alice\"," +
                                "\"roles\":[{\"id\":2,\"roleName\":\"Kiem_Khach\"," +
                                "\"account\":\"alice\",\"faction\":0," +
                                "\"factionName\":\"Thiếu Lâm\",\"level\":1}]}}";
            fake.QueueResponse("GET", "/v1/role/by-account/alice", 200, body);
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListRolesAsync("alice");

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(1, resp.data.roles.Count);
            var role = resp.data.roles[0];
            Assert.AreEqual(2, role.id);
            Assert.AreEqual("Kiem_Khach", role.roleName);
            Assert.AreEqual("alice", role.account);
            Assert.AreEqual(0, role.faction);
            Assert.AreEqual("Thiếu Lâm", role.factionName, "factionName UTF-8 phải decode đúng");
            Assert.AreEqual(1, role.level);
        }

        [Test]
        public async Task ListRolesAsync_UrlEncodesAccNameWithUnicode()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "T%C3%AAn_C%C3%B3_D%E1%BA%A5u", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"account\":\"Tên_Có_Dấu\",\"roles\":[]}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListRolesAsync("Tên_Có_Dấu");

            Assert.IsTrue(resp.IsSuccess);
            // URL đã gửi phải chứa ký tự percent-encoded (không raw UTF-8).
            StringAssert.Contains("%", fake.Sent[0].Url,
                "accName có ký tự đặc biệt phải được URL-encode");
            // KHÔNG được gửi raw UTF-8 (server FastAPI vẫn accept nhưng convention
            // là encode để log/proxy middlebox không bị mojibake).
            Assert.IsFalse(fake.Sent[0].Url.Contains("Tên"),
                "URL phải encode accName, không raw UTF-8; got " + fake.Sent[0].Url);
        }

        [Test]
        public async Task ListRolesAsync_404_AccountNotFound()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/role/by-account/ghost", 404,
                "{\"detail\":\"account not found\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListRolesAsync("ghost");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task ListRolesAsync_500_ServerError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/role/by-account/alice", 500,
                "{\"detail\":\"oops\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListRolesAsync("alice");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task ListRolesAsync_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/role/by-account/alice", 200, "{not json");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListRolesAsync("alice");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        [Test]
        public async Task ListRolesAsync_EmptyAccName_ValidationErrorBeforeSend()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListRolesAsync("");

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        // ============================================================
        // GetPlayerStateAsync
        // ============================================================

        [Test]
        public async Task GetPlayerStateAsync_HappyPath()
        {
            var fake = new FakeHttpTransport();
            // Match response 10_get_player_state.json — stat Kim mặc định
            // 35/25/25/15 (parity với backend task_head.lua:79-82).
            const string body = "{\"code\":\"200\",\"message\":\"Success\"," +
                                "\"data\":{\"id\":1,\"roleId\":2,\"level\":1,\"exp\":0," +
                                "\"transLife\":0,\"freePoint\":0,\"magicPoint\":0," +
                                "\"strength\":35,\"dexterity\":25,\"vitality\":25,\"spirit\":15," +
                                "\"series\":0,\"money\":0,\"repute\":0}}";
            fake.QueueResponse("GET", "/v1/player/by-role/2", 200, body);
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetPlayerStateAsync(2);

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(1, resp.data.id);
            Assert.AreEqual(2, resp.data.roleId);
            Assert.AreEqual(1, resp.data.level);
            Assert.AreEqual(0, resp.data.exp);
            Assert.AreEqual(35, resp.data.strength);
            Assert.AreEqual(25, resp.data.dexterity);
            Assert.AreEqual(25, resp.data.vitality);
            Assert.AreEqual(15, resp.data.spirit);
            Assert.AreEqual(0, resp.data.series);

            // URL đã gửi.
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("GET", fake.Sent[0].Method);
            StringAssert.Contains("/v1/player/by-role/2", fake.Sent[0].Url);
        }

        [Test]
        public async Task GetPlayerStateAsync_404_NoPlayerYet()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/player/by-role/999", 404,
                "{\"detail\":\"player state not found\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetPlayerStateAsync(999);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("404", resp.code);
        }

        [Test]
        public async Task GetPlayerStateAsync_500_ServerError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/player/by-role/2", 500, "{\"detail\":\"oops\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetPlayerStateAsync(2);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task GetPlayerStateAsync_MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/player/by-role/2", 200, "[]"); // array, không phải object
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetPlayerStateAsync(2);

            Assert.IsFalse(resp.IsSuccess);
            // data=null → parse_error hoặc backend trả về object rỗng tùy parser;
            // kiểm tra 1 trong 2 mã lỗi chấp nhận được.
            Assert.IsTrue(resp.code == "parse_error" || resp.code == "200",
                $"code phải parse_error hoặc 200; got {resp.code}");
        }

        [Test]
        public async Task GetPlayerStateAsync_NonPositiveRoleId_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetPlayerStateAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        [Test]
        public async Task GetPlayerStateAsync_NegativeRoleId_ValidationError()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetPlayerStateAsync(-5);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("validation_error", resp.code);
            Assert.AreEqual(0, fake.Sent.Count);
        }

        // ============================================================
        // Cancellation
        // ============================================================

        [Test]
        public async Task LoginAsync_CancelledToken_DoesNotThrow()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("POST", "/v1/account/login", 200,
                "{\"code\":\"200\",\"message\":\"Success\"," +
                "\"data\":{\"accName\":\"alice\",\"serviceFlag\":0,\"extPoint\":0}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Phải KHÔNG throw — chỉ trả cancelled hoặc response.
            var resp = await backend.LoginAsync("alice", "pw", ct: cts.Token);
            Assert.IsTrue(resp.code == "cancelled" || resp.code == "200",
                $"code phải cancelled hoặc 200; got {resp.code}");
        }
    }
}
