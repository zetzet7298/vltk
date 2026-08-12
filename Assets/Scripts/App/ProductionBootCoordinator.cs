using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VLTK.Production.Networking;
using VLTK.Production.World.Unity;

namespace VLTK.Production.App
{
    public sealed class ProductionBootCoordinator
    {
        private readonly ProductionBootStateMachine _state;
        private readonly IProductionRestGateway _rest;
        private readonly string _apiBaseUrl;
        private readonly string _realmId;
        private readonly ProductionMapRuntimeLoader _mapLoader;
        private readonly Func<IRealtimeHelloEncoder, IRealtimeAdmissionAckDecoder, RealtimeAdmissionClient> _admissionClientFactory;
        public RealtimeAdmissionClient RealtimeClient { get; private set; }
        public MovementIntentSender MovementSender { get; private set; }

        public ProductionBootCoordinator(ProductionBootStateMachine state, IProductionRestGateway rest, string apiBaseUrl, string realmId, ProductionMapRuntimeLoader mapLoader, Func<IRealtimeHelloEncoder, IRealtimeAdmissionAckDecoder, RealtimeAdmissionClient> admissionClientFactory = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _rest = rest ?? throw new ArgumentNullException(nameof(rest));
            if (string.IsNullOrWhiteSpace(apiBaseUrl) || !apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) throw new ArgumentException("https api base url required", nameof(apiBaseUrl));
            if (string.IsNullOrWhiteSpace(realmId)) throw new ArgumentException("realm id required", nameof(realmId));
            _apiBaseUrl = apiBaseUrl.TrimEnd('/');
            _realmId = realmId;
            _mapLoader = mapLoader ?? throw new ArgumentNullException(nameof(mapLoader));
            _admissionClientFactory = admissionClientFactory ?? ((hello, ack) => RealtimeAdmissionClient.CreateDefault(hello, ack));
        }

        public async Task<bool> RunAsync(ProductionBootRequest request, CancellationToken cancellationToken)
        {
            if (!_state.BeginBootstrap()) return false;
            RealmListResponse realms = await _rest.ListRealmsAsync(cancellationToken).ConfigureAwait(false);
            if (realms == null || !realms.HasRealm(_realmId)) return _state.FailExternal("realm_unavailable");
            if (!_state.ApplyBootstrap(new RealmBootstrapResponse { realmId = _realmId, apiBaseUrl = _apiBaseUrl, clientVersion = request.clientVersion })) return false;

            AuthSessionResponse auth = await AuthenticateAsync(request, cancellationToken).ConfigureAwait(false);
            if (!_state.ApplyAuth(auth)) return false;

            CharacterSummary character = await SelectOrCreateCharacterAsync(request, cancellationToken).ConfigureAwait(false);
            if (character == null || !character.IsValid()) return _state.FailExternal("character_unavailable");
            if (string.IsNullOrWhiteSpace(request.contentReleaseId)) return _state.FailExternal("content_release_required");
            AdmissionResponse selectedAdmission = await _rest.SelectCharacterAsync(character.EffectiveId, request.contentReleaseId, NewIdempotencyKey(), cancellationToken).ConfigureAwait(false);
            if (!_state.ApplyCharacter(new CharacterSelectionResponse { selectedCharacter = character, realtimeEndpoint = selectedAdmission?.url, admissionTicket = selectedAdmission?.ticket })) return false;

            BootstrapResponse bootstrap = await _rest.BootstrapAsync(character.EffectiveId, request.clientVersion, NewIdempotencyKey(), cancellationToken).ConfigureAwait(false);
            if (bootstrap == null || !bootstrap.IsValid(_realmId) || !string.Equals(bootstrap.contentReleaseId, request.contentReleaseId, StringComparison.OrdinalIgnoreCase)) return _state.FailExternal("bootstrap_invalid");
            AdmissionResponse admission = bootstrap.admission ?? selectedAdmission;

            MapRuntimeValidationResult validation;
            MapRuntimeTrustMode mapTrust = request.trustMode == ContentTrustMode.ProductionSignature ? MapRuntimeTrustMode.ProductionSignature : MapRuntimeTrustMode.EditorPinnedDigest;
            var map = _mapLoader.Load(mapTrust, out validation);
            var content = new VerifiedContentResponse
            {
                verified = map != null && validation.ok,
                mapId = ProductionMapIds.CanonicalBootMapId,
                contentDigest = bootstrap.contentDigest,
                trust = new ContentTrustResult(request.trustMode, map != null && validation.ok, request.trustMode == ContentTrustMode.ProductionSignature, validation.code),
                provenanceSha256 = MapRuntimeContract.PinnedProvenanceSha256
            };
            if (!_state.ApplyContent(content)) return false;

            var codec = new GameV1RealtimeCodec();
            RealtimeClient = _admissionClientFactory(codec, codec);
            var admitted = await RealtimeClient.AdmitAsync(new RealtimeAdmission { endpoint = admission.url, admissionTicket = admission.ticket, clientVersion = request.clientVersion, content = content }, cancellationToken).ConfigureAwait(false);
            if (!_state.ApplyRealtimeAdmission(admitted)) return false;
            if (codec.LastSnapshot == null) return _state.FailExternal("initial_snapshot_missing");
            MovementSender = new MovementIntentSender(RealtimeClient.ActiveSocket, codec);
            return _state.ApplyMapLoaded(ProductionMapIds.CanonicalBootMapId) && _state.ApplyAvatarPresented() && _state.ApplyJoystickReady();
        }

        private async Task<AuthSessionResponse> AuthenticateAsync(ProductionBootRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return await _rest.LoginAsync(request.auth, cancellationToken).ConfigureAwait(false);
            }
            catch (InvalidOperationException) when (request.authPolicy == AuthFlowPolicy.RegisterOrLogin)
            {
                return await _rest.RegisterAsync(request.auth, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task<CharacterSummary> SelectOrCreateCharacterAsync(ProductionBootRequest request, CancellationToken cancellationToken)
        {
            CharacterListResponse list = await _rest.ListCharactersAsync(cancellationToken).ConfigureAwait(false);
            CharacterSummary selected = FindCharacter(list?.characters, request.preferredCharacterId);
            if (selected != null) return selected;
            if (request.characterPolicy != CharacterFlowPolicy.CreateOrSelect) return null;
            return await _rest.CreateCharacterAsync(request.createCharacter, NewIdempotencyKey(), cancellationToken).ConfigureAwait(false);
        }

        private static CharacterSummary FindCharacter(List<CharacterSummary> characters, string preferredId)
        {
            if (characters == null || characters.Count == 0) return null;
            if (!string.IsNullOrWhiteSpace(preferredId))
            {
                for (int i = 0; i < characters.Count; i++)
                    if (string.Equals(characters[i]?.EffectiveId, preferredId, StringComparison.OrdinalIgnoreCase)) return characters[i];
                return null;
            }
            for (int i = 0; i < characters.Count; i++)
                if (characters[i] != null && characters[i].IsValid()) return characters[i];
            return null;
        }

        private static string NewIdempotencyKey()
        {
            return Guid.NewGuid().ToString("N");
        }
    }
}
