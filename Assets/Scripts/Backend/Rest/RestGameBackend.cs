// -----------------------------------------------------------------------------
// VLTK.Backend.Rest — RestGameBackend
// Implementation production của IGameBackend. Mọi call HTTP đi qua
// IHttpTransport — nhờ đó EditMode test có thể thay thế bằng FakeHttpTransport
// (xem VLTK.Backend.Tests).
//
// Slice FS-01D (smoke):
//   - GET  /health
//   - GET  /v1/map
//
// Slice FS-03C (combat — server-authoritative):
//   - POST /v1/combat/damage/calc  (KNpc::CalcDamage parity — server mutate target)
//   - POST /v1/combat/status/tick  (KNpc::ProcessState parity — 1 frame server tick)
//   - POST /v1/combat/pk/check     (PK hợp lệ server-side; client KHÔNG tự quyết)
//
// Slice FS-04C (movement runtime sync — server-authoritative):
//   - POST /v1/movement            (KNpc::SetPos parity — server update toạ độ runtime)
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
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

        public Task<BackendResponse<SceneResponse>> EnterMapAsync(
            EnterMapRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", "EnterMapRequest is null"));
            }
            string url = Config.ResolveApiUrl("map/enter");
            // Backend dùng CamelCaseModel + extra="forbid" → serialize bằng
            // Newtonsoft với camelCase (mặc định) để field khớp alias của
            // backend. Null request đã được chặn ở trên.
            string bodyJson = JsonConvert.SerializeObject(request);
            return ExecuteAsync<SceneResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: bodyJson,
                isEnvelope: true, // /v1/map/enter trả DataResponse[SceneResponse]
                ct: ct);
        }

        public Task<BackendResponse<SceneResponse>> GetMapPositionAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {roleId}"));
            }
            string url = Config.ResolveApiUrl($"map/position/{roleId}");
            return ExecuteAsync<SceneResponse>(
                method: "GET",
                url: url,
                queryParams: null,
                bodyJson: null,
                isEnvelope: true, // /v1/map/position/{id} trả DataResponse[SceneResponse]
                ct: ct);
        }

        public Task<BackendResponse<ItemListResponse>> ListItemsAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<ItemListResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {roleId}"));
            }
            string url = Config.ResolveApiUrl($"item/by-role/{roleId}");
            return ExecuteAsync<ItemListResponse>(
                method: "GET",
                url: url,
                queryParams: null,
                bodyJson: null,
                isEnvelope: true, // /v1/item/by-role/{id} trả DataResponse[ItemListResponse]
                ct: ct);
        }

        // ----------------------------------------------------------------
        // FS-03C — Combat (server-authoritative)
        // ----------------------------------------------------------------

        public Task<BackendResponse<DamageCalcResponse>> CalcDamageAsync(
            DamageCalcRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<DamageCalcResponse>.Failure(
                    "invalid_arg", "DamageCalcRequest is null"));
            }
            if (request.target == null)
            {
                return Task.FromResult(BackendResponse<DamageCalcResponse>.Failure(
                    "invalid_arg", "DamageCalcRequest.target is null"));
            }
            // Server là NGUỒN CHÂN LÝ duy nhất cho damage. Client KHÔNG tự tính.
            // Gửi toàn bộ context (atkMin/atkMax/kind/melee/return/pkRate/target/
            // attacker/seed) — server mutate target tại chỗ và trả về damage +
            // state sau.
            string url = Config.ResolveApiUrl("combat/damage/calc");
            string bodyJson = JsonConvert.SerializeObject(request);
            return ExecuteAsync<DamageCalcResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: bodyJson,
                isEnvelope: true, // /v1/combat/damage/calc trả DataResponse[DamageCalcResponse]
                ct: ct);
        }

        public Task<BackendResponse<StatusTickResponse>> StatusTickAsync(
            StatusTickRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<StatusTickResponse>.Failure(
                    "invalid_arg", "StatusTickRequest is null"));
            }
            if (request.target == null || request.status == null)
            {
                return Task.FromResult(BackendResponse<StatusTickResponse>.Failure(
                    "invalid_arg", "StatusTickRequest.target/status is null"));
            }
            // Server tiến 1 frame ProcessState (KNpc.cpp:612-863), mutate target
            // + status tại chỗ. Client KHÔNG tự tick status local.
            string url = Config.ResolveApiUrl("combat/status/tick");
            string bodyJson = JsonConvert.SerializeObject(request);
            return ExecuteAsync<StatusTickResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: bodyJson,
                isEnvelope: true, // /v1/combat/status/tick trả DataResponse[StatusTickResponse]
                ct: ct);
        }

        public Task<BackendResponse<PkCheckResponse>> CheckPkAsync(
            PkCheckRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<PkCheckResponse>.Failure(
                    "invalid_arg", "PkCheckRequest is null"));
            }
            if (string.IsNullOrEmpty(request.mapType))
            {
                return Task.FromResult(BackendResponse<PkCheckResponse>.Failure(
                    "invalid_arg", "PkCheckRequest.mapType is null/empty"));
            }
            // Server quyết định có được phép đánh (vùng an toàn/khác phe/battle).
            // Client LUÔN gọi trước khi damage.
            string url = Config.ResolveApiUrl("combat/pk/check");
            string bodyJson = JsonConvert.SerializeObject(request);
            return ExecuteAsync<PkCheckResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: bodyJson,
                isEnvelope: true, // /v1/combat/pk/check trả DataResponse[PkCheckResponse]
                ct: ct);
        }

        // ----------------------------------------------------------------
        // FS-04C — Movement (server-authoritative, parity KNpc::SetPos)
        // ----------------------------------------------------------------

        public Task<BackendResponse<SceneResponse>> UpdatePositionAsync(
            UpdatePositionRequest request, CancellationToken ct = default)
        {
            if (request == null)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "invalid_arg", "UpdatePositionRequest is null"));
            }
            if (request.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (request.posX < 0 || request.posY < 0)
            {
                return Task.FromResult(BackendResponse<SceneResponse>.Failure(
                    "validation_error",
                    $"posX/posY phải >= 0; got ({request.posX},{request.posY})"));
            }
            // Server là NGUỒN CHÂN LÝ duy nhất cho vị trí runtime — parity
            // KNpc.cpp:5496 KNpc::SetPos(int nX, int nY) vốn ghi vào struct
            // Npc[] toàn cục. Client KHÔNG tự quyết toạ độ cuối; chỉ gửi vị
            // trí dự đoán rồi đợi server confirm.
            string url = Config.ResolveApiUrl("movement");
            string bodyJson = JsonConvert.SerializeObject(request);
            return ExecuteAsync<SceneResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: bodyJson,
                isEnvelope: true, // /v1/movement trả DataResponse[SceneResponse]
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

// ---- FS-03B (skill read + cast) ----

        public Task<BackendResponse<PlayerSkillListResponse>> ListSkillsAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillListResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            string url = Config.ResolveApiUrl("skill/by-role/" + roleId.ToString());
            return ExecuteAsync<PlayerSkillListResponse>(
                method: "GET",
                url: url,
                queryParams: null,
                bodyJson: null,
                isEnvelope: true,
                ct: ct);
        }

        public Task<BackendResponse<PlayerSkillResponse>> LearnSkillAsync(
            SkillLearnRequest req, CancellationToken ct = default)
        {
            if (req == null)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "req không được null"));
            }
            if (req.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (req.skillId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            if (req.charLevel < 1 || req.charLevel > 200)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "charLevel phải trong [1..200]"));
            }
            // faction: -1 (chưa nhập phái) hoặc 0..9 — Pydantic cũng chấp nhận -1.
            if (req.faction < -1 || req.faction > 9)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "faction phải trong [-1..9]"));
            }
            string url = Config.ResolveApiUrl("skill/learn");
            return ExecuteAsync<PlayerSkillResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: req.ToJson(),
                isEnvelope: true,
                ct: ct);
        }

        public Task<BackendResponse<PlayerSkillResponse>> LevelUpSkillAsync(
            int roleId, int skillId, CancellationToken ct = default)
        {
            if (roleId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (skillId <= 0)
            {
                return Task.FromResult(BackendResponse<PlayerSkillResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            // /v1/skill/by-role/{roleId}/level-up/{skillId} — path params int.
            string url = Config.ResolveApiUrl(
                "skill/by-role/" + roleId.ToString() + "/level-up/" + skillId.ToString());
            return ExecuteAsync<PlayerSkillResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: null, // body rỗng — toàn bộ state nằm trong URL
                isEnvelope: true,
                ct: ct);
        }

        public Task<BackendResponse<SkillCastCheckResponse>> CastSkillCheckAsync(
            SkillCastCheckRequest req, CancellationToken ct = default)
        {
            if (req == null)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "req không được null"));
            }
            if (req.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (req.skillId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            // nowMs >= 1 (parity backend Pydantic constraint).
            if (req.nowMs < 1)
            {
                return Task.FromResult(BackendResponse<SkillCastCheckResponse>.Failure(
                    "validation_error", "nowMs phải >= 1"));
            }
            string url = Config.ResolveApiUrl("skill/cast/check");
            return ExecuteAsync<SkillCastCheckResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: req.ToJson(),
                isEnvelope: true,
                ct: ct);
        }

        public Task<BackendResponse<SkillCastResponse>> CastSkillAsync(
            SkillCastRequest req, CancellationToken ct = default)
        {
            if (req == null)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "req không được null"));
            }
            if (req.roleId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "roleId phải > 0"));
            }
            if (req.skillId <= 0)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "skillId phải > 0"));
            }
            if (req.nowMs < 1)
            {
                return Task.FromResult(BackendResponse<SkillCastResponse>.Failure(
                    "validation_error", "nowMs phải >= 1"));
            }
            string url = Config.ResolveApiUrl("skill/cast");
            return ExecuteAsync<SkillCastResponse>(
                method: "POST",
                url: url,
                queryParams: null,
                bodyJson: req.ToJson(),
                isEnvelope: true,
                ct: ct);
        }

        // -------- FS-02B (auth) --------

        public Task<BackendResponse<LoginResponse>> LoginAsync(
            string accName, string password, string otp = null,
            string clientIp = null, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(accName) || string.IsNullOrEmpty(password))
                return Task.FromResult(BackendResponse<LoginResponse>.Failure(
                    "invalid_arg", "accName/password trống"));
            var body = new Dictionary<string, string> { {"accName", accName}, {"password", password} };
            if (otp != null) body["otp"] = otp;
            if (clientIp != null) body["clientIp"] = clientIp;
            string url = Config.ResolveApiUrl("account/login");
            return ExecuteAsync<LoginResponse>(
                method: "POST", url: url, queryParams: null,
                bodyJson: JsonConvert.SerializeObject(body),
                isEnvelope: true, ct: ct);
        }

        public Task<BackendResponse<RoleListResponse>> ListRolesAsync(
            string accName, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(accName))
                return Task.FromResult(BackendResponse<RoleListResponse>.Failure(
                    "invalid_arg", "accName trống"));
            string url = Config.ResolveApiUrl($"role/by-account/{accName}");
            return ExecuteAsync<RoleListResponse>(
                method: "GET", url: url, queryParams: null,
                bodyJson: null, isEnvelope: true, ct: ct);
        }

        public Task<BackendResponse<PlayerStateResponse>> GetPlayerStateAsync(
            int roleId, CancellationToken ct = default)
        {
            if (roleId <= 0)
                return Task.FromResult(BackendResponse<PlayerStateResponse>.Failure(
                    "invalid_arg", $"roleId phải > 0; got {roleId}"));
            string url = Config.ResolveApiUrl($"player/by-role/{roleId}");
            return ExecuteAsync<PlayerStateResponse>(
                method: "GET", url: url, queryParams: null,
                bodyJson: null, isEnvelope: true, ct: ct);
        }
    }
}
