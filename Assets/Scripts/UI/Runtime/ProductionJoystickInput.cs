using UnityEngine;

namespace VLTK.Production.UI.Runtime
{
    public readonly struct JoystickIntent
    {
        public readonly bool active;
        public readonly Vector2 move;
        public readonly int quantizedX;
        public readonly int quantizedY;

        public JoystickIntent(bool active, Vector2 move, int quantizedX, int quantizedY)
        {
            this.active = active;
            this.move = move;
            this.quantizedX = quantizedX;
            this.quantizedY = quantizedY;
        }
    }

    public static class ProductionJoystickInput
    {
        public const float DefaultDeadZone = 0.15f;
        public const int DefaultQuantizationSteps = 1000;

        public static JoystickIntent Quantize(Vector2 raw, float deadZone = DefaultDeadZone, int steps = DefaultQuantizationSteps)
        {
            deadZone = Mathf.Clamp01(deadZone);
            steps = Mathf.Max(1, steps);

            float magnitude = raw.magnitude;
            if (magnitude <= deadZone)
                return new JoystickIntent(false, Vector2.zero, 0, 0);

            Vector2 clamped = magnitude > 1f ? raw / magnitude : raw;
            float scaledMagnitude = (clamped.magnitude - deadZone) / (1f - deadZone);
            Vector2 scaled = clamped.normalized * Mathf.Clamp01(scaledMagnitude);
            int qx = Mathf.RoundToInt(scaled.x * steps);
            int qy = Mathf.RoundToInt(scaled.y * steps);
            Vector2 quantized = new Vector2(qx / (float)steps, qy / (float)steps);
            return new JoystickIntent(true, Vector2.ClampMagnitude(quantized, 1f), qx, qy);
        }
    }
}
