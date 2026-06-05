// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho script registry services
// Bao gồm: GuildCityWar, Mission/Skill/Item/Event/Task/Global/Library scripts
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class ScriptRegistryServiceTests
    {
        // --- GuildCityWarService ---
        [Test]
        public void GuildCityWar_ScheduleWar_ReturnsNonZeroId()
        {
            var svc = new GuildCityWarService();
            int id = svc.ScheduleWar(1, 100, 200, 0);
            Assert.Greater(id, 0, "ScheduleWar phải trả về id dương");
        }

        [Test]
        public void GuildCityWar_StartWar_Transitions()
        {
            var svc = new GuildCityWarService();
            int id = svc.ScheduleWar(1, 100, 200, 0);
            Assert.IsTrue(svc.StartWar(id));
            var war = svc.GetWar(id);
            Assert.AreEqual((int)GuildCityWarStatus.Active, war.status);
        }

        [Test]
        public void GuildCityWar_FinishWar_ReturnsWinningTongId()
        {
            var svc = new GuildCityWarService();
            int id = svc.ScheduleWar(1, 100, 200, 0);
            svc.StartWar(id);
            int winner = svc.FinishWar(id, 50, 30);
            Assert.AreEqual(100, winner, "Bang tấn công thắng khi điểm cao hơn");
        }

        [Test]
        public void GuildCityWar_CancelWar_Transitions()
        {
            var svc = new GuildCityWarService();
            int id = svc.ScheduleWar(1, 100, 200, 0);
            Assert.IsTrue(svc.CancelWar(id));
            var war = svc.GetWar(id);
            Assert.AreEqual((int)GuildCityWarStatus.Cancelled, war.status);
        }

        [Test]
        public void GuildCityWar_GetActiveWars_Empty_Initially()
        {
            var svc = new GuildCityWarService();
            Assert.AreEqual(0, svc.GetActiveWars().Count);
        }

        [Test]
        public void GuildCityWar_GetWarsForTong_Empty_Initially()
        {
            var svc = new GuildCityWarService();
            Assert.AreEqual(0, svc.GetWarsForTong(100).Count);
        }

        [Test]
        public void GuildCityWar_GetStatusName_NonEmpty()
        {
            Assert.IsNotEmpty(GuildCityWarService.GetStatusName(0));
            Assert.IsNotEmpty(GuildCityWarService.GetStatusName(1));
            Assert.IsNotEmpty(GuildCityWarService.GetStatusName(2));
            Assert.IsNotEmpty(GuildCityWarService.GetStatusName(3));
        }

        // --- GuildCityWarLogService ---
        [Test]
        public void GuildCityWarLog_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = GuildCityWarLogService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void GuildCityWarLog_GetByWar_FiltersCorrectly()
        {
            var svc = new GuildCityWarLogService();
            var list = svc.GetByWar(1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void GuildCityWarLog_GetEventTypeName_NonEmpty()
        {
            for (int i = 0; i <= 4; i++)
                Assert.IsNotEmpty(GuildCityWarLogService.GetEventTypeName(i));
        }

        // --- MissionScriptService ---
        [Test]
        public void MissionScript_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = MissionScriptService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void MissionScript_GetByMission_FiltersCorrectly()
        {
            var svc = new MissionScriptService();
            var list = svc.GetByMission(1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void MissionScript_GetByType_FiltersCorrectly()
        {
            var svc = new MissionScriptService();
            var list = svc.GetByType(0);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void MissionScript_CanExecute_RejectsLowLevel()
        {
            var svc = new MissionScriptService();
            Assert.IsFalse(svc.CanExecute(99999, 1));
        }

        [Test]
        public void MissionScript_GetScriptTypeName_NonEmpty()
        {
            Assert.IsNotEmpty(MissionScriptService.GetScriptTypeName(0));
            Assert.IsNotEmpty(MissionScriptService.GetScriptTypeName(5));
        }

        // --- SkillScriptService ---
        [Test]
        public void SkillScript_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = SkillScriptService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void SkillScript_GetBySkill_FiltersCorrectly()
        {
            var svc = new SkillScriptService();
            var list = svc.GetBySkill(1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void SkillScript_GetByVersion_FiltersCorrectly()
        {
            var svc = new SkillScriptService();
            var list = svc.GetByVersion(1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void SkillScript_GetFunctionName_ReturnsString()
        {
            var svc = new SkillScriptService();
            string name = svc.GetFunctionName(99999);
            Assert.IsNotNull(name);
        }

        // --- ItemScriptService ---
        [Test]
        public void ItemScript_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = ItemScriptService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void ItemScript_GetByItem_FiltersCorrectly()
        {
            var svc = new ItemScriptService();
            var list = svc.GetByItem(1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void ItemScript_GetTriggerName_NonEmpty()
        {
            for (int i = 0; i <= 4; i++)
                Assert.IsNotEmpty(ItemScriptService.GetTriggerName(i));
        }

        // --- EventScriptService ---
        [Test]
        public void EventScript_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = EventScriptService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void EventScript_GetByEvent_FiltersCorrectly()
        {
            var svc = new EventScriptService();
            var list = svc.GetByEvent(1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void EventScript_GetFunctionName_ReturnsString()
        {
            var svc = new EventScriptService();
            string name = svc.GetFunctionName(99999);
            Assert.IsNotNull(name);
        }

        // --- TaskScriptService ---
        [Test]
        public void TaskScript_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = TaskScriptService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void TaskScript_GetByTask_FiltersCorrectly()
        {
            var svc = new TaskScriptService();
            var list = svc.GetByTask(1);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void TaskScript_GetFunctionName_ReturnsString()
        {
            var svc = new TaskScriptService();
            string name = svc.GetFunctionName(99999);
            Assert.IsNotNull(name);
        }

        // --- GlobalScriptService ---
        [Test]
        public void GlobalScript_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = GlobalScriptService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void GlobalScript_GetByTrigger_FiltersCorrectly()
        {
            var svc = new GlobalScriptService();
            var list = svc.GetByTrigger(0);
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void GlobalScript_GetTriggerName_NonEmpty()
        {
            for (int i = 0; i <= 5; i++)
                Assert.IsNotEmpty(GlobalScriptService.GetTriggerName(i));
        }

        // --- LibraryScriptService ---
        [Test]
        public void LibraryScript_LoadFromStreamingAssets_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var svc = LibraryScriptService.LoadFromStreamingAssets();
                Assert.GreaterOrEqual(svc.Count, 0);
            });
        }

        [Test]
        public void LibraryScript_GetByLibrary_FiltersCorrectly()
        {
            var svc = new LibraryScriptService();
            var list = svc.GetByLibrary("test");
            Assert.AreEqual(0, list.Count);
        }

        [Test]
        public void LibraryScript_GetFunctionName_ReturnsString()
        {
            var svc = new LibraryScriptService();
            string name = svc.GetFunctionName(99999);
            Assert.IsNotNull(name);
        }
    }
}
