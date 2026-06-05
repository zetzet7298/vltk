using System.IO;
using NUnit.Framework;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox.ItemData
{
    public class PcPendantParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcItem/pendant_sample.txt");

        [Test]
        public void ParseFile_LoadsAtLeastFivePendantRows()
        {
            var rows = PcPendantParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
        }

        [Test]
        public void ParseFile_ProducesNonEmptyNames()
        {
            var rows = PcPendantParser.ParseFile(SamplePath);
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
