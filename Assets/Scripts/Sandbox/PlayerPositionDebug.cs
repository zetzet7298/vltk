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
    /// Disabled for release builds. Re-enable by changing to #if true.
    /// </summary>
    public class PlayerPositionDebug : MonoBehaviour
    {
#if false
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
            int mpsX = Mathf.RoundToInt(worldPos.x);
            int bestMpsY = 0;
            float bestError = float.MaxValue;
            for (int testRegionRow = 0; testRegionRow < 200; testRegionRow++)
            {
                int testMpsY = Mathf.RoundToInt(-worldPos.y + testRegionRow * 512);
                int checkRegionRow = testMpsY / 1024;
                if (checkRegionRow == testRegionRow)
                {
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
#endif

        [ContextMenu("Log Current Position")]
        public void LogCurrentPosition()
        {
            Vector2 worldPos = transform.position;
            int mpsX = Mathf.RoundToInt(worldPos.x);
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
