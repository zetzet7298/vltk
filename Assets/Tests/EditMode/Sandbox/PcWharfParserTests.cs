using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcWharfParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/wharf_sample.txt");

        [Test]
        public void ParseFile_LoadsFiveWharfRows()
        {
            var rows = PcWharfParser.ParseFile(SamplePath);
            Assert.AreEqual(5, rows.Count, "Expected 5 wharf rows from sample file");
        }

        [Test]
        public void ParseFile_AllIdsPositive()
        {
            var rows = PcWharfParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.wharfId > 0));
        }

        [Test]
        public void ParseFile_PositionInValidRange()
        {
            var rows = PcWharfParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.posX >= 0 && r.posX < 200000),
                "Wharf posX must be in valid PC range");
            Assert.IsTrue(rows.All(r => r.posY >= 0 && r.posY < 200000),
                "Wharf posY must be in valid PC range");
        }

        [Test]
        public void ParseFile_SectCountReflectsColumns()
        {
            var rows = PcWharfParser.ParseFile(SamplePath);
            var multi = rows.FirstOrDefault(r => r.sectCount > 1);
            Assert.IsNotNull(multi, "Sample includes at least one multi-stop wharf");
            Assert.GreaterOrEqual(multi.sectCount, 2);
        }
    }
}
