// VLTK Mobile — CityWar NPC transfer route split proof tests.

using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class CityWarTransferRouteServiceTests
    {
        [Test]
        public void DefenderCard_RoutesToDefenderTransferMap222Only()
        {
            var input = ActiveInput(cityId: 1);
            input.ItemCounts[CityWarPcConstants.GetCardItemIdForCity(1, CityWarCardSide.Defender)] = 1;

            var route = Build(input, CityWarCardSide.Defender);

            AssertAccepted(route, CityWarCardSide.Defender, 222, 1);
            Assert.IsTrue(route.MatchedCard);
            Assert.IsFalse(route.MatchedTaskFallback);
        }

        [Test]
        public void AttackerCard_RoutesToAttackerTransferMap223Only()
        {
            var input = ActiveInput(cityId: 1);
            input.ItemCounts[CityWarPcConstants.GetCardItemIdForCity(1, CityWarCardSide.Attacker)] = 1;

            var route = Build(input, CityWarCardSide.Attacker);

            AssertAccepted(route, CityWarCardSide.Attacker, 223, 2);
            Assert.IsTrue(route.MatchedCard);
            Assert.IsFalse(route.MatchedTaskFallback);
        }

        [Test]
        public void DefenderTaskFallback_AllowsNpcRouteWithoutTongOrCard()
        {
            var input = ActiveInput(cityId: 2);
            input.TaskCityId = 2;
            input.TaskValue = 1;
            input.TaskId = CityWarTransferRouteService.MissionId;

            var route = Build(input, CityWarCardSide.Defender);

            AssertAccepted(route, CityWarCardSide.Defender, 222, 1);
            Assert.IsTrue(route.MatchedTaskFallback);
            Assert.IsFalse(route.MatchedCard);
            Assert.IsFalse(route.MatchedTong);
        }

        [Test]
        public void AttackerTaskFallback_AllowsNpcRouteWithoutTongOrCard()
        {
            var input = ActiveInput(cityId: 2);
            input.TaskCityId = 2;
            input.TaskValue = 2;
            input.TaskId = CityWarTransferRouteService.MissionId;

            var route = Build(input, CityWarCardSide.Attacker);

            AssertAccepted(route, CityWarCardSide.Attacker, 223, 2);
            Assert.IsTrue(route.MatchedTaskFallback);
        }

        [Test]
        public void NoTongNoCardNoTaskFallback_RejectsWithoutFabricatedDestination()
        {
            var route = Build(ActiveInput(cityId: 1), CityWarCardSide.Defender);

            Assert.IsFalse(route.Accepted);
            Assert.AreEqual(CityWarTransferRouteService.RejectReason, route.FailureReason);
            Assert.AreEqual(0, route.TransferMapId);
            Assert.IsEmpty(route.PossibleNewWorlds);
        }

        [Test]
        public void TransferTrapRouteCamp_Maps222ToCamp1AndEveryOtherTransferToCamp2()
        {
            Assert.AreEqual(1, CityWarTransferRouteService.RouteCampFromTransferMap(222));
            Assert.AreEqual(2, CityWarTransferRouteService.RouteCampFromTransferMap(223));
            Assert.AreEqual(CityWarJoinRouterRuntimeService.RouteCamp(222), CityWarTransferRouteService.RouteCampFromTransferMap(222));
            Assert.AreEqual(CityWarJoinRouterRuntimeService.RouteCamp(223), CityWarTransferRouteService.RouteCampFromTransferMap(223));
        }

        [Test]
        public void NoFabrication_OnlyPcTransferCoordinatesAndJoinMapSplitAreExposed()
        {
            var defender = Build(CardInput(1, CityWarCardSide.Defender), CityWarCardSide.Defender);
            var attacker = Build(CardInput(1, CityWarCardSide.Attacker), CityWarCardSide.Attacker);

            CollectionAssert.AreEqual(new[] { new CityWarCell(222, 1614, 3172), new CityWarCell(222, 1629, 3193) }, defender.PossibleNewWorlds);
            CollectionAssert.AreEqual(new[] { new CityWarCell(223, 1614, 3172), new CityWarCell(223, 1629, 3193) }, attacker.PossibleNewWorlds);
            Assert.IsFalse(defender.PossibleNewWorlds.Any(c => c.MapId == 221));
            Assert.AreEqual(221, CityWarTransferRouteService.MissionMapId);
            Assert.AreEqual(new CityWarCell(221, 1533, 3211), CityWarJoinRouterRuntimeService.DefenderSpawn);
            Assert.AreEqual(new CityWarCell(221, 1903, 3608), CityWarJoinRouterRuntimeService.AttackerSpawn);
        }

        private static CityWarTransferRoute Build(CityWarTransferRouteInput input, CityWarCardSide side)
        {
            return new CityWarTransferRouteService().BuildNpcRoute(input, side);
        }

        private static CityWarTransferRouteInput ActiveInput(int cityId)
        {
            return new CityWarTransferRouteInput { CityId = cityId, TongName = "Guest", DefenderTongName = "Def", AttackerTongName = "Atk" };
        }

        private static CityWarTransferRouteInput CardInput(int cityId, CityWarCardSide side)
        {
            var input = ActiveInput(cityId);
            input.ItemCounts[CityWarPcConstants.GetCardItemIdForCity(cityId, side)] = 1;
            return input;
        }

        private static void AssertAccepted(CityWarTransferRoute route, CityWarCardSide side, int mapId, int camp)
        {
            Assert.IsTrue(route.Accepted, route.FailureReason);
            Assert.AreEqual(side, route.RequestedSide);
            Assert.AreEqual(mapId, route.TransferMapId);
            Assert.AreEqual(camp, route.RouteCamp);
            Assert.AreEqual(2, route.PossibleNewWorlds.Count);
            Assert.IsTrue(route.PossibleNewWorlds.All(c => c.MapId == mapId));
        }
    }
}
