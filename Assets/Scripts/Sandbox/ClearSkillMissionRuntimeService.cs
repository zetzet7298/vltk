// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill mission runtime semantics model.
// PC source of truth:
// - script/missions/clearskill/head.lua
// - script/missions/clearskill/testhole.lua
// - script/missions/clearskill/mission.lua
// - script/global/特殊用地/梦境/trap/梦境to梦境山洞1..4.lua
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Pure C# model for the PC ClearSkill TeamEnterHole/JoinHole decision plan.
    /// It deliberately emits deterministic operations instead of touching Unity/player state.
    /// </summary>
    public sealed class ClearSkillMissionRuntimeService
    {
        public const int MissionId = 10;
        public const int JoinStateTaskId = 100;
        public const int CombatTempTaskId = 200;
        public const int MaxMemberCount = 20;
        public const int MaxTestMapCount = 10;
        public const int CampManMpsX = 1582 * 32;
        public const int CampManMpsY = 3303 * 32;
        public const string DeathScript = @"\script\missions\clearskill\playerdeath.lua";

        public static readonly int[] ClearMapIds = { 242, 243, 244, 245, 246, 247, 248 };
        public static readonly int[] TestMapBeginIds = { 249, 259, 269, 279, 289, 299, 309 };
        public static readonly HoleCoord[] TestHoleCoords =
        {
            new HoleCoord(1621, 3236), new HoleCoord(1533, 3235),
            new HoleCoord(1520, 3352), new HoleCoord(1670, 3347),
        };

        public TeamEnterHolePlan PlanTeamEnterHole(TeamEnterHoleInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (input.TrapId < 1 || input.TrapId > TestHoleCoords.Length)
                return TeamEnterHolePlan.Reject("invalid_trap_id");
            if (!input.IsCaptain)
                return TeamEnterHolePlan.Reject("not_captain");
            if (!input.IsDisabledUseTownPortal)
                return TeamEnterHolePlan.Reject("csp_check_valid_failed");

            int size = input.TeamMembers == null ? 0 : input.TeamMembers.Count;
            if (size < 2 || size > MaxMemberCount)
                return TeamEnterHolePlan.Reject("team_size_out_of_range");

            int cityIndex = GetCityIndexByClearMap(input.CurrentMapId);
            if (cityIndex <= 0)
                return TeamEnterHolePlan.Reject("current_map_not_clear_map");

            int freeMap = GetFreeTestMapId(cityIndex, input.TestMaps);
            if (freeMap <= 0)
                return TeamEnterHolePlan.Reject("no_free_test_map", cityIndex);

            var plan = TeamEnterHolePlan.Accept(cityIndex, freeMap, input.TrapId);
            plan.MissionOperations.Add(RuntimeOperation.OneInt("OpenMission", MissionId));
            plan.MissionOperations.Add(RuntimeOperation.OneInt("RunMission", MissionId));

            HoleCoord coord = TestHoleCoords[input.TrapId - 1];
            for (int i = 0; i < input.TeamMembers.Count; i++)
            {
                TeamMemberState member = input.TeamMembers[i];
                if (member == null) continue;
                if (!member.IsDisabledUseTownPortal) continue;
                if (GetCityIndexByClearMap(member.CurrentMapId) != cityIndex) continue;
                plan.JoinHolePlans.Add(CreateJoinHolePlan(member, freeMap, coord));
            }

            return plan;
        }

        public static int GetCityIndexByClearMap(int mapId)
        {
            for (int i = 0; i < ClearMapIds.Length; i++)
            {
                if (ClearMapIds[i] == mapId) return i + 1;
            }
            return -1;
        }

        public static int GetFreeTestMapId(int cityIndex, IReadOnlyList<TestMapState> maps)
        {
            if (cityIndex < 1 || cityIndex > TestMapBeginIds.Length || maps == null) return -1;
            int begin = TestMapBeginIds[cityIndex - 1];
            for (int offset = 0; offset < MaxTestMapCount; offset++)
            {
                int mapId = begin + offset;
                for (int i = 0; i < maps.Count; i++)
                {
                    TestMapState state = maps[i];
                    if (state != null && state.MapId == mapId && state.IsLoaded && state.MissionV1 == 0)
                        return mapId;
                }
            }
            return -1;
        }

        private static JoinHolePlan CreateJoinHolePlan(TeamMemberState member, int testMap, HoleCoord coord)
        {
            var plan = new JoinHolePlan(member.PlayerId, member.IsActivePlayer, testMap, coord.X, coord.Y);
            plan.Operations.Add(RuntimeOperation.NoArgs("LeaveTeam"));
            plan.Operations.Add(RuntimeOperation.ThreeInts("NewWorld", testMap, coord.X, coord.Y));
            plan.Operations.Add(RuntimeOperation.TwoInts("AddMSPlayer", MissionId, 1));
            plan.Operations.Add(RuntimeOperation.TwoInts("SetTaskTemp", JoinStateTaskId, 1));
            plan.Operations.Add(RuntimeOperation.TwoInts("SetTaskTemp", CombatTempTaskId, 1));
            plan.Operations.Add(RuntimeOperation.OneInt("SetFightState", 1));
            plan.Operations.Add(RuntimeOperation.OneInt("SetLogoutRV", 1));
            plan.Operations.Add(RuntimeOperation.Text("SetDeathScript", DeathScript));
            plan.Operations.Add(RuntimeOperation.OneInt("SetPunish", 0));
            plan.Operations.Add(RuntimeOperation.ThreeInts("SetTempRevPos", testMap, CampManMpsX, CampManMpsY));
            plan.Operations.Add(RuntimeOperation.OneInt("ForbidChangePK", 0));
            plan.Operations.Add(RuntimeOperation.OneInt("SetPKFlag", 1));
            return plan;
        }
    }

    public sealed class TeamEnterHoleInput
    {
        public int TrapId;
        public bool IsCaptain;
        public bool IsDisabledUseTownPortal;
        public int CurrentMapId;
        public List<TeamMemberState> TeamMembers = new List<TeamMemberState>();
        public List<TestMapState> TestMaps = new List<TestMapState>();
    }

    public sealed class TeamMemberState
    {
        public int PlayerId;
        public bool IsActivePlayer;
        public bool IsDisabledUseTownPortal;
        public int CurrentMapId;

        public TeamMemberState(int playerId, bool isActivePlayer, bool isDisabledUseTownPortal, int currentMapId)
        {
            PlayerId = playerId;
            IsActivePlayer = isActivePlayer;
            IsDisabledUseTownPortal = isDisabledUseTownPortal;
            CurrentMapId = currentMapId;
        }
    }

    public sealed class TestMapState
    {
        public int MapId;
        public bool IsLoaded;
        public int MissionV1;

        public TestMapState(int mapId, bool isLoaded, int missionV1)
        {
            MapId = mapId;
            IsLoaded = isLoaded;
            MissionV1 = missionV1;
        }
    }

    public sealed class TeamEnterHolePlan
    {
        public bool Accepted;
        public string ReasonCode;
        public int CityIndex;
        public int TestMapId;
        public int TrapId;
        public readonly List<RuntimeOperation> MissionOperations = new List<RuntimeOperation>();
        public readonly List<JoinHolePlan> JoinHolePlans = new List<JoinHolePlan>();

        public static TeamEnterHolePlan Reject(string reasonCode, int cityIndex = -1)
        {
            return new TeamEnterHolePlan { Accepted = false, ReasonCode = reasonCode, CityIndex = cityIndex, TestMapId = -1 };
        }

        public static TeamEnterHolePlan Accept(int cityIndex, int testMapId, int trapId)
        {
            return new TeamEnterHolePlan
            {
                Accepted = true,
                ReasonCode = "accepted",
                CityIndex = cityIndex,
                TestMapId = testMapId,
                TrapId = trapId,
            };
        }
    }

    public sealed class JoinHolePlan
    {
        public readonly int PlayerId;
        public readonly bool IsActivePlayer;
        public readonly int TestMapId;
        public readonly int EnterX;
        public readonly int EnterY;
        public readonly List<RuntimeOperation> Operations = new List<RuntimeOperation>();

        public JoinHolePlan(int playerId, bool isActivePlayer, int testMapId, int enterX, int enterY)
        {
            PlayerId = playerId;
            IsActivePlayer = isActivePlayer;
            TestMapId = testMapId;
            EnterX = enterX;
            EnterY = enterY;
        }
    }

    public sealed class RuntimeOperation
    {
        public readonly string Name;
        public readonly int[] IntArgs;
        public readonly string TextArg;

        private RuntimeOperation(string name, int[] intArgs, string textArg)
        {
            Name = name;
            IntArgs = intArgs ?? Array.Empty<int>();
            TextArg = textArg ?? string.Empty;
        }

        public static RuntimeOperation NoArgs(string name)
        {
            return new RuntimeOperation(name, Array.Empty<int>(), string.Empty);
        }

        public static RuntimeOperation OneInt(string name, int a)
        {
            return new RuntimeOperation(name, new[] { a }, string.Empty);
        }

        public static RuntimeOperation TwoInts(string name, int a, int b)
        {
            return new RuntimeOperation(name, new[] { a, b }, string.Empty);
        }

        public static RuntimeOperation ThreeInts(string name, int a, int b, int c)
        {
            return new RuntimeOperation(name, new[] { a, b, c }, string.Empty);
        }

        public static RuntimeOperation Text(string name, string text)
        {
            return new RuntimeOperation(name, Array.Empty<int>(), text);
        }
    }

    public readonly struct HoleCoord
    {
        public readonly int X;
        public readonly int Y;

        public HoleCoord(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
