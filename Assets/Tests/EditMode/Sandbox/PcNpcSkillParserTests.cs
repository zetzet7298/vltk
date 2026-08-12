using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcNpcSkillParserTests
    {
        private static string NpcSkillPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcSkill/npcskills.txt");

        private static string NpcSkillDir => Path.GetDirectoryName(NpcSkillPath);

        [Test]
        public void ParseFile_ReadsPcSkills1NpcBossSubset_NotOld43Claim()
        {
            var rows = PcNpcSkillParser.ParseFile(NpcSkillPath);

            Assert.AreEqual(158, rows.Count, "PC Server settings/skills1.txt NPC/Boss subset has 158 rows, not 43");
            Assert.AreNotEqual(43, rows.Count);
            Assert.IsTrue(rows.TrueForAll(r => r.isNpcScript || r.isBossName));
        }

        [Test]
        public void BuildRegistry_ProvesNpcScriptAndBossNameCounts()
        {
            var registry = PcNpcSkillParser.BuildRegistry(NpcSkillDir);

            Assert.AreEqual(158, registry.Count);
            Assert.AreEqual(145, registry.NpcScriptCount);
            Assert.AreEqual(21, registry.BossNameCount);
            Assert.AreEqual(13, registry.BossNameOnlyCount);
        }

        [Test]
        public void ParseFile_PreservesPcSkills1HeaderAndRepresentativeNpcSkill()
        {
            var header = PcItemCommon.ReadServerLines(NpcSkillPath)[0].Split('\t');
            var registry = PcNpcSkillParser.BuildRegistry(NpcSkillDir);
            var skill = registry.Get(233);

            Assert.AreEqual(PcNpcSkillParser.PcSkills1ColumnCount, header.Length);
            Assert.IsNotNull(skill);
            Assert.IsTrue(skill.nameRaw.Contains("npc"));
            Assert.IsTrue(skill.isNpcScript);
            Assert.IsFalse(skill.isBossName);
            Assert.AreEqual(54, skill.childSkillId);
            Assert.AreEqual(1, skill.missilesForm);
            Assert.AreEqual(270, skill.attackRadius);
            Assert.IsTrue(skill.levelSetScript.StartsWith("\\script\\skill\\npc"));
        }

        [Test]
        public void ParseFile_ProvesBossNameRowsOutsideNpcScriptFolder()
        {
            var registry = PcNpcSkillParser.BuildRegistry(NpcSkillDir);
            var skill = registry.Get(1584);

            Assert.IsNotNull(skill);
            Assert.IsTrue(skill.isBossName);
            Assert.IsFalse(skill.isNpcScript);
            Assert.AreEqual("\\script\\skill\\biggoldboss.lua", skill.levelSetScript);
            Assert.AreEqual(432, skill.childSkillId);
            Assert.AreEqual(64, skill.childSkillNum);
            Assert.AreEqual(700, skill.attackRadius);
        }

        [Test]
        public void ParseFile_ProvesLateNpcScriptRowsFromSkills1()
        {
            var registry = PcNpcSkillParser.BuildRegistry(NpcSkillDir);
            var skill = registry.Get(1617);

            Assert.IsNotNull(skill);
            Assert.IsTrue(skill.isNpcScript);
            Assert.IsFalse(skill.isBossName);
            Assert.AreEqual("\\script\\skill\\npc\\newmianyi_npc.lua", skill.levelSetScript);
            Assert.AreEqual(3, skill.skillStyle);
            Assert.AreEqual(7, skill.missilesForm);
            Assert.AreEqual(60, skill.maxLevel);
        }

        [Test]
        public void ParserMissingAndInvalidRows_ReturnEmptyOrSkipBadRows()
        {
            Assert.AreEqual(0, PcNpcSkillParser.ParseFile("/tmp/not-a-real-npcskills-file.txt").Count);
            Assert.AreEqual(0, PcNpcSkillParser.BuildRegistry("/tmp/not-a-real-pcskill-dir").Count);

            var source = PcItemCommon.ReadServerLines(NpcSkillPath);
            var lines = new List<string> { source[0], "bad", "Invalid\t\t0", source[1] };
            var rows = PcNpcSkillParser.ParseLines(lines);

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual(233, rows[0].skillId);
        }

        [Test]
        public void CatalogServiceLoadsDefaultPcSkillNpcskills()
        {
            var service = NpcSkillCatalogService.LoadFromStreamingAssets();

            Assert.AreEqual(158, service.Count);
            Assert.AreEqual(145, service.NpcScriptCount);
            Assert.IsNotNull(service.Get(753));
            Assert.IsTrue(service.Get(753).isBossName);
        }
    }
}
