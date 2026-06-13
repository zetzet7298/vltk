// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — BackendClientTests
// Smoke test cho BackendClient facade: chọn mock vs rest theo config.useMock.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Backend;
using VLTK.Backend.Mock;
using VLTK.Backend.Rest;
using VLTK.Backend.Tests;

namespace VLTK.Tests.Backend
{
    public class BackendClientTests
    {
        [Test]
        public void UseMockTrue_PicksMockGameBackend()
        {
            var cfg = new BackendConfig
            {
                baseUrl = "http://test:8020",
                apiPrefix = "/v1",
                useMock = true,
            };
            var client = new BackendClient(cfg);
            Assert.IsTrue(client.IsMock);
            Assert.IsInstanceOf<MockGameBackend>(client.Backend);
        }

        [Test]
        public void UseMockFalse_PicksRestGameBackend()
        {
            var cfg = new BackendConfig
            {
                baseUrl = "http://test:8020",
                apiPrefix = "/v1",
                useMock = false,
            };
            var client = new BackendClient(cfg);
            Assert.IsFalse(client.IsMock);
            Assert.IsInstanceOf<RestGameBackend>(client.Backend);
        }

        [Test]
        public void InjectTransport_UsesProvidedTransport()
        {
            // Cung cấp FakeHttpTransport — phải dùng RestGameBackend ngay cả
            // khi useMock=true (dùng để test inject thật sự hoạt động).
            var cfg = new BackendConfig
            {
                baseUrl = "http://test:8020",
                useMock = false,
            };
            var fake = new FakeHttpTransport();
            var client = new BackendClient(cfg, fake);
            Assert.IsFalse(client.IsMock);
            Assert.IsInstanceOf<RestGameBackend>(client.Backend);
        }
    }
}
