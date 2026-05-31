using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Model
{
    [Serializable]
    public class RuntimeSandboxState
    {
        public int activeMapId = -1;
        public string activeRegion;
        public Vector3 playerPosition;
        public string cameraState;
        public float timeScale = 1f;
        public string weatherOverride;
        public List<string> visibleDebugLayers = new();
        public string selectedEntity;
        public string lastError;
        public List<string> logFilters = new();
    }
}
