using System.IO;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcDropRateParserTests
    {
        private static string SampleDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcDropRate");
        private static string Sample10 => Path.Combine(SampleDir, "npcdroprate10_sample.ini");

        [Test]
        public void ParseFile_LoadsMainAndEntries()
        {
            var table = PcDropRateParser.ParseFile(Sample10, "npcdroprate10_sample");

            Assert.IsNotNull(table, "Parser should not return null for a real sample file");
            Assert.IsNotNull(table.entries, "entries list should be allocated");
            Assert.AreEqual("npcdroprate10_sample", table.tableName);
            Assert.Greater(table.count, 0, "Count should be > 0 from [Main]");
            Assert.Greater(table.randRange, 0, "RandRange should be > 0 from [Main]");
            Assert.GreaterOrEqual(table.moneyRate, 0, "MoneyRate should be >= 0");
            Assert.LessOrEqual(table.moneyRate, 100, "MoneyRate should be <= 100 (PC convention)");
            Assert.Greater(table.entries.Count, 0, "At least one [N] entry expected in the sample");
            foreach (var e in table.entries)
            {
                Assert.GreaterOrEqual(e.randRate, 0, "Each entry RandRate should be >= 0");
                Assert.GreaterOrEqual(e.probability, 0f, "Probability should be >= 0");
            }
        }

        [Test]
        public void ParseLines_HandlesMissingSectionGracefully()
        {
            var table = PcDropRateParser.ParseLines(new[]
            {
                "[Main]",
                "Count=3",
                "RandRange=100",
            }, "partial");

            Assert.IsNotNull(table);
            Assert.AreEqual(3, table.count);
            Assert.AreEqual(100, table.randRange);
            Assert.AreEqual(0, table.entries.Count);
        }

        [Test]
        public void ResolveItemId_FoldsGenreDetailParticular()
        {
            int id = PcDropRateParser.ResolveItemId(6, 1, 196);
            Assert.AreEqual(6001196, id);
        }

        [Test]
        public void ParseFile_AppliesMainDefaultsWhenKeysMissing()
        {
            var table = PcDropRateParser.ParseLines(new[]
            {
                "[Main]",
                "Count=0",
            }, "empty");

            Assert.AreEqual(0, table.count);
            Assert.AreEqual(0, table.randRange);
            Assert.AreEqual(0, table.moneyRate);
        }
    }
}
