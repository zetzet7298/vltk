using System.IO;
using NUnit.Framework;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox.ItemData
{
    public class PcArmorParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItem/armor_sample.txt");

        [Test]
        public void ParseFile_LoadsAtLeastFiveArmorRows()
        {
            var rows = PcArmorParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
        }

        [Test]
        public void ParseFile_ProducesNonEmptyNames()
        {
            var rows = PcArmorParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
            foreach (var r in rows)
            {
                Assert.Greater(r.itemId, 0);
                Assert.IsFalse(string.IsNullOrEmpty(r.nameRaw), $"Empty nameRaw for id={r.itemId}");
                Assert.IsFalse(string.IsNullOrEmpty(r.nameNormalized), $"Empty nameNormalized for id={r.itemId}");
                Assert.IsFalse(r.nameRaw.Contains("�"), $"Armor id={r.itemId} nameRaw has Unicode replacement char");
                Assert.IsFalse(r.nameNormalized.Contains("�"), $"Armor id={r.itemId} nameNormalized has Unicode replacement char");
            }
        }

        [Test]
        public void ParseRow_DirectlyMaps46ColArmor()
        {
            string[] row = new string[46];
            for (int i = 0; i < 46; i++) row[i] = string.Empty;
            row[0] = "T\xe1\xba\xa3 y";
            row[1] = "0";
            row[4] = "\\spr\\item\\equip\\armor\\test.spr";
            row[5] = "22";
            row[11] = "100";
            row[14] = "30";
            row[15] = "50";
            row[16] = "50";
            row[17] = "31";
            row[18] = "5";

            var item = PcArmorParser.ParseRow(row, itemIdHint: 5);
            Assert.IsNotNull(item);
            Assert.AreEqual(5, item.itemId);
            Assert.AreEqual("T\xe1\xba\xa3 y", item.nameRaw);
            Assert.AreEqual(item.nameRaw, item.nameNormalized);
        }
    }
}
