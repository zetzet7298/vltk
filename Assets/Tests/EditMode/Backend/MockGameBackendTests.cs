// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — MockGameBackendTests
// Smoke test cho MockGameBackend: trả về IsSuccess=true với data hợp lệ cho cả
// /health và /v1/map, không cần network.
// -----------------------------------------------------------------------------

using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Mock;

namespace VLTK.Tests.Backend
{
    public class MockGameBackendTests
    {
        private static BackendConfig NewConfig() => new BackendConfig
        {
            baseUrl = "http://test:8020",
            apiPrefix = "/v1",
            useMock = true,
        };

        [Test]
        public async Task GetHealthAsync_ReturnsOk()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.GetHealthAsync();

            Assert.IsTrue(resp.IsSuccess, "Mock phải trả IsSuccess=true");
            Assert.IsNotNull(resp.data, "data không được null");
            Assert.IsTrue(resp.data.IsOk, "status phải là 'ok'");
            Assert.AreEqual("mock", resp.data.service, "service phải đánh dấu là 'mock'");
        }

        [Test]
        public async Task ListMapsAsync_ReturnsSeededMaps()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.ListMapsAsync();

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            Assert.GreaterOrEqual(resp.data.total, 1, "Mock phải trả ít nhất 1 map");
            Assert.IsNotNull(resp.data.maps);
            Assert.GreaterOrEqual(resp.data.maps.Count, 1);
            // Tên tiếng Việt — kiểm tra chuỗi không rỗng
            Assert.IsTrue(resp.data.maps.All(m => !string.IsNullOrEmpty(m.name)));
        }

        [Test]
        public async Task ListMapsAsync_FilterByMapType()
        {
            var backend = new MockGameBackend(NewConfig());
            var resp = await backend.ListMapsAsync(mapType: "city");

            Assert.IsTrue(resp.IsSuccess);
            Assert.IsNotNull(resp.data);
            // Mọi map trả về phải có mapType khớp filter
            Assert.IsTrue(resp.data.maps.All(m => m.mapType == "city"));
        }

        [Test]
        public void IsConfigured_TrueWithConfig()
        {
            var backend = new MockGameBackend(NewConfig());
            Assert.IsTrue(backend.IsConfigured);
        }
    }
}
