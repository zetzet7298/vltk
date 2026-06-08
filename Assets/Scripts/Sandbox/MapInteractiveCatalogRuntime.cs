// -----------------------------------------------------------------------------
// VLTK Mobile — runtime loader for generated Region_S trap/object catalog.
// Source: Assets/StreamingAssets/MapInteractiveCatalog.json generated from
// PC SceneDataDef.h KSPTrap/KSPObj records.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [Serializable]
    public sealed class MapInteractiveCatalogFile
    {
        public MapInteractiveGeometry[] geometries;

        public MapInteractiveGeometry FindForMap(MapDefinition mapDef)
        {
            if (mapDef == null || geometries == null) return null;
            string geometryKey = mapDef.catalogEntry?.geometryKey;
            int mapId = mapDef.catalogEntry?.mapId ?? 0;
            foreach (var geometry in geometries)
            {
                if (geometry == null) continue;
                if (!string.IsNullOrEmpty(geometryKey) &&
                    string.Equals(geometry.geometryKey, geometryKey, StringComparison.OrdinalIgnoreCase))
                    return geometry;
                if (geometry.ContainsMapId(mapId)) return geometry;
            }
            return null;
        }
    }

    [Serializable]
    public sealed class MapInteractiveGeometry
    {
        public string geometryKey;
        public int primaryMapId;
        public int[] mapIds;
        public string pcMapPath;
        public string serverMapPath;
        public int trapCount;
        public int objectCount;
        public int[] staticTrapClearMapIds;
        public MapInteractiveTrap[] traps;
        public MapInteractiveObject[] objects;

        public bool ContainsMapId(int mapId)
        {
            if (mapId <= 0 || mapIds == null) return false;
            for (int i = 0; i < mapIds.Length; i++)
                if (mapIds[i] == mapId) return true;
            return false;
        }
    }

    [Serializable]
    public sealed class MapInteractiveTrap
    {
        public int regionCol;
        public int regionRow;
        public int index;
        public int cellX;
        public int cellY;
        public int numCell;
        public uint trapId;
        public string trapIdHex;
        public bool scriptResolved;
        public string scriptPath;
        public int[] inactiveMapIds;
        public int reserved;

        public bool IsInactiveForMap(int mapId)
        {
            if (inactiveMapIds == null) return false;
            for (int i = 0; i < inactiveMapIds.Length; i++)
                if (inactiveMapIds[i] == mapId) return true;
            return false;
        }
    }

    [Serializable]
    public sealed class MapInteractiveObject
    {
        public int regionCol;
        public int regionRow;
        public int index;
        public int templateId;
        public int state;
        public int bioIndex;
        public int mpsX;
        public int mpsY;
        public int z;
        public int direction;
        public bool skipPaint;
        public string script;
        public string nameVi;
        public string kind;
        public string imageName;
        public string imageUid;
        public int imageCurFrame;
        public int imageCurDir;
        public int imageTotalFrame;
        public int imageTotalDir;
        public int imageInterval;
        public int imageCgXpos;
        public int imageCgYpos;
        public int height;
        public int layer;
        public int isUnseen;
        public int obstacleKind;
        public int loopAnimation;
    }

    public static class MapInteractiveCatalogRuntime
    {
        public static MapInteractiveCatalogFile LoadFromStreamingAssets(string fileName = "MapInteractiveCatalog.json")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<MapInteractiveCatalogFile>(File.ReadAllText(path));
        }
    }
}
