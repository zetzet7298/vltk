using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcRareSpawnParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc/rare_sample.txt");

        [Test]
        public void ParseFile_LoadsFiveSampleRows()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            Assert.AreEqual(5, rows.Count, "Expected exactly 5 rare rows from the sample");
        }

        [Test]
        public void ParseFile_NamesAreNonEmpty()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.nameRaw), $"entryId={r.entryId} nameRaw empty");
            }
        }

        [Test]
        public void ParseFile_MagicIdPositive()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.npcTemplateId, 0, $"entryId={r.entryId} magicId/npcTemplateId must be > 0");
            }
        }

        [Test]
        public void ParseFile_RespawnSecDerivedFromLevelMax()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.respawnSec, 0, $"entryId={r.entryId} respawnSec must be > 0");
                Assert.AreEqual(r.levelMax, r.respawnSec);
            }
        }

        [Test]
        public void ParseRow_HandlesShortRow()
        {
            var cols = new string[2];
            cols[0] = "TinyRow";
            cols[1] = "42";
            var row = PcRareSpawnParser.ParseRow(cols);
            Assert.AreEqual("TinyRow", row.nameRaw);
            Assert.AreEqual(42, row.npcTemplateId);
            Assert.AreEqual(0, row.levelMin);
            Assert.AreEqual(0, row.levelMax);
        }

        [Test]
        public void ParseLines_HandlesEmptyLines()
        {
            var lines = new[]
            {
                "NAME\tMAGIC_ID\tMAG_P1_MIN\tMAG_P1_MAX",
                "",
                "Foo\t1\t1\t2",
                "   ",
                "Bar\t2\t3\t4",
            };

            var rows = PcRareSpawnParser.ParseLines(lines);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(0, rows[0].entryId);
            Assert.AreEqual(1, rows[1].entryId);
        }
    }
}
