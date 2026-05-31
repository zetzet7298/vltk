using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Model
{
    [Serializable]
    public enum ConversionStatus
    {
        NotStarted,
        InProgress,
        Complete,
        Partial,
        Failed,
    }

    [Serializable]
    public class RectDef
    {
        public float x;
        public float y;
        public float width;
        public float height;
    }

    [Serializable]
    public class MapCatalogEntry
    {
        public int mapId;
        public string displayNameRaw;
        public string displayNameNormalized;
        public string sourceMapPath;
        public SourceAssetId settingSourceId;
        public string worldSetMembership;
        public RectDef rect;
        public int mapLeftTopRegionIndex;
        public bool isIndoor;
        public float defaultBrightness;
        public Color defaultColor;
        public string weatherProfileId;
        public string lightProfileId;
        public ConversionStatus conversionStatus;
    }

    [Serializable]
    public enum MinimapArtifactStatus
    {
        Unknown,
        Registered,
        Missing,
    }

    /// <summary>
    /// M1.8 — Reference to a map's minimap/world-map overview artifact.
    /// Tracks the source id it was derived from and whether the artifact is
    /// registered (resolvable through the asset registry) or missing.
    /// </summary>
    [Serializable]
    public class MinimapRef
    {
        public SourceAssetId sourceId;
        public string artifactPath;
        public int sourceWidth;
        public int sourceHeight;
        public MinimapArtifactStatus status = MinimapArtifactStatus.Unknown;

        public bool IsRegistered => status == MinimapArtifactStatus.Registered;
        public bool IsMissing => status == MinimapArtifactStatus.Missing;
    }

    [Serializable]
    public class EnvironmentProfile
    {
        public float brightness;
        public Color tint;
        public string weatherId;
        public string lightId;
    }

    [Serializable]
    public class MapDefinition
    {
        public MapCatalogEntry catalogEntry;
        public int regionCountX;
        public int regionCountY;
        public int regionWidthPixels;
        public int regionHeightPixels;
        public int cellWidth;
        public int cellHeight;
        public EnvironmentProfile environmentProfile;
        public ConversionStatus conversionStatus;
        public string conversionReportRef;

        // M1.1 AC#1: source rect from PC map settings
        public RectDef sourceBoundsRect;

        // M1.1 AC#2: top-left region index anchor
        public int mapLtRegionIndex;

        // M1.1 AC#4: light profile (time-of-day data)
        public LightProfile lightProfile;

        // M1.1 AC#5: weather profile
        public WeatherProfile weatherProfile;

        // M1.1 AC#6: warnings from conversion (missing fields, etc.)
        public List<string> conversionWarnings = new();

        // M1.8 AC#1: minimap / world-map overview artifact reference (spec 4.5)
        public MinimapRef minimapRef;
    }
}
