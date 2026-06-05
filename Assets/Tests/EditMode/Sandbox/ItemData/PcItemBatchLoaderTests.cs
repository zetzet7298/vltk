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
        public void LoadAll_ReadsAllFourteenPcItemFiles()
        {
            var batch = PcItemBatchLoader.LoadAll(ItemDir);
            Assert.IsNotNull(batch);
            Assert.AreEqual(14, batch.perFileCounts.Count, "batch should have 14 per-file entries");
            foreach (var key in new[]
            {
                "armor", "helm", "boot", "cuff", "belt", "ring",
                "amulet", "pendant", "meleeweapon", "rangeweapon",
                "horse", "potion", "goldequip", "platinaequip"
            })
            {
                Assert.IsTrue(batch.perFileCounts.ContainsKey(key), $"Missing per-file key: {key}");
                Assert.GreaterOrEqual(batch.perFileCounts[key], 5, $"{key} should have at least 5 rows");
            }
            // 12 sample files (~5 each) + goldequip (~5346) + platinaequip (~5336) = ~10k+
            Assert.GreaterOrEqual(batch.totalLoaded, 5000);
        }

        [Test]
        public void LoadAll_ItemsHavePositiveItemIdAndNonEmptyNames()
        {
            var batch = PcItemBatchLoader.LoadAll(ItemDir);
            Assert.GreaterOrEqual(batch.items.Count, 5000);
            foreach (var item in batch.items)
            {
                Assert.Greater(item.itemId, 0, $"Item missing id: {item.nameRaw}");
                Assert.IsFalse(string.IsNullOrEmpty(item.nameRaw));
                Assert.IsFalse(string.IsNullOrEmpty(item.nameNormalized));
                Assert.IsFalse(item.nameRaw.Contains("�"));
            }
        }

        [Test]
        public void LoadAll_GoldEquipItemsHaveStatsAndMagicIndices()
        {
            var batch = PcItemBatchLoader.LoadAll(ItemDir);
            Assert.GreaterOrEqual(batch.perFileCounts["goldequip"], 5000, "goldequip should have thousands of items");
            int itemsWithMagic = 0;
            foreach (var item in batch.items)
            {
                if (item.statDeltas.Exists(d => d.ruleId.StartsWith("MAGIC_IDX_")))
                    itemsWithMagic++;
            }
            Assert.Greater(itemsWithMagic, 100, "many gold equip items should have magic attribute indices");
        }

        [Test]
        public void LoadAll_PlatinaEquipItemsHaveStats()
        {
            var batch = PcItemBatchLoader.LoadAll(ItemDir);
            Assert.GreaterOrEqual(batch.perFileCounts["platinaequip"], 5000, "platinaequip should have thousands of items");
        }

        [Test]
        public void ImportInto_BuildsItemContractBundleWithAllItems()
        {
            var importer = PcItemBatchLoader.ImportInto(ItemDir, new VLTK.Sandbox.ItemContractImporter());
            Assert.GreaterOrEqual(importer.Count, 5000);
        }
    }
}
