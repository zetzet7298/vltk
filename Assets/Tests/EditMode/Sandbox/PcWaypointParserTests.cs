using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcWaypointParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/waypoint_sample.txt");

        [Test]
        public void ParseFile_LoadsTenWaypointRows()
        {
            var rows = PcWaypointParser.ParseFile(SamplePath);
            Assert.AreEqual(10, rows.Count, "Expected 10 waypoint rows from sample file");
        }

        [Test]
        public void ParseFile_IdsAreUnique()
        {
            var rows = PcWaypointParser.ParseFile(SamplePath);
            var ids = rows.Select(r => r.waypointId).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Waypoint ids must be unique");
        }

        [Test]
        public void ParseFile_AllIdsPositive()
        {
            var rows = PcWaypointParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.waypointId > 0));
        }

        [Test]
        public void ParseFile_FightStateInValidSet()
        {
            var rows = PcWaypointParser.ParseFile(SamplePath);
            var valid = new System.Collections.Generic.HashSet<int> { 0, 1, 2, 3 };
            Assert.IsTrue(rows.All(r => valid.Contains(r.fightState)),
                "fightState must be one of {0,1,2,3}");
        }

        [Test]
        public void ParseFile_MapIdParsedFromSect()
        {
            var rows = PcWaypointParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.mapId > 0), "Every waypoint must carry a mapId parsed from SECT");
        }
    }
}
