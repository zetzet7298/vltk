using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class ClearSkillTeamEnterHoleTests
    {
        private const uint TrapId = 0xC1EA5001;

        [Test]
        public void Execute_JoinHoleSubset_UsesPcTrap2EntryAndFlags()
        {
            var host = new FakeTrapTravelHost
            {
                currentMapId = 242,
                allMapsLoaded = false,
                loadedMaps = { 249 },
            };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(CreateCatalog(trapIndex: 2), host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = TrapId }, out var result));

            Assert.IsTrue(result.success, result.detail);
            Assert.AreEqual(249, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1533 * 32, 3235 * 32), host.position);
            Assert.IsTrue(host.leftTeam);
            Assert.AreEqual(1, host.taskTempValues[100]);
            Assert.AreEqual(1, host.taskTempValues[200]);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(1, host.logoutRv);
            Assert.AreEqual(@"\script\missions\clearskill\playerdeath.lua", host.deathScript);
            Assert.AreEqual(0, host.punish);
            Assert.AreEqual(0, host.forbidChangePk);
            Assert.AreEqual(1, host.pkFlag);
            CollectionAssert.IsNotEmpty(sideEffects.notes);
            StringAssert.Contains("AddMSPlayer(MISSIONID=10,1)", sideEffects.notes[0]);
            StringAssert.Contains("SetTempRevPos(TestMap,50624,105696)", sideEffects.notes[0]);
            StringAssert.Contains("team-size 2..20", result.detail);
            StringAssert.Contains("TeamEnterHole(2)", result.detail);
        }

        [Test]
        public void Execute_SelectsFirstLoadedTestMapForCurrentClearMapCity()
        {
            var host = new FakeTrapTravelHost
            {
                currentMapId = 245,
                allMapsLoaded = false,
                loadedMaps = { 282 },
            };
            var executor = new PcTrapActionExecutor(CreateCatalog(trapIndex: 4), host);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = TrapId }, out var result));

            Assert.IsTrue(result.success, result.detail);
            Assert.AreEqual(282, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1670 * 32, 3347 * 32), host.position);
            StringAssert.Contains("cityIndex=4", result.detail);
        }

        [Test]
        public void Execute_CurrentMapOutsideClearSkillClearMap_NoWarp()
        {
            var host = new FakeTrapTravelHost
            {
                currentMapId = 907,
                allMapsLoaded = true,
            };
            var executor = new PcTrapActionExecutor(CreateCatalog(trapIndex: 1), host);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = TrapId }, out var result));

            Assert.IsTrue(result.success, result.detail);
            Assert.AreEqual(-1, host.mapId);
            Assert.IsFalse(host.leftTeam);
            StringAssert.Contains("CSP_GetCityIndexByClearMap(907)", result.detail);
        }

        [Test]
        public void Execute_NoLoadedTestMap_ReturnsFailureAndDoesNotApplySideEffects()
        {
            var host = new FakeTrapTravelHost
            {
                currentMapId = 242,
                allMapsLoaded = false,
            };
            var executor = new PcTrapActionExecutor(CreateCatalog(trapIndex: 3), host);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = TrapId }, out var result));

            Assert.IsFalse(result.success, result.detail);
            Assert.AreEqual(-1, host.mapId);
            Assert.AreEqual(-1, host.fightState);
            Assert.IsFalse(host.leftTeam);
            StringAssert.Contains("CSP_GetFreeTestMapID", result.detail);
        }

        private static PcTrapActionCatalogFile CreateCatalog(int trapIndex)
        {
            return new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = TrapId,
                        trapIdHex = "0xC1EA5001",
                        scriptPath = @"\script\global\特殊用地\梦境\trap\梦境to梦境山洞" + trapIndex + ".lua",
                        sourceRelPath = @"script\global\特殊用地\梦境\trap\梦境to梦境山洞" + trapIndex + ".lua",
                        actionKind = "ClearSkillTeamEnterHole",
                        trapIndex = trapIndex,
                        enterCellX = EnterCellX(trapIndex),
                        enterCellY = EnterCellY(trapIndex),
                        missionId = 10,
                        fightState = 1,
                        logoutRv = 1,
                        deathScript = @"\script\missions\clearskill\playerdeath.lua",
                        punish = 0,
                        forbidChangePk = 0,
                        pkFlag = 1,
                        setTaskTempId = 100,
                        setTaskTempValue = 1,
                        clearSkillClearMapIds = new[] { 242, 243, 244, 245, 246, 247, 248 },
                        clearSkillTestMapBeginIds = new[] { 249, 259, 269, 279, 289, 299, 309 },
                        clearSkillTestMapCount = 10,
                        source = "PC clearskill testhole.lua TeamEnterHole/JoinHole deterministic subset",
                    }
                }
            };
        }

        private static int EnterCellX(int trapIndex)
        {
            switch (trapIndex)
            {
                case 1: return 1621;
                case 2: return 1533;
                case 3: return 1520;
                case 4: return 1670;
                default: return 0;
            }
        }

        private static int EnterCellY(int trapIndex)
        {
            switch (trapIndex)
            {
                case 1: return 3236;
                case 2: return 3235;
                case 3: return 3352;
                case 4: return 3347;
                default: return 0;
            }
        }

        private sealed class FakeTrapTravelHost : ITrapTravelHost
        {
            public int mapId = -1;
            public Vector2 position;
            public int currentMapId = 242;
            public bool allMapsLoaded = true;
            public HashSet<int> loadedMaps = new HashSet<int>();
            public int fightState = -1;
            public int logoutRv = -1;
            public int pkFlag = -1;
            public int forbidChangePk = -1;
            public int punish = -1;
            public string deathScript;
            public bool leftTeam;
            public Dictionary<int, int> taskTempValues = new Dictionary<int, int>();
            public Dictionary<int, int> taskValues = new Dictionary<int, int>();

            public bool HasMap(int targetMapId) => allMapsLoaded || loadedMaps.Contains(targetMapId);
            public int GetCurrentMapId() => currentMapId;
            public bool TryGetPlayerReviveWorld(out int targetMapId, out Vector2 worldPosition)
            {
                targetMapId = 0;
                worldPosition = default;
                return false;
            }

            public int GetPlayerLevel() => 1;
            public int GetPlayerSeriesId() => -1;
            public int GetPlayerSex() => 0;
            public string GetPlayerName() => "Người chơi";
            public long GetCurrentDateYmdHm() => 202606090900;
            public int RandomIntInclusive(int minInclusive, int maxInclusive) => minInclusive;
            public int GetTaskValue(int taskId) => taskValues.TryGetValue(taskId, out var value) ? value : 0;
            public void SetTaskValue(int taskId, int value) => taskValues[taskId] = value;
            public int GetTaskTempValue(int taskId) => taskTempValues.TryGetValue(taskId, out var value) ? value : 0;
            public int GetMissionValue(int missionVarId) => 0;
            public int GetMissionPlayerGroup(int missionId) => 0;
            public bool HasSummonedPartner() => false;
            public int GetPartnerMasterTaskState(int masterTaskId) => 0;
            public bool HaveItem(int pcQuestKeyDetailType, int minCount) => false;
            public bool DelItem(int pcQuestKeyDetailType, int count) => false;
            public int GetCurCamp() => 0;
            public int GetCamp() => 0;
            public int GetBattleRank() => 0;
            public int GetFightState() => fightState;
            public int GetPlayerFactionId() => 0;
            public int GetTimerId() => 0;
            public void SetTimer(int ticks, int timerId) { }
            public int GetCityArea() => 0;
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
            public void SetCurCamp(int camp) { }
            public void SetLogoutRv(int value) => logoutRv = value;
            public void SetPkFlag(int value) => pkFlag = value;
            public void ForbidChangePk(int value) => forbidChangePk = value;
            public void SetPunish(int value) => punish = value;
            public void SetCreateTeam(int value) { }
            public void SetTaskTemp(int taskId, int value) => taskTempValues[taskId] = value;
            public void SetDeathScript(string scriptPath) => deathScript = scriptPath;
            public void LeaveTeam() => leftTeam = true;
            public void SetRevPos(int mapId, int reviveId) { }
        }

        private sealed class FakeTrapActionSideEffects : ITrapActionSideEffects
        {
            public readonly List<string> messages = new List<string>();
            public readonly List<int> stationIds = new List<int>();
            public readonly List<int> terminiIds = new List<int>();
            public readonly List<string> notes = new List<string>();
            public int protectTicks;
            public int skillStateId;
            public int skillStateLevel;
            public int skillStateTime;
            public int rankEffect;

            public void PostMessage(string message) => messages.Add(message);
            public void AddStation(int stationId) => stationIds.Add(stationId);
            public void AddTermini(int terminiId) => terminiIds.Add(terminiId);
            public void SetProtectTime(int ticks) => protectTicks = ticks;
            public void AddSkillState(int nextSkillStateId, int level, int durationTicks)
            {
                skillStateId = nextSkillStateId;
                skillStateLevel = level;
                skillStateTime = durationTicks;
            }
            public void AddNote(string note) => notes.Add(note);
            public void ApplyCityWarRankEffect(int rank) => rankEffect = rank;
        }
    }
}
