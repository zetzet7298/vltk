using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class TongMapEntranceTests
    {
        [Test]
        public void DefaultRegion_BannedNonOwner_ReturnsToCurrentMapCopyEntrance()
        {
            var host = new FakeHost { currentMapId = 591 };
            host.taskValues[PcTrapActionCatalogEntry.TongMapEntranceTaskLpCountId] = 1;
            var sideEffects = new FakeSideEffects();
            var executor = NewExecutor(NewAction(tongMapTongId: 10, playerTongId: 20, mapBan: 1), host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 1001 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(ExpectedWorld(1712, 3330), host.position);
            Assert.AreEqual(-1, host.fightState, "non-cn_ib PC script only SetPos, not SetFightState");
            Assert.That(sideEffects.messages, Has.Count.EqualTo(1));
            Assert.That(sideEffects.messages[0], Does.Contain("nhiệm vụ lệnh bài"));
            Assert.That(result.detail, Does.Contain("catalog-driven"));
        }

        [Test]
        public void LoadFromStreamingAssets_RoutesPcDeferredTrapIdAsHostLimitedNoOp()
        {
            var catalog = PcTrapActionCatalogRuntime.LoadFromStreamingAssets();
            var host = new FakeHost { currentMapId = 591, position = new Vector2(7, 8) };
            var executor = new PcTrapActionExecutor(catalog, host, new FakeSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 860441529u, trapIdHex = "0x33494BB9" }, out var result));

            Assert.IsTrue(result.success, result.detail);
            Assert.AreEqual(new Vector2(7, 8), host.position);
            StringAssert.Contains("TongMapEntrance mapTongId=0", result.detail);
            StringAssert.Contains("host lacks PC", result.detail);
        }

        [Test]
        public void DefaultRegion_OwnerAllowed_DoesNotMoveOrMessage()
        {
            var host = new FakeHost { position = new Vector2(12, -34) };
            var sideEffects = new FakeSideEffects();
            var executor = NewExecutor(NewAction(tongMapTongId: 10, playerTongId: 10, mapBan: 1), host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 1001 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(new Vector2(12, -34), host.position);
            Assert.That(sideEffects.messages, Is.Empty);
            Assert.That(result.detail, Does.Contain("allowed"));
        }

        [Test]
        public void CnIb_Expired_ResetsFightStateAndUsesTemplateEntrance()
        {
            var host = new FakeHost { fightState = 1 };
            var sideEffects = new FakeSideEffects();
            var action = NewAction(
                productRegion: "cn_ib",
                tongMapTongId: 77,
                playerTongId: 77,
                mapBan: 0,
                expireState: 2,
                templateMapId: 587,
                message: "Khu vực bang hội đã quá thời hạn sử dụng!");
            var executor = NewExecutor(action, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 1001 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(ExpectedWorld(1718, 3313), host.position);
            Assert.That(sideEffects.messages, Has.Count.EqualTo(1));
            Assert.That(sideEffects.messages[0], Does.Contain("quá thời hạn"));
            Assert.That(result.detail, Does.Contain("cn_ib expired"));
            Assert.That(result.detail, Does.Contain("catalog-driven"));
        }

        [Test]
        public void CnIb_BannedNonOwner_UsesTemplateMapCopyEntranceAndGenericBanMessage()
        {
            var host = new FakeHost { fightState = 1 };
            host.taskValues[PcTrapActionCatalogEntry.TongMapEntranceTaskLpCountId] = 0;
            var sideEffects = new FakeSideEffects();
            var action = NewAction(
                productRegion: "cn_ib",
                tongMapTongId: 77,
                playerTongId: 22,
                mapBan: 1,
                templateMapId: 591);
            var executor = NewExecutor(action, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 1001 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(ExpectedWorld(1712, 3330), host.position);
            Assert.That(sideEffects.messages, Has.Count.EqualTo(1));
            Assert.That(sideEffects.messages[0], Does.Not.Contain("nhiệm vụ lệnh bài"));
            Assert.That(sideEffects.messages[0], Does.Contain("không thể bước vào"));
            Assert.That(result.detail, Does.Contain("cn_ib banned"));
        }

        [Test]
        public void CnIb_ExpiringOwnerWarning_PostsWarningButDoesNotMove()
        {
            var host = new FakeHost { position = new Vector2(5, 6), fightState = 1 };
            var sideEffects = new FakeSideEffects();
            var action = NewAction(
                productRegion: "cn_ib",
                tongMapTongId: 77,
                playerTongId: 77,
                mapBan: 0,
                expireState: 1,
                noExpireWarning: 0,
                expireDate: "2026-06-30");
            var executor = NewExecutor(action, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 1001 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(new Vector2(5, 6), host.position);
            Assert.That(sideEffects.messages, Has.Count.EqualTo(1));
            Assert.That(sideEffects.messages[0], Does.Contain("2026-06-30"));
            Assert.That(result.detail, Does.Contain("expire-warning"));
        }

        private static PcTrapActionExecutor NewExecutor(PcTrapActionCatalogEntry action, FakeHost host, FakeSideEffects sideEffects)
            => new(new PcTrapActionCatalogFile { entries = new[] { action } }, host, sideEffects);

        private static PcTrapActionCatalogEntry NewAction(
            string productRegion = null,
            int tongMapTongId = 1,
            int playerTongId = 2,
            int mapBan = 1,
            int expireState = 0,
            int noExpireWarning = 1,
            int templateMapId = 0,
            string expireDate = null,
            string message = null)
            => new()
            {
                trapId = 1001,
                scriptPath = @"\script\tong\map\entrance_trap.lua",
                actionKind = "TongMapEntrance",
                tongProductRegion = productRegion,
                tongMapType = 1,
                tongMapTongId = tongMapTongId,
                tongPlayerTongId = playerTongId,
                tongMapBan = mapBan,
                tongExpireState = expireState,
                tongNoExpireWarning = noExpireWarning,
                tongTemplateMapId = templateMapId,
                tongExpireDate = expireDate,
                message = message,
                tongTaskLpCountId = PcTrapActionCatalogEntry.TongMapEntranceTaskLpCountId,
                tongEnterMapCopyIds = new[] { PcTrapActionCatalogEntry.TongMapEntranceBorderMapCopyId },
                tongEnterCellXs = new[] { PcTrapActionCatalogEntry.TongMapEntranceBorderCellX },
                tongEnterCellYs = new[] { PcTrapActionCatalogEntry.TongMapEntranceBorderCellY },
            };

        private static Vector2 ExpectedWorld(int cellX, int cellY)
            => MapEnemyDatabase.MpsToWorld(cellX * 32, cellY * 32);

        private sealed class FakeHost : ITrapTravelHost
        {
            public readonly Dictionary<int, int> taskValues = new();
            public int currentMapId = 587;
            public Vector2 position;
            public int fightState = -1;

            public bool HasMap(int mapId) => true;
            public int GetCurrentMapId() => currentMapId;
            public bool TryGetPlayerReviveWorld(out int mapId, out Vector2 worldPosition)
            {
                mapId = 0;
                worldPosition = default;
                return false;
            }
            public int GetPlayerLevel() => 1;
            public int GetPlayerSeriesId() => 0;
            public int GetPlayerSex() => 0;
            public string GetPlayerName() => "Tester";
            public long GetCurrentDateYmdHm() => 202606090000;
            public int RandomIntInclusive(int minInclusive, int maxInclusive) => minInclusive;
            public int GetTaskValue(int taskId) => taskValues.TryGetValue(taskId, out var value) ? value : 0;
            public void SetTaskValue(int taskId, int value) => taskValues[taskId] = value;
            public int GetTaskTempValue(int taskId) => 0;
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
            public void NewWorld(int mapId, Vector2 worldPosition) => position = worldPosition;
            public void SetPos(Vector2 worldPosition) => position = worldPosition;
            public void SetFightState(int nextFightState) => fightState = nextFightState;
            public void SetCurCamp(int camp) { }
            public void SetLogoutRv(int value) { }
            public void SetPkFlag(int value) { }
            public void ForbidChangePk(int value) { }
            public void SetPunish(int value) { }
            public void SetCreateTeam(int value) { }
            public void SetTaskTemp(int taskId, int value) { }
            public void SetDeathScript(string scriptPath) { }
            public void LeaveTeam() { }
            public void SetRevPos(int mapId, int reviveId) { }
        }

        private sealed class FakeSideEffects : ITrapActionSideEffects
        {
            public readonly List<string> messages = new();
            public void PostMessage(string message) => messages.Add(message);
            public void AddStation(int stationId) { }
            public void AddTermini(int terminiId) { }
            public void SetProtectTime(int ticks) { }
            public void AddSkillState(int skillStateId, int level, int durationTicks) { }
            public void AddNote(string note) { }
            public void ApplyCityWarRankEffect(int rank) { }
        }
        // --- Host-driven entry tests (CanPlayerEnter + EnterTongMap) ---

        /// <summary>Fake ITongMapHost for CanPlayerEnter/EnterTongMap tests.</summary>
        private sealed class FakeTongMapHost : ITongMapHost
        {
            public int OwnerTongId = 0;
            public bool Banned = false;
            public long ExpireTime = 0;
            public bool PlayerInTong = true;
            public bool CanEnter = true;
            public bool FightStateResult = true;
            public bool SetPosResult = true;
            public int SetPosCalls = 0;
            public int SetFightStateCalls = 0;
            public int SendMessageCalls = 0;
            public System.Collections.Generic.List<string> SentMessages = new();

            public int GetTongOwner(int mapId) => OwnerTongId;
            public bool IsTongBanned(int tongId, int mapId) => Banned;
            public long GetTongExpireTime(int tongId, int mapId) => ExpireTime;
            public bool IsPlayerInTong(string player, int tongId) => PlayerInTong;
            public bool CanEnterTongMap(int mapId, int level, int tongId) => CanEnter;
            public bool SetFightState(string player, bool fighting) { SetFightStateCalls++; return FightStateResult; }
            public bool SetPos(string player, int x, int y) { SetPosCalls++; return SetPosResult; }
            public bool SendMessage(string player, string message) { SendMessageCalls++; SentMessages.Add(message); return true; }
        }

        [Test]
        public void CanPlayerEnter_PublicMap_AllowsAnyone()
        {
            var host = new FakeTongMapHost { OwnerTongId = 0 };
            var svc = new TongMapEntranceRuntimeService(host);

            var d = svc.CanPlayerEnter(mapId: 907, "hero", level: 1, tongId: 5, now: 0);

            Assert.IsTrue(d.Allowed);
            Assert.AreEqual("PublicMap", d.ReasonVi);
        }

        [Test]
        public void CanPlayerEnter_OwnerTong_AllowsOwner()
        {
            var host = new FakeTongMapHost { OwnerTongId = 5, PlayerInTong = true };
            var svc = new TongMapEntranceRuntimeService(host);

            var d = svc.CanPlayerEnter(mapId: 949, "hero", level: 10, tongId: 5, now: 0);

            Assert.IsTrue(d.Allowed);
            Assert.AreEqual("Owner", d.ReasonVi);
        }

        [Test]
        public void CanPlayerEnter_BannedTong_Denies()
        {
            var host = new FakeTongMapHost { OwnerTongId = 5, Banned = true, PlayerInTong = false };
            var svc = new TongMapEntranceRuntimeService(host);

            var d = svc.CanPlayerEnter(mapId: 949, "intruder", level: 100, tongId: 7, now: 0);

            Assert.IsFalse(d.Allowed);
            Assert.AreEqual("Banned", d.ReasonVi);
        }

        [Test]
        public void CanPlayerEnter_ExpiredTong_Denies()
        {
            var host = new FakeTongMapHost { OwnerTongId = 5, ExpireTime = 1000, PlayerInTong = false };
            var svc = new TongMapEntranceRuntimeService(host);

            var d = svc.CanPlayerEnter(mapId: 949, "intruder", level: 100, tongId: 7, now: 2000);

            Assert.IsFalse(d.Allowed);
            Assert.AreEqual("Expired", d.ReasonVi);
        }

        [Test]
        public void CanPlayerEnter_LevelTooLow_Denies()
        {
            var host = new FakeTongMapHost { OwnerTongId = 5, CanEnter = false, PlayerInTong = false };
            var svc = new TongMapEntranceRuntimeService(host);

            var d = svc.CanPlayerEnter(mapId: 949, "lowbie", level: 5, tongId: 7, now: 0);

            Assert.IsFalse(d.Allowed);
            Assert.AreEqual("LevelTooLow", d.ReasonVi);
        }

        [Test]
        public void EnterTongMap_OnSuccess_CallsSetPosAndSetFightState()
        {
            var host = new FakeTongMapHost { OwnerTongId = 5, PlayerInTong = true };
            var svc = new TongMapEntranceRuntimeService(host);

            bool ok = svc.EnterTongMap(mapId: 949, "hero", level: 10, tongId: 5,
                x: 100, y: 200, fighting: true, now: 0);

            Assert.IsTrue(ok);
            Assert.AreEqual(1, host.SetPosCalls, "SetPos called once on success.");
            Assert.AreEqual(1, host.SetFightStateCalls, "SetFightState called once on success.");
            Assert.AreEqual(0, host.SendMessageCalls, "No denial message on success.");
        }

        [Test]
        public void EnterTongMap_OnDeny_CallsSendMessageNotSetPos()
        {
            var host = new FakeTongMapHost { OwnerTongId = 5, Banned = true, PlayerInTong = false };
            var svc = new TongMapEntranceRuntimeService(host);

            bool ok = svc.EnterTongMap(mapId: 949, "intruder", level: 100, tongId: 7,
                x: 100, y: 200, fighting: false, now: 0);

            Assert.IsFalse(ok);
            Assert.AreEqual(0, host.SetPosCalls, "No SetPos on denial.");
            Assert.AreEqual(0, host.SetFightStateCalls, "No SetFightState on denial.");
            Assert.AreEqual(1, host.SendMessageCalls, "Denial message sent.");
            StringAssert.Contains("Cấm", host.SentMessages[0]);
        }

    }
}
