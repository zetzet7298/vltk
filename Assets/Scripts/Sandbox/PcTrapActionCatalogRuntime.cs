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
        public int ifFightState = -1;
        public int ifTargetCellX;
        public int ifTargetCellY;
        public int ifNextFightState = -1;
        public int elseFightState = -1;
        public int elseTargetCellX;
        public int elseTargetCellY;
        public int elseNextFightState = -1;
        public string message;
        public string[] messages;
        public int requiredLevel;
        public int failTargetCellX;
        public int failTargetCellY;
        public int[] terminiIds;
        public int protectTicks;
        public int skillStateId;
        public int skillStateLevel;
        public int skillStateTime;
        public string source;

        public bool IsNewWorld => string.Equals(actionKind, "NewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsSetPos => string.Equals(actionKind, "SetPos", StringComparison.OrdinalIgnoreCase);
        public bool IsFightStateSetPos => string.Equals(actionKind, "FightStateSetPos", StringComparison.OrdinalIgnoreCase);
        public bool IsMsg2Player => string.Equals(actionKind, "Msg2Player", StringComparison.OrdinalIgnoreCase);
        public bool IsSayMessage => string.Equals(actionKind, "SayMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTalkMessage => string.Equals(actionKind, "TalkMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsMsg2PlayerNewWorld => string.Equals(actionKind, "Msg2PlayerNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsLevelGateNewWorld => string.Equals(actionKind, "LevelGateNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsMessageOnly => IsMsg2Player || IsSayMessage || IsTalkMessage;

        public Vector2 TargetWorldPosition()
            => CellToWorld(targetCellX, targetCellY);

        public Vector2 ConditionalTargetWorldPosition(int currentFightState)
            => currentFightState == ifFightState
                ? CellToWorld(ifTargetCellX, ifTargetCellY)
                : CellToWorld(elseTargetCellX, elseTargetCellY);

        public Vector2 FailTargetWorldPosition()
            => CellToWorld(failTargetCellX, failTargetCellY);

        public int ConditionalNextFightState(int currentFightState)
            => currentFightState == ifFightState ? ifNextFightState : elseNextFightState;

        private static Vector2 CellToWorld(int cellX, int cellY)
            => MapEnemyDatabase.MpsToWorld(cellX * 32, cellY * 32);
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
