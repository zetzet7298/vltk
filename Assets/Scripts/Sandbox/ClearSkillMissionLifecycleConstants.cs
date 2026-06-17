// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill mission lifecycle PC facts.
// PC source of truth: 00.src-tinh-kiem/server1/script/missions/clearskill/{head,mission,timer,camperman,playerdeath}.lua
// This is a pure proof model only; executor/runtime integration is intentionally out of scope.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Deterministic constants and lifecycle operation plans copied from the PC ClearSkill Lua mission.
    /// </summary>
    public static class ClearSkillMissionLifecycleConstants
    {
        public const int MissionId = 10;
        public const int PkTimeTicks = 18 * 60 * 5;
        public const int MissionTimerId = 20;
        public const int CampNpcTemplateId = 68;
        public const int CampNpcLevel = 10;
        public const int CampNpcMpsX = 1582 * 32;
        public const int CampNpcMpsY = 3303 * 32;
        public const int CampNpcDirection = 1;
        public const int RuntimeSubWorldArgument = -2; // sentinel for PC SubWorld runtime variable, not a map id
        public const int MissionVActiveFlagSlot = 1;
        public const int MissionVCampNpcIdSlot = 2;
        public const int MissionVFree = 0;
        public const int MissionVActive = 1;
        public const int CombatTempTaskId = 200;
        public const int OnLeaveMissionGroup = 0;
        public const string CampNpcName = "Sứ giả bang phái";
        public const string CampNpcScript = @"\script\missions\clearskill\camperman.lua";

        public static IReadOnlyList<LifecycleOperation> PlanInitMission()
        {
            return new[]
            {
                LifecycleOperation.ThreeInts("StartMissionTimer", MissionId, MissionTimerId, PkTimeTicks),
                LifecycleOperation.SixIntsText("AddNpc", CampNpcTemplateId, CampNpcLevel, RuntimeSubWorldArgument, CampNpcMpsX, CampNpcMpsY, CampNpcDirection, CampNpcName),
                LifecycleOperation.TwoInts("SetMissionV", MissionVCampNpcIdSlot, LifecycleOperation.ResultOfPreviousOperation),
                LifecycleOperation.IntText("SetNpcScript", LifecycleOperation.ResultOfPreviousOperation, CampNpcScript),
                LifecycleOperation.TwoInts("SetMissionV", MissionVActiveFlagSlot, MissionVActive),
            };
        }

        public static IReadOnlyList<LifecycleOperation> PlanEndMission(int campNpcId)
        {
            var operations = new List<LifecycleOperation>
            {
                LifecycleOperation.NoArgs("GameOver"),
                LifecycleOperation.TwoInts("SetMissionV", MissionVActiveFlagSlot, MissionVFree),
                LifecycleOperation.OneInt("GetMissionV", MissionVCampNpcIdSlot),
            };

            if (campNpcId > 0)
            {
                operations.Add(LifecycleOperation.OneInt("DelNpc", campNpcId));
            }

            return operations;
        }

        public static IReadOnlyList<LifecycleOperation> PlanOnLeave(int roleIndex, int missionPlayerCount)
        {
            var operations = new List<LifecycleOperation>
            {
                LifecycleOperation.OneInt("SetPlayerIndex", roleIndex),
                LifecycleOperation.OneInt("SetLogoutRV", 1),
                LifecycleOperation.Text("SetDeathScript", string.Empty),
                LifecycleOperation.OneInt("SetPKFlag", 0),
                LifecycleOperation.OneInt("ForbidChangePK", 1),
                LifecycleOperation.TwoInts("SetTaskTemp", CombatTempTaskId, 0),
                LifecycleOperation.TwoInts("GetMSPlayerCount", MissionId, OnLeaveMissionGroup),
            };

            if (missionPlayerCount <= 1)
            {
                operations.Add(LifecycleOperation.OneInt("CloseMission", MissionId));
            }

            return operations;
        }

        public static IReadOnlyList<LifecycleOperation> PlanTimer()
        {
            return new[] { LifecycleOperation.OneInt("CloseMission", MissionId) };
        }
    }

    public sealed class LifecycleOperation
    {
        public const int ResultOfPreviousOperation = -1;

        private LifecycleOperation(string name, int[] intArgs, string textArg)
        {
            Name = name;
            IntArgs = intArgs ?? Array.Empty<int>();
            TextArg = textArg;
        }

        public string Name { get; }
        public int[] IntArgs { get; }
        public string TextArg { get; }

        public static LifecycleOperation NoArgs(string name) => new LifecycleOperation(name, Array.Empty<int>(), null);
        public static LifecycleOperation OneInt(string name, int a) => new LifecycleOperation(name, new[] { a }, null);
        public static LifecycleOperation TwoInts(string name, int a, int b) => new LifecycleOperation(name, new[] { a, b }, null);
        public static LifecycleOperation ThreeInts(string name, int a, int b, int c) => new LifecycleOperation(name, new[] { a, b, c }, null);
        public static LifecycleOperation SixIntsText(string name, int a, int b, int c, int d, int e, int f, string text) => new LifecycleOperation(name, new[] { a, b, c, d, e, f }, text);
        public static LifecycleOperation IntText(string name, int a, string text) => new LifecycleOperation(name, new[] { a }, text);
        public static LifecycleOperation Text(string name, string text) => new LifecycleOperation(name, Array.Empty<int>(), text);
    }
}
