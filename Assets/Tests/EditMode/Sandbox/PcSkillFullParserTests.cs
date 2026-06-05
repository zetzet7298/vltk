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
        public void LoadFromDirectory_RegistersAllSkills()
        {
            var reg = PcSkillRegistry.LoadFromDirectory(SkillDir);
            Assert.GreaterOrEqual(reg.Count, 1000);
            Assert.IsNotNull(reg.Resolve(1));
        }
    }
}
