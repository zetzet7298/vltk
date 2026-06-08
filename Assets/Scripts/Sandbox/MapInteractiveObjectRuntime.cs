// -----------------------------------------------------------------------------
// VLTK Mobile — Region_S Obj_S.dat runtime renderer.
// Renders only exact PC ObjData SPRs; missing art is skipped/logged, never faked.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [DisallowMultipleComponent]
    public sealed class MapInteractiveObjectRuntime : MonoBehaviour
    {
        public int CurrentMapId { get; private set; } = -1;
        public int RenderedCount { get; private set; }
        public int SkippedCount { get; private set; }
        public int MissingVisualCount { get; private set; }
        public int InteractiveActionCount { get; private set; }

        private Transform _root;
        private MapInteractiveCatalogFile _catalog;
        private PcObjectActionCatalogFile _actionCatalog;
        private PcObjectActionExecutor _actionExecutor;

        private void Awake()
        {
            EnsureRoot();
        }

        public void RenderForMap(MapDefinition mapDef)
        {
            Clear();
            if (mapDef == null || mapDef.catalogEntry == null) return;
            CurrentMapId = mapDef.catalogEntry.mapId;
            EnsureRoot();
            _catalog ??= MapInteractiveCatalogRuntime.LoadFromStreamingAssets();
            var geometry = _catalog?.FindForMap(mapDef);
            if (geometry?.objects == null || geometry.objects.Length == 0)
            {
                SubsystemLog.Info("MapObject", $"Map {CurrentMapId}: no PC Region_S objects");
                return;
            }

            foreach (var obj in geometry.objects)
                RenderObject(obj);

            SubsystemLog.Info("MapObject",
                $"Map {CurrentMapId}: rendered={RenderedCount}, skipped={SkippedCount}, missingVisual={MissingVisualCount}");
        }

        public void Clear()
        {
            if (_root != null)
            {
                for (int i = _root.childCount - 1; i >= 0; i--)
                    DestroySafe(_root.GetChild(i).gameObject);
            }
            RenderedCount = 0;
            SkippedCount = 0;
            MissingVisualCount = 0;
            InteractiveActionCount = 0;
            CurrentMapId = -1;
        }

        private void EnsureRoot()
        {
            if (_root != null) return;
            var child = transform.Find("RegionSObjects");
            if (child == null)
            {
                var go = new GameObject("RegionSObjects");
                go.transform.SetParent(transform, false);
                child = go.transform;
            }
            _root = child;
        }

        private void RenderObject(MapInteractiveObject obj)
        {
            if (!CanRender(obj))
            {
                SkippedCount++;
                return;
            }

            Vector2 world = MapEnemyDatabase.MpsToWorld(obj.mpsX, obj.mpsY);
            var go = new GameObject($"Obj_{obj.templateId}_{obj.nameVi}");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(world.x, world.y, 0f);
            var visual = go.AddComponent<PcMapObjectVisual>();
            visual.Configure(obj);
            if (!visual.HasVisual)
            {
                DestroySafe(go);
                MissingVisualCount++;
                return;
            }
            AttachInteractionIfPorted(go, obj);
            RenderedCount++;
        }

        private void AttachInteractionIfPorted(GameObject go, MapInteractiveObject obj)
        {
            _actionCatalog ??= PcObjectActionCatalogRuntime.LoadFromStreamingAssets();
            _actionExecutor ??= new PcObjectActionExecutor(_actionCatalog, new SandboxTrapTravelHost());
            if (!_actionExecutor.HasAction(obj)) return;
            var interaction = go.AddComponent<PcMapObjectInteraction>();
            interaction.Configure(obj, _actionExecutor);
            InteractiveActionCount++;
        }

        private static void DestroySafe(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        private static bool CanRender(MapInteractiveObject obj)
        {
            return obj != null &&
                   !obj.skipPaint &&
                   obj.isUnseen == 0 &&
                   !string.IsNullOrWhiteSpace(obj.imageName);
        }
    }
}
