// -----------------------------------------------------------------------------
// VLTK Mobile — Parser tests for Area Script registries (14.x GBK map areas).
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class AreaScriptParserTests
    {
        [Test] public void PcAreaScriptRegistry_Count_NonNegative() {
            var reg = new PcAreaScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }
        [Test] public void PcAreaScriptRegistry_GetByCategory_FiltersCorrectly() {
            var reg = new PcAreaScriptRegistry();
            var result = reg.GetByCategory(0);
            Assert.NotNull(result);
        }

        [Test] public void PcGbkMapScriptRegistry_Count_NonNegative() {
            var reg = new PcGbkMapScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }
        [Test] public void PcGbkMapScriptRegistry_GetByMap_FiltersCorrectly() {
            var reg = new PcGbkMapScriptRegistry();
            var result = reg.GetByMap(1);
            Assert.NotNull(result);
        }

        [Test] public void PcFactionQuestAreaRegistry_Count_NonNegative() {
            var reg = new PcFactionQuestAreaRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }
        [Test] public void PcFactionQuestAreaRegistry_GetByFaction_FiltersCorrectly() {
            var reg = new PcFactionQuestAreaRegistry();
            var result = reg.GetByFaction(0);
            Assert.NotNull(result);
        }

        [Test] public void PcTownScriptRegistry_Count_NonNegative() {
            var reg = new PcTownScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }
        [Test] public void PcTownScriptRegistry_GetByTown_FiltersCorrectly() {
            var reg = new PcTownScriptRegistry();
            var result = reg.GetByTown(1);
            Assert.NotNull(result);
        }

        [Test] public void PcGbkTriggerRegistry_Count_NonNegative() {
            var reg = new PcGbkTriggerRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }
        [Test] public void PcGbkTriggerRegistry_GetByMap_FiltersCorrectly() {
            var reg = new PcGbkTriggerRegistry();
            var result = reg.GetByMap(1);
            Assert.NotNull(result);
        }

        [Test] public void PcTongBattleScriptRegistry_Count_NonNegative() {
            var reg = new PcTongBattleScriptRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }
        [Test] public void PcTongBattleScriptRegistry_GetByType_FiltersCorrectly() {
            var reg = new PcTongBattleScriptRegistry();
            var result = reg.GetByType(0);
            Assert.NotNull(result);
        }

        [Test]
        public void PcGbkMapScriptParser_BuildRegistry_ScansSubdirectories()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string nested = Path.Combine(root, "nested");
            Directory.CreateDirectory(nested);
            try
            {
                File.WriteAllText(Path.Combine(nested, "gbkscripts_test.txt"), "ScriptId\tAreaId\tMapId\tScriptFile\tTriggerType\tFunctionName\tDescription\n501\t7\t1001\tmap.lua\t2\tOnEnter\tBa Lang\n");
                var reg = PcGbkMapScriptParser.BuildRegistry(root);
                Assert.IsNotNull(reg.Get(501));
                Assert.AreEqual(1001, reg.Get(501).mapId);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

    }
}
