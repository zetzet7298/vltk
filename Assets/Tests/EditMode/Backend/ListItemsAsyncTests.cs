// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — ListItemsAsyncTests
// EditMode test cho slice FS-02C endpoint ListItemsAsync:
//   - Rest:  GET /v1/item/by-role/{role_id} → DataResponse[ItemListResponse]
//   - Mock:  trả về ItemListResponse với 2 vật phẩm cứng trong túi
//
// Phủ: URL build với roleId trong path, parse envelope + List<ItemResponse>
// camelCase, validation roleId <= 0, error paths.
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
    public class ListItemsAsyncTests
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

        // Envelope thật từ backend FastAPI: items[] với đủ field camelCase.
        private const string EnvelopeOk = "{" +
            "\"code\":\"200\"," +
            "\"message\":\"Success\"," +
            "\"data\":{" +
              "\"roleId\":42," +
              "\"items\":[" +
                "{\"id\":1,\"roleId\":42,\"genre\":2,\"detail\":1,\"particular\":1," +
                "\"level\":1,\"amount\":5,\"slot\":0,\"equipSlot\":-1,\"name\":\"Hồi Huyết Đan\"}," +
                "{\"id\":2,\"roleId\":42,\"genre\":0,\"detail\":1,\"particular\":12," +
                "\"level\":10,\"amount\":1,\"slot\":1,\"equipSlot\":-1,\"name\":\"Kiếm Phổ Thông\"}" +
              "]" +
            "}}";

        // -------- RestGameBackend --------

        [Test]
        public async Task Rest_BuildsGetUrlWithRoleIdInPath()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/item/by-role/42", 200, EnvelopeOk);
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.ListItemsAsync(42);

            Assert.IsTrue(resp.IsSuccess, $"IsSuccess phải true; code={resp.code} msg={resp.message}");
            Assert.AreEqual("200", resp.code);
            Assert.IsNotNull(resp.data);
            Assert.AreEqual(42, resp.data.roleId);
            Assert.IsNotNull(resp.data.items);
            Assert.AreEqual(2, resp.data.items.Count);

            // Item 0: Hồi Huyết Đan
            Assert.AreEqual(1, resp.data.items[0].id);
            Assert.AreEqual(2, resp.data.items[0].genre);
            Assert.AreEqual(1, resp.data.items[0].detail);
            Assert.AreEqual(1, resp.data.items[0].particular);
            Assert.AreEqual(1, resp.data.items[0].level);
            Assert.AreEqual(5, resp.data.items[0].amount);
            Assert.AreEqual(0, resp.data.items[0].slot);
            Assert.AreEqual(-1, resp.data.items[0].equipSlot);
            Assert.AreEqual("Hồi Huyết Đan", resp.data.items[0].name);

            // Item 1: Kiếm Phổ Thông
            Assert.AreEqual(2, resp.data.items[1].id);
            Assert.AreEqual(0, resp.data.items[1].genre);
            Assert.AreEqual(12, resp.data.items[1].particular);
            Assert.AreEqual(10, resp.data.items[1].level);
            Assert.AreEqual("Kiếm Phổ Thông", resp.data.items[1].name);

            // URL: GET base + /v1/item/by-role/42
            Assert.AreEqual(1, fake.Sent.Count);
            Assert.AreEqual("GET", fake.Sent[0].Method);
            Assert.AreEqual($"{BaseUrl}/v1/item/by-role/42", fake.Sent[0].Url);
            Assert.IsTrue(string.IsNullOrEmpty(fake.Sent[0].Body));
        }

        [Test]
        public async Task Rest_RoleIdZero_ReturnsInvalidArg()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.ListItemsAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
            Assert.AreEqual(0, fake.Sent.Count, "Không gửi request khi roleId=0");
        }

        [Test]
        public async Task Rest_NegativeRoleId_ReturnsInvalidArg()
        {
            var fake = new FakeHttpTransport();
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.ListItemsAsync(-1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Rest_HttpError500_ReturnsFailure()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/item/by-role/42", 500, "{\"detail\":\"db down\"}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.ListItemsAsync(42);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("500", resp.code);
        }

        [Test]
        public async Task Rest_TransportError_ReturnsTransportError()
        {
            var fake = new FakeHttpTransport();
            fake.QueueTransportError("GET", "/v1/item/by-role/", new Exception("dns failure"));
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.ListItemsAsync(1);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("transport_error", resp.code);
        }

        [Test]
        public async Task Rest_EmptyBag_ReturnsEmptyList()
        {
            var fake = new FakeHttpTransport();
            fake.QueueResponse("GET", "/v1/item/by-role/100", 200,
                "{\"code\":\"200\",\"message\":\"Success\",\"data\":{\"roleId\":100,\"items\":[]}}");
            var backend = new RestGameBackend(NewRestConfig(), fake);

            var resp = await backend.ListItemsAsync(100);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual(100, resp.data.roleId);
            Assert.IsNotNull(resp.data.items);
            Assert.AreEqual(0, resp.data.items.Count);
        }

        // -------- MockGameBackend --------

        [Test]
        public async Task Mock_ReturnsSeededBag()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.ListItemsAsync(42);

            Assert.IsTrue(resp.IsSuccess);
            Assert.AreEqual("Mock", resp.message);
            Assert.AreEqual(42, resp.data.roleId);
            Assert.IsNotNull(resp.data.items);
            Assert.GreaterOrEqual(resp.data.items.Count, 1);
            // Tất cả items thuộc role 42
            Assert.IsTrue(resp.data.items.TrueForAll(it => it.roleId == 42));
            // Tên tiếng Việt không rỗng
            Assert.IsTrue(resp.data.items.TrueForAll(it => !string.IsNullOrEmpty(it.name)));
            // equipSlot mặc định = -1 cho mọi item
            Assert.IsTrue(resp.data.items.TrueForAll(it => it.equipSlot == -1));
        }

        [Test]
        public async Task Mock_RoleIdZero_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.ListItemsAsync(0);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }

        [Test]
        public async Task Mock_NegativeRoleId_ReturnsInvalidArg()
        {
            var backend = new MockGameBackend(NewMockConfig());
            var resp = await backend.ListItemsAsync(-99);

            Assert.IsFalse(resp.IsSuccess);
            Assert.AreEqual("invalid_arg", resp.code);
        }
    }
}
