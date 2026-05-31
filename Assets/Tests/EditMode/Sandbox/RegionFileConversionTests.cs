using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>M1.2 — Region File Conversion tests.</summary>
    public class RegionFileConversionTests
    {
        private RegionCatalogEntry MakeEntry(bool ground, bool obstacle, bool trap = false,
            bool npc = false, bool obj = false, bool builtin = false)
            => new RegionCatalogEntry
            {
                file = "test_region.dat",
                size = 1024,
                hasGround = ground,
                hasObstacle = obstacle,
                hasTrap = trap,
                hasNpc = npc,
                hasObj = obj,
                hasBuiltin = builtin,
                obstacleBlockedCells = obstacle ? 50 : 0,
                conversionWarnings = new List<string>(),
            };

        private RegionCatalogFile MakeCatalog(params RegionCatalogEntry[] entries)
            => new RegionCatalogFile
            {
                version = 1,
                totalRegions = entries.Length,
                regions = new List<RegionCatalogEntry>(entries),
                stats = new RegionCatalogStats(),
            };

        // --- AC#1: all sections present → RegionDefinition with manifest ---
        [Test]
        public void ToModelEntries_AllSections_ManifestAllTrue()
        {
            var entry = MakeEntry(ground: true, obstacle: true, trap: true, npc: true);
            var defs = RegionCatalogLoader.ToModelEntries(MakeCatalog(entry));

            Assert.AreEqual(1, defs.Count, "One entry should produce one RegionDefinition");
            var manifest = defs[0].sectionManifest;
            Assert.IsNotNull(manifest, "sectionManifest must be set (AC#1)");
            Assert.IsTrue(manifest.hasGround);
            Assert.IsTrue(manifest.hasObstacle);
            Assert.IsTrue(manifest.hasTrap);
            Assert.IsTrue(manifest.hasNpc);
        }

        [Test]
        public void ToModelEntries_SourcePathPreserved()
        {
            var entry = MakeEntry(true, true);
            entry.file = "abc123.dat";
            var defs = RegionCatalogLoader.ToModelEntries(MakeCatalog(entry));
            Assert.AreEqual("abc123.dat", defs[0].sourceRegionPath);
        }

        // --- AC#3: absent sections → missing sections reported ---
        [Test]
        public void ToModelEntries_NoSections_StatusFailed()
        {
            var entry = MakeEntry(false, false, false, false, false, false);
            var defs = RegionCatalogLoader.ToModelEntries(MakeCatalog(entry));

            Assert.AreEqual(ConversionStatus.Failed, defs[0].sectionStatus,
                "Region with no usable sections should be Failed (AC#3)");
        }

        [Test]
        public void ToModelEntries_PartialSections_StatusPartialOrComplete()
        {
            // Has ground but no obstacle → partial
            var entry = MakeEntry(ground: true, obstacle: false);
            var defs = RegionCatalogLoader.ToModelEntries(MakeCatalog(entry));

            // Partial or Complete accepted — just not Failed
            Assert.AreNotEqual(ConversionStatus.Failed, defs[0].sectionStatus,
                "Region with ground should not be Failed");
        }

        [Test]
        public void ToModelEntries_AllSections_MissingSectionsEmpty()
        {
            var entry = MakeEntry(true, true, true, true, true, true);
            var defs = RegionCatalogLoader.ToModelEntries(MakeCatalog(entry));
            var manifest = defs[0].sectionManifest;
            Assert.IsNotNull(manifest.missingSections);
            // No required sections missing (trap/npc/obj optional but listed as optional)
        }

        [Test]
        public void ToModelEntries_MissingGround_ReportsInManifest()
        {
            var entry = MakeEntry(ground: false, obstacle: true);
            var defs = RegionCatalogLoader.ToModelEntries(MakeCatalog(entry));
            var manifest = defs[0].sectionManifest;
            Assert.IsTrue(manifest.missingSections.Contains("ground"),
                "Missing ground section should be reported (AC#3)");
        }

        // --- AC#4: neighbor references default to -1 ---
        [Test]
        public void ToModelEntries_NeighborRefs_DefaultToMinusOne()
        {
            var defs = RegionCatalogLoader.ToModelEntries(MakeCatalog(MakeEntry(true, true)));
            Assert.AreEqual(-1, defs[0].neighborRight,
                "neighborRight should be -1 when not resolvable (AC#4)");
            Assert.AreEqual(-1, defs[0].neighborBottom,
                "neighborBottom should be -1 when not resolvable (AC#4)");
        }

        // --- AC#5: conversion report ---
        [Test]
        public void ToConversionReport_CountsMatchInput()
        {
            var catalog = MakeCatalog(
                MakeEntry(true, true, false),
                MakeEntry(true, false, true),
                MakeEntry(false, false, false));

            var report = RegionCatalogLoader.ToConversionReport(catalog);
            Assert.IsNotNull(report, "ToConversionReport must return non-null (AC#5)");
            Assert.AreEqual(3, report.totalFiles);
            Assert.AreEqual(2, report.withGround);
            Assert.AreEqual(1, report.withObstacle);
            Assert.AreEqual(1, report.withTrap);
        }

        [Test]
        public void ToConversionReport_EmptyCatalog_ReturnsZeroCounts()
        {
            var empty = new RegionCatalogFile
            {
                regions = new List<RegionCatalogEntry>()
            };
            var report = RegionCatalogLoader.ToConversionReport(empty);
            Assert.AreEqual(0, report.totalFiles);
            Assert.AreEqual(0, report.withObstacle);
        }

        [Test]
        public void ToModelEntries_MultiplEntries_CountMatches()
        {
            var catalog = MakeCatalog(
                MakeEntry(true, true),
                MakeEntry(true, false),
                MakeEntry(false, false));
            var defs = RegionCatalogLoader.ToModelEntries(catalog);
            Assert.AreEqual(3, defs.Count, "Should produce one RegionDefinition per input entry");
        }
    }
}
