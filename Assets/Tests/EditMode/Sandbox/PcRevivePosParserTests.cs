using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcRevivePosParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/revivepos_sample.ini");
        private static string MapListSamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/maplist_sample.ini");

        [Test]
        public void ParseFile_LoadsAtLeastFivePositions()
        {
            var rows = PcRevivePosParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 5);
        }

        [Test]
        public void ParseFile_AllMapIdsPositive()
        {
            var rows = PcRevivePosParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.mapId > 0));
        }

        [Test]
        public void ParseFile_PositionsAreNonNegative()
        {
            var rows = PcRevivePosParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.x >= 0 && r.y >= 0));
        }

        [Test]
        public void ParseFile_CampJoinsFromMapList()
        {
            var mapList = PcMapListParser.ParseFile(MapListSamplePath);
            var rows = PcRevivePosParser.ParseFile(SamplePath, mapList);
            Assert.Greater(rows.Count, 0);
            Assert.IsTrue(rows.All(r => r.camp >= 0 && r.camp <= 4),
                "camp must be 0..4 (City/Field/Cave/Tong/Battle/Mission slots)");
        }
    }
}
