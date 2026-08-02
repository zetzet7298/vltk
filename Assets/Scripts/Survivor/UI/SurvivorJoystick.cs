using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Touch (left half) + keyboard joystick. Self-contained, no inspector deps.
    /// activeInputHandler = Both, so UnityEngine.Input works.
    /// </summary>
    public sealed class SurvivorJoystick
    {
        public Vector2 Move;

        private int _fingerId = -1;
        private Vector2 _origin;
        private const float Radius = 140f;

        public void Update()
        {
            Vector2 key = KeyDir();

            Vector2 touchVec = Vector2.zero;
            bool touch = false;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var tc = Input.GetTouch(i);
                if (_fingerId == -1 && tc.phase == TouchPhase.Began && tc.position.x < Screen.width * 0.5f)
                {
                    _fingerId = tc.fingerId;
                    _origin = tc.position;
                }
                if (tc.fingerId == _fingerId)
                {
                    if (tc.phase == TouchPhase.Ended || tc.phase == TouchPhase.Canceled) _fingerId = -1;
                    else { touchVec = ((Vector2)tc.position - _origin) / Radius; touch = true; }
                }
            }

            Move = touch ? Vector2.ClampMagnitude(touchVec, 1f) : key;
            if (Move.sqrMagnitude > 1f) Move.Normalize();
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
