// -----------------------------------------------------------------------------
// VLTK Mobile — executes the deterministic subset of PC trap Lua actions.
// Ported APIs: NewWorld(mapId,x,y), SetPos(x,y), and simple
// GetFightState()/SetFightState() gate traps with PC cell coords, plus read-only
// Msg2Player/Say/Talk message-only traps, and Msg2Player+NewWorld traps.
// -----------------------------------------------------------------------------

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
        int GetFightState();
        void NewWorld(int mapId, Vector2 worldPosition);
        void SetPos(Vector2 worldPosition);
        void SetFightState(int fightState);
    }

    public interface ITrapActionSideEffects
    {
        void PostMessage(string message);
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

        private void ApplyFightState(PcTrapActionCatalogEntry action)
        {
            if (action.fightState >= 0)
                _host.SetFightState(action.fightState);
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
    }

    public sealed class SandboxTrapTravelHost : ITrapTravelHost
    {
        public bool HasMap(int mapId)
        {
            var manager = SandboxManager.Instance;
            return manager?.MapManager?.Catalog != null && manager.MapManager.Catalog.ContainsKey(mapId);
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
