// -----------------------------------------------------------------------------
// VLTK Mobile — Runtime service cho dịch chuyển bằng lệnh bài (PC waypoint.txt).
//
// PC source: settings/waypoint.txt = 225 rows. Each row = one waypoint (a
// destination the player can teleport to by consuming a station item). Format:
// ID, DESC, SECT(map,x,y), FightState, RequiredLevel. Travel flow:
//   1. Validate waypoint exists
//   2. Validate player level ≥ RequiredLevel
//   3. Validate player is not in a no-travel map (jail/instance/etc.)
//   4. Consume the waypoint item (lệnh bài)
//   5. SetPos(player, waypoint.MapId, x, y)
//   6. SetFightState(player, waypoint.FightState)
//   7. SendMessage to player with arrival info
//
// Used by station-travel (wharf / horse-caravan / faction-portal / etc.).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>Decision result for a waypoint travel request.</summary>
    public readonly struct PcWaypointTravelDecision
    {
        public readonly bool Allowed;
        public readonly PcWaypointEntry Waypoint;
        public readonly string ReasonVi;

        public PcWaypointTravelDecision(bool allowed, PcWaypointEntry waypoint, string reasonVi)
        {
            Allowed = allowed;
            Waypoint = waypoint;
            ReasonVi = reasonVi ?? string.Empty;
        }

        public override string ToString()
            => Allowed ? $"OK waypoint={Waypoint?.WaypointId} map={Waypoint?.MapId}" : $"DENY: {ReasonVi}";
    }

    /// <summary>Plan for a waypoint travel: dispatch instructions for the host.</summary>
    public sealed class PcWaypointTravelPlan
    {
        public PcWaypointEntry Waypoint;
        public bool ConsumeItem;
        public bool SetPos;
        public bool SetFightState;
        public bool SendMessage;
        public string MessageVi;

        public override string ToString()
            => $"waypoint={Waypoint?.WaypointId} target={Waypoint?.MapId}({Waypoint?.PosX},{Waypoint?.PosY}) "
               + $"fight={Waypoint?.FightState} consume={ConsumeItem} msg={SendMessage}";
    }

    /// <summary>Host seam for waypoint travel side effects.</summary>
    public interface IWaypointHost
    {
        int GetPlayerLevel(string player);
        bool IsInNoTravelMap(string player);
        bool ConsumeWaypointItem(string player, int waypointId);
        void SetPos(string player, int mapId, int x, int y);
        void SetFightState(string player, int fightState);
        void SendMessage(string player, string message);
    }

    public class WaypointTravelService
    {
        public const string LogTag = "WaypointTravel";

        private readonly PcWaypointRegistry _registry;
        private readonly IWaypointHost _host;

        public WaypointTravelService(PcWaypointRegistry registry, IWaypointHost host)
        {
            _registry = registry;
            _host = host;
        }

        /// <summary>
        /// Decide whether a waypoint travel is allowed (no host calls — pure
        /// data validation).
        /// </summary>
        public PcWaypointTravelDecision DecideTravel(
            int waypointId,
            int playerLevel,
            int currentMapId)
        {
            if (_registry == null)
                return new PcWaypointTravelDecision(false, null, "NoRegistry");
            var wp = _registry.Get(waypointId);
            if (wp == null)
                return new PcWaypointTravelDecision(false, null, "UnknownWaypoint");
            if (playerLevel < wp.RequiredLevel)
                return new PcWaypointTravelDecision(false, wp, "LevelTooLow");
            if (wp.MapId == currentMapId)
                return new PcWaypointTravelDecision(false, wp, "AlreadyAtDestination");
            return new PcWaypointTravelDecision(true, wp, string.Empty);
        }

        /// <summary>Build a full travel plan. Pure data, no host calls.</summary>
        public PcWaypointTravelPlan BuildPlan(PcWaypointEntry waypoint)
        {
            return new PcWaypointTravelPlan
            {
                Waypoint = waypoint,
                ConsumeItem = true,
                SetPos = true,
                SetFightState = true,
                SendMessage = true,
                MessageVi = waypoint == null
                    ? string.Empty
                    : $"Dịch chuyển tới {waypoint.Name} (bản đồ {waypoint.MapId}).",
            };
        }

        /// <summary>Execute a plan via the host. Caller must have already Decided.</summary>
        public bool ExecutePlan(string player, PcWaypointTravelPlan plan)
        {
            if (plan == null || plan.Waypoint == null || _host == null) return false;
            if (_host.IsInNoTravelMap(player)) return false;
            if (plan.ConsumeItem)
            {
                if (!_host.ConsumeWaypointItem(player, plan.Waypoint.WaypointId))
                    return false;
            }
            if (plan.SetPos)
                _host.SetPos(player, plan.Waypoint.MapId, plan.Waypoint.PosX, plan.Waypoint.PosY);
            if (plan.SetFightState)
                _host.SetFightState(player, plan.Waypoint.FightState);
            if (plan.SendMessage && !string.IsNullOrEmpty(plan.MessageVi))
                _host.SendMessage(player, plan.MessageVi);
            return true;
        }
    }
}
