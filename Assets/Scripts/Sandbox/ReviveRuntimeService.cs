// -----------------------------------------------------------------------------
// VLTK Mobile — Runtime service cho hồi sinh (PC revivepos.ini).
//
// PC source: settings/revivepos.ini = 139 map sections / 241 coordinate rows.
// Format: [mapId], region=start,end, x,y. When a player dies in map M at
// (x, y):
//   - if M is in revivepos.ini AND (x, y) ∈ [regionStart, regionEnd] of M
//     → revive at section (x, y) [same map, walk-back]
//   - if M is in revivepos.ini but no region OR (x, y) is outside the region
//     → teleport to the default city revive (PC main city / faction city)
//   - if M is NOT in revivepos.ini (mission/instanced map)
//     → teleport to the player's bound city (faction-specific)
//
// This is the PC engine logic for choosing WHERE to revive. The actual scene
// move + fight-state reset is dispatched via IReviveHost.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>Mode of revival determined by the death position and map config.</summary>
    public enum ReviveMode
    {
        /// <summary>Player died in safe zone (no section in revivepos.ini for this map).</summary>
        InPlace = 0,
        /// <summary>Player died in a region of a revivepos.ini map → revive at section (x, y).</summary>
        WalkBack = 1,
        /// <summary>Player died outside any region → teleport to default city revive.</summary>
        TeleportCity = 2,
        /// <summary>Player died in instanced/mission map → teleport to bound city.</summary>
        TeleportBoundCity = 3,
    }

    /// <summary>Resolved revive position + mode.</summary>
    public readonly struct PcRevivePosition
    {
        public readonly ReviveMode Mode;
        public readonly int TargetMapId;
        public readonly int TargetX;
        public readonly int TargetY;
        public readonly int SourceMapId;
        public readonly string ReasonVi;

        public PcRevivePosition(ReviveMode mode, int targetMapId, int tx, int ty,
            int sourceMapId, string reasonVi)
        {
            Mode = mode;
            TargetMapId = targetMapId;
            TargetX = tx;
            TargetY = ty;
            SourceMapId = sourceMapId;
            ReasonVi = reasonVi ?? string.Empty;
        }

        public override string ToString()
            => $"{Mode} map:{SourceMapId}→{TargetMapId} ({TargetX},{TargetY}) [{ReasonVi}]";
    }

    /// <summary>Plan to execute: revive position + set of host calls to make.</summary>
    public sealed class PcRevivePlan
    {
        public PcRevivePosition Position;
        public bool ResetFightState;
        public int DefaultFightState;
        public bool SendMessage;
        public string MessageVi;

        public override string ToString()
            => $"{Position} resetFight={ResetFightState} fightState={DefaultFightState} sendMsg={SendMessage}";
    }

    /// <summary>
    /// Default city revive coordinates (PC: each faction has a different "main
    /// city" revive map; we use one shared default for now and let the host
    /// pick the faction-specific one).
    /// </summary>
    public readonly struct PcReviveCity
    {
        public readonly int MapId;
        public readonly int PosX;
        public readonly int PosY;
        public readonly string FactionVi;

        public PcReviveCity(int mapId, int x, int y, string factionVi)
        {
            MapId = mapId; PosX = x; PosY = y; FactionVi = factionVi ?? string.Empty;
        }
    }

    /// <summary>
    /// Host seam for revive side effects (set player pos, set fight state,
    /// send message).
    /// </summary>
    public interface IReviveHost
    {
        int GetCurrentMapId(string player);
        (int x, int y) GetCurrentPos(string player);
        void SetPos(string player, int mapId, int x, int y);
        void SetFightState(string player, int fightState);
        void SendMessage(string player, string message);
    }

    public class ReviveRuntimeService
    {
        public const string LogTag = "ReviveRuntime";

        private readonly PcRevivePosRegistry _registry;
        private readonly IReviveHost _host;
        private readonly PcReviveCity _defaultCity;

        public ReviveRuntimeService(
            PcRevivePosRegistry registry,
            IReviveHost host,
            PcReviveCity defaultCity)
        {
            _registry = registry;
            _host = host;
            _defaultCity = defaultCity;
        }

        /// <summary>
        /// Resolve where a player should revive given their death location.
        /// </summary>
        public PcRevivePosition ResolveRevive(int playerMapId, int deathX, int deathY)
        {
            if (_registry == null)
            {
                return new PcRevivePosition(ReviveMode.TeleportCity,
                    _defaultCity.MapId, _defaultCity.PosX, _defaultCity.PosY,
                    playerMapId, "NoRegistry");
            }

            // PC: walk back if the section's [regionStart, regionEnd] contains
            // the death X. revivepos.ini is a list keyed by ReviveId, but the
            // format groups entries by mapId (the parser preserves MapId).
            // For a given mapId, PC looks for an entry whose [regionStart,
            // regionEnd] brackets the death X; if multiple, the first wins.
            PcRevivePosEntry walkBackEntry = null;
            bool sectionExists = false;
            bool hasRegion = false;
            foreach (var e in _registry.GetByMap(playerMapId))
            {
                sectionExists = true;
                if (e.RegionStart == 0 && e.RegionEnd == 0)
                {
                    // No region — city map. PC: revive in place.
                    return new PcRevivePosition(ReviveMode.InPlace,
                        playerMapId, e.PosX, e.PosY, playerMapId, "CityMap");
                }
                hasRegion = true;
                if (deathX >= e.RegionStart && deathX <= e.RegionEnd)
                {
                    walkBackEntry = e;
                    break;
                }
            }

            if (!sectionExists)
            {
                // Map not in revivepos.ini → instanced/mission map → bound city
                return new PcRevivePosition(ReviveMode.TeleportBoundCity,
                    _defaultCity.MapId, _defaultCity.PosX, _defaultCity.PosY,
                    playerMapId, "MapNotInReviveTable");
            }

            if (walkBackEntry != null)
            {
                return new PcRevivePosition(ReviveMode.WalkBack,
                    playerMapId, walkBackEntry.PosX, walkBackEntry.PosY,
                    playerMapId, "InRegion");
            }

            // Has region but death is outside all regions → main city
            return new PcRevivePosition(ReviveMode.TeleportCity,
                _defaultCity.MapId, _defaultCity.PosX, _defaultCity.PosY,
                playerMapId, "OutOfRegion");
        }

        /// <summary>
        /// Build a full revive plan (position + host-call plan). Does NOT
        /// execute. Caller invokes the host methods on the plan.
        /// </summary>
        public PcRevivePlan BuildPlan(int playerMapId, int deathX, int deathY, int deathType)
        {
            var pos = ResolveRevive(playerMapId, deathX, deathY);
            var plan = new PcRevivePlan
            {
                Position = pos,
                ResetFightState = true,
                // PC: revive puts player in non-PK fight state (0 = peace)
                DefaultFightState = 0,
                SendMessage = pos.Mode != ReviveMode.InPlace,
            };

            switch (pos.Mode)
            {
                case ReviveMode.InPlace:
                    plan.MessageVi = "Hồi sinh tại chỗ.";
                    break;
                case ReviveMode.WalkBack:
                    plan.MessageVi = "Hồi sinh tại điểm hồi sinh bản đồ.";
                    break;
                case ReviveMode.TeleportCity:
                    plan.MessageVi = "Hồi sinh tại thành chính.";
                    break;
                case ReviveMode.TeleportBoundCity:
                    plan.MessageVi = "Hồi sinh tại thành phái.";
                    break;
            }

            if (deathType == 1)
            {
                // PK death: also restore full HP and apply PK penalty message
                plan.MessageVi = "[PK] " + plan.MessageVi + " (Phạt giết người)";
            }

            return plan;
        }

        /// <summary>Execute the plan via the host. Returns true if host calls were issued.</summary>
        public bool ExecutePlan(string player, PcRevivePlan plan)
        {
            if (plan == null || _host == null) return false;
            _host.SetPos(player, plan.Position.TargetMapId, plan.Position.TargetX, plan.Position.TargetY);
            if (plan.ResetFightState)
                _host.SetFightState(player, plan.DefaultFightState);
            if (plan.SendMessage && !string.IsNullOrEmpty(plan.MessageVi))
                _host.SendMessage(player, plan.MessageVi);
            return true;
        }
    }
}
