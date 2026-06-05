using System.IO;
using NUnit.Framework;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox.ItemData
{
    public class PcItemBatchLoaderTests
    {
        private static string ItemDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItem");

        [Test]
        public void LoadAll_ReadsAllTwelvePcItemFiles()
        {
            var batch = PcItemBatchLoader.LoadAll(ItemDir);
            Assert.IsNotNull(batch);
            Assert.AreEqual(12, batch.perFileCounts.Count, "batch should have 12 per-file entries");
            foreach (var key in new[]
            {
                "armor", "helm", "boot", "cuff", "belt", "ring",
                "amulet", "pendant", "meleeweapon", "rangeweapon",
                "horse", "potion"
            })
            {
                Assert.IsTrue(batch.perFileCounts.ContainsKey(key), $"Missing per-file key: {key}");
                Assert.GreaterOrEqual(batch.perFileCounts[key], 5, $"{key} should have at least 5 sample rows");
            }
            Assert.GreaterOrEqual(batch.totalLoaded, 60);
        }

        [Test]
        public void LoadAll_ItemsHavePositiveItemIdAndNonEmptyNames()
        {
            var batch = PcItemBatchLoader.LoadAll(ItemDir);
            Assert.GreaterOrEqual(batch.items.Count, 60);
            foreach (var item in batch.items)
            {
                Assert.Greater(item.itemId, 0, $"Item missing id: {item.nameRaw}");
                Assert.IsFalse(string.IsNullOrEmpty(item.nameRaw));
                Assert.IsFalse(string.IsNullOrEmpty(item.nameNormalized));
                Assert.IsFalse(item.nameRaw.Contains("�"));
            }
        }

        [Test]
        public void ImportInto_BuildsItemContractBundleWithAllItems()
        {
            var importer = PcItemBatchLoader.ImportInto(ItemDir, new VLTK.Sandbox.ItemContractImporter());
            Assert.GreaterOrEqual(importer.Count, 60);
        }
    }
}
