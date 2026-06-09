using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class NpcSkillScriptCatalogServiceTests
    {
        private static string NpcSkillDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcSkill");

        private static NpcSkillScriptCatalogService CreateService()
        {
            var registry = PcNpcSkillParser.BuildRegistry(NpcSkillDir);
            return NpcSkillScriptCatalogService.FromCatalog(new NpcSkillCatalogService(registry));
        }

        [Test]
        public void BuildIndex_GroupsCurrentNpcBossCatalogScriptPaths()
        {
            var service = CreateService();

            Assert.AreEqual(158, service.SourceSkillCount);
            Assert.AreEqual(145, service.NpcScriptRowCount);
            Assert.AreEqual(49, service.UniqueScriptCount);
            Assert.AreEqual(45, service.UniqueNpcScriptPathCount);
            Assert.AreEqual(1, service.UniqueBossSpecialScriptPathCount);
            Assert.AreEqual(42, service.ExistingScriptPathCount);
            Assert.AreEqual(7, service.MissingScriptPathCount);
        }

        [Test]
        public void RepresentativeNpcSkill233_PathExistsUnderPcServerScriptRoot()
        {
            var service = CreateService();
            var fact = service.GetBySkillId(233);

            Assert.IsNotNull(fact);
            Assert.AreEqual("\\script\\skill\\npc\\残阳如血.lua", fact.ScriptPath);
            Assert.AreEqual("script/skill/npc/残阳如血.lua", fact.NormalizedRelativePath);
            Assert.IsTrue(fact.IsNpcScriptPath);
            Assert.IsTrue(fact.ExistsUnderPcServerRoot);
            Assert.AreEqual(1, fact.ReferencingSkillCount);
        }

        [Test]
        public void BossNameNpcScript753_PathExistsAndRemainsIndexOnly()
        {
            var service = CreateService();
            var fact = service.GetBySkillId(753);

            Assert.IsNotNull(fact);
            Assert.AreEqual("\\script\\skill\\npc\\randomtask_npc.lua", fact.ScriptPath);
            Assert.IsTrue(fact.ExistsUnderPcServerRoot);
            Assert.GreaterOrEqual(fact.BossNameRowCount, 1);
            Assert.IsFalse(service.ExecutesScripts);
            Assert.IsTrue(NpcSkillScriptCatalogService.NoExecutionClaim.Contains("does not execute"));
        }

        [Test]
        public void BossSpecialReference_IsIndexedButMissingFromScopedPcSource()
        {
            var service = CreateService();
            var fact = service.GetBySkillId(1604);

            Assert.IsNotNull(fact);
            Assert.AreEqual("\\script\\skill\\special\\boss_libaiskill.lua", fact.ScriptPath);
            Assert.IsTrue(fact.IsSpecialScriptPath);
            Assert.IsTrue(fact.IsBossSpecialScriptPath);
            Assert.IsFalse(fact.ExistsUnderPcServerRoot,
                "vl_update_27 server script root does not contain the referenced boss_libaiskill.lua; do not substitute boss_specialskill.lua.");
            Assert.IsTrue(service.MissingScripts.Any(m => m.ScriptPath == fact.ScriptPath));
        }

        [Test]
        public void MissingPathList_ExposesOnlyUnprovenReferences()
        {
            var service = CreateService();
            var missingPaths = service.MissingScripts.Select(m => m.ScriptPath).ToArray();

            CollectionAssert.Contains(missingPaths, "\\script\\skill\\npc\\biaoche_mianyi.lua");
            CollectionAssert.Contains(missingPaths, "\\script\\skill\\biggoldboss.lua");
            CollectionAssert.Contains(missingPaths, "\\script\\skill\\special\\boss_libaiskill.lua");
            CollectionAssert.DoesNotContain(missingPaths, "\\script\\skill\\npc\\残阳如血.lua");
            CollectionAssert.DoesNotContain(missingPaths, "\\script\\skill\\npc\\randomtask_npc.lua");
        }
    }
}
