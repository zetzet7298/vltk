// -----------------------------------------------------------------------------
// VLTK Mobile — Vietnamese HUD text overlay
// Uses IMGUI labels only for localized text because UI Toolkit text is disabled
// without a full runtime text theme in this project. Decorative HUD art remains
// sourced from PC SPR assets.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.UI
{
    [DisallowMultipleComponent]
    public sealed class PcHudVietnameseTextOverlay : MonoBehaviour
    {
        private GUIStyle _topCaption;
        private GUIStyle _topValue;
        private GUIStyle _chatWarn;
        private GUIStyle _menu;

        private void EnsureStyles()
        {
            if (_topCaption != null) return;

            _topCaption = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.92f, 0.92f, 0.82f, 1f) }
            };
            _topValue = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white }
            };
            _chatWarn = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.08f, 0.04f, 1f) }
            };
            _menu = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.88f, 0.92f, 0.82f, 1f) }
            };
        }

        private void OnGUI()
        {
            EnsureStyles();
            float sx = Screen.width / 1280f;
            float sy = Screen.height / 720f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));

            // Top PC bar Vietnamese captions/values. Positions mirror GameHud.uss.
            Label(150, 2, 34, 10, "Cấp", _topCaption);
            Label(179, 2, 18, 14, "1", _topValue);
            Label(203, 2, 104, 10, "Kinh nghiệm", _topCaption);
            Label(315, 2, 104, 10, "Sinh lực", _topCaption);
            Label(427, 2, 104, 10, "Nội lực", _topCaption);
            Label(539, 2, 104, 10, "Thể lực", _topCaption);

            Label(203, 19, 104, 12, "0%", _topValue);
            Label(315, 19, 104, 12, "100/100", _topValue);
            Label(427, 19, 104, 12, "50/50", _topValue);
            Label(539, 19, 104, 12, "100/100", _topValue);

            // Chat/system hint, like Vietnamese PC client.
            Label(155, 642, 430, 20, "!! Hãy sử dụng hồi phục", _chatWarn);

            // Bottom menu labels.
            string[] labels = { "Nhân", "Túi", "Võ", "Đội", "Bang", "PK" };
            float startX = 975f;
            for (int i = 0; i < labels.Length; i++)
                Label(startX + i * 50f, 706, 46, 12, labels[i], _menu);

            GUI.matrix = Matrix4x4.identity;
        }

        private static void Label(float x, float y, float w, float h, string text, GUIStyle style)
        {
            GUI.Label(new Rect(x, y, w, h), text, style);
        }
    }
}
