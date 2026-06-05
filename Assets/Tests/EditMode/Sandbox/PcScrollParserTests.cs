using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcScrollParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/scroll_sample.txt");

        [Test]
        public void ParseFile_LoadsTenScrollRows()
        {
            var rows = PcScrollParser.ParseFile(SamplePath);
            Assert.AreEqual(10, rows.Count, "Expected 10 scroll rows from sample file");
        }

        [Test]
        public void ParseFile_IdsAreUnique()
        {
            var rows = PcScrollParser.ParseFile(SamplePath);
            var ids = rows.Select(r => r.scrollId).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Scroll ids must be unique");
        }

        [Test]
        public void ParseFile_AllIdsPositive()
        {
            var rows = PcScrollParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.scrollId > 0));
        }

        [Test]
        public void ParseFile_FightStateIsValid()
        {
            var rows = PcScrollParser.ParseFile(SamplePath);
            var valid = new System.Collections.Generic.HashSet<int> { 0, 1, 2, 3 };
            Assert.IsTrue(rows.All(r => valid.Contains(r.fightState)),
                "fightState must be one of {0,1,2,3} for parity with waypoint encoding");
        }
    }
}
