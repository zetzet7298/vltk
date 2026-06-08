// -----------------------------------------------------------------------------
// VLTK Mobile — CityWarJoinRouter trap action tests.
// PC sources:
// - script/missions/citywar_city/zhongzhuan_map/trap.lua
// - script/missions/citywar_city/head.lua
// - script/missions/citywar_city/camper.lua
// - script/missions/citywar_global/head.lua
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class CityWarJoinRouterTests
    {
        private sealed class FakeTrapTravelHost : ITrapTravelHost
        {
            public bool hasMap = true;
            public int currentMapId = 222;
            public int cityArea = 3;
            public int mapId = -1;
            public Vector2 position;
            public readonly Dictionary<int, int> missionValues = new Dictionary<int, int>();
            public readonly Dictionary<int, int> taskValues = new Dictionary<int, int>();
            public readonly Dictionary<int, int> taskTempValues = new Dictionary<int, int>();
            public readonly Dictionary<int, int> itemCounts = new Dictionary<int, int>();
            public int curCamp;
            public int originalCamp;
            public int logoutRv = -1;
            public int pkFlag = -1;
            public int forbidChangePk = -1;
            public int punish = -1;
            public int createTeam = -1;
            public int fightState = -1;
            public string deathScript;
            public bool leftTeam;

            public bool HasMap(int targetMapId) => hasMap && targetMapId == 221;
            public int GetCurrentMapId() => currentMapId;
            public bool TryGetPlayerReviveWorld(out int targetMapId, out Vector2 worldPosition)
            {
                targetMapId = 0;
                worldPosition = default;
                return false;
            }
            public int GetPlayerLevel() => 1;
            public int GetPlayerSeriesId() => 0;
            public int GetPlayerSex() => 0;
            public string GetPlayerName() => "Người chơi";
            public long GetCurrentDateYmdHm() => 202606090900;
            public int RandomIntInclusive(int minInclusive, int maxInclusive) => minInclusive;
            public int GetTaskValue(int taskId) => taskValues.TryGetValue(taskId, out var value) ? value : 0;
            public void SetTaskValue(int taskId, int value) => taskValues[taskId] = value;
            public int GetTaskTempValue(int taskId) => taskTempValues.TryGetValue(taskId, out var value) ? value : 0;
            public int GetMissionValue(int missionVarId) => missionValues.TryGetValue(missionVarId, out var value) ? value : 0;
            public int GetMissionPlayerGroup(int missionId) => 0;
            public bool HasSummonedPartner() => false;
            public int GetPartnerMasterTaskState(int masterTaskId) => 0;
            public bool HaveItem(int pcQuestKeyDetailType, int minCount)
                => itemCounts.TryGetValue(pcQuestKeyDetailType, out var count) && count >= minCount;
            public bool DelItem(int pcQuestKeyDetailType, int count)
            {
                if (!itemCounts.TryGetValue(pcQuestKeyDetailType, out var oldCount) || oldCount < count) return false;
                int newCount = oldCount - count;
                if (newCount <= 0) itemCounts.Remove(pcQuestKeyDetailType);
                else itemCounts[pcQuestKeyDetailType] = newCount;
                return true;
            }
            public int GetCurCamp() => curCamp;
            public int GetCamp() => originalCamp;
            public int GetBattleRank() => 0;
            public int GetFightState() => fightState;
            public int GetPlayerFactionId() => 0;
            public int GetTimerId() => 0;
            public void SetTimer(int ticks, int timerId) { }
            public int GetCityArea() => cityArea;
            public string GetCityOwnerMasterName(int cityId) => string.Empty;
            public string GetCityStatusSummary(int cityId) => string.Empty;
            public string GetCitySealInfo() => string.Empty;
            public void NewWorld(int targetMapId, Vector2 worldPosition)
            {
                mapId = targetMapId;
                position = worldPosition;
            }
            public void SetPos(Vector2 worldPosition) => position = worldPosition;
            public void SetFightState(int nextFightState) => fightState = nextFightState;
            public void SetCurCamp(int nextCamp) => curCamp = nextCamp;
            public void SetLogoutRv(int value) => logoutRv = value;
            public void SetPkFlag(int value) => pkFlag = value;
            public void ForbidChangePk(int value) => forbidChangePk = value;
            public void SetPunish(int value) => punish = value;
            public void SetCreateTeam(int value) => createTeam = value;
            public void SetTaskTemp(int taskId, int value) => taskTempValues[taskId] = value;
            public void SetDeathScript(string scriptPath) => deathScript = scriptPath;
            public void LeaveTeam() => leftTeam = true;
            public void SetRevPos(int mapId, int reviveId) { }
        }

        private sealed class FakeSideEffects : ITrapActionSideEffects
        {
            public readonly List<string> messages = new List<string>();
            public void PostMessage(string message) => messages.Add(message);
            public void AddStation(int stationId) { }
            public void AddTermini(int terminiId) { }
            public void SetProtectTime(int ticks) { }
            public void AddSkillState(int skillStateId, int level, int durationTicks) { }
            public void AddNote(string note) { }
            public void ApplyCityWarRankEffect(int rank) { }
        }

        [Test]
        public void ExistingTicket_OnMap222_JoinsDefenderCampFromPcConstants()
        {
            var host = ActiveHost(currentMapId: 222);
            host.taskValues[232] = 9876;
            host.taskValues[231] = 1;

            Assert.IsTrue(Execute(host, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(221, host.mapId);
            Assert.AreEqual(1, host.curCamp);
            AssertJoinCampSideEffects(host);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1533 * 32, 3211 * 32), host.position);
        }

        [Test]
        public void LoadFromStreamingAssets_RoutesPcDeferredTrapId()
        {
            var catalog = PcTrapActionCatalogRuntime.LoadFromStreamingAssets();
            var host = ActiveHost(currentMapId: 222);
            host.taskValues[232] = 9876;
            host.taskValues[231] = 1;
            var executor = new PcTrapActionExecutor(catalog, host, new FakeSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 907670210u, trapIdHex = "0x3619F2C2" }, out var result));

            Assert.IsTrue(result.success, result.detail);
            Assert.AreEqual(221, host.mapId);
            Assert.AreEqual(1, host.curCamp);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1533 * 32, 3211 * 32), host.position);
        }

        [Test]
        public void ExistingTicket_NonMap222_JoinsAttackCampFromPcConstants()
        {
            var host = ActiveHost(currentMapId: 223);
            host.taskValues[232] = 9876;
            host.taskValues[231] = 2;

            Assert.IsTrue(Execute(host, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(221, host.mapId);
            Assert.AreEqual(2, host.curCamp);
            AssertJoinCampSideEffects(host);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1903 * 32, 3608 * 32), host.position);
        }

        [Test]
        public void ActiveWar_WithOddCityCard_ConsumesCardSetsPcTasksAndJoinsAttackCamp()
        {
            var host = ActiveHost(currentMapId: 222);
            host.cityArea = 3;
            host.itemCounts[367] = 1;

            Assert.IsTrue(Execute(host, out var result));

            Assert.IsTrue(result.success);
            Assert.IsFalse(host.itemCounts.ContainsKey(367));
            Assert.AreEqual(6, host.taskValues[230]);
            Assert.AreEqual(2, host.taskValues[231]);
            Assert.AreEqual(9876, host.taskValues[232]);
            Assert.AreEqual(3, host.taskValues[233]);
            Assert.AreEqual(221, host.mapId);
            Assert.AreEqual(2, host.curCamp);
            AssertJoinCampSideEffects(host);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1903 * 32, 3608 * 32), host.position);
        }

        [Test]
        public void ActiveWar_WithEvenCityCard_ConsumesCardSetsPcTasksAndJoinsDefenderCamp()
        {
            var host = ActiveHost(currentMapId: 223);
            host.cityArea = 3;
            host.itemCounts[366] = 1;

            Assert.IsTrue(Execute(host, out var result));

            Assert.IsTrue(result.success);
            Assert.IsFalse(host.itemCounts.ContainsKey(366));
            Assert.AreEqual(6, host.taskValues[230]);
            Assert.AreEqual(1, host.taskValues[231]);
            Assert.AreEqual(9876, host.taskValues[232]);
            Assert.AreEqual(3, host.taskValues[233]);
            Assert.AreEqual(221, host.mapId);
            Assert.AreEqual(1, host.curCamp);
            AssertJoinCampSideEffects(host);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1533 * 32, 3211 * 32), host.position);
        }

        [Test]
        public void ActiveWar_NoCard_PostsPcMessageAndReturnsToOuterTransferPosition()
        {
            var host = ActiveHost(currentMapId: 222);
            var sideEffects = new FakeSideEffects();

            Assert.IsTrue(Execute(host, out var result, sideEffects));

            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1613 * 32, 3185 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Ngươi không có lệnh bài làm sao vào được! Đi đi!" }, sideEffects.messages);
        }

        [Test]
        public void MissionNotStarted_PostsPcWaitingMessageWithoutWarp()
        {
            var host = new FakeTrapTravelHost { currentMapId = 222 };
            var sideEffects = new FakeSideEffects();

            Assert.IsTrue(Execute(host, out var result, sideEffects));

            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, host.mapId);
            CollectionAssert.AreEqual(new[] { "Phe ta hiện đang tập hợp chuẩn bị vào đấu trường! Xin mọi người hãy bình tĩnh, chuẩn bị tinh thần!" }, sideEffects.messages);
        }

        [Test]
        public void MissingMissionMap_FailsWithoutApplyingSideEffects()
        {
            var host = ActiveHost(currentMapId: 222);
            host.hasMap = false;

            Assert.IsTrue(Execute(host, out var result));

            Assert.IsFalse(result.success);
            Assert.AreEqual(-1, host.mapId);
            Assert.IsFalse(host.leftTeam);
        }

        [Test]
        public void MissingTravelHost_ReturnsFailureInsteadOfThrowing()
        {
            var executor = new PcTrapActionExecutor(CreateCatalog(), null, new FakeSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 907670210, trapIdHex = "0x3619F2C2" }, out var result));

            Assert.IsFalse(result.success);
            StringAssert.Contains("trap travel host unavailable", result.detail);
        }

        private static FakeTrapTravelHost ActiveHost(int currentMapId)
        {
            var host = new FakeTrapTravelHost { currentMapId = currentMapId };
            host.missionValues[1] = 1;
            host.missionValues[99] = 9876;
            return host;
        }

        private static bool Execute(FakeTrapTravelHost host, out TrapActionExecutionResult result, FakeSideEffects sideEffects = null)
        {
            var executor = new PcTrapActionExecutor(CreateCatalog(), host, sideEffects ?? new FakeSideEffects());
            return executor.TryExecute(new TrapDefinition { trapId = 907670210, trapIdHex = "0x3619F2C2" }, out result);
        }

        private static PcTrapActionCatalogFile CreateCatalog()
        {
            return new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 907670210,
                        trapIdHex = "0x3619F2C2",
                        scriptPath = @"\script\missions\citywar_city\zhongzhuan_map\trap.lua",
                        sourceRelPath = @"script\missions\citywar_city\zhongzhuan_map\trap.lua",
                        actionKind = "CityWarJoinRouter",
                        targetMapId = 221,
                        enterCellX = 1533,
                        enterCellY = 3211,
                        exitCellX = 1903,
                        exitCellY = 3608,
                        blockedCellX = 1613,
                        blockedCellY = 3185,
                    }
                }
            };
        }

        private static void AssertJoinCampSideEffects(FakeTrapTravelHost host)
        {
            Assert.IsTrue(host.leftTeam);
            Assert.AreEqual(1, host.taskTempValues[242]);
            Assert.AreEqual(1, host.taskTempValues[200]);
            Assert.AreEqual(1, host.logoutRv);
            Assert.AreEqual(0, host.punish);
            Assert.AreEqual(0, host.createTeam);
            Assert.AreEqual(1, host.pkFlag);
            Assert.AreEqual(1, host.forbidChangePk);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(@"\script\missions\citywar_city\playerdeath.lua", host.deathScript);
        }
    }
}
