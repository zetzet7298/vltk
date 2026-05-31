using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.PlayMode
{
    /// <summary>
    /// E2E PlayMode tests for the live region-streaming MonoBehaviour. Drives
    /// RegionStreamController through real GameObject/Transform input and the frame
    /// loop, exercising AC#1 (active+ring load), AC#2 (deterministic boundary
    /// crossing), AC#4 (failed region keeps runtime running), and AC#5 (budget).
    /// This is the integrated path EditMode (pure service) tests cannot cover.
    /// </summary>
    public class RegionStreamingE2ETests
    {
        private GameObject _root;

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.DestroyImmediate(_root);
            _root = null;
        }

        private RegionStreamController MakeController(out Transform target,
            List<RegionCoord> loaded, List<RegionCoord> unloaded, RegionCoord? failAt = null)
        {
            _root = new GameObject("StreamRoot");
            var ctrlGo = new GameObject("RegionStreamController");
            ctrlGo.transform.SetParent(_root.transform, false);
            var ctrl = ctrlGo.AddComponent<RegionStreamController>(); // Awake builds service

            var targetGo = new GameObject("Target");
            targetGo.transform.SetParent(_root.transform, false);
            target = targetGo.transform;

            ctrl.regionCountX = 10;
            ctrl.regionCountY = 10;
            ctrl.regionWidth = 100f;
            ctrl.regionHeight = 100f;
            ctrl.worldOrigin = Vector2.zero;
            ctrl.ringRadius = 1;
            ctrl.maxLoaded = 9;
            ctrl.target = target;
            ctrl.BuildService(); // rebuild with the configured grid

            var fail = failAt;
            ctrl.LoadRegion = c =>
            {
                if (fail.HasValue && c.Equals(fail.Value)) return false;
                loaded.Add(c);
                return true;
            };
            ctrl.UnloadRegion = c => unloaded.Add(c);
            return ctrl;
        }

        private Vector2 RegionCenter(int rx, int ry) => new Vector2(rx * 100f + 50f, ry * 100f + 50f);

        // --- AC#1: first tick loads active + neighbor ring ---

        [UnityTest]
        public IEnumerator E2E_FirstTick_LoadsActivePlusRing()
        {
            var loaded = new List<RegionCoord>();
            var unloaded = new List<RegionCoord>();
            var ctrl = MakeController(out var target, loaded, unloaded);

            var plan = ctrl.Tick(RegionCenter(5, 5));
            yield return null;

            Assert.IsNotNull(plan);
            Assert.AreEqual(9, plan.toLoad.Count, "3x3 ring around (5,5) should load 9 regions");
            Assert.AreEqual(9, loaded.Count, "Load callback should fire for each region");
            Assert.AreEqual(RegionStreamState.Loaded, ctrl.Service.GetState(new RegionCoord(5, 5)));
        }

        // --- AC#2: boundary crossing loads/unloads; same region = no churn ---

        [UnityTest]
        public IEnumerator E2E_SameRegion_NoChurn()
        {
            var loaded = new List<RegionCoord>();
            var unloaded = new List<RegionCoord>();
            var ctrl = MakeController(out var target, loaded, unloaded);

            ctrl.Tick(RegionCenter(5, 5));
            int afterFirst = loaded.Count;
            // Tick again within the same region (slightly different pos) → no new work.
            var plan = ctrl.Tick(new Vector2(5 * 100f + 60f, 5 * 100f + 40f));
            yield return null;

            Assert.IsNull(plan, "Re-tick inside same region should return null (no boundary crossing)");
            Assert.AreEqual(afterFirst, loaded.Count, "No additional loads within the same region");
        }

        [UnityTest]
        public IEnumerator E2E_CrossBoundary_LoadsAndUnloads()
        {
            var loaded = new List<RegionCoord>();
            var unloaded = new List<RegionCoord>();
            var ctrl = MakeController(out var target, loaded, unloaded);

            ctrl.Tick(RegionCenter(5, 5));
            loaded.Clear();
            var plan = ctrl.Tick(RegionCenter(6, 5)); // move one region east
            yield return null;

            Assert.IsNotNull(plan);
            Assert.AreEqual(3, plan.toLoad.Count, "Column x=7 enters → 3 new regions");
            Assert.AreEqual(3, plan.toUnload.Count, "Column x=4 leaves → 3 regions unloaded");
            Assert.AreEqual(3, unloaded.Count, "Unload callback should fire 3 times");
        }

        // --- AC#4: failed region marked, runtime keeps running ---

        [UnityTest]
        public IEnumerator E2E_FailedRegion_MarkedAndRuntimeContinues()
        {
            var loaded = new List<RegionCoord>();
            var unloaded = new List<RegionCoord>();
            var failAt = new RegionCoord(5, 5);
            // The MarkFailed path logs an error; expect it so the test does not fail.
            LogAssert.Expect(LogType.Error, "[RegionStreaming] Region (5,5) failed to load");
            var ctrl = MakeController(out var target, loaded, unloaded, failAt);

            ctrl.Tick(RegionCenter(5, 5));
            yield return null;

            Assert.AreEqual(RegionStreamState.Failed, ctrl.Service.GetState(failAt),
                "Failed region should be marked Failed");
            // Runtime continues: a subsequent boundary cross still works.
            var plan = ctrl.Tick(RegionCenter(6, 5));
            yield return null;
            Assert.IsNotNull(plan, "Runtime should continue streaming after a region failure");
        }

        // --- AC#5: budget cap respected through the frame loop ---

        [UnityTest]
        public IEnumerator E2E_BudgetCap_NeverExceeded()
        {
            var loaded = new List<RegionCoord>();
            var unloaded = new List<RegionCoord>();
            var ctrl = MakeController(out var target, loaded, unloaded);
            ctrl.ringRadius = 3; // would want 7x7=49 regions
            ctrl.maxLoaded = 9;
            ctrl.BuildService();
            ctrl.LoadRegion = c => { loaded.Add(c); return true; };

            ctrl.Tick(RegionCenter(5, 5));
            yield return null;

            Assert.LessOrEqual(ctrl.Service.LoadedCount, ctrl.maxLoaded,
                "Loaded region count must respect the mobile memory budget");
        }

        // --- Update loop: driving via Transform + frame ticks ---

        [UnityTest]
        public IEnumerator E2E_UpdateLoop_StreamsFromTransform()
        {
            var loaded = new List<RegionCoord>();
            var unloaded = new List<RegionCoord>();
            var ctrl = MakeController(out var target, loaded, unloaded);
            ctrl.updateInterval = 0f; // evaluate every frame

            target.position = RegionCenter(5, 5);
            yield return null; // Update() runs and ticks
            yield return null;

            Assert.IsTrue(ctrl.Service.HasActive, "Update loop should drive streaming from the target transform");
            Assert.AreEqual(new RegionCoord(5, 5), ctrl.Service.ActiveRegion);
        }
    }
}
