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
            public int curCamp = 0;
            public int originalCamp = 0;
            public int battleRank = 0;
            public int logoutRv = -1;

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
            public int GetTaskValue(int taskId) => taskValue;
            public int GetCurCamp() => curCamp;
            public int GetCamp() => originalCamp;
            public int GetBattleRank() => battleRank;
            public int GetFightState() => fightState;
            public void NewWorld(int targetMapId, Vector2 worldPosition)
            {
                mapId = targetMapId;
                position = worldPosition;
            }
            public void SetPos(Vector2 worldPosition) => position = worldPosition;
            public void SetFightState(int nextFightState) => fightState = nextFightState;
            public void SetCurCamp(int nextCamp) => curCamp = nextCamp;
            public void SetLogoutRv(int value) => logoutRv = value;
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

            public void PostMessage(string nextMessage)
            {
                message = nextMessage;
                messages.Add(nextMessage);
            }
            public void AddEventItem(int eventItemId) => eventItems.Add(eventItemId);
            public void AddNote(string note) => notes.Add(note);
        }

        [Test]
        public void TrapActionCatalog_LoadsDeterministicPcNewWorldActions()
        {
            var catalog = PcTrapActionCatalogRuntime.LoadFromStreamingAssets();

            Assert.IsNotNull(catalog);
            Assert.AreEqual(692, catalog.Count);
            Assert.AreEqual(112, catalog.entries.Count(e => e != null && e.IsFightStateSetPos));
            Assert.AreEqual(25, catalog.entries.Count(e => e != null && e.IsMessageOnly));
            Assert.AreEqual(22, catalog.entries.Count(e => e != null && e.IsSayMessage));
            Assert.AreEqual(2, catalog.entries.Count(e => e != null && e.IsTalkMessage));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsMsg2Player));
            Assert.AreEqual(2, catalog.entries.Count(e => e != null && e.IsMsg2PlayerNewWorld));
            Assert.AreEqual(20, catalog.entries.Count(e => e != null && e.IsLevelGateNewWorld));
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
            Assert.AreEqual(166, catalog.Count);
            Assert.AreEqual(7, catalog.entries.Count(e => e != null && e.IsNewWorld));
            Assert.AreEqual(16, catalog.entries.Count(e => e != null && e.IsPickupMessage));
            Assert.AreEqual(142, catalog.entries.Count(e => e != null && e.IsSayMessage));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsTalkMessage));
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
                    }
                }
            };

            var host = new FakeTrapTravelHost { currentMapId = 320 };
            var executor = new PcTrapActionExecutor(catalog, host);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapId = 906 }, out var result));
            Assert.IsTrue(result.success);
            Assert.AreEqual(320, host.mapId);
            Assert.AreEqual(1, host.fightState);
            Assert.AreEqual(MapEnemyDatabase.MpsToWorld(1570 * 32, 2337 * 32), host.position);

            var reviveWorld = MapEnemyDatabase.MpsToWorld(51104, 102592);
            host = new FakeTrapTravelHost
            {
                currentMapId = 923,
                hasReviveTarget = true,
                reviveMapId = 1,
                revivePosition = reviveWorld,
            };
            executor = new PcTrapActionExecutor(catalog, host);
            Assert.IsTrue(executor.TryExecute(new TrapDefinition { trapIdHex = "0x0000038A" }, out result));
            Assert.IsTrue(result.success);
            Assert.AreEqual(1, host.mapId);
            Assert.AreEqual(reviveWorld, host.position);
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
