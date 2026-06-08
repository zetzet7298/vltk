// -----------------------------------------------------------------------------
// VLTK Mobile — executes the deterministic subset of PC trap Lua actions.
// Ported APIs: NewWorld(mapId,x,y), SetPos(x,y), and simple
// GetFightState()/SetFightState() gate traps with PC cell coords, plus read-only
// Msg2Player/Say/Talk message-only traps, Msg2Player+NewWorld traps, and
// open-server date gates from configall.lua, and task-state gates from PC GetTask(), and deterministic citywar camp gates.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public sealed class TrapActionExecutionResult
    {
        public bool success;
        public string detail;
    }

    public interface ITrapActionExecutor
    {
        bool TryExecute(TrapDefinition trap, out TrapActionExecutionResult result);
    }

    public interface ITrapTravelHost
    {
        bool HasMap(int mapId);
        int GetCurrentMapId();
        bool TryGetPlayerReviveWorld(out int mapId, out Vector2 worldPosition);
        int GetPlayerLevel();
        long GetCurrentDateYmdHm();
        int RandomIntInclusive(int minInclusive, int maxInclusive);
        int GetTaskValue(int taskId);
        int GetCurCamp();
        int GetCamp();
        int GetBattleRank();
        int GetFightState();
        int GetPlayerFactionId();
        void NewWorld(int mapId, Vector2 worldPosition);
        void SetPos(Vector2 worldPosition);
        void SetFightState(int fightState);
        void SetCurCamp(int camp);
        void SetLogoutRv(int value);
        void SetPkFlag(int value);
        void ForbidChangePk(int value);
        void SetPunish(int value);
        void SetCreateTeam(int value);
        void SetTaskTemp(int taskId, int value);
        void SetDeathScript(string scriptPath);
        void LeaveTeam();
        void SetRevPos(int mapId, int reviveId);
    }

    public interface ITrapActionSideEffects
    {
        void PostMessage(string message);
        void AddStation(int stationId);
        void AddTermini(int terminiId);
        void SetProtectTime(int ticks);
        void AddSkillState(int skillStateId, int level, int durationTicks);
        void ApplyCityWarRankEffect(int rank);
    }

    public sealed class PcTrapActionExecutor : ITrapActionExecutor
    {
        private readonly PcTrapActionCatalogFile _catalog;
        private readonly ITrapTravelHost _host;
        private readonly ITrapActionSideEffects _sideEffects;

        public PcTrapActionExecutor(PcTrapActionCatalogFile catalog, ITrapTravelHost host, ITrapActionSideEffects sideEffects = null)
        {
            _catalog = catalog;
            _host = host;
            _sideEffects = sideEffects;
        }

        public bool TryExecute(TrapDefinition trap, out TrapActionExecutionResult result)
        {
            result = null;
            if (trap == null || _catalog == null) return false;
            var action = _catalog.Find(trap.trapId, trap.trapIdHex);
            if (action == null) return false;

            if (action.IsMessageOnly)
            {
                if (_sideEffects == null)
                {
                    result = Failure(action, "trap side-effect host unavailable");
                    return true;
                }
                int posted = 0;
                if (action.messages != null)
                {
                    foreach (string message in action.messages)
                    {
                        if (string.IsNullOrWhiteSpace(message)) continue;
                        _sideEffects.PostMessage(message);
                        posted++;
                    }
                }
                else if (!string.IsNullOrWhiteSpace(action.message))
                {
                    _sideEffects.PostMessage(action.message);
                    posted = 1;
                }
                result = Success(action, $"{action.actionKind}(lines={posted})");
                return true;
            }

            if (_host == null)
            {
                result = Failure(action, "trap travel host unavailable");
                return true;
            }

            var target = action.TargetWorldPosition();
            if (action.IsTaskSetPosMessage)
            {
                int taskValue = _host.GetTaskValue(action.taskId);
                var branch = FindTaskBranch(action, taskValue);
                if (branch == null)
                {
                    result = Success(action, $"GetTask({action.taskId})=={taskValue} -> no branch");
                    return true;
                }

                var branchTarget = action.TaskBranchWorldPosition(branch);
                _host.SetPos(branchTarget);
                if (_sideEffects != null && !string.IsNullOrWhiteSpace(branch.message))
                    _sideEffects.PostMessage(branch.message);
                result = Success(action,
                    $"GetTask({action.taskId})=={taskValue} -> SetPos({branch.targetCellX},{branch.targetCellY}) -> {branchTarget}");
                return true;
            }

            if (action.IsTaskOptionalMessageNewWorld)
            {
                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }

                int taskValue = _host.GetTaskValue(action.taskId);
                var branch = FindTaskBranch(action, taskValue);
                if (branch != null && _sideEffects != null && !string.IsNullOrWhiteSpace(branch.message))
                    _sideEffects.PostMessage(branch.message);
                ApplyFightState(action);
                _host.NewWorld(action.targetMapId, target);
                result = Success(action,
                    $"GetTask({action.taskId})=={taskValue} -> optional Talk + NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsTaskFactionGateNewWorld)
            {
                int taskValue = _host.GetTaskValue(action.taskId);
                int factionId = _host.GetPlayerFactionId();
                if (taskValue >= action.passTaskMinInclusive && factionId == action.requiredFactionId)
                {
                    if (!_host.HasMap(action.targetMapId))
                    {
                        result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                        return true;
                    }
                    _host.NewWorld(action.targetMapId, target);
                    if (action.fightState >= 0)
                        _host.SetFightState(action.fightState);
                    result = Success(action,
                        $"GetTask({action.taskId})={taskValue}, GetFaction()=={action.requiredFaction}#{action.requiredFactionId} -> NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                    return true;
                }

                var failTarget = action.FailTargetWorldPosition();
                string failMessage = taskValue > action.midTaskMinExclusive && taskValue < action.midTaskMaxExclusive
                    ? action.message
                    : action.blockedMessage;
                if (_sideEffects != null && !string.IsNullOrWhiteSpace(failMessage))
                    _sideEffects.PostMessage(failMessage);
                _host.SetPos(failTarget);
                result = Success(action,
                    $"GetTask({action.taskId})={taskValue}, faction={factionId} -> Talk + SetPos({action.failTargetCellX},{action.failTargetCellY}) -> {failTarget}");
                return true;
            }

            if (action.IsTaskPromptDefaultNewWorld)
            {
                int taskValue = _host.GetTaskValue(action.taskId);
                var branch = FindTaskBranch(action, taskValue);
                if (branch != null)
                {
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(branch.message))
                        _sideEffects.PostMessage(branch.message);
                    result = Success(action,
                        $"GetTask({action.taskId})=={taskValue} -> prompt-only branch lines={(string.IsNullOrWhiteSpace(branch.message) ? 0 : 1)}");
                    return true;
                }

                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }
                ApplyFightState(action);
                _host.NewWorld(action.targetMapId, target);
                ApplyOptionalSideEffects(action);
                result = Success(action,
                    $"GetTask({action.taskId})=={taskValue} -> default enter_cave NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsTaskFactionMessageGateNewWorld)
            {
                int taskValue = _host.GetTaskValue(action.taskId);
                int factionId = _host.GetPlayerFactionId();
                if (taskValue >= action.passTaskMinInclusive && factionId == action.requiredFactionId)
                {
                    if (!_host.HasMap(action.targetMapId))
                    {
                        result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                        return true;
                    }
                    ApplyFightState(action);
                    _host.NewWorld(action.targetMapId, target);
                    result = Success(action,
                        $"GetTask({action.taskId})={taskValue}, GetFaction()=={action.requiredFaction}#{action.requiredFactionId} -> NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                    return true;
                }

                string failMessage = taskValue < action.passTaskMinInclusive
                    ? action.message
                    : action.blockedMessage;
                if (_sideEffects != null && !string.IsNullOrWhiteSpace(failMessage))
                    _sideEffects.PostMessage(failMessage);
                result = Success(action,
                    $"GetTask({action.taskId})={taskValue}, faction={factionId} -> Talk only, no warp");
                return true;
            }

            if (action.IsTaskFactionPromptGateNewWorld)
            {
                int taskValue = _host.GetTaskValue(action.taskId);
                int factionId = _host.GetPlayerFactionId();
                if (factionId != action.requiredFactionId)
                {
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.blockedMessage))
                        _sideEffects.PostMessage(action.blockedMessage);
                    result = Success(action,
                        $"GetTask({action.taskId})={taskValue}, faction={factionId} -> wrong-faction Msg2Player only, no warp");
                    return true;
                }

                var branch = FindTaskBranch(action, taskValue);
                if (branch != null)
                {
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(branch.message))
                        _sideEffects.PostMessage(branch.message);
                    result = Success(action,
                        $"GetTask({action.taskId})=={taskValue}, GetFaction()=={action.requiredFaction}#{action.requiredFactionId} -> callback prompt only, no warp");
                    return true;
                }

                if (taskValue >= action.passTaskMinInclusive)
                {
                    if (!_host.HasMap(action.targetMapId))
                    {
                        result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                        return true;
                    }
                    ApplyFightState(action);
                    _host.NewWorld(action.targetMapId, target);
                    result = Success(action,
                        $"GetTask({action.taskId})={taskValue}, GetFaction()=={action.requiredFaction}#{action.requiredFactionId} -> NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                    return true;
                }

                if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.message))
                    _sideEffects.PostMessage(action.message);
                result = Success(action,
                    $"GetTask({action.taskId})={taskValue}, GetFaction()=={action.requiredFaction}#{action.requiredFactionId} -> low-task Msg2Player only, no warp");
                return true;
            }

            if (action.IsTaskCurrentMapReturnNewWorld)
            {
                int taskValue = _host.GetTaskValue(action.taskId);
                if (taskValue != 0)
                {
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.message))
                        _sideEffects.PostMessage(action.message);
                    result = Success(action,
                        $"GetTask({action.taskId})={taskValue} -> Say callback prompt only, no auto-return");
                    return true;
                }

                int currentMapId = _host.GetCurrentMapId();
                int mappedIndex = IndexOf(action.currentMapIds, currentMapId);
                if (mappedIndex < 0)
                {
                    result = Success(action, $"current map {currentMapId} not in PC return table -> no action");
                    return true;
                }

                if (action.currentTargetMapIds == null || action.currentTargetCellXs == null || action.currentTargetCellYs == null ||
                    mappedIndex >= action.currentTargetMapIds.Length || mappedIndex >= action.currentTargetCellXs.Length || mappedIndex >= action.currentTargetCellYs.Length)
                {
                    result = Failure(action, "current-map return table is malformed");
                    return true;
                }

                int targetMapId = action.currentTargetMapIds[mappedIndex];
                if (!_host.HasMap(targetMapId))
                {
                    result = Failure(action, $"target map {targetMapId} missing from catalog");
                    return true;
                }

                var mappedTarget = action.CurrentMapTargetWorldPosition(mappedIndex);
                _host.NewWorld(targetMapId, mappedTarget);
                result = Success(action,
                    $"GetTask({action.taskId})=0, currentMap={currentMapId} -> NewWorld({targetMapId},{action.currentTargetCellXs[mappedIndex]},{action.currentTargetCellYs[mappedIndex]}) -> {mappedTarget}");
                return true;
            }

            if (action.IsClearSkillSwitchTrap)
            {
                int currentFightState = _host.GetFightState();
                if (currentFightState == action.ifFightState)
                {
                    var enterTarget = action.EnterWorldPosition();
                    if (action.enterNextFightState >= 0)
                        _host.SetFightState(action.enterNextFightState);
                    ApplyPcFlagSideEffects(action.pkFlag, action.forbidChangePk, action.punish, action.logoutRv);
                    _host.SetPos(enterTarget);
                    result = Success(action,
                        $"CSP_SwitchTrap({action.trapIndex}) GetFightState()=={currentFightState} -> SetFightState({action.enterNextFightState}) + SetPos({action.enterCellX},{action.enterCellY}) -> {enterTarget}");
                    return true;
                }

                var exitTarget = action.ExitWorldPosition();
                if (action.exitNextFightState >= 0)
                    _host.SetFightState(action.exitNextFightState);
                ApplyPcFlagSideEffects(action.exitPkFlag, action.exitForbidChangePk, action.exitPunish, action.exitLogoutRv);
                _host.SetPos(exitTarget);
                result = Success(action,
                    $"CSP_SwitchTrap({action.trapIndex}) GetFightState()=={currentFightState} -> SetFightState({action.exitNextFightState}) + SetPos({action.exitCellX},{action.exitCellY}) -> {exitTarget}");
                return true;
            }

            if (action.IsClearSkillLeaveGame)
            {
                int currentMapId = _host.GetCurrentMapId();
                int targetMapId = ResolveClearSkillClearMap(action, currentMapId);
                if (targetMapId <= 0)
                {
                    result = Failure(action, $"CSP_GetCityIndexByTestMap({currentMapId}) target unavailable");
                    return true;
                }
                if (!_host.HasMap(targetMapId))
                {
                    result = Failure(action, $"target map {targetMapId} missing from catalog");
                    return true;
                }

                int camp = _host.GetCamp();
                ApplyFightState(action);
                ApplyPcFlagSideEffects(action.pkFlag, action.forbidChangePk, action.punish, action.logoutRv);
                if (action.setTaskTempId > 0)
                    _host.SetTaskTemp(action.setTaskTempId, action.setTaskTempValue);
                _host.SetCurCamp(camp);
                _host.SetDeathScript(action.deathScript ?? string.Empty);
                _host.LeaveTeam();
                if (action.reviveSubWorldId > 0)
                    _host.SetRevPos(targetMapId, action.reviveSubWorldId);
                var leaveTarget = action.EnterWorldPosition();
                _host.NewWorld(targetMapId, leaveTarget);
                result = Success(action,
                    $"LeaveGame({action.trapIndex}) map {currentMapId} -> NewWorld({targetMapId},{action.enterCellX},{action.enterCellY}) -> {leaveTarget}");
                return true;
            }

            if (action.IsCsArenaLeaveTrap)
            {
                int targetMapId = _host.GetTaskValue(action.leaveMapTaskId);
                int targetCellX = _host.GetTaskValue(action.leaveCellXTaskId);
                int targetCellY = _host.GetTaskValue(action.leaveCellYTaskId);
                if (targetMapId <= 0 || targetCellX <= 0 || targetCellY <= 0)
                {
                    result = Failure(action,
                        $"GetLeavePos() task values unavailable: GetTask({action.leaveMapTaskId},{action.leaveCellXTaskId},{action.leaveCellYTaskId}) -> ({targetMapId},{targetCellX},{targetCellY})");
                    return true;
                }
                if (!_host.HasMap(targetMapId))
                {
                    result = Failure(action, $"target map {targetMapId} missing from catalog");
                    return true;
                }

                _host.LeaveTeam();
                _host.SetCurCamp(_host.GetCamp());
                ApplyFightState(action);
                if (action.logoutRv >= 0)
                    _host.SetLogoutRv(action.logoutRv);
                if (action.reviveMapId > 0 || action.reviveSubWorldId > 0)
                    _host.SetRevPos(action.reviveMapId, action.reviveSubWorldId);
                var leaveTarget = CellToWorld(targetCellX, targetCellY);
                _host.NewWorld(targetMapId, leaveTarget);
                result = Success(action,
                    $"CS arena LeaveTrap -> LeaveTeam, SetCurCamp(GetCamp), SetFightState({action.fightState}), SetLogoutRV({action.logoutRv}), SetRevPos({action.reviveMapId},{action.reviveSubWorldId}), NewWorld({targetMapId},{targetCellX},{targetCellY}) -> {leaveTarget}");
                return true;
            }

            if (action.IsTaskTripletLeaveTrap)
            {
                int targetMapId = _host.GetTaskValue(action.leaveMapTaskId);
                int targetCellX = _host.GetTaskValue(action.leaveCellXTaskId);
                int targetCellY = _host.GetTaskValue(action.leaveCellYTaskId);
                if (targetMapId <= 0 || targetCellX <= 0 || targetCellY <= 0)
                {
                    result = Failure(action,
                        $"GetLeavePos() task values unavailable: GetTask({action.leaveMapTaskId},{action.leaveCellXTaskId},{action.leaveCellYTaskId}) -> ({targetMapId},{targetCellX},{targetCellY})");
                    return true;
                }
                if (!_host.HasMap(targetMapId))
                {
                    result = Failure(action, $"target map {targetMapId} missing from catalog");
                    return true;
                }

                _host.SetCurCamp(_host.GetCamp());
                ApplyFightState(action);
                if (action.reviveMapId > 0 || action.reviveSubWorldId > 0)
                    _host.SetRevPos(action.reviveMapId, action.reviveSubWorldId);
                if (action.logoutRv >= 0)
                    _host.SetLogoutRv(action.logoutRv);
                if (action.createTeam >= 0)
                    _host.SetCreateTeam(action.createTeam);
                if (action.deathScript != null)
                    _host.SetDeathScript(action.deathScript);
                ApplyPcFlagSideEffects(action.pkFlag, action.forbidChangePk, action.punish, -1);
                if (action.setTaskTempId > 0)
                    _host.SetTaskTemp(action.setTaskTempId, action.setTaskTempValue);
                var leaveTarget = CellToWorld(targetCellX, targetCellY);
                _host.NewWorld(targetMapId, leaveTarget);
                result = Success(action,
                    $"TaskTripletLeaveTrap -> SetCurCamp(GetCamp), SetFightState({action.fightState}), SetRevPos({action.reviveMapId},{action.reviveSubWorldId}), NewWorld({targetMapId},{targetCellX},{targetCellY}) -> {leaveTarget}");
                return true;
            }

            if (action.IsCityWarCampGateSetPos)
            {
                int currentFightState = _host.GetFightState();
                if (currentFightState == action.ifFightState)
                {
                    var enterTarget = action.CityWarEnterWorldPosition();
                    _host.SetPos(enterTarget);
                    if (action.enterNextFightState >= 0)
                        _host.SetFightState(action.enterNextFightState);
                    if (action.applyRankEffectOnEnter && _sideEffects != null)
                        _sideEffects.ApplyCityWarRankEffect(_host.GetBattleRank());
                    result = Success(action,
                        $"GetFightState()=={currentFightState} -> SetPos({action.enterCellX},{action.enterCellY}) -> {enterTarget}, SetFightState({action.enterNextFightState}), bt_RankEffect");
                    return true;
                }

                int currentCamp = _host.GetCurCamp();
                if (currentCamp != action.requiredCamp)
                {
                    var blockedTarget = action.CityWarBlockedWorldPosition();
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.blockedMessage))
                        _sideEffects.PostMessage(action.blockedMessage);
                    _host.SetPos(blockedTarget);
                    result = Success(action,
                        $"GetCurCamp()=={currentCamp} != {action.requiredCamp} -> Msg2Player + SetPos({action.blockedCellX},{action.blockedCellY}) -> {blockedTarget}");
                    return true;
                }

                var exitTarget = action.CityWarExitWorldPosition();
                _host.SetPos(exitTarget);
                if (action.exitNextFightState >= 0)
                    _host.SetFightState(action.exitNextFightState);
                result = Success(action,
                    $"GetCurCamp()=={currentCamp}, GetFightState()=={currentFightState} -> SetPos({action.exitCellX},{action.exitCellY}) -> {exitTarget}, SetFightState({action.exitNextFightState})");
                return true;
            }

            if (action.IsCityWarCampReturnNewWorld)
            {
                int currentCamp = _host.GetCurCamp();
                if (currentCamp != action.requiredCamp)
                {
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.blockedMessage))
                        _sideEffects.PostMessage(action.blockedMessage);
                    result = Success(action, $"GetCurCamp()=={currentCamp} != {action.requiredCamp} -> Msg2Player");
                    return true;
                }

                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }

                if (action.resetCurCampToOriginal)
                    _host.SetCurCamp(_host.GetCamp());
                ApplyFightState(action);
                if (action.logoutRv >= 0)
                    _host.SetLogoutRv(action.logoutRv);
                _host.NewWorld(action.targetMapId, target);
                result = Success(action,
                    $"GetCurCamp()=={currentCamp} -> SetCurCamp(GetCamp()), SetLogoutRV({action.logoutRv}), NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsMsg2PlayerNewWorld)
            {
                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }
                if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.message))
                    _sideEffects.PostMessage(action.message);
                ApplyFightState(action);
                _host.NewWorld(action.targetMapId, target);
                ApplyOptionalSideEffects(action);
                result = Success(action, $"Msg2Player + NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsLevelGateNewWorld)
            {
                int playerLevel = _host.GetPlayerLevel();
                if (playerLevel >= action.requiredLevel)
                {
                    if (!_host.HasMap(action.targetMapId))
                    {
                        result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                        return true;
                    }
                    ApplyFightState(action);
                    ApplyOptionalSideEffects(action);
                    _host.NewWorld(action.targetMapId, target);
                    result = Success(action,
                        $"GetLevel()=={playerLevel} >= {action.requiredLevel} -> NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                    return true;
                }

                PostTrapMessages(action);
                if (action.failTargetCellX > 0 || action.failTargetCellY > 0)
                {
                    var failTarget = action.FailTargetWorldPosition();
                    _host.SetPos(failTarget);
                    result = Success(action,
                        $"GetLevel()=={playerLevel} < {action.requiredLevel} -> Talk + SetPos({action.failTargetCellX},{action.failTargetCellY}) -> {failTarget}");
                    return true;
                }
                result = Success(action, $"GetLevel()=={playerLevel} < {action.requiredLevel} -> Talk");
                return true;
            }

            if (action.IsLevelBracketNewWorld)
            {
                int playerLevel = _host.GetPlayerLevel();
                if (playerLevel < action.requiredLevel)
                {
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.message))
                        _sideEffects.PostMessage(action.message);
                    ApplyOptionalSideEffects(action);
                    result = Success(action, $"GetLevel()=={playerLevel} < {action.requiredLevel} -> Talk(message), no NewWorld");
                    return true;
                }

                int branchIndex = FindLevelBracket(action, playerLevel);
                if (branchIndex < 0)
                {
                    result = Failure(action, $"GetLevel()=={playerLevel} no level bracket target");
                    return true;
                }
                int targetMapId = action.levelBracketTargetMapIds[branchIndex];
                if (!_host.HasMap(targetMapId))
                {
                    result = Failure(action, $"target map {targetMapId} missing from catalog");
                    return true;
                }

                var bracketTarget = action.LevelBracketWorldPosition(branchIndex);
                _host.NewWorld(targetMapId, bracketTarget);
                ApplyFightState(action);
                if (_sideEffects != null && action.levelBracketMessages != null && branchIndex < action.levelBracketMessages.Length)
                {
                    string branchMessage = action.levelBracketMessages[branchIndex];
                    if (!string.IsNullOrWhiteSpace(branchMessage))
                        _sideEffects.PostMessage(branchMessage);
                }
                ApplyOptionalSideEffects(action);
                result = Success(action,
                    $"GetLevel()=={playerLevel} -> bracket#{branchIndex} NewWorld({targetMapId},{action.levelBracketTargetCellXs[branchIndex]},{action.levelBracketTargetCellYs[branchIndex]}) -> {bracketTarget}, SetFightState({action.fightState})");
                return true;
            }

            if (action.IsOpenServerDateGateSetPos)
            {
                long currentDate = _host.GetCurrentDateYmdHm();
                if (currentDate < action.openServerDate)
                {
                    var closedTarget = action.ClosedTargetWorldPosition();
                    _host.SetPos(closedTarget);
                    if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.openServerMessage))
                        _sideEffects.PostMessage(action.openServerMessage);
                    ApplyStationProtectSkill(action.closedStationIds, action.closedProtectTicks,
                        action.closedSkillStateId, action.closedSkillStateLevel, action.closedSkillStateTime);
                    result = Success(action,
                        $"GetLocalDate()=={currentDate} < {action.openServerDate} -> SetPos({action.closedTargetCellX},{action.closedTargetCellY}) -> {closedTarget}");
                    return true;
                }

                int currentFightState = _host.GetFightState();
                var openTarget = action.ConditionalTargetWorldPosition(currentFightState);
                int nextFightState = action.ConditionalNextFightState(currentFightState);
                _host.SetPos(openTarget);
                if (nextFightState >= 0)
                    _host.SetFightState(nextFightState);
                ApplyStationProtectSkill(action.openStationIds, action.openProtectTicks,
                    action.openSkillStateId, action.openSkillStateLevel, action.openSkillStateTime);
                result = Success(action,
                    $"GetLocalDate()=={currentDate} >= {action.openServerDate}, GetFightState()=={currentFightState} -> SetPos({(currentFightState == action.ifFightState ? action.ifTargetCellX : action.elseTargetCellX)},{(currentFightState == action.ifFightState ? action.ifTargetCellY : action.elseTargetCellY)}) -> {openTarget}, SetFightState({nextFightState})");
                return true;
            }

            if (action.IsMessageRandomNewWorld)
            {
                int branchIndex = ChooseRandomBranch(action, _host.RandomIntInclusive(action.randomMin, action.randomMax));
                int targetMapId = action.randomTargetMapIds[branchIndex];
                if (!_host.HasMap(targetMapId))
                {
                    result = Failure(action, $"target map {targetMapId} missing from catalog");
                    return true;
                }
                var randomTarget = action.RandomTargetWorldPosition(branchIndex);
                if (_sideEffects != null && !string.IsNullOrWhiteSpace(action.message))
                    _sideEffects.PostMessage(action.message);
                if (action.randomFightState >= 0)
                    _host.SetFightState(action.randomFightState);
                _host.NewWorld(targetMapId, randomTarget);
                result = Success(action,
                    $"Talk + random({action.randomMin},{action.randomMax}) branch#{branchIndex} -> NewWorld({targetMapId},{action.randomTargetCellXs[branchIndex]},{action.randomTargetCellYs[branchIndex]}) -> {randomTarget}");
                return true;
            }

            if (action.IsRandomNewWorld)
            {
                int currentMapId = _host.GetCurrentMapId();
                if (Contains(action.noActionMapIds, currentMapId))
                {
                    result = Success(action, $"SubWorldIdx2ID(SubWorld)=={currentMapId} -> return");
                    return true;
                }

                if (action.gateCurrentMapId > 0 && currentMapId == action.gateCurrentMapId)
                {
                    if (!_host.HasMap(action.gateTargetMapId))
                    {
                        result = Failure(action, $"target map {action.gateTargetMapId} missing from catalog");
                        return true;
                    }
                    var gateTarget = action.GateTargetWorldPosition();
                    if (action.gateFightState >= 0)
                        _host.SetFightState(action.gateFightState);
                    _host.NewWorld(action.gateTargetMapId, gateTarget);
                    result = Success(action,
                        $"GetWorldPos()=={currentMapId} -> NewWorld({action.gateTargetMapId},{action.gateTargetCellX},{action.gateTargetCellY}) -> {gateTarget}");
                    return true;
                }

                int branchIndex = ChooseRandomBranch(action, _host.RandomIntInclusive(action.randomMin, action.randomMax));
                int targetMapId = action.randomTargetMapIds[branchIndex];
                if (!_host.HasMap(targetMapId))
                {
                    result = Failure(action, $"target map {targetMapId} missing from catalog");
                    return true;
                }
                var randomTarget = action.RandomTargetWorldPosition(branchIndex);
                if (action.randomFightState >= 0)
                    _host.SetFightState(action.randomFightState);
                _host.NewWorld(targetMapId, randomTarget);
                result = Success(action,
                    $"random({action.randomMin},{action.randomMax}) branch#{branchIndex} -> NewWorld({targetMapId},{action.randomTargetCellXs[branchIndex]},{action.randomTargetCellYs[branchIndex]}) -> {randomTarget}");
                return true;
            }

            if (action.IsReviveReturnNewWorld)
            {
                int currentMapId = _host.GetCurrentMapId();
                if (Contains(action.reviveReturnMapIds, currentMapId))
                {
                    if (!_host.TryGetPlayerReviveWorld(out int reviveMapId, out var reviveTarget))
                    {
                        result = Failure(action, "GetPlayerRev()/RevID2WXY target unavailable");
                        return true;
                    }
                    if (!_host.HasMap(reviveMapId))
                    {
                        result = Failure(action, $"revive target map {reviveMapId} missing from catalog");
                        return true;
                    }
                    _host.NewWorld(reviveMapId, reviveTarget);
                    result = Success(action,
                        $"SubWorldIdx2ID(SubWorld)=={currentMapId} -> RevID2WXY(GetPlayerRev()) -> NewWorld({reviveMapId}) at {reviveTarget}");
                    return true;
                }

                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }
                ApplyFightState(action);
                _host.NewWorld(action.targetMapId, target);
                ApplyOptionalSideEffects(action);
                result = Success(action,
                    $"SetFightState({action.fightState}) -> NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsNewWorld)
            {
                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }
                ApplyFightState(action);
                _host.NewWorld(action.targetMapId, target);
                result = Success(action, $"NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsSetPos)
            {
                ApplyFightState(action);
                _host.SetPos(target);
                result = Success(action, $"SetPos({action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsFightStateSetPos)
            {
                int currentFightState = _host.GetFightState();
                var conditionalTarget = action.ConditionalTargetWorldPosition(currentFightState);
                int nextFightState = action.ConditionalNextFightState(currentFightState);
                if (nextFightState >= 0)
                    _host.SetFightState(nextFightState);
                _host.SetPos(conditionalTarget);
                result = Success(action,
                    $"GetFightState()=={currentFightState} -> SetPos({(currentFightState == action.ifFightState ? action.ifTargetCellX : action.elseTargetCellX)},{(currentFightState == action.ifFightState ? action.ifTargetCellY : action.elseTargetCellY)}) -> {conditionalTarget}, SetFightState({nextFightState})");
                return true;
            }

            result = Failure(action, $"unsupported trap action '{action.actionKind}'");
            return true;
        }

        private static bool Contains(int[] values, int needle)
        {
            if (values == null) return false;
            foreach (int value in values)
                if (value == needle)
                    return true;
            return false;
        }

        private static int IndexOf(int[] values, int needle)
        {
            if (values == null) return -1;
            for (int i = 0; i < values.Length; i++)
                if (values[i] == needle)
                    return i;
            return -1;
        }

        private static PcTrapTaskSetPosBranch FindTaskBranch(PcTrapActionCatalogEntry action, int taskValue)
        {
            if (action.taskBranches == null) return null;
            foreach (var branch in action.taskBranches)
            {
                if (branch == null) continue;
                if (Contains(branch.values, taskValue))
                    return branch;
            }
            return null;
        }

        private static int ChooseRandomBranch(PcTrapActionCatalogEntry action, int randomValue)
        {
            if (action.randomThresholds != null)
            {
                for (int i = 0; i < action.randomThresholds.Length; i++)
                    if (randomValue < action.randomThresholds[i])
                        return i;
            }
            int count = action.randomTargetMapIds?.Length ?? 0;
            return Mathf.Max(0, count - 1);
        }

        private void ApplyFightState(PcTrapActionCatalogEntry action)
        {
            if (action.fightState >= 0)
                _host.SetFightState(action.fightState);
        }

        private static int FindLevelBracket(PcTrapActionCatalogEntry action, int playerLevel)
        {
            int count = action.levelBracketTargetMapIds?.Length ?? 0;
            for (int i = 0; i < count; i++)
            {
                int min = action.levelBracketMinLevels != null && i < action.levelBracketMinLevels.Length
                    ? action.levelBracketMinLevels[i]
                    : 0;
                int max = action.levelBracketMaxExclusiveLevels != null && i < action.levelBracketMaxExclusiveLevels.Length
                    ? action.levelBracketMaxExclusiveLevels[i]
                    : 0;
                if (playerLevel >= min && (max <= 0 || playerLevel < max))
                    return i;
            }
            return -1;
        }

        private void ApplyPcFlagSideEffects(int pkFlag, int forbidChangePk, int punish, int logoutRv)
        {
            if (pkFlag >= 0)
                _host.SetPkFlag(pkFlag);
            if (forbidChangePk >= 0)
                _host.ForbidChangePk(forbidChangePk);
            if (punish >= 0)
                _host.SetPunish(punish);
            if (logoutRv >= 0)
                _host.SetLogoutRv(logoutRv);
        }

        private static Vector2 CellToWorld(int cellX, int cellY)
            => MapEnemyDatabase.MpsToWorld(cellX * 32, cellY * 32);

        private static int ResolveClearSkillClearMap(PcTrapActionCatalogEntry action, int currentMapId)
        {
            if (action.clearSkillClearMapIds == null || action.clearSkillTestMapBeginIds == null)
                return 0;
            int count = action.clearSkillTestMapCount > 0 ? action.clearSkillTestMapCount : 10;
            int cityCount = Mathf.Min(action.clearSkillClearMapIds.Length, action.clearSkillTestMapBeginIds.Length);
            for (int i = 0; i < cityCount; i++)
            {
                int begin = action.clearSkillTestMapBeginIds[i];
                if (currentMapId >= begin && currentMapId < begin + count)
                    return action.clearSkillClearMapIds[i];
            }
            return 0;
        }

        private void PostTrapMessages(PcTrapActionCatalogEntry action)
        {
            if (_sideEffects == null) return;
            if (action.messages != null)
            {
                foreach (string message in action.messages)
                    if (!string.IsNullOrWhiteSpace(message))
                        _sideEffects.PostMessage(message);
                return;
            }
            if (!string.IsNullOrWhiteSpace(action.message))
                _sideEffects.PostMessage(action.message);
        }

        private void ApplyOptionalSideEffects(PcTrapActionCatalogEntry action)
        {
            if (_sideEffects == null) return;
            if (action.terminiIds != null)
            {
                foreach (int terminiId in action.terminiIds)
                    _sideEffects.AddTermini(terminiId);
            }
            if (action.protectTicks > 0)
                _sideEffects.SetProtectTime(action.protectTicks);
            if (action.skillStateId > 0)
                _sideEffects.AddSkillState(action.skillStateId, action.skillStateLevel, action.skillStateTime);
        }

        private void ApplyStationProtectSkill(int[] stationIds, int protectTicks,
            int skillStateId, int skillStateLevel, int skillStateTime)
        {
            if (_sideEffects == null) return;
            if (stationIds != null)
            {
                foreach (int stationId in stationIds)
                    _sideEffects.AddStation(stationId);
            }
            if (protectTicks > 0)
                _sideEffects.SetProtectTime(protectTicks);
            if (skillStateId > 0)
                _sideEffects.AddSkillState(skillStateId, skillStateLevel, skillStateTime);
        }

        private static TrapActionExecutionResult Success(PcTrapActionCatalogEntry action, string detail)
            => new TrapActionExecutionResult { success = true, detail = Detail(action, detail) };

        private static TrapActionExecutionResult Failure(PcTrapActionCatalogEntry action, string detail)
            => new TrapActionExecutionResult { success = false, detail = Detail(action, detail) };

        private static string Detail(PcTrapActionCatalogEntry action, string detail)
        {
            string fight = action.fightState >= 0 ? $", SetFightState({action.fightState})" : string.Empty;
            return $"{detail}{fight}; script={action.scriptPath}";
        }
    }

    public sealed class SandboxTrapActionSideEffects : ITrapActionSideEffects
    {
        public void PostMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            var manager = SandboxManager.Instance;
            if (manager?.ChatService != null)
                manager.ChatService.PostSystemMessage(message);
            SubsystemLog.Info("Trap", $"PC trap message: {message}");
        }

        public void AddStation(int stationId)
        {
            if (stationId <= 0) return;
            SubsystemLog.Info("Trap", $"PC AddStation({stationId}) recorded");
        }

        public void AddTermini(int terminiId)
        {
            if (terminiId <= 0) return;
            SubsystemLog.Info("Trap", $"PC AddTermini({terminiId}) recorded");
        }

        public void SetProtectTime(int ticks)
        {
            if (ticks <= 0) return;
            SubsystemLog.Info("Trap", $"PC SetProtectTime({ticks}) recorded");
        }

        public void AddSkillState(int skillStateId, int level, int durationTicks)
        {
            if (skillStateId <= 0) return;
            SubsystemLog.Info("Trap", $"PC AddSkillState({skillStateId},{level},0,{durationTicks}) recorded");
        }

        public void ApplyCityWarRankEffect(int rank)
        {
            SubsystemLog.Info("Trap", $"PC bt_RankEffect(BT_GetData(PL_CURRANK={rank})) recorded");
        }
    }

    public sealed class SandboxTrapTravelHost : ITrapTravelHost
    {
        public bool HasMap(int mapId)
        {
            var manager = SandboxManager.Instance;
            return manager?.MapManager?.Catalog != null && manager.MapManager.Catalog.ContainsKey(mapId);
        }

        public int GetCurrentMapId()
        {
            var manager = SandboxManager.Instance;
            return manager?.MapManager?.ActiveMapId ?? manager?.defaultMapId ?? -1;
        }

        public bool TryGetPlayerReviveWorld(out int mapId, out Vector2 worldPosition)
        {
            mapId = 0;
            worldPosition = default;
            var manager = SandboxManager.Instance;
            int currentMapId = GetCurrentMapId();
            var revive = manager?.MapManager?.TravelData?.GetDefaultRevivePosition(currentMapId);
            if (revive == null || revive.mapId <= 0)
                return false;
            mapId = revive.mapId;
            worldPosition = MapEnemyDatabase.MpsToWorld(revive.x, revive.y);
            return true;
        }

        public int GetPlayerLevel()
        {
            var manager = SandboxManager.Instance;
            return manager?.GameplayLoop?.Player?.level
                   ?? manager?.GameplayLoop?.LevelService?.Level
                   ?? manager?.PlayerProgression?.level
                   ?? 1;
        }

        public long GetCurrentDateYmdHm()
            => long.Parse(DateTime.Now.ToString("yyyyMMddHHmm"));

        public int RandomIntInclusive(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
                return minInclusive;
            return UnityEngine.Random.Range(minInclusive, maxInclusive + 1);
        }

        public int GetTaskValue(int taskId)
            => SandboxManager.Instance?.TaskFlagService?.GetFlag(taskId) ?? 0;

        public int GetCurCamp()
            => SandboxManager.Instance?.GetCurCamp() ?? 0;

        public int GetCamp()
            => SandboxManager.Instance?.GetCamp() ?? 0;

        public int GetBattleRank()
            => 0;

        public int GetFightState()
        {
            return SandboxManager.Instance?.GetFightState() ?? 1;
        }

        public int GetPlayerFactionId()
        {
            var manager = SandboxManager.Instance;
            var gameplayFaction = manager?.GameplayLoop?.Player?.combat.faction ?? CombatFaction.None;
            if (gameplayFaction != CombatFaction.None)
                return (int)gameplayFaction;
            return (int)(manager?.PlayerProgression?.faction ?? CombatFaction.None);
        }

        public void NewWorld(int mapId, Vector2 worldPosition)
        {
            var manager = SandboxManager.Instance;
            manager?.SwitchMap(mapId);
            manager?.PlayerController?.PlaceAt(worldPosition, snapCamera: true);
            SubsystemLog.Info("Trap", $"PC NewWorld trap moved player to map {mapId} at {worldPosition}");
        }

        public void SetPos(Vector2 worldPosition)
        {
            var manager = SandboxManager.Instance;
            manager?.PlayerController?.PlaceAt(worldPosition, snapCamera: true);
            SubsystemLog.Info("Trap", $"PC SetPos trap moved player to {worldPosition}");
        }

        public void SetFightState(int fightState)
        {
            SandboxManager.Instance?.SetFightState(fightState);
        }

        public void SetCurCamp(int camp)
        {
            SandboxManager.Instance?.SetCurCamp(camp);
        }

        public void SetLogoutRv(int value)
        {
            SandboxManager.Instance?.SetLogoutRv(value);
        }

        public void SetPkFlag(int value)
        {
            SandboxManager.Instance?.SetPkFlag(value);
        }

        public void ForbidChangePk(int value)
        {
            SandboxManager.Instance?.ForbidChangePk(value);
        }

        public void SetPunish(int value)
        {
            SandboxManager.Instance?.SetPunish(value);
        }

        public void SetCreateTeam(int value)
        {
            SandboxManager.Instance?.SetCreateTeam(value);
        }

        public void SetTaskTemp(int taskId, int value)
        {
            SandboxManager.Instance?.SetTaskTemp(taskId, value);
        }

        public void SetDeathScript(string scriptPath)
        {
            SandboxManager.Instance?.SetDeathScript(scriptPath);
        }

        public void LeaveTeam()
        {
            SandboxManager.Instance?.LeaveTeamForPcTrap();
        }

        public void SetRevPos(int mapId, int reviveId)
        {
            SandboxManager.Instance?.SetRevPos(mapId, reviveId);
        }
    }
}
