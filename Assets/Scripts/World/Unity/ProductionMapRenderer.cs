using UnityEngine;

namespace VLTK.Production.World.Unity
{
    [DisallowMultipleComponent]
    public sealed class ProductionMapRenderer : MonoBehaviour
    {
        public int LoadedMapId { get; private set; } = -1;
        public Rect LoadedBounds { get; private set; }
        public Vector2 Spawn { get; private set; }

        public bool Present(MapRuntimeManifest manifest)
        {
            if (manifest == null || manifest.bounds == null || manifest.bounds.world == null || manifest.spawn == null || manifest.spawn.world == null || manifest.mapId != MapRuntimeContract.CanonicalMapId)
                return false;

            Clear();
            LoadedMapId = manifest.mapId;
            LoadedBounds = manifest.bounds.world.ToRect();
            Spawn = manifest.spawn.world.ToVector2();

            GameObject floor = new GameObject("map-53-bounds");
            floor.transform.SetParent(transform, false);
            var box = floor.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = LoadedBounds.size;
            box.offset = LoadedBounds.center;
            return true;
        }

        public void Clear()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child); else DestroyImmediate(child);
            }
            LoadedMapId = -1;
            LoadedBounds = new Rect();
            Spawn = Vector2.zero;
        }
    }
}
