using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // NOTE (audit 2026-06-12): This test was originally written against a richer
    // PcRareSpawnEntry shape (nameRaw/entryId/levelMin/levelMax) and a parser with
    // ParseRow/ParseLines. The real production parser (Assets/Scripts/Sandbox/
    // PcRareSpawnParser.cs) models rare.txt as a numeric spawn table:
    //   npcId, npcTemplateId, mapId, posX, posY, respawnSec, probability
    // and only exposes ParseFile(path). The tests below are aligned to that real
    // schema. Assertions are preserved where the production API supports them;
    // cases that referenced a non-existent per-row/per-lines API are marked
    // Assert.Ignore with a TODO instead of being silently dropped.
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
        public void ParseFile_NpcIdsArePositive()
        {
            // Real rare.txt is numeric-keyed (no name column). Original test checked
            // nameRaw non-empty; the equivalent validity check on this schema is npcId > 0.
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.npcId, 0, $"npcId must be > 0 (mapId={r.mapId})");
            }
        }

        [Test]
        public void ParseFile_TemplateIdPositive()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.npcTemplateId, 0, $"npcId={r.npcId} npcTemplateId must be > 0");
            }
        }

        [Test]
        public void ParseFile_RespawnSecPositive()
        {
            // Original test asserted respawnSec == levelMax. The real schema has no
            // levelMax column; respawnSec is parsed directly from the RespawnCol.
            // Preserve the "respawn is a valid positive duration" intent.
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.respawnSec, 0, $"npcId={r.npcId} respawnSec must be > 0");
            }
        }

        [Test]
        public void ParseFile_ProbabilityInRange()
        {
            // Added: rare spawn probability is normalized to 0..1 by the parser.
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareSpawnParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.GreaterOrEqual(r.probability, 0f, $"npcId={r.npcId} probability >= 0");
                Assert.LessOrEqual(r.probability, 1f, $"npcId={r.npcId} probability <= 1");
            }
        }

        [Test]
        public void ParseRow_HandlesShortRow()
        {
            // TODO(port): production PcRareSpawnParser exposes only ParseFile(path);
            // there is no public per-row ParseRow(string[]) API to unit-test a short row.
            // Restore this assertion if/when a row-level parser is added.
            Assert.Ignore("PcRareSpawnParser has no public ParseRow API; per-row parsing is internal to ParseFile.");
        }

        [Test]
        public void ParseLines_HandlesEmptyLines()
        {
            // TODO(port): production PcRareSpawnParser exposes only ParseFile(path);
            // there is no public ParseLines(string[]) API to test empty-line skipping
            // in isolation (ParseFile does skip blank lines internally via ReadServerLines).
            Assert.Ignore("PcRareSpawnParser has no public ParseLines API; blank-line skipping is internal to ParseFile.");
        }
    }
}
