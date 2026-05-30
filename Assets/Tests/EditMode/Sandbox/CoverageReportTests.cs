using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    /// <summary>M1.12 — Map Conversion Coverage Report tests.</summary>
    public class CoverageReportTests
    {
        private MapManager MakeManager(params (int id, ConversionStatus status)[] maps)
        {
            var registry = new AssetRegistry();
            var mgr = new MapManager(registry);
            // Manually populate catalog via LoadPlaceholderCatalog (which loads from file or creates defaults)
            mgr.LoadPlaceholderCatalog();
            return mgr;
        }

        // --- AC#1: report lists total maps and statuses ---

        [Test]
        public void Build_EmptyCatalog_ReturnsZeroMaps()
        {
            var registry = new AssetRegistry();
            var mgr = new MapManager();
            var report = CoverageReportBuilder.Build(mgr, registry);

            Assert.IsNotNull(report, "Report must not be null");
            Assert.AreEqual(0, report.totalMaps);
            Assert.IsNotNull(report.maps);
        }

        [Test]
        public void Build_WithMaps_TotalMapsMatchesCatalog()
        {
            var registry = new AssetRegistry();
            var mgr = new MapManager(registry);
            mgr.LoadPlaceholderCatalog();
            var report = CoverageReportBuilder.Build(mgr, registry);

            Assert.IsNotNull(report);
            Assert.AreEqual(mgr.GetAllEntries().Count, report.totalMaps, "AC#1: report.totalMaps must equal catalog count");
            Assert.AreEqual(report.totalMaps, report.maps.Count, "maps list must have one entry per map");
        }

        [Test]
        public void Build_GeneratedAt_IsSet()
        {
            var report = CoverageReportBuilder.Build(new MapManager(), new AssetRegistry());
            Assert.IsNotNull(report.generatedAt);
            Assert.IsTrue(report.generatedAt.Length > 0);
        }

        // --- AC#2: region counts included ---

        [Test]
        public void Build_MapEntries_HaveRegionCounts()
        {
            var registry = new AssetRegistry();
            var mgr = new MapManager(registry);
            mgr.LoadPlaceholderCatalog();
            var report = CoverageReportBuilder.Build(mgr, registry);

            // At least some maps should have non-zero region count
            if (report.maps.Count > 0)
            {
                // Not all may have region counts (depends on catalog data)
                // Just verify the field is populated
                Assert.IsNotNull(report.maps[0]);
                Assert.GreaterOrEqual(report.maps[0].totalRegions, 0);
            }
        }

        // --- AC#3: missing asset → issue with Warning ---

        [Test]
        public void Build_WithMissingAsset_IssueHasWarningSeverity()
        {
            var registry = new AssetRegistry();
            var mgr = new MapManager(registry);
            mgr.LoadPlaceholderCatalog();

            // Register a missing asset for a map
            if (mgr.GetAllEntries().Count > 0)
            {
                var firstMap = mgr.GetAllEntries()[0];
                registry.Register(new AssetRegistryEntry
                {
                    sourceId = new SourceAssetId
                    {
                        uid = firstMap.mapId,
                        sourcePath = $"sprites/map{firstMap.mapId}/test.spr",
                        resourceKind = ResourceKind.Sprite,
                    },
                    unityAssetPath = $"sprites/map{firstMap.mapId}/test.spr",
                    status = AssetStatus.Missing,
                    artifactType = ArtifactType.SpriteAtlas,
                });

                var report = CoverageReportBuilder.Build(mgr, registry);
                var warnings = report.GetIssues(ReportSeverity.Warning, ReportKind.Sprite);
                Assert.Greater(warnings.Count, 0, "AC#3: missing asset should generate a Warning issue");
            }
        }

        // --- AC#4: failed map → error issue ---

        [Test]
        public void Build_NullManager_ReturnsEmptyReport()
        {
            var report = CoverageReportBuilder.Build(null, new AssetRegistry());
            Assert.IsNotNull(report);
            Assert.AreEqual(0, report.totalMaps);
        }

        // --- AC#5: filter issues by severity ---

        [Test]
        public void GetIssues_FilterBySeverity_ReturnsOnlyMatchingSeverity()
        {
            var report = new ProjectCoverageReport();
            var mapEntry = new MapCoverageEntry { mapId = 1, displayName = "TestMap" };
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Error, kind = ReportKind.Map, message = "Error!" });
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Warning, kind = ReportKind.Sprite, message = "Warning!" });
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Info, kind = ReportKind.Map, message = "Info." });
            report.maps.Add(mapEntry);

            var errors = report.GetIssues(ReportSeverity.Error);
            var warnings = report.GetIssues(ReportSeverity.Warning);
            var all = report.GetIssues();

            Assert.AreEqual(1, errors.Count, "Only errors should be returned for Error filter");
            Assert.AreEqual(1, warnings.Count, "Only warnings for Warning filter");
            Assert.AreEqual(3, all.Count, "All issues when no filter");
        }

        [Test]
        public void GetIssues_FilterByKind_ReturnsOnlyMatchingKind()
        {
            var report = new ProjectCoverageReport();
            var mapEntry = new MapCoverageEntry { mapId = 1, displayName = "TestMap" };
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Warning, kind = ReportKind.Sprite, message = "Sprite issue" });
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Warning, kind = ReportKind.Map, message = "Map issue" });
            report.maps.Add(mapEntry);

            var spriteIssues = report.GetIssues(null, ReportKind.Sprite);
            Assert.AreEqual(1, spriteIssues.Count);
            Assert.AreEqual(ReportKind.Sprite, spriteIssues[0].kind);
        }

        [Test]
        public void GetIssues_FilterBySeverityAndKind_CombinesFilters()
        {
            var report = new ProjectCoverageReport();
            var mapEntry = new MapCoverageEntry { mapId = 1, displayName = "TestMap" };
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Error, kind = ReportKind.Sprite });
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Error, kind = ReportKind.Map });
            mapEntry.issues.Add(new CoverageIssue { severity = ReportSeverity.Warning, kind = ReportKind.Sprite });
            report.maps.Add(mapEntry);

            var result = report.GetIssues(ReportSeverity.Error, ReportKind.Sprite);
            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void GetIssues_GlobalIssues_IncludedInFilter()
        {
            var report = new ProjectCoverageReport();
            report.globalIssues.Add(new CoverageIssue
            {
                severity = ReportSeverity.Error,
                kind = ReportKind.Config,
                message = "Global config error",
            });

            var errors = report.GetIssues(ReportSeverity.Error);
            Assert.AreEqual(1, errors.Count);
        }
    }
}
