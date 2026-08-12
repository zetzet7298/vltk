using System;
using UnityEngine;

namespace VLTK.Production.World.Unity
{
    [Serializable]
    public sealed class MapRuntimeCatalog
    {
        public string schema;
        public int mapRuntimeVersion;
        public MapRuntimeCatalogMap[] maps;
        public MapRuntimeCatalogArtifact[] artifacts;
        public MapRuntimeCatalogSecurity security;
    }

    [Serializable] public sealed class MapRuntimeCatalogMap { public int mapId; public string artifact; public string sha256; public string nameVi; }
    [Serializable] public sealed class MapRuntimeCatalogArtifact { public string kind; public string logicalPath; public string sha256; public string mediaType; }
    [Serializable] public sealed class MapRuntimeCatalogSecurity { public bool aliasRemapAllowed; public bool filesystemFallbackAllowed; public bool map79Allowed; public bool productionSignatureVerified; public bool testDataAllowed; }

    [Serializable]
    public sealed class MapRuntimeManifest
    {
        public string schema;
        public int mapId;
        public CanonicalIdentity canonicalIdentity;
        public MapRuntimeBounds bounds;
        public MapRuntimeSpawn spawn;
        public MapRuntimeMovement movement;
        public MapRuntimeWalkability walkability;
        public string sourceProvenanceSha256;
    }

    [Serializable] public sealed class CanonicalIdentity { public int mapId; public string nameVi; public string geometryKey; public string pcMapPath; }
    [Serializable] public sealed class MapRuntimeBounds { public MapRuntimeWorldRect world; }
    [Serializable] public sealed class MapRuntimeWorldRect { public float x; public float y; public float width; public float height; public Rect ToRect() => new Rect(x, y, width, height); }
    [Serializable] public sealed class MapRuntimeSpawn { public MapRuntimeWorldPoint world; public int[] regionCell; public string source; public string sourceStatus; }
    [Serializable] public sealed class MapRuntimeWorldPoint { public float x; public float y; public Vector2 ToVector2() => new Vector2(x, y); }
    [Serializable] public sealed class MapRuntimeMovement { public MapRuntimeMovementRules rules; }
    [Serializable]
    public sealed class MapRuntimeMovementRules
    {
        public bool absoluteRuntimePathsAllowed;
        public bool aliasRemapAllowed;
        public int[] allowMapIds;
        public bool filesystemFallbackAllowed;
        public bool loosePcFolderFallbackAllowed;
        public int[] rejectMapIds;
        public bool requiresWalkableRegionCell;
        public bool testDataAllowed;
    }
    [Serializable] public sealed class MapRuntimeWalkability { public string representation; public int[][] walkableRegionCells; public int[][] blockedRegionCells; }

    [Serializable] public sealed class MapRuntimeSignatureFile { public string schema; public string artifactSha256; public string signature; public string signingKeyId; public MapRuntimeSignatureVerification verification; }
    [Serializable] public sealed class MapRuntimeSignatureVerification { public bool productionSignatureVerified; public string status; public string reason; }

    public static class MapRuntimeContract
    {
        public const int CanonicalMapId = 53;
        public const string CatalogSchema = "map-runtime.catalog.v1";
        public const string ManifestSchema = "map-runtime.v1";
        public const string CatalogPath = "MapRuntime/map-runtime.catalog.v1.json";
        public const string ArtifactPath = "MapRuntime/map-runtime.v1.json";
        public const string ProvenancePath = "MapRuntime/map-runtime.v1.provenance.json";
        public const string SignaturePath = "MapRuntime/map-runtime.v1.signature.json";
        public const string PinnedArtifactSha256 = "27e8553e0e699b60c088ec7c621a38147e96b91643c4501379a8cb61316e1e3c";
        public const string PinnedProvenanceSha256 = "7694d9545a4d02fc0d349223a21596508ec4193b0b4934daa01932ecca6eaabb";
        public const string PinnedSignatureSha256 = "f2922d43ac68905ce6c6c03f890974f49014dda4692f02ac5c96fc55f6cb0c1c";
    }
}
