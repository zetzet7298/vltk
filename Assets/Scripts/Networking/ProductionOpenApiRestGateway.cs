using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VLTK.Production.Networking
{
    public sealed class ProductionOpenApiRestGateway : IProductionRestGateway, IDisposable
    {
        private readonly HttpClient _http;
        private readonly string _baseUrl;
        private readonly string _realmId;
        private readonly Func<string> _accessToken;
        private string _sessionAccessToken;
        private static readonly JsonSerializerSettings StrictJson = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Error,
            DateParseHandling = DateParseHandling.None
        };
        public string BaseUrl => _baseUrl;
        public string RealmId => _realmId;

        public ProductionOpenApiRestGateway(string baseUrl, string realmId, Func<string> accessToken = null, HttpClient http = null)
        {
            if (string.IsNullOrWhiteSpace(baseUrl) || !baseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("production REST base URL must use https", nameof(baseUrl));
            if (!IsUuid(realmId))
                throw new ArgumentException("realm id uuid required", nameof(realmId));
            _baseUrl = baseUrl.TrimEnd('/');
            _realmId = realmId;
            _accessToken = accessToken;
            _http = http ?? new HttpClient();
        }

        public async Task<RealmListResponse> ListRealmsAsync(CancellationToken cancellationToken)
        {
            return await SendAsync<RealmListResponse>(HttpMethod.Get, "/realms", null, null, false, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false);
        }

        public async Task<AuthSessionResponse> RegisterAsync(AuthRequest request, CancellationToken cancellationToken)
        {
            ValidateAuthRequest(request);
            var response = await SendAsync<AuthSessionResponse>(HttpMethod.Post, "/auth/register", new RegisterLoginRequest { realmId = _realmId, accountName = request.accountName, password = request.password, otp = request.otp }, null, false, HttpStatusCode.Created, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsValid()) throw SafeFailure(HttpStatusCode.UnprocessableEntity, "invalid token pair");
            _sessionAccessToken = response?.accessToken;
            return response;
        }

        public async Task<AuthSessionResponse> LoginAsync(AuthRequest request, CancellationToken cancellationToken)
        {
            ValidateAuthRequest(request);
            var response = await SendAsync<AuthSessionResponse>(HttpMethod.Post, "/auth/login", new RegisterLoginRequest { realmId = _realmId, accountName = request.accountName, password = request.password, otp = request.otp }, null, false, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsValid()) throw SafeFailure(HttpStatusCode.UnprocessableEntity, "invalid token pair");
            _sessionAccessToken = response?.accessToken;
            return response;
        }

        public async Task<CharacterListResponse> ListCharactersAsync(CancellationToken cancellationToken)
        {
            return await SendAsync<CharacterListResponse>(HttpMethod.Get, "/characters", null, null, true, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false);
        }

        public async Task<CharacterSummary> CreateCharacterAsync(CreateCharacterRequest request, string idempotencyKey, CancellationToken cancellationToken)
        {
            return await SendAsync<CharacterSummary>(HttpMethod.Post, "/characters", request, idempotencyKey, true, HttpStatusCode.Created, cancellationToken).ConfigureAwait(false);
        }

        public async Task<AdmissionResponse> SelectCharacterAsync(string characterId, string contentReleaseId, string idempotencyKey, CancellationToken cancellationToken)
        {
            if (!IsUuid(characterId)) throw SafeFailure(HttpStatusCode.UnprocessableEntity, "character id uuid required");
            if (!IsUuid(contentReleaseId)) throw SafeFailure(HttpStatusCode.UnprocessableEntity, "content release uuid required");
            var response = await SendAsync<AdmissionResponse>(HttpMethod.Post, "/characters/" + Uri.EscapeDataString(characterId) + "/select", new SelectCharacterRequest { contentReleaseId = contentReleaseId }, idempotencyKey, true, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false);
            if (response == null || !response.IsValid()) throw SafeFailure(HttpStatusCode.UnprocessableEntity, "invalid admission");
            return response;
        }

        public async Task<BootstrapResponse> BootstrapAsync(string characterId, string clientVersion, string idempotencyKey, CancellationToken cancellationToken)
        {
            if (!IsUuid(characterId)) throw SafeFailure(HttpStatusCode.UnprocessableEntity, "character id uuid required");
            if (string.IsNullOrWhiteSpace(clientVersion)) throw SafeFailure(HttpStatusCode.UnprocessableEntity, "client version required");
            string path = "/bootstrap?characterId=" + Uri.EscapeDataString(characterId) + "&clientVersion=" + Uri.EscapeDataString(clientVersion);
            return await SendAsync<BootstrapResponse>(HttpMethod.Post, path, new object(), idempotencyKey, true, HttpStatusCode.OK, cancellationToken).ConfigureAwait(false);
        }

        private async Task<T> SendAsync<T>(HttpMethod method, string pathAndQuery, object body, string idempotencyKey, bool bearer, HttpStatusCode expectedStatus, CancellationToken cancellationToken)
        {
            using (var req = new HttpRequestMessage(method, _baseUrl + pathAndQuery))
            {
                if (bearer)
                {
                    req.Headers.TryAddWithoutValidation("X-Realm-ID", _realmId);
                    if (method == HttpMethod.Post && !IsIdempotencyKey(idempotencyKey))
                        throw SafeFailure(HttpStatusCode.UnprocessableEntity, "Idempotency-Key invalid");
                    if (!string.IsNullOrWhiteSpace(idempotencyKey))
                        req.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
                    string token = _accessToken != null ? _accessToken() : _sessionAccessToken;
                    if (string.IsNullOrWhiteSpace(token))
                        throw SafeFailure(HttpStatusCode.Unauthorized, "bearer token required");
                    req.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
                }
                if (body != null)
                    req.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");

                using (var res = await _http.SendAsync(req, cancellationToken).ConfigureAwait(false))
                {
                    string payload = await res.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (res.StatusCode != expectedStatus)
                        throw SafeFailure(res.StatusCode, payload);
                    T decoded;
                    try
                    {
                        decoded = JsonConvert.DeserializeObject<T>(payload, StrictJson);
                    }
                    catch (JsonException ex)
                    {
                        throw SafeFailure(HttpStatusCode.UnprocessableEntity, ex.Message);
                    }
                    if (decoded == null)
                        throw SafeFailure(HttpStatusCode.UnprocessableEntity, "empty response");
                    return decoded;
                }
            }
        }

        public void Dispose() { _http.Dispose(); }

        private static void ValidateAuthRequest(AuthRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.accountName) || request.accountName.Length < 3)
                throw SafeFailure(HttpStatusCode.UnprocessableEntity, "accountName invalid");
            if (string.IsNullOrWhiteSpace(request.password) || request.password.Length < 8)
                throw SafeFailure(HttpStatusCode.UnprocessableEntity, "password invalid");
        }

        private static bool IsUuid(string value)
        {
            return Guid.TryParseExact(value, "D", out _);
        }

        private static bool IsIdempotencyKey(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length < 16 || value.Length > 128) return false;
            for (int i = 0; i < value.Length; i++)
                if (value[i] < '!' || value[i] > '~') return false;
            return true;
        }

        private static InvalidOperationException SafeFailure(HttpStatusCode status, string detail)
        {
            return new InvalidOperationException(SecretRedactor.RedactMessage("REST failed status=" + (int)status + " detail=" + (detail ?? string.Empty)));
        }

        private sealed class RegisterLoginRequest
        {
            public string realmId;
            public string accountName;
            public string password;
            public string otp;
        }

        private sealed class SelectCharacterRequest
        {
            public string contentReleaseId;
        }
    }
}
