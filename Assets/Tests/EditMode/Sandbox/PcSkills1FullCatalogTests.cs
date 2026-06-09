using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcSkills1FullCatalogTests
    {
        private static string ReferenceDir => Path.Combine(
            Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
        private static string SkillDir => Path.Combine(ReferenceDir, "PcSkill");
        private static string FullPath => Path.Combine(SkillDir, "skills1_full.txt");

        [Test]
        public void FullCatalog_ProvesExactPcServerSkills1Shape()
        {
            var service = Skills1FullCatalogService.LoadFromDirectory(SkillDir);
            var stats = service.Stats;

            Assert.AreEqual("9b8dc327e143f62328e01d8941ad411eca40c6d85c4ca60d486172739f9e6932", Sha256Hex(FullPath));
            Assert.AreEqual(1714, stats.sourceLineCount);
            Assert.AreEqual(1713, stats.nonEmptyLineCount);
            Assert.AreEqual(115, stats.headerColumnCount);
            Assert.AreEqual(1712, stats.dataRowCount);
            Assert.AreEqual(1712, stats.rowsWithExpectedColumnCount);
            Assert.AreEqual(1711, stats.uniqueSkillIdCount);
            Assert.AreEqual(1, stats.duplicateSkillIdCount);
            Assert.IsNotNull(service.Resolve(1));
            Assert.IsNotNull(service.Resolve(1874));
        }

        [Test]
        public void FullCatalog_ClarifiesExistingSpecialAndNpcSubsets()
        {
            var service = Skills1FullCatalogService.LoadFromDirectory(SkillDir);
            var stats = service.Stats;

            Assert.AreEqual(576, stats.specialSkillScriptRows);
            Assert.AreEqual(576, PcSpecialSkillParser.ParseFile(Path.Combine(SkillDir, "specialskills.txt")).Count);
            Assert.AreEqual(145, stats.npcSkillScriptRows);
            Assert.AreEqual(21, stats.bossNameRows);
            Assert.AreEqual(158, stats.npcSubsetUnionRows);
            Assert.AreEqual(158, PcNpcSkillParser.ParseFile(Path.Combine(SkillDir, "npcskills.txt")).Count);
        }

        [Test]
        public void FullCatalog_ClarifiesModSkillsIsSeparateTableNotRuntimeClaim()
        {
            var service = Skills1FullCatalogService.LoadFromDirectory(SkillDir);
            Assert.AreEqual(498, service.Stats.skillIdAtLeast1216Rows);

            string modSkills = Path.Combine(ReferenceDir, "ModSkills.txt");
            var modLines = PcMapListParser.ReadLines(modSkills);
            int nonEmpty = 0;
            string header = null;
            foreach (var line in modLines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                nonEmpty++;
                if (header == null) header = line;
            }

            Assert.AreEqual(114, header.Split('\t').Length);
            Assert.AreEqual(1555, nonEmpty);
            Assert.AreEqual(1554, PcModSkillParser.ParseFile(modSkills, 0).Count);
        }

        private static string Sha256Hex(string path)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(File.ReadAllBytes(path));
                return System.BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
