using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcRevivePosParserTests
    {
        private const int ExpectedPcReviveSections = 139;
        private const int ExpectedPcRevivePositions = 241;
        private const string PcSourcePath = "/var/www/vltksource_new/vl_update_27/Client 6.0/settings/revivepos.ini";
        private static string ReferencePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/revivepos.ini");
        private static string MapListReferencePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/maplist.ini");

        [Test]
        public void ParseFile_LoadsExactPcRevivePositionCount()
        {
            var rows = PcRevivePosParser.ParseFile(ReferencePath);
            Assert.AreEqual(ExpectedPcRevivePositions, rows.Count,
                $"Reference revivepos.ini must match exact PC coordinate-row count from {PcSourcePath}");
        }

        [Test]
        public void ParseFile_LoadsExactPcReviveSectionCount()
        {
            var rows = PcRevivePosParser.ParseFile(ReferencePath);
            Assert.AreEqual(ExpectedPcReviveSections, rows.Select(r => r.mapId).Distinct().Count(),
                "PC revivepos.ini has 139 map sections with coordinate rows");
        }

        [Test]
        public void ParseFile_AllMapIdsPositive()
        {
            var rows = PcRevivePosParser.ParseFile(ReferencePath);
            Assert.IsTrue(rows.All(r => r.mapId > 0));
        }

        [Test]
        public void ParseFile_PositionsAreNonNegative()
        {
            var rows = PcRevivePosParser.ParseFile(ReferencePath);
            Assert.IsTrue(rows.All(r => r.x >= 0 && r.y >= 0));
        }

        [Test]
        public void ParseFile_CampJoinsFromMapList()
        {
            var mapList = PcMapListParser.ParseFile(MapListReferencePath);
            var rows = PcRevivePosParser.ParseFile(ReferencePath, mapList);
            Assert.AreEqual(ExpectedPcRevivePositions, rows.Count);
            Assert.IsTrue(rows.All(r => r.camp >= 0 && r.camp <= 4),
                "camp must be 0..4 (City/Field/Cave/Tong/Battle/Mission slots)");
        }

        [Test]
        public void ParseFile_PreservesKnownRegionRangeMismatchForMap949()
        {
            var rows = PcRevivePosParser.ParseFile(ReferencePath).Where(r => r.mapId == 949).ToList();
            Assert.AreEqual(1, rows.Count,
                "Known PC mismatch: [949] declares region=1,3 but only coordinate key 1 is present; do not fabricate missing rows.");
            Assert.AreEqual(1, rows[0].regionStart);
            Assert.AreEqual(3, rows[0].regionEnd);
            Assert.AreEqual(1, rows[0].regionIndex);
            Assert.AreEqual(51264, rows[0].x);
            Assert.AreEqual(102368, rows[0].y);
        }
    }
}
