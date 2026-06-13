// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — HealthResponse
// Mirror cho payload của GET /health (xem backend/app/main.py).
// Backend trả dict thuần: {status, service, version, timestamp}.
// Field camelCase khớp với response thật; field name (snake_case) chưa cần
// vì endpoint này không dùng CamelCaseModel.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Backend.Dto
{
    /// <summary>
    /// Phản hồi từ GET /health. Trường JSON dùng camelCase (status, service, version, timestamp).
    /// </summary>
    [Serializable]
    public sealed class HealthResponse
    {
        public string status;
        public string service;
        public string version;
        public string timestamp;

        /// <summary>True khi backend báo status="ok".</summary>
        public bool IsOk => !string.IsNullOrEmpty(status) &&
            string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase);
    }
}
