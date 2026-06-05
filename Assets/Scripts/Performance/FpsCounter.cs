// -----------------------------------------------------------------------------
// VLTK Mobile — Development FPS Counter
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>Overlay FPS đơn giản cho QA mobile/debug build.</summary>
    public sealed class FpsCounter : MonoBehaviour
    {
        public bool showOnlyInDebugBuild = true;
        public float refreshInterval = 0.5f;

        private float _accumulatedTime;
        private int _frames;
        private float _fps;
        private readonly GUIStyle _style = new GUIStyle();

        private void Awake()
        {
            _style.fontSize = 24;
            _style.normal.textColor = Color.yellow;
            _style.alignment = TextAnchor.UpperRight;
        }

        private void Update()
        {
            _accumulatedTime += Time.unscaledDeltaTime;
            _frames++;

            if (_accumulatedTime >= refreshInterval)
            {
                _fps = _frames / _accumulatedTime;
                _frames = 0;
                _accumulatedTime = 0f;
            }
        }

        private void OnGUI()
        {
            if (showOnlyInDebugBuild && !Debug.isDebugBuild) return;
            GUI.Label(new Rect(Screen.width - 180, 8, 170, 32), $"FPS: {_fps:0}", _style);
        }
    }
}
