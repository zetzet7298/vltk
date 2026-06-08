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
            public int fightState = -1;

            public bool HasMap(int targetMapId) => hasMap;
            public int GetFightState() => fightState;
            public void NewWorld(int targetMapId, Vector2 worldPosition)
            {
                mapId = targetMapId;
                position = worldPosition;
            }
            public void SetPos(Vector2 worldPosition) => position = worldPosition;
            public void SetFightState(int nextFightState) => fightState = nextFightState;
        }

        private sealed class FakeTrapActionSideEffects : ITrapActionSideEffects
        {
            public readonly List<string> messages = new();

            public void PostMessage(string message) => messages.Add(message);
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
            Assert.AreEqual(670, catalog.Count);
            Assert.AreEqual(112, catalog.entries.Count(e => e != null && e.IsFightStateSetPos));
            Assert.AreEqual(25, catalog.entries.Count(e => e != null && e.IsMessageOnly));
            Assert.AreEqual(22, catalog.entries.Count(e => e != null && e.IsSayMessage));
            Assert.AreEqual(2, catalog.entries.Count(e => e != null && e.IsTalkMessage));
            Assert.AreEqual(1, catalog.entries.Count(e => e != null && e.IsMsg2Player));
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
