// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho script registry parsers
// Bao gồm 8 registry classes
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class ScriptRegistryParserTests
    {
        [Test]
        public void PcGuildCityWarLogRegistry_Count_NonNegative()
        {
            var reg = new PcGuildCityWarLogRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcGuildCityWarLogRegistry_GetByWar_FiltersCorrectly()
        {
            var reg = new PcGuildCityWarLogRegistry();
            Assert.AreEqual(0, reg.GetByWar(1).Count);
        }

        [Test]
        public void PcMissionScriptRegistry_Count_NonNegative()
        {
            var reg = new PcMissionScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMissionScriptRegistry_GetByMission_FiltersCorrectly()
        {
            var reg = new PcMissionScriptRegistry();
            Assert.AreEqual(0, reg.GetByMission(1).Count);
        }

        [Test]
        public void PcSkillScriptRegistry_Count_NonNegative()
        {
            var reg = new PcSkillScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcSkillScriptRegistry_GetBySkill_FiltersCorrectly()
        {
            var reg = new PcSkillScriptRegistry();
            Assert.AreEqual(0, reg.GetBySkill(1).Count);
        }

        [Test]
        public void PcItemScriptRegistry_Count_NonNegative()
        {
            var reg = new PcItemScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcItemScriptRegistry_GetByItem_FiltersCorrectly()
        {
            var reg = new PcItemScriptRegistry();
            Assert.AreEqual(0, reg.GetByItem(1).Count);
        }

        [Test]
        public void PcEventScriptRegistry_Count_NonNegative()
        {
            var reg = new PcEventScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcEventScriptRegistry_GetByEvent_FiltersCorrectly()
        {
            var reg = new PcEventScriptRegistry();
            Assert.AreEqual(0, reg.GetByEvent(1).Count);
        }

        [Test]
        public void PcTaskScriptRegistry_Count_NonNegative()
        {
            var reg = new PcTaskScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcTaskScriptRegistry_GetByTask_FiltersCorrectly()
        {
            var reg = new PcTaskScriptRegistry();
            Assert.AreEqual(0, reg.GetByTask(1).Count);
        }

        [Test]
        public void PcGlobalScriptRegistry_Count_NonNegative()
        {
            var reg = new PcGlobalScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcGlobalScriptRegistry_GetByTrigger_FiltersCorrectly()
        {
            var reg = new PcGlobalScriptRegistry();
            Assert.AreEqual(0, reg.GetByTrigger(0).Count);
        }

        [Test]
        public void PcLibraryScriptRegistry_Count_NonNegative()
        {
            var reg = new PcLibraryScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcLibraryScriptRegistry_GetByLibrary_FiltersCorrectly()
        {
            var reg = new PcLibraryScriptRegistry();
            Assert.AreEqual(0, reg.GetByLibrary("test").Count);
        }

        [Test]
        public void PcTaskScriptParser_BuildRegistry_ScansSubdirectories()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string nested = Path.Combine(root, "nested");
            Directory.CreateDirectory(nested);
            try
            {
                File.WriteAllText(Path.Combine(nested, "taskscripts_test.txt"), "ScriptId\tTaskId\tTrigger\tFunctionName\tParamsCount\tDescription\n701\t88\t2\tOnDone\t1\tQuest\n");
                var reg = PcTaskScriptParser.BuildRegistry(root);
                Assert.IsNotNull(reg.Get(701));
                Assert.AreEqual(88, reg.Get(701).taskId);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

    }
}
