using NUnit.Framework;
using System.Collections.Generic;
using VLTK.Model;
using VLTK.Sandbox;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// EditMode tests for M0.9 — Map Catalog Discovery.
    /// Verifies all four acceptance criteria are satisfied by the loader and model.
    /// </summary>
    public class MapCatalogDiscoveryTests
    {
        // -----------------------------------------------------------------------
        // Helper: build a minimal MapCatalogFile with a single entry
        // -----------------------------------------------------------------------
        private static MapCatalogFile MakeCatalog(
            string status,
            string displayNameRaw = "",
            string displayNameNormalized = "",
            string eventName = "",
            int mapId = 1,
            bool haveMap = true,
            int regionWidth = 10,
            int regionHeight = 10)
        {
            return new MapCatalogFile
            {
                version = 1,
                totalMaps = 1,
                outdoor = 1,
                indoor = 0,
                totalRegions = regionWidth * regionHeight,
                conversionReport = new ConversionReportJson
                {
                    totalDiscovered = 5,
                    available = 2,
                    missing = 2,
                    incomplete = 1,
                    unnamed = 3,
                    generatedAt = "2026-05-30T00:00:00Z",
                    toolVersion = "1.1.0",
                },
                maps = new List<MapCatalogFileEntry>
                {
                    new MapCatalogFileEntry
                    {
                        mapId = mapId,
                        sourceFile = $"test_{mapId:x4}.ini",
                        haveMap = haveMap,
                        isIndoor = false,
                        brightness = 255,
                        color = "255,255,255,255",
                        eventName = eventName,
                        displayNameRaw = displayNameRaw,
                        displayNameNormalized = displayNameNormalized,
                        status = status,
                        rectLeft = 0, rectTop = 0,
                        rectRight = regionWidth - 1, rectBottom = regionHeight - 1,
                        regionWidth = regionWidth,
                        regionHeight = regionHeight,
                    }
                },
            };
        }

        // -----------------------------------------------------------------------
        // AC3: status "available" → ConversionStatus.NotStarted
        // -----------------------------------------------------------------------
        [Test]
        public void StatusAvailable_MapsTo_ConversionStatusNotStarted()
        {
            // AC3: available = haveMap=True with valid rect; not yet converted to Unity
            var catalog = MakeCatalog(status: "available", displayNameNormalized: "TestMap", mapId: 1);
            var entries = MapCatalogLoader.ToModelEntries(catalog);

            Assert.AreEqual(1, entries.Count);
            // AC3 assertion: available source → NotStarted (not yet converted to Unity assets)
            Assert.AreEqual(ConversionStatus.NotStarted, entries[0].conversionStatus,
                "AC3: 'available' status must map to ConversionStatus.NotStarted (exists in PC data, not yet converted)");
        }

        // -----------------------------------------------------------------------
        // AC3: status "missing" → ConversionStatus.Failed
        // -----------------------------------------------------------------------
        [Test]
        public void StatusMissing_MapsTo_ConversionStatusFailed()
        {
            // AC3: missing = haveMap=False; no usable map source data
            var catalog = MakeCatalog(status: "missing", haveMap: false, displayNameNormalized: "Map_2", mapId: 2);
            var entries = MapCatalogLoader.ToModelEntries(catalog);

            Assert.AreEqual(1, entries.Count);
            // AC3 assertion: missing status → Failed (cannot be converted, no source)
            Assert.AreEqual(ConversionStatus.Failed, entries[0].conversionStatus,
                "AC3: 'missing' status must map to ConversionStatus.Failed (no PC source data)");
        }

        // -----------------------------------------------------------------------
        // AC3: status "incomplete" → ConversionStatus.Partial
        // -----------------------------------------------------------------------
        [Test]
        public void StatusIncomplete_MapsTo_ConversionStatusPartial()
        {
            // AC3: incomplete = haveMap=True but rect/region data absent
            var catalog = MakeCatalog(
                status: "incomplete",
                haveMap: true,
                regionWidth: 0,
                regionHeight: 0,
                displayNameNormalized: "Map_3",
                mapId: 3);
            var entries = MapCatalogLoader.ToModelEntries(catalog);

            Assert.AreEqual(1, entries.Count);
            // AC3 assertion: incomplete status → Partial (partial data available)
            Assert.AreEqual(ConversionStatus.Partial, entries[0].conversionStatus,
                "AC3: 'incomplete' status must map to ConversionStatus.Partial");
        }

        // -----------------------------------------------------------------------
        // AC2: displayNameNormalized from JSON is used when present
        // -----------------------------------------------------------------------
        [Test]
        public void DisplayNameNormalized_UsedWhenProvided()
        {
            // AC2: the JSON has a ready-made normalized name (ASCII-safe)
            var catalog = MakeCatalog(
                status: "available",
                displayNameRaw: "中原北区",
                displayNameNormalized: "ZhongYuan_North",
                eventName: "中原北区",
                mapId: 1);
            var entries = MapCatalogLoader.ToModelEntries(catalog);

            Assert.AreEqual(1, entries.Count);
            // AC2 assertion: normalized name comes from JSON field, not re-computed
            Assert.AreEqual("ZhongYuan_North", entries[0].displayNameNormalized,
                "AC2: displayNameNormalized from JSON must be used as-is");
            // AC2 assertion: raw name is preserved separately
            Assert.AreEqual("中原北区", entries[0].displayNameRaw,
                "AC2: displayNameRaw must preserve original encoding from JSON");
        }

        // -----------------------------------------------------------------------
        // AC2: displayNameNormalized falls back to Map_{id} when JSON field empty
        // -----------------------------------------------------------------------
        [Test]
        public void DisplayNameNormalized_FallsBackToMapId_WhenEmpty()
        {
            // AC2: JSON has no normalized name (empty string) — loader must fall back
            var catalog = MakeCatalog(
                status: "available",
                displayNameRaw: "",
                displayNameNormalized: "",
                eventName: "",
                mapId: 7);
            var entries = MapCatalogLoader.ToModelEntries(catalog);

            Assert.AreEqual(1, entries.Count);
            // AC2 assertion: fallback is "Map_{id}" when normalized name is absent
            Assert.AreEqual("Map_7", entries[0].displayNameNormalized,
                "AC2: displayNameNormalized must fall back to 'Map_{mapId}' when JSON field is empty");
        }

        // -----------------------------------------------------------------------
        // AC4: ToDiscoveryReport produces correct counts from conversionReport JSON
        // -----------------------------------------------------------------------
        [Test]
        public void ToDiscoveryReport_ReturnsCorrectCounts()
        {
            // AC4: conversionReport section present → MapDiscoveryReport populated
            var catalog = MakeCatalog(status: "available", displayNameNormalized: "X", mapId: 1);
            var report = MapCatalogLoader.ToDiscoveryReport(catalog);

            Assert.IsNotNull(report, "AC4: ToDiscoveryReport must return non-null when conversionReport is present");
            Assert.AreEqual(5, report.totalDiscovered, "AC4: totalDiscovered matches JSON");
            Assert.AreEqual(2, report.available,       "AC4: available count matches JSON");
            Assert.AreEqual(2, report.missing,         "AC4: missing count matches JSON");
            Assert.AreEqual(1, report.incomplete,      "AC4: incomplete count matches JSON");
            Assert.AreEqual(3, report.unnamed,         "AC4: unnamed count matches JSON");
            Assert.AreEqual("1.1.0", report.toolVersion, "AC4: toolVersion matches JSON");
        }

        // -----------------------------------------------------------------------
        // AC4: ToDiscoveryReport returns null when no conversionReport section
        // -----------------------------------------------------------------------
        [Test]
        public void ToDiscoveryReport_ReturnsNull_WhenReportMissing()
        {
            // AC4: catalog without conversionReport → null (no crash)
            var catalog = new MapCatalogFile
            {
                version = 1,
                totalMaps = 0,
                maps = new List<MapCatalogFileEntry>(),
                conversionReport = null,
            };
            var report = MapCatalogLoader.ToDiscoveryReport(catalog);
            Assert.IsNull(report, "AC4: ToDiscoveryReport must return null when conversionReport is absent");
        }

        // -----------------------------------------------------------------------
        // AC1: all discovered entries (including missing/incomplete) appear in list
        // -----------------------------------------------------------------------
        [Test]
        public void ToModelEntries_IncludesAllStatuses()
        {
            // AC1: catalog with mixed statuses — all entries must be returned
            var catalog = new MapCatalogFile
            {
                version = 1,
                totalMaps = 2,
                conversionReport = new ConversionReportJson { totalDiscovered = 3 },
                maps = new List<MapCatalogFileEntry>
                {
                    new MapCatalogFileEntry
                    {
                        mapId = 1, status = "available", haveMap = true,
                        displayNameNormalized = "RegionA",
                        brightness = 255, color = "255,255,255,255",
                        regionWidth = 10, regionHeight = 10,
                    },
                    new MapCatalogFileEntry
                    {
                        mapId = 0, status = "missing", haveMap = false,
                        displayNameNormalized = "Map_0",
                        brightness = 255, color = "255,255,255,255",
                    },
                    new MapCatalogFileEntry
                    {
                        mapId = 0, status = "incomplete", haveMap = true,
                        displayNameNormalized = "Map_0",
                        brightness = 255, color = "255,255,255,255",
                    },
                },
            };

            var entries = MapCatalogLoader.ToModelEntries(catalog);

            // AC1 assertion: all 3 entries returned, not just haveMap=true ones
            Assert.AreEqual(3, entries.Count,
                "AC1: ToModelEntries must include ALL discovered entries (available + missing + incomplete)");

            // Verify statuses present
            var statuses = new HashSet<ConversionStatus>();
            foreach (var e in entries) statuses.Add(e.conversionStatus);
            Assert.IsTrue(statuses.Contains(ConversionStatus.NotStarted), "AC1/AC3: at least one available entry");
            Assert.IsTrue(statuses.Contains(ConversionStatus.Failed),     "AC1/AC3: at least one missing entry");
            Assert.IsTrue(statuses.Contains(ConversionStatus.Partial),    "AC1/AC3: at least one incomplete entry");
        }
    }
}
