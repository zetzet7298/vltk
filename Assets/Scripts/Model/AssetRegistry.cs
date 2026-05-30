using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    [Serializable]
    public enum ArtifactType
    {
        Unknown,
        Texture2D,
        Sprite,
        SpriteAtlas,
        MapDefinition,
        RegionDefinition,
        BinaryBlob,
        AudioClip,
        TextAsset,
        Script,
        Prefab,
        Other,
    }

    [Serializable]
    public enum LoadMode
    {
        Unknown,
        EditorDirect,
        Resources,
        AssetBundle,
        Addressables,
        StreamingAssets,
        TestFixture,
    }

    [Serializable]
    public enum AssetStatus
    {
        Available,
        Missing,
        Invalid,
        Pending,
        Deprecated,
    }

    [Serializable]
    public class ConversionManifest
    {
        public int manifestVersion;
        public string sourceRoot;
        public long conversionTimestamp;
        public string toolVersion;
        public List<ConversionEntry> inputs = new();
        public List<ConversionEntry> outputs = new();
        public List<string> warnings = new();
        public List<string> errors = new();
        public CoverageReport coverage = new();

        [Serializable]
        public class ConversionEntry
        {
            public string path;
            public string checksum;
        }

        [Serializable]
        public class CoverageReport
        {
            public int totalAssets;
            public int converted;
            public int missing;
            public int invalid;
            public Dictionary<string, int> byKind = new();
        }
    }

    [Serializable]
    public class AssetRegistryEntry
    {
        public SourceAssetId sourceId;
        public ArtifactType artifactType;
        public string unityAssetPath;
        public string bundleName;
        public LoadMode loadMode;
        public AssetStatus status;
        public string validationHash;
    }
}
