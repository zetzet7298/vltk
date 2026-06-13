// -----------------------------------------------------------------------------
// VLTK.Backend — BackendResponse<T>
// Mirror của DataResponse<T> envelope phía backend (cores/model/base_model.py).
// Tách biệt thành field thường (không phải property) để JsonUtility + Newtonsoft
// đều đọc được. Vì một số endpoint (/health) trả về dict thuần, nên BackendResponse
// KHÔNG bắt buộc có `data` cho mọi payload — caller tự validate.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Backend
{
    /// <summary>
    /// Envelope phản hồi từ backend, tương ứng với `DataResponse[T]` của FastAPI
    /// ({ code: "200", message: "Success", data: T }).
    /// </summary>
    [Serializable]
    public sealed class BackendResponse<T>
    {
        public string code;
        public string message;
        public T data;

        /// <summary>True khi backend báo code=200 và data không null.</summary>
        public bool IsSuccess => code == "200" && data != null;

        /// <summary>True khi code là 2xx-string ("200", "201"…) theo convention.</summary>
        public bool IsSuccessCode => !string.IsNullOrEmpty(code) && code.StartsWith("2");

        /// <summary>Tạo response thất bại (transport / HTTP error).</summary>
        public static BackendResponse<T> Failure(string code, string message, Exception error = null)
        {
            return new BackendResponse<T>
            {
                code = code ?? "unknown",
                message = message ?? string.Empty,
                data = default,
            };
        }
    }
}
