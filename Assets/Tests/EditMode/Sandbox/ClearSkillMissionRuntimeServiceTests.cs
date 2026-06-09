// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill TeamEnterHole/JoinHole semantic plan tests.
// PC sources:
// - script/missions/clearskill/head.lua
// - script/missions/clearskill/testhole.lua
// - script/missions/clearskill/mission.lua
// - script/global/特殊用地/梦境/trap/梦境to梦境山洞1..4.lua
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class ClearSkillMissionRuntimeServiceTests
    {
        [Test]
        public void Constants_MatchPcHeadAndJoinHole()
        {
            Assert.That(ClearSkillMissionRuntimeService.MissionId, Is.EqualTo(10));
            Assert.That(ClearSkillMissionRuntimeService.JoinStateTaskId, Is.EqualTo(100));
            Assert.That(ClearSkillMissionRuntimeService.MaxMemberCount, Is.EqualTo(20));
            Assert.That(ClearSkillMissionRuntimeService.ClearMapIds, Is.EqualTo(new[] { 242, 243, 244, 245, 246, 247, 248 }));
            Assert.That(ClearSkillMissionRuntimeService.TestMapBeginIds, Is.EqualTo(new[] { 249, 259, 269, 279, 289, 299, 309 }));
            Assert.That(ClearSkillMissionRuntimeService.MaxTestMapCount, Is.EqualTo(10));
            Assert.That(ClearSkillMissionRuntimeService.TestHoleCoords[0].X, Is.EqualTo(1621));
            Assert.That(ClearSkillMissionRuntimeService.TestHoleCoords[3].Y, Is.EqualTo(3347));
            Assert.That(ClearSkillMissionRuntimeService.CampManMpsX, Is.EqualTo(50624));
            Assert.That(ClearSkillMissionRuntimeService.CampManMpsY, Is.EqualTo(105696));
        }

        [Test]
        public void PlanTeamEnterHole_CaptainAndCspAndTeamSizeGatesRejectBeforeAllocation()
        {
            var service = new ClearSkillMissionRuntimeService();

            Assert.That(service.PlanTeamEnterHole(ValidInput(captain: false)).ReasonCode, Is.EqualTo("not_captain"));
            Assert.That(service.PlanTeamEnterHole(ValidInput(cspValid: false)).ReasonCode, Is.EqualTo("csp_check_valid_failed"));

            TeamEnterHoleInput tooSmall = ValidInput();
            tooSmall.TeamMembers.RemoveAt(1);
            Assert.That(service.PlanTeamEnterHole(tooSmall).ReasonCode, Is.EqualTo("team_size_out_of_range"));

            TeamEnterHoleInput tooLarge = ValidInput();
            for (int i = 3; i <= 21; i++)
                tooLarge.TeamMembers.Add(new TeamMemberState(i, false, true, 242));
            Assert.That(service.PlanTeamEnterHole(tooLarge).ReasonCode, Is.EqualTo("team_size_out_of_range"));
        }

        [Test]
        public void PlanTeamEnterHole_RequiresCurrentClearMapAndDerivesCityIndex()
        {
            var service = new ClearSkillMissionRuntimeService();
            TeamEnterHoleInput input = ValidInput(currentMap: 245);
            input.TestMaps.Clear();
            input.TestMaps.Add(new TestMapState(279, true, 0));

            TeamEnterHolePlan plan = service.PlanTeamEnterHole(input);

            Assert.That(plan.Accepted, Is.True);
            Assert.That(plan.CityIndex, Is.EqualTo(4));
            Assert.That(plan.TestMapId, Is.EqualTo(279));
            Assert.That(service.PlanTeamEnterHole(ValidInput(currentMap: 1)).ReasonCode, Is.EqualTo("current_map_not_clear_map"));
        }

        [Test]
        public void PlanTeamEnterHole_ScansFirstFreeLoadedTestMapWithinTenMapCityRange()
        {
            var service = new ClearSkillMissionRuntimeService();
            TeamEnterHoleInput input = ValidInput(currentMap: 243);
            input.TestMaps.Clear();
            input.TestMaps.Add(new TestMapState(259, true, 1));
            input.TestMaps.Add(new TestMapState(260, false, 0));
            input.TestMaps.Add(new TestMapState(261, true, 0));
            input.TestMaps.Add(new TestMapState(269, true, 0));

            TeamEnterHolePlan plan = service.PlanTeamEnterHole(input);

            Assert.That(plan.Accepted, Is.True);
            Assert.That(plan.CityIndex, Is.EqualTo(2));
            Assert.That(plan.TestMapId, Is.EqualTo(261));

            input.TestMaps[2].MissionV1 = 1;
            Assert.That(service.PlanTeamEnterHole(input).ReasonCode, Is.EqualTo("no_free_test_map"));
        }

        [Test]
        public void PlanTeamEnterHole_EmitsDeterministicJoinHolePlanForEligibleActivePlayerAndMember()
        {
            var service = new ClearSkillMissionRuntimeService();
            TeamEnterHoleInput input = ValidInput(trapId: 4);
            input.TeamMembers.Add(new TeamMemberState(3, false, false, 242));
            input.TeamMembers.Add(new TeamMemberState(4, false, true, 243));

            TeamEnterHolePlan plan = service.PlanTeamEnterHole(input);

            Assert.That(plan.MissionOperations[0].Name, Is.EqualTo("OpenMission"));
            Assert.That(plan.MissionOperations[1].Name, Is.EqualTo("RunMission"));
            Assert.That(plan.JoinHolePlans.Count, Is.EqualTo(2));
            AssertJoinHole(plan.JoinHolePlans[0], playerId: 1, active: true);
            AssertJoinHole(plan.JoinHolePlans[1], playerId: 2, active: false);
        }

        private static TeamEnterHoleInput ValidInput(int trapId = 1, int currentMap = 242, bool captain = true, bool cspValid = true)
        {
            var input = new TeamEnterHoleInput
            {
                TrapId = trapId,
                IsCaptain = captain,
                IsDisabledUseTownPortal = cspValid,
                CurrentMapId = currentMap,
            };
            input.TeamMembers.Add(new TeamMemberState(1, true, true, currentMap));
            input.TeamMembers.Add(new TeamMemberState(2, false, true, currentMap));
            input.TestMaps.Add(new TestMapState(249, true, 0));
            return input;
        }

        private static void AssertJoinHole(JoinHolePlan plan, int playerId, bool active)
        {
            Assert.That(plan.PlayerId, Is.EqualTo(playerId));
            Assert.That(plan.IsActivePlayer, Is.EqualTo(active));
            Assert.That(plan.TestMapId, Is.EqualTo(249));
            Assert.That(plan.EnterX, Is.EqualTo(1670));
            Assert.That(plan.EnterY, Is.EqualTo(3347));
            AssertOp(plan.Operations[0], "LeaveTeam");
            AssertOp(plan.Operations[1], "NewWorld", 249, 1670, 3347);
            AssertOp(plan.Operations[2], "AddMSPlayer", 10, 1);
            AssertOp(plan.Operations[3], "SetTaskTemp", 100, 1);
            AssertOp(plan.Operations[4], "SetTaskTemp", 200, 1);
            AssertOp(plan.Operations[5], "SetFightState", 1);
            AssertOp(plan.Operations[6], "SetLogoutRV", 1);
            Assert.That(plan.Operations[7].Name, Is.EqualTo("SetDeathScript"));
            Assert.That(plan.Operations[7].TextArg, Is.EqualTo(@"\script\missions\clearskill\playerdeath.lua"));
            AssertOp(plan.Operations[8], "SetPunish", 0);
            AssertOp(plan.Operations[9], "SetTempRevPos", 249, 50624, 105696);
            AssertOp(plan.Operations[10], "ForbidChangePK", 0);
            AssertOp(plan.Operations[11], "SetPKFlag", 1);
        }

        private static void AssertOp(RuntimeOperation op, string name, params int[] intArgs)
        {
            Assert.That(op.Name, Is.EqualTo(name));
            Assert.That(op.IntArgs, Is.EqualTo(intArgs));
        }
    }
}
