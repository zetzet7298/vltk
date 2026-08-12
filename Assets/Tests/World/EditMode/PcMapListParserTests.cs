using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMapListParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcMap/maplist_sample.ini");

        [Test]
        public void ParseFile_LoadsAll10MapSampleRows()
        {
            var rows = PcMapListParser.ParseFile(SamplePath);
            Assert.AreEqual(10, rows.Count, "Expected 10 map rows from sample file");
        }

        [Test]
        public void ParseFile_AllIdsArePositive()
        {
            var rows = PcMapListParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => r.mapId > 0), "Every map row must have mapId > 0");
        }

        [Test]
        public void ParseFile_NamesAreNonEmpty()
        {
            var rows = PcMapListParser.ParseFile(SamplePath);
            Assert.IsTrue(rows.All(r => !string.IsNullOrEmpty(r.nameRaw)), "Every map row must have a name");
            Assert.IsTrue(rows.All(r => !string.IsNullOrEmpty(r.nameNormalized)), "Every map row must have a normalized name");
        }

        [Test]
        public void ParseFile_MapTypesInKnownSet()
        {
            var rows = PcMapListParser.ParseFile(SamplePath);
            var known = new System.Collections.Generic.HashSet<string> { "City", "Field", "Cave", "Tong", "Battle", "Mission" };
            foreach (var r in rows)
            {
                Assert.IsTrue(known.Contains(r.mapType),
                    $"mapId {r.mapId} has unknown mapType '{r.mapType}'");
            }
        }

        [Test]
        public void ParseFile_NoReplacementCharInNames()
        {
            var rows = PcMapListParser.ParseFile(SamplePath);
            foreach (var r in rows)
            {
                Assert.IsFalse(r.nameRaw.Contains('\ufffd'),
                    $"mapId {r.mapId} nameRaw contains Unicode replacement char");
                Assert.IsFalse(r.nameNormalized.Contains('\ufffd'),
                    $"mapId {r.mapId} nameNormalized contains Unicode replacement char");
            }
        }

        [Test]
        public void ParseFile_CaveRowsCarryLevelRange()
        {
            var rows = PcMapListParser.ParseFile(SamplePath);
            var caves = rows.Where(r => r.mapType == "Cave").ToList();
            Assert.Greater(caves.Count, 0, "Sample should include Cave maps");
            Assert.IsTrue(caves.Any(c => c.levelMax >= c.levelMin && c.levelMax > 0),
                "Cave maps should carry a level range");
        }

        [Test]
        public void BuildMapCatalog_StripsEmptyRows()
        {
            var rows = PcMapListParser.ParseFile(SamplePath);
            var built = PcMapListParser.BuildMapCatalog(rows);
            Assert.AreEqual(rows.Count, built.Count);
            Assert.IsTrue(built.All(b => b.mapId > 0));
            Assert.IsTrue(built.All(b => !string.IsNullOrEmpty(b.nameRaw)));
        }
    }
}
