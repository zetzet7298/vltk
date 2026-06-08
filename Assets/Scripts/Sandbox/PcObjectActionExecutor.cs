// -----------------------------------------------------------------------------
// VLTK Mobile — executes deterministic PC Region_S object Lua actions.
// Ported object API subset: NewWorld(mapId,x,y) with optional SetFightState().
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class ObjectActionExecutionResult
    {
        public bool success;
        public string detail;
    }

    public sealed class PcObjectActionExecutor
    {
        private readonly PcObjectActionCatalogFile _catalog;
        private readonly ITrapTravelHost _host;

        public PcObjectActionExecutor(PcObjectActionCatalogFile catalog, ITrapTravelHost host)
        {
            _catalog = catalog;
            _host = host;
        }

        public bool HasAction(MapInteractiveObject obj)
            => obj != null && _catalog?.Find(obj.script) != null;

        public bool TryExecute(MapInteractiveObject obj, out ObjectActionExecutionResult result)
        {
            result = null;
            if (obj == null || _catalog == null) return false;
            var action = _catalog.Find(obj.script);
            if (action == null) return false;

            if (_host == null)
            {
                result = Failure(action, "object travel host unavailable");
                return true;
            }

            if (action.IsNewWorld)
            {
                if (!_host.HasMap(action.targetMapId))
                {
                    result = Failure(action, $"target map {action.targetMapId} missing from catalog");
                    return true;
                }
                if (action.fightState >= 0)
                    _host.SetFightState(action.fightState);
                var target = action.TargetWorldPosition();
                _host.NewWorld(action.targetMapId, target);
                result = Success(action, $"NewWorld({action.targetMapId},{action.targetCellX},{action.targetCellY}) -> {target}");
                return true;
            }

            result = Failure(action, $"unsupported object action '{action.actionKind}'");
            return true;
        }

        private static ObjectActionExecutionResult Success(PcObjectActionCatalogEntry action, string detail)
            => new ObjectActionExecutionResult { success = true, detail = Detail(action, detail) };

        private static ObjectActionExecutionResult Failure(PcObjectActionCatalogEntry action, string detail)
            => new ObjectActionExecutionResult { success = false, detail = Detail(action, detail) };

        private static string Detail(PcObjectActionCatalogEntry action, string detail)
        {
            string fight = action.fightState >= 0 ? $", SetFightState({action.fightState})" : string.Empty;
            return $"{detail}{fight}; script={action.scriptPath}";
        }
    }

    [DisallowMultipleComponent]
    public sealed class PcMapObjectInteraction : MonoBehaviour
    {
        private MapInteractiveObject _object;
        private PcObjectActionExecutor _executor;

        public MapInteractiveObject Object => _object;

        public void Configure(MapInteractiveObject obj, PcObjectActionExecutor executor)
        {
            _object = obj;
            _executor = executor;
            EnsureClickCollider(obj);
        }

        public ObjectActionExecutionResult Interact()
        {
            if (_executor == null || !_executor.TryExecute(_object, out var result))
                return null;
            if (result.success)
                SubsystemLog.Info("MapObject", $"PC object action applied: {result.detail}");
            else
                SubsystemLog.Error("MapObject", $"PC object action failed: {result.detail}");
            return result;
        }

        private void OnMouseDown() => Interact();

        private void EnsureClickCollider(MapInteractiveObject obj)
        {
            if (obj == null || GetComponent<Collider2D>() != null) return;
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            float width = Mathf.Max(1f, obj.imageCgXpos * 2f / 32f);
            float height = Mathf.Max(1f, (obj.height > 0 ? obj.height : obj.imageCgYpos) / 32f);
            box.size = new Vector2(width, height);
            box.offset = new Vector2(0f, height * 0.5f);
        }
    }
}
