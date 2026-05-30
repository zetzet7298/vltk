using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    // AC4: Top-level conversion report parsed from JSON conversionReport section.
    [Serializable]
    public class ConversionReportJson
    {
        public int totalDiscovered;
        public int available;
        public int missing;
        public int incomplete;
        public int unnamed;
        public string generatedAt;
        public string toolVersion;
    }

    [Serializable]
    public class MapCatalogFile
    {
        public int version;
        public int totalMaps;
        public int outdoor;
        public int indoor;
        public int totalRegions;
        // AC4: discovery report embedded in catalog JSON
        public ConversionReportJson conversionReport;
        public List<MapCatalogFileEntry> maps;
    }

    [Serializable]
    public class MapCatalogFileEntry
    {
        public int mapId;
        public string sourceFile;
        public bool haveMap;
        public bool isIndoor;
        public int brightness;
        public string color;
        public string eventName;
        // AC2: raw (original GBK-decoded) and normalized (ASCII-safe) display names
        public string displayNameRaw;
        public string displayNameNormalized;
        // AC3: status field — "available" | "missing" | "incomplete"
        public string status;
        public string rect;
        public int rectLeft, rectTop, rectRight, rectBottom;
        public int regionWidth, regionHeight;
        public string windSpeed;
        public int version;
        // M1.1 AC#2: top-left region index
        public int mapLtRegionIndex;
        // M1.1 AC#4: light section raw key=value entries
        public List<string> lightProfile;
        // M1.1 AC#5: weather profiles
        public List<WeatherEntry> weatherProfiles;
        // M1.1 AC#6: conversion warnings
        public List<string> conversionWarnings;
    }


    public static class MapCatalogLoader
    {
        private const string CATALOG_PATH = "MapCatalog.json";

        public static MapCatalogFile LoadFromStreamingAssets()
        {
            var path = Path.Combine(Application.streamingAssetsPath, CATALOG_PATH);
            if (!File.Exists(path))
            {
                SubsystemLog.Warn("MapCatalog", $"No catalog at {path}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                var catalog = JsonUtility.FromJson<MapCatalogFile>(json);
                if (catalog == null)
                {
                    SubsystemLog.Error("MapCatalog", "Failed to parse JSON");
                    return null;
                }

                SubsystemLog.Info("MapCatalog",
                    $"Loaded {catalog.totalMaps} maps ({catalog.outdoor} outdoor, {catalog.indoor} indoor, " +
                    $"{catalog.totalRegions} regions)");
                return catalog;
            }
            catch (Exception ex)
            {
                SubsystemLog.Error("MapCatalog", $"Load error: {ex.Message}");
                return null;
            }
        }

        public static List<MapCatalogEntry> ToModelEntries(MapCatalogFile catalog)
        {
            var result = new List<MapCatalogEntry>();
            if (catalog?.maps == null) return result;

            foreach (var f in catalog.maps)
            {
                Color defaultColor = Color.white;

                // Parse color from "r,g,b,a" string
                if (!string.IsNullOrEmpty(f.color))
                {
                    var parts = f.color.Split(',');
                    if (parts.Length >= 3)
                    {
                        float r = 0, g = 0, b = 0;
                        float.TryParse(parts[0], out r);
                        float.TryParse(parts[1], out g);
                        float.TryParse(parts[2], out b);
                        defaultColor = new Color(r / 255f, g / 255f, b / 255f);
                    }
                }

                // AC3: map JSON status → ConversionStatus
                var convStatus = StatusToConversion(f.status);

                // AC2: prefer displayNameNormalized from JSON; fall back to eventName logic
                string normalizedName;
                if (!string.IsNullOrEmpty(f.displayNameNormalized))
                {
                    normalizedName = f.displayNameNormalized;
                }
                else
                {
                    normalizedName = string.IsNullOrEmpty(f.eventName) || f.eventName == "null"
                        ? $"Map_{f.mapId}"
                        : f.eventName;
                }

                // AC2: raw name — prefer displayNameRaw, fall back to eventName
                string rawName = !string.IsNullOrEmpty(f.displayNameRaw)
                    ? f.displayNameRaw
                    : f.eventName;

                var entry = new MapCatalogEntry
                {
                    mapId = f.mapId,
                    displayNameRaw = rawName,
                    displayNameNormalized = normalizedName,
                    sourceMapPath = f.sourceFile,
                    isIndoor = f.isIndoor,
                    defaultBrightness = f.brightness / 255f,
                    defaultColor = defaultColor,
                    rect = new RectDef
                    {
                        x = f.rectLeft,
                        y = f.rectTop,
                        width = f.regionWidth,
                        height = f.regionHeight,
                    },
                    mapLeftTopRegionIndex = f.rectLeft,
                    conversionStatus = convStatus,
                };

                result.Add(entry);
            }

            return result;
        }

        /// <summary>
        /// Convert discovery status string from JSON to ConversionStatus model value.
        /// AC3 mapping:
        ///   "available"  → NotStarted  (file exists; not yet converted to Unity assets)
        ///   "missing"    → Failed      (haveMap=false; no usable source data)
        ///   "incomplete" → Partial     (has ini but missing rect/region data)
        ///   default      → NotStarted
        /// </summary>
        public static ConversionStatus StatusToConversion(string status)
        {
            return status switch
            {
                "available"  => ConversionStatus.NotStarted,
                "missing"    => ConversionStatus.Failed,
                "incomplete" => ConversionStatus.Partial,
                _            => ConversionStatus.NotStarted,
            };
        }

        /// <summary>
        /// Build a MapDiscoveryReport model from the embedded JSON conversion report.
        /// Returns null if the catalog or conversionReport section is absent.
        /// </summary>
        public static MapDiscoveryReport ToDiscoveryReport(MapCatalogFile catalog)
        {
            if (catalog?.conversionReport == null) return null;
            var r = catalog.conversionReport;
            return new MapDiscoveryReport
            {
                totalDiscovered = r.totalDiscovered,
                available       = r.available,
                missing         = r.missing,
                incomplete      = r.incomplete,
                unnamed         = r.unnamed,
                generatedAt     = r.generatedAt,
                toolVersion     = r.toolVersion,
            };
        }

        /// <summary>
        /// M1.1 — Build full MapDefinition objects from catalog entries.
        /// Includes weather profile, light profile, source rect, and conversion warnings.
        /// </summary>
        public static List<MapDefinition> ToMapDefinitions(MapCatalogFile catalog)
        {
            var result = new List<MapDefinition>();
            if (catalog?.maps == null) return result;

            var entries = ToModelEntries(catalog);
            var entryMap = new Dictionary<int, MapCatalogEntry>();
            foreach (var e in entries) entryMap[e.mapId] = e;

            foreach (var f in catalog.maps)
            {
                if (!entryMap.TryGetValue(f.mapId, out var catalogEntry))
                    continue;

                var def = new MapDefinition
                {
                    catalogEntry = catalogEntry,
                    regionCountX = f.regionWidth,
                    regionCountY = f.regionHeight,
                    conversionStatus = catalogEntry.conversionStatus,

                    // M1.1 AC#1: source rect from PC settings
                    sourceBoundsRect = new RectDef
                    {
                        x = f.rectLeft,
                        y = f.rectTop,
                        width = f.rectRight - f.rectLeft,
                        height = f.rectBottom - f.rectTop,
                    },

                    // M1.1 AC#2: top-left region anchor
                    mapLtRegionIndex = f.mapLtRegionIndex,

                    // M1.1 AC#3: environment profile with brightness + color
                    environmentProfile = new EnvironmentProfile
                    {
                        brightness = f.brightness / 255f,
                        tint = catalogEntry.defaultColor,
                    },

                    // M1.1 AC#6: conversion warnings
                    conversionWarnings = f.conversionWarnings ?? new List<string>(),
                };

                // M1.1 AC#5: weather profile
                if (f.weatherProfiles != null && f.weatherProfiles.Count > 0)
                {
                    def.weatherProfile = new WeatherProfile
                    {
                        profileId = $"map_{f.mapId}_weather",
                        entries = f.weatherProfiles,
                    };

                    // Parse windSpeed "x,y,z"
                    if (!string.IsNullOrEmpty(f.windSpeed))
                    {
                        var parts = f.windSpeed.Split(',');
                        if (parts.Length >= 3)
                        {
                            float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out def.weatherProfile.windSpeedX);
                            float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out def.weatherProfile.windSpeedY);
                            float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out def.weatherProfile.windSpeedZ);
                        }
                    }
                }

                // M1.1 AC#4: light profile
                if (f.lightProfile != null && f.lightProfile.Count > 0)
                {
                    def.lightProfile = new LightProfile
                    {
                        profileId = $"map_{f.mapId}_light",
                        rawEntries = f.lightProfile,
                    };
                }

                // M1.1 AC#6: validate required fields, add warnings for missing
                if (f.regionWidth == 0 || f.regionHeight == 0)
                    def.conversionWarnings.Add("Missing or invalid rect: regionWidth/Height is 0");
                if (f.brightness == 0 && string.IsNullOrEmpty(f.color))
                    def.conversionWarnings.Add("Missing environment: brightness and color both absent");
                if (!f.haveMap)
                    def.conversionWarnings.Add("Source map file not found (haveMap=false)");

                result.Add(def);
            }

            return result;
        }
    }
}
