using NUnit.Framework;
using VLTK.Model;

namespace VLTK.Tests.Model
{
    public class ConversionManifestTests
    {
        [Test]
        public void ConversionManifest_RoundTrip_ViaJsonUtility()
        {
            var manifest = new ConversionManifest
            {
                manifestVersion = 1,
                sourceRoot = "/var/www/vltktool/unpacked/maps_pak",
                conversionTimestamp = 1748000000L,
                toolVersion = "parse_map_settings.py@git:abc123",
            };
            manifest.inputs.Add(new ConversionManifest.ConversionEntry
            {
                path = "maps_pak/971b75ae.ini",
                checksum = "sha256:deadbeef",
            });
            manifest.outputs.Add(new ConversionManifest.ConversionEntry
            {
                path = "MapCatalog.json",
                checksum = "sha256:cafebabe",
            });
            manifest.warnings.Add("3 maps missing eventName");
            manifest.coverage.totalAssets = 158;
            manifest.coverage.converted = 155;
            manifest.coverage.missing = 3;

            var json = UnityEngine.JsonUtility.ToJson(manifest);
            Assert.IsNotNull(json);

            var restored = UnityEngine.JsonUtility.FromJson<ConversionManifest>(json);
            Assert.IsNotNull(restored);
            Assert.AreEqual(1, restored.manifestVersion);
            Assert.AreEqual(1748000000L, restored.conversionTimestamp);
            Assert.AreEqual(1, restored.inputs.Count);
            Assert.AreEqual(1, restored.outputs.Count);
            Assert.AreEqual(1, restored.warnings.Count);
            Assert.AreEqual(158, restored.coverage.totalAssets);
            Assert.AreEqual(3, restored.coverage.missing);
        }

        [Test]
        public void ConversionManifest_EmptyByDefault()
        {
            var manifest = new ConversionManifest();
            Assert.IsNotNull(manifest.inputs);
            Assert.IsNotNull(manifest.outputs);
            Assert.IsNotNull(manifest.warnings);
            Assert.IsNotNull(manifest.errors);
            Assert.IsNotNull(manifest.coverage);
            Assert.AreEqual(0, manifest.inputs.Count);
            Assert.AreEqual(0, manifest.errors.Count);
        }
    }
}
