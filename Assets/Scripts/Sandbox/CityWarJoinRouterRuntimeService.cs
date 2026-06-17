// -----------------------------------------------------------------------------
// VLTK Mobile — standalone CityWarJoinRouter runtime semantics model.
// PC source: 00.src-tinh-kiem Server 6.0/server/home_jxser/server1/script/missions/
// citywar_city/zhongzhuan_map/trap.lua, citywar_city/head.lua,
// citywar_city/camper.lua, citywar_global/head.lua.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Pure C# plan builder for the PC CityWarJoinRouter Lua subset. It returns
    /// deterministic side-effect descriptions; it does not touch Unity runtime state.
    /// </summary>
    public sealed class CityWarJoinRouterRuntimeService
    {
        public const int MissionId = 6;
        public const int MissionStateVar = 1;
        public const int MissionKeyVar = 99;
        public const int TaskId = 230;
        public const int TaskValue = 231;
        public const int TaskKey = 232;
        public const int TaskCityId = 233;
        public const int SeriesSkillRealTask = 2305;
        public const int LegacyTask1017 = 1017;
        public const int MissionMapId = 221;
        public const int DefenderTransferMapId = 222;
        public const int JoinStateTempTask = 242;
        public const int CityWarTempTask = 200;
        public const int DefenderCamp = 1;
        public const int AttackerCamp = 2;
        public const int DirectJoinType = 1;
        public const int CardJoinType = 2;
        public const string DeathScript = @"\script\missions\citywar_city\playerdeath.lua";
        public const string WaitingMessage = "Phe ta hiện đang tập hợp chuẩn bị vào đấu trường! Xin mọi người hãy bình tĩnh, chuẩn bị tinh thần!";
        public const string NoCardMessage = "Ngươi không có lệnh bài làm sao vào được! Đi đi!";
        public const string TooNewTongMessage = "Thời gian bạn gia nhập bang hội quá ngắn (chưa hết 1 ngày), không thể tham gia chiến đấu!";
        public const string ExpiredCardMessageTemplate = "Lệnh bài này từ {0} ngày trước đã hết hạn, không thể dùng được";

        public static readonly int[] CardTab = { 363, 362, 355, 354, 367, 366, 359, 358, 357, 356, 365, 364, 361, 360 };
        public static readonly CityWarCell DefenderSpawn = new CityWarCell(MissionMapId, 1533, 3211);
        public static readonly CityWarCell AttackerSpawn = new CityWarCell(MissionMapId, 1903, 3608);
        public static readonly CityWarCell OuterPosition = new CityWarCell(0, 1613, 3185);

        public CityWarJoinPlan BuildPlan(CityWarJoinInput input)
        {
            var plan = new CityWarJoinPlan();
            if (input == null)
            {
                plan.Fail("input unavailable");
                return plan;
            }

            plan.RouteCamp = RouteCamp(input.CurrentMapId);
            if (!input.MissionMapAvailable)
            {
                plan.Fail("mission map 221 unavailable");
                return plan;
            }

            if (input.MissionState == 0)
            {
                plan.AddMessage(WaitingMessage);
                plan.Detail = "GetMissionV(MS_STATE)==0 -> Say waiting, no join";
                return plan;
            }

            return CheckAndJoin(plan.RouteCamp, input, plan);
        }

        public static int RouteCamp(int currentMapId)
        {
            return currentMapId == DefenderTransferMapId ? DefenderCamp : AttackerCamp;
        }

        public static int GetCardItemIdForCity(int cityId, bool oddCard)
        {
            int index = cityId * 2 - (oddCard ? 1 : 0);
            return index >= 1 && index <= CardTab.Length ? CardTab[index - 1] : 0;
        }

        private CityWarJoinPlan CheckAndJoin(int camp, CityWarJoinInput input, CityWarJoinPlan plan)
        {
            if (string.Equals(input.TongName, input.GetMissionS(camp), StringComparison.Ordinal) && !string.IsNullOrEmpty(input.TongName))
            {
                if (input.JoinTongMinutes >= 1440)
                {
                    if (input.TaskKeyValue != input.MissionKey)
                    {
                        AddBattleReset(plan, includeBattleCamp: true);
                        plan.AddSetTask(TaskKey, input.MissionKey);
                        plan.AddLeaveBattle();
                        plan.AddClearPlayerData();
                    }

                    AddJoinCamp(plan, camp, DirectJoinType);
                    plan.Detail = "Tong owns route camp and join time >= 1440 -> JoinCamp(Camp,1)";
                    return plan;
                }

                bool joinedWithCard = JoinWithCard(camp, ui: false, input, plan);
                if (!joinedWithCard)
                    plan.AddMessage(TooNewTongMessage);
                if (string.IsNullOrEmpty(plan.Detail))
                    plan.Detail = "Tong join time < 1440 -> card fallback";
                return plan;
            }

            JoinWithCard(camp, ui: true, input, plan);
            if (string.IsNullOrEmpty(plan.Detail))
                plan.Detail = "non-owner Tong path -> card fallback";
            return plan;
        }

        private bool JoinWithCard(int routeCamp, bool ui, CityWarJoinInput input, CityWarJoinPlan plan)
        {
            if (input.TaskKeyValue == input.MissionKey && input.TaskCampValue == routeCamp)
            {
                AddJoinCamp(plan, routeCamp, CardJoinType);
                plan.Detail = "existing TV_TASKKEY/TV_TASKVALUE ticket -> JoinCamp(Camp,2)";
                return true;
            }

            if (input.WarCityId == 0)
            {
                plan.Fail("GetWarOfCity()==0");
                return false;
            }

            int camp = DefenderCamp;
            int cardItemId = 0;
            int oddCard = GetCardItemIdForCity(input.WarCityId, oddCard: true);
            int evenCard = GetCardItemIdForCity(input.WarCityId, oddCard: false);

            if (input.GetItemCount(oddCard) >= 1)
            {
                cardItemId = oddCard;
                camp = AttackerCamp;
            }
            else if (input.GetItemCount(evenCard) >= 1)
            {
                cardItemId = evenCard;
                camp = DefenderCamp;
            }
            else
            {
                if (ui)
                    plan.AddMessage(NoCardMessage);
                plan.AddSetPos(OuterPosition);
                plan.Detail = "no city-war card -> SetPos outer, no join";
                return false;
            }

            int cardLifeDays = (int)Math.Floor(input.GetItemLifeMinutes(cardItemId) / 1440.0);
            if (cardLifeDays > 5)
            {
                plan.AddDeleteItem(cardItemId);
                plan.AddMessage(string.Format(ExpiredCardMessageTemplate, cardLifeDays));
                plan.AddSetPos(OuterPosition);
                plan.Detail = "expired city-war card life floor(life/1440)>5 -> DelItem, Say, SetPos outer";
                return false;
            }

            plan.AddDeleteItem(cardItemId);
            AddBattleReset(plan, includeBattleCamp: false);
            plan.AddSetTask(TaskId, MissionId);
            plan.AddSetTask(TaskKey, input.MissionKey);
            plan.AddSetTask(TaskValue, camp);
            plan.AddSetTask(TaskCityId, input.WarCityId);
            plan.AddLeaveBattle();
            plan.AddClearPlayerData();
            AddJoinCamp(plan, camp, CardJoinType);
            plan.Detail = string.Format("card {0} -> Camp {1}, JoinCamp(Camp,2)", cardItemId, camp);
            return true;
        }

        private static void AddBattleReset(CityWarJoinPlan plan, bool includeBattleCamp)
        {
            plan.AddBattleData("PL_KEYNUMBER", 0);
            plan.AddBattleData("PL_TOTALPOINT", 0);
            plan.AddSetTask(LegacyTask1017, 0);
            plan.AddSetTask(SeriesSkillRealTask, 0);
            if (includeBattleCamp)
                plan.AddBattleData("PL_BATTLECAMP", 0);
        }

        private static void AddJoinCamp(CityWarJoinPlan plan, int camp, int type)
        {
            CityWarCell spawn = camp == DefenderCamp ? DefenderSpawn : AttackerSpawn;
            int missionGroup = type == DirectJoinType ? camp : camp + 2;

            plan.JoinCamp = new CityWarJoinCampEffect(camp, type, missionGroup, spawn);
            plan.AddAction(CityWarAction.LeaveTeam());
            plan.AddAction(CityWarAction.AddMissionPlayer(MissionId, missionGroup));
            plan.AddSetTaskTemp(JoinStateTempTask, 1);
            plan.AddAction(CityWarAction.SetCurCamp(camp));
            plan.AddSetTaskTemp(CityWarTempTask, 1);
            plan.AddAction(CityWarAction.SetLogoutRv(1));
            plan.AddAction(CityWarAction.SetPunish(0));
            plan.AddAction(CityWarAction.SetCreateTeam(0));
            plan.AddAction(CityWarAction.SetPkFlag(1));
            plan.AddAction(CityWarAction.ForbidChangePk(1));
            plan.AddAction(CityWarAction.SetRevPosFromPlayerRev());
            plan.AddAction(CityWarAction.SetDeathScript(DeathScript));
            plan.AddAction(CityWarAction.SetFightState(0));
            plan.AddAction(CityWarAction.SetTempRevPos(spawn));
            plan.AddNewWorld(spawn);
            plan.AddAction(CityWarAction.BattleBroadcasts());
            plan.AddAction(CityWarAction.JudgeTitle());
        }
    }

    public sealed class CityWarJoinInput
    {
        public int CurrentMapId = CityWarJoinRouterRuntimeService.DefenderTransferMapId;
        public bool MissionMapAvailable = true;
        public int MissionState;
        public int MissionKey;
        public int TaskKeyValue;
        public int TaskCampValue;
        public int WarCityId;
        public string TongName;
        public string DefenderMissionTongName;
        public string AttackerMissionTongName;
        public int JoinTongMinutes;
        public readonly Dictionary<int, int> ItemCounts = new Dictionary<int, int>();
        public readonly Dictionary<int, int> ItemLifeMinutes = new Dictionary<int, int>();

        public string GetMissionS(int camp)
        {
            return camp == CityWarJoinRouterRuntimeService.DefenderCamp ? DefenderMissionTongName : AttackerMissionTongName;
        }

        public int GetItemCount(int itemId)
        {
            if (itemId <= 0)
                return 0;
            int count;
            return ItemCounts.TryGetValue(itemId, out count) ? count : 0;
        }

        public int GetItemLifeMinutes(int itemId)
        {
            int minutes;
            return ItemLifeMinutes.TryGetValue(itemId, out minutes) ? minutes : 0;
        }
    }

    public sealed class CityWarJoinPlan
    {
        public bool Success = true;
        public string FailureReason;
        public string Detail;
        public int RouteCamp;
        public CityWarJoinCampEffect JoinCamp;
        public readonly List<CityWarAction> Actions = new List<CityWarAction>();
        public readonly List<CityWarTaskWrite> TaskWrites = new List<CityWarTaskWrite>();
        public readonly List<CityWarTaskWrite> TempTaskWrites = new List<CityWarTaskWrite>();
        public readonly List<string> Messages = new List<string>();
        public readonly List<int> DeletedItems = new List<int>();
        public readonly List<CityWarCell> SetPositions = new List<CityWarCell>();
        public readonly List<CityWarCell> NewWorlds = new List<CityWarCell>();
        public readonly List<CityWarBattleDataWrite> BattleDataWrites = new List<CityWarBattleDataWrite>();

        public bool Joined { get { return JoinCamp != null; } }

        public void Fail(string reason)
        {
            Success = false;
            FailureReason = reason;
            Detail = reason;
        }

        public void AddMessage(string message)
        {
            Messages.Add(message);
            AddAction(CityWarAction.Message(message));
        }

        public void AddDeleteItem(int itemId)
        {
            DeletedItems.Add(itemId);
            AddAction(CityWarAction.DeleteItem(itemId));
        }

        public void AddSetPos(CityWarCell cell)
        {
            SetPositions.Add(cell);
            AddAction(CityWarAction.SetPos(cell));
        }

        public void AddNewWorld(CityWarCell cell)
        {
            NewWorlds.Add(cell);
            AddAction(CityWarAction.NewWorld(cell));
        }

        public void AddSetTask(int taskId, int value)
        {
            TaskWrites.Add(new CityWarTaskWrite(taskId, value));
            AddAction(CityWarAction.SetTask(taskId, value));
        }

        public void AddSetTaskTemp(int taskId, int value)
        {
            TempTaskWrites.Add(new CityWarTaskWrite(taskId, value));
            AddAction(CityWarAction.SetTaskTemp(taskId, value));
        }

        public void AddBattleData(string name, int value)
        {
            BattleDataWrites.Add(new CityWarBattleDataWrite(name, value));
            AddAction(CityWarAction.BattleData(name, value));
        }

        public void AddLeaveBattle() { AddAction(CityWarAction.Named("BT_LeaveBattle")); }
        public void AddClearPlayerData() { AddAction(CityWarAction.Named("BT_ClearPlayerData")); }
        public void AddAction(CityWarAction action) { Actions.Add(action); }
    }

    public sealed class CityWarCell : IEquatable<CityWarCell>
    {
        public readonly int MapId;
        public readonly int CellX;
        public readonly int CellY;

        public CityWarCell(int mapId, int cellX, int cellY)
        {
            MapId = mapId;
            CellX = cellX;
            CellY = cellY;
        }

        public bool Equals(CityWarCell other)
        {
            return other != null && MapId == other.MapId && CellX == other.CellX && CellY == other.CellY;
        }

        public override bool Equals(object obj) { return Equals(obj as CityWarCell); }

        public override int GetHashCode()
        {
            unchecked { return (MapId * 397) ^ (CellX * 17) ^ CellY; }
        }

        public override string ToString()
        {
            return string.Format("{0}:{1},{2}", MapId, CellX, CellY);
        }
    }

    public sealed class CityWarTaskWrite
    {
        public readonly int TaskId;
        public readonly int Value;

        public CityWarTaskWrite(int taskId, int value)
        {
            TaskId = taskId;
            Value = value;
        }
    }

    public sealed class CityWarBattleDataWrite
    {
        public readonly string Name;
        public readonly int Value;

        public CityWarBattleDataWrite(string name, int value)
        {
            Name = name;
            Value = value;
        }
    }

    public sealed class CityWarJoinCampEffect
    {
        public readonly int Camp;
        public readonly int Type;
        public readonly int MissionGroup;
        public readonly CityWarCell Spawn;

        public CityWarJoinCampEffect(int camp, int type, int missionGroup, CityWarCell spawn)
        {
            Camp = camp;
            Type = type;
            MissionGroup = missionGroup;
            Spawn = spawn;
        }
    }

    public sealed class CityWarAction
    {
        public readonly string Kind;
        public readonly int TaskId;
        public readonly int Value;
        public readonly int ItemId;
        public readonly string Text;
        public readonly string Name;
        public readonly CityWarCell Cell;

        private CityWarAction(string kind, int taskId = 0, int value = 0, int itemId = 0, string text = null, string name = null, CityWarCell cell = null)
        {
            Kind = kind;
            TaskId = taskId;
            Value = value;
            ItemId = itemId;
            Text = text;
            Name = name;
            Cell = cell;
        }

        public static CityWarAction Named(string name) { return new CityWarAction(name); }
        public static CityWarAction Message(string text) { return new CityWarAction("Say", text: text); }
        public static CityWarAction DeleteItem(int itemId) { return new CityWarAction("DelItemEx", itemId: itemId); }
        public static CityWarAction SetPos(CityWarCell cell) { return new CityWarAction("SetPos", cell: cell); }
        public static CityWarAction NewWorld(CityWarCell cell) { return new CityWarAction("NewWorld", cell: cell); }
        public static CityWarAction SetTask(int taskId, int value) { return new CityWarAction("SetTask", taskId: taskId, value: value); }
        public static CityWarAction SetTaskTemp(int taskId, int value) { return new CityWarAction("SetTaskTemp", taskId: taskId, value: value); }
        public static CityWarAction BattleData(string name, int value) { return new CityWarAction("BT_SetData", value: value, name: name); }
        public static CityWarAction AddMissionPlayer(int missionId, int group) { return new CityWarAction("AddMSPlayer", taskId: missionId, value: group); }
        public static CityWarAction SetCurCamp(int camp) { return new CityWarAction("SetCurCamp", value: camp); }
        public static CityWarAction SetLogoutRv(int value) { return new CityWarAction("SetLogoutRV", value: value); }
        public static CityWarAction SetPunish(int value) { return new CityWarAction("SetPunish", value: value); }
        public static CityWarAction SetCreateTeam(int value) { return new CityWarAction("SetCreateTeam", value: value); }
        public static CityWarAction SetPkFlag(int value) { return new CityWarAction("SetPKFlag", value: value); }
        public static CityWarAction ForbidChangePk(int value) { return new CityWarAction("ForbidChangePK", value: value); }
        public static CityWarAction SetFightState(int value) { return new CityWarAction("SetFightState", value: value); }
        public static CityWarAction SetDeathScript(string path) { return new CityWarAction("SetDeathScript", text: path); }
        public static CityWarAction SetTempRevPos(CityWarCell cell) { return new CityWarAction("SetTempRevPos", cell: cell); }
        public static CityWarAction LeaveTeam() { return Named("LeaveTeam"); }
        public static CityWarAction SetRevPosFromPlayerRev() { return Named("SetRevPos(GetPlayerRev())"); }
        public static CityWarAction BattleBroadcasts() { return Named("BT_BroadcastCityWarState"); }
        public static CityWarAction JudgeTitle() { return Named("bt_JudgePLAddTitle"); }
    }
}
