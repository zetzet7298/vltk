using NUnit.Framework;
using VLTK.Model;
using UnityEngine;

namespace VLTK.Tests.Model
{
    public class MapModelTests
    {
        [Test]
        public void MapCatalogEntry_Serializable_DefaultValues()
        {
            var entry = new MapCatalogEntry();
            Assert.AreEqual(0, entry.mapId);
            Assert.IsNull(entry.displayNameRaw);
            Assert.AreEqual(ConversionStatus.NotStarted, entry.conversionStatus);
        }

        [Test]
        public void MapCatalogEntry_RoundTrip_ViaJsonUtility()
        {
            var entry = new MapCatalogEntry
            {
                mapId = 42,
                displayNameRaw = "Trung Nguyen",
                displayNameNormalized = "Trung Nguyen",
                sourceMapPath = "maps/971b75ae.dat",
                isIndoor = false,
                defaultBrightness = 0.8f,
                defaultColor = Color.white,
                conversionStatus = ConversionStatus.Complete,
                rect = new RectDef { x = 0, y = 0, width = 73, height = 51 },
            };

            var json = JsonUtility.ToJson(entry);
            Assert.IsNotNull(json);
            Assert.IsTrue(json.Length > 0);

            var restored = JsonUtility.FromJson<MapCatalogEntry>(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(42, restored.mapId);
            Assert.AreEqual("Trung Nguyen", restored.displayNameNormalized);
            Assert.AreEqual(ConversionStatus.Complete, restored.conversionStatus);
            Assert.AreEqual(73, restored.rect.width);
        }

        [Test]
        public void MapDefinition_Serializable_DefaultValues()
        {
            var def = new MapDefinition();
            Assert.IsNull(def.catalogEntry);
            Assert.AreEqual(0, def.regionCountX);
            Assert.AreEqual(ConversionStatus.NotStarted, def.conversionStatus);
        }

        [Test]
        public void MapDefinition_RoundTrip_ViaJsonUtility()
        {
            var def = new MapDefinition
            {
                regionCountX = 10,
                regionCountY = 8,
                regionWidthPixels = 512,
                regionHeightPixels = 1024,
                cellWidth = 32,
                cellHeight = 32,
                conversionStatus = ConversionStatus.Partial,
                catalogEntry = new MapCatalogEntry { mapId = 7 },
            };

            var json = JsonUtility.ToJson(def);
            var restored = JsonUtility.FromJson<MapDefinition>(json);

            Assert.AreEqual(10, restored.regionCountX);
            Assert.AreEqual(512, restored.regionWidthPixels);
            Assert.AreEqual(ConversionStatus.Partial, restored.conversionStatus);
            Assert.IsNotNull(restored.catalogEntry);
            Assert.AreEqual(7, restored.catalogEntry.mapId);
        }
    }
}
