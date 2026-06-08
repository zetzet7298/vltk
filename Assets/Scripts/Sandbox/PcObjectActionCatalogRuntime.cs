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
        public string[] taskNotes;
        public bool setPropState;
        public int reviveId;
        public int[] ladderIds;
        public string requiredFaction;
        public int requiredFactionId;
        public int requiredCamp;
        public int taskId;
        public int taskValue;
        public int noteTaskId;
        public int noteTaskMinExclusive;
        public int noteTaskMaxExclusive;
        public int requiredMissingItemId;
        public int[] requiredItemIds;
        public int[] requiredItemCounts;
        public int[] consumeItemIds;
        public int[] consumeItemCounts;
        public int setTaskId;
        public int setTaskValue;
        public string[] preConsumeMessages;
        public string[] successMessages;
        public string[] missingItemMessages;
        public string[] elseMessages;
        public PcObjectActionBranch[] branches;
        public PcObjectActionChoice[] choices;
        public string source;

        public bool IsNewWorld => string.Equals(actionKind, "NewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsPickupMessage => string.Equals(actionKind, "PickupMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskOptionalPickupMessage => string.Equals(actionKind, "TaskOptionalPickupMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskMissingItemPickupMessage => string.Equals(actionKind, "TaskMissingItemPickupMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskItemConsumeMessage => string.Equals(actionKind, "TaskItemConsumeMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskItemBranchMessage => string.Equals(actionKind, "TaskItemBranchMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsPromptBranchMessage => string.Equals(actionKind, "PromptBranchMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsSayMessage => string.Equals(actionKind, "SayMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTalkMessage => string.Equals(actionKind, "TalkMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskTalkMessage => string.Equals(actionKind, "TaskTalkMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsOpenBox => string.Equals(actionKind, "OpenBox", StringComparison.OrdinalIgnoreCase);
        public bool IsFactionOpenBox => string.Equals(actionKind, "FactionOpenBox", StringComparison.OrdinalIgnoreCase);
        public bool IsCampOpenBox => string.Equals(actionKind, "CampOpenBox", StringComparison.OrdinalIgnoreCase);
        public bool IsShowLadder => string.Equals(actionKind, "ShowLadder", StringComparison.OrdinalIgnoreCase);

        public Vector2 TargetWorldPosition()
            => MapEnemyDatabase.MpsToWorld(targetCellX * 32, targetCellY * 32);
    }

    [Serializable]
    public sealed class PcObjectActionBranch
    {
        public string label;
        public string promptMessage;
        public PcObjectActionCondition[] conditions;
        public PcObjectActionChoice[] choices;
        public PcObjectActionEffect[] effects;
    }

    [Serializable]
    public sealed class PcObjectActionChoice
    {
        public string label;
        public string promptMessage;
        public PcObjectActionCondition[] conditions;
        public PcObjectActionEffect[] effects;
    }

    [Serializable]
    public sealed class PcObjectActionCondition
    {
        public string type;
        public int taskId;
        public int value;
        public int minValue;
        public int maxValue;
        public int itemId;
        public int byteIndex;
        public int bitIndex;
        public int count = 1;
    }

    [Serializable]
    public sealed class PcObjectActionEffect
    {
        public string type;
        public int taskId;
        public int value;
        public int itemId;
        public int byteIndex;
        public int bitIndex;
        public int compareByteIndex;
        public int setByteIndex;
        public int[] itemIds;
        public int[] itemCounts;
        public string message;
        public string failureMessage;
        public string noteMessage;
        public string[] messages;
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
