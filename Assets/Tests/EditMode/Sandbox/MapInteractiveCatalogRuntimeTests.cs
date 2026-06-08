using System.Linq;
using NUnit.Framework;
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
        }
    }
}
