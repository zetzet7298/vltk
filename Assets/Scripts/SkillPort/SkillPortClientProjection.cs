using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf;

namespace VLTK.SkillPort
{
    public enum SkillPortProjectionLoadFailure
    {
        None = 0,
        MissingFile = 1,
        InvalidJson = 2,
        UnknownField = 3,
        MissingField = 4,
        SchemaMismatch = 5,
        CountMismatch = 6,
        HashMismatch = 7,
        InvalidProtobuf = 8,
        SignatureRejected = 9,
        BlockedSkillExposure = 10,
        MissingDependencyData = 11,
    }

    public enum SkillPortManifestTrustPolicy
    {
        Production = 0,
        DevelopmentFixture = 1,
    }

    public interface ISkillPortManifestVerifier
    {
        bool VerifyManifestSignature(
            string signingKeyId,
            byte[] canonicalSigningPayload,
            string signatureBase64,
            SkillPortManifestTrustPolicy policy,
            out string detail);
    }

    public static class SkillPortManifestVerifiers
    {
        public static readonly ISkillPortManifestVerifier Production = new ProductionManifestVerifier();
        public static readonly ISkillPortManifestVerifier DevelopmentFixture = new DevelopmentFixtureManifestVerifier();

        public const string TestOnlySigningKeyId = "test-only-skill-port-ed25519-fixture-v1";
        public const string FixtureSignatureBase64 = "hJ7AgHb2iYtumL0Usw3UyIjY+eKnYiNilPR7JdQCR1IpQ3PYq9iUHDDL72a5KXsapaJqaa/NrPlz0HW4KKvWDQ==";
        public const string FixtureSigningPayloadSha256 = "36e167aa4cea33eff67a9b809e04125a199d86833a7e0f196266e43548102982";

        private sealed class ProductionManifestVerifier : ISkillPortManifestVerifier
        {
            public bool VerifyManifestSignature(
                string signingKeyId,
                byte[] canonicalSigningPayload,
                string signatureBase64,
                SkillPortManifestTrustPolicy policy,
                out string detail)
            {
                if (string.IsNullOrEmpty(signingKeyId) || signingKeyId.StartsWith("test-only-", StringComparison.Ordinal))
                {
                    detail = "production manifest uses forbidden or missing signing key";
                    return false;
                }

                // ponytail: no production keyring shipped in repo fixture; fail closed until real keys land.
                detail = "production manifest verifier has no trusted key for " + signingKeyId;
                return false;
            }
        }

        private sealed class DevelopmentFixtureManifestVerifier : ISkillPortManifestVerifier
        {
            public bool VerifyManifestSignature(
                string signingKeyId,
                byte[] canonicalSigningPayload,
                string signatureBase64,
                SkillPortManifestTrustPolicy policy,
                out string detail)
            {
                if (policy != SkillPortManifestTrustPolicy.DevelopmentFixture)
                {
                    detail = "development fixture verifier disabled by policy";
                    return false;
                }
                if (!string.Equals(signingKeyId, TestOnlySigningKeyId, StringComparison.Ordinal) ||
                    !string.Equals(signatureBase64, FixtureSignatureBase64, StringComparison.Ordinal) ||
                    !string.Equals(Sha256(canonicalSigningPayload), FixtureSigningPayloadSha256, StringComparison.Ordinal))
                {
                    detail = "development fixture signature mismatch";
                    return false;
                }
                detail = null;
                return true;
            }
        }

        internal static string Sha256(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return LowerHex(sha.ComputeHash(bytes));
        }

        private static string LowerHex(byte[] bytes)
        {
            char[] chars = new char[bytes.Length * 2];
            const string hex = "0123456789abcdef";
            for (int i = 0; i < bytes.Length; i++)
            {
                chars[i * 2] = hex[bytes[i] >> 4];
                chars[i * 2 + 1] = hex[bytes[i] & 0xF];
            }
            return new string(chars);
        }
    }

    public readonly struct SkillPortProjectionLoadResult
    {
        public readonly SkillPortClientProjection projection;
        public readonly SkillPortProjectionLoadFailure failure;
        public readonly string detail;

        public bool success => failure == SkillPortProjectionLoadFailure.None && projection != null;

        public SkillPortProjectionLoadResult(
            SkillPortClientProjection projection,
            SkillPortProjectionLoadFailure failure,
            string detail)
        {
            this.projection = projection;
            this.failure = failure;
            this.detail = detail;
        }
    }

    public sealed class SkillPortClientProjection
    {
        public const string ExpectedSchema = "vltk.skill_port.client_projection/v1";
        public const int ExpectedCatalogUnionSize = 242;

        private readonly Dictionary<int, SkillPortClientSkillRow> _rowsBySkillId =
            new Dictionary<int, SkillPortClientSkillRow>();

        public string schema;
        public string toolRevision;
        public string manifestSha256;
        public string projectionSha256;
        public string contentReleaseId;
        public string sourceSnapshotId;
        public string protocolManifestSha256;
        public string catalogUnionSha256;
        public string runtimeSkillPolicyId;
        public List<SkillPortClientSkillRow> rows = new List<SkillPortClientSkillRow>();

