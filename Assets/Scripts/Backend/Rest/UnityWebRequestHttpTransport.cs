// -----------------------------------------------------------------------------
// VLTK.Backend.Rest — UnityWebRequestHttpTransport
// Production transport: build UnityWebRequest, set headers/body, poll until
// done với await Task.Yield() (giống pattern Assets/Scripts/Sandbox/AudioService.cs).
// Không có retry/back-off; slice FS-01D chỉ cần single-shot.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VLTK.Core;

namespace VLTK.Backend.Rest
{
    /// <summary>
    /// Production transport dựa trên UnityWebRequest. Hỗ trợ GET/POST với body
    /// JSON và headers tùy ý. CancellationToken sẽ Abort request khi trip.
    /// </summary>
    public sealed class UnityWebRequestHttpTransport : IHttpTransport
    {
        private const string Subsystem = "Backend.Http";

        public async Task<HttpTransportResult> SendAsync(HttpRequest request, CancellationToken ct)
        {
            UnityWebRequest uwr = BuildRequest(request);
            CancellationTokenRegistration ctr = default;
            if (ct.CanBeCanceled)
            {
                ctr = ct.Register(() =>
                {
                    try { if (!uwr.isDone) uwr.Abort(); }
                    catch { /* Abort có thể ném khi đã dispose, bỏ qua */ }
                });
            }

            try
            {
                UnityWebRequestAsyncOperation op = uwr.SendWebRequest();
                // Poll cho đến khi xong (giống AudioService).
                while (!op.isDone)
                {
                    if (ct.IsCancellationRequested)
                    {
                        return HttpTransportResult.Error("cancelled", null);
                    }
                    await Task.Yield();
                }

                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    string body = uwr.downloadHandler != null ? uwr.downloadHandler.text : string.Empty;
                    return HttpTransportResult.Ok((int)uwr.responseCode, body);
                }

                string errMsg = $"{uwr.result}: {uwr.error}";
                SubsystemLog.Warn(Subsystem, $"{request.Method} {request.Url} -> {errMsg}");
                return HttpTransportResult.Error(errMsg, null);
            }
            catch (System.Exception ex)
            {
                SubsystemLog.Error(Subsystem, $"{request.Method} {request.Url} threw {ex.GetType().Name}: {ex.Message}");
                return HttpTransportResult.Error(ex.Message, ex);
            }
            finally
            {
                ctr.Dispose();
                uwr.Dispose();
            }
        }

        private static UnityWebRequest BuildRequest(HttpRequest request)
        {
            UnityWebRequest uwr;
            if (string.Equals(request.Method, "GET", System.StringComparison.OrdinalIgnoreCase))
            {
                uwr = UnityWebRequest.Get(request.Url);
            }
            else if (string.Equals(request.Method, "POST", System.StringComparison.OrdinalIgnoreCase))
            {
                // UploadHandlerRaw + DownloadHandlerBuffer = generic POST.
                byte[] payload = string.IsNullOrEmpty(request.BodyJson)
                    ? System.Array.Empty<byte>()
                    : System.Text.Encoding.UTF8.GetBytes(request.BodyJson);
                uwr = new UnityWebRequest(request.Url, UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = new UploadHandlerRaw(payload) { contentType = "application/json" },
                    downloadHandler = new DownloadHandlerBuffer(),
                };
            }
            else
            {
                uwr = new UnityWebRequest(request.Url, request.Method);
            }

            uwr.timeout = Mathf.Max(1, request.TimeoutSeconds);

            if (request.Headers != null)
            {
                foreach (KeyValuePair<string, string> h in request.Headers)
                {
                    if (string.IsNullOrEmpty(h.Key)) continue;
                    uwr.SetRequestHeader(h.Key, h.Value ?? string.Empty);
                }
            }
            // Đảm bảo Accept luôn JSON trừ khi caller đã set khác.
            if (request.Headers == null || !request.Headers.ContainsKey("Accept"))
            {
                uwr.SetRequestHeader("Accept", "application/json");
            }
            return uwr;
        }
    }
}
