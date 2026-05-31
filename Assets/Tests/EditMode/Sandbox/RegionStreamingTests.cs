using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M1.9 — Region Streaming service tests. Pure-logic coverage of the
    /// desired-set computation, deterministic boundary load/unload, budget cap,
    /// failure handling, and GM overlay color mapping (AC#1–AC#5).
    /// </summary>
    public class RegionStreamingTests
    {
        // 10x10 grid, 100x100 world units per region, origin at (0,0).
        private RegionStreamingService MakeService(int ring = 1, int maxLoaded = 9)
            => new RegionStreamingService(
                countX: 10, countY: 10,
                regionWidth: 100f, regionHeight: 100f,
                worldOrigin: Vector2.zero,
                ringRadius: ring,
                maxLoaded: maxLoaded);

        // Center of region (rx, ry).
        private Vector2 RegionCenter(int rx, int ry)
            => new Vector2(rx * 100f + 50f, ry * 100f + 50f);

        // --- WorldToRegion mapping ---

        [Test]
        public void WorldToRegion_MapsToGridCell()
        {
            var svc = MakeService();
            Assert.AreEqual(new RegionCoord(0, 0), svc.WorldToRegion(new Vector2(50f, 50f)));
            Assert.AreEqual(new RegionCoord(3, 2), svc.WorldToRegion(new Vector2(350f, 250f)));
            Assert.AreEqual(new RegionCoord(9, 9), svc.WorldToRegion(new Vector2(950f, 950f)));
        }

        [Test]
        public void InBounds_RejectsOutOfGrid()
        {
            var svc = MakeService();
            Assert.IsTrue(svc.InBounds(new RegionCoord(0, 0)));
            Assert.IsTrue(svc.InBounds(new RegionCoord(9, 9)));
            Assert.IsFalse(svc.InBounds(new RegionCoord(-1, 0)));
            Assert.IsFalse(svc.InBounds(new RegionCoord(10, 0)));
        }

        // --- AC#1: active + neighbor ring loads on map load ---

        [Test]
        public void Update_FirstLoad_LoadsActivePlusRing()
        {
            var svc = MakeService(ring: 1);
            var plan = svc.Update(RegionCenter(5, 5));

            Assert.IsTrue(plan.activeInBounds);
            Assert.AreEqual(new RegionCoord(5, 5), plan.active);
            // 3x3 ring around (5,5) = 9 regions, all newly loading.
            Assert.AreEqual(9, plan.toLoad.Count);
            Assert.AreEqual(0, plan.toUnload.Count);
            Assert.Contains(new RegionCoord(5, 5), plan.toLoad);
            Assert.Contains(new RegionCoord(4, 4), plan.toLoad);
            Assert.Contains(new RegionCoord(6, 6), plan.toLoad);
            foreach (var c in plan.toLoad)
                Assert.AreEqual(RegionStreamState.Loading, svc.GetState(c));
        }

        [Test]
        public void Update_FirstLoad_AtCorner_ClampsRingToBounds()
        {
            var svc = MakeService(ring: 1);
            var plan = svc.Update(RegionCenter(0, 0));
            // Corner (0,0): only (0,0),(1,0),(0,1),(1,1) are in bounds.
            Assert.AreEqual(4, plan.toLoad.Count);
            Assert.Contains(new RegionCoord(0, 0), plan.toLoad);
            Assert.Contains(new RegionCoord(1, 1), plan.toLoad);
        }

        // --- AC#2: boundary crossing loads/unloads deterministically ---

        [Test]
        public void Update_CrossBoundary_LoadsAndUnloadsDeterministically()
        {
            var svc = MakeService(ring: 1);
            svc.Update(RegionCenter(5, 5));
            // Simulate all loads completing.
            foreach (var c in new List<RegionCoord>(svc.States.Keys)) svc.MarkLoaded(c);

            // Move one region to the right -> active (6,5).
            var plan = svc.Update(RegionCenter(6, 5));

            Assert.AreEqual(new RegionCoord(6, 5), plan.active);
            // Column x=4 leaves; column x=7 enters. 3 unload, 3 load.
            Assert.AreEqual(3, plan.toLoad.Count);
            Assert.AreEqual(3, plan.toUnload.Count);
            foreach (var c in plan.toUnload)
            {
                Assert.AreEqual(4, c.x);
                Assert.AreEqual(RegionStreamState.Unloaded, svc.GetState(c));
            }
            foreach (var c in plan.toLoad)
                Assert.AreEqual(7, c.x);
        }

        [Test]
        public void Update_SamePosition_NoChurn()
        {
            var svc = MakeService(ring: 1);
            svc.Update(RegionCenter(5, 5));
            foreach (var c in new List<RegionCoord>(svc.States.Keys)) svc.MarkLoaded(c);

            var plan = svc.Update(RegionCenter(5, 5));
            Assert.AreEqual(0, plan.toLoad.Count);
            Assert.AreEqual(0, plan.toUnload.Count);
        }

        [Test]
        public void Update_IsDeterministic_AcrossInstances()
        {
            var a = MakeService(ring: 2);
            var b = MakeService(ring: 2);
            var pa = a.Update(RegionCenter(5, 5));
            var pb = b.Update(RegionCenter(5, 5));
            CollectionAssert.AreEqual(pa.toLoad, pb.toLoad);
            CollectionAssert.AreEqual(pa.toUnload, pb.toUnload);
        }

        // --- AC#5: max-loaded budget respected ---

        [Test]
        public void ComputeDesired_RespectsBudget_NearestWins()
        {
            // ring 2 -> 5x5 = 25 candidates, but budget caps at 9.
            var svc = MakeService(ring: 2, maxLoaded: 9);
            var desired = svc.ComputeDesired(new RegionCoord(5, 5));
            Assert.AreEqual(9, desired.Count);
            // Active must always be included (distance 0).
            Assert.Contains(new RegionCoord(5, 5), desired);
            // A distance-2 region should be dropped in favor of nearer ones.
            Assert.IsFalse(desired.Contains(new RegionCoord(7, 7)));
        }

        [Test]
        public void Update_NeverExceedsBudget()
        {
            var svc = MakeService(ring: 3, maxLoaded: 9);
            svc.Update(RegionCenter(5, 5));
            Assert.LessOrEqual(svc.LoadedCount, svc.MaxLoaded);
            Assert.LessOrEqual(svc.States.Count, svc.MaxLoaded);
        }

        // --- AC#4: failure handling, runtime continues ---

        [Test]
        public void MarkFailed_SetsFailedState_DoesNotThrow()
        {
            var svc = MakeService(ring: 1);
            svc.Update(RegionCenter(5, 5));
            var target = new RegionCoord(5, 5);
            LogAssert.Expect(LogType.Error, "[RegionStreaming] Region (5,5) failed to load");
            Assert.DoesNotThrow(() => svc.MarkFailed(target));
            Assert.AreEqual(RegionStreamState.Failed, svc.GetState(target));
            // Subsequent updates still function (runtime continues).
            Assert.DoesNotThrow(() => svc.Update(RegionCenter(6, 5)));
        }

        [Test]
        public void Update_PlayerLeavesMap_KeepsStateNoChurn()
        {
            var svc = MakeService(ring: 1);
            svc.Update(RegionCenter(5, 5));
            int before = svc.States.Count;

            var plan = svc.Update(new Vector2(-500f, -500f)); // off the grid
            Assert.IsFalse(plan.activeInBounds);
            Assert.AreEqual(0, plan.toLoad.Count);
            Assert.AreEqual(0, plan.toUnload.Count);
            Assert.AreEqual(before, svc.States.Count);
        }

        // --- AC#3: GM overlay color mapping ---

        [Test]
        public void GetStateColor_MapsEachState()
        {
            var svc = MakeService(ring: 1);
            svc.Update(RegionCenter(5, 5));
            var loading = new RegionCoord(5, 5);
            Assert.AreEqual(Color.yellow, svc.GetStateColor(loading));

            svc.MarkLoaded(loading);
            Assert.AreEqual(Color.green, svc.GetStateColor(loading));

            LogAssert.Expect(LogType.Error, "[RegionStreaming] Region (5,5) failed to load");
            svc.MarkFailed(loading);
            Assert.AreEqual(Color.red, svc.GetStateColor(loading));

            // Unknown / unloaded region -> gray.
            Assert.AreEqual(Color.gray, svc.GetStateColor(new RegionCoord(0, 0)));
        }
    }
}