        public IReadOnlyDictionary<int, SkillPortClientSkillRow> rowsBySkillId => _rowsBySkillId;

        public bool TryGetRow(int skillId, out SkillPortClientSkillRow row)
        {
            return _rowsBySkillId.TryGetValue(skillId, out row);
        }

        internal bool IndexRows()
        {
            _rowsBySkillId.Clear();
            if (rows == null || rows.Count != ExpectedCatalogUnionSize)
                return false;
            foreach (SkillPortClientSkillRow row in rows)
            {
                if (row == null || row.skillId <= 0 || _rowsBySkillId.ContainsKey(row.skillId))
                    return false;
                _rowsBySkillId.Add(row.skillId, row);
            }
            return true;
        }
    }

    public sealed class SkillPortClientSkillRow
    {
        public int skillId;
        public string skillName;
        public string exposureState;
        public List<string> blockers = new List<string>();
        public List<SkillPortFactionProjection> factions = new List<SkillPortFactionProjection>();
        public List<SkillPortAssetDependencyProjection> assetDependencies = new List<SkillPortAssetDependencyProjection>();

        public bool blocked => blockers == null || blockers.Count > 0 || !string.Equals(exposureState, "exposed", StringComparison.Ordinal);
    }

    public sealed class SkillPortFactionProjection
    {
        public string key;
        public string name;
        public string classification;
    }

    public sealed class SkillPortAssetDependencyProjection
    {
        public int skillId;
        public string kind;
        public string sourceField;
        public string sourcePath;
        public string status;
        public List<string> blockers = new List<string>();
    }

    public static class SkillPortClientProjectionLoader
    {
        private const string ClientPbFileName = "skill_port.client.pb";
        private const string ClientJsonFileName = "skill_port.client.json";
        private const string ManifestFileName = "manifest.json";

        public static SkillPortProjectionLoadResult LoadFromDirectory(string directory)
        {
            return LoadFromDirectory(directory, SkillPortManifestVerifiers.Production, SkillPortManifestTrustPolicy.Production);
        }

        public static SkillPortProjectionLoadResult LoadDevelopmentFixtureFromDirectory(string directory)
        {
            return LoadFromDirectory(directory, SkillPortManifestVerifiers.DevelopmentFixture, SkillPortManifestTrustPolicy.DevelopmentFixture);
        }

        public static SkillPortProjectionLoadResult LoadFromDirectory(
            string directory,
            ISkillPortManifestVerifier verifier,
            SkillPortManifestTrustPolicy policy)
        {
            if (string.IsNullOrEmpty(directory))
                return Fail(SkillPortProjectionLoadFailure.MissingFile, "projection directory is required");
            return Load(
                Path.Combine(directory, ClientPbFileName),
                Path.Combine(directory, ManifestFileName),
                verifier,
                policy);
        }

