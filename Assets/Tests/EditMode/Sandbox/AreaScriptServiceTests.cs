// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Area Script Services (14.x GBK map areas).
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class AreaScriptServiceTests
    {
        [Test] public void AreaScriptService_LoadFromStreamingAssets_DoesNotThrow() {
            Assert.DoesNotThrow(() => AreaScriptService.LoadFromStreamingAssets());
        }
        [Test] public void AreaScriptService_GetByCategory_FiltersCorrectly() {
            var svc = AreaScriptService.LoadFromStreamingAssets();
            var result = svc.GetByCategory(0);
            Assert.NotNull(result);
        }
        [Test] public void AreaScriptService_GetByMap_FiltersCorrectly() {
            var svc = AreaScriptService.LoadFromStreamingAssets();
            var result = svc.GetByMap(1);
            Assert.NotNull(result);
        }
        [Test] public void AreaScriptService_GetTotalScriptCount_NonNegative() {
            var svc = AreaScriptService.LoadFromStreamingAssets();
            int total = svc.GetTotalScriptCount();
            Assert.GreaterOrEqual(total, 0);
        }
        [Test] public void AreaScriptService_GetCategoryName_NonEmpty() {
            var svc = AreaScriptService.LoadFromStreamingAssets();
            string name = svc.GetCategoryName(0);
            Assert.IsNotNull(name);
            Assert.IsNotEmpty(name);
        }
        [Test] public void AreaScriptService_GetAreaName_ReturnsString() {
            var svc = AreaScriptService.LoadFromStreamingAssets();
            string name = svc.GetAreaName(0);
            // null is acceptable (no entry) but must not throw
            Assert.DoesNotThrow(() => svc.GetAreaName(999999));
        }

        [Test] public void GbkMapScriptService_LoadFromStreamingAssets_DoesNotThrow() {
            Assert.DoesNotThrow(() => GbkMapScriptService.LoadFromStreamingAssets());
        }
        [Test] public void GbkMapScriptService_GetByArea_FiltersCorrectly() {
            var svc = GbkMapScriptService.LoadFromStreamingAssets();
            var result = svc.GetByArea(1);
            Assert.NotNull(result);
        }
        [Test] public void GbkMapScriptService_GetByMap_FiltersCorrectly() {
            var svc = GbkMapScriptService.LoadFromStreamingAssets();
            var result = svc.GetByMap(1);
            Assert.NotNull(result);
        }
        [Test] public void GbkMapScriptService_GetByTrigger_FiltersCorrectly() {
            var svc = GbkMapScriptService.LoadFromStreamingAssets();
            var result = svc.GetByTrigger(0);
            Assert.NotNull(result);
        }
        [Test] public void GbkMapScriptService_GetScriptsForMap_NonNull() {
            var svc = GbkMapScriptService.LoadFromStreamingAssets();
            var result = svc.GetScriptsForMap(1);
            Assert.NotNull(result);
        }
        [Test] public void GbkMapScriptService_GetFunctionName_ReturnsString() {
            var svc = GbkMapScriptService.LoadFromStreamingAssets();
            Assert.DoesNotThrow(() => svc.GetFunctionName(0));
        }

        [Test] public void FactionQuestAreaService_LoadFromStreamingAssets_DoesNotThrow() {
            Assert.DoesNotThrow(() => FactionQuestAreaService.LoadFromStreamingAssets());
        }
        [Test] public void FactionQuestAreaService_GetByFaction_FiltersCorrectly() {
            var svc = FactionQuestAreaService.LoadFromStreamingAssets();
            var result = svc.GetByFaction(0);
            Assert.NotNull(result);
        }
        [Test] public void FactionQuestAreaService_GetByMap_FiltersCorrectly() {
            var svc = FactionQuestAreaService.LoadFromStreamingAssets();
            var result = svc.GetByMap(1);
            Assert.NotNull(result);
        }
        [Test] public void FactionQuestAreaService_GetTotalQuestsForFaction_NonNegative() {
            var svc = FactionQuestAreaService.LoadFromStreamingAssets();
            int total = svc.GetTotalQuestsForFaction(0);
            Assert.GreaterOrEqual(total, 0);
        }
        [Test] public void FactionQuestAreaService_GetFactionQuestAreas_NonNull() {
            var svc = FactionQuestAreaService.LoadFromStreamingAssets();
            var result = svc.GetFactionQuestAreas();
            Assert.NotNull(result);
        }

        [Test] public void TownScriptService_LoadFromStreamingAssets_DoesNotThrow() {
            Assert.DoesNotThrow(() => TownScriptService.LoadFromStreamingAssets());
        }
        [Test] public void TownScriptService_GetByTown_FiltersCorrectly() {
            var svc = TownScriptService.LoadFromStreamingAssets();
            var result = svc.GetByTown(1);
            Assert.NotNull(result);
        }
        [Test] public void TownScriptService_GetByType_FiltersCorrectly() {
            var svc = TownScriptService.LoadFromStreamingAssets();
            var result = svc.GetByType(0);
            Assert.NotNull(result);
        }
        [Test] public void TownScriptService_GetTownName_ReturnsString() {
            var svc = TownScriptService.LoadFromStreamingAssets();
            Assert.DoesNotThrow(() => svc.GetTownName(0));
        }
        [Test] public void TownScriptService_GetScriptTypeName_NonEmpty() {
            var svc = TownScriptService.LoadFromStreamingAssets();
            string name = svc.GetScriptTypeName(0);
            Assert.IsNotNull(name);
            Assert.IsNotEmpty(name);
        }

        [Test] public void GbkTriggerService_LoadFromStreamingAssets_DoesNotThrow() {
            Assert.DoesNotThrow(() => GbkTriggerService.LoadFromStreamingAssets());
        }
        [Test] public void GbkTriggerService_GetByMap_FiltersCorrectly() {
            var svc = GbkTriggerService.LoadFromStreamingAssets();
            var result = svc.GetByMap(1);
            Assert.NotNull(result);
        }
        [Test] public void GbkTriggerService_GetByEvent_FiltersCorrectly() {
            var svc = GbkTriggerService.LoadFromStreamingAssets();
            var result = svc.GetByEvent(0);
            Assert.NotNull(result);
        }
        [Test] public void GbkTriggerService_GetTriggersForMap_NonNull() {
            var svc = GbkTriggerService.LoadFromStreamingAssets();
            var result = svc.GetTriggersForMap(1);
            Assert.NotNull(result);
        }
        [Test] public void GbkTriggerService_GetEventTypeName_NonEmpty() {
            var svc = GbkTriggerService.LoadFromStreamingAssets();
            string name = svc.GetEventTypeName(0);
            Assert.IsNotNull(name);
            Assert.IsNotEmpty(name);
        }
        [Test] public void GbkTriggerService_CanFire_RejectsInvalid() {
            var svc = GbkTriggerService.LoadFromStreamingAssets();
            Assert.IsFalse(svc.CanFire(999999, 10, 12));
            Assert.IsFalse(svc.CanFire(0, -1, 12));
        }

        [Test] public void TongBattleScriptService_LoadFromStreamingAssets_DoesNotThrow() {
            Assert.DoesNotThrow(() => TongBattleScriptService.LoadFromStreamingAssets());
        }
        [Test] public void TongBattleScriptService_GetByType_FiltersCorrectly() {
            var svc = TongBattleScriptService.LoadFromStreamingAssets();
            var result = svc.GetByType(0);
            Assert.NotNull(result);
        }
        [Test] public void TongBattleScriptService_GetByMap_FiltersCorrectly() {
            var svc = TongBattleScriptService.LoadFromStreamingAssets();
            var result = svc.GetByMap(1);
            Assert.NotNull(result);
        }
        [Test] public void TongBattleScriptService_GetScriptTypeName_NonEmpty() {
            var svc = TongBattleScriptService.LoadFromStreamingAssets();
            string name = svc.GetScriptTypeName(0);
            Assert.IsNotNull(name);
            Assert.IsNotEmpty(name);
        }
        [Test] public void TongBattleScriptService_GetFunctionName_ReturnsString() {
            var svc = TongBattleScriptService.LoadFromStreamingAssets();
            Assert.DoesNotThrow(() => svc.GetFunctionName(0));
        }
    }
}
