// -----------------------------------------------------------------------------
// VLTK.Backend — BackendConfig
// Cấu hình backend (base URL, useMock, timeout). Là ScriptableObject để editor
// authoring, đồng thời hỗ trợ runtime override qua JSON trong StreamingAssets/.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;

namespace VLTK.Backend
{
    /// <summary>
    /// Cấu hình tĩnh cho IGameBackend. Mặc định useMock=true để SandboxManager
    /// (runtime offline hiện tại) tiếp tục chạy mà không cần backend thật.
    /// </summary>
    [CreateAssetMenu(menuName = "VLTK/Backend Config", fileName = "BackendConfig")]
    public sealed class BackendConfig : ScriptableObject
    {
        /// <summary>Đường dẫn Resources mặc định (file Assets/Resources/BackendConfig.asset).</summary>
        public const string DefaultResourcePath = "BackendConfig";

        /// <summary>Đường dẫn JSON override trong StreamingAssets/.</summary>
        public const string StreamingAssetsOverridePath = "BackendConfig.json";

        [Header("Endpoints")]
        [Tooltip("Base URL của FastAPI game server (không bao gồm /v1). Mặc định localhost cổng 8020.")]
        public string baseUrl = "http://127.0.0.1:8020";

        [Tooltip("API prefix, mặc định /v1 theo convention của backend.")]
        public string apiPrefix = "/v1";

        [Tooltip("Timeout mặc định cho mỗi HTTP request, đơn vị giây.")]
        [Min(1)]
        public int defaultTimeoutSeconds = 10;

        [Header("Behavior")]
        [Tooltip("Nếu true, dùng MockGameBackend (offline). Nếu false, dùng RestGameBackend (HTTP thật).")]
        public bool useMock = true;

        /// <summary>
        /// Tải BackendConfig từ Resources/BackendConfig.asset; nếu chưa có thì
        /// tạo instance mặc định. KHÔNG ném exception — caller phải tự kiểm
        /// tra IsConfigured.
        /// </summary>
        public static BackendConfig LoadOrDefault()
        {
            var asset = Resources.Load<BackendConfig>(DefaultResourcePath);
            if (asset != null) return asset;
            return CreateInstance<BackendConfig>();
        }

        /// <summary>
        /// Áp dụng override từ StreamingAssets/BackendConfig.json nếu tồn tại.
        /// Hỗ trợ các field: baseUrl, apiPrefix, defaultTimeoutSeconds, useMock.
        /// Trả về config đã áp dụng (chính `this`).
        /// </summary>
        public BackendConfig ApplyStreamingAssetsOverrideIfPresent()
        {
            string path = Path.Combine(Application.streamingAssetsPath, StreamingAssetsOverridePath);
            if (!File.Exists(path)) return this;
            try
            {
                string json = File.ReadAllText(path);
                ApplyJsonOverride(json);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[VLTK.Backend] Không đọc được override JSON tại '{path}': {ex.Message}");
            }
            return this;
        }

        /// <summary>
        /// Áp dụng JSON override. Dùng JsonUtility để giữ field naming đơn giản
        /// (lowercase, không lồng nhau). Các field không xuất hiện trong JSON
        /// sẽ giữ giá trị hiện tại.
        /// </summary>
        public void ApplyJsonOverride(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;
            var data = JsonUtility.FromJson<BackendConfigJson>(json);
            if (data == null) return;
            if (!string.IsNullOrEmpty(data.baseUrl)) baseUrl = data.baseUrl;
            if (!string.IsNullOrEmpty(data.apiPrefix)) apiPrefix = data.apiPrefix;
            if (data.defaultTimeoutSeconds > 0) defaultTimeoutSeconds = data.defaultTimeoutSeconds;
            // useMock: bool mặc định là false trong C#, nên chỉ override nếu JSON có chứa key
            if (json.Contains("\"useMock\"")) useMock = data.useMock;
        }

        /// <summary>
        /// Tạo URL đầy đủ cho một endpoint path. Ghép baseUrl + apiPrefix + path,
        /// đảm bảo đúng một dấu '/' giữa các phần.
        /// </summary>
        public string ResolveApiUrl(string path)
        {
            string b = (baseUrl ?? string.Empty).TrimEnd('/');
            string p = (apiPrefix ?? string.Empty).Trim('/');
            string s = (path ?? string.Empty).TrimStart('/');
            if (string.IsNullOrEmpty(p)) return $"{b}/{s}";
            if (string.IsNullOrEmpty(s)) return $"{b}/{p}";
            return $"{b}/{p}/{s}";
        }

        /// <summary>
        /// URL cho endpoint không thuộc /v1 (ví dụ /health).
        /// </summary>
        public string ResolveRootUrl(string path)
        {
            string b = (baseUrl ?? string.Empty).TrimEnd('/');
            string s = (path ?? string.Empty).TrimStart('/');
            return $"{b}/{s}";
        }

        /// <summary>Schema JSON cho override từ StreamingAssets.</summary>
        [System.Serializable]
        private sealed class BackendConfigJson
        {
            public string baseUrl;
            public string apiPrefix;
            public int defaultTimeoutSeconds;
            public bool useMock;
        }
    }
}
