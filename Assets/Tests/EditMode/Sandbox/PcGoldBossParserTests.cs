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

            for (int i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(i, rows[i].bossTemplateId, $"Row {i} should have bossTemplateId={i}");
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
                Assert.IsFalse(string.IsNullOrEmpty(r.nameRaw), $"bossTemplateId={r.bossTemplateId} nameRaw empty");
                Assert.IsFalse(string.IsNullOrEmpty(r.nameNormalized), $"bossTemplateId={r.bossTemplateId} nameNormalized empty");
            }
        }

        [Test]
        public void ParseFile_PhysicalDamageBaseParsed()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcGoldBossParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.GreaterOrEqual(r.physicalDamageBase, 0, $"bossTemplateId={r.bossTemplateId} physicalDamageBase must be >= 0");
            }
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
                    $"bossTemplateId={r.bossTemplateId} auraSkillName must be present");
                Assert.IsFalse(string.IsNullOrEmpty(r.passiveSkillName),
                    $"bossTemplateId={r.bossTemplateId} passiveSkillName must be present");
            }
        }

        [Test]
        public void ParseRow_HandlesShortRow()
        {
            var cols = new string[2];
            cols[0] = "TinyBoss";
            cols[1] = "5|10";
            var row = PcGoldBossParser.ParseRow(cols);
            Assert.AreEqual("TinyBoss", row.nameRaw);
            Assert.AreEqual(5, row.physicalDamageBase);
            Assert.AreEqual(0, row.auraSkillLevel);
            Assert.AreEqual(string.Empty, row.auraSkillName);
        }

        [Test]
        public void ParseRow_ParsesRateToken()
        {
            var cols = new string[]
            {
                "BossX", "5|10", "0", "0|0", "0", "0|0", "0", "0|0", "0", "0|0", "0", "AuraName", "30", "PassiveName", "60",
            };
            var row = PcGoldBossParser.ParseRow(cols);
            Assert.AreEqual(5, row.physicalDamageBase, "Rate token should be parsed as 5 (left of |)");
            Assert.AreEqual("AuraName", row.auraSkillName);
            Assert.AreEqual(30, row.auraSkillLevel);
            Assert.AreEqual("PassiveName", row.passiveSkillName);
            Assert.AreEqual(60, row.passiveSkillLevel);
        }

        [Test]
        public void ParseLines_HandlesEmptyLines()
        {
            var lines = new[]
            {
                "Name\tPhysicalDamageBase\tPhysicalMagic\tPoisonDamageBase\tPoisonMagic\tColdDamageBase\tColdMagic\tFireDamageBase\tFireMagic\tLightingDamageBase\tLightingMagic\tAuraSkillName\tAuraSkillLevel\tPasstSkillName\tPasstSkillLevel",
                "",
                "Boss1\t5|10\t0\t0|0\t1\t0|0\t1\t0|0\t1\t0|1000\t1\tAura1\t30\tPassive1\t60",
                "   ",
                "Boss2\t6|20\t0\t0|0\t1\t0|0\t1\t0|0\t1\t0|1000\t1\tAura2\t40\tPassive2\t70",
            };

            var rows = PcGoldBossParser.ParseLines(lines);

            Assert.AreEqual(2, rows.Count);
            Assert.AreEqual(0, rows[0].bossTemplateId);
            Assert.AreEqual(1, rows[1].bossTemplateId);
        }
    }
}
