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
            public void NewWorld(int targetMapId, Vector2 worldPosition)
            {
                mapId = targetMapId;
                position = worldPosition;
            }
            public void SetPos(Vector2 worldPosition) => position = worldPosition;
            public void SetFightState(int nextFightState) => fightState = nextFightState;
        }

        [Test]
        public void TrapActionCatalog_LoadsDeterministicPcNewWorldActions()
        {
            var catalog = PcTrapActionCatalogRuntime.LoadFromStreamingAssets();

            Assert.IsNotNull(catalog);
            Assert.AreEqual(533, catalog.Count);
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
