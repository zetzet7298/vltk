// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — BackendConfigTests
// Smoke test cho BackendConfig: URL resolution, JSON override, default values.
// Chạy trong EditMode (VLTK_ENABLE_TESTS define).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Backend;

namespace VLTK.Tests.Backend
{
    public class BackendConfigTests
    {
        [Test]
        public void Defaults_AreOfflineSafe()
        {
            // BackendConfig.LoadOrDefault phải luôn trả về một instance
            // (kể cả khi Resources/BackendConfig.asset chưa tồn tại).
            var cfg = BackendConfig.LoadOrDefault();
            Assert.IsNotNull(cfg, "LoadOrDefault must never return null");
            Assert.IsTrue(cfg.useMock, "useMock default phải là true (offline-first)");
            Assert.IsFalse(string.IsNullOrEmpty(cfg.baseUrl), "baseUrl phải có giá trị mặc định");
            Assert.IsTrue(cfg.baseUrl.Contains("8020"), "baseUrl mặc định phải trỏ về cổng 8020");
        }

        [Test]
        public void ResolveApiUrl_ConcatenatesPath()
        {
            var cfg = new BackendConfig
            {
                baseUrl = "http://127.0.0.1:8020",
                apiPrefix = "/v1",
            };
            // Đường dẫn thông thường
            Assert.AreEqual("http://127.0.0.1:8020/v1/map", cfg.ResolveApiUrl("map"));
            // Input có dấu '/' thừa ở đầu — phải được chuẩn hoá
            Assert.AreEqual("http://127.0.0.1:8020/v1/map", cfg.ResolveApiUrl("/map"));
            // Input có dấu '/' thừa ở cuối baseUrl
            Assert.AreEqual("http://127.0.0.1:8020/v1/map",
                new BackendConfig { baseUrl = "http://127.0.0.1:8020/", apiPrefix = "/v1" }
                .ResolveApiUrl("map"));
        }

        [Test]
        public void ResolveApiUrl_HandlesEmptyApiPrefix()
        {
            var cfg = new BackendConfig
            {
                baseUrl = "http://127.0.0.1:8020",
                apiPrefix = "",
            };
            // Khi apiPrefix rỗng, chỉ ghép base + path
            Assert.AreEqual("http://127.0.0.1:8020/map", cfg.ResolveApiUrl("map"));
        }

        [Test]
        public void ResolveRootUrl_ForHealth()
        {
            var cfg = new BackendConfig
            {
                baseUrl = "http://127.0.0.1:8020",
            };
            // /health nằm ngoài /v1, dùng ResolveRootUrl
            Assert.AreEqual("http://127.0.0.1:8020/health", cfg.ResolveRootUrl("health"));
        }

        [Test]
        public void ApplyJsonOverride_UpdatesFields()
        {
            var cfg = new BackendConfig
            {
                baseUrl = "http://default:8020",
                apiPrefix = "/v1",
                useMock = true,
            };
            const string json = "{\"baseUrl\":\"http://10.0.0.5:8020\",\"useMock\":false}";
            cfg.ApplyJsonOverride(json);
            Assert.AreEqual("http://10.0.0.5:8020", cfg.baseUrl, "baseUrl phải bị override");
            Assert.IsFalse(cfg.useMock, "useMock phải bị override thành false");
        }

        [Test]
        public void ApplyJsonOverride_IgnoresEmptyValues()
        {
            var cfg = new BackendConfig
            {
                baseUrl = "http://keep:8020",
                useMock = true,
            };
            // baseUrl rỗng trong JSON → giữ giá trị cũ
            cfg.ApplyJsonOverride("{\"baseUrl\":\"\"}");
            Assert.AreEqual("http://keep:8020", cfg.baseUrl);
            Assert.IsTrue(cfg.useMock, "useMock phải giữ mặc định khi JSON không chứa key");
        }
    }
}
