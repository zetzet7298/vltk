using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VLTK.Production.Networking
{
    public enum AuthFlowPolicy { LoginOnly, RegisterOrLogin }
    public enum CharacterFlowPolicy { SelectOnly, CreateOrSelect }
    public enum ContentTrustMode { ProductionSignature, EditorPinnedDigest }

    public readonly struct ProductionBootRequest
    {
        public readonly AuthRequest auth;
        public readonly AuthFlowPolicy authPolicy;
        public readonly CharacterFlowPolicy characterPolicy;
        public readonly string preferredCharacterId;
        public readonly CreateCharacterRequest createCharacter;
        public readonly string clientVersion;
        public readonly string contentReleaseId;
        public readonly ContentTrustMode trustMode;

        public ProductionBootRequest(AuthRequest auth, AuthFlowPolicy authPolicy, CharacterFlowPolicy characterPolicy, string preferredCharacterId, CreateCharacterRequest createCharacter, string clientVersion, string contentReleaseId, ContentTrustMode trustMode)
        {
            this.auth = auth;
            this.authPolicy = authPolicy;
            this.characterPolicy = characterPolicy;
            this.preferredCharacterId = preferredCharacterId;
            this.createCharacter = createCharacter;
            this.clientVersion = clientVersion;
            this.contentReleaseId = contentReleaseId;
            this.trustMode = trustMode;
        }
    }

    public sealed class RealmSummary
    {
        public string id;
        public string code;
        public string name;
        public string status;
    }

    public sealed class RealmListResponse
    {
        public List<RealmSummary> realms;
        public bool HasRealm(string realmId)
        {
            if (string.IsNullOrWhiteSpace(realmId) || realms == null) return false;
            for (int i = 0; i < realms.Count; i++)
                if (string.Equals(realms[i]?.id, realmId, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }
    }

    public sealed class RealmBootstrapResponse
    {
        public string realmId;
        public string apiBaseUrl;
        public string clientVersion;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(realmId)
                && !string.IsNullOrWhiteSpace(apiBaseUrl)
                && apiBaseUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(clientVersion);
        }
    }

    public readonly struct AuthRequest
    {
        public readonly string accountName;
        public readonly string password;
        public readonly string otp;

        public AuthRequest(string accountName, string password, string otp = null)
        {
            this.accountName = accountName;
            this.password = password;
            this.otp = otp;
        }
    }

    public sealed class AuthSessionResponse
    {
        public string accountId;
        public string realmId;
        public string accessToken;
        public string accessExpiresAt;
        public string refreshToken;
        public string refreshExpiresAt;
        public string displayName;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(accountId)
                && !string.IsNullOrWhiteSpace(realmId)
                && !string.IsNullOrWhiteSpace(accessToken)
                && !string.IsNullOrWhiteSpace(accessExpiresAt)
                && !string.IsNullOrWhiteSpace(refreshToken)
                && !string.IsNullOrWhiteSpace(refreshExpiresAt);
        }
    }

    public sealed class CreateCharacterRequest
    {
        public string name;
        public string gender;
        public int series;
        public int homelandId;
        public int slot;
        public int appearanceId;
    }

    public sealed class CharacterSummary
    {
        public string characterId;
        public string id;
        public string name;
        public string gender;
        public int faction;
        public int series;
        public int homelandId;
        public int level;
        public int mapId;
        public int slot;
        public int appearanceId;
        public long appearanceRevision;
        public long version;
        public string deletedAt;
        public string purgeAfter;
        public float spawnX;
        public float spawnY;

        public string EffectiveId => !string.IsNullOrWhiteSpace(characterId) ? characterId : id;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(EffectiveId)
                && !string.IsNullOrWhiteSpace(name)
                && !string.IsNullOrWhiteSpace(gender)
                && homelandId >= 1
                && level >= 1
                && slot >= 1 && slot <= 3
                && appearanceRevision >= 1
                && version >= 1
                && mapId == ProductionMapIds.CanonicalBootMapId;
        }
    }

    public sealed class CharacterListResponse
    {
        public List<CharacterSummary> characters;
    }

    public sealed class CharacterSelectionResponse
    {
        public CharacterSummary selectedCharacter;
        public string admissionTicket;
        public string realtimeEndpoint;

        public bool IsValid()
        {
            return selectedCharacter != null
                && selectedCharacter.IsValid()
                && !string.IsNullOrWhiteSpace(admissionTicket)
                && RealtimeEndpointPolicy.IsProductionWss(realtimeEndpoint);
        }
    }

    public sealed class ContentDigestDto
    {
        public string contentReleaseId;
        public string manifestSha256;
        public string sourceSnapshotId;
        public uint catalogUnionSize;
        public string catalogUnionSha256;
        public string runtimeSkillPolicyId;
        public string clientProjectionSha256;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(contentReleaseId)
                && DigestPolicy.IsSha256Hex(manifestSha256)
                && !string.IsNullOrWhiteSpace(sourceSnapshotId)
                && catalogUnionSize == 242
                && DigestPolicy.IsSha256Hex(catalogUnionSha256)
                && !string.IsNullOrWhiteSpace(runtimeSkillPolicyId)
                && DigestPolicy.IsSha256Hex(clientProjectionSha256);
        }
    }

    public readonly struct ContentTrustResult
    {
        public readonly ContentTrustMode mode;
        public readonly bool trusted;
        public readonly bool productionSignatureVerified;
        public readonly string failureCode;

        public ContentTrustResult(ContentTrustMode mode, bool trusted, bool productionSignatureVerified, string failureCode)
        {
            this.mode = mode;
            this.trusted = trusted;
            this.productionSignatureVerified = productionSignatureVerified;
            this.failureCode = failureCode;
        }
    }

    public sealed class VerifiedContentResponse
    {
        public bool verified;
        public int mapId;
        public ContentDigestDto contentDigest;
        public ContentTrustResult trust;
        public string provenanceSha256;

        public string contentReleaseId => contentDigest?.contentReleaseId;
        public string sourceSnapshotId => contentDigest?.sourceSnapshotId;
        public string runtimeSkillPolicyId => contentDigest?.runtimeSkillPolicyId;
        public string digestSha256 => contentDigest?.manifestSha256;

        public bool IsValid()
        {
            return verified
                && mapId == ProductionMapIds.CanonicalBootMapId
                && contentDigest != null
                && contentDigest.IsValid()
                && trust.trusted
                && (trust.mode != ContentTrustMode.ProductionSignature || trust.productionSignatureVerified)
                && DigestPolicy.IsSha256Hex(provenanceSha256);
        }
    }

    public sealed class AdmissionResponse
    {
        public string url;
        public string subprotocol;
        public string ticket;
        public string expiresAt;
        public uint tickRateHz;
        public ulong sessionEpoch;
        public uint reconnectGraceSeconds;

        public bool IsValid()
        {
            return RealtimeEndpointPolicy.IsProductionWss(url)
                && subprotocol == "game.v1"
                && !string.IsNullOrWhiteSpace(ticket)
                && !string.IsNullOrWhiteSpace(expiresAt)
                && tickRateHz == 18
                && sessionEpoch > 0
                && reconnectGraceSeconds == 15;
        }
    }

    public sealed class BootstrapResponse
    {
        public string realmId;
        public string contentReleaseId;
        public string sourceSnapshotId;
        public string userFacingLocale;
        public string manifestUrl;
        public string manifestSha256;
        public string minClientVersion;
        public string recommendedClientVersion;
        public UiPanelFlagBundleDto uiPanelFlags;
        public ContentDigestDto contentDigest;
        public RuntimeSkillPolicyDto runtimeSkillPolicy;
        public AdmissionResponse admission;

        public bool IsValid(string realmIdExpected)
        {
            return string.Equals(realmId, realmIdExpected, StringComparison.OrdinalIgnoreCase)
                && userFacingLocale == "vi"
                && !string.IsNullOrWhiteSpace(manifestUrl)
                && DigestPolicy.IsSha256Hex(manifestSha256)
                && contentDigest != null
                && contentDigest.IsValid()
                && contentDigest.contentReleaseId == contentReleaseId
                && contentDigest.manifestSha256 == manifestSha256
                && runtimeSkillPolicy != null
                && runtimeSkillPolicy.IsValid(contentDigest)
                && uiPanelFlags != null
                && uiPanelFlags.IsValid(contentReleaseId)
                && admission != null
                && admission.IsValid();
        }
    }

    public sealed class RuntimeSkillPolicyDto
    {
        public string policyId;
        public uint catalogUnionSize;
        public string catalogUnionSha256;
        public string sourceTool;
        public bool filesystemFallbackAllowed;
        public bool runtimeParityClaimed;
        public string pcRuntimeEvidenceStatus;
        public string androidPhysicalEvidenceStatus;

        public bool IsValid(ContentDigestDto digest)
        {
            return digest != null
                && policyId == digest.runtimeSkillPolicyId
                && catalogUnionSize == digest.catalogUnionSize
                && catalogUnionSha256 == digest.catalogUnionSha256
                && sourceTool == "vltktool"
                && !filesystemFallbackAllowed
                && !runtimeParityClaimed
                && !string.IsNullOrWhiteSpace(pcRuntimeEvidenceStatus)
                && !string.IsNullOrWhiteSpace(androidPhysicalEvidenceStatus);
        }
    }

    public sealed class UiPanelFlagBundleDto
    {
        public int schemaVersion;
        public long revision;
        public string contentReleaseId;
        public string issuedAt;
        public string signingKeyId;
        public string signatureAlgorithm;
        public string signature;
        public List<UiPanelFlagDto> flags;

        public bool IsValid(string expectedContentReleaseId)
        {
            return schemaVersion == 1
                && revision >= 1
                && contentReleaseId == expectedContentReleaseId
                && !string.IsNullOrWhiteSpace(issuedAt)
                && !string.IsNullOrWhiteSpace(signingKeyId)
                && signatureAlgorithm == "Ed25519"
                && !string.IsNullOrWhiteSpace(signature)
                && flags != null;
        }
    }

    public sealed class UiPanelFlagDto
    {
        public string key;
        public string panelId;
        public string variant;
        public string cohort;
        public string minClientVersion;
        public string contentVersion;
        public int rolloutBasisPoints;
        public string owner;
        public string expiresAt;
        public string rollbackKey;
        public string reason;
        public long revision;
        public List<string> releaseAllowedVariants;
    }

    public static class ProductionMapIds
    {
        public const int CanonicalBootMapId = 53;
    }

    public interface IProductionRestGateway
    {
        Task<RealmListResponse> ListRealmsAsync(CancellationToken cancellationToken);
        Task<AuthSessionResponse> RegisterAsync(AuthRequest request, CancellationToken cancellationToken);
        Task<AuthSessionResponse> LoginAsync(AuthRequest request, CancellationToken cancellationToken);
        Task<CharacterListResponse> ListCharactersAsync(CancellationToken cancellationToken);
        Task<CharacterSummary> CreateCharacterAsync(CreateCharacterRequest request, string idempotencyKey, CancellationToken cancellationToken);
        Task<AdmissionResponse> SelectCharacterAsync(string characterId, string contentReleaseId, string idempotencyKey, CancellationToken cancellationToken);
        Task<BootstrapResponse> BootstrapAsync(string characterId, string clientVersion, string idempotencyKey, CancellationToken cancellationToken);
    }
}
