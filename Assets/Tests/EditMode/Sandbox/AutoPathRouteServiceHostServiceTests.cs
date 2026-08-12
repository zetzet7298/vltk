// -----------------------------------------------------------------------------
// VLTK Mobile — AutoPathRouteService host dispatch tests
// Runtime service for auto path finding / route queries between maps.
// Verifies IAutoPathRouteServiceHost receives expected events for load / query /
// start / waypoint / finish navigation.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class AutoPathRouteServiceHostServiceTests
    {
        private sealed class FakeHost : IAutoPathRouteServiceHost
        {
            public int ResolvedCalls;
            public int LastResolvedRouteId;
            public int LastResolvedFromMap;
            public int LastResolvedToMap;
            public int LastResolvedDistance;
            public int LastResolvedWaypointCount;

            public int ByFromToQueriedCalls;
            public int LastByFromToFromMap;
            public int LastByFromToToMap;
            public int LastByFromToResultCount;

            public int AllQueriedCalls;
            public int LastAllResultCount;

            public int NavStartedCalls;
            public int LastNavRouteId;
            public int LastNavFromMap;
            public int LastNavToMap;
            public int LastNavTotalWaypoints;

            public int WaypointReachedCalls;
            public int LastWaypointRouteId;
            public int LastWaypointIndex;
            public int LastWaypointMapId;

            public int NavFinishedCalls;
            public int LastNavFinishedRouteId;
            public bool LastNavFinishedSuccess;
            public int LastNavFinishedReachedWaypoints;

            public int UIShowCalls;
            public int LastUIRouteId;
            public int LastUIFromMap;
            public int LastUIToMap;

            public int LogCalls;
            public int LastLogRouteId;
            public string LastLogEventType;
            public string LastLogDetail;

            public int SFXCalls;
            public int LastSFXRouteId;
            public string LastSFXAction;

            public int SaveCalls;
            public int LastSaveRouteId;
            public int LastSaveFromMap;
            public int LastSaveToMap;

            public void OnRouteResolved(int routeId, int fromMapId, int toMapId, int distance, int waypointCount)
            {
                ResolvedCalls++;
                LastResolvedRouteId = routeId;
                LastResolvedFromMap = fromMapId;
                LastResolvedToMap = toMapId;
                LastResolvedDistance = distance;
                LastResolvedWaypointCount = waypointCount;
            }
            public void OnRoutesByFromToQueried(int fromMapId, int toMapId, int resultCount)
            {
                ByFromToQueriedCalls++;
                LastByFromToFromMap = fromMapId;
                LastByFromToToMap = toMapId;
                LastByFromToResultCount = resultCount;
            }
            public void OnAllRoutesQueried(int resultCount)
            {
                AllQueriedCalls++;
                LastAllResultCount = resultCount;
            }
            public void OnRouteNavigationStarted(int routeId, int fromMapId, int toMapId, int totalWaypoints)
            {
                NavStartedCalls++;
                LastNavRouteId = routeId;
                LastNavFromMap = fromMapId;
                LastNavToMap = toMapId;
                LastNavTotalWaypoints = totalWaypoints;
            }
            public void OnWaypointReached(int routeId, int waypointIndex, int waypointMapId)
            {
                WaypointReachedCalls++;
                LastWaypointRouteId = routeId;
                LastWaypointIndex = waypointIndex;
                LastWaypointMapId = waypointMapId;
            }
            public void OnRouteNavigationFinished(int routeId, bool success, int reachedWaypoints)
            {
                NavFinishedCalls++;
                LastNavFinishedRouteId = routeId;
                LastNavFinishedSuccess = success;
                LastNavFinishedReachedWaypoints = reachedWaypoints;
            }
            public void ShowRouteUI(int routeId, int fromMapId, int toMapId)
            {
                UIShowCalls++;
                LastUIRouteId = routeId;
                LastUIFromMap = fromMapId;
                LastUIToMap = toMapId;
            }
            public void LogRouteEvent(string eventType, int routeId, string detailVi)
            {
                LogCalls++;
                LastLogEventType = eventType;
                LastLogRouteId = routeId;
                LastLogDetail = detailVi;
            }
            public void PlayRouteSFX(string action, int routeId)
            {
                SFXCalls++;
                LastSFXAction = action;
                LastSFXRouteId = routeId;
            }
            public void SaveRouteState(int routeId, int fromMapId, int toMapId)
            {
                SaveCalls++;
                LastSaveRouteId = routeId;
                LastSaveFromMap = fromMapId;
                LastSaveToMap = toMapId;
            }
        }

        private static PcAutoPathRouteRegistry MakeRegistry()
        {
            var reg = new PcAutoPathRouteRegistry();
            reg.Add(new PcAutoPathRouteEntry
            {
                RouteId = 1, FromMapId = 200, ToMapId = 201, Distance = 500,
                WaypointSequence = new List<int> { 1, 2, 3, 4 },
            });
            reg.Add(new PcAutoPathRouteEntry
            {
                RouteId = 2, FromMapId = 201, ToMapId = 202, Distance = 300,
                WaypointSequence = new List<int> { 5, 6, 7 },
            });
            return reg;
        }

        // ── Ctor / AttachHost ───────────────────────────────────────────────
        [Test]
        public void Ctor_Default_Empty()
        {
            var svc = new AutoPathRouteService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachHost_NullSafe()
        {
            var svc = new AutoPathRouteService();
            Assert.DoesNotThrow(() => svc.AttachHost(null));
        }

        // ── GetRoute dispatch ───────────────────────────────────────────────
        [Test]
        public void GetRoute_Found_DispatchesResolved()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            var r = svc.GetRoute(1);
            Assert.IsNotNull(r);
            Assert.AreEqual(baseline + 1, host.ResolvedCalls);
            Assert.AreEqual(1, host.LastResolvedRouteId);
            Assert.AreEqual(200, host.LastResolvedFromMap);
            Assert.AreEqual(201, host.LastResolvedToMap);
            Assert.AreEqual(500, host.LastResolvedDistance);
            Assert.AreEqual(4, host.LastResolvedWaypointCount);
        }

        [Test]
        public void GetRoute_Missing_LogsButNoResolve()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            int baseline = host.ResolvedCalls;
            int baselineLog = host.LogCalls;
            var r = svc.GetRoute(9999);
            Assert.IsNull(r);
            Assert.AreEqual(baseline, host.ResolvedCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("query_missing", host.LastLogEventType);
        }

        // ── GetByFromTo dispatch ───────────────────────────────────────────
        [Test]
        public void GetByFromTo_DispatchesHostCount()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            int n = 0;
            foreach (var _ in svc.GetByFromTo(200, 201)) n++;
            Assert.AreEqual(1, n);
            Assert.AreEqual(1, host.ByFromToQueriedCalls);
            Assert.AreEqual(200, host.LastByFromToFromMap);
            Assert.AreEqual(201, host.LastByFromToToMap);
            Assert.AreEqual(1, host.LastByFromToResultCount);
        }

        [Test]
        public void GetByFromTo_NoMatch_DispatchesZero()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            int n = 0;
            foreach (var _ in svc.GetByFromTo(999, 888)) n++;
            Assert.AreEqual(0, n);
            Assert.AreEqual(1, host.ByFromToQueriedCalls);
            Assert.AreEqual(0, host.LastByFromToResultCount);
        }

        // ── StartNavigation dispatch ────────────────────────────────────────
        [Test]
        public void StartNavigation_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            int baselineUI = host.UIShowCalls;
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            int baselineSave = host.SaveCalls;
            svc.StartNavigation(1);
            Assert.AreEqual(1, host.NavStartedCalls);
            Assert.AreEqual(1, host.LastNavRouteId);
            Assert.AreEqual(200, host.LastNavFromMap);
            Assert.AreEqual(201, host.LastNavToMap);
            Assert.AreEqual(4, host.LastNavTotalWaypoints);
            Assert.AreEqual(baselineUI + 1, host.UIShowCalls);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("start", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("start", host.LastSFXAction);
            Assert.AreEqual(baselineSave + 1, host.SaveCalls);
        }

        [Test]
        public void StartNavigation_UnknownRoute_NoDispatch()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            int baseline = host.NavStartedCalls;
            svc.StartNavigation(9999);
            Assert.AreEqual(baseline, host.NavStartedCalls);
        }

        // ── ReachWaypoint dispatch ──────────────────────────────────────────
        [Test]
        public void ReachWaypoint_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            int baselineLog = host.LogCalls;
            int baselineSFX = host.SFXCalls;
            svc.ReachWaypoint(1, 2, 203);
            Assert.AreEqual(1, host.WaypointReachedCalls);
            Assert.AreEqual(1, host.LastWaypointRouteId);
            Assert.AreEqual(2, host.LastWaypointIndex);
            Assert.AreEqual(203, host.LastWaypointMapId);
            Assert.AreEqual(baselineLog + 1, host.LogCalls);
            Assert.AreEqual("waypoint", host.LastLogEventType);
            Assert.AreEqual(baselineSFX + 1, host.SFXCalls);
            Assert.AreEqual("waypoint", host.LastSFXAction);
        }

        // ── FinishNavigation dispatch ──────────────────────────────────────
        [Test]
        public void FinishNavigation_Success_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            svc.FinishNavigation(1, true, 4);
            Assert.AreEqual(1, host.NavFinishedCalls);
            Assert.IsTrue(host.LastNavFinishedSuccess);
            Assert.AreEqual(4, host.LastNavFinishedReachedWaypoints);
            Assert.AreEqual("end", host.LastLogEventType);
            Assert.AreEqual("end", host.LastSFXAction);
        }

        [Test]
        public void FinishNavigation_Interrupt_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new AutoPathRouteService(MakeRegistry());
            svc.AttachHost(host);
            svc.FinishNavigation(1, false, 2);
            Assert.IsFalse(host.LastNavFinishedSuccess);
            Assert.AreEqual(2, host.LastNavFinishedReachedWaypoints);
            Assert.AreEqual("interrupt", host.LastLogEventType);
            Assert.AreEqual("interrupt", host.LastSFXAction);
        }

        // ── No-host path is silent ─────────────────────────────────────────
        [Test]
        public void NoHost_OperationsDoNotThrow()
        {
            var svc = new AutoPathRouteService(MakeRegistry());
            Assert.DoesNotThrow(() => svc.GetRoute(1));
            Assert.DoesNotThrow(() => { foreach (var _ in svc.GetByFromTo(200, 201)) { } });
            Assert.DoesNotThrow(() => svc.StartNavigation(1));
            Assert.DoesNotThrow(() => svc.ReachWaypoint(1, 0, 0));
            Assert.DoesNotThrow(() => svc.FinishNavigation(1, true, 0));
        }
    }
}