        public static SkillPortProjectionLoadResult Load(
            string clientPbPath,
            string manifestPath,
            ISkillPortManifestVerifier verifier,
            SkillPortManifestTrustPolicy policy)
        {
            try
            {
                if (string.IsNullOrEmpty(clientPbPath) || !File.Exists(clientPbPath))
                    return Fail(SkillPortProjectionLoadFailure.MissingFile, "client protobuf projection is missing");
                if (string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath))
                    return Fail(SkillPortProjectionLoadFailure.MissingFile, "manifest is missing");
                if (verifier == null)
                    return Fail(SkillPortProjectionLoadFailure.SignatureRejected, "manifest verifier is required");

                byte[] clientBytes = File.ReadAllBytes(clientPbPath);
                byte[] manifestBytes = File.ReadAllBytes(manifestPath);
                string clientSha = Sha256(clientBytes);
                string manifestFileSha = Sha256(manifestBytes);

                Dictionary<string, object> manifest = ParseManifest(manifestBytes);
                Dictionary<string, object> contentDigest = RequireObject(manifest, "contentDigest", "manifest");
                Dictionary<string, object> runtimeSkillPolicy = RequireObject(manifest, "runtimeSkillPolicy", "manifest");

                if (RequireLong(manifest, "schemaVersion", "manifest") != 1)
                    return Fail(SkillPortProjectionLoadFailure.SchemaMismatch, "manifest schema mismatch");
                if (RequireBool(manifest, "hotReloadAllowed", "manifest"))
                    return Fail(SkillPortProjectionLoadFailure.SchemaMismatch, "manifest hot reload must be disabled");
                if (!ContentReleaseDigest.IsLowerHexSha256(RequireString(manifest, "manifestSha256", "manifest")) ||
                    !string.Equals(RequireString(manifest, "manifestSha256", "manifest"), RequireString(contentDigest, "manifestSha256", "manifest.contentDigest"), StringComparison.Ordinal))
                    return Fail(SkillPortProjectionLoadFailure.HashMismatch, "manifest digest fields mismatch");

                Dictionary<string, object> artifact = FindArtifact(RequireArray(manifest, "artifacts", "manifest"), ClientPbFileName);
                if (artifact == null)
                    return Fail(SkillPortProjectionLoadFailure.MissingField, "manifest missing client protobuf artifact");
                if (RequireLong(artifact, "sizeBytes", "manifest artifact") != clientBytes.Length ||
                    !string.Equals(RequireString(artifact, "sha256", "manifest artifact"), clientSha, StringComparison.Ordinal))
                    return Fail(SkillPortProjectionLoadFailure.HashMismatch, "client protobuf hash or byte count mismatch");
                if (!string.Equals(RequireString(contentDigest, "clientProjectionSha256", "manifest.contentDigest"), clientSha, StringComparison.Ordinal))
                    return Fail(SkillPortProjectionLoadFailure.HashMismatch, "client projection digest mismatch");

                if (RequireLong(contentDigest, "catalogUnionSize", "manifest.contentDigest") != SkillPortClientProjection.ExpectedCatalogUnionSize ||
                    RequireLong(runtimeSkillPolicy, "catalogUnionSize", "manifest.runtimeSkillPolicy") != SkillPortClientProjection.ExpectedCatalogUnionSize)
                    return Fail(SkillPortProjectionLoadFailure.CountMismatch, "manifest catalog union size mismatch");
                if (RequireBool(runtimeSkillPolicy, "filesystemFallbackAllowed", "manifest.runtimeSkillPolicy") ||
                    RequireBool(runtimeSkillPolicy, "runtimeParityClaimed", "manifest.runtimeSkillPolicy"))
                    return Fail(SkillPortProjectionLoadFailure.SchemaMismatch, "manifest runtime policy must fail closed");
                if (!string.Equals(RequireString(contentDigest, "catalogUnionSha256", "manifest.contentDigest"), RequireString(runtimeSkillPolicy, "catalogUnionSha256", "manifest.runtimeSkillPolicy"), StringComparison.Ordinal) ||
                    !string.Equals(RequireString(contentDigest, "runtimeSkillPolicyId", "manifest.contentDigest"), RequireString(runtimeSkillPolicy, "policyId", "manifest.runtimeSkillPolicy"), StringComparison.Ordinal))
                    return Fail(SkillPortProjectionLoadFailure.HashMismatch, "runtime policy digest mismatch");

                byte[] signingPayload = SkillPortJson.CanonicalBytes(WithoutKey(manifest, "signature"));
                if (!verifier.VerifyManifestSignature(
                    RequireString(manifest, "signingKeyId", "manifest"),
                    signingPayload,
                    RequireString(manifest, "signature", "manifest"),
                    policy,
                    out string signatureDetail))
                    return Fail(SkillPortProjectionLoadFailure.SignatureRejected, signatureDetail);

                global::Content.V1.ClientSkillCatalog catalog = global::Content.V1.ClientSkillCatalog.Parser.ParseFrom(clientBytes);
                SkillPortProjectionLoadResult structural = BuildFromProto(catalog, clientSha, manifestFileSha, contentDigest, runtimeSkillPolicy);
                return structural;
            }
            catch (InvalidProtocolBufferException e)
            {
                return Fail(SkillPortProjectionLoadFailure.InvalidProtobuf, e.Message);
            }
            catch (SkillPortJsonException e)
            {
                return Fail(SkillPortProjectionLoadFailure.InvalidJson, e.Message);
            }
            catch (FormatException e)
            {
                return Fail(SkillPortProjectionLoadFailure.UnknownField, e.Message);
            }
            catch (OverflowException e)
            {
                return Fail(SkillPortProjectionLoadFailure.InvalidJson, e.Message);
            }
        }

        // Shadow-only JSON loader for fixture diff/debug. Never production authority.
        public static SkillPortProjectionLoadResult LoadJsonShadowFromDirectory(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return Fail(SkillPortProjectionLoadFailure.MissingFile, "projection directory is required");
            return LoadJsonShadow(Path.Combine(directory, ClientJsonFileName), Path.Combine(directory, ManifestFileName));
        }

