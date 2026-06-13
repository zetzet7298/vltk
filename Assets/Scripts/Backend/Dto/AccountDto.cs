// -----------------------------------------------------------------------------
// VLTK.Backend.Dto — Account DTOs (login contract FS-02A)
//
// Pin từ backend contract (FS-02A, commit 1625566, branch main ở vltk-server):
//   POST /v1/account/login body = { accName, password PLAINTEXT, otp?, clientIp? }
//     - KHÔNG gửi MD5 trước. Server tự hash MD5-IN-HOA rồi so với accounts.password.
//     - extra=forbid ở backend: gửi field lạ → 422.
//     - KHÔNG yêu cầu Authorization header (FS-02 chưa có bearer/JWT).
//
//   200 → { code:"200", message:"Success", data:{ accName, serviceFlag, extPoint } }
//   401 → sai tên HOẶC sai mật khẩu HOẶC account không tồn tại — CÙNG message
//   403 → account bị banned
//   422 → body thiếu field bắt buộc hoặc có field lạ
//   429 → vượt LimitAccountPerIP (SoLuongAccGioiHan=4)
//   501 → account bật isUseOtp mà OTP engine chưa cấu hình
//
// Field camelCase khớp alias generator (to_camel) của backend CamelCaseModel.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace VLTK.Backend.Dto
{
    /// <summary>
    /// Request body cho POST /v1/account/login.
    /// Tất cả field camelCase, KHÔNG hash password phía client.
    /// otp/clientIp optional — chỉ gửi khi account bật isUseOtp hoặc cần áp LimitAccountPerIP.
    /// </summary>
    [Serializable]
    public sealed class LoginRequest
    {
        /// <summary>Tên tài khoản, 1..32 char, unique toàn server.</summary>
        public string accName;

        /// <summary>Mật khẩu PLAINTEXT (server sẽ tự hash MD5-IN-HOA để so với cột accounts.password).</summary>
        public string password;

        /// <summary>OTP — chỉ cần khi account bật isUseOtp=true.</summary>
        public string otp;

        /// <summary>IP client (cho rate-limit theo IP). Thường set từ networking layer.</summary>
        public string clientIp;

        public LoginRequest() { }

        public LoginRequest(string accName, string password, string otp = null, string clientIp = null)
        {
            this.accName = accName;
            this.password = password;
            this.otp = otp;
            this.clientIp = clientIp;
        }

        /// <summary>Serialize thành JSON string để gửi qua IHttpTransport.</summary>
        public string ToJson()
        {
            // Dùng NullValueHandling.Ignore để optional field (otp, clientIp) không
            // xuất hiện trong body khi null — backend đã verify 2026-06-13 rằng
            // request KHÔNG có field optional vẫn pass validation (extra=forbid chỉ
            // áp dụng khi field LẠ xuất hiện; field optional-null vắng mặt hoàn toàn
            // là hợp lệ).
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
            };
            return JsonConvert.SerializeObject(this, settings);
        }
    }

    /// <summary>
    /// Phản hồi data từ POST /v1/account/login.
    /// KHÔNG có token field — accName là session id duy nhất cho FS-02.
    /// serviceFlag/extPoint là metadata tài khoản (parity account_tong PC).
    /// </summary>
    [Serializable]
    public sealed class LoginResponse
    {
        public string accName;
        public int serviceFlag;
        public int extPoint;
    }
}
