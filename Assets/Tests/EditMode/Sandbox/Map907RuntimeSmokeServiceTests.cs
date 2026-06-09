using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public sealed class Map907RuntimeSmokeServiceTests
    {
        [Test]
        public void Run_ProvesMap907CatalogBoundsMinimapAndTrapReadiness()
        {
            var result = new Map907RuntimeSmokeService().Run();

            Assert.IsTrue(result.Success, result.MissingRuntimeDependency);
            Assert.GreaterOrEqual(result.CatalogCount, 1005);
            Assert.AreEqual(MapPortManifest.VuotAiNhiepThiTranId, result.ActiveMapId);
            Assert.AreEqual("Vượt ải Nhiếp Thí Trần", result.MapName);
            Assert.AreEqual("g_a7649e666581b845", result.GeometryKey);
            Assert.IsTrue(result.BoundsUsable);
            Assert.IsTrue(result.BoundsMatchCommittedData);
            Assert.IsTrue(result.TrapCatalogLoaded);
            Assert.IsTrue(result.TrapGeometryFound);
            Assert.AreEqual(16, result.TrapCount);
            Assert.AreEqual(0, result.ObjectCount);
            Assert.IsTrue(result.StaticTrapClearForMap);
            Assert.IsTrue(result.AllTrapScriptsResolved);
            StringAssert.Contains("Scene/player feel smoke", string.Join("\n", result.Notes));
        }

        [Test]
        public void Run_UsesCurrentCommittedMap907CoordinateFacts()
        {
            var result = new Map907RuntimeSmokeService().Run();

            Assert.IsTrue(result.Success, result.MissingRuntimeDependency);
            Assert.AreEqual(39424f, result.Bounds.x);
            Assert.AreEqual(-56320f, result.Bounds.y);
            Assert.AreEqual(14848f, result.Bounds.width);
            Assert.AreEqual(7168f, result.Bounds.height);
            Assert.AreEqual(new Vector2(47232f, -52544f), result.RepresentativeWorld);
            Assert.AreEqual(new Vector2(54272f, -56320f), result.ClampedOutOfBoundsTarget);
            Assert.AreEqual(new Vector2(54272f, -56320f), result.MinimapBottomRightClickWorld);
        }

        [Test]
        public void Run_ProvesMap907MinimapRoundTripForRepresentativeSpawn()
        {
            var result = new Map907RuntimeSmokeService().Run();

            Assert.IsTrue(result.Success, result.MissingRuntimeDependency);
            Assert.AreEqual(0.5258621f, result.RepresentativeNormalized.x, 0.0001f);
            Assert.AreEqual(0.5267857f, result.RepresentativeNormalized.y, 0.0001f);
            Assert.AreEqual(134.6207f, result.RepresentativePixel.x, 0.0001f);
            Assert.AreEqual(121.1429f, result.RepresentativePixel.y, 0.0001f);
            Assert.AreEqual(47232f, result.RoundTripWorld.x, 0.05f);
            Assert.AreEqual(-52544f, result.RoundTripWorld.y, 0.05f);
            Assert.IsTrue(result.RoundTripMatchesRepresentative);
        }
        [Test]
        public void Run_WhenGivenSandboxPlayerController_ProvesRuntimeMoveToClamp()
        {
            var go = new GameObject("map907-runtime-smoke-player-clamp");
            try
            {
                var controller = go.AddComponent<SandboxPlayerController>();

                var result = new Map907RuntimeSmokeService().Run(playerController: controller);

                Assert.IsTrue(result.Success, result.MissingRuntimeDependency);
                Assert.IsTrue(result.PlayerControllerClampProbeRan);
                Assert.IsTrue(result.PlayerControllerClampMatchesCommittedData);
                Assert.AreEqual(new Vector2(54272f, -56320f), result.PlayerControllerMoveTarget);
                Assert.IsTrue(controller.HasMoveTarget);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

    }
}
