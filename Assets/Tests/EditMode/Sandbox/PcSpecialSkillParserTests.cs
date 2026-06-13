using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcSpecialSkillParserTests
    {
        private static string SpecialSkillPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcSkill/specialskills.txt");

        private static string SpecialSkillDir => Path.GetDirectoryName(SpecialSkillPath);

        [Test]
        public void ParseFile_ReadsPcSkills1SpecialScriptSubset_NotOld58Claim()
        {
            var rows = PcSpecialSkillParser.ParseFile(SpecialSkillPath);

            Assert.AreEqual(576, rows.Count, "PC Server settings/skills1.txt special-script subset has 576 rows, not 58");
            Assert.AreNotEqual(58, rows.Count);
            Assert.IsTrue(rows.TrueForAll(r => r.isSpecialSkillScript));
            Assert.IsTrue(rows.TrueForAll(r => r.levelSetScript.StartsWith(PcSpecialSkillParser.SpecialScriptPrefix)));
        }

        [Test]
        public void ParseFile_PreservesPcSkills1HeaderAndSourceShape()
        {
            var header = PcItemCommon.ReadServerLines(SpecialSkillPath)[0].Split('\t');
            var registry = PcSpecialSkillParser.BuildRegistry(SpecialSkillDir);

            Assert.AreEqual(PcSpecialSkillParser.PcSkills1ColumnCount, header.Length);
            Assert.AreEqual(576, registry.Count);
            Assert.AreEqual(575, registry.UniqueSkillIdCount, "PC skills1.txt repeats SkillId 521 twice in the special-script subset");
            Assert.AreEqual(84, registry.UniqueScriptCount);
        }

        [Test]
        public void ParseFile_ProvesRepresentativeBaseAndBossSpecialRows()
        {
            var registry = PcSpecialSkillParser.BuildRegistry(SpecialSkillDir);
            var baseAttack = registry.Get(1);
            var bossShield = registry.Get(1207);

            Assert.IsNotNull(baseAttack);
            Assert.AreEqual(0, baseAttack.skillStyle);
            Assert.AreEqual("\\script\\skill\\special\\³Ô±ứẻùÀớạƠằữ.lua", baseAttack.levelSetScript);
            Assert.AreEqual("physicsenhance_p", baseAttack.levelSetting1);

            Assert.IsNotNull(bossShield);
            Assert.AreEqual(3, bossShield.skillStyle);
            Assert.AreEqual("\\script\\skill\\special\\boss_specialskill.lua", bossShield.levelSetScript);
            Assert.AreEqual("dynamicmagicshield_v", bossShield.levelSetting1);
            Assert.AreEqual("xuantianwuji", bossShield.levelData1);
        }

        [Test]
        public void ParseFile_PreservesDuplicatePcSkillIdRows()
        {
            var rows = PcSpecialSkillParser.ParseFile(SpecialSkillPath);
            var skill521Rows = new List<PcSpecialSkillEntry>();
            foreach (var row in rows)
            {
                if (row.skillId == 521) skill521Rows.Add(row);
            }

            Assert.AreEqual(2, skill521Rows.Count);
            Assert.AreEqual("Giảm Băng hoàn (kỹ năng) ", skill521Rows[0].nameRaw);
            Assert.AreEqual("kháng tấn công khí mệnh (kỹ năng) ", skill521Rows[1].nameRaw);
        }

        [Test]
        public void RegistryLookupByScript_ProvesPcSpecialScriptBuckets()
        {
            var registry = PcSpecialSkillParser.BuildRegistry(SpecialSkillDir);

            Assert.AreEqual(131, registry.GetByScript("\\script\\skill\\special\\platina.lua").Count);
            Assert.AreEqual(101, registry.GetByScript("\\script\\skill\\special\\skillstate.lua").Count);
            Assert.AreEqual(27, registry.GetByScript("\\script\\skill\\special\\boss_superskill.lua").Count);
            Assert.AreEqual(0, registry.GetByScript("\\script\\skill\\npc\\not_special.lua").Count);
        }

        [Test]
        public void ParserMissingAndInvalidRows_ReturnEmptyOrSkipBadRows()
        {
            Assert.AreEqual(0, PcSpecialSkillParser.ParseFile("/tmp/not-a-real-specialskills-file.txt").Count);
            Assert.AreEqual(0, PcSpecialSkillParser.BuildRegistry("/tmp/not-a-real-pcskill-dir").Count);

            var source = PcItemCommon.ReadServerLines(SpecialSkillPath);
            var lines = new List<string> { source[0], "bad", "Invalid\t\t0", source[1] };
            var rows = PcSpecialSkillParser.ParseLines(lines);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(1, rows[0].skillId);
        }

        [Test]
        public void CatalogServiceLoadsDefaultPcSkillSpecialskills()
        {
            var service = SpecialSkillCatalogService.LoadFromStreamingAssets();

            Assert.AreEqual(576, service.Count);
            Assert.AreEqual(575, service.UniqueSkillIdCount);
            Assert.AreEqual(84, service.UniqueScriptCount);
            Assert.IsNotNull(service.Get(1872));
            Assert.AreEqual("\\script\\skill\\special\\xiancaolu.lua", service.Get(1872).levelSetScript);
        }
    }
}
