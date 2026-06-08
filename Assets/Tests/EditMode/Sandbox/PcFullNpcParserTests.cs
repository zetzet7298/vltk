using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcFullNpcParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc/npcs_sample.txt");

        private static string FullPcPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc/npcs.txt");

        [Test]
        public void ParseFile_LoadsTenSampleRows()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            Assert.AreEqual(10, rows.Count, "Expected exactly 10 NPC rows from the sample");
        }

        [Test]
        public void ParseFile_TemplateIdsAreSequential()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            for (int i = 0; i < rows.Count; i++)
            {
                Assert.AreEqual(i, rows[i].templateId, $"Row {i} should have templateId={i}");
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

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.nameRaw), $"templateId={r.templateId} nameRaw empty");
                Assert.IsFalse(string.IsNullOrEmpty(r.nameNormalized), $"templateId={r.templateId} nameNormalized empty");
            }
        }

        [Test]
        public void ParseFile_DecodesLegacyTcvn3VietnameseNames()
        {
            if (!File.Exists(FullPcPath))
            {
                Assert.Inconclusive($"Full PC npcs.txt not found at {FullPcPath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(FullPcPath);

            Assert.GreaterOrEqual(rows.Count, 30);
            Assert.AreEqual("Đông Bắc hổ", rows[0].nameNormalized.Trim());
            Assert.AreEqual("Hoa Nam hổ", rows[1].nameNormalized.Trim());
            Assert.AreEqual("Nhím", rows[12].nameNormalized.Trim());
            Assert.IsFalse(rows[0].nameNormalized.Contains("¶"));
        }

        [Test]
        public void ParseFile_KindValuesInRange()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.GreaterOrEqual(r.kind, 0);
                Assert.LessOrEqual(r.kind, 5, $"templateId={r.templateId} kind={r.kind} out of range");
            }
        }

        [Test]
        public void ParseFile_SeriesValuesInRange()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.GreaterOrEqual(r.series, 0);
                Assert.LessOrEqual(r.series, 4, $"templateId={r.templateId} series={r.series} out of range");
            }
        }

        [Test]
        public void ParseFile_WalkAndRunSpeedPositive()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.walkSpeed, 0, $"templateId={r.templateId} walkSpeed must be > 0");
                Assert.Greater(r.runSpeed, 0, $"templateId={r.templateId} runSpeed must be > 0");
            }
        }

        [Test]
        public void ParseFile_VisionRadiusPositive()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.visionRadius, 0, $"templateId={r.templateId} visionRadius must be > 0");
            }
        }

        [Test]
        public void ParseFile_NamesHaveNoReplacementChars()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.IsFalse(r.nameNormalized.Contains('\ufffd'),
                    $"templateId={r.templateId} nameNormalized contains replacement char: {r.nameNormalized}");
                Assert.IsFalse(r.nameNormalized.Contains('?'),
                    $"templateId={r.templateId} nameNormalized contains '?': {r.nameNormalized}");
            }
        }


        [Test]
        public void ParseFile_DecodesLegacyTcvn3NamesFromFullReference()
        {
            string fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/StreamingAssets/Reference/PcNpc/npcs.txt");
            if (!File.Exists(fullPath))
            {
                Assert.Inconclusive($"Full NPC file not found at {fullPath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(fullPath);

            Assert.Greater(rows.Count, 601, "Full NPC reference should contain PC template rows");
            Assert.AreEqual("Đông Bắc hổ", rows[0].nameRaw.Trim());
            Assert.AreEqual("Hươu đốm", rows[42].nameRaw.Trim());
            Assert.AreEqual("Heo trắng", rows[43].nameRaw.Trim());
            Assert.AreEqual("Ngọc Hoành Tử Lâm Du Quan", rows[588].nameRaw.Trim());
            Assert.AreEqual("Giới Luật Viện đầu tọa Trường Bạch nam", rows[601].nameRaw.Trim());
            Assert.IsFalse(rows[601].nameRaw.Contains('�'));
            Assert.IsFalse(rows[601].nameRaw.Contains('?'));
        }

        [Test]
        public void ParseFile_PreservesGb2312ChineseNamesWhenNpcRowIsNotVietnamese()
        {
            string fullPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "Assets/StreamingAssets/Reference/PcNpc/npcs.txt");
            if (!File.Exists(fullPath))
            {
                Assert.Inconclusive($"Full NPC file not found at {fullPath}");
                return;
            }

            var rows = PcFullNpcParser.ParseFile(fullPath);

            Assert.Greater(rows.Count, 1341, "Full NPC reference should contain mixed-encoding tail rows");
            Assert.AreEqual("宋军运粮士兵", rows[1341].nameRaw.Trim());
        }

        [Test]
        public void ParseRow_HandlesShortRow()
        {
            var cols = new string[5];
            cols[0] = "ShortRow";
            var row = PcFullNpcParser.ParseRow(cols);
            Assert.AreEqual("ShortRow", row.nameRaw);
            Assert.AreEqual(0, row.kind);
            Assert.AreEqual(0, row.series);
            Assert.AreEqual(0, row.walkSpeed);
        }

        [Test]
        public void ParseRow_DashOneFieldsClearToEmptyString()
        {
            var cols = new string[103];
            for (int i = 0; i < cols.Length; i++) cols[i] = string.Empty;
            cols[0] = "DashOne";
            cols[31] = "-1";
            cols[32] = "-1";
            cols[87] = "-1";
            cols[58] = "6";
            cols[59] = "6";
            cols[62] = "400";
            cols[1] = "0";
            cols[3] = "0";
            var row = PcFullNpcParser.ParseRow(cols);
            Assert.AreEqual(string.Empty, row.actionScript);
            Assert.AreEqual(string.Empty, row.levelScript);
            Assert.AreEqual(string.Empty, row.dropRateFile);
            Assert.AreEqual(6, row.walkSpeed);
        }

        [Test]
        public void ParseLines_HandlesEmptyLines()
        {
            var lines = new[]
            {
                "Name\tKind\tCamp\tSeries\tTreasure\tHeadImage\tClientOnly\tCorpseIdx\tRedLum\tGreenLum\tBlueLum\tNpcResType\tArmorType\tHelmType\tWeaponType\tHorseType\tRideHorse\tStandFrame\tStandFrame1\tDeathFrame\tWalkFrame\tRunFrame\tHurtFrame\tSkill1\tLevel1\tSkill2\tLevel2\tSkill3\tLevel3\tSkill4\tLevel4\tActionScript\tLevelScript\tExpParam\tExpParam1\tExpParam2\tExpParam3\tLifeParam\tLifeParam1\tLifeParam2\tLifeParam3\tLifeReplenish\tARParam\tARParam1\tARParam2\tARParam3\tDefenseParam\tDefenseParam1\tDefenseParam2\tDefenseParam3\tMinDamageParam\tMinDamageParam1\tMinDamageParam2\tMinDamageParam3\tMaxDamageParam\tMaxDamageParam1\tMaxDamageParam2\tMaxDamageParam3\tWalkSpeed\tRunSpeed\tAttackSpeed\tCastSpeed\tVisionRadius\tHitRecover\tActiveRadius\tAIMode\tAIParam1\tAIParam2\tAIParam3\tAIParam4\tAIParam5\tAIParam6\tAIParam7\tAIParam8\tAIParam9\tFireResist\tColdResist\tLightResist\tPoisonResist\tPhysicsResist\tFireResistMax\tColdResistMax\tLightResistMax\tPoisonResistMax\tPhysicsResistMax\tReviveFrame\tStature\tDropRateFile",
                "",
                "MockNPC\t0\t0\t0\t0\t\t\t0\t\t\t\tani001\t0\t0\t0\t0\t0\t10\t10\t20\t5\t10\t5\t\t0\t\t0\t\t0\t\t0\t\t\t\t100\t0\t10\t50\t100\t0\t20\t50\t0|0.05\t100\t0.07\t2.5\t10\t100\t0.1\t2.5\t10\t100\t0\t0\t0\t100\t0\t0\t0\t5\t5\t10\t10\t300\t40\t500\t0\t0\t0\t0\t0\t0\t5\t0\t0\t5\t50\t50\t50\t50\t50\t50\t100\t",
                "   ",
                "MockNPC2\t0\t0\t0\t0\t\t\t0\t\t\t\tani002\t0\t0\t0\t0\t0\t10\t10\t20\t5\t10\t5\t\t0\t\t0\t\t0\t\t0\t\t\t\t100\t0\t10\t50\t100\t0\t20\t50\t0|0.05\t100\t0.07\t2.5\t10\t100\t0.1\t2.5\t10\t100\t0\t0\t0\t100\t0\t0\t0\t5\t5\t10\t10\t300\t40\t500\t0\t0\t0\t0\t0\t0\t5\t0\t0\t5\t50\t50\t50\t50\t50\t50\t100\t",
            };

            var rows = PcFullNpcParser.ParseLines(lines);

            Assert.AreEqual(2, rows.Count, "Empty lines should be skipped");
            Assert.AreEqual(0, rows[0].templateId);
            Assert.AreEqual(1, rows[1].templateId);
        }
    }
}
