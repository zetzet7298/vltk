// -----------------------------------------------------------------------------
// VLTK Mobile — deterministic PC Region_S object action catalog runtime.
// Source: MapObjectActionCatalog.json generated from original PC object Lua main().
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    [Serializable]
    public sealed class PcObjectActionCatalogFile
    {
        public PcObjectActionCatalogEntry[] entries;

        [NonSerialized] private Dictionary<string, PcObjectActionCatalogEntry> _scriptLookup;

        public int Count => entries?.Length ?? 0;

        public PcObjectActionCatalogEntry Find(string scriptPath)
        {
            EnsureLookup();
            if (string.IsNullOrWhiteSpace(scriptPath)) return null;
            return _scriptLookup.TryGetValue(Normalize(scriptPath), out var entry) ? entry : null;
        }

        private void EnsureLookup()
        {
            if (_scriptLookup != null) return;
            _scriptLookup = new Dictionary<string, PcObjectActionCatalogEntry>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.scriptPath)) continue;
                _scriptLookup[Normalize(entry.scriptPath)] = entry;
            }
        }

        private static string Normalize(string scriptPath)
            => scriptPath.Replace('/', '\\').Trim();
    }

    [Serializable]
    public sealed class PcObjectActionCatalogEntry
    {
        public string scriptPath;
        public uint scriptId;
        public string scriptIdHex;
        public string sourceRelPath;
        public string actionKind;
        public int targetMapId;
        public int targetCellX;
        public int targetCellY;
        public int fightState = -1;
        public string message;
        public string[] messages;
        public int[] eventItemIds;
        public string[] notes;
        public bool setPropState;
        public int reviveId;
        public int[] ladderIds;
        public string source;

        public bool IsNewWorld => string.Equals(actionKind, "NewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsPickupMessage => string.Equals(actionKind, "PickupMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsSayMessage => string.Equals(actionKind, "SayMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTalkMessage => string.Equals(actionKind, "TalkMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsOpenBox => string.Equals(actionKind, "OpenBox", StringComparison.OrdinalIgnoreCase);
        public bool IsShowLadder => string.Equals(actionKind, "ShowLadder", StringComparison.OrdinalIgnoreCase);

        public Vector2 TargetWorldPosition()
            => MapEnemyDatabase.MpsToWorld(targetCellX * 32, targetCellY * 32);
    }

    public static class PcObjectActionCatalogRuntime
    {
        public static PcObjectActionCatalogFile LoadFromStreamingAssets(string fileName = "MapObjectActionCatalog.json")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<PcObjectActionCatalogFile>(File.ReadAllText(path));
        }
    }
}
