using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.12 — Builds a ProjectCoverageReport from available runtime data.
    /// AC1: lists all maps + statuses.
    /// AC2: includes region counts (estimated from regionWidth×Height since regions don't store mapId yet).
    /// AC3: includes asset registry missing/invalid counts.
    /// AC4: merges runtime load status.
    /// AC5: GetIssues() on report enables filtering by map/kind/severity.
    /// </summary>
    public static class CoverageReportBuilder
    {
        public static ProjectCoverageReport Build(
            MapManager mapManager,
            IAssetRegistry registry,
            RegionCatalogFile regionCatalog = null)
        {
            var report = new ProjectCoverageReport
            {
                generatedAt = DateTime.UtcNow.ToString("o"),
            };

            if (mapManager == null) return report;

            var allEntries = mapManager.GetAllEntries();
            report.totalMaps = allEntries.Count;

            // Region catalog stats
            int catalogRegionsWithObstacle = regionCatalog?.stats?.withObstacle ?? 0;
            int catalogRegionsWithGround = regionCatalog?.stats?.withGround ?? 0;
            report.regionsWithObstacle = catalogRegionsWithObstacle;
            report.regionsWithGround = catalogRegionsWithGround;
            report.totalRegions = regionCatalog?.totalRegions ?? 0;

            // Registry missing/invalid
            int totalMissing = 0, totalInvalid = 0;
            if (registry != null)
            {
                var all = registry.GetAll();
                foreach (var entry in all)
                {
                    if (entry.status == AssetStatus.Missing) totalMissing++;
                    else if (entry.status == AssetStatus.Invalid) totalInvalid++;
                }
            }
            report.totalAssetsMissing = totalMissing;
            report.totalAssetsInvalid = totalInvalid;

            foreach (var catalogEntry in allEntries)
            {
                var mapEntry = new MapCoverageEntry
                {
                    mapId = catalogEntry.mapId,
                    displayName = catalogEntry.displayNameNormalized,
                    status = catalogEntry.conversionStatus,
                    // AC#2: estimate region counts from catalog dimensions
                    totalRegions = GetEstimatedRegionCount(catalogEntry, mapManager),
                };

                // AC#4: runtime load status
                bool isActive = mapManager.ActiveMapId == catalogEntry.mapId;

                // AC#1/#3: issues from status
                switch (catalogEntry.conversionStatus)
                {
                    case ConversionStatus.Failed:
                        mapEntry.regionsFailed++;
                        mapEntry.issues.Add(new CoverageIssue
                        {
                            mapId = catalogEntry.mapId,
                            kind = ReportKind.Map,
                            severity = ReportSeverity.Error,
                            message = $"Map {catalogEntry.mapId} ({catalogEntry.displayNameNormalized}): source missing (status=Failed)",
                        });
                        break;

                    case ConversionStatus.Partial:
                        mapEntry.regionsMissing++;
                        mapEntry.issues.Add(new CoverageIssue
                        {
                            mapId = catalogEntry.mapId,
                            kind = ReportKind.Map,
                            severity = ReportSeverity.Warning,
                            message = $"Map {catalogEntry.mapId}: incomplete conversion (Partial)",
                        });
                        break;

                    case ConversionStatus.NotStarted:
                        mapEntry.issues.Add(new CoverageIssue
                        {
                            mapId = catalogEntry.mapId,
                            kind = ReportKind.Map,
                            severity = ReportSeverity.Info,
                            message = $"Map {catalogEntry.mapId}: available but not yet converted",
                        });
                        break;
                }

                // AC#3: check registry for missing assets for this map
                if (registry != null)
                {
                    var mapAssets = registry.GetByMapId(catalogEntry.mapId);
                    foreach (var asset in mapAssets)
                    {
                        if (asset.status == AssetStatus.Missing)
                        {
                            mapEntry.assetsMissing++;
                            mapEntry.issues.Add(new CoverageIssue
                            {
                                mapId = catalogEntry.mapId,
                                sourceId = asset.sourceId?.ToKey(),
                                kind = ReportKind.Sprite,
                                severity = ReportSeverity.Warning,
                                message = $"Asset missing: {asset.unityAssetPath}",
                            });
                        }
                        else if (asset.status == AssetStatus.Invalid)
                        {
                            mapEntry.assetsInvalid++;
                            mapEntry.issues.Add(new CoverageIssue
                            {
                                mapId = catalogEntry.mapId,
                                sourceId = asset.sourceId?.ToKey(),
                                kind = ReportKind.Sprite,
                                severity = ReportSeverity.Error,
                                message = $"Asset invalid: {asset.unityAssetPath}",
                            });
                        }
                    }
                }

                if (catalogEntry.conversionStatus != ConversionStatus.Failed)
                    report.mapsAvailable++;
                else
                    report.mapsMissing++;

                report.maps.Add(mapEntry);
            }

            SubsystemLog.Info("Coverage",
                $"Report built: {report.totalMaps} maps, {report.totalAssetsMissing} missing assets, " +
                $"{report.GetIssues(ReportSeverity.Error).Count} errors");

            return report;
        }

        private static int GetEstimatedRegionCount(MapCatalogEntry entry, MapManager manager)
        {
            // Attempt to get from MapDefinition if available
            var def = manager.GetDefinition(entry.mapId);
            if (def != null)
                return def.regionCountX * def.regionCountY;

            // Fall back to catalog rect dimensions
            if (entry.rect != null)
                return (int)(entry.rect.width * entry.rect.height);

            return 0;
        }
    }
}
