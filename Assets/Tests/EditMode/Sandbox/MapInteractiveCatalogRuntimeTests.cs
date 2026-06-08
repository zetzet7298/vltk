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
