using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using UnityEngine;

namespace VLTK.Tests.Sandbox
{
    /// <summary>M1.1 — Map Metadata Conversion tests.</summary>
    public class MapMetadataTests
    {
        private MapCatalogFile MakeCatalog(MapCatalogFileEntry entry)
        {
            return new MapCatalogFile
            {
                version = 1,
                totalMaps = 1,
                maps = new List<MapCatalogFileEntry> { entry },
            };
        }

        private MapCatalogFileEntry FullEntry(int mapId = 1) => new MapCatalogFileEntry
        {
            mapId = mapId,
            sourceFile = $"map{mapId}.ini",
            haveMap = true,
            isIndoor = false,
            brightness = 200,
            color = "200,210,220,255",
            eventName = "TestMap",
            displayNameRaw = "TestMap",
            displayNameNormalized = "TestMap",
            status = "available",
            rect = "10,20,30,40",
            rectLeft = 10, rectTop = 20, rectRight = 30, rectBottom = 40,
            regionWidth = 21, regionHeight = 21,
            windSpeed = "0.1,0.0,0.0",
            mapLtRegionIndex = 42,
            weatherProfiles = new List<WeatherEntry>
            {
                new WeatherEntry { index = 0, type = 1, odds = 5 },
                new WeatherEntry { index = 1, type = 2, odds = 3 },
            },
            lightProfile = new List<string> { "ambient=0.8", "direction=45" },
            conversionWarnings = new List<string>(),
        };

        // --- AC#1: source rect stored in MapDefinition ---
        [Test]
        public void ToMapDefinitions_SourceBoundsRect_MatchesInputRect()
        {
            var entry = FullEntry();
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            Assert.AreEqual(1, defs.Count);
            var def = defs[0];
            Assert.IsNotNull(def.sourceBoundsRect, "sourceBoundsRect must be set (AC#1)");
            Assert.AreEqual(10f, def.sourceBoundsRect.x);
            Assert.AreEqual(20f, def.sourceBoundsRect.y);
            Assert.AreEqual(20f, def.sourceBoundsRect.width);   // 30-10
            Assert.AreEqual(20f, def.sourceBoundsRect.height);  // 40-20
        }

        // --- AC#2: mapLtRegionIndex preserved ---
        [Test]
        public void ToMapDefinitions_MapLtRegionIndex_Preserved()
        {
            var entry = FullEntry();
            entry.mapLtRegionIndex = 42;
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            Assert.AreEqual(42, defs[0].mapLtRegionIndex, "mapLtRegionIndex must be set (AC#2)");
        }

        // --- AC#3: brightness/color → environment profile ---
        [Test]
        public void ToMapDefinitions_EnvironmentProfile_BrightnessSet()
        {
            var entry = FullEntry();
            entry.brightness = 200;
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            Assert.IsNotNull(defs[0].environmentProfile, "environmentProfile must be set (AC#3)");
            Assert.AreEqual(200f / 255f, defs[0].environmentProfile.brightness, 0.01f);
        }

        // --- AC#4: light profile generated ---
        [Test]
        public void ToMapDefinitions_LightProfile_GeneratedFromSection()
        {
            var entry = FullEntry();
            entry.lightProfile = new List<string> { "ambient=0.8", "direction=45" };
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            var def = defs[0];
            Assert.IsNotNull(def.lightProfile, "lightProfile must be set when [light] section present (AC#4)");
            Assert.AreEqual(2, def.lightProfile.rawEntries.Count);
            StringAssert.Contains("ambient=0.8", def.lightProfile.rawEntries[0]);
        }

        [Test]
        public void ToMapDefinitions_LightProfile_NullWhenNoLightSection()
        {
            var entry = FullEntry();
            entry.lightProfile = null;
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            Assert.IsNull(defs[0].lightProfile, "lightProfile should be null when no [light] data");
        }

        // --- AC#5: weather profile generated ---
        [Test]
        public void ToMapDefinitions_WeatherProfile_GeneratedFromEntries()
        {
            var entry = FullEntry();
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            var def = defs[0];
            Assert.IsNotNull(def.weatherProfile, "weatherProfile must be set (AC#5)");
            Assert.AreEqual(2, def.weatherProfile.entries.Count);
            Assert.AreEqual(1, def.weatherProfile.entries[0].type);
        }

        [Test]
        public void ToMapDefinitions_WeatherProfile_WindSpeedParsed()
        {
            var entry = FullEntry();
            entry.windSpeed = "0.1,0.2,0.3";
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            var wp = defs[0].weatherProfile;
            Assert.IsNotNull(wp);
            Assert.AreEqual(0.1f, wp.windSpeedX, 0.001f);
            Assert.AreEqual(0.2f, wp.windSpeedY, 0.001f);
            Assert.AreEqual(0.3f, wp.windSpeedZ, 0.001f);
        }

        [Test]
        public void ToMapDefinitions_WeatherProfile_NullWhenNoWeather()
        {
            var entry = FullEntry();
            entry.weatherProfiles = null;
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            Assert.IsNull(defs[0].weatherProfile, "weatherProfile should be null when no weather data");
        }

        // --- AC#6: missing fields → conversionWarnings ---
        [Test]
        public void ToMapDefinitions_MissingRect_AddsWarning()
        {
            var entry = FullEntry();
            entry.regionWidth = 0;
            entry.regionHeight = 0;
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            Assert.IsNotNull(defs[0].conversionWarnings);
            Assert.Greater(defs[0].conversionWarnings.Count, 0,
                "Missing rect should generate a warning (AC#6)");
            StringAssert.Contains("rect", defs[0].conversionWarnings[0].ToLower());
        }

        [Test]
        public void ToMapDefinitions_HaveMapFalse_AddsWarning()
        {
            var entry = FullEntry();
            entry.haveMap = false;
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(entry));

            Assert.IsNotNull(defs[0].conversionWarnings);
            Assert.Greater(defs[0].conversionWarnings.Count, 0,
                "Missing map file should generate a warning (AC#6)");
        }

        [Test]
        public void ToMapDefinitions_FullyValid_NoWarnings()
        {
            var defs = MapCatalogLoader.ToMapDefinitions(MakeCatalog(FullEntry()));
            Assert.AreEqual(0, defs[0].conversionWarnings.Count,
                "A fully valid entry should have no warnings");
        }

        [Test]
        public void ToMapDefinitions_EmptyCatalog_ReturnsEmptyList()
        {
            var empty = new MapCatalogFile { maps = new List<MapCatalogFileEntry>() };
            var defs = MapCatalogLoader.ToMapDefinitions(empty);
            Assert.IsNotNull(defs);
            Assert.AreEqual(0, defs.Count);
        }
    }
}