        public static SkillPortProjectionLoadResult LoadJsonShadow(string clientJsonPath, string manifestPath)
        {
            try
            {
                if (string.IsNullOrEmpty(clientJsonPath) || !File.Exists(clientJsonPath))
                    return Fail(SkillPortProjectionLoadFailure.MissingFile, "client json projection is missing");
                if (string.IsNullOrEmpty(manifestPath) || !File.Exists(manifestPath))
                    return Fail(SkillPortProjectionLoadFailure.MissingFile, "manifest is missing");

                byte[] clientBytes = File.ReadAllBytes(clientJsonPath);
                byte[] manifestBytes = File.ReadAllBytes(manifestPath);
                string clientSha = Sha256(clientBytes);
                string manifestSha = Sha256(manifestBytes);

                Dictionary<string, object> manifest = ParseManifest(manifestBytes);
                Dictionary<string, object> artifact = FindArtifact(RequireArray(manifest, "artifacts", "manifest"), ClientJsonFileName);
                if (artifact == null)
                    return Fail(SkillPortProjectionLoadFailure.MissingField, "manifest missing client json artifact");
                if (RequireLong(artifact, "sizeBytes", "manifest artifact") != clientBytes.Length ||
                    !string.Equals(RequireString(artifact, "sha256", "manifest artifact"), clientSha, StringComparison.Ordinal))
                    return Fail(SkillPortProjectionLoadFailure.HashMismatch, "client json hash or byte count mismatch");

                Dictionary<string, object> contentDigest = RequireObject(manifest, "contentDigest", "manifest");
                Dictionary<string, object> client = AsObject(SkillPortJson.Parse(Encoding.UTF8.GetString(clientBytes)), "client projection");
                StrictKeys(client, "client projection", "faction_progression_order", "rows", "schema", "tool_revision");
                if (!string.Equals(RequireString(client, "schema", "client projection"), SkillPortClientProjection.ExpectedSchema, StringComparison.Ordinal))
                    return Fail(SkillPortProjectionLoadFailure.SchemaMismatch, "client projection schema mismatch");

                var projection = new SkillPortClientProjection
                {
                    schema = RequireString(client, "schema", "client projection"),
                    toolRevision = RequireString(client, "tool_revision", "client projection"),
                    manifestSha256 = manifestSha,
                    projectionSha256 = clientSha,
                    contentReleaseId = RequireString(contentDigest, "contentReleaseId", "manifest.contentDigest"),
                    sourceSnapshotId = RequireString(contentDigest, "sourceSnapshotId", "manifest.contentDigest"),
                    protocolManifestSha256 = RequireString(contentDigest, "manifestSha256", "manifest.contentDigest"),
                    catalogUnionSha256 = RequireString(contentDigest, "catalogUnionSha256", "manifest.contentDigest"),
                    runtimeSkillPolicyId = RequireString(contentDigest, "runtimeSkillPolicyId", "manifest.contentDigest"),
                };

                List<object> rows = RequireArray(client, "rows", "client projection");
                if (rows.Count != SkillPortClientProjection.ExpectedCatalogUnionSize)
                    return Fail(SkillPortProjectionLoadFailure.CountMismatch, "client projection row count mismatch");
                foreach (object rowValue in rows)
                    projection.rows.Add(ParseJsonRow(AsObject(rowValue, "client projection row")));

                if (!projection.IndexRows())
                    return Fail(SkillPortProjectionLoadFailure.CountMismatch, "client projection contains duplicate or invalid skill id");
                return new SkillPortProjectionLoadResult(projection, SkillPortProjectionLoadFailure.None, null);
            }
            catch (SkillPortJsonException e)
            {
                return Fail(SkillPortProjectionLoadFailure.InvalidJson, e.Message);
            }
            catch (FormatException e)
            {
                return Fail(SkillPortProjectionLoadFailure.UnknownField, e.Message);
            }
            catch (OverflowException e)
            {
                return Fail(SkillPortProjectionLoadFailure.InvalidJson, e.Message);
            }
        }

