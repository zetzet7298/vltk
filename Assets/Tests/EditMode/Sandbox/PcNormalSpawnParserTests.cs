using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcNormalSpawnParserTests
    {
        private const string SampleRelative = "Assets/StreamingAssets/Reference/PcSpawn/normal_sample.txt";

        [Test]
        public void ParseFile_LoadsAll20SampleRows()
        {
            var path = LocateSamplePath();
            if (path == null)
            {
                Assert.Inconclusive("normal_sample.txt not found at " + SampleRelative);
                return;
            }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.AreEqual(20, rows.Count, "Sample has 1 header + 20 data rows; parser should yield 20 SpawnPoints");
        }

        [Test]
        public void ParseFile_TemplateIdsArePositive()
        {
            var path = LocateSamplePath();
            if (path == null) { Assert.Inconclusive("missing sample"); return; }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.IsTrue(rows.All(r => r.npcTemplateId > 0), "All sample templateIds should be > 0");
        }

        [Test]
        public void ParseFile_LevelFieldIsNonNegative()
        {
            var path = LocateSamplePath();
            if (path == null) { Assert.Inconclusive("missing sample"); return; }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.IsTrue(rows.All(r => r.level >= 0), "level should be >= 0 (column 8 in source is 1..10)");
        }

        [Test]
        public void ParseFile_NamesHaveNoReplacementChar()
        {
            var path = LocateSamplePath();
            if (path == null) { Assert.Inconclusive("missing sample"); return; }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.IsTrue(rows.All(r => !PcNormalSpawnParser.IsReplacementCharPresent(r.nameRaw)),
                "nameRaw must not contain U+FFFD when source is read with the GB2312 helper");
        }

        [Test]
        public void ParseFile_PopulatesSourceFileAndRowIndex()
        {
            var path = LocateSamplePath();
            if (path == null) { Assert.Inconclusive("missing sample"); return; }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.IsTrue(rows.All(r => r.sourceFile == "normal.txt"));
            Assert.AreEqual(0, rows[0].rowIndex);
            Assert.AreEqual(19, rows[19].rowIndex);
        }

        [Test]
        public void ParseFile_StampsShapeWarning()
        {
            var path = LocateSamplePath();
            if (path == null) { Assert.Inconclusive("missing sample"); return; }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.IsTrue(rows.All(r => r.warnings.Any(w => w.Contains("normal.txt is item equipment data"))),
                "Each row should carry a warning that the source is item equipment data, not monster spawns");
        }

        [Test]
        public void ParseFile_FirstRowMatchesKnownSchema()
        {
            var path = LocateSamplePath();
            if (path == null) { Assert.Inconclusive("missing sample"); return; }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.AreEqual(1, rows[0].npcTemplateId);
            Assert.AreEqual(10, rows[0].level);
            Assert.AreEqual("梦龙之正黄僧帽", rows[0].nameRaw);
        }

        [Test]
        public void ParseFile_KeepsZeroForAbsentMapFields()
        {
            var path = LocateSamplePath();
            if (path == null) { Assert.Inconclusive("missing sample"); return; }

            var rows = PcNormalSpawnParser.ParseFile(path);

            Assert.IsTrue(rows.All(r => r.mapId == 0));
            Assert.IsTrue(rows.All(r => r.x == 0 && r.y == 0));
            Assert.IsTrue(rows.All(r => r.direction == 0));
            Assert.IsTrue(rows.All(r => r.count == 0));
            Assert.IsTrue(rows.All(r => r.respawnSec == 0));
            Assert.IsTrue(rows.All(r => r.aiMode == 0));
            Assert.IsTrue(rows.All(r => r.groupId == 0));
        }

        [Test]
        public void ParseLines_EmptyInputReturnsEmpty()
        {
            var rows = PcNormalSpawnParser.ParseLines(new string[0]);
            Assert.AreEqual(0, rows.Count);
        }

        [Test]
        public void ParseLines_SkipsHeaderRow()
        {
            var lines = new[]
            {
                "c1\tc2\tc3\tc4\tc5\tc6\tc7\tc8",
                "梦龙之正黄僧帽\t1\t1\t-1\t-1\t-1\t-1\t10",
            };
            var rows = PcNormalSpawnParser.ParseLines(lines);
            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(1, rows[0].npcTemplateId);
        }

        [Test]
        public void ParseLines_SkipsBlankRows()
        {
            var lines = new[]
            {
                "h1\th2\th3\th4\th5\th6\th7\th8",
                "",
                "梦龙之正黄僧帽\t1\t1\t-1\t-1\t-1\t-1\t10",
                "   ",
                "梦龙之金丝正红袈裟\t1\t2\t-1\t-1\t-1\t-1\t10",
            };
            var rows = PcNormalSpawnParser.ParseLines(lines);
            Assert.AreEqual(2, rows.Count);
        }

        [Test]
        public void ParseLines_SkipsRowsShorterThanMinColumns()
        {
            var lines = new[]
            {
                "h1\th2\th3\th4\th5\th6\th7\th8",
                "name\t1\t1",
            };
            var rows = PcNormalSpawnParser.ParseLines(lines);
            Assert.AreEqual(0, rows.Count);
        }

        private static string LocateSamplePath()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var direct = Path.Combine(repoRoot, SampleRelative);
            if (File.Exists(direct)) return direct;
            var dataPath = Path.Combine(Application.dataPath, "StreamingAssets", "Reference", "PcSpawn", "normal_sample.txt");
            if (File.Exists(dataPath)) return dataPath;
            return null;
        }
    }
}
