using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcSkillFullParserTests
    {
        private static string SkillDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcSkill");

        [Test]
        public void ParseFile_LoadsFullPcSkillCatalog()
        {
            var path = Path.Combine(SkillDir, "skills.txt");
            var rows = PcSkillFullParser.ParseFile(path);
            Assert.GreaterOrEqual(rows.Count, 1000, "Full PC skills.txt should have thousands of skills");
        }

        [Test]
        public void ParseFile_SkillsHaveNonEmptyNames()
        {
            var path = Path.Combine(SkillDir, "skills.txt");
            var rows = PcSkillFullParser.ParseFile(path);
            int withName = 0;
            foreach (var s in rows)
            {
                if (!string.IsNullOrEmpty(s.nameRaw)) withName++;
            }
            Assert.Greater(withName, 500);
        }

        [Test]
        public void ParseFile_PreservesLevelUpScriptColumnsForRepresentativeSkills()
        {
            var rows = PcSkillFullParser.ParseFile(Path.Combine(SkillDir, "skills.txt"));
            var byId = new System.Collections.Generic.Dictionary<int, PcSkillEntry>();
            foreach (var row in rows)
                byId[row.skillId] = row;

            Assert.AreEqual(80, byId[332].reqLevel);
            Assert.AreEqual(20, byId[332].maxLevel);
            Assert.AreEqual(@"\script\skill\emei.lua", byId[332].lvlSetScript);
            Assert.AreEqual(@"\script\skill\lvlup_pudu_zhongsheng.lua", byId[332].levelUpScript);
            Assert.AreEqual(@"\script\skill\translife_4\lvlup_waigong.lua", byId[1123].levelUpScript);
        }

        [Test]
        public void LoadFromDirectory_RegistersAllSkills()
        {
            var reg = PcSkillRegistry.LoadFromDirectory(SkillDir);
            Assert.GreaterOrEqual(reg.Count, 1000);
            Assert.IsNotNull(reg.Resolve(1));
        }
    }
}
