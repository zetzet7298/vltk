// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill mission lifecycle PC proof tests.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class ClearSkillMissionLifecycleConstantsTests
    {
        [Test]
        public void Constants_MatchPcHeadMissionAndTimerLua()
        {
            Assert.That(ClearSkillMissionLifecycleConstants.MissionId, Is.EqualTo(10));
            Assert.That(ClearSkillMissionLifecycleConstants.PkTimeTicks, Is.EqualTo(18 * 60 * 5));
            Assert.That(ClearSkillMissionLifecycleConstants.MissionTimerId, Is.EqualTo(20));
            Assert.That(ClearSkillMissionLifecycleConstants.CampNpcTemplateId, Is.EqualTo(68));
            Assert.That(ClearSkillMissionLifecycleConstants.CampNpcLevel, Is.EqualTo(10));
            Assert.That(ClearSkillMissionLifecycleConstants.CampNpcMpsX, Is.EqualTo(1582 * 32));
            Assert.That(ClearSkillMissionLifecycleConstants.CampNpcMpsY, Is.EqualTo(3303 * 32));
            Assert.That(ClearSkillMissionLifecycleConstants.CampNpcDirection, Is.EqualTo(1));
            Assert.That(ClearSkillMissionLifecycleConstants.RuntimeSubWorldArgument, Is.EqualTo(-2));
            Assert.That(ClearSkillMissionLifecycleConstants.MissionVActiveFlagSlot, Is.EqualTo(1));
            Assert.That(ClearSkillMissionLifecycleConstants.MissionVCampNpcIdSlot, Is.EqualTo(2));
            Assert.That(ClearSkillMissionLifecycleConstants.MissionVFree, Is.EqualTo(0));
            Assert.That(ClearSkillMissionLifecycleConstants.MissionVActive, Is.EqualTo(1));
            Assert.That(ClearSkillMissionLifecycleConstants.CombatTempTaskId, Is.EqualTo(200));
            Assert.That(ClearSkillMissionLifecycleConstants.CampNpcName, Is.EqualTo("Sứ giả bang phái"));
            Assert.That(ClearSkillMissionLifecycleConstants.CampNpcScript, Is.EqualTo(@"\script\missions\clearskill\camperman.lua"));
        }

        [Test]
        public void InitMission_PlanMatchesPcLuaOrder()
        {
            var operations = ClearSkillMissionLifecycleConstants.PlanInitMission();

            Assert.That(operations.Count, Is.EqualTo(5));
            AssertOp(operations[0], "StartMissionTimer", 10, 20, 5400);
            AssertOp(operations[1], "AddNpc", 68, 10, -2, 50624, 105696, 1);
            Assert.That(operations[1].TextArg, Is.EqualTo("Sứ giả bang phái"));
            AssertOp(operations[2], "SetMissionV", 2, LifecycleOperation.ResultOfPreviousOperation);
            AssertOp(operations[3], "SetNpcScript", LifecycleOperation.ResultOfPreviousOperation);
            Assert.That(operations[3].TextArg, Is.EqualTo(@"\script\missions\clearskill\camperman.lua"));
            AssertOp(operations[4], "SetMissionV", 1, 1);
        }

        [Test]
        public void EndMission_PlanResetsActiveFlagAndDeletesExistingCampNpc()
        {
            var operations = ClearSkillMissionLifecycleConstants.PlanEndMission(campNpcId: 1234);

            Assert.That(operations.Count, Is.EqualTo(4));
            AssertOp(operations[0], "GameOver");
            AssertOp(operations[1], "SetMissionV", 1, 0);
            AssertOp(operations[2], "GetMissionV", 2);
            AssertOp(operations[3], "DelNpc", 1234);

            Assert.That(ClearSkillMissionLifecycleConstants.PlanEndMission(0).Count, Is.EqualTo(3));
        }

        [Test]
        public void OnLeave_PlanResetsPcTaskLogoutDeathPkAndClosesWhenLastPlayer()
        {
            var operations = ClearSkillMissionLifecycleConstants.PlanOnLeave(roleIndex: 77, missionPlayerCount: 1);

            Assert.That(operations.Count, Is.EqualTo(8));
            AssertOp(operations[0], "SetPlayerIndex", 77);
            AssertOp(operations[1], "SetLogoutRV", 1);
            Assert.That(operations[2].Name, Is.EqualTo("SetDeathScript"));
            Assert.That(operations[2].TextArg, Is.Empty);
            AssertOp(operations[3], "SetPKFlag", 0);
            AssertOp(operations[4], "ForbidChangePK", 1);
            AssertOp(operations[5], "SetTaskTemp", 200, 0);
            AssertOp(operations[6], "GetMSPlayerCount", 10, 0);
            AssertOp(operations[7], "CloseMission", 10);

            Assert.That(ClearSkillMissionLifecycleConstants.PlanOnLeave(77, 2).Count, Is.EqualTo(7));
        }

        [Test]
        public void Timer_PlanClosesClearSkillMission()
        {
            var operations = ClearSkillMissionLifecycleConstants.PlanTimer();

            Assert.That(operations.Count, Is.EqualTo(1));
            AssertOp(operations[0], "CloseMission", 10);
        }

        private static void AssertOp(LifecycleOperation op, string name, params int[] intArgs)
        {
            Assert.That(op.Name, Is.EqualTo(name));
            Assert.That(op.IntArgs, Is.EqualTo(intArgs));
        }
    }
}
