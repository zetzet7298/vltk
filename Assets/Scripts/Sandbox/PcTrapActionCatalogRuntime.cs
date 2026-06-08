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
    public sealed class PcTrapTaskSetPosBranch
    {
        public int[] values;
        public int targetCellX;
        public int targetCellY;
        public string message;
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
        public int[] levelBracketMinLevels;
        public int[] levelBracketMaxExclusiveLevels;
        public int[] levelBracketTargetMapIds;
        public int[] levelBracketTargetCellXs;
        public int[] levelBracketTargetCellYs;
        public string[] levelBracketMessages;
        public int failTargetCellX;
        public int failTargetCellY;
        public int[] terminiIds;
        public int protectTicks;
        public int skillStateId;
        public int skillStateLevel;
        public int skillStateTime;
        public long openServerDate;
        public string openServerMessage;
        public int closedTargetCellX;
        public int closedTargetCellY;
        public int[] closedStationIds;
        public int[] openStationIds;
        public int closedProtectTicks;
        public int openProtectTicks;
        public int closedSkillStateId;
        public int closedSkillStateLevel;
        public int closedSkillStateTime;
        public int openSkillStateId;
        public int openSkillStateLevel;
        public int openSkillStateTime;
        public int randomMin;
        public int randomMax;
        public int[] randomThresholds;
        public int[] randomTargetMapIds;
        public int[] randomTargetCellXs;
        public int[] randomTargetCellYs;
        public int randomFightState = -1;
        public int[] noActionMapIds;
        public int gateCurrentMapId;
        public int gateTargetMapId;
        public int gateTargetCellX;
        public int gateTargetCellY;
        public int gateFightState = -1;
        public int[] reviveReturnMapIds;
        public int taskId;
        public int passTaskMinInclusive;
        public int midTaskMinExclusive;
        public int midTaskMaxExclusive;
        public string requiredFaction;
        public int requiredFactionId;
        public PcTrapTaskSetPosBranch[] taskBranches;
        public int requiredCamp;
        public int enterCellX;
        public int enterCellY;
        public int enterNextFightState = -1;
        public int exitCellX;
        public int exitCellY;
        public int exitNextFightState = -1;
        public int blockedCellX;
        public int blockedCellY;
        public string blockedMessage;
        public bool applyRankEffectOnEnter;
        public bool resetCurCampToOriginal;
        public int logoutRv = -1;
        public int pkFlag = -1;
        public int forbidChangePk = -1;
        public int punish = -1;
        public int exitPkFlag = -1;
        public int exitForbidChangePk = -1;
        public int exitPunish = -1;
        public int exitLogoutRv = -1;
        public int trapIndex;
        public int[] clearSkillClearMapIds;
        public int[] clearSkillTestMapBeginIds;
        public int clearSkillTestMapCount;
        public int leaveMapTaskId;
        public int leaveCellXTaskId;
        public int leaveCellYTaskId;
        public int reviveMapId;
        public int createTeam = -1;
        public int setTaskTempId;
        public int setTaskTempValue;
        public string deathScript;
        public int reviveSubWorldId;
        public string source;

        public bool IsNewWorld => string.Equals(actionKind, "NewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsSetPos => string.Equals(actionKind, "SetPos", StringComparison.OrdinalIgnoreCase);
        public bool IsFightStateSetPos => string.Equals(actionKind, "FightStateSetPos", StringComparison.OrdinalIgnoreCase);
        public bool IsMsg2Player => string.Equals(actionKind, "Msg2Player", StringComparison.OrdinalIgnoreCase);
        public bool IsSayMessage => string.Equals(actionKind, "SayMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTalkMessage => string.Equals(actionKind, "TalkMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsPromptMessage => string.Equals(actionKind, "PromptMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsMsg2PlayerNewWorld => string.Equals(actionKind, "Msg2PlayerNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsLevelGateNewWorld => string.Equals(actionKind, "LevelGateNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsLevelBracketNewWorld => string.Equals(actionKind, "LevelBracketNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsOpenServerDateGateSetPos => string.Equals(actionKind, "OpenServerDateGateSetPos", StringComparison.OrdinalIgnoreCase);
        public bool IsRandomNewWorld => string.Equals(actionKind, "RandomNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsMessageRandomNewWorld => string.Equals(actionKind, "MessageRandomNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsReviveReturnNewWorld => string.Equals(actionKind, "ReviveReturnNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskSetPosMessage => string.Equals(actionKind, "TaskSetPosMessage", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskOptionalMessageNewWorld => string.Equals(actionKind, "TaskOptionalMessageNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskFactionGateNewWorld => string.Equals(actionKind, "TaskFactionGateNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskPromptDefaultNewWorld => string.Equals(actionKind, "TaskPromptDefaultNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsCityWarCampGateSetPos => string.Equals(actionKind, "CityWarCampGateSetPos", StringComparison.OrdinalIgnoreCase);
        public bool IsCityWarCampReturnNewWorld => string.Equals(actionKind, "CityWarCampReturnNewWorld", StringComparison.OrdinalIgnoreCase);
        public bool IsClearSkillSwitchTrap => string.Equals(actionKind, "ClearSkillSwitchTrap", StringComparison.OrdinalIgnoreCase);
        public bool IsClearSkillLeaveGame => string.Equals(actionKind, "ClearSkillLeaveGame", StringComparison.OrdinalIgnoreCase);
        public bool IsCsArenaLeaveTrap => string.Equals(actionKind, "CsArenaLeaveTrap", StringComparison.OrdinalIgnoreCase);
        public bool IsTaskTripletLeaveTrap => string.Equals(actionKind, "TaskTripletLeaveTrap", StringComparison.OrdinalIgnoreCase);
        public bool IsMessageOnly => IsMsg2Player || IsSayMessage || IsTalkMessage || IsPromptMessage;

        public Vector2 TargetWorldPosition()
            => CellToWorld(targetCellX, targetCellY);

        public Vector2 ConditionalTargetWorldPosition(int currentFightState)
            => currentFightState == ifFightState
                ? CellToWorld(ifTargetCellX, ifTargetCellY)
                : CellToWorld(elseTargetCellX, elseTargetCellY);

        public Vector2 FailTargetWorldPosition()
            => CellToWorld(failTargetCellX, failTargetCellY);

        public Vector2 LevelBracketWorldPosition(int index)
            => CellToWorld(levelBracketTargetCellXs[index], levelBracketTargetCellYs[index]);

        public Vector2 ClosedTargetWorldPosition()
            => CellToWorld(closedTargetCellX, closedTargetCellY);

        public Vector2 GateTargetWorldPosition()
            => CellToWorld(gateTargetCellX, gateTargetCellY);

        public Vector2 RandomTargetWorldPosition(int index)
            => CellToWorld(randomTargetCellXs[index], randomTargetCellYs[index]);

        public Vector2 TaskBranchWorldPosition(PcTrapTaskSetPosBranch branch)
            => CellToWorld(branch.targetCellX, branch.targetCellY);

        public Vector2 EnterWorldPosition()
            => CellToWorld(enterCellX, enterCellY);

        public Vector2 ExitWorldPosition()
            => CellToWorld(exitCellX, exitCellY);

        public Vector2 BlockedWorldPosition()
            => CellToWorld(blockedCellX, blockedCellY);

        public Vector2 CityWarEnterWorldPosition()
            => EnterWorldPosition();

        public Vector2 CityWarExitWorldPosition()
            => ExitWorldPosition();

        public Vector2 CityWarBlockedWorldPosition()
            => BlockedWorldPosition();

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
