using System;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Keyboard joystick (WASD/arrows) + touch-override seam cho uGUI joystick UI.
    /// Self-contained, no inspector deps. activeInputHandler = Both, nên
    /// UnityEngine.Input hoạt động.
    /// Touch-override: SurvivorJoystickUi.Build wire 2 delegate này vào
    /// MobileJoystick (Sandbox) — joystick đang giữ → move từ MobileJoystick
    /// (deadzone + smoothing đã xử lý); release → về keyboard (KeyDir).
    /// </summary>
    public sealed class SurvivorJoystick
    {
        /// <summary>Move output mỗi frame (touch override hoặc keyboard).</summary>
        public Vector2 Move;

        /// <summary>uGUI joystick đang được giữ? null → keyboard-only (EditMode/test).</summary>
        public Func<bool> TouchOverrideActive;

        /// <summary>Move vector từ uGUI joystick (đã qua deadzone, ≤1).</summary>
        public Func<Vector2> TouchOverrideMove;

        public void Update()
        {
            if (TouchOverrideActive != null && TouchOverrideActive())
            {
                var m = TouchOverrideMove != null ? TouchOverrideMove() : Vector2.zero;
                Move = m.sqrMagnitude > 1f ? m.normalized : m;
                return;
            }
            Move = KeyDir();
        }

        private static Vector2 KeyDir()
        {
            float x = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0);
            float y = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                    - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0);
            return new Vector2(x, y);
        }
    }
}
