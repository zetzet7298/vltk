using System;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public static class SandboxTeleportExtensions
    {
        public static void HandleWharfTeleport(this SandboxManager sandbox, int wharfId)
        {
            var wharf = sandbox.WharfService?.GetWharf(wharfId);
            if (wharf == null)
            {
                SubsystemLog.Warn("Sandbox", $"Wharf {wharfId} not found in WharfService.");
                return;
            }

            SubsystemLog.Info("Sandbox", $"Wharf teleport triggered: {wharf.nameVi} -> Map={wharf.toMapId}, Pos=({wharf.toX}, {wharf.toY})");
            // Find map coords (wharf.toX, wharf.toY are usually in standard script coordinates, divide by 32 for unity world pos)
            var worldPos = new Vector2(wharf.toX / 32f, wharf.toY / 32f); 
            
            // Switch Map and Place Player
            if (sandbox.MapManager?.ActiveMapId != wharf.toMapId)
            {
                sandbox.SwitchMapAndPlacePlayer(wharf.toMapId, worldPos);
            }
            else
            {
                sandbox.PlayerController?.PlaceAt(worldPos, snapCamera: true);
            }
        }
    }
}
