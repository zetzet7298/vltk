using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcCaveListParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/cavelist_sample.ini");
        private static string MapListSamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/maplist_sample.ini");

        [Test]
        public void ParseFile_LoadsFiveCaveSampleRows()
        {
            var rows = PcCaveListParser.ParseFile(SamplePath);
            Assert.AreEqual(5, rows.Count, "Expected 5 cave rows from sample file");
        }

        [Test]
        public void ParseFile_CaveIdsArePositive()
        {
            var rows = PcCaveListParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.caveId > 0));
        }

        [Test]
        public void ParseFile_MapIdsMatchCaveIds()
        {
            var rows = PcCaveListParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.mapId == r.caveId), "Default mapId should equal caveId");
        }

        [Test]
        public void ParseFile_NamesAreNonEmpty()
        {
            var rows = PcCaveListParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => !string.IsNullOrEmpty(r.nameRaw)));
        }

        [Test]
        public void ParseFile_BossTemplateIdJoinsFromMapList()
        {
            var mapList = PcMapListParser.ParseFile(MapListSamplePath);
            var rows = PcCaveListParser.ParseFile(SamplePath, mapList);
            Assert.Greater(rows.Count(r => r.bossTemplateId > 0), 0,
                "Sample maplist declares AutoGoldenNpc>0 on at least one cave; cavelist join should pick that up");
        }
    }
}
