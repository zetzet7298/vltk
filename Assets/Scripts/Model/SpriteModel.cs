using System;
using UnityEngine;

namespace VLTK.Model
{
    [Serializable]
    public enum SpriteValidationStatus
    {
        Unknown,
        Valid,
        MissingFrames,
        InvalidPalette,
        InvalidFormat,
        Partial,
    }

    [Serializable]
    public class SpriteClipDefinition
    {
        public SourceAssetId sourceSpriteId;
        public int frameCount;
        public float frameRate = 10f;
        public int directionCount;
        public string actionName;
        public Vector2 pivot;
        public Vector2[] frameOffsets;
        public string atlasRef;
        public string paletteInfo;
        public string alphaMode;
        public string renderStyle;
        public SpriteValidationStatus validationStatus;
    }
}
