using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcWaypointParserTests
    {
        private const int ExpectedPcWaypointRows = 225;
        private const string PcSourcePath = "/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/waypoint.txt";
        private static string ReferencePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/waypoint.txt");

        [Test]
        public void ParseFile_LoadsExactPcWaypointCount()
        {
            var rows = PcWaypointParser.ParseFile(ReferencePath);
            Assert.AreEqual(ExpectedPcWaypointRows, rows.Count,
                $"Reference waypoint.txt must match exact PC source row count from {PcSourcePath}");
        }

        [Test]
        public void ParseFile_IdsAreUnique()
        {
            var rows = PcWaypointParser.ParseFile(ReferencePath);
            var ids = rows.Select(r => r.waypointId).ToList();
            Assert.AreEqual(ids.Count, ids.Distinct().Count(), "Waypoint ids must be unique");
        }

        [Test]
        public void ParseFile_AllIdsPositive()
        {
            var rows = PcWaypointParser.ParseFile(ReferencePath);
            Assert.IsTrue(rows.All(r => r.waypointId > 0));
        }

        [Test]
        public void ParseFile_FightStateInValidSet()
        {
            var rows = PcWaypointParser.ParseFile(ReferencePath);
            var valid = new System.Collections.Generic.HashSet<int> { 0, 1, 2, 3 };
            Assert.IsTrue(rows.All(r => valid.Contains(r.fightState)),
                "fightState must be one of {0,1,2,3}");
        }

        [Test]
        public void ParseFile_MapAndPositionParsedFromSectColumn()
        {
            var rows = PcWaypointParser.ParseFile(ReferencePath);
            Assert.IsTrue(rows.All(r => r.mapId > 0), "Every waypoint must carry a mapId parsed from SECT");
            Assert.IsTrue(rows.All(r => r.posX > 0 && r.posY > 0), "Every waypoint must carry x/y parsed from SECT");

            var first = rows.Single(r => r.waypointId == 1);
            Assert.AreEqual(2, first.mapId);
            Assert.AreEqual(2288, first.posX);
            Assert.AreEqual(4091, first.posY);

            var last = rows.Single(r => r.waypointId == 225);
            Assert.AreEqual(340, last.mapId);
            Assert.AreEqual(1853, last.posX);
            Assert.AreEqual(3446, last.posY);
        }
    }
}
