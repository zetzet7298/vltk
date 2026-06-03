// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Debug utility to display player world coordinates and MPS coordinates on screen.
    /// Attach to player GameObject to see real-time position.
    /// </summary>
    public class PlayerPositionDebug : MonoBehaviour
    {
        private GUIStyle _style;

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label);
                _style.fontSize = 20;
                _style.normal.textColor = Color.yellow;
                _style.alignment = TextAnchor.UpperLeft;
            }

            Vector2 worldPos = transform.position;
            
            // Convert World to MPS
            // Reverse of: int regionRow = mpsY / 1024; worldX = mpsX; worldY = -(mpsY - regionRow * 512);
            // So: mpsX = worldX; 
            // worldY = -(mpsY - regionRow * 512) => mpsY - regionRow * 512 = -worldY
            // We need to find regionRow and mpsY
            // Let's approximate: regionRow ≈ round(abs(worldY) / 512)
            
            int mpsX = Mathf.RoundToInt(worldPos.x);
            
            // Try different regionRow values to find the correct mpsY
            int bestMpsY = 0;
            float bestError = float.MaxValue;
            for (int testRegionRow = 0; testRegionRow < 200; testRegionRow++)
            {
                // worldY = -(mpsY - regionRow * 512)
                // -worldY = mpsY - regionRow * 512
                // mpsY = -worldY + regionRow * 512
                int testMpsY = Mathf.RoundToInt(-worldPos.y + testRegionRow * 512);
                
                // Check if this mpsY gives us the correct regionRow
                int checkRegionRow = testMpsY / 1024;
                if (checkRegionRow == testRegionRow)
                {
                    // Verify conversion back
                    float checkWorldY = -(testMpsY - testRegionRow * 512);
                    float error = Mathf.Abs(checkWorldY - worldPos.y);
                    if (error < bestError)
                    {
                        bestError = error;
                        bestMpsY = testMpsY;
                    }
                }
            }

            string text = $"Player Position:\n" +
                         $"World: ({worldPos.x:F2}, {worldPos.y:F2})\n" +
                         $"MPS: ({mpsX}, {bestMpsY})\n" +
                         $"Target: World (53246, -52041)";

            GUI.Label(new Rect(10, 80, 500, 200), text, _style);
        }

        [ContextMenu("Log Current Position")]
        public void LogCurrentPosition()
        {
            Vector2 worldPos = transform.position;
            int mpsX = Mathf.RoundToInt(worldPos.x);
            
            // Same conversion as OnGUI
            int bestMpsY = 0;
            for (int testRegionRow = 0; testRegionRow < 200; testRegionRow++)
            {
                int testMpsY = Mathf.RoundToInt(-worldPos.y + testRegionRow * 512);
                int checkRegionRow = testMpsY / 1024;
                if (checkRegionRow == testRegionRow)
                {
                    float checkWorldY = -(testMpsY - testRegionRow * 512);
                    if (Mathf.Abs(checkWorldY - worldPos.y) < 0.5f)
                    {
                        bestMpsY = testMpsY;
                        break;
                    }
                }
            }

            SubsystemLog.Info("PlayerPositionDebug", 
                $"World: ({worldPos.x:F2}, {worldPos.y:F2}) | MPS: ({mpsX}, {bestMpsY})");
        }
    }
}
