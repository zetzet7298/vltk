using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcTranslifeSkillSourceParserTests
    {
        private static string PcSkillDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcSkill");
        private static string TranslifeSkillPath => Path.Combine(PcSkillDir, "translifeskill.txt");
        private static string TranslifeLevelTablePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcTask/translife.txt");

        [Test]
        public void ParseFile_LoadsNineRowsFromPcSkillsTxtTranslife4thSubset()
        {
            var rows = PcTranslifeSkillSourceParser.ParseFile(TranslifeSkillPath);
            var header = PcItemCommon.ReadServerLines(TranslifeSkillPath)[0].Split('\t');

            Assert.AreEqual(9, rows.Count, "PC skills.txt has 9 translife4th rows; do not reuse PcTask/translife.txt level rows.");
            Assert.AreEqual(PcTranslifeSkillSourceParser.PcSkillsColumnCount, header.Length);
            Assert.AreEqual(new[] { 1123, 1124, 1125, 1126, 1127, 1128, 1129, 1130, 1171 },
                rows.Select(r => r.skillId).ToArray());
            Assert.IsTrue(rows.All(r => r.sourceColumnCount == PcTranslifeSkillSourceParser.PcSkillsColumnCount));
        }

        [Test]
        public void ParseFile_PreservesRepresentativePcSkillRowsAndSettings()
        {
            var registry = PcTranslifeSkillSourceParser.BuildRegistry(PcSkillDir);
            var vitality = registry.Get(1123);
            var life = registry.Get(1127);
            var enhance = registry.Get(1171);

            Assert.IsNotNull(vitality);
            Assert.AreEqual(503, vitality.attrib);
            Assert.AreEqual("vitality_v", vitality.levelSettings[0].settingName);
            Assert.AreEqual("vn_translife4th", vitality.levelSettings[0].dataKey);
            Assert.AreEqual("\\script\\skill\\special\\translife4th.lua", vitality.levelSetScript);

            Assert.IsNotNull(life);
            Assert.AreEqual(2, life.levelSettings.Length);
            Assert.AreEqual("lifemax_v", life.levelSettings[0].settingName);
            Assert.AreEqual("lifemax_yan_v", life.levelSettings[1].settingName);
            Assert.IsTrue(life.isExpSkill);
            Assert.AreEqual(20, life.maxLevel);

            Assert.IsNotNull(enhance);
            Assert.AreEqual("skill_enhance", enhance.levelSettings[0].settingName);
            Assert.AreEqual("zhanyiqianqiu", enhance.levelSettings[0].dataKey);
        }

        [Test]
        public void Parser_DoesNotConflateTranslifeLevelBonusTableWithSkillSource()
        {
            Assert.AreEqual(0, PcTranslifeSkillSourceParser.ParseFile(TranslifeLevelTablePath).Count);
            Assert.AreEqual(41, PcTranslifeLevelParser.ParseFile(TranslifeLevelTablePath).Count);
        }

        [Test]
        public void ParserMissingAndInvalidRows_ReturnEmptyOrSkipBadRows()
        {
            Assert.AreEqual(0, PcTranslifeSkillSourceParser.ParseFile("/tmp/not-a-real-translifeskill-file.txt").Count);
            Assert.AreEqual(0, PcTranslifeSkillSourceParser.BuildRegistry("/tmp/not-a-real-pcskill-dir").Count);

            var source = PcItemCommon.ReadServerLines(TranslifeSkillPath);
            var wrongScript = source[1].Replace(
                "\\script\\skill\\special\\translife4th.lua",
                "\\script\\skill\\emei.lua");
            var rows = PcTranslifeSkillSourceParser.ParseLines(new List<string> { source[0], "bad", wrongScript, source[1] });

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(1123, rows[0].skillId);
        }

        [Test]
        public void SourceServiceLoadsDefaultPcSkillTranslifeSkillCatalog()
        {
            var service = TranslifeSkillSourceService.LoadFromStreamingAssets();

            Assert.AreEqual(9, service.Count);
            Assert.AreEqual(PcTranslifeSkillSourceParser.PcSourceRelativePath, TranslifeSkillSourceService.PcSourceRelativePath);
            Assert.IsNotNull(service.Get(1130));
            Assert.AreEqual("adddefense_v", service.Get(1130).levelSettings[0].settingName);
        }
    }
}
