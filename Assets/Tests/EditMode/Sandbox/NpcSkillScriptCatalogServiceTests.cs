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
            Assert.AreEqual("\\script\\skill\\npc\\²éẹụẩỗẹê.lua", fact.ScriptPath);
            Assert.AreEqual("script/skill/npc/²éẹụẩỗẹê.lua", fact.NormalizedRelativePath);
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
            CollectionAssert.DoesNotContain(missingPaths, "\\script\\skill\\npc\\²éẹụẩỗẹê.lua");
            CollectionAssert.DoesNotContain(missingPaths, "\\script\\skill\\npc\\randomtask_npc.lua");
        }

        // CTS-01: assert a known Vietnamese NPC skill name from PcSkill/npcskills.txt
        // is loaded by PcNpcSkillParser (ReadLinesTcvn3) — which is the data source
        // for NpcSkillScriptCatalogService — without mojibake. Skill #233 =
        // "Tàn Dương Như Huyết npc" — Vietnamese diacritics must round-trip
        // cleanly and contain no U+FFFD replacement char, and the catalog must
        // still index that skill id.
        [Test]
        public void VietnameseNpcSkillName_TanDuongNhuHuyet_ResolvesCatalog()
        {
            var registry = PcNpcSkillParser.BuildRegistry(NpcSkillDir);
            Assert.IsNotNull(registry);
            var skill233 = registry.Get(233);
            Assert.IsNotNull(skill233, "PcNpcSkillRegistry must contain skill id=233 from npcskills.txt");

            string name = skill233.nameRaw ?? string.Empty;
            Assert.IsFalse(name.Contains('\uFFFD'),
                "nameRaw must not contain U+FFFD (mojibake); got '" + name + "'");
            Assert.AreEqual("Tàn Dương Như Huyết npc", name.Trim(),
                "Skill #233 name must match the expected Vietnamese 'Tàn Dương Như Huyết npc'");

            // And the catalog must index that same skill id
            var service = NpcSkillScriptCatalogService.FromCatalog(new NpcSkillCatalogService(registry));
            var fact = service.GetBySkillId(233);
            Assert.IsNotNull(fact, "NpcSkillScriptCatalogService must index skill id=233");
            Assert.IsTrue(fact.IsNpcScriptPath, "Skill #233 should be tagged as an NPC script path");
            Assert.AreEqual(1, fact.ReferencingSkillCount);
        }
    }
}
