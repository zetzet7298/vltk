using NUnit.Framework;
using UnityEngine;
using VLTK.Production.World.Unity;

namespace VLTK.Tests.Production.EditMode
{
    public sealed class MapRuntimeValidationTests
    {
        [Test]
        public void Validate_RejectsUnsignedArtifact_InProductionSignatureMode()
        {
            var result = MapRuntimeValidator.Validate(Catalog(), Manifest(), Signature(false), MapRuntimeContract.PinnedArtifactSha256, MapRuntimeContract.PinnedProvenanceSha256, MapRuntimeContract.PinnedSignatureSha256, MapRuntimeTrustMode.ProductionSignature);
            Assert.That(result.ok, Is.False);
            Assert.That(result.code, Is.EqualTo("signature_rejected"));
        }

        [Test]
        public void Validate_AcceptsEditorPinnedDigest_ForExactMapRuntimeHashesOnly()
        {
            var result = MapRuntimeValidator.Validate(Catalog(), Manifest(), Signature(false), MapRuntimeContract.PinnedArtifactSha256, MapRuntimeContract.PinnedProvenanceSha256, MapRuntimeContract.PinnedSignatureSha256, MapRuntimeTrustMode.EditorPinnedDigest);
            Assert.That(result.ok, Is.True, result.code);
        }

        [TestCase("non_canonical_map")]
        [TestCase("forbidden_fallback")]
        [TestCase("editor_pinned_digest_mismatch")]
        public void Validate_FailsClosed_ForContractViolations(string violation)
        {
            var catalog = Catalog();
            var manifest = Manifest();
            string artifactHash = MapRuntimeContract.PinnedArtifactSha256;
            if (violation == "non_canonical_map") manifest.mapId = 79;
            if (violation == "forbidden_fallback") catalog.security.testDataAllowed = true;
            if (violation == "editor_pinned_digest_mismatch") artifactHash = new string('b', 64);
            var result = MapRuntimeValidator.Validate(catalog, manifest, Signature(false), artifactHash, MapRuntimeContract.PinnedProvenanceSha256, MapRuntimeContract.PinnedSignatureSha256, MapRuntimeTrustMode.EditorPinnedDigest);
            Assert.That(result.ok, Is.False);
            Assert.That(result.code, Is.EqualTo(violation));
        }

        internal static MapRuntimeCatalog Catalog()
        {
            return new MapRuntimeCatalog { schema = MapRuntimeContract.CatalogSchema, security = new MapRuntimeCatalogSecurity(), maps = new[] { new MapRuntimeCatalogMap { mapId = 53, artifact = "map-runtime.v1.json", sha256 = MapRuntimeContract.PinnedArtifactSha256 } } };
        }

        internal static MapRuntimeManifest Manifest()
        {
            return new MapRuntimeManifest
            {
                schema = MapRuntimeContract.ManifestSchema,
                mapId = 53,
                canonicalIdentity = new CanonicalIdentity { mapId = 53, nameVi = "Ba Lăng huyện" },
                sourceProvenanceSha256 = MapRuntimeContract.PinnedProvenanceSha256,
                bounds = new MapRuntimeBounds { world = new MapRuntimeWorldRect { x = 41984, y = -55808, width = 16896, height = 10240 } },
                spawn = new MapRuntimeSpawn { world = new MapRuntimeWorldPoint { x = 50432, y = -50432 }, regionCell = new[] { 98, 98 } },
                movement = new MapRuntimeMovement { rules = new MapRuntimeMovementRules { allowMapIds = new[] { 53 }, rejectMapIds = new[] { 79 }, requiresWalkableRegionCell = true } },
                walkability = new MapRuntimeWalkability { walkableRegionCells = new[] { new[] { 98, 98 } } }
            };
        }

        private static MapRuntimeSignatureFile Signature(bool verified)
        {
            return new MapRuntimeSignatureFile { artifactSha256 = MapRuntimeContract.PinnedArtifactSha256, signature = verified ? "prod-sig" : null, verification = new MapRuntimeSignatureVerification { productionSignatureVerified = verified } };
        }
    }
}
