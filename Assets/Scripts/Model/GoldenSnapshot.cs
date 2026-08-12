using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>Serializable RGBA32 visual-regression snapshot contract.</summary>
    [Serializable]
    public class GoldenSnapshot
    {
        public const string SchemaV2 = "vltk.golden-snapshot/v2";
        public const string ComparerV1 = "ssim-bucket-rec709-v1";
        public const string AlphaPremultiplyTransparentBlack = "premultiply_transparent_black";
        public const string ColorRec709LumaBuckets = "rec709_luma_buckets";
        public const string PayloadRgba32U8 = "rgba32_u8";

        public string schema;
        public string comparerVersion;
        public string mapId;
        public string caseId;
        public int width;
        public int height;
        public int gridX;
        public int gridY;
        public string alphaMode;
        public string colorSpace;
        public string unityColorSpace;
        public string payload;

        /// <summary>Row-major 5-bit RGB bucket signature derived from payload.</summary>
        public List<int> signature = new();
        public string contentHash;

        // Required capture provenance. Comparer rejects unset/default values.
        public int skillId = -1;
        public string faction;
        public int frame = -1;
        public long tick = -1;
        public int skillFxLayer = -1;
        public string skillFxLayerName;
        public int nonTransparentPixelCount;
        public long generatedAt;
        public string toolVersion;
        public string goldenUpdateReason;

        public int SignatureLength => signature?.Count ?? 0;
    }
}
