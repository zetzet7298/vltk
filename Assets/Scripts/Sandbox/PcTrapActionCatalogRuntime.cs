// -----------------------------------------------------------------------------
// VLTK Mobile — deterministic PC Region_S trap action catalog runtime.
// Source: MapTrapActionCatalog.json generated from original PC trap Lua main().
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    [Serializable]
    public sealed class PcTrapActionCatalogFile
    {
        public PcTrapActionCatalogEntry[] entries;

        [NonSerialized] private Dictionary<uint, PcTrapActionCatalogEntry> _idLookup;
        [NonSerialized] private Dictionary<string, PcTrapActionCatalogEntry> _hexLookup;

        public int Count => entries?.Length ?? 0;

        public PcTrapActionCatalogEntry Find(uint trapId, string trapIdHex = null)
        {
            EnsureLookup();
            if (trapId != 0 && _idLookup.TryGetValue(trapId, out var byId))
                return byId;
            if (!string.IsNullOrEmpty(trapIdHex) && _hexLookup.TryGetValue(trapIdHex, out var byHex))
                return byHex;
            return null;
        }

        private void EnsureLookup()
        {
            if (_idLookup != null) return;
            _idLookup = new Dictionary<uint, PcTrapActionCatalogEntry>();
            _hexLookup = new Dictionary<string, PcTrapActionCatalogEntry>(StringComparer.OrdinalIgnoreCase);
            if (entries == null) return;
            foreach (var entry in entries)
            {
                if (entry == null) continue;
                if (entry.trapId != 0) _idLookup[entry.trapId] = entry;
                if (!string.IsNullOrEmpty(entry.trapIdHex)) _hexLookup[entry.trapIdHex] = entry;
            }
        }
    }

    [Serializable]
    public sealed class PcTrapActionCatalogEntry
    {
        public uint trapId;
        public string trapIdHex;
        public string scriptPath;
        public string sourceRelPath;
        public string actionKind;
        public int targetMapId;
        public int targetCellX;
        public int targetCellY;
        public int fightState = -1;
        public string source;

        public bool IsNewWorld => string.Equals(actionKind, "NewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsSetPos => string.Equals(actionKind, "SetPos", StringComparison.OrdinalIgnoreCase);

        public Vector2 TargetWorldPosition()
            => MapEnemyDatabase.MpsToWorld(targetCellX * 32, targetCellY * 32);
    }

    public static class PcTrapActionCatalogRuntime
    {
        public static PcTrapActionCatalogFile LoadFromStreamingAssets(string fileName = "MapTrapActionCatalog.json")
        {
            string path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!File.Exists(path)) return null;
            return JsonUtility.FromJson<PcTrapActionCatalogFile>(File.ReadAllText(path));
        }
    }
}
