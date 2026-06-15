// -----------------------------------------------------------------------------
// VLTK Mobile — Auto Path Route Service Host Interface (Unity → sandbox)
// Runtime service for auto path finding / route queries between maps.
// Unity runtime dispatches load / route query events to a host implementation
// that owns UI (path preview, mini-map polyline), save/load (favorite routes).
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host boundary cho <see cref="AutoPathRouteService"/>. Decouples sandbox
    /// logic (registry parse, route lookup) from Unity-side UI (path preview
    /// line, mini-map polyline, marker placement).
    ///
    /// All methods are best-effort callbacks. Implementations must tolerate
    /// null/invalid args — sandbox never throws.
    /// </summary>
    public interface IAutoPathRouteServiceHost
    {
        // ── Query dispatch ────────────────────────────────────────────────
        /// <summary>GetRoute resolved by id — null if not found.</summary>
        void OnRouteResolved(int routeId, int fromMapId, int toMapId, int distance, int waypointCount);

        /// <summary>GetByFromTo — count of routes for the given from/to map.</summary>
        void OnRoutesByFromToQueried(int fromMapId, int toMapId, int resultCount);

        /// <summary>All routes snapshot — count of routes in registry.</summary>
        void OnAllRoutesQueried(int resultCount);

        // ── Path navigation dispatch (called by gameplay code) ────────────
        /// <summary>Start navigation along a route.</summary>
        void OnRouteNavigationStarted(int routeId, int fromMapId, int toMapId, int totalWaypoints);

        /// <summary>Waypoint reached during navigation.</summary>
        void OnWaypointReached(int routeId, int waypointIndex, int waypointMapId);

        /// <summary>Navigation finished (success or interrupted).</summary>
        void OnRouteNavigationFinished(int routeId, bool success, int reachedWaypoints);

        // ── UI / SFX / Persistence ────────────────────────────────────────
        /// <summary>Show route preview line on the world map.</summary>
        void ShowRouteUI(int routeId, int fromMapId, int toMapId);

        /// <summary>Log a route event (query, start, waypoint) for the GM / log file.</summary>
        void LogRouteEvent(string eventType, int routeId, string detailVi);

        /// <summary>Play a route-related SFX: "load" / "start" / "waypoint" / "end".</summary>
        void PlayRouteSFX(string action, int routeId);

        /// <summary>Save the active route / favorite routes to local cache.</summary>
        void SaveRouteState(int routeId, int fromMapId, int toMapId);
    }
}
