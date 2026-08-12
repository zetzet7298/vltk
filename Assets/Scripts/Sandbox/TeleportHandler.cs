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
                Debug.LogWarning($"[Sandbox] Wharf {wharfId} not found in WharfService.");
                return;
            }

            Debug.Log($"[Sandbox] Wharf teleport triggered: {wharf.Name} -> Map={wharf.FromMapId}, Pos=({wharf.PosX}, {wharf.PosY})");
            // Find map coords (wharf.PosX, wharf.PosY are usually in standard script coordinates, divide by 32 for unity world pos)
            var worldPos = new Vector2(wharf.PosX / 32f, wharf.PosY / 32f);
            
            // Switch Map and Place Player
            if (sandbox.MapManager?.ActiveMapId != wharf.FromMapId)
            {
                sandbox.SwitchMapAndPlacePlayer(wharf.FromMapId, worldPos);
            }
            else
            {
                sandbox.PlayerController?.PlaceAt(worldPos, snapCamera: true);
            }
        }
    }
}
