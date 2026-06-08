// -----------------------------------------------------------------------------
// VLTK Mobile — Region_S Trap.dat runtime metadata/trigger layer.
// PC KSPTrap has no sprite; this creates invisible trigger volumes only and
// routes to TrapTriggerService with deterministic PC NewWorld/SetPos actions.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [DisallowMultipleComponent]
    public sealed class MapTrapRuntime : MonoBehaviour
    {
        private const float CellSize = 32f;
        private const int RegionMpsWidth = 512;
        private const int RegionMpsHeight = 1024;

        public int CurrentMapId { get; private set; } = -1;
        public int ActiveTriggerCount { get; private set; }
        public int DisabledTrapCount { get; private set; }
        public int MissingScriptCount { get; private set; }

        private Transform _root;
        private MapInteractiveCatalogFile _catalog;
        private PcTrapActionCatalogFile _actionCatalog;
        private TrapTriggerService _triggerService;

        private void Awake()
        {
            EnsureRoot();
            EnsureService();
        }

        public void BuildForMap(MapDefinition mapDef)
        {
            Clear();
            if (mapDef == null || mapDef.catalogEntry == null) return;
            CurrentMapId = mapDef.catalogEntry.mapId;
            EnsureRoot();
            EnsureService();
            _catalog ??= MapInteractiveCatalogRuntime.LoadFromStreamingAssets();
            var geometry = _catalog?.FindForMap(mapDef);
            if (geometry?.traps == null || geometry.traps.Length == 0)
            {
                SubsystemLog.Info("MapTrap", $"Map {CurrentMapId}: no PC Region_S traps");
                return;
            }

            foreach (var trap in geometry.traps)
            {
                if (trap == null) continue;
                if (trap.IsInactiveForMap(CurrentMapId))
                {
                    DisabledTrapCount++;
                    continue;
                }
                CreateTrigger(trap);
            }
            SubsystemLog.Info("MapTrap",
                $"Map {CurrentMapId}: active={ActiveTriggerCount}, disabled={DisabledTrapCount}, missingScript={MissingScriptCount}");
        }

        public void Clear()
        {
            if (_root != null)
            {
                for (int i = _root.childCount - 1; i >= 0; i--)
                    DestroySafe(_root.GetChild(i).gameObject);
            }
            ActiveTriggerCount = 0;
            DisabledTrapCount = 0;
            MissingScriptCount = 0;
            CurrentMapId = -1;
            _triggerService?.ClearLog();
        }

        private void CreateTrigger(MapInteractiveTrap trap)
        {
            if (!trap.scriptResolved || string.IsNullOrEmpty(trap.scriptPath))
                MissingScriptCount++;

            float width = Mathf.Max(1, trap.numCell) * CellSize;
            float height = CellSize;
            int mpsX = trap.regionCol * RegionMpsWidth + trap.cellX * (int)CellSize;
            int mpsY = trap.regionRow * RegionMpsHeight + trap.cellY * (int)CellSize;
            Vector2 topLeft = MapEnemyDatabase.MpsToWorld(mpsX, mpsY);

            var go = new GameObject($"Trap_{trap.trapIdHex ?? trap.trapId.ToString()}");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(topLeft.x + width * 0.5f, topLeft.y - height * 0.5f, 0f);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(width, height);

            var trigger = go.AddComponent<MapTrapTrigger>();
            trigger.Configure(ToTrapDefinition(trap), _triggerService);
            ActiveTriggerCount++;
        }

        private static TrapDefinition ToTrapDefinition(MapInteractiveTrap trap)
        {
            var script = string.IsNullOrEmpty(trap.scriptPath) ? null : trap.scriptPath;
            var def = new TrapDefinition
            {
                trapIndex = trap.index,
                trapId = trap.trapId,
                trapIdHex = trap.trapIdHex,
                boundsRect = new RectDef
                {
                    x = trap.cellX,
                    y = trap.cellY,
                    width = Mathf.Max(1, trap.numCell),
                    height = 1,
                },
                scriptRef = script,
                triggerType = TrapTriggerType.Enter,
                scriptFound = trap.scriptResolved,
            };
            if (!trap.scriptResolved)
                def.warnings.Add($"Trap script unresolved for {trap.trapIdHex ?? trap.trapId.ToString()}");
            return def;
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            var child = transform.Find("RegionSTraps");
            if (child == null)
            {
                var go = new GameObject("RegionSTraps");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            _root = child;
        }

        private void EnsureService()
        {
            if (_triggerService != null) return;
            _actionCatalog ??= PcTrapActionCatalogRuntime.LoadFromStreamingAssets();
            var executor = new PcTrapActionExecutor(_actionCatalog, new SandboxTrapTravelHost());
            _triggerService = new TrapTriggerService(null, luaEnabled: false, actionExecutor: executor) { EnterFunction = "main" };
        }

        private static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }
    }
}
