using System.Reflection;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.EditMode.Sandbox
{
    /// <summary>
    /// M_visual — Ba Lăng huyện (BLH) visual-load regression. Locks the
    /// <see cref="MapRenderer"/> local-data mapId remap so the BLH region
    /// files at <c>TestData/Regions/Map_79_C/</c> are picked up when the
    /// catalog says map 53 (PC <c>maplist.ini</c> canonical id for 巴陵县).
    ///
    /// Without this remap the renderer would look for
    /// <c>TestData/Regions/Map_53_C/</c> (which does not exist on disk),
    /// fall through to "No test region data available", and leave the BLH
    /// map unrendered.
    /// </summary>
    public class MapRendererLocalDataMapIdRemapTests
    {
        private static System.Collections.Generic.Dictionary<int, int> GetRemap()
        {
            var field = typeof(MapRenderer).GetField(
                "LocalDataMapIdOverrides",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field, "MapRenderer.LocalDataMapIdOverrides must exist.");
            return (System.Collections.Generic.Dictionary<int, int>)field.GetValue(null);
        }

        [Test]
        public void BalangCatalogId_ResolvesToLocalDataFolderId()
        {
            // PC maplist.ini: 53 = 巴陵县 (Ba Lăng huyện). Local TestData/Regions/ uses 79.
            var remap = GetRemap();
            Assert.IsTrue(remap.ContainsKey(53),
                "Ba Lăng huyện (PC mapId 53) must remap to the local data-folder mapId.");
            Assert.AreEqual(79, remap[53],
                "Map_79_C/ on disk is the only place that holds the BLH region data.");
        }

        [Test]
        public void OtherMaps_NotInRemap_UseTheirOwnCatalogId()
        {
            // Maps that don't have a data-vs-catalog mismatch must NOT be in the
            // remap (otherwise we'd accidentally redirect a working map to the
            // wrong folder).
            var remap = GetRemap();
            Assert.IsFalse(remap.ContainsKey(1),  "Phượng Tường (1) matches PC + local catalog; no remap needed.");
            Assert.IsFalse(remap.ContainsKey(11), "Thành Đô (11) matches PC + local catalog; no remap needed.");
            Assert.IsFalse(remap.ContainsKey(78), "Tương Dương (78) matches PC + local catalog; no remap needed.");
            Assert.IsFalse(remap.ContainsKey(389), "Phong Kỳ 120+ (389) matches PC + local catalog; no remap needed.");
            Assert.IsFalse(remap.ContainsKey(907), "Vượt ải Nhiếp Thí Trần (907) matches PC + local catalog; no remap needed.");
            Assert.IsFalse(remap.ContainsKey(319), "Lâm Du Quan (319) matches PC + local catalog; no remap needed.");
        }
    }
}
