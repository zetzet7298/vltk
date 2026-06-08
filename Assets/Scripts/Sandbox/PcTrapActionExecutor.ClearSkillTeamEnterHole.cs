// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill TeamEnterHole trap action hook.
// PC source: script/missions/clearskill/head.lua + testhole.lua and
// script/global/特殊用地/梦境/trap/梦境to梦境山洞*.lua.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionExecutor
    {
        private const int ClearSkillMissionId = 10; // PC MISSIONID in clearskill/head.lua.
        private const int ClearSkillJoinStateTaskId = 100; // PC JOINSTATE in clearskill/head.lua.
        private const int ClearSkillCombatTempTaskId = 200; // PC JoinHole temp combat marker.
        private const int ClearSkillDefaultTestMapCount = 10; // PC CSP_MaxTestMapCount.
        private const int ClearSkillMaxMemberCount = 20; // PC MAX_MEMBER_COUNT.
        private const int ClearSkillCampManMpsX = 1582 * 32; // PC CSP_CAMPMANX.
        private const int ClearSkillCampManMpsY = 3303 * 32; // PC CSP_CAMPMANY.
        private const string ClearSkillDeathScript = @"\script\missions\clearskill\playerdeath.lua";

        private bool TryExecuteClearSkillTeamEnterHole(PcTrapActionCatalogEntry action, out TrapActionExecutionResult result)
        {
            result = null;
            if (action == null || !action.IsClearSkillTeamEnterHole)
                return false;

            if (_host == null)
            {
                result = Failure(action, "trap travel host unavailable");
                return true;
            }

            int currentMapId = _host.GetCurrentMapId();
            int cityIndex = ClearSkillCityIndexFromClearMap(action, currentMapId);
            if (cityIndex < 0)
            {
                result = Success(action, $"CSP_GetCityIndexByClearMap({currentMapId})<=0 -> no TeamEnterHole action");
                return true;
            }

            int targetMapId = ResolveFirstLoadedClearSkillTestMap(action, cityIndex);
            if (targetMapId <= 0)
            {
                result = Failure(action,
                    $"CSP_GetFreeTestMapID(cityIndex={cityIndex + 1}) unavailable via host HasMap over CSP_TestMapBeginTab/CSP_MaxTestMapCount");
                return true;
            }

            int trapIndex = action.trapIndex > 0 ? action.trapIndex : 0;
            if (!TryResolveClearSkillEnterCell(action, trapIndex, out int enterCellX, out int enterCellY))
            {
                result = Failure(action, $"CSP_TestHoleTab[{trapIndex}] unavailable");
                return true;
            }

            var target = CellToWorld(enterCellX, enterCellY);
            _host.LeaveTeam();
            _host.NewWorld(targetMapId, target);
            _host.SetTaskTemp(action.setTaskTempId > 0 ? action.setTaskTempId : ClearSkillJoinStateTaskId, 1);
            _host.SetTaskTemp(ClearSkillCombatTempTaskId, 1);
            _host.SetFightState(action.fightState >= 0 ? action.fightState : 1);
            _host.SetLogoutRv(action.logoutRv >= 0 ? action.logoutRv : 1);
            _host.SetDeathScript(string.IsNullOrEmpty(action.deathScript) ? ClearSkillDeathScript : action.deathScript);
            _host.SetPunish(action.punish >= 0 ? action.punish : 0);
            _host.ForbidChangePk(action.forbidChangePk >= 0 ? action.forbidChangePk : 0);
            _host.SetPkFlag(action.pkFlag >= 0 ? action.pkFlag : 1);

            _sideEffects?.AddNote(
                $"ClearSkill TeamEnterHole host-limited subset: captain/CSP_CheckValid/team-size 2..{ClearSkillMaxMemberCount} validation, " +
                $"OpenMission/RunMission/AddMSPlayer(MISSIONID={ClearSkillMissionId},1), per-test-map MissionV free allocation, " +
                $"and SetTempRevPos(TestMap,{ClearSkillCampManMpsX},{ClearSkillCampManMpsY}) remain integration gaps.");

            result = Success(action,
                $"TeamEnterHole({trapIndex}) currentMap={currentMapId} cityIndex={cityIndex + 1} -> " +
                $"JoinHole({targetMapId},{trapIndex}) deterministic active-player subset: LeaveTeam, " +
                $"NewWorld({targetMapId},{enterCellX},{enterCellY}) -> {target}, " +
                $"SetTaskTemp({ClearSkillJoinStateTaskId},1), SetTaskTemp({ClearSkillCombatTempTaskId},1), " +
                "SetFightState(1), SetLogoutRV(1), SetDeathScript(playerdeath.lua), SetPunish(0), ForbidChangePK(0), SetPKFlag(1); " +
                $"gaps: captain/CSP_CheckValid/team-size 2..{ClearSkillMaxMemberCount}/free-mission/AddMSPlayer(MISSIONID={ClearSkillMissionId})/SetTempRevPos");
            return true;
        }

        private static int ClearSkillCityIndexFromClearMap(PcTrapActionCatalogEntry action, int currentMapId)
        {
            if (action.clearSkillClearMapIds == null)
                return -1;
            for (int i = 0; i < action.clearSkillClearMapIds.Length; i++)
                if (action.clearSkillClearMapIds[i] == currentMapId)
                    return i;
            return -1;
        }

        private int ResolveFirstLoadedClearSkillTestMap(PcTrapActionCatalogEntry action, int cityIndex)
        {
            if (action.clearSkillTestMapBeginIds == null || cityIndex < 0 || cityIndex >= action.clearSkillTestMapBeginIds.Length)
                return 0;
            int begin = action.clearSkillTestMapBeginIds[cityIndex];
            int count = action.clearSkillTestMapCount > 0 ? action.clearSkillTestMapCount : ClearSkillDefaultTestMapCount;
            for (int offset = 0; offset < count; offset++)
            {
                int candidate = begin + offset;
                if (_host.HasMap(candidate))
                    return candidate;
            }
            return 0;
        }

        private static bool TryResolveClearSkillEnterCell(PcTrapActionCatalogEntry action, int trapIndex, out int cellX, out int cellY)
        {
            cellX = action.enterCellX;
            cellY = action.enterCellY;
            if (cellX > 0 && cellY > 0)
                return true;

            switch (trapIndex)
            {
                case 1: cellX = 1621; cellY = 3236; return true;
                case 2: cellX = 1533; cellY = 3235; return true;
                case 3: cellX = 1520; cellY = 3352; return true;
                case 4: cellX = 1670; cellY = 3347; return true;
                default: return false;
            }
        }
    }
}
