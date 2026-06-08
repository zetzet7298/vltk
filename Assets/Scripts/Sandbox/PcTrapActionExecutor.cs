// -----------------------------------------------------------------------------
// VLTK Mobile — executes the deterministic subset of PC trap Lua actions.
// Ported APIs: NewWorld(mapId,x,y) and SetPos(x,y), with PC cell coords.
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
        void NewWorld(int mapId, Vector2 worldPosition);
        void SetPos(Vector2 worldPosition);
    }

    public sealed class PcTrapActionExecutor : ITrapActionExecutor
    {
        private readonly PcTrapActionCatalogFile _catalog;
        private readonly ITrapTravelHost _host;

        public PcTrapActionExecutor(PcTrapActionCatalogFile catalog, ITrapTravelHost host)
        {
            _catalog = catalog;
            _host = host;
        }

        public bool TryExecute(TrapDefinition trap, out TrapActionExecutionResult result)
        {
            result = null;
            if (trap == null || _catalog == null) return false;
            var action = _catalog.Find(trap.trapId, trap.trapIdHex);
            if (action == null) return false;

            if (_host == null)
            {
                result = Failure(action, "trap travel host unavailable");
                return true;
            }

            var target = action.TargetWorldPosition();
            if (action.IsNewWorld)
            {
                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }
                _host.NewWorld(action.targetMapId, target);
                result = Success(action, $"NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            if (action.IsSetPos)
            {
                _host.SetPos(target);
                result = Success(action, $"SetPos({action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            result = Failure(action, $"unsupported trap action '{action.actionKind}'");
            return true;
        }

        private static TrapActionExecutionResult Success(PcTrapActionCatalogEntry action, string detail)
            => new TrapActionExecutionResult { success = true, detail = Detail(action, detail) };

        private static TrapActionExecutionResult Failure(PcTrapActionCatalogEntry action, string detail)
            => new TrapActionExecutionResult { success = false, detail = Detail(action, detail) };

        private static string Detail(PcTrapActionCatalogEntry action, string detail)
        {
            string fight = action.fightState >= 0 ? $", SetFightState({action.fightState}) pending" : string.Empty;
            return $"{detail}{fight}; script={action.scriptPath}";
        }
    }

    public sealed class SandboxTrapTravelHost : ITrapTravelHost
    {
        public bool HasMap(int mapId)
        {
            var manager = SandboxManager.Instance;
            return manager?.MapManager?.Catalog != null && manager.MapManager.Catalog.ContainsKey(mapId);
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
    }
}
