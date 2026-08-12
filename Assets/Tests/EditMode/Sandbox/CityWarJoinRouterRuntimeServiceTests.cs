// VLTK Mobile — CityWarJoinRouter pure runtime semantics tests.

using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class CityWarJoinRouterRuntimeServiceTests
    {
        [Test]
        public void Constants_MatchPcCityWarLuaSubset()
        {
            Assert.AreEqual(6, CityWarJoinRouterRuntimeService.MissionId);
            Assert.AreEqual(1, CityWarJoinRouterRuntimeService.MissionStateVar);
            Assert.AreEqual(99, CityWarJoinRouterRuntimeService.MissionKeyVar);
            Assert.AreEqual(230, CityWarJoinRouterRuntimeService.TaskId);
            Assert.AreEqual(231, CityWarJoinRouterRuntimeService.TaskValue);
            Assert.AreEqual(232, CityWarJoinRouterRuntimeService.TaskKey);
            Assert.AreEqual(233, CityWarJoinRouterRuntimeService.TaskCityId);
            Assert.AreEqual(221, CityWarJoinRouterRuntimeService.MissionMapId);
            Assert.AreEqual(1, CityWarJoinRouterRuntimeService.RouteCamp(222));
            Assert.AreEqual(2, CityWarJoinRouterRuntimeService.RouteCamp(223));
            Assert.AreEqual(new CityWarCell(221, 1533, 3211), CityWarJoinRouterRuntimeService.DefenderSpawn);
            Assert.AreEqual(new CityWarCell(221, 1903, 3608), CityWarJoinRouterRuntimeService.AttackerSpawn);
            Assert.AreEqual(new CityWarCell(0, 1613, 3185), CityWarJoinRouterRuntimeService.OuterPosition);
            CollectionAssert.AreEqual(new[] { 363, 362, 355, 354, 367, 366, 359, 358, 357, 356, 365, 364, 361, 360 }, CityWarJoinRouterRuntimeService.CardTab);
        }

        [Test]
        public void MissionNotStarted_PostsWaitingMessageWithoutJoin()
        {
            var plan = Build(new CityWarJoinInput { MissionState = 0, MissionMapAvailable = true });

            Assert.IsTrue(plan.Success);
            Assert.IsFalse(plan.Joined);
            CollectionAssert.AreEqual(new[] { CityWarJoinRouterRuntimeService.WaitingMessage }, plan.Messages);
            Assert.IsEmpty(plan.NewWorlds);
            Assert.IsEmpty(plan.SetPositions);
        }

        [Test]
        public void ExistingTaskKeyValue_JoinsRouteCampWithCardJoinType()
        {
            var plan = Build(ActiveInput(currentMapId: 222, missionKey: 9876, taskKey: 9876, taskCamp: 1));

            AssertJoin(plan, camp: 1, type: 2, missionGroup: 3, CityWarJoinRouterRuntimeService.DefenderSpawn);
            Assert.IsFalse(plan.TaskWrites.Any(t => t.TaskId == CityWarJoinRouterRuntimeService.TaskId));
            Assert.IsEmpty(plan.DeletedItems);
        }

        [Test]
        public void NoCityId_FailsWithoutSideEffects()
        {
            var input = ActiveInput(currentMapId: 222);
            input.WarCityId = 0;

            var plan = Build(input);

            Assert.IsFalse(plan.Success);
            Assert.AreEqual("GetWarOfCity()==0", plan.FailureReason);
            Assert.IsFalse(plan.Joined);
            Assert.IsEmpty(plan.Actions);
            Assert.IsEmpty(plan.TaskWrites);
            Assert.IsEmpty(plan.DeletedItems);
        }

        [Test]
        public void OddCityCard_JoinsCamp2AndWritesPcTasks()
        {
            var input = ActiveInput(currentMapId: 222, cityId: 1);
            input.ItemCounts[363] = 1;

            var plan = Build(input);

            AssertJoin(plan, camp: 2, type: 2, missionGroup: 4, CityWarJoinRouterRuntimeService.AttackerSpawn);
            CollectionAssert.AreEqual(new[] { 363 }, plan.DeletedItems);
            AssertTask(plan, CityWarJoinRouterRuntimeService.TaskId, 6);
            AssertTask(plan, CityWarJoinRouterRuntimeService.TaskKey, 9876);
            AssertTask(plan, CityWarJoinRouterRuntimeService.TaskValue, 2);
            AssertTask(plan, CityWarJoinRouterRuntimeService.TaskCityId, 1);
        }

        [Test]
        public void EvenCityCard_JoinsCamp1AndWritesPcTasks()
        {
            var input = ActiveInput(currentMapId: 223, cityId: 1);
            input.ItemCounts[362] = 1;

            var plan = Build(input);

            AssertJoin(plan, camp: 1, type: 2, missionGroup: 3, CityWarJoinRouterRuntimeService.DefenderSpawn);
            CollectionAssert.AreEqual(new[] { 362 }, plan.DeletedItems);
            AssertTask(plan, CityWarJoinRouterRuntimeService.TaskValue, 1);
        }

        [Test]
        public void NoCard_PostsMessageAndSetsOuterPositionWithoutJoin()
        {
            var plan = Build(ActiveInput(currentMapId: 222, cityId: 1));

            Assert.IsTrue(plan.Success);
            Assert.IsFalse(plan.Joined);
            CollectionAssert.AreEqual(new[] { CityWarJoinRouterRuntimeService.NoCardMessage }, plan.Messages);
            CollectionAssert.AreEqual(new[] { CityWarJoinRouterRuntimeService.OuterPosition }, plan.SetPositions);
            Assert.IsEmpty(plan.NewWorlds);
        }

        [Test]
        public void ExpiredCard_DeletesCardPostsVietnameseMessageAndSetsOuterPosition()
        {
            var input = ActiveInput(currentMapId: 222, cityId: 1);
            input.ItemCounts[363] = 1;
            input.ItemLifeMinutes[363] = 6 * 1440;

            var plan = Build(input);

            Assert.IsTrue(plan.Success);
            Assert.IsFalse(plan.Joined);
            CollectionAssert.AreEqual(new[] { 363 }, plan.DeletedItems);
            CollectionAssert.AreEqual(new[] { "Lệnh bài này từ 6 ngày trước đã hết hạn, không thể dùng được" }, plan.Messages);
            CollectionAssert.AreEqual(new[] { CityWarJoinRouterRuntimeService.OuterPosition }, plan.SetPositions);
            Assert.IsEmpty(plan.NewWorlds);
        }

        [Test]
        public void TongOwnerWithEnoughJoinTime_DirectJoinType1AndResetsOldTicketData()
        {
            var input = ActiveInput(currentMapId: 222, missionKey: 9876, taskKey: 111, taskCamp: 0, cityId: 1);
            input.TongName = "Bang A";
            input.DefenderMissionTongName = "Bang A";
            input.JoinTongMinutes = 1440;

            var plan = Build(input);

            AssertJoin(plan, camp: 1, type: 1, missionGroup: 1, CityWarJoinRouterRuntimeService.DefenderSpawn);
            AssertTask(plan, CityWarJoinRouterRuntimeService.TaskKey, 9876);
            AssertBattleData(plan, "PL_KEYNUMBER", 0);
            AssertBattleData(plan, "PL_TOTALPOINT", 0);
            AssertBattleData(plan, "PL_BATTLECAMP", 0);
            Assert.IsEmpty(plan.DeletedItems);
        }

        [Test]
        public void TongOwnerTooNew_NoCardFallsBackAndPostsTooNewMessageOnly()
        {
            var input = ActiveInput(currentMapId: 222, cityId: 1);
            input.TongName = "Bang A";
            input.DefenderMissionTongName = "Bang A";
            input.JoinTongMinutes = 1439;

            var plan = Build(input);

            Assert.IsTrue(plan.Success);
            Assert.IsFalse(plan.Joined);
            CollectionAssert.AreEqual(new[] { CityWarJoinRouterRuntimeService.OuterPosition }, plan.SetPositions);
            CollectionAssert.AreEqual(new[] { CityWarJoinRouterRuntimeService.TooNewTongMessage }, plan.Messages);
        }

        [Test]
        public void MissingMissionMap_FailsBeforeAnySideEffects()
        {
            var input = ActiveInput(currentMapId: 222, cityId: 1);
            input.MissionMapAvailable = false;
            input.ItemCounts[363] = 1;

            var plan = Build(input);

            Assert.IsFalse(plan.Success);
            Assert.IsFalse(plan.Joined);
            Assert.IsEmpty(plan.Actions);
            Assert.IsEmpty(plan.DeletedItems);
        }

        private static CityWarJoinPlan Build(CityWarJoinInput input)
        {
            return new CityWarJoinRouterRuntimeService().BuildPlan(input);
        }

        private static CityWarJoinInput ActiveInput(int currentMapId, int missionKey = 9876, int taskKey = 0, int taskCamp = 0, int cityId = 1)
        {
            return new CityWarJoinInput
            {
                CurrentMapId = currentMapId,
                MissionMapAvailable = true,
                MissionState = 1,
                MissionKey = missionKey,
                TaskKeyValue = taskKey,
                TaskCampValue = taskCamp,
                WarCityId = cityId,
            };
        }

        private static void AssertJoin(CityWarJoinPlan plan, int camp, int type, int missionGroup, CityWarCell spawn)
        {
            Assert.IsTrue(plan.Success, plan.FailureReason);
            Assert.IsTrue(plan.Joined);
            Assert.AreEqual(camp, plan.JoinCamp.Camp);
            Assert.AreEqual(type, plan.JoinCamp.Type);
            Assert.AreEqual(missionGroup, plan.JoinCamp.MissionGroup);
            Assert.AreEqual(spawn, plan.JoinCamp.Spawn);
            CollectionAssert.Contains(plan.NewWorlds, spawn);
            AssertTempTask(plan, CityWarJoinRouterRuntimeService.JoinStateTempTask, 1);
            AssertTempTask(plan, CityWarJoinRouterRuntimeService.CityWarTempTask, 1);
            AssertAction(plan, "LeaveTeam");
            AssertAction(plan, "SetPKFlag");
            AssertAction(plan, "ForbidChangePK");
            AssertAction(plan, "SetDeathScript");
            AssertAction(plan, "SetFightState");
        }

        private static void AssertTask(CityWarJoinPlan plan, int taskId, int value)
        {
            Assert.IsTrue(plan.TaskWrites.Any(t => t.TaskId == taskId && t.Value == value), "missing SetTask " + taskId + "=" + value);
        }

        private static void AssertTempTask(CityWarJoinPlan plan, int taskId, int value)
        {
            Assert.IsTrue(plan.TempTaskWrites.Any(t => t.TaskId == taskId && t.Value == value), "missing SetTaskTemp " + taskId + "=" + value);
        }

        private static void AssertBattleData(CityWarJoinPlan plan, string name, int value)
        {
            Assert.IsTrue(plan.BattleDataWrites.Any(t => t.Name == name && t.Value == value), "missing BT_SetData " + name + "=" + value);
        }

        private static void AssertAction(CityWarJoinPlan plan, string kind)
        {
            Assert.IsTrue(plan.Actions.Any(a => a.Kind == kind), "missing action " + kind);
        }
    }
}
