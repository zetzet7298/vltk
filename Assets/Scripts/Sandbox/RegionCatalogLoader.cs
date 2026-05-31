using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [Serializable]
    public class RegionCatalogStats
    {
        public int withObstacle;
        public int withTrap;
        public int withNpc;
        public int withObj;
        public int withGround;
        public int withBuiltin;
    }

    [Serializable]
    public class RegionConversionReport
    {
        public int totalFiles;
        public int validRegions;
        public int invalidRegions;
        public int withObstacle;
        public int withTrap;
        public int withNpc;
        public int withObj;
        public int withGround;
        public int withBuiltin;
        public string generatedAt;
    }

    [Serializable]
    public class RegionCatalogFile
    {
        public int version;
        public int totalRegions;
        public RegionCatalogStats stats;
        public RegionConversionReport conversionReport;
        public List<RegionCatalogEntry> regions;
    }

    [Serializable]
    public class RegionCatalogEntry
    {
        public string file;
        public int size;
        public bool hasObstacle;
        public bool hasTrap;
        public bool hasNpc;
        public bool hasObj;
        public bool hasGround;
        public bool hasBuiltin;
        public int obstacleBlockedCells;
        public List<string> conversionWarnings;
    }

    /// <summary>
    /// M1.2 — Loads and converts the region catalog from StreamingAssets.
    /// AC1-5: maps RegionCatalogEntry → RegionDefinition with section manifest,
    /// status, and neighbor placeholders.
    /// </summary>
    public static class RegionCatalogLoader
    {
        private const string CATALOG_PATH = "RegionCatalog.json";

        public static RegionCatalogFile LoadFromStreamingAssets()
        {
            var path = Path.Combine(Application.streamingAssetsPath, CATALOG_PATH);
            if (!File.Exists(path))
            {
                SubsystemLog.Warn("RegionCatalog", $"No catalog at {path}");
                return null;
            }

            try
            {
                var json = File.ReadAllText(path);
                var catalog = JsonUtility.FromJson<RegionCatalogFile>(json);
                if (catalog == null)
                {
                    SubsystemLog.Error("RegionCatalog", "Failed to parse RegionCatalog.json");
                    return null;
                }

                SubsystemLog.Info("RegionCatalog",
                    $"Loaded {catalog.totalRegions} regions ({catalog.stats?.withObstacle ?? 0} with obstacle)");
                return catalog;
            }
            catch (Exception ex)
            {
                SubsystemLog.Error("RegionCatalog", $"Load error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// M1.2 AC#1-4 — Convert each RegionCatalogEntry into a canonical RegionDefinition.
        /// </summary>
        public static List<RegionDefinition> ToModelEntries(RegionCatalogFile catalog)
        {
            var result = new List<RegionDefinition>();
            if (catalog?.regions == null) return result;

            foreach (var r in catalog.regions)
            {
                var manifest = new RegionSectionManifest
                {
                    hasObstacle = r.hasObstacle,
                    hasTrap = r.hasTrap,
                    hasNpc = r.hasNpc,
                    hasObj = r.hasObj,
                    hasGround = r.hasGround,
                    hasBuiltin = r.hasBuiltin,
                    missingSections = new List<string>(),
                    warnings = r.conversionWarnings ?? new List<string>(),
                };

                // AC#3: detect missing optional/required sections
                if (!r.hasGround) manifest.missingSections.Add("ground");
                if (!r.hasObstacle) manifest.missingSections.Add("obstacle");
                if (!r.hasTrap) manifest.missingSections.Add("trap (optional)");
                if (!r.hasNpc) manifest.missingSections.Add("npc (optional)");

                var status = DetermineStatus(r, manifest);

                var def = new RegionDefinition
                {
                    sourceRegionPath = r.file,
                    sectionManifest = manifest,
                    sectionStatus = status,
                    // AC#4: neighbor references — not yet derivable from DAT format
                    neighborRight = -1,
                    neighborBottom = -1,
                };

                result.Add(def);
            }

            return result;
        }

        /// <summary>
        /// M1.2 AC#5 — Build conversion report from catalog.
        /// </summary>
        public static RegionConversionReport ToConversionReport(RegionCatalogFile catalog)
        {
            if (catalog?.conversionReport != null)
                return catalog.conversionReport;

            // Fallback: compute from regions list
            if (catalog?.regions == null) return new RegionConversionReport();

            int obstacle = 0, trap = 0, npc = 0, obj = 0, ground = 0, builtin = 0;
            foreach (var r in catalog.regions)
            {
                if (r.hasObstacle) obstacle++;
                if (r.hasTrap) trap++;
                if (r.hasNpc) npc++;
                if (r.hasObj) obj++;
                if (r.hasGround) ground++;
                if (r.hasBuiltin) builtin++;
            }

            return new RegionConversionReport
            {
                totalFiles = catalog.regions.Count,
                validRegions = catalog.regions.Count,
                withObstacle = obstacle,
                withTrap = trap,
                withNpc = npc,
                withObj = obj,
                withGround = ground,
                withBuiltin = builtin,
            };
        }

        private static ConversionStatus DetermineStatus(RegionCatalogEntry r, RegionSectionManifest manifest)
        {
            // AC#3: completely empty → Failed
            if (!r.hasObstacle && !r.hasGround && !r.hasBuiltin && !r.hasTrap && !r.hasNpc && !r.hasObj)
                return ConversionStatus.Failed;

            // Has ground or obstacle → useful, but may be partial if some sections missing
            if (r.hasGround || r.hasObstacle)
                return manifest.missingSections.Count > 2
                    ? ConversionStatus.Partial
                    : ConversionStatus.Complete;

            return ConversionStatus.Partial;
        }
    }
}
