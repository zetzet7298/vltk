// -----------------------------------------------------------------------------
// VLTK Mobile — RegionStreamingService EditMode tests.
// Kiểm tra region stream lifecycle: ComputeDesired, Update (load/unload),
// MarkLoaded/MarkFailed, host dispatch chain (load/unload/overlay/save).
// PC source: M1.9 region streaming + lua region_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class RegionStreamingUpdateTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IRegionStreamingHost
        {
            public int LoadStartCalls;
            public int LoadCompleteCalls;
            public int LoadFailedCalls;
            public int UnloadCalls;
            public int OverlayCalls;
            public int SfxCalls;
            public int LogCalls;
            public int SaveCalls;
            public RegionCoord LastRegion;
            public int LastActiveX;
            public int LastActiveY;
            public int LastLoadedCount;
            public int LastMaxLoaded;
            public RegionStreamState LastSaveState;

            public void OnRegionLoadStarted(RegionCoord region, int activeRegionX, int activeRegionY)
            {
                LoadStartCalls++;
                LastRegion = region;
                LastActiveX = activeRegionX;
                LastActiveY = activeRegionY;
            }
            public void OnRegionLoaded(RegionCoord region, int loadTimeMs) { LoadCompleteCalls++; }
            public void OnRegionLoadFailed(RegionCoord region, string errorMessage) { LoadFailedCalls++; }
            public void OnRegionUnloaded(RegionCoord region, int activeRegionX, int activeRegionY) { UnloadCalls++; }
            public void UpdateRegionOverlay(RegionCoord activeRegion, int loadedCount, int maxLoaded)
            {
                OverlayCalls++;
                LastLoadedCount = loadedCount;
                LastMaxLoaded = maxLoaded;
            }
            public void PlayRegionLoadSFX(RegionCoord region) { SfxCalls++; }
            public void LogRegionEvent(RegionCoord region, string message) { LogCalls++; }
            public void SaveRegionState(RegionCoord region, RegionStreamState state, int loadedCount)
            {
                SaveCalls++;
                LastSaveState = state;
            }
        }

        private static RegionStreamingService BuildService(int countX = 5, int countY = 5, int ringRadius = 1, int maxLoaded = 9, IRegionStreamingHost host = null)
            => new RegionStreamingService(countX, countY, 100f, 100f, Vector2.zero, ringRadius, maxLoaded, host);

        // ── WorldToRegion ────────────────────────────────────────────────────

        [Test]
        public void WorldToRegion_Origin()
        {
            var svc = BuildService();
            var r = svc.WorldToRegion(Vector2.zero);
            Assert.AreEqual(0, r.x);
            Assert.AreEqual(0, r.y);
        }

        [Test]
        public void WorldToRegion_Offset()
        {
            var svc = BuildService();
            var r = svc.WorldToRegion(new Vector2(150f, 250f));
            Assert.AreEqual(1, r.x);
            Assert.AreEqual(2, r.y);
        }

        [Test]
        public void WorldToRegion_NegativeOrigin()
        {
            var svc = new RegionStreamingService(5, 5, 100f, 100f, new Vector2(50f, 50f));
            var r = svc.WorldToRegion(new Vector2(0f, 0f));
            Assert.AreEqual(-1, r.x);
        }

        // ── InBounds ─────────────────────────────────────────────────────────

        [Test]
        public void InBounds_True()
        {
            var svc = BuildService(3, 3);
            Assert.IsTrue(svc.InBounds(new RegionCoord(0, 0)));
            Assert.IsTrue(svc.InBounds(new RegionCoord(2, 2)));
        }

        [Test]
        public void InBounds_False()
        {
            var svc = BuildService(3, 3);
            Assert.IsFalse(svc.InBounds(new RegionCoord(3, 0)));
            Assert.IsFalse(svc.InBounds(new RegionCoord(-1, 0)));
        }

        // ── ComputeDesired ───────────────────────────────────────────────────

        [Test]
        public void ComputeDesired_Radius1_3x3()
        {
            var svc = BuildService(5, 5, 1, 9);
            var desired = svc.ComputeDesired(new RegionCoord(2, 2));
            Assert.AreEqual(9, desired.Count);
        }

        [Test]
        public void ComputeDesired_Radius0_1()
        {
            var svc = BuildService(5, 5, 0, 9);
            var desired = svc.ComputeDesired(new RegionCoord(2, 2));
            Assert.AreEqual(1, desired.Count);
        }

        [Test]
        public void ComputeDesired_OutOfBounds_Empty()
        {
            var svc = BuildService(5, 5, 1, 9);
            var desired = svc.ComputeDesired(new RegionCoord(99, 99));
            Assert.AreEqual(0, desired.Count);
        }

        [Test]
        public void ComputeDesired_ClampedToBounds()
        {
            var svc = BuildService(3, 3, 1, 9);
            var desired = svc.ComputeDesired(new RegionCoord(0, 0));
            // 3x3 corner → 4 cells (only the ones in bounds)
            Assert.AreEqual(4, desired.Count);
        }

        [Test]
        public void ComputeDesired_MaxLoadedCap()
        {
            var svc = BuildService(10, 10, 2, 5); // 5x5 = 25 desired, capped to 5
            var desired = svc.ComputeDesired(new RegionCoord(5, 5));
            Assert.AreEqual(5, desired.Count);
        }

        [Test]
        public void ComputeDesired_ClosestFirst()
        {
            var svc = BuildService(5, 5, 1, 9);
            var desired = svc.ComputeDesired(new RegionCoord(2, 2));
            // First should be (2,2) (distance 0)
            Assert.AreEqual(new RegionCoord(2, 2), desired[0]);
        }

        // ── Update ───────────────────────────────────────────────────────────

        [Test]
        public void Update_FirstCall_SetsActive()
        {
            var svc = BuildService();
            var plan = svc.Update(new Vector2(150f, 150f));
            Assert.IsTrue(plan.activeInBounds);
            Assert.IsTrue(svc.HasActive);
            Assert.AreEqual(new RegionCoord(1, 1), svc.ActiveRegion);
        }

        [Test]
        public void Update_OutOfBounds_NoChurn()
        {
            var svc = BuildService();
            var plan = svc.Update(new Vector2(10000f, 10000f));
            Assert.IsFalse(plan.activeInBounds);
            Assert.IsFalse(svc.HasActive);
        }

        [Test]
        public void Update_LoadingState()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            Assert.AreEqual(RegionStreamState.Loading, svc.GetState(new RegionCoord(1, 1)));
        }

        [Test]
        public void Update_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host: host);
            svc.Update(new Vector2(150f, 150f));
            Assert.AreEqual(9, host.LoadStartCalls); // 3x3
            Assert.AreEqual(1, host.OverlayCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void Update_NoNewRegions_NoLoadDispatch()
        {
            var host = new FakeHost();
            var svc = BuildService(host: host);
            svc.Update(new Vector2(150f, 150f));
            host.LoadStartCalls = 0;
            host.OverlayCalls = 0;
            svc.Update(new Vector2(150f, 150f));
            Assert.AreEqual(0, host.LoadStartCalls);
        }

        [Test]
        public void Update_MoveToNewRegion_UnloadsOld()
        {
            var host = new FakeHost();
            var svc = new RegionStreamingService(10, 10, 100f, 100f, Vector2.zero, 1, 9, host);
            svc.Update(new Vector2(50f, 50f)); // region (0,0)
            host.UnloadCalls = 0;
            svc.Update(new Vector2(550f, 50f)); // region (5,0)
            Assert.IsTrue(host.UnloadCalls > 0);
        }

        [Test]
        public void Update_LoadsNewRegionsOnMove()
        {
            var host = new FakeHost();
            var svc = new RegionStreamingService(10, 10, 100f, 100f, Vector2.zero, 1, 9, host);
            svc.Update(new Vector2(50f, 50f));
            int initialLoad = host.LoadStartCalls;
            svc.Update(new Vector2(550f, 50f));
            Assert.IsTrue(host.LoadStartCalls > initialLoad);
        }

        [Test]
        public void Update_FiresOnStreamingPlanEvent()
        {
            var svc = BuildService();
            int fired = 0;
            svc.OnStreamingPlan += p => fired++;
            svc.Update(new Vector2(150f, 150f));
            Assert.AreEqual(1, fired);
        }

        // ── MarkLoaded ───────────────────────────────────────────────────────

        [Test]
        public void MarkLoaded_TransitionsToLoaded()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            svc.MarkLoaded(new RegionCoord(1, 1));
            Assert.AreEqual(RegionStreamState.Loaded, svc.GetState(new RegionCoord(1, 1)));
        }

        [Test]
        public void MarkLoaded_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = BuildService(host: host);
            svc.Update(new Vector2(150f, 150f));
            int before = host.LoadCompleteCalls;
            int sfxBefore = host.SfxCalls;
            svc.MarkLoaded(new RegionCoord(1, 1));
            Assert.AreEqual(before + 1, host.LoadCompleteCalls);
            Assert.AreEqual(sfxBefore + 1, host.SfxCalls);
        }

        [Test]
        public void MarkLoaded_NotInLoading_NoStateChange()
        {
            var svc = BuildService();
            svc.MarkLoaded(new RegionCoord(0, 0));
            Assert.AreEqual(RegionStreamState.Unloaded, svc.GetState(new RegionCoord(0, 0)));
        }

        [Test]
        public void MarkLoaded_WithoutHost_DoesNotThrow()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            Assert.DoesNotThrow(() => svc.MarkLoaded(new RegionCoord(1, 1)));
        }

        // ── MarkFailed ───────────────────────────────────────────────────────

        [Test]
        public void MarkFailed_TransitionsToFailed()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Region.*failed to load.*"));
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            svc.MarkFailed(new RegionCoord(1, 1));
            Assert.AreEqual(RegionStreamState.Failed, svc.GetState(new RegionCoord(1, 1)));
        }

        [Test]
        public void MarkFailed_DispatchesHost()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Region.*failed to load.*"));
            var host = new FakeHost();
            var svc = BuildService(host: host);
            svc.Update(new Vector2(150f, 150f));
            svc.MarkFailed(new RegionCoord(1, 1));
            Assert.AreEqual(1, host.LoadFailedCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.AreEqual(RegionStreamState.Failed, host.LastSaveState);
        }

        [Test]
        public void MarkFailed_WithoutHost_DoesNotThrow()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(".*Region.*failed to load.*"));
            var svc = BuildService();
            Assert.DoesNotThrow(() => svc.MarkFailed(new RegionCoord(0, 0)));
        }

        // ── GetState / LoadedCount ──────────────────────────────────────────

        [Test]
        public void GetState_UnloadedRegion_ReturnsUnloaded()
        {
            var svc = BuildService();
            Assert.AreEqual(RegionStreamState.Unloaded, svc.GetState(new RegionCoord(0, 0)));
        }

        [Test]
        public void LoadedCount_AfterLoading()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            Assert.AreEqual(9, svc.LoadedCount);
        }

        [Test]
        public void LoadedCount_AfterMarkedLoaded()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            svc.MarkLoaded(new RegionCoord(1, 1));
            Assert.AreEqual(9, svc.LoadedCount); // still counted
        }

        [Test]
        public void LoadedCount_AfterUnload()
        {
            var svc = BuildService();
            svc.Update(new Vector2(0f, 0f));
            int initial = svc.LoadedCount;
            svc.Update(new Vector2(500f, 0f));
            Assert.IsTrue(svc.LoadedCount <= initial);
        }

        // ── State colors ─────────────────────────────────────────────────────

        [Test]
        public void GetStateColor_Loaded_Green()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            svc.MarkLoaded(new RegionCoord(1, 1));
            Assert.AreEqual(Color.green, svc.GetStateColor(new RegionCoord(1, 1)));
        }

        [Test]
        public void GetStateColor_Loading_Yellow()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            Assert.AreEqual(Color.yellow, svc.GetStateColor(new RegionCoord(1, 1)));
        }

        [Test]
        public void GetStateColor_Failed_Red()
        {
            var svc = BuildService();
            svc.Update(new Vector2(150f, 150f));
            svc.MarkFailed(new RegionCoord(1, 1));
            Assert.AreEqual(Color.red, svc.GetStateColor(new RegionCoord(1, 1)));
        }

        [Test]
        public void GetStateColor_Unloaded_Gray()
        {
            var svc = BuildService();
            Assert.AreEqual(Color.gray, svc.GetStateColor(new RegionCoord(0, 0)));
        }

        // ── PlayerId / AttachHost ───────────────────────────────────────────

        [Test]
        public void PlayerId_Default()
        {
            var svc = BuildService();
            Assert.AreEqual(0, svc.PlayerId);
        }

        [Test]
        public void AttachHost_Replaces()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var svc = new RegionStreamingService(5, 5, 100f, 100f, Vector2.zero, 1, 9, host1);
            svc.AttachHost(host2);
            svc.Update(new Vector2(150f, 150f));
            Assert.AreEqual(0, host1.LoadStartCalls);
            Assert.AreEqual(9, host2.LoadStartCalls);
        }
    }
}
