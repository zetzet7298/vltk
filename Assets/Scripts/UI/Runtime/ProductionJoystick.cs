using System;
using UnityEngine;

namespace VLTK.Production.UI.Runtime
{
    [DisallowMultipleComponent]
    public sealed class ProductionJoystick : MonoBehaviour
    {
        [Range(0f, 1f)] public float deadZone = ProductionJoystickInput.DefaultDeadZone;
        public int quantizationSteps = ProductionJoystickInput.DefaultQuantizationSteps;

        public JoystickIntent LastIntent { get; private set; }
        public event Action<JoystickIntent> MoveSubmitted;

        public void SubmitRaw(Vector2 raw)
        {
            LastIntent = ProductionJoystickInput.Quantize(raw, deadZone, quantizationSteps);
            MoveSubmitted?.Invoke(LastIntent);
        }
    }
}
