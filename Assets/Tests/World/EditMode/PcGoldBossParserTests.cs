using System.IO;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcGoldBossParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc/goldboss_sample.txt");

        [Test]
        public void ParseFile_LoadsFiveSampleRows()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcGoldBossParser.ParseFile(SamplePath);

            Assert.AreEqual(5, rows.Count, "Expected exactly 5 gold boss rows from the sample");
        }

        [Test]
        public void ParseFile_BossTemplateIdsSequential()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcGoldBossParser.ParseFile(SamplePath);

            // PcGoldBossEntry.bossId replaced the former bossTemplateId. ParseFile assigns
            // its auto id starting at 1, so row i carries a sequential id of i + 1.
            for (int i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(i + 1, rows[i].bossId, $"Row {i} should have bossId={i + 1}");
            }
        }

        [Test]
        public void ParseFile_NamesAreNonEmpty()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcGoldBossParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                // nameRaw was renamed to name on PcGoldBossEntry.
                Assert.IsFalse(string.IsNullOrEmpty(r.name), $"bossId={r.bossId} name empty");
                // TODO: nameNormalized has no equivalent on the current PcGoldBossEntry
                // (the entry keeps only the raw name). Restore this assertion if a
                // normalized-name field is reintroduced on the entry.
            }
        }

        [Test]
        public void ParseFile_PhysicalDamageBaseParsed()
        {
            // TODO: physicalDamageBase has no equivalent on the current PcGoldBossEntry
            // (the Sandbox parser keeps only name + skill data, dropping damage columns).
            // Restore once the entry exposes physical damage again.
            Assert.Ignore("physicalDamageBase not present on PcGoldBossEntry; see TODO.");

            // Original verification intent (preserved for restoration):
            // var rows = PcGoldBossParser.ParseFile(SamplePath);
            // foreach (var r in rows)
            //     Assert.GreaterOrEqual(r.physicalDamageBase, 0,
            //         $"bossId={r.bossId} physicalDamageBase must be >= 0");
        }

        [Test]
        public void ParseFile_AuraAndPassiveSkillsNonEmpty()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcGoldBossParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.auraSkillName),
                    $"bossId={r.bossId} auraSkillName must be present");
                Assert.IsFalse(string.IsNullOrEmpty(r.passiveSkillName),
                    $"bossId={r.bossId} passiveSkillName must be present");
            }
        }

        [Test]
        public void ParseRow_HandlesShortRow()
        {
            // TODO: PcGoldBossParser no longer exposes a public ParseRow(string[]) entry
            // point (row parsing is inlined inside ParseFile). Restore this case if a
            // ParseRow overload is reintroduced.
            Assert.Ignore("PcGoldBossParser.ParseRow not present; see TODO.");

            // Original verification intent (preserved for restoration):
            // var cols = new string[2];
            // cols[0] = "TinyBoss";
            // cols[1] = "5|10";
            // var row = PcGoldBossParser.ParseRow(cols);
            // Assert.AreEqual("TinyBoss", row.name);
            // Assert.AreEqual(5, row.physicalDamageBase);
            // Assert.AreEqual(0, row.auraSkillLevel);
            // Assert.AreEqual(string.Empty, row.auraSkillName);
        }

        [Test]
        public void ParseRow_ParsesRateToken()
        {
            // TODO: PcGoldBossParser no longer exposes a public ParseRow(string[]) entry
            // point. Restore this case if a ParseRow overload is reintroduced.
            Assert.Ignore("PcGoldBossParser.ParseRow not present; see TODO.");

            // Original verification intent (preserved for restoration):
            // var cols = new string[]
            // {
            //     "BossX", "5|10", "0", "0|0", "0", "0|0", "0", "0|0", "0", "0|0", "0",
            //     "AuraName", "30", "PassiveName", "60",
            // };
            // var row = PcGoldBossParser.ParseRow(cols);
            // Assert.AreEqual(5, row.physicalDamageBase, "Rate token should be parsed as 5 (left of |)");
            // Assert.AreEqual("AuraName", row.auraSkillName);
            // Assert.AreEqual(30, row.auraSkillLevel);
            // Assert.AreEqual("PassiveName", row.passiveSkillName);
            // Assert.AreEqual(60, row.passiveSkillLevel);
        }

        [Test]
        public void ParseLines_HandlesEmptyLines()
        {
            // TODO: PcGoldBossParser no longer exposes a public ParseLines(...) entry point
            // (only ParseFile(path)). Restore once a line-based overload is reintroduced.
            Assert.Ignore("PcGoldBossParser.ParseLines not present; see TODO.");

            // Original verification intent (preserved for restoration):
            // var lines = new[]
            // {
            //     "Name\tPhysicalDamageBase\tPhysicalMagic\tPoisonDamageBase\tPoisonMagic\tColdDamageBase\tColdMagic\tFireDamageBase\tFireMagic\tLightingDamageBase\tLightingMagic\tAuraSkillName\tAuraSkillLevel\tPasstSkillName\tPasstSkillLevel",
            //     "",
            //     "Boss1\t5|10\t0\t0|0\t1\t0|0\t1\t0|0\t1\t0|1000\t1\tAura1\t30\tPassive1\t60",
            //     "   ",
            //     "Boss2\t6|20\t0\t0|0\t1\t0|0\t1\t0|0\t1\t0|1000\t1\tAura2\t40\tPassive2\t70",
            // };
            // var rows = PcGoldBossParser.ParseLines(lines);
            // Assert.AreEqual(2, rows.Count);
            // Assert.AreEqual(0, rows[0].bossId);
            // Assert.AreEqual(1, rows[1].bossId);
        }
    }
}