        public static RuntimePolicySnapshot BuildRuntimePolicy(SkillPortClientProjection projection, ulong policyRevision)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection));

            var policy = new RuntimePolicySnapshot(policyRevision);
            foreach (SkillPortClientSkillRow row in projection.rows)
            {
                string factionKey = row.factions != null && row.factions.Count > 0 ? row.factions[0].key : "unknown";
                policy.SetSkill(new SkillRuntimeMode
                {
                    skillId = row.skillId,
                    factionKey = factionKey,
                    exposed = !row.blocked,
                    authorityMode = row.blocked ? SkillAuthorityMode.Disabled : SkillAuthorityMode.LegacyActiveGoShadow,
                    presentationMode = row.blocked ? SkillPresentationMode.Disabled : SkillPresentationMode.GraphV2Shadow,
                });
            }
            return policy;
        }

        private static SkillPortProjectionLoadResult BuildFromProto(
            global::Content.V1.ClientSkillCatalog catalog,
            string clientSha,
            string manifestFileSha,
            Dictionary<string, object> contentDigest,
            Dictionary<string, object> runtimeSkillPolicy)
        {
            if (catalog == null || catalog.Header == null || catalog.RuntimeSkillPolicy == null)
                return Fail(SkillPortProjectionLoadFailure.MissingField, "client protobuf header or runtime policy missing");
            if (catalog.Header.SchemaVersion != 1 || !string.Equals(catalog.Header.ProjectionName, "client", StringComparison.Ordinal))
                return Fail(SkillPortProjectionLoadFailure.SchemaMismatch, "client protobuf projection header mismatch");
            if (catalog.Header.CatalogUnionSize != SkillPortClientProjection.ExpectedCatalogUnionSize ||
                catalog.Rows.Count != SkillPortClientProjection.ExpectedCatalogUnionSize)
                return Fail(SkillPortProjectionLoadFailure.CountMismatch, "client protobuf row count mismatch");
            if (!string.Equals(catalog.Header.CatalogUnionSha256, RequireString(contentDigest, "catalogUnionSha256", "manifest.contentDigest"), StringComparison.Ordinal) ||
                !string.Equals(catalog.Header.SourceSnapshotId, RequireString(contentDigest, "sourceSnapshotId", "manifest.contentDigest"), StringComparison.Ordinal) ||
                !string.Equals(catalog.RuntimeSkillPolicy.PolicyId, RequireString(runtimeSkillPolicy, "policyId", "manifest.runtimeSkillPolicy"), StringComparison.Ordinal) ||
                !string.Equals(catalog.RuntimeSkillPolicy.CatalogUnionSha256, catalog.Header.CatalogUnionSha256, StringComparison.Ordinal))
                return Fail(SkillPortProjectionLoadFailure.HashMismatch, "client protobuf digest mismatch");
            if (catalog.RuntimeSkillPolicy.FilesystemFallbackAllowed || catalog.RuntimeSkillPolicy.RuntimeParityClaimed)
                return Fail(SkillPortProjectionLoadFailure.SchemaMismatch, "client protobuf runtime policy must fail closed");

            var projection = new SkillPortClientProjection
            {
                schema = SkillPortClientProjection.ExpectedSchema,
                toolRevision = catalog.Header.ToolRevision,
                manifestSha256 = manifestFileSha,
                projectionSha256 = clientSha,
                contentReleaseId = RequireString(contentDigest, "contentReleaseId", "manifest.contentDigest"),
                sourceSnapshotId = catalog.Header.SourceSnapshotId,
                protocolManifestSha256 = RequireString(contentDigest, "manifestSha256", "manifest.contentDigest"),
                catalogUnionSha256 = catalog.Header.CatalogUnionSha256,
                runtimeSkillPolicyId = catalog.RuntimeSkillPolicy.PolicyId,
            };

            foreach (global::Content.V1.ClientSkillRow row in catalog.Rows)
            {
                SkillPortClientSkillRow parsed = ParseProtoRow(row);
                if (parsed.blockers == null || parsed.blockers.Count == 0)
                    return Fail(SkillPortProjectionLoadFailure.BlockedSkillExposure, "client protobuf row lacks blockers");
                foreach (SkillPortAssetDependencyProjection dependency in parsed.assetDependencies)
                {
                    if (dependency.blockers == null || dependency.blockers.Count == 0 || dependency.skillId != parsed.skillId)
                        return Fail(SkillPortProjectionLoadFailure.MissingDependencyData, "client protobuf dependency lacks blocker data");
                }
                projection.rows.Add(parsed);
            }

            if (!projection.IndexRows())
                return Fail(SkillPortProjectionLoadFailure.CountMismatch, "client protobuf contains duplicate or invalid skill id");
            foreach (SkillPortClientSkillRow row in projection.rows)
            {
                if (!row.blocked)
                    return Fail(SkillPortProjectionLoadFailure.BlockedSkillExposure, "client protobuf would expose unblocked skill");
            }
            return new SkillPortProjectionLoadResult(projection, SkillPortProjectionLoadFailure.None, null);
        }

        private static SkillPortClientSkillRow ParseProtoRow(global::Content.V1.ClientSkillRow row)
        {
            if (row == null || row.SkillId == 0 || string.IsNullOrEmpty(row.SkillName))
                throw new FormatException("client protobuf row has missing required value");
            var parsed = new SkillPortClientSkillRow
            {
                skillId = CheckedInt(row.SkillId),
                skillName = row.SkillName,
                exposureState = ExposureToString(row.ExposureState),
                blockers = new List<string>(row.Blockers),
                factions = new List<SkillPortFactionProjection>(),
                assetDependencies = new List<SkillPortAssetDependencyProjection>(),
            };
            foreach (global::Content.V1.FactionMembership faction in row.Factions)
            {
                parsed.factions.Add(new SkillPortFactionProjection
                {
                    classification = faction.Classification,
                    key = faction.Key,
                    name = faction.Name,
                });
            }
            foreach (global::Content.V1.AssetDependency dependency in row.AssetDependencies)
            {
                parsed.assetDependencies.Add(new SkillPortAssetDependencyProjection
                {
                    blockers = new List<string>(dependency.Blockers),
                    kind = dependency.Kind,
                    skillId = CheckedInt(dependency.SkillId),
                    sourceField = dependency.SourceField,
                    sourcePath = dependency.SourcePath,
                    status = dependency.Status,
                });
            }
            return parsed;
        }

        private static SkillPortClientSkillRow ParseJsonRow(Dictionary<string, object> row)
        {
            StrictKeys(row, "client projection row",
                "asset_dependencies", "blockers", "exposure_state", "factions", "presentation_fields", "skill_id", "skill_name", "state_relation");
            RequireObject(row, "presentation_fields", "client projection row");
            RequireObject(row, "state_relation", "client projection row");

            var parsed = new SkillPortClientSkillRow
            {
                skillId = (int)RequireLong(row, "skill_id", "client projection row"),
                skillName = RequireString(row, "skill_name", "client projection row"),
                exposureState = RequireString(row, "exposure_state", "client projection row"),
                blockers = ParseStringArray(RequireArray(row, "blockers", "client projection row"), "client projection row.blockers"),
                factions = ParseJsonFactions(RequireArray(row, "factions", "client projection row")),
                assetDependencies = ParseJsonAssetDependencies(RequireArray(row, "asset_dependencies", "client projection row")),
            };
            if (parsed.skillId <= 0 || string.IsNullOrEmpty(parsed.skillName) || string.IsNullOrEmpty(parsed.exposureState))
                throw new FormatException("client projection row has missing required value");
            return parsed;
        }

        private static Dictionary<string, object> ParseManifest(byte[] manifestBytes)
        {
            Dictionary<string, object> manifest = AsObject(SkillPortJson.Parse(Encoding.UTF8.GetString(manifestBytes)), "manifest");
            StrictKeys(manifest, "manifest",
                "artifacts", "contentDigest", "createdAt", "hotReloadAllowed", "luaPolicy", "manifestSha256", "realmId",
                "releaseId", "runtimeSkillPolicy", "schemaVersion", "signature", "signingKeyId", "sourceSnapshot",
                "userFacingLocale", "version");
            Dictionary<string, object> contentDigest = RequireObject(manifest, "contentDigest", "manifest");
            StrictKeys(contentDigest, "manifest.contentDigest",
                "catalogUnionSha256", "catalogUnionSize", "clientProjectionSha256", "contentReleaseId", "manifestSha256",
                "runtimeSkillPolicyId", "sourceSnapshotId");
            Dictionary<string, object> runtimeSkillPolicy = RequireObject(manifest, "runtimeSkillPolicy", "manifest");
            StrictKeys(runtimeSkillPolicy, "manifest.runtimeSkillPolicy",
                "androidPhysicalEvidenceStatus", "catalogUnionSha256", "catalogUnionSize", "filesystemFallbackAllowed",
                "pcRuntimeEvidenceStatus", "policyId", "runtimeParityClaimed", "sourceTool");
            return manifest;
        }

        private static Dictionary<string, object> FindArtifact(List<object> artifacts, string logicalPath)
        {
            foreach (object value in artifacts)
            {
                Dictionary<string, object> artifact = AsObject(value, "manifest.artifacts[]");
                StrictKeys(artifact, "manifest.artifacts[]", "kind", "logicalPath", "mediaType", "provenance", "sha256", "sizeBytes", "uri");
                Dictionary<string, object> provenance = RequireObject(artifact, "provenance", "manifest.artifacts[]");
                StrictKeys(provenance, "manifest.artifacts[].provenance", "discoveryTool", "parserName", "parserVersion", "sourcePath", "sourceSnapshotId");
                if (string.Equals(RequireString(artifact, "logicalPath", "manifest.artifacts[]"), logicalPath, StringComparison.Ordinal))
                    return artifact;
            }
            return null;
        }

        private static List<SkillPortFactionProjection> ParseJsonFactions(List<object> values)
        {
            var result = new List<SkillPortFactionProjection>();
            foreach (object value in values)
            {
                Dictionary<string, object> faction = AsObject(value, "row.factions[]");
                StrictKeys(faction, "row.factions[]", "classification", "key", "name");
                result.Add(new SkillPortFactionProjection
                {
                    classification = RequireString(faction, "classification", "row.factions[]"),
                    key = RequireString(faction, "key", "row.factions[]"),
                    name = RequireString(faction, "name", "row.factions[]"),
                });
            }
            return result;
        }

        private static List<SkillPortAssetDependencyProjection> ParseJsonAssetDependencies(List<object> values)
        {
            var result = new List<SkillPortAssetDependencyProjection>();
            foreach (object value in values)
            {
                Dictionary<string, object> dependency = AsObject(value, "row.asset_dependencies[]");
                StrictKeys(dependency, "row.asset_dependencies[]", "blockers", "kind", "skill_id", "source_field", "source_path", "status");
                result.Add(new SkillPortAssetDependencyProjection
                {
                    blockers = ParseStringArray(RequireArray(dependency, "blockers", "row.asset_dependencies[]"), "row.asset_dependencies[].blockers"),
                    kind = RequireString(dependency, "kind", "row.asset_dependencies[]"),
                    skillId = (int)RequireLong(dependency, "skill_id", "row.asset_dependencies[]"),
                    sourceField = RequireString(dependency, "source_field", "row.asset_dependencies[]"),
                    sourcePath = RequireString(dependency, "source_path", "row.asset_dependencies[]"),
                    status = RequireString(dependency, "status", "row.asset_dependencies[]"),
                });
            }
            return result;
        }

        private static List<string> ParseStringArray(List<object> values, string context)
        {
            var result = new List<string>();
            foreach (object value in values)
            {
                if (!(value is string text) || string.IsNullOrEmpty(text))
                    throw new FormatException(context + " must contain non-empty strings only");
                result.Add(text);
            }
            return result;
        }

        private static Dictionary<string, object> WithoutKey(Dictionary<string, object> source, string removedKey)
        {
            var copy = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, object> item in source)
            {
                if (!string.Equals(item.Key, removedKey, StringComparison.Ordinal))
                    copy.Add(item.Key, CloneJson(item.Value));
            }
            return copy;
        }

        private static object CloneJson(object value)
        {
            if (value is Dictionary<string, object> obj)
            {
                var copy = new Dictionary<string, object>(StringComparer.Ordinal);
                foreach (KeyValuePair<string, object> item in obj)
                    copy.Add(item.Key, CloneJson(item.Value));
                return copy;
            }
            if (value is List<object> list)
            {
                var copy = new List<object>(list.Count);
                foreach (object item in list)
                    copy.Add(CloneJson(item));
                return copy;
            }
            return value;
        }

        private static string ExposureToString(global::Content.V1.ExposureState state)
        {
            switch (state)
            {
                case global::Content.V1.ExposureState.Exposed: return "exposed";
                case global::Content.V1.ExposureState.PcOnly: return "pc_only";
                case global::Content.V1.ExposureState.EvidencePending: return "evidence_pending";
                default: return "unspecified";
            }
        }

        private static int CheckedInt(uint value)
        {
            if (value > int.MaxValue)
                throw new OverflowException("uint value exceeds int range");
            return (int)value;
        }

        private static Dictionary<string, object> RequireObject(Dictionary<string, object> obj, string key, string context)
        {
            if (!obj.TryGetValue(key, out object value))
                throw new FormatException(context + " missing field " + key);
            return AsObject(value, context + "." + key);
        }

        private static Dictionary<string, object> AsObject(object value, string context)
        {
            var obj = value as Dictionary<string, object>;
            if (obj == null)
                throw new SkillPortJsonException(context + " must be an object");
            return obj;
        }

        private static List<object> RequireArray(Dictionary<string, object> obj, string key, string context)
        {
            if (!obj.TryGetValue(key, out object value))
                throw new FormatException(context + " missing field " + key);
            var list = value as List<object>;
            if (list == null)
                throw new SkillPortJsonException(context + "." + key + " must be an array");
            return list;
        }

        private static string RequireString(Dictionary<string, object> obj, string key, string context)
        {
            if (!obj.TryGetValue(key, out object value))
                throw new FormatException(context + " missing field " + key);
            var text = value as string;
            if (text == null)
                throw new SkillPortJsonException(context + "." + key + " must be a string");
            return text;
        }

        private static long RequireLong(Dictionary<string, object> obj, string key, string context)
        {
            if (!obj.TryGetValue(key, out object value))
                throw new FormatException(context + " missing field " + key);
            if (!(value is long number))
                throw new SkillPortJsonException(context + "." + key + " must be an integer");
            return number;
        }

        private static bool RequireBool(Dictionary<string, object> obj, string key, string context)
        {
            if (!obj.TryGetValue(key, out object value))
                throw new FormatException(context + " missing field " + key);
            if (!(value is bool flag))
                throw new SkillPortJsonException(context + "." + key + " must be a boolean");
            return flag;
        }

        private static void StrictKeys(Dictionary<string, object> obj, string context, params string[] keys)
        {
            if (obj.Count != keys.Length)
            {
                foreach (string key in obj.Keys)
                {
                    bool known = false;
                    for (int i = 0; i < keys.Length; i++)
                        known |= string.Equals(key, keys[i], StringComparison.Ordinal);
                    if (!known)
                        throw new FormatException(context + " has unknown field " + key);
                }
                throw new FormatException(context + " missing required field");
            }

            for (int i = 0; i < keys.Length; i++)
            {
                if (!obj.ContainsKey(keys[i]))
                    throw new FormatException(context + " missing field " + keys[i]);
            }
        }

        private static string Sha256(byte[] bytes)
        {
            return SkillPortManifestVerifiers.Sha256(bytes);
        }

        private static SkillPortProjectionLoadResult Fail(SkillPortProjectionLoadFailure failure, string detail)
        {
            return new SkillPortProjectionLoadResult(null, failure, detail);
        }
    }

    internal sealed class SkillPortJsonException : Exception
    {
        public SkillPortJsonException(string message) : base(message) { }
    }

    internal static class SkillPortJson
    {
        public static object Parse(string json)
        {
            var parser = new Parser(json);
            object value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.End)
                throw new SkillPortJsonException("trailing json content");
            return value;
        }

        public static byte[] CanonicalBytes(object value)
        {
            var builder = new StringBuilder();
            WriteCanonical(value, builder, 0);
            builder.Append('\n');
            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        private static void WriteCanonical(object value, StringBuilder builder, int depth)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }
            if (value is string text)
            {
                WriteString(text, builder);
                return;
            }
            if (value is long number)
            {
                builder.Append(number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return;
            }
            if (value is bool flag)
            {
                builder.Append(flag ? "true" : "false");
                return;
            }
            if (value is List<object> list)
            {
                builder.Append('[');
                if (list.Count > 0)
                {
                    builder.Append('\n');
                    for (int i = 0; i < list.Count; i++)
                    {
                        Indent(builder, depth + 1);
                        WriteCanonical(list[i], builder, depth + 1);
                        if (i + 1 < list.Count)
                            builder.Append(',');
                        builder.Append('\n');
                    }
                    Indent(builder, depth);
                }
                builder.Append(']');
                return;
            }
            if (value is Dictionary<string, object> obj)
            {
                builder.Append('{');
                if (obj.Count > 0)
                {
                    var keys = new List<string>(obj.Keys);
                    keys.Sort(StringComparer.Ordinal);
                    builder.Append('\n');
                    for (int i = 0; i < keys.Count; i++)
                    {
                        Indent(builder, depth + 1);
                        WriteString(keys[i], builder);
                        builder.Append(": ");
                        WriteCanonical(obj[keys[i]], builder, depth + 1);
                        if (i + 1 < keys.Count)
                            builder.Append(',');
                        builder.Append('\n');
                    }
                    Indent(builder, depth);
                }
                builder.Append('}');
                return;
            }
            throw new SkillPortJsonException("unsupported canonical json value");
        }

        private static void WriteString(string value, StringBuilder builder)
        {
            builder.Append('"');
            foreach (char c in value)
            {
                switch (c)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (c < ' ')
                            builder.Append("\\u").Append(((int)c).ToString("x4", System.Globalization.CultureInfo.InvariantCulture));
                        else
                            builder.Append(c);
                        break;
                }
            }
            builder.Append('"');
        }

        private static void Indent(StringBuilder builder, int depth)
        {
            for (int i = 0; i < depth; i++)
                builder.Append("  ");
        }

        private sealed class Parser
        {
            private readonly string _json;
            private int _index;

            public Parser(string json)
            {
                _json = json ?? string.Empty;
            }

            public bool End => _index >= _json.Length;

            public object ParseValue()
            {
                SkipWhitespace();
                if (End)
                    throw new SkillPortJsonException("unexpected end of json");

                char c = _json[_index];
                if (c == '{') return ParseObject();
                if (c == '[') return ParseArray();
                if (c == '"') return ParseString();
                if (c == '-' || (c >= '0' && c <= '9')) return ParseInteger();
                if (Match("true")) return true;
                if (Match("false")) return false;
                if (Match("null")) return null;
                throw new SkillPortJsonException("unexpected json token at " + _index);
            }

            public void SkipWhitespace()
            {
                while (!End)
                {
                    char c = _json[_index];
                    if (c != ' ' && c != '\n' && c != '\r' && c != '\t')
                        return;
                    _index++;
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                Expect('{');
                var obj = new Dictionary<string, object>(StringComparer.Ordinal);
                SkipWhitespace();
                if (TryConsume('}'))
                    return obj;

                while (true)
                {
                    SkipWhitespace();
                    string key = ParseString();
                    if (obj.ContainsKey(key))
                        throw new SkillPortJsonException("duplicate json field " + key);
                    SkipWhitespace();
                    Expect(':');
                    obj.Add(key, ParseValue());
                    SkipWhitespace();
                    if (TryConsume('}'))
                        return obj;
                    Expect(',');
                }
            }

            private List<object> ParseArray()
            {
                Expect('[');
                var list = new List<object>();
                SkipWhitespace();
                if (TryConsume(']'))
                    return list;

                while (true)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (TryConsume(']'))
                        return list;
                    Expect(',');
                }
            }

            private string ParseString()
            {
                Expect('"');
                var builder = new StringBuilder();
                while (!End)
                {
                    char c = _json[_index++];
                    if (c == '"')
                        return builder.ToString();
                    if (c != '\\')
                    {
                        builder.Append(c);
                        continue;
                    }
                    if (End)
                        throw new SkillPortJsonException("unterminated json escape");
                    char escaped = _json[_index++];
                    switch (escaped)
                    {
                        case '"': builder.Append('"'); break;
                        case '\\': builder.Append('\\'); break;
                        case '/': builder.Append('/'); break;
                        case 'b': builder.Append('\b'); break;
                        case 'f': builder.Append('\f'); break;
                        case 'n': builder.Append('\n'); break;
                        case 'r': builder.Append('\r'); break;
                        case 't': builder.Append('\t'); break;
                        case 'u': builder.Append(ParseUnicodeEscape()); break;
                        default: throw new SkillPortJsonException("invalid json escape");
                    }
                }
                throw new SkillPortJsonException("unterminated json string");
            }

            private char ParseUnicodeEscape()
            {
                if (_index + 4 > _json.Length)
                    throw new SkillPortJsonException("invalid unicode escape");
                int value = 0;
                for (int i = 0; i < 4; i++)
                {
                    char c = _json[_index++];
                    value <<= 4;
                    if (c >= '0' && c <= '9') value += c - '0';
                    else if (c >= 'a' && c <= 'f') value += c - 'a' + 10;
                    else if (c >= 'A' && c <= 'F') value += c - 'A' + 10;
                    else throw new SkillPortJsonException("invalid unicode escape");
                }
                return (char)value;
            }

            private long ParseInteger()
            {
                int start = _index;
                if (_json[_index] == '-')
                    _index++;
                if (End || _json[_index] < '0' || _json[_index] > '9')
                    throw new SkillPortJsonException("invalid json number");
                if (_json[_index] == '0')
                    _index++;
                else
                    while (!End && _json[_index] >= '0' && _json[_index] <= '9')
                        _index++;
                if (!End && (_json[_index] == '.' || _json[_index] == 'e' || _json[_index] == 'E'))
                    throw new SkillPortJsonException("floating point json numbers are unsupported");
                return long.Parse(_json.Substring(start, _index - start), System.Globalization.CultureInfo.InvariantCulture);
            }

            private bool Match(string token)
            {
                if (_index + token.Length > _json.Length)
                    return false;
                for (int i = 0; i < token.Length; i++)
                {
                    if (_json[_index + i] != token[i])
                        return false;
                }
                _index += token.Length;
                return true;
            }

            private bool TryConsume(char expected)
            {
                if (!End && _json[_index] == expected)
                {
                    _index++;
                    return true;
                }
                return false;
            }

            private void Expect(char expected)
            {
                if (End || _json[_index] != expected)
                    throw new SkillPortJsonException("expected '" + expected + "' at " + _index);
                _index++;
            }
        }
    }
}
