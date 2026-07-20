using System;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace VLTK.Production.World.Unity
{
    public enum MapRuntimeTrustMode { ProductionSignature, EditorPinnedDigest }

    public readonly struct MapRuntimeValidationResult
    {
        public readonly bool ok;
        public readonly string code;
        public MapRuntimeValidationResult(bool ok, string code) { this.ok = ok; this.code = code; }
    }

    public static class MapRuntimeValidator
    {
        public static MapRuntimeValidationResult Validate(
            MapRuntimeCatalog catalog,
            MapRuntimeManifest manifest,
            MapRuntimeSignatureFile signature,
            string artifactSha256,
            string provenanceSha256,
            string signatureSha256,
            MapRuntimeTrustMode trustMode)
        {
            if (catalog == null || manifest == null || signature == null) return Fail("missing_manifest");
            if (catalog.schema != MapRuntimeContract.CatalogSchema || manifest.schema != MapRuntimeContract.ManifestSchema) return Fail("schema_mismatch");
            if (manifest.mapId != MapRuntimeContract.CanonicalMapId || manifest.canonicalIdentity == null || manifest.canonicalIdentity.mapId != MapRuntimeContract.CanonicalMapId) return Fail("non_canonical_map");
            if (catalog.security == null || catalog.security.aliasRemapAllowed || catalog.security.filesystemFallbackAllowed || catalog.security.map79Allowed || catalog.security.testDataAllowed) return Fail("forbidden_fallback");
            var rules = manifest.movement != null ? manifest.movement.rules : null;
            if (rules == null || rules.aliasRemapAllowed || rules.filesystemFallbackAllowed || rules.testDataAllowed || rules.loosePcFolderFallbackAllowed || rules.absoluteRuntimePathsAllowed || !rules.requiresWalkableRegionCell || !Contains(rules.allowMapIds, 53) || !Contains(rules.rejectMapIds, 79)) return Fail("forbidden_fallback");
            if (manifest.bounds == null || manifest.bounds.world == null || manifest.bounds.world.width <= 0f || manifest.bounds.world.height <= 0f) return Fail("invalid_bounds");
            if (manifest.spawn == null || manifest.spawn.world == null || !manifest.bounds.world.ToRect().Contains(manifest.spawn.world.ToVector2())) return Fail("invalid_spawn");
            if (!string.Equals(manifest.sourceProvenanceSha256, provenanceSha256, StringComparison.OrdinalIgnoreCase)) return Fail("provenance_mismatch");
            if (trustMode == MapRuntimeTrustMode.ProductionSignature)
            {
                if (!string.Equals(signature.artifactSha256, artifactSha256, StringComparison.OrdinalIgnoreCase)) return Fail("signature_artifact_mismatch");
                return signature.verification != null && signature.verification.productionSignatureVerified && !string.IsNullOrEmpty(signature.signature) ? new MapRuntimeValidationResult(true, null) : Fail("signature_rejected");
            }
            if (!string.Equals(artifactSha256, MapRuntimeContract.PinnedArtifactSha256, StringComparison.OrdinalIgnoreCase) || !string.Equals(provenanceSha256, MapRuntimeContract.PinnedProvenanceSha256, StringComparison.OrdinalIgnoreCase) || !string.Equals(signatureSha256, MapRuntimeContract.PinnedSignatureSha256, StringComparison.OrdinalIgnoreCase)) return Fail("editor_pinned_digest_mismatch");
            return new MapRuntimeValidationResult(true, null);
        }

        public static bool IsWalkableRegionCell(MapRuntimeManifest manifest, int col, int row)
        {
            var cells = manifest != null && manifest.walkability != null ? manifest.walkability.walkableRegionCells : null;
            if (cells == null) return false;
            for (int i = 0; i < cells.Length; i++) if (cells[i] != null && cells[i].Length == 2 && cells[i][0] == col && cells[i][1] == row) return true;
            return false;
        }

        public static string Sha256Hex(string value) => Sha256Hex(Encoding.UTF8.GetBytes(value ?? string.Empty));
        public static string Sha256Hex(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes ?? Array.Empty<byte>());
                StringBuilder sb = new StringBuilder(hash.Length * 2);
                for (int i = 0; i < hash.Length; i++) sb.Append(hash[i].ToString("x2"));
                return sb.ToString();
            }
        }

        private static bool Contains(int[] xs, int want) { if (xs == null) return false; for (int i = 0; i < xs.Length; i++) if (xs[i] == want) return true; return false; }
        private static MapRuntimeValidationResult Fail(string code) => new MapRuntimeValidationResult(false, code);
    }
}
