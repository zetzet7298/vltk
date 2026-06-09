// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill mission lifecycle plan recorder tests.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class ClearSkillMissionLifecyclePlanExecutorTests
    {
        [Test]
        public void InitMission_ReplayResolvesCampNpcIdForMissionValueAndNpcScript()
        {
            var host = new RecordingClearSkillMissionLifecycleHost { NextNpcId = 4321 };

            var result = ClearSkillMissionLifecyclePlanExecutor.Replay(
                ClearSkillMissionLifecycleConstants.PlanInitMission(), host);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(host.Calls.Count, Is.EqualTo(5));
            AssertCall(host.Calls[0], "StartMissionTimer", null, null, 10, 20, 5400);
            AssertCall(host.Calls[1], "AddNpc", "Sứ giả bang phái", 4321, 68, 10, -2, 50624, 105696, 1);
            AssertCall(host.Calls[2], "SetMissionV", null, null, 2, 4321);
            AssertCall(host.Calls[3], "SetNpcScript", @"\script\missions\clearskill\camperman.lua", null, 4321);
            AssertCall(host.Calls[4], "SetMissionV", null, null, 1, 1);
        }

        [Test]
        public void EndMission_ReplayRecordsPcOrderAndNpcDelete()
        {
            var host = new RecordingClearSkillMissionLifecycleHost();
            host.SetMissionValueSnapshot(ClearSkillMissionLifecycleConstants.MissionVCampNpcIdSlot, 2468);

            var result = ClearSkillMissionLifecyclePlanExecutor.Replay(
                ClearSkillMissionLifecycleConstants.PlanEndMission(campNpcId: 2468), host);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(host.Calls.Count, Is.EqualTo(4));
            AssertCall(host.Calls[0], "GameOver", null, null);
            AssertCall(host.Calls[1], "SetMissionV", null, null, 1, 0);
            AssertCall(host.Calls[2], "GetMissionV", null, 2468, 2);
            AssertCall(host.Calls[3], "DelNpc", null, null, 2468);
        }

        [Test]
        public void OnLeave_ReplayRecordsPcResetOrderAndCloseWhenLastPlayer()
        {
            var host = new RecordingClearSkillMissionLifecycleHost { MissionPlayerCount = 1 };

            var result = ClearSkillMissionLifecyclePlanExecutor.Replay(
                ClearSkillMissionLifecycleConstants.PlanOnLeave(roleIndex: 77, missionPlayerCount: 1), host);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(host.Calls.Count, Is.EqualTo(8));
            AssertCall(host.Calls[0], "SetPlayerIndex", null, null, 77);
            AssertCall(host.Calls[1], "SetLogoutRV", null, null, 1);
            AssertCall(host.Calls[2], "SetDeathScript", string.Empty, null);
            AssertCall(host.Calls[3], "SetPKFlag", null, null, 0);
            AssertCall(host.Calls[4], "ForbidChangePK", null, null, 1);
            AssertCall(host.Calls[5], "SetTaskTemp", null, null, 200, 0);
            AssertCall(host.Calls[6], "GetMSPlayerCount", null, 1, 10, 0);
            AssertCall(host.Calls[7], "CloseMission", null, null, 10);
        }

        [Test]
        public void Timer_ReplayClosesClearSkillMission()
        {
            var host = new RecordingClearSkillMissionLifecycleHost();

            var result = ClearSkillMissionLifecyclePlanExecutor.Replay(
                ClearSkillMissionLifecycleConstants.PlanTimer(), host);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(host.Calls.Count, Is.EqualTo(1));
            AssertCall(host.Calls[0], "CloseMission", null, null, 10);
        }

        [Test]
        public void UnsupportedOperation_IsReportedAsFailureWithoutHostCall()
        {
            var host = new RecordingClearSkillMissionLifecycleHost();
            IReadOnlyList<LifecycleOperation> plan = new[] { LifecycleOperation.NoArgs("UnknownPcApi") };

            var result = ClearSkillMissionLifecyclePlanExecutor.Replay(plan, host);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(host.Calls.Count, Is.EqualTo(0));
            Assert.That(result.Failures.Count, Is.EqualTo(1));
            Assert.That(result.Failures[0].OperationIndex, Is.EqualTo(0));
            Assert.That(result.Failures[0].OperationName, Is.EqualTo("UnknownPcApi"));
            Assert.That(result.Failures[0].Reason, Is.EqualTo("unsupported operation"));
        }

        private static void AssertCall(ClearSkillMissionLifecycleCall call, string name, string textArg, int? returnValue, params int[] intArgs)
        {
            Assert.That(call.Name, Is.EqualTo(name));
            Assert.That(call.IntArgs, Is.EqualTo(intArgs));
            Assert.That(call.TextArg, Is.EqualTo(textArg));
            Assert.That(call.ReturnValue, Is.EqualTo(returnValue));
        }
    }
}
