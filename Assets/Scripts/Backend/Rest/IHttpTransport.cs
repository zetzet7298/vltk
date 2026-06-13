// -----------------------------------------------------------------------------
// VLTK.Backend.Rest — IHttpTransport
// Ranh giới HTTP duy nhất của VLTK.Backend. Mọi call HTTP đều đi qua interface
// này để EditMode test có thể thay thế bằng FakeHttpTransport (không cần
// server thật, không cần UnityWebRequest runtime).
//
// Lý do: UnityWebRequest yêu cầu PlayerLoop đang chạy; EditMode test không có
// PlayerLoop đầy đủ. Vì vậy tách HTTP boundary thành interface để test
// chỉ chạm interface trong bộ nhớ, không spin up UnityWebRequest thật.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VLTK.Backend.Rest
{
    /// <summary>
    /// Yêu cầu HTTP bất đồng bộ. Body là JSON string (null nếu GET).
    /// Headers là IDictionary&lt;string,string&gt; vì Dictionary&lt;,&gt; không
    /// serializable trong JsonUtility nhưng ta không cần serialize — chỉ
    /// truyền nội bộ.
    /// </summary>
    public readonly struct HttpRequest
    {
        public readonly string Method;        // "GET" | "POST" | ...
        public readonly string Url;
        public readonly string BodyJson;      // null cho GET
        public readonly IDictionary<string, string> Headers;
        public readonly int TimeoutSeconds;

        public HttpRequest(
            string method,
            string url,
            string bodyJson = null,
            IDictionary<string, string> headers = null,
            int timeoutSeconds = 10)
        {
            Method = method;
            Url = url;
            BodyJson = bodyJson;
            Headers = headers;
            TimeoutSeconds = timeoutSeconds;
        }
    }

    /// <summary>
    /// Kết quả HTTP từ transport. StatusCode là HTTP status (200, 404, 500).
    /// Body là response body thô. ErrorMessage + TransportError chỉ set khi
    /// lỗi transport (DNS, timeout, abort…) — khi đó StatusCode=0.
    /// </summary>
    public readonly struct HttpTransportResult
    {
        public readonly int StatusCode;
        public readonly string Body;
        public readonly string ErrorMessage;
        public readonly System.Exception TransportError;

        public bool HasError => TransportError != null || StatusCode == 0;
        public bool IsHttpSuccess => StatusCode >= 200 && StatusCode < 300;

        public HttpTransportResult(
            int statusCode,
            string body,
            string errorMessage,
            System.Exception transportError)
        {
            StatusCode = statusCode;
            Body = body;
            ErrorMessage = errorMessage;
            TransportError = transportError;
        }

        public static HttpTransportResult Ok(int statusCode, string body)
            => new HttpTransportResult(statusCode, body, null, null);

        public static HttpTransportResult Error(string message, System.Exception ex = null)
            => new HttpTransportResult(0, null, message, ex);
    }

    /// <summary>
    /// Transport HTTP. UnityWebRequestHttpTransport là impl production;
    /// FakeHttpTransport là impl test (xem VLTK.Backend.Tests).
    /// </summary>
    public interface IHttpTransport
    {
        Task<HttpTransportResult> SendAsync(HttpRequest request, CancellationToken ct);
    }
}
