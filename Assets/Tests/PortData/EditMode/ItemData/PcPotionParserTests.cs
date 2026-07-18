using System.IO;
using NUnit.Framework;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox.ItemData
{
    public class PcPotionParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItem/potion_sample.txt");

        [Test]
        public void ParseFile_LoadsAtLeastFivePotionRows()
        {
            var rows = PcPotionParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
        }

        [Test]
        public void ParseFile_ProducesNonEmptyNames()
        {
            var rows = PcPotionParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
            foreach (var r in rows)
            {
                Assert.Greater(r.itemId, 0);
                Assert.IsFalse(string.IsNullOrEmpty(r.nameRaw));
                Assert.IsFalse(string.IsNullOrEmpty(r.nameNormalized));
                Assert.IsFalse(r.nameRaw.Contains("�"));
            }
        }

        [Test]
        public void ParseRow_DirectlyMaps28ColPotion()
        {
            string[] row = new string[28];
            for (int i = 0; i < 28; i++) row[i] = string.Empty;
            row[0] = "Kim Sang Dược (tiểu)";
            row[4] = "\\spr\\item\\medecine\\test.spr";
            row[13] = "153";
            row[14] = "10";
            row[15] = "100";

            var item = PcPotionParser.ParseRow(row, itemIdHint: 7);
            Assert.IsNotNull(item);
            Assert.AreEqual(7, item.itemId);
            Assert.AreEqual("Kim Sang Dược (tiểu)", item.nameRaw);
            Assert.GreaterOrEqual(item.statDeltas.Count, 1);
        }
    }
}
