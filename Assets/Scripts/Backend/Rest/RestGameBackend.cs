// -----------------------------------------------------------------------------
// VLTK.Backend.Rest — RestGameBackend
// Implementation production của IGameBackend. Mọi call HTTP đi qua
// IHttpTransport — nhờ đó EditMode test có thể thay thế bằng FakeHttpTransport
// (xem VLTK.Backend.Tests).
//
// Slice FS-01D chỉ có 2 endpoint:
//   - GET  /health  (root, không có /v1)
//   - GET  /v1/map  (danh mục bản đồ)
// Endpoint nào ngoài 2 này chưa được port; mở rộng dần trong các slice sau.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using VLTK.Backend.Dto;
using VLTK.Core;

namespace VLTK.Backend.Rest
{
    /// <summary>
    /// Implementation production của IGameBackend dựa trên HTTP REST.
    /// </summary>
    public sealed class RestGameBackend : IGameBackend
    {
        private const string Subsystem = "Backend.Rest";

        private readonly IHttpTransport _transport;
        private readonly Dictionary<string, string> _defaultHeaders = new();

        public BackendConfig Config { get; }
        public bool IsConfigured => Config != null && !string.IsNullOrEmpty(Config.baseUrl);

        public RestGameBackend(BackendConfig config, IHttpTransport transport)
        {
            Config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _transport = transport != null ? transport : throw new ArgumentNullException(nameof(transport));
        }

        public Task<BackendResponse<HealthResponse>> GetHealthAsync(CancellationToken ct = default)
        {
            string url = Config.ResolveRootUrl("health");
            return ExecuteAsync<HealthResponse>(
                method: "GET",
                url: url,
                queryParams: null,
                bodyJson: null,
                isEnvelope: false, // /health trả dict thuần
                ct: ct);
        }

        public Task<BackendResponse<MapListResponse>> ListMapsAsync(
            string mapType = null, CancellationToken ct = default)
        {
            string url = Config.ResolveApiUrl("map");
            Dictionary<string, string> q = null;
            if (!string.IsNullOrEmpty(mapType))
            {
                q = new Dictionary<string, string> { { "map_type", mapType } };
            }
            return ExecuteAsync<MapListResponse>(
                method: "GET",
                url: url,
                queryParams: q,
                bodyJson: null,
                isEnvelope: true, // /v1/map trả DataResponse[MapListResponse]
                ct: ct);
        }

        // -------- internal helpers --------

        /// <summary>
        /// Hàm chung cho cả endpoint envelope và non-envelope. Khi isEnvelope=true
        /// ta parse `data` con từ BackendResponse; ngược lại body chính là data.
        /// </summary>
        private async Task<BackendResponse<T>> ExecuteAsync<T>(
            string method,
            string url,
            IDictionary<string, string> queryParams,
            string bodyJson,
            bool isEnvelope,
            CancellationToken ct)
        {
            string finalUrl = AppendQueryString(url, queryParams);
            HttpRequest request = new HttpRequest(
                method: method,
                url: finalUrl,
                bodyJson: bodyJson,
                headers: _defaultHeaders,
                timeoutSeconds: Config != null ? Config.defaultTimeoutSeconds : 10);

            HttpTransportResult result;
            try
            {
                result = await _transport.SendAsync(request, ct);
            }
            catch (OperationCanceledException)
            {
                return BackendResponse<T>.Failure("cancelled", "request cancelled");
            }
            catch (Exception ex)
            {
                SubsystemLog.Error(Subsystem, $"{method} {finalUrl} threw {ex.GetType().Name}: {ex.Message}");
                return BackendResponse<T>.Failure("transport_error", ex.Message);
            }

            if (result.HasError)
            {
                return BackendResponse<T>.Failure("transport_error", result.ErrorMessage ?? "unknown");
            }
            if (!result.IsHttpSuccess)
            {
                return BackendResponse<T>.Failure(
                    result.StatusCode.ToString(),
                    $"http {result.StatusCode}",
                    result.TransportError);
            }

            return ParseBody<T>(result.Body, isEnvelope);
        }

        /// <summary>
        /// Parse body theo 2 dạng:
        ///   - isEnvelope=true:  parse thành BackendResponse&lt;DataShape&gt;
        ///                        rồi unwrap `data` thành BackendResponse&lt;T&gt;
        ///   - isEnvelope=false: parse thẳng thành T rồi wrap
        /// </summary>
        private static BackendResponse<T> ParseBody<T>(string body, bool isEnvelope)
        {
            if (string.IsNullOrEmpty(body))
            {
                return BackendResponse<T>.Failure("empty_body", "response body is empty");
            }
            try
            {
                if (isEnvelope)
                {
                    // Parse thành envelope "thô" với data ở dạng Newtonsoft JToken,
                    // rồi serialize data lại và parse vào T (Newtonsoft xử lý List<>
                    // tốt hơn JsonUtility).
                    var raw = JsonConvert.DeserializeObject<RawEnvelope>(body);
                    if (raw == null)
                    {
                        return BackendResponse<T>.Failure("parse_error", "envelope is null");
                    }
                    T data = raw.data == null
                        ? default
                        : JsonConvert.DeserializeObject<T>(raw.data.ToString(Formatting.None));
                    return new BackendResponse<T>
                    {
                        code = raw.code,
                        message = raw.message,
                        data = data,
                    };
                }
                else
                {
                    T data = JsonConvert.DeserializeObject<T>(body);
                    if (data == null)
                    {
                        return BackendResponse<T>.Failure("parse_error", "data is null");
                    }
                    return new BackendResponse<T>
                    {
                        code = "200",
                        message = "Success",
                        data = data,
                    };
                }
            }
            catch (Exception ex)
            {
                return BackendResponse<T>.Failure("parse_error", ex.Message, ex);
            }
        }

        private static string AppendQueryString(string url, IDictionary<string, string> q)
        {
            if (q == null || q.Count == 0) return url;
            var sb = new System.Text.StringBuilder(url);
            sb.Append(url.IndexOf('?') < 0 ? '?' : '&');
            bool first = true;
            foreach (var kv in q)
            {
                if (string.IsNullOrEmpty(kv.Key)) continue;
                if (!first) sb.Append('&');
                sb.Append(UnityWebRequest.EscapeURL(kv.Key));
                sb.Append('=');
                sb.Append(UnityWebRequest.EscapeURL(kv.Value ?? string.Empty));
                first = false;
            }
            return sb.ToString();
        }

        /// <summary>Envelope trung gian, dùng Newtonsoft JToken để giữ data raw.</summary>
        private sealed class RawEnvelope
        {
            public string code;
            public string message;
            public Newtonsoft.Json.Linq.JToken data;
        }
    }
}
