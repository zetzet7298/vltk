// -----------------------------------------------------------------------------
// VLTK.Backend.Tests — FakeHttpTransport
// Test-only transport. Script phản hồi theo URL. Không gọi network thật, không
// cần UnityWebRequest, chạy được trong EditMode test (không có PlayerLoop).
//
// Cách dùng:
//   var fake = new FakeHttpTransport();
//   fake.QueueResponse("GET", "http://.../health", 200, "{\"status\":\"ok\"}");
//   var backend = new RestGameBackend(config, fake);
//   var resp = await backend.GetHealthAsync();
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VLTK.Backend.Rest;

namespace VLTK.Backend.Tests
{
    public sealed class FakeHttpTransport : IHttpTransport
    {
        /// <summary>Một bản ghi phản hồi đã lên lịch.</summary>
        public readonly struct ScriptedResponse
        {
            public readonly string Method;
            public readonly string UrlContains;
            public readonly int StatusCode;
            public readonly string Body;
            public readonly Exception Error;

            public ScriptedResponse(string method, string urlContains, int statusCode, string body, Exception error)
            {
                Method = method;
                UrlContains = urlContains;
                StatusCode = statusCode;
                Body = body;
                Error = error;
            }
        }

        private readonly Queue<ScriptedResponse> _queue = new();

        /// <summary>Danh sách các request thật sự đã gửi (URL + method + body).</summary>
        public readonly List<(string Method, string Url, string Body)> Sent = new();

        /// <summary>Lập lịch một phản hồi sẽ trả về cho request tiếp theo match URL contains.</summary>
        public void QueueResponse(string method, string urlContains, int statusCode, string body)
        {
            _queue.Enqueue(new ScriptedResponse(method, urlContains, statusCode, body, null));
        }

        /// <summary>Lập lịch một transport error (ví dụ timeout) cho request tiếp theo.</summary>
        public void QueueTransportError(string method, string urlContains, Exception error)
        {
            _queue.Enqueue(new ScriptedResponse(method, urlContains, 0, null, error));
        }

        public async Task<HttpTransportResult> SendAsync(HttpRequest request, CancellationToken ct)
        {
            Sent.Add((request.Method, request.Url, request.BodyJson));

            // Trả về response khớp đầu tiên trong queue.
            foreach (ScriptedResponse r in _queue)
            {
                bool methodOk = string.IsNullOrEmpty(r.Method)
                    || string.Equals(r.Method, request.Method, StringComparison.OrdinalIgnoreCase);
                bool urlOk = string.IsNullOrEmpty(r.UrlContains)
                    || (request.Url != null && request.Url.IndexOf(r.UrlContains, StringComparison.OrdinalIgnoreCase) >= 0);
                if (methodOk && urlOk)
                {
                    if (r.Error != null)
                        return HttpTransportResult.Error(r.Error.Message, r.Error);
                    return HttpTransportResult.Ok(r.StatusCode, r.Body);
                }
            }

            // Không có script → trả 404 để test phát hiện thiếu setup.
            return HttpTransportResult.Ok(404, "{\"detail\":\"no scripted response\"}");
        }
    }
}
