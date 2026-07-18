using System.IO;
using NUnit.Framework;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox.ItemData
{
    public class PcAmuletParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItem/amulet_sample.txt");

        [Test]
        public void ParseFile_LoadsAtLeastFiveAmuletRows()
        {
            var rows = PcAmuletParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
        }

        [Test]
        public void ParseFile_ProducesNonEmptyNames()
        {
            var rows = PcAmuletParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
            foreach (var r in rows)
            {
                Assert.Greater(r.itemId, 0);
                Assert.IsFalse(string.IsNullOrEmpty(r.nameRaw));
                Assert.IsFalse(string.IsNullOrEmpty(r.nameNormalized));
                Assert.IsFalse(r.nameRaw.Contains("�"));
            }
        }
    }
}
