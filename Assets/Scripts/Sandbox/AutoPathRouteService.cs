// -----------------------------------------------------------------------------
// VLTK Mobile — AutoPathRouteService: runtime service cho auto path finding
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class AutoPathRouteService
    {
        private readonly PcAutoPathRouteRegistry _reg;
        private IAutoPathRouteServiceHost _host;
        public int Count => _reg?.Count ?? 0;

        public AutoPathRouteService() { }
        public AutoPathRouteService(PcAutoPathRouteRegistry reg) { _reg = reg ?? new PcAutoPathRouteRegistry(); }

        public void AttachHost(IAutoPathRouteServiceHost host) { _host = host; }

        public static AutoPathRouteService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            var svc = new AutoPathRouteService(PcAutoPathRouteParser.BuildRegistry(path));
            if (svc._host != null)
            {
                svc._host.OnAllRoutesQueried(svc.Count);
                svc._host.LogRouteEvent("load", 0, $"Loaded {svc.Count} routes");
                svc._host.PlayRouteSFX("load", 0);
            }
            return svc;
        }

        public PcAutoPathRouteEntry GetRoute(int id)
        {
            var r = _reg.Get(id);
            if (_host != null)
            {
                if (r != null)
                    _host.OnRouteResolved(r.RouteId, r.FromMapId, r.ToMapId, r.Distance, r.WaypointSequence?.Count ?? 0);
                else
                    _host.LogRouteEvent("query_missing", id, "Route not found in registry");
            }
            return r;
        }
        public IEnumerable<PcAutoPathRouteEntry> GetByFromTo(int fromMap, int toMap)
        {
            int count = 0;
            foreach (var e in _reg.GetByFromTo(fromMap, toMap))
            {
                count++;
                yield return e;
            }
            if (_host != null)
                _host.OnRoutesByFromToQueried(fromMap, toMap, count);
        }

        public void StartNavigation(int routeId)
        {
            var r = GetRoute(routeId);
            if (r == null) return;
            if (_host != null)
            {
                _host.OnRouteNavigationStarted(r.RouteId, r.FromMapId, r.ToMapId, r.WaypointSequence?.Count ?? 0);
                _host.ShowRouteUI(r.RouteId, r.FromMapId, r.ToMapId);
                _host.LogRouteEvent("start", r.RouteId, $"Start nav from map {r.FromMapId} to {r.ToMapId}");
                _host.PlayRouteSFX("start", r.RouteId);
                _host.SaveRouteState(r.RouteId, r.FromMapId, r.ToMapId);
            }
        }

        public void ReachWaypoint(int routeId, int waypointIndex, int waypointMapId)
        {
            if (_host != null)
            {
                _host.OnWaypointReached(routeId, waypointIndex, waypointMapId);
                _host.LogRouteEvent("waypoint", routeId, $"Reached waypoint {waypointIndex} on map {waypointMapId}");
                _host.PlayRouteSFX("waypoint", routeId);
            }
        }

        public void FinishNavigation(int routeId, bool success, int reachedWaypoints)
        {
            if (_host != null)
            {
                _host.OnRouteNavigationFinished(routeId, success, reachedWaypoints);
                _host.LogRouteEvent(success ? "end" : "interrupt", routeId, success ? $"Nav done at waypoint {reachedWaypoints}" : $"Nav interrupted at waypoint {reachedWaypoints}");
                _host.PlayRouteSFX(success ? "end" : "interrupt", routeId);
            }
        }
    }
}
