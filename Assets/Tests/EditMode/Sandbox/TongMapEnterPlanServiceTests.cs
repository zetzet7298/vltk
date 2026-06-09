using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.EditMode.Sandbox
{
    public class TongMapEnterPlanServiceTests
    {
        [Test]
        public void CatalogFacts_LoadFromCurrentFactionMapService()
        {
            var service = TongMapEnterPlanService.LoadFromStreamingAssets();

            Assert.AreEqual(33, service.CatalogRowCount);
            Assert.AreEqual(11, service.CityMapCount);
            Assert.AreEqual(7, service.DynamicTemplateCount);
            Assert.AreEqual(7, service.CityAltarNpcMapCount);
        }

        [Test]
        public void Map591_UsesPcBorderEnterPositionAndNewWorldCommand()
        {
            var plan = TongMapEnterPlanService.LoadFromStreamingAssets().BuildEnterPlan(new TongMapEnterRequest
            {
                TargetMapId = 591,
                PlayerLevel = 10
            });

            Assert.IsTrue(plan.IsAllowed);
            Assert.AreEqual(TongMapEnterDecision.Allowed, plan.Decision);
            Assert.AreEqual("aDynMapCopyName", plan.SourceTable);
            Assert.AreEqual("dynamic_template", plan.MapKind);
            Assert.AreEqual(10, plan.RequiredLevel);
            Assert.AreEqual(1, plan.Commands.Count);
            Assert.AreEqual(TongMapEnterCommandKind.NewWorld, plan.Commands[0].Kind);
            Assert.AreEqual(591, plan.Commands[0].TargetMapId);
            Assert.AreEqual(1712, plan.Commands[0].X);
            Assert.AreEqual(3330, plan.Commands[0].Y);
            StringAssert.Contains("tong_mix.lua", plan.PcTongMixSource);
        }

        [Test]
        public void CityAltarMap176_PreservesNpc329CoordinatesFromAddTongNpc()
        {
            var fact = TongMapEnterPlanService.LoadFromStreamingAssets().GetCityAltarNpcFact(176);

            Assert.IsTrue(fact.Found);
            Assert.AreEqual(176, fact.MapId);
            Assert.AreEqual(329, fact.NpcTemplateId);
            Assert.AreEqual(1561, fact.X);
            Assert.AreEqual(2942, fact.Y);
            Assert.AreEqual(@"\\script\\tong\\npc\\jitan.lua", fact.ScriptRaw);
            StringAssert.Contains("addtongnpc.lua", fact.PcAddTongNpcSource);
        }

        [Test]
        public void UnderLevel_RejectsBeforeAnyMovementCommand()
        {
            var plan = TongMapEnterPlanService.LoadFromStreamingAssets().BuildEnterPlan(new TongMapEnterRequest
            {
                TargetMapId = 591,
                PlayerLevel = 9
            });

            Assert.IsFalse(plan.IsAllowed);
            Assert.AreEqual(TongMapEnterDecision.UnderLevel, plan.Decision);
            Assert.AreEqual(10, plan.RequiredLevel);
            Assert.AreEqual(TongMapEnterPlanService.UnderLevelMessage, plan.Message);
            Assert.AreEqual(0, plan.Commands.Count);
        }

        [Test]
        public void MissingMap_RejectsWithoutFabricatedCoordinates()
        {
            var plan = TongMapEnterPlanService.LoadFromStreamingAssets().BuildEnterPlan(new TongMapEnterRequest
            {
                TargetMapId = 999999,
                PlayerLevel = 200
            });

            Assert.IsFalse(plan.IsAllowed);
            Assert.AreEqual(TongMapEnterDecision.MissingMap, plan.Decision);
            Assert.IsNull(plan.Map);
            Assert.AreEqual(0, plan.Commands.Count);
        }

        [Test]
        public void SetPosSurface_UsesSamePcCoordinatesWithoutHostMutation()
        {
            var plan = TongMapEnterPlanService.LoadFromStreamingAssets().BuildEnterPlan(new TongMapEnterRequest
            {
                TargetMapId = 591,
                PlayerLevel = 10,
                CommandKind = TongMapEnterCommandKind.SetPos
            });

            Assert.IsTrue(plan.IsAllowed);
            Assert.AreEqual(1, plan.Commands.Count);
            Assert.AreEqual(TongMapEnterCommandKind.SetPos, plan.Commands[0].Kind);
            Assert.AreEqual(591, plan.Commands[0].TargetMapId);
            Assert.AreEqual(1712, plan.Commands[0].X);
            Assert.AreEqual(3330, plan.Commands[0].Y);
        }
    }
}
