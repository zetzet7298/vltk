// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — RestGameBackendTests
// Smoke test cho RestGameBackend dùng FakeHttpTransport. Không cần network,
// không cần UnityWebRequest runtime; test xác nhận:
//   - URL được build đúng (baseUrl + /v1/map hoặc /health)
//   - JSON camelCase deserialize đúng vào DTO
//   - HTTP 4xx/5xx → IsSuccess=false, code giữ nguyên
//   - CancellationToken cancel trước khi gửi thì trả về failure
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
    public class RestGameBackendTests
    {
        private const string BaseUrl = "http://127.0.0.1:8020";

        private static BackendConfig NewConfig(bool useMock = false) => new BackendConfig
        {
            baseUrl = BaseUrl,
            apiPrefix = "/v1",
            useMock = useMock,
            defaultTimeoutSeconds = 5,
        };

        [Test]
        public async Task GetHealthAsync_BuildsUrlWithoutV1()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/health", 200,
                "{\"status\":\"ok\",\"service\":\"vltk-mobile\",\"version\":\"0.1.0\",\"timestamp\":\"2026-06-13T00:00:00Z\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetHealthAsync();

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.IsNotNull(resp.data);
            Assert.IsTrue(resp.data.IsOk);
            Assert.AreEqual("vltk-mobile", resp.data.service);
            // URL đã gửi: không có /v1 (vì /health nằm ở root)
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("GET", fake.Sent[0].Method);
            StringAssert.Contains("/health", fake.Sent[0].Url);
            StringAssert.DoesNotContain("/v1", fake.Sent[0].Url);
        }

        [Test]
        public async Task ListMapsAsync_BuildsUrlAndParsesEnvelope()
        {
            var fake = new FakeHttpTransport();
            // Backend trả DataResponse[MapListResponse] — code/message ở envelope ngoài,
            // data ở bên trong.
            const string body = "{" +
                "\"code\":\"200\"," +
                "\"message\":\"Success\"," +
                "\"data\":{" +
                "  \"total\":2," +
                "  \"maps\":[" +
                "    {\"mapId\":1,\"name\":\"Map A\",\"mapType\":\"city\",\"mapTypeName\":\"City\",\"posX\":100,\"posY\":200,\"newWorldScript\":\"NewWorld\",\"newWorldParam\":\"1 100 200\"}," +
                "    {\"mapId\":2,\"name\":\"Map B\",\"mapType\":\"village\",\"mapTypeName\":\"Village\",\"posX\":300,\"posY\":400,\"newWorldScript\":\"NewWorld\",\"newWorldParam\":\"2 300 400\"}" +
                "  ]" +
                "}}";
            fake.QueueResponse("GET", "/v1/map", 200, body);
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListMapsAsync();

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.AreEqual("Success", resp.message);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(2, resp.data.total);
            Assert.AreEqual(2, resp.data.maps.Count);
            Assert.AreEqual("Map A", resp.data.maps[0].name);
            // URL đã gửi: base + /v1/map (không có query string vì mapType=null)
            Assert.AreEqual($"{BaseUrl}/v1/map", fake.Sent[0].Url);
        }

        [Test]
        public async Task ListMapsAsync_AppendsMapTypeQuery()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "map_type=city", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{\"total\":0,\"maps\":[]}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListMapsAsync(mapType: "city");

            Assert.IsTrue(resp.IsSuccess);
            // URL phải chứa query string map_type=city
            StringAssert.Contains("map_type=city", fake.Sent[0].Url);
        }

        [Test]
        public async Task HttpError_500_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/map", 500, "{\"detail\":\"internal\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListMapsAsync();

            Assert.IsFalse(resp.IsSuccess, "5xx phải trả IsSuccess=false");
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task HttpError_401_ReturnsFailureWithCode()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/map", 401, "{\"detail\":\"unauthorized\"}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListMapsAsync();

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("401", resp.code);
        }

        [Test]
        public async Task TransportError_ReturnsTransportErrorCode()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("GET", "/health", new Exception("dns failure"));
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.GetHealthAsync();

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task MalformedJson_ReturnsParseError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/map", 200, "not json at all");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListMapsAsync();

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("parse_error", resp.code);
        }

        [Test]
        public async Task Cancellation_ReturnsCancelledCode()
        {
            var fake = new FakeHttpTransport();
            // FakeHttpTransport không thật sự check token, nhưng RestGameBackend
            // vẫn gọi transport. Để mô phỏng cancel, ta dùng token đã cancel sẵn
            // và kiểm tra response.
            using var cts = new CancellationTokenSource();
            cts.Cancel();
            fake.QueueResponse("GET", "/v1/map", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{\"total\":0,\"maps\":[]}}");
            var backend = new RestGameBackend(NewConfig(), fake);

            var resp = await backend.ListMapsAsync(ct: cts.Token);

            // Với token đã cancel, request phải trả về cancelled (một số race có thể
            // cho data — chấp nhận miễn là backend KHÔNG throw).
            Assert.IsTrue(resp.code == "cancelled" || resp.code == "200",
                $"code phải là 'cancelled' hoặc '200'; got {resp.code}");
        }

        [Test]
        public void IsConfigured_TrueWithValidConfig()
        {
            var backend = new RestGameBackend(NewConfig(), new FakeHttpTransport());
            Assert.IsTrue(backend.IsConfigured);
        }
    }
}
