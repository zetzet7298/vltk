// -----------------------------------------------------------------------------
// VLTK Mobile — executes the deterministic subset of PC trap Lua actions.
// Ported APIs: NewWorld(mapId,x,y), SetPos(x,y), and simple
// GetFightState()/SetFightState() gate traps with PC cell coords, plus read-only
// Msg2Player/Say/Talk message-only traps, Msg2Player+NewWorld traps, and
// open-server date gates from configall.lua.
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
        int GetPlayerLevel();
        long GetCurrentDateYmdHm();
        int RandomIntInclusive(int minInclusive, int maxInclusive);
        int GetFightState();
        void NewWorld(int mapId, Vector2 worldPosition);
        void SetPos(Vector2 worldPosition);
        void SetFightState(int fightState);
    }

    public interface ITrapActionSideEffects
    {
        void PostMessage(string message);
        void AddStation(int stationId);
        void AddTermini(int terminiId);
        void SetProtectTime(int ticks);
        void AddSkillState(int skillStateId, int level, int durationTicks);
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

        public int GetFightState()
        {
            return SandboxManager.Instance?.GetFightState() ?? 1;
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
    }
}
