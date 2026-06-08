using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class MapInteractiveCatalogRuntimeTests
    {
        [Test]
        public void LoadFromStreamingAssets_ContainsPcObjectsWithExactSprites()
        {
            var catalog = MapInteractiveCatalogRuntime.LoadFromStreamingAssets();

            Assert.IsNotNull(catalog);
            Assert.IsNotNull(catalog.geometries);
            var withObject = catalog.geometries.FirstOrDefault(g => g.objects != null && g.objects.Length > 0);
            Assert.IsNotNull(withObject, "Expected at least one generated geometry with Obj_S records");
            var obj = withObject.objects.FirstOrDefault(o => !string.IsNullOrEmpty(o.imageName) && !o.skipPaint && o.isUnseen == 0);
            Assert.IsNotNull(obj, "Obj_S records should be enriched from PC ObjData imageName");
            Assert.IsFalse(obj.skipPaint);
            Assert.IsTrue(obj.imageName.StartsWith(@"\spr\obj\"));
            Assert.IsFalse(string.IsNullOrEmpty(obj.imageUid));
        }

        [Test]
        public void FindForMap_ResolvesVuotAiSharedGeometry()
        {
            var catalog = MapInteractiveCatalogRuntime.LoadFromStreamingAssets();
            var mapDef = new MapDefinition
            {
                catalogEntry = new MapCatalogEntry
                {
                    mapId = MapPortManifest.VuotAiNhiepThiTranId,
                    geometryKey = "g_a7649e666581b845",
                }
            };

            var geometry = catalog.FindForMap(mapDef);

            Assert.IsNotNull(geometry);
            Assert.IsTrue(geometry.mapIds.Contains(MapPortManifest.VuotAiNhiepThiTranId));
            Assert.AreEqual(16, geometry.trapCount);
            Assert.AreEqual(0, geometry.objectCount);
            Assert.IsTrue(geometry.staticTrapClearMapIds.Contains(MapPortManifest.VuotAiNhiepThiTranId));
            Assert.IsTrue(geometry.traps[0].scriptResolved);
            Assert.IsTrue(geometry.traps[0].scriptPath.Contains(@"\trap\"));
            Assert.IsTrue(geometry.traps[0].IsInactiveForMap(MapPortManifest.VuotAiNhiepThiTranId));
        }

        [Test]
        public void SandboxPlayerController_ConfiguresPhysicsBodyForRegionSTrapTriggers()
        {
            var go = new GameObject("trap-contact-player-test");
            try
            {
                go.AddComponent<SandboxPlayerController>();

                var body = go.GetComponent<Rigidbody2D>();
                var collider = go.GetComponent<CircleCollider2D>();

                Assert.IsNotNull(body);
                Assert.AreEqual(RigidbodyType2D.Kinematic, body.bodyType);
                Assert.AreEqual(0f, body.gravityScale);
                Assert.IsNotNull(collider);
                Assert.IsTrue(collider.isTrigger);
                Assert.AreEqual(16f, collider.radius);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private sealed class FakeTrapTravelHost : ITrapTravelHost
        {
            public int mapId = -1;
            public Vector2 position;
            public bool hasMap = true;
            public int currentMapId = 907;
            public bool hasReviveTarget;
            public int reviveMapId = 53;
            public Vector2 revivePosition;
            public int fightState = -1;
            public int playerLevel = 1;
            public long currentDateYmdHm = 202606080900;
            public int randomValue = 0;
            public int taskValue = 0;
            public Dictionary<int, int> taskValues = new();
            public int curCamp = 0;
            public int originalCamp = 0;
            public int battleRank = 0;
            public int playerFactionId = (int)CombatFaction.None;
            public int logoutRv = -1;
            public int pkFlag = -1;
            public int forbidChangePk = -1;
            public int punish = -1;
            public int createTeam = -1;
            public int taskTempId;
            public int taskTempValue;
            public string deathScript;
            public bool leftTeam;
            public int revPosMapId;
            public int revPosId;

            public bool HasMap(int targetMapId) => hasMap;
            public int GetCurrentMapId() => currentMapId;
            public bool TryGetPlayerReviveWorld(out int targetMapId, out Vector2 worldPosition)
            {
                targetMapId = reviveMapId;
                worldPosition = revivePosition;
                return hasReviveTarget;
            }
            public int GetPlayerLevel() => playerLevel;
            public long GetCurrentDateYmdHm() => currentDateYmdHm;
            public int RandomIntInclusive(int minInclusive, int maxInclusive) => randomValue;
            public int GetTaskValue(int taskId) => taskValues.TryGetValue(taskId, out var value) ? value : taskValue;
            public int GetCurCamp() => curCamp;
            public int GetCamp() => originalCamp;
            public int GetBattleRank() => battleRank;
            public int GetFightState() => fightState;
            public int GetPlayerFactionId() => playerFactionId;
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
            public void SetTaskTemp(int taskId, int value)
            {
                taskTempId = taskId;
                taskTempValue = value;
            }
            public void SetDeathScript(string scriptPath) => deathScript = scriptPath;
            public void LeaveTeam() => leftTeam = true;
            public void SetRevPos(int mapId, int reviveId)
            {
                revPosMapId = mapId;
                revPosId = reviveId;
            }
        }

        private sealed class FakeTrapActionSideEffects : ITrapActionSideEffects
        {
            public readonly List<string> messages = new();
            public readonly List<int> stationIds = new();
            public readonly List<int> terminiIds = new();
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
            public void ApplyCityWarRankEffect(int rank) => rankEffect = rank;
        }

        private sealed class FakeObjectActionSideEffects : IObjectActionSideEffects
        {
            public string message;
            public readonly List<string> messages = new();
            public readonly List<int> eventItems = new();
            public readonly List<string> notes = new();
            public bool openedBox;
            public int[] ladderIds;

            public void PostMessage(string nextMessage)
            {
                message = nextMessage;
                messages.Add(nextMessage);
            }
            public void AddEventItem(int eventItemId) => eventItems.Add(eventItemId);
            public void AddNote(string note) => notes.Add(note);
            public void OpenBox() => openedBox = true;
            public void ShowLadder(int[] nextLadderIds) => ladderIds = nextLadderIds;
        }

        [Test]
        public void TrapActionCatalog_LoadsDeterministicPcNewWorldActions()
        {
            var catalog = PcTrapActionCatalogRuntime.LoadFromStreamingAssets();

            Assert.IsNotNull(catalog);
            Assert.AreEqual(777, catalog.Count);
            Assert.AreEqual(112, catalog.entries.Count(e => e != null && e.IsFightStateSetPos));
            Assert.AreEqual(26, catalog.entries.Count(e => e != null && e.IsMessageOnly));
            Assert.AreEqual(23, catalog.entries.Count(e => e != null && e.IsSayMessage));
            Assert.AreEqual(2, catalog.entries.Count(e => e != null && e.IsTalkMessage));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsMsg2Player));
            Assert.AreEqual(3, catalog.entries.Count(e => e != null && e.IsMsg2PlayerNewWorld));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsTaskOptionalMessageNewWorld));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsTaskFactionGateNewWorld));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsMessageRandomNewWorld));
            Assert.AreEqual(20, catalog.entries.Count(e => e != null && e.IsLevelGateNewWorld));
            Assert.AreEqual(2, catalog.entries.Count(e => e != null && e.IsLevelBracketNewWorld));
            var entry = catalog.entries.FirstOrDefault(e => e != null && e.IsNewWorld);
            Assert.IsNotNull(entry);
            Assert.Greater(entry.targetMapId, 0);
            Assert.Greater(entry.targetCellX, 0);
            Assert.Greater(entry.targetCellY, 0);
            Assert.AreEqual(
                MapEnemyDatabase.MpsToWorld(entry.targetCellX * 32, entry.targetCellY * 32),
                entry.TargetWorldPosition());
        }

        [Test]
        public void ObjectActionCatalog_LoadsDeterministicPcNewWorldActions()
        {
            var catalog = PcObjectActionCatalogRuntime.LoadFromStreamingAssets();

            Assert.IsNotNull(catalog);
            Assert.AreEqual(268, catalog.Count);
            Assert.AreEqual(7, catalog.entries.Count(e => e != null && e.IsNewWorld));
            Assert.AreEqual(19, catalog.entries.Count(e => e != null && e.IsPickupMessage));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsTaskOptionalPickupMessage));
            Assert.AreEqual(144, catalog.entries.Count(e => e != null && e.IsSayMessage));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsTalkMessage));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsTaskTalkMessage));
            Assert.AreEqual(51, catalog.entries.Count(e => e != null && e.IsOpenBox));
            Assert.AreEqual(19, catalog.entries.Count(e => e != null && e.IsFactionOpenBox));
            Assert.AreEqual(2, catalog.entries.Count(e => e != null && e.IsCampOpenBox));
            Assert.AreEqual(23, catalog.entries.Count(e => e != null && e.IsShowLadder));
            var sign = catalog.Find(@"\script\两湖区\巴陵县\obj\巴陵县-路标3.lua");
            Assert.IsNotNull(sign);
            Assert.IsTrue(sign.IsSayMessage);
            Assert.AreEqual("Ba Lăng huyện<---->Miêu Lĩnh", sign.message);
            var pickup = catalog.Find(@"\script\两湖区\巴陵县\obj\玉佩.lua");
            Assert.IsNotNull(pickup);
            Assert.IsTrue(pickup.IsPickupMessage);
            Assert.AreEqual(182, pickup.eventItemIds.Single());
            var optionalPickup = catalog.Find(@"\script\西南北区\成都\成都\地图Obj\emobj01.lua");
            Assert.IsNotNull(optionalPickup);
            Assert.IsTrue(optionalPickup.IsTaskOptionalPickupMessage);
            Assert.AreEqual(1, optionalPickup.noteTaskId);
            Assert.AreEqual(10 * 256, optionalPickup.noteTaskMinExclusive);
            Assert.AreEqual(20 * 256, optionalPickup.noteTaskMaxExclusive);
            Assert.AreEqual(118, optionalPickup.eventItemIds.Single());
            Assert.AreEqual("Tìm thấy một miếng Lượng Ngân Khoáng trong khu rừng ở phía tây Thành Đô.", optionalPickup.taskNotes.Single());
            var taskTalk = catalog.Find(@"\script\中原南区\丐帮\地下迷宫三层\obj\地图_gbl60_宝箱empty.lua");
            Assert.IsNotNull(taskTalk);
            Assert.IsTrue(taskTalk.IsTaskTalkMessage);
            Assert.AreEqual(8, taskTalk.taskId);
            Assert.AreEqual(60 * 256 + 10, taskTalk.taskValue);
            CollectionAssert.AreEqual(new[] { "Mở bảo rương ra.", "Bạn thất vọng vì chiếc rương này trống rỗng." }, taskTalk.messages);
            CollectionAssert.AreEqual(new[] { "Bảo rương này đã khóa rồi" }, taskTalk.elseMessages);
            var entry = catalog.Find(@"\script\两湖区\天王帮\洞庭湖底山洞1\trap\洞庭湖底1to洞庭湖底2.lua");
            Assert.IsNotNull(entry);
            Assert.IsTrue(entry.IsNewWorld);
            Assert.AreEqual(67, entry.targetMapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1591 * 32, 3193 * 32), entry.TargetWorldPosition());
        }

        [Test]
        public void PcObjectActionExecutor_NewWorld_UsesPcCellCoordinates()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\object_newworld.lua",
                        actionKind = "NewWorld",
                        targetMapId = 67,
                        targetCellX = 1591,
                        targetCellY = 3193,
                        fightState = 1,
                    }
                }
            };
            var host = new FakeTrapTravelHost();
            var executor = new PcObjectActionExecutor(catalog, host);
            var obj = new MapInteractiveObject { script = @"\script\object_newworld.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(67, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1591 * 32, 3193 * 32), host.position);
            Assert.AreEqual(1, host.fightState);
            StringAssert.Contains("SetFightState(1)", result.detail);
        }


        [Test]
        public void PcObjectActionExecutor_PickupMessage_AppliesPcSideEffects()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\pickup.lua",
                        actionKind = "PickupMessage",
                        message = "Tìm được Linh Chi.",
                        eventItemIds = new[] { 116 },
                        notes = new[] { "Tại khu Đông Bắc Vũ Lăng sơn tìm được Linh Chi." },
                        setPropState = true,
                    }
                }
            };
            var sideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, new FakeTrapTravelHost(), sideEffects);
            var obj = new MapInteractiveObject { script = @"\script\pickup.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var result));

            Assert.IsTrue(result.success);
            Assert.IsTrue(result.hideObject);
            Assert.AreEqual("Tìm được Linh Chi.", sideEffects.message);
            Assert.AreEqual(116, sideEffects.eventItems.Single());
            Assert.AreEqual("Tại khu Đông Bắc Vũ Lăng sơn tìm được Linh Chi.", sideEffects.notes.Single());
            StringAssert.Contains("SetPropState=True", result.detail);
        }


        [Test]
        public void PcObjectActionExecutor_SayMessage_PostsPcText()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\signpost.lua",
                        actionKind = "SayMessage",
                        message = "Đi đến Biện Kinh.",
                    }
                }
            };
            var sideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, new FakeTrapTravelHost(), sideEffects);
            var obj = new MapInteractiveObject { script = @"\script\signpost.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var result));

            Assert.IsTrue(result.success);
            Assert.IsFalse(result.hideObject);
            Assert.AreEqual("Đi đến Biện Kinh.", sideEffects.message);
            StringAssert.Contains("SayMessage", result.detail);
        }

        [Test]
        public void PcObjectActionExecutor_TalkMessage_PostsAllPcLines()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\talk_sign.lua",
                        actionKind = "TalkMessage",
                        messages = new[]
                        {
                            "Bạn thử dùng sức đẩy tảng đá,",
                            "nhưng nó cứ nằm trơ trơ",
                        },
                    }
                }
            };
            var sideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, new FakeTrapTravelHost(), sideEffects);
            var obj = new MapInteractiveObject { script = @"\script\talk_sign.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var result));

            Assert.IsTrue(result.success);
            Assert.IsFalse(result.hideObject);
            CollectionAssert.AreEqual(new[] { "Bạn thử dùng sức đẩy tảng đá,", "nhưng nó cứ nằm trơ trơ" }, sideEffects.messages);
            StringAssert.Contains("TalkMessage", result.detail);
        }

        [Test]
        public void PcObjectActionExecutor_OpenBox_AppliesPcBoxAndReviveSideEffects()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\两湖区\巴陵县\obj\巴陵县-储物箱1.lua",
                        actionKind = "OpenBox",
                        reviveId = 19,
                    }
                }
            };
            var host = new FakeTrapTravelHost { currentMapId = 53 };
            var sideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, host, sideEffects);
            var obj = new MapInteractiveObject { script = @"\script\两湖区\巴陵县\obj\巴陵县-储物箱1.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var result));

            Assert.IsTrue(result.success);
            Assert.IsTrue(sideEffects.openedBox);
            Assert.AreEqual(53, host.revPosMapId);
            Assert.AreEqual(19, host.revPosId);
            StringAssert.Contains("OpenBox", result.detail);
        }


        [Test]
        public void PcObjectActionExecutor_FactionOpenBox_AlwaysOpensAndGatesPcReviveByFaction()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\两湖区\天王帮\天王帮\obj\天王帮-储物箱1.lua",
                        actionKind = "FactionOpenBox",
                        reviveId = 21,
                        requiredFaction = "tianwang",
                        requiredFactionId = (int)CombatFaction.TianWang,
                    }
                }
            };
            var matchingHost = new FakeTrapTravelHost
            {
                currentMapId = 21,
                playerFactionId = (int)CombatFaction.TianWang,
            };
            var matchingSideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, matchingHost, matchingSideEffects);
            var obj = new MapInteractiveObject { script = @"\script\两湖区\天王帮\天王帮\obj\天王帮-储物箱1.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var matched));

            Assert.IsTrue(matched.success);
            Assert.IsTrue(matchingSideEffects.openedBox);
            Assert.AreEqual(21, matchingHost.revPosMapId);
            Assert.AreEqual(21, matchingHost.revPosId);
            StringAssert.Contains("matched=True", matched.detail);

            var otherHost = new FakeTrapTravelHost
            {
                currentMapId = 21,
                playerFactionId = (int)CombatFaction.CaiBang,
            };
            var otherSideEffects = new FakeObjectActionSideEffects();
            executor = new PcObjectActionExecutor(catalog, otherHost, otherSideEffects);

            Assert.IsTrue(executor.TryExecute(obj, out var unmatched));

            Assert.IsTrue(unmatched.success);
            Assert.IsTrue(otherSideEffects.openedBox);
            Assert.AreEqual(0, otherHost.revPosId);
            StringAssert.Contains("matched=False", unmatched.detail);
        }


        [Test]
        public void PcObjectActionExecutor_CampOpenBox_GatesPcBattlefieldBoxByCurrentCamp()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\中原北区\宋金战场\obj\song-chuwuxiang.lua",
                        actionKind = "CampOpenBox",
                        requiredCamp = 1,
                        message = "Nhìn ngươi mắt la mày loét, nhất định là Kim quốc gian tế! Người đâu! Bắt lấy hắn!",
                    }
                }
            };
            var matchingHost = new FakeTrapTravelHost { curCamp = 1 };
            var matchingSideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, matchingHost, matchingSideEffects);
            var obj = new MapInteractiveObject { script = @"\script\中原北区\宋金战场\obj\song-chuwuxiang.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var matched));

            Assert.IsTrue(matched.success);
            Assert.IsTrue(matchingSideEffects.openedBox);
            Assert.AreEqual(0, matchingSideEffects.messages.Count);
            StringAssert.Contains("matched=True", matched.detail);

            var otherHost = new FakeTrapTravelHost { curCamp = 2 };
            var otherSideEffects = new FakeObjectActionSideEffects();
            executor = new PcObjectActionExecutor(catalog, otherHost, otherSideEffects);

            Assert.IsTrue(executor.TryExecute(obj, out var unmatched));

            Assert.IsTrue(unmatched.success);
            Assert.IsFalse(otherSideEffects.openedBox);
            Assert.AreEqual("Nhìn ngươi mắt la mày loét, nhất định là Kim quốc gian tế! Người đâu! Bắt lấy hắn!", otherSideEffects.message);
            StringAssert.Contains("matched=False", unmatched.detail);
        }



        [Test]
        public void PcObjectActionExecutor_TaskOptionalPickupMessage_AddsPcNoteOnlyInsideTaskRange()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\西南北区\成都\成都\地图Obj\emobj01.lua",
                        actionKind = "TaskOptionalPickupMessage",
                        message = "Nhặt được một miếng Lượng Ngân Khoáng.",
                        eventItemIds = new[] { 118 },
                        setPropState = true,
                        noteTaskId = 1,
                        noteTaskMinExclusive = 10 * 256,
                        noteTaskMaxExclusive = 20 * 256,
                        taskNotes = new[] { "Tìm thấy một miếng Lượng Ngân Khoáng trong khu rừng ở phía tây Thành Đô." },
                    }
                }
            };
            var matchingHost = new FakeTrapTravelHost();
            matchingHost.taskValues[1] = 15 * 256;
            var sideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, matchingHost, sideEffects);
            var obj = new MapInteractiveObject { script = @"\script\西南北区\成都\成都\地图Obj\emobj01.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var matched));

            Assert.IsTrue(matched.success);
            Assert.IsTrue(matched.hideObject);
            Assert.AreEqual(118, sideEffects.eventItems.Single());
            Assert.AreEqual("Tìm thấy một miếng Lượng Ngân Khoáng trong khu rừng ở phía tây Thành Đô.", sideEffects.notes.Single());
            StringAssert.Contains("matched=True", matched.detail);

            var otherHost = new FakeTrapTravelHost();
            otherHost.taskValues[1] = 20 * 256;
            sideEffects = new FakeObjectActionSideEffects();
            executor = new PcObjectActionExecutor(catalog, otherHost, sideEffects);

            Assert.IsTrue(executor.TryExecute(obj, out var unmatched));

            Assert.IsTrue(unmatched.success);
            Assert.IsTrue(unmatched.hideObject);
            Assert.AreEqual(118, sideEffects.eventItems.Single());
            Assert.AreEqual(0, sideEffects.notes.Count);
            StringAssert.Contains("matched=False", unmatched.detail);
        }

        [Test]
        public void PcObjectActionExecutor_TaskTalkMessage_BranchesOnPcGetTaskValue()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\中原南区\丐帮\地下迷宫三层\obj\地图_gbl60_宝箱empty.lua",
                        actionKind = "TaskTalkMessage",
                        taskId = 8,
                        taskValue = 60 * 256 + 10,
                        messages = new[] { "Mở bảo rương ra.", "Bạn thất vọng vì chiếc rương này trống rỗng." },
                        elseMessages = new[] { "Bảo rương này đã khóa rồi" },
                    }
                }
            };
            var matchingHost = new FakeTrapTravelHost();
            matchingHost.taskValues[8] = 60 * 256 + 10;
            var sideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, matchingHost, sideEffects);
            var obj = new MapInteractiveObject { script = @"\script\中原南区\丐帮\地下迷宫三层\obj\地图_gbl60_宝箱empty.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var matched));

            Assert.IsTrue(matched.success);
            CollectionAssert.AreEqual(new[] { "Mở bảo rương ra.", "Bạn thất vọng vì chiếc rương này trống rỗng." }, sideEffects.messages);
            StringAssert.Contains("matched=True", matched.detail);

            var otherHost = new FakeTrapTravelHost();
            otherHost.taskValues[8] = 0;
            sideEffects = new FakeObjectActionSideEffects();
            executor = new PcObjectActionExecutor(catalog, otherHost, sideEffects);

            Assert.IsTrue(executor.TryExecute(obj, out var unmatched));

            Assert.IsTrue(unmatched.success);
            CollectionAssert.AreEqual(new[] { "Bảo rương này đã khóa rồi" }, sideEffects.messages);
            StringAssert.Contains("matched=False", unmatched.detail);
        }

        [Test]
        public void PcObjectActionExecutor_ShowLadder_AppliesPcLadderIds()
        {
            var catalog = new PcObjectActionCatalogFile
            {
                entries = new[]
                {
                    new PcObjectActionCatalogEntry
                    {
                        scriptPath = @"\script\中原北区\天忍教\天忍教室外\obj\天忍教-告示牌1.lua",
                        actionKind = "ShowLadder",
                        ladderIds = new[] { 2, 12, 23 },
                    }
                }
            };
            var sideEffects = new FakeObjectActionSideEffects();
            var executor = new PcObjectActionExecutor(catalog, new FakeTrapTravelHost(), sideEffects);
            var obj = new MapInteractiveObject { script = @"\script\中原北区\天忍教\天忍教室外\obj\天忍教-告示牌1.lua" };

            Assert.IsTrue(executor.TryExecute(obj, out var result));

            Assert.IsTrue(result.success);
            CollectionAssert.AreEqual(new[] { 2, 12, 23 }, sideEffects.ladderIds);
            StringAssert.Contains("ShowLadder", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_NewWorld_UsesPcCellCoordinates()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 1234,
                        trapIdHex = "0x000004D2",
                        scriptPath = @"\script\trap.lua",
                        actionKind = "NewWorld",
                        targetMapId = 52,
                        targetCellX = 1729,
                        targetCellY = 3225,
                        fightState = 1,
                    }
                }
            };
            var host = new FakeTrapTravelHost();
            var executor = new PcTrapActionExecutor(catalog, host);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x000004D2" }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(52, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1729 * 32, 3225 * 32), host.position);
            Assert.AreEqual(1, host.fightState);
            StringAssert.Contains("SetFightState(1)", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_NewWorldMissingMap_DoesNotApplyFightState()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 4321,
                        trapIdHex = "0x000010E1",
                        actionKind = "NewWorld",
                        targetMapId = 999999,
                        targetCellX = 100,
                        targetCellY = 200,
                        fightState = 0,
                    }
                }
            };
            var host = new FakeTrapTravelHost { hasMap = false };
            var executor = new PcTrapActionExecutor(catalog, host);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 4321 }, out var result));

            Assert.IsFalse(result.success);
            Assert.AreEqual(-1, host.fightState);
            StringAssert.Contains("target map 999999 missing", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_FightStateSetPos_UsesCurrentFightStateBranch()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 777,
                        trapIdHex = "0x00000309",
                        actionKind = "FightStateSetPos",
                        ifFightState = 0,
                        ifTargetCellX = 1577,
                        ifTargetCellY = 3246,
                        ifNextFightState = 1,
                        elseFightState = 1,
                        elseTargetCellX = 1581,
                        elseTargetCellY = 3233,
                        elseNextFightState = 0,
                    }
                }
            };
            var host = new FakeTrapTravelHost { fightState = 0 };
            var executor = new PcTrapActionExecutor(catalog, host);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 777 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1577 * 32, 3246 * 32), host.position);
            Assert.AreEqual(1, host.fightState);
            StringAssert.Contains("GetFightState()==0", result.detail);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x00000309" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1581 * 32, 3233 * 32), host.position);
            Assert.AreEqual(0, host.fightState);
            StringAssert.Contains("GetFightState()==1", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_MessageOnly_PostsPcTrapLines()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 901,
                        trapIdHex = "0x00000385",
                        scriptPath = @"\script\trap_message.lua",
                        actionKind = "TalkMessage",
                        messages = new[]
                        {
                            "Bạn cảm thấy một làn gió lạnh thổi đến.",
                            "Trên vách viết: Thanh Âm động.",
                        },
                    }
                }
            };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, new FakeTrapTravelHost(), sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 901 }, out var result));

            Assert.IsTrue(result.success);
            CollectionAssert.AreEqual(new[]
            {
                "Bạn cảm thấy một làn gió lạnh thổi đến.",
                "Trên vách viết: Thanh Âm động.",
            }, sideEffects.messages);
            StringAssert.Contains("TalkMessage", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_Msg2PlayerNewWorld_PostsMessageThenWarps()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 902,
                        trapIdHex = "0x00000386",
                        scriptPath = @"\script\msg_newworld.lua",
                        actionKind = "Msg2PlayerNewWorld",
                        message = "Bạn thoát khỏi nơi nguy hiểm.",
                        terminiIds = new[] { 148 },
                        targetMapId = 131,
                        targetCellX = 1459,
                        targetCellY = 3277,
                    }
                }
            };
            var host = new FakeTrapTravelHost();
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 902 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(131, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1459 * 32, 3277 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Bạn thoát khỏi nơi nguy hiểm." }, sideEffects.messages);
            CollectionAssert.AreEqual(new[] { 148 }, sideEffects.terminiIds);
            StringAssert.Contains("Msg2Player + NewWorld", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_LevelGateNewWorld_BranchesFromPlayerLevel()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 903,
                        trapIdHex = "0x00000387",
                        scriptPath = @"\script\level_gate.lua",
                        actionKind = "LevelGateNewWorld",
                        requiredLevel = 5,
                        targetMapId = 54,
                        targetCellX = 1471,
                        targetCellY = 2992,
                        fightState = 1,
                        failTargetCellX = 1808,
                        failTargetCellY = 3456,
                        messages = new[] { "Phía trước nguy hiểm! Xin hãy quay về rèn luyện thêm!" },
                        terminiIds = new[] { 46 },
                        protectTicks = 54,
                        skillStateId = 963,
                        skillStateLevel = 1,
                        skillStateTime = 54,
                    }
                }
            };
            var sideEffects = new FakeTrapActionSideEffects();
            var host = new FakeTrapTravelHost { playerLevel = 4 };
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 903 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1808 * 32, 3456 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Phía trước nguy hiểm! Xin hãy quay về rèn luyện thêm!" }, sideEffects.messages);

            host.playerLevel = 5;
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x00000387" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(54, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1471 * 32, 2992 * 32), host.position);
            Assert.AreEqual(1, host.fightState);
            CollectionAssert.AreEqual(new[] { 46 }, sideEffects.terminiIds);
            Assert.AreEqual(54, sideEffects.protectTicks);
            Assert.AreEqual(963, sideEffects.skillStateId);
            Assert.AreEqual(1, sideEffects.skillStateLevel);
            Assert.AreEqual(54, sideEffects.skillStateTime);
            StringAssert.Contains("GetLevel()==5", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_LevelBracketNewWorld_BranchesFromPcBattlefieldLevelRanges()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 914,
                        trapIdHex = "0x00000392",
                        scriptPath = @"\script\中原南区\襄阳\襄阳\trap\襄阳to宋金战场.lua",
                        actionKind = "LevelBracketNewWorld",
                        requiredLevel = 40,
                        message = "Chiến trường Tống Kim gian khổ khốc liệt, ngươi chưa đạt đến cấp 40 hãy về luyện thêm rồi hãy tính.",
                        levelBracketMinLevels = new[] { 40, 80, 120 },
                        levelBracketMaxExclusiveLevels = new[] { 80, 120, 0 },
                        levelBracketTargetMapIds = new[] { 323, 324, 325 },
                        levelBracketTargetCellXs = new[] { 1541, 1541, 1541 },
                        levelBracketTargetCellYs = new[] { 3178, 3178, 3178 },
                        levelBracketMessages = new[]
                        {
                            "Đến nơi báo danh Chiến Trường Tống Kim Sơ Cấp",
                            "Đến nơi báo danh Chiến Trường Tống Kim Trung Cấp",
                            "Đến nơi báo danh Chiến Trường Tống Kim Cao Cấp",
                        },
                        fightState = 0,
                        protectTicks = 54,
                        skillStateId = 963,
                        skillStateLevel = 1,
                        skillStateTime = 54,
                    }
                }
            };

            var sideEffects = new FakeTrapActionSideEffects();
            var host = new FakeTrapTravelHost { playerLevel = 39 };
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 914 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, host.mapId);
            CollectionAssert.AreEqual(new[] { "Chiến trường Tống Kim gian khổ khốc liệt, ngươi chưa đạt đến cấp 40 hãy về luyện thêm rồi hãy tính." }, sideEffects.messages);
            Assert.AreEqual(54, sideEffects.protectTicks);
            Assert.AreEqual(963, sideEffects.skillStateId);

            sideEffects = new FakeTrapActionSideEffects();
            host = new FakeTrapTravelHost { playerLevel = 80 };
            executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x00000392" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(324, host.mapId);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1541 * 32, 3178 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Đến nơi báo danh Chiến Trường Tống Kim Trung Cấp" }, sideEffects.messages);
            Assert.AreEqual(54, sideEffects.protectTicks);
            Assert.AreEqual(963, sideEffects.skillStateId);
            Assert.AreEqual(1, sideEffects.skillStateLevel);
            Assert.AreEqual(54, sideEffects.skillStateTime);
        }

        [Test]
        public void PcTrapActionExecutor_OpenServerDateGateSetPos_BranchesFromDateAndFightState()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 904,
                        trapIdHex = "0x00000388",
                        scriptPath = @"\script\gate.lua",
                        actionKind = "OpenServerDateGateSetPos",
                        openServerDate = 202202111248,
                        openServerMessage = "Thời gian open server là 17h, xin hãy quay lại sau",
                        closedTargetCellX = 1695,
                        closedTargetCellY = 3099,
                        ifFightState = 0,
                        ifTargetCellX = 1697,
                        ifTargetCellY = 3097,
                        ifNextFightState = 1,
                        elseTargetCellX = 1695,
                        elseTargetCellY = 3099,
                        elseNextFightState = 0,
                        closedStationIds = new[] { 10 },
                        openStationIds = new[] { 15 },
                        closedProtectTicks = 54,
                        closedSkillStateId = 963,
                        closedSkillStateLevel = 1,
                        closedSkillStateTime = 54,
                    }
                }
            };

            var closedHost = new FakeTrapTravelHost { currentDateYmdHm = 202101010900, fightState = 0 };
            var closedSideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, closedHost, closedSideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 904 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1695 * 32, 3099 * 32), closedHost.position);
            Assert.AreEqual(0, closedHost.fightState);
            CollectionAssert.AreEqual(new[] { "Thời gian open server là 17h, xin hãy quay lại sau" }, closedSideEffects.messages);
            CollectionAssert.AreEqual(new[] { 10 }, closedSideEffects.stationIds);
            Assert.AreEqual(54, closedSideEffects.protectTicks);
            Assert.AreEqual(963, closedSideEffects.skillStateId);

            var openHost = new FakeTrapTravelHost { currentDateYmdHm = 202606080900, fightState = 0 };
            var openSideEffects = new FakeTrapActionSideEffects();
            executor = new PcTrapActionExecutor(catalog, openHost, openSideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x00000388" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1697 * 32, 3097 * 32), openHost.position);
            Assert.AreEqual(1, openHost.fightState);
            CollectionAssert.IsEmpty(openSideEffects.messages);
            CollectionAssert.AreEqual(new[] { 15 }, openSideEffects.stationIds);
            StringAssert.Contains("GetLocalDate()==202606080900", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_ClearSkillSwitchTrap_TogglesPcFightStateAndPkFlags()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 910,
                        trapIdHex = "0x0000038E",
                        scriptPath = @"\script\global\特殊用地\梦境\trap\战斗切换点4.lua",
                        actionKind = "ClearSkillSwitchTrap",
                        trapIndex = 4,
                        ifFightState = 0,
                        enterCellX = 1581,
                        enterCellY = 3166,
                        enterNextFightState = 1,
                        pkFlag = 0,
                        forbidChangePk = 1,
                        punish = 0,
                        logoutRv = 1,
                        exitCellX = 1591,
                        exitCellY = 3174,
                        exitNextFightState = 0,
                        exitPkFlag = 1,
                        exitForbidChangePk = 0,
                    }
                }
            };

            var host = new FakeTrapTravelHost { fightState = 0 };
            var executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 910 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(0, host.pkFlag);
            Assert.AreEqual(1, host.forbidChangePk);
            Assert.AreEqual(0, host.punish);
            Assert.AreEqual(1, host.logoutRv);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1581 * 32, 3166 * 32), host.position);

            host = new FakeTrapTravelHost { fightState = 1 };
            executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x0000038E" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(1, host.pkFlag);
            Assert.AreEqual(0, host.forbidChangePk);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1591 * 32, 3174 * 32), host.position);
        }

        [Test]
        public void PcTrapActionExecutor_ClearSkillLeaveGame_DerivesClearMapFromCurrentTestMap()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 911,
                        trapIdHex = "0x0000038F",
                        scriptPath = @"\script\global\特殊用地\梦境山洞\trap\梦境山洞to梦境2.lua",
                        actionKind = "ClearSkillLeaveGame",
                        trapIndex = 2,
                        fightState = 1,
                        pkFlag = 0,
                        forbidChangePk = 1,
                        punish = 0,
                        logoutRv = 1,
                        setTaskTempId = 100,
                        setTaskTempValue = 0,
                        deathScript = "",
                        reviveSubWorldId = 1,
                        enterCellX = 1741,
                        enterCellY = 3264,
                        clearSkillClearMapIds = new[] { 242, 243, 244, 245, 246, 247, 248 },
                        clearSkillTestMapBeginIds = new[] { 249, 259, 269, 279, 289, 299, 309 },
                        clearSkillTestMapCount = 10,
                    }
                }
            };

            var host = new FakeTrapTravelHost { currentMapId = 263, originalCamp = 2, fightState = 0 };
            var executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 911 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(243, host.mapId);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(0, host.pkFlag);
            Assert.AreEqual(1, host.forbidChangePk);
            Assert.AreEqual(0, host.punish);
            Assert.AreEqual(1, host.logoutRv);
            Assert.AreEqual(100, host.taskTempId);
            Assert.AreEqual(0, host.taskTempValue);
            Assert.AreEqual(2, host.curCamp);
            Assert.AreEqual(string.Empty, host.deathScript);
            Assert.IsTrue(host.leftTeam);
            Assert.AreEqual(243, host.revPosMapId);
            Assert.AreEqual(1, host.revPosId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1741 * 32, 3264 * 32), host.position);

            host = new FakeTrapTravelHost { currentMapId = 907 };
            executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 911 }, out result));

            Assert.IsFalse(result.success);
            StringAssert.Contains("CSP_GetCityIndexByTestMap", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_CsArenaLeaveTrap_UsesPcGetLeavePosTaskTriplet()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 912,
                        trapIdHex = "0x00000390",
                        scriptPath = @"\script\missions\cs竞技场\leavetrap.lua",
                        actionKind = "CsArenaLeaveTrap",
                        fightState = 1,
                        logoutRv = 0,
                        reviveMapId = 80,
                        reviveSubWorldId = 36,
                        leaveMapTaskId = 300,
                        leaveCellXTaskId = 301,
                        leaveCellYTaskId = 302,
                    }
                }
            };

            var host = new FakeTrapTravelHost
            {
                originalCamp = 2,
                fightState = 0,
                taskValues =
                {
                    [300] = 209,
                    [301] = 1548,
                    [302] = 3297,
                }
            };
            var executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 912 }, out var result));

            Assert.IsTrue(result.success);
            Assert.IsTrue(host.leftTeam);
            Assert.AreEqual(2, host.curCamp);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(0, host.logoutRv);
            Assert.AreEqual(80, host.revPosMapId);
            Assert.AreEqual(36, host.revPosId);
            Assert.AreEqual(209, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1548 * 32, 3297 * 32), host.position);

            host = new FakeTrapTravelHost
            {
                taskValues =
                {
                    [300] = 0,
                    [301] = 1548,
                    [302] = 3297,
                }
            };
            executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 912 }, out result));

            Assert.IsFalse(result.success);
            StringAssert.Contains("GetLeavePos", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_TaskTripletLeaveTrap_PreservesPcMissionLeaveSideEffects()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 913,
                        trapIdHex = "0x00000391",
                        scriptPath = @"\script\missions\citywar_arena\leavetrap.lua",
                        actionKind = "TaskTripletLeaveTrap",
                        fightState = 0,
                        logoutRv = 0,
                        reviveMapId = 99,
                        reviveSubWorldId = 43,
                        createTeam = 1,
                        deathScript = string.Empty,
                        pkFlag = 0,
                        forbidChangePk = 0,
                        setTaskTempId = 200,
                        setTaskTempValue = 0,
                        leaveMapTaskId = 300,
                        leaveCellXTaskId = 301,
                        leaveCellYTaskId = 302,
                    }
                }
            };

            var host = new FakeTrapTravelHost
            {
                originalCamp = 1,
                fightState = 1,
                taskValues =
                {
                    [300] = 20,
                    [301] = 1536,
                    [302] = 3223,
                }
            };
            var executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 913 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(1, host.curCamp);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(99, host.revPosMapId);
            Assert.AreEqual(43, host.revPosId);
            Assert.AreEqual(0, host.logoutRv);
            Assert.AreEqual(1, host.createTeam);
            Assert.AreEqual(string.Empty, host.deathScript);
            Assert.AreEqual(0, host.pkFlag);
            Assert.AreEqual(0, host.forbidChangePk);
            Assert.AreEqual(200, host.taskTempId);
            Assert.AreEqual(0, host.taskTempValue);
            Assert.AreEqual(20, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1536 * 32, 3223 * 32), host.position);
        }

        [Test]
        public void PcTrapActionExecutor_CityWarCampGateSetPos_PreservesPcCampBranches()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 908,
                        trapIdHex = "0x0000038C",
                        scriptPath = @"\script\missions\citywar_city\chengzhan_map\ctrap1.lua",
                        actionKind = "CityWarCampGateSetPos",
                        requiredCamp = 1,
                        ifFightState = 0,
                        enterCellX = 1571,
                        enterCellY = 3263,
                        enterNextFightState = 1,
                        blockedCellX = 1571,
                        blockedCellY = 3263,
                        blockedMessage = "Không thể đi được, nếu đi sẽ đến nơi phục kích của địch quân. ",
                        exitCellX = 1565,
                        exitCellY = 3246,
                        exitNextFightState = 0,
                        applyRankEffectOnEnter = true,
                    }
                }
            };

            var host = new FakeTrapTravelHost { fightState = 0, curCamp = 2, battleRank = 4 };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 908 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1571 * 32, 3263 * 32), host.position);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(4, sideEffects.rankEffect);

            host = new FakeTrapTravelHost { fightState = 1, curCamp = 2 };
            sideEffects = new FakeTrapActionSideEffects();
            executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x0000038C" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1571 * 32, 3263 * 32), host.position);
            Assert.AreEqual(1, host.fightState);
            CollectionAssert.AreEqual(new[] { "Không thể đi được, nếu đi sẽ đến nơi phục kích của địch quân. " }, sideEffects.messages);

            host = new FakeTrapTravelHost { fightState = 1, curCamp = 1 };
            executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 908 }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1565 * 32, 3246 * 32), host.position);
            Assert.AreEqual(0, host.fightState);
        }

        [Test]
        public void PcTrapActionExecutor_CityWarCampReturnNewWorld_ResetsCampAndWarps()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 909,
                        trapIdHex = "0x0000038D",
                        scriptPath = @"\script\missions\citywar_city\chengzhan_map\trap1.lua",
                        actionKind = "CityWarCampReturnNewWorld",
                        requiredCamp = 1,
                        targetMapId = 222,
                        targetCellX = 1613,
                        targetCellY = 3185,
                        fightState = 0,
                        resetCurCampToOriginal = true,
                        logoutRv = 0,
                        blockedMessage = "Không thể đi được, nếu đi sẽ đến nơi phục kích của địch quân. ",
                    }
                }
            };

            var blockedHost = new FakeTrapTravelHost { curCamp = 2 };
            var blockedEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, blockedHost, blockedEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 909 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, blockedHost.mapId);
            CollectionAssert.AreEqual(new[] { "Không thể đi được, nếu đi sẽ đến nơi phục kích của địch quân. " }, blockedEffects.messages);

            var host = new FakeTrapTravelHost { curCamp = 1, originalCamp = 0, fightState = 1 };
            executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x0000038D" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(222, host.mapId);
            Assert.AreEqual(0, host.curCamp);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(0, host.logoutRv);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1613 * 32, 3185 * 32), host.position);
        }

        [Test]
        public void PcTrapActionExecutor_TaskSetPosMessage_BranchesOnPcGetTaskValue()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 907,
                        trapIdHex = "0x0000038B",
                        scriptPath = @"\script\task\tollgate\messenger\trap\trap_fengzhiqi.lua",
                        actionKind = "TaskSetPosMessage",
                        taskId = 1201,
                        taskBranches = new[]
                        {
                            new PcTrapTaskSetPosBranch
                            {
                                values = new[] { 10 },
                                targetCellX = 1563,
                                targetCellY = 3118,
                                message = "Trước tiên phải đối thoại trước với Dịch Quan trong khu vực",
                            },
                            new PcTrapTaskSetPosBranch
                            {
                                values = new[] { 20 },
                                targetCellX = 1559,
                                targetCellY = 3113,
                            },
                            new PcTrapTaskSetPosBranch
                            {
                                values = new[] { 30, 25, 0 },
                                targetCellX = 1563,
                                targetCellY = 3118,
                                message = "Xin lỗi! Hiện tại bạn không thể vào ải được.",
                            },
                        },
                    }
                }
            };

            var host = new FakeTrapTravelHost { taskValue = 20 };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 907 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1559 * 32, 3113 * 32), host.position);
            CollectionAssert.IsEmpty(sideEffects.messages);
            StringAssert.Contains("GetTask(1201)==20", result.detail);

            host = new FakeTrapTravelHost { taskValue = 25 };
            sideEffects = new FakeTrapActionSideEffects();
            executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x0000038B" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1563 * 32, 3118 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Xin lỗi! Hiện tại bạn không thể vào ải được." }, sideEffects.messages);

            host = new FakeTrapTravelHost { taskValue = 99 };
            executor = new PcTrapActionExecutor(catalog, host, new FakeTrapActionSideEffects());

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 907 }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(default(Vector2), host.position);
            StringAssert.Contains("no branch", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_TaskOptionalMessageNewWorld_PostsOnlyMatchingPcTaskTalk()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 910,
                        trapIdHex = "0x0000038E",
                        scriptPath = @"\script\江南区\临安\莫空月居所\trap\离开.lua",
                        actionKind = "TaskOptionalMessageNewWorld",
                        taskId = 43,
                        taskBranches = new[]
                        {
                            new PcTrapTaskSetPosBranch
                            {
                                values = new[] { 100 },
                                message = "Cút mau! Đừng để ta gặp lại thấy ngươi đấy!",
                            },
                        },
                        fightState = 0,
                        targetMapId = 176,
                        targetCellX = 1413,
                        targetCellY = 2991,
                    }
                }
            };

            var host = new FakeTrapTravelHost { taskValue = 100 };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 910 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(176, host.mapId);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1413 * 32, 2991 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Cút mau! Đừng để ta gặp lại thấy ngươi đấy!" }, sideEffects.messages);

            host = new FakeTrapTravelHost { taskValue = 0 };
            sideEffects = new FakeTrapActionSideEffects();
            executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x0000038E" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(176, host.mapId);
            Assert.AreEqual(0, host.fightState);
            CollectionAssert.IsEmpty(sideEffects.messages);
            StringAssert.Contains("GetTask(43)==0", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_TaskFactionGateNewWorld_BranchesOnPcTaskAndFaction()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 0x308D7B8F,
                        trapIdHex = "0x308D7B8F",
                        scriptPath = @"\script\中原北区\天忍教\天忍教室内3\trap\天忍教室内3to天忍教圣洞1.lua",
                        actionKind = "TaskFactionGateNewWorld",
                        taskId = 4,
                        passTaskMinInclusive = 60 * 256 + 50,
                        midTaskMinExclusive = 60 * 256,
                        midTaskMaxExclusive = 60 * 256 + 50,
                        requiredFaction = "tianren",
                        requiredFactionId = (int)CombatFaction.TianRen,
                        targetMapId = 51,
                        targetCellX = 1666,
                        targetCellY = 3291,
                        fightState = 1,
                        failTargetCellX = 1749,
                        failTargetCellY = 3081,
                        message = "Bạn chưa đưa 5 thanh đoản kiếm cho Hoàn Nhan Hùng Liệt, chưa thể vào Thánh động.",
                        blockedMessage = "Đây là Thánh động Thiên Nhẫn giáo, những người đã vào không thể trở ra.",
                    }
                }
            };

            var host = new FakeTrapTravelHost
            {
                taskValue = 60 * 256 + 50,
                playerFactionId = (int)CombatFaction.TianRen,
            };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x308D7B8F" }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(51, host.mapId);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1666 * 32, 3291 * 32), host.position);
            CollectionAssert.IsEmpty(sideEffects.messages);

            host = new FakeTrapTravelHost
            {
                taskValue = 60 * 256 + 10,
                playerFactionId = (int)CombatFaction.TianRen,
            };
            sideEffects = new FakeTrapActionSideEffects();
            executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 0x308D7B8F }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1749 * 32, 3081 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Bạn chưa đưa 5 thanh đoản kiếm cho Hoàn Nhan Hùng Liệt, chưa thể vào Thánh động." }, sideEffects.messages);

            host = new FakeTrapTravelHost
            {
                taskValue = 0,
                playerFactionId = (int)CombatFaction.TianRen,
            };
            sideEffects = new FakeTrapActionSideEffects();
            executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x308D7B8F" }, out result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1749 * 32, 3081 * 32), host.position);
            CollectionAssert.AreEqual(new[] { "Đây là Thánh động Thiên Nhẫn giáo, những người đã vào không thể trở ra." }, sideEffects.messages);
        }

        [Test]
        public void PcTrapActionExecutor_RandomNewWorld_UsesPcBranchTableAndCurrentMapGuards()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 905,
                        trapIdHex = "0x00000389",
                        scriptPath = @"\script\desert_random.lua",
                        actionKind = "RandomNewWorld",
                        randomMin = 0,
                        randomMax = 120,
                        randomThresholds = new[] { 5, 10 },
                        randomTargetMapIds = new[] { 224, 225, 227 },
                        randomTargetCellXs = new[] { 1591, 1476, 1583 },
                        randomTargetCellYs = new[] { 3013, 3274, 3240 },
                        randomFightState = 1,
                        noActionMapIds = new[] { 919, 920 },
                        gateCurrentMapId = 875,
                        gateTargetMapId = 54,
                        gateTargetCellX = 1732,
                        gateTargetCellY = 3154,
                        gateFightState = 0,
                    }
                }
            };

            var host = new FakeTrapTravelHost { currentMapId = 919, randomValue = 0 };
            var executor = new PcTrapActionExecutor(catalog, host);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 905 }, out var result));
            Assert.IsTrue(result.success);
            Assert.AreEqual(-1, host.mapId);
            StringAssert.Contains("return", result.detail);

            host = new FakeTrapTravelHost { currentMapId = 875, randomValue = 0 };
            executor = new PcTrapActionExecutor(catalog, host);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 905 }, out result));
            Assert.AreEqual(54, host.mapId);
            Assert.AreEqual(0, host.fightState);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1732 * 32, 3154 * 32), host.position);

            host = new FakeTrapTravelHost { currentMapId = 224, randomValue = 116 };
            executor = new PcTrapActionExecutor(catalog, host);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x00000389" }, out result));
            Assert.AreEqual(227, host.mapId);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1583 * 32, 3240 * 32), host.position);
            StringAssert.Contains("branch#2", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_MessageRandomNewWorld_PostsPcTalkThenUsesPcRandomBranch()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 911,
                        trapIdHex = "0x0000038F",
                        scriptPath = @"\script\中原南区\伏牛山\周云泉居所\trap\离开.lua",
                        actionKind = "MessageRandomNewWorld",
                        message = "Bạn mau chóng đi xuống núi, phía sau vẫn vang lên tiếng chửi mắng của Lôi Quyết: 'Tiểu tử thối! Đừng có chạy'!",
                        randomMin = 0,
                        randomMax = 99,
                        randomThresholds = new[] { 33, 67 },
                        randomTargetMapIds = new[] { 41, 41, 41 },
                        randomTargetCellXs = new[] { 1951, 1685, 1788 },
                        randomTargetCellYs = new[] { 2989, 3268, 3085 },
                    }
                }
            };

            var host = new FakeTrapTravelHost { randomValue = 66 };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);

            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 911 }, out var result));

            Assert.IsTrue(result.success);
            Assert.AreEqual(41, host.mapId);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1685 * 32, 3268 * 32), host.position);
            CollectionAssert.AreEqual(new[]
            {
                "Bạn mau chóng đi xuống núi, phía sau vẫn vang lên tiếng chửi mắng của Lôi Quyết: 'Tiểu tử thối! Đừng có chạy'!"
            }, sideEffects.messages);
            StringAssert.Contains("Talk + random(0,99) branch#1", result.detail);
        }

        [Test]
        public void PcTrapActionExecutor_ReviveReturnNewWorld_UsesPcFixedTargetOrPlayerRevive()
        {
            var catalog = new PcTrapActionCatalogFile
            {
                entries = new[]
                {
                    new PcTrapActionCatalogEntry
                    {
                        trapId = 906,
                        trapIdHex = "0x0000038A",
                        scriptPath = @"\script\revive_return.lua",
                        actionKind = "ReviveReturnNewWorld",
                        reviveReturnMapIds = new[] { 923, 924 },
                        targetMapId = 320,
                        targetCellX = 1570,
                        targetCellY = 2337,
                        fightState = 1,
                        terminiIds = new[] { 195 },
                    }
                }
            };

            var host = new FakeTrapTravelHost { currentMapId = 320 };
            var sideEffects = new FakeTrapActionSideEffects();
            var executor = new PcTrapActionExecutor(catalog, host, sideEffects);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 906 }, out var result));
            Assert.IsTrue(result.success);
            Assert.AreEqual(320, host.mapId);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1570 * 32, 2337 * 32), host.position);
            CollectionAssert.AreEqual(new[] { 195 }, sideEffects.terminiIds);

            var reviveWorld = MapEnemyDatabase.MpsToWorld(51104, 102592);
            host = new FakeTrapTravelHost
            {
                currentMapId = 923,
                hasReviveTarget = true,
                reviveMapId = 1,
                revivePosition = reviveWorld,
            };
            sideEffects = new FakeTrapActionSideEffects();
            executor = new PcTrapActionExecutor(catalog, host, sideEffects);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x0000038A" }, out result));
            Assert.IsTrue(result.success);
            Assert.AreEqual(1, host.mapId);
            Assert.AreEqual(reviveWorld, host.position);
            CollectionAssert.IsEmpty(sideEffects.terminiIds);
            StringAssert.Contains("RevID2WXY(GetPlayerRev())", result.detail);

            host = new FakeTrapTravelHost { currentMapId = 924, hasReviveTarget = false };
            executor = new PcTrapActionExecutor(catalog, host);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 906 }, out result));
            Assert.IsFalse(result.success);
            StringAssert.Contains("GetPlayerRev", result.detail);
        }

        [Test]
        public void MapTrapRuntime_DisablesStaticTrapsForVuotAiMissionMaps()
        {
            var go = new GameObject("trap-runtime-test");
            try
            {
                var runtime = go.AddComponent<MapTrapRuntime>();
                runtime.BuildForMap(new MapDefinition
                {
                    catalogEntry = new MapCatalogEntry
                    {
                        mapId = MapPortManifest.VuotAiNhiepThiTranId,
                        geometryKey = "g_a7649e666581b845",
                    }
                });

                Assert.AreEqual(0, runtime.ActiveTriggerCount);
                Assert.AreEqual(16, runtime.DisabledTrapCount);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
