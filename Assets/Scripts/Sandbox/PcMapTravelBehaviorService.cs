// -----------------------------------------------------------------------------
// VLTK Mobile — PcMapTravelBehaviorService
// Performs actual scene map switching and player teleportation based on
// resolved PcMapTravelActionResult.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    public interface IMapTeleportHost
    {
        bool HasMap(int mapId);
        void SwitchMapAndPlacePlayer(int mapId, Vector2 worldPosition);
    }

    public sealed class PcMapTravelBehaviorService
    {
        private readonly IMapTeleportHost _host;

        public PcMapTravelBehaviorService(IMapTeleportHost host)
        {
            _host = host;
        }

        public GmItemActionResult ExecuteTravelAction(PcMapTravelActionResult actionResult)
        {
            if (actionResult == null)
                return GmItemActionResult.Invalid("Action result null.");

            if (!actionResult.HasTeleport)
                return GmItemActionResult.Blocked(actionResult.Message ?? "Không thể dịch chuyển.");

            if (_host == null)
                return GmItemActionResult.NotPorted("Chưa có TeleportHost.");

            var mapId = actionResult.TargetMapId;
            if (!_host.HasMap(mapId))
                return GmItemActionResult.NotPorted($"Map đích {mapId} chưa có trong catalog.");

            // Convert PC coordinates to Unity world coordinates.
            Vector2 worldPosition;
            if (actionResult.Kind == PcMapTravelActionKind.WaypointTeleport || actionResult.Kind == PcMapTravelActionKind.ScrollValue)
            {
                // Waypoint and Scroll targets usually use Cell coordinates in PC data.
                worldPosition = MapEnemyDatabase.MpsToWorld(actionResult.X * 32, actionResult.Y * 32);
            }
            else
            {
                // Revive uses MPS coordinates directly.
                worldPosition = MapEnemyDatabase.MpsToWorld(actionResult.X, actionResult.Y);
            }

            _host.SwitchMapAndPlacePlayer(mapId, worldPosition);
            Debug.Log($"[Travel] TravelAction -> map {mapId} {worldPosition} kind={actionResult.Kind}");
            return GmItemActionResult.Success(actionResult.Message ?? $"Đã chuyển tới map {mapId}.");
        }
    }
}
