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
        private GUIStyle _minimap;
        private GUIStyle _preview;

        private void EnsureStyles()
        {
            if (_topCaption != null && _topValue != null && _chatWarn != null && _menu != null && _minimap != null && _preview != null) return;

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
            _minimap = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0f, 1f, 0f, 1f) }
            };
            _preview = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.96f, 0.95f, 0.78f, 1f) }
            };

            // Domain reload off can leave nested GUIStyleState references null after script reload.
            _topCaption.normal.textColor = new Color(0.92f, 0.92f, 0.82f, 1f);
            _topValue.normal.textColor = Color.white;
            _chatWarn.normal.textColor = new Color(1f, 0.08f, 0.04f, 1f);
            _menu.normal.textColor = new Color(0.88f, 0.92f, 0.82f, 1f);
            _minimap.normal.textColor = new Color(0f, 1f, 0f, 1f);
            _preview.normal.textColor = new Color(0.96f, 0.95f, 0.78f, 1f);
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

            DrawMinimapCoordinates();

            // Bottom menu labels.
            string[] labels = { "Nhân", "Túi", "Võ", "Đội", "Bang", "PK" };
            float startX = 975f;
            for (int i = 0; i < labels.Length; i++)
                Label(startX + i * 50f, 706, 46, 12, labels[i], _menu);

            GUI.matrix = Matrix4x4.identity;
        }

        private void DrawMinimapCoordinates()
        {
            var player = VLTK.Sandbox.SandboxManager.Instance != null
                ? VLTK.Sandbox.SandboxManager.Instance.PlayerController
                : FindObjectOfType<VLTK.Sandbox.SandboxPlayerController>();
            if (player == null) return;

            var pos = (Vector2)player.transform.position;
            var coord = $"{Mathf.FloorToInt(pos.x / 8f)}/{Mathf.FloorToInt(-pos.y / 8f)}";
            var rawMapName = VLTK.Sandbox.SandboxManager.Instance?.MapManager?.ActiveMap?.catalogEntry?.displayNameRaw
                ?? VLTK.Sandbox.SandboxManager.Instance?.MapManager?.ActiveMap?.catalogEntry?.displayNameNormalized
                ?? "Bản đồ";
            var mapName = ToVietnameseMapName(rawMapName);

            // PC small minimap shows scene name + coord above/on minimap. UI Toolkit text can be unreliable,
            // so draw IMGUI on top at exact HUD coords.
            Label(1144, 4, 112, 14, mapName, _minimap);
            Label(1146, 18, 112, 14, coord, _minimap);

            // Large preview window coordinate readout, visible while preview is open.
            var hud = FindObjectOfType<GameHudController>();
            var doc = hud != null ? hud.GetComponent<UnityEngine.UIElements.UIDocument>() : null;
            var root = doc != null ? FindElement(doc.rootVisualElement, "GameHud") : null;
            var overlay = root != null ? FindElement(root, "MapPreviewOverlay") : null;
            if (overlay != null && !overlay.ClassListContains("hidden"))
            {
                Label(394, 82, 492, 20, $"{mapName}  {coord}", _preview);
                Label(394, 596, 492, 20, $"Vị trí: {coord}", _preview);
            }
        }

        private static string ToVietnameseMapName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "Bản đồ";
            return raw switch
            {
                "巴陵县" => "Ba Lăng huyện",
                "Map_79" => "Ba Lăng huyện",
                _ => raw,
            };
        }

        private static UnityEngine.UIElements.VisualElement FindElement(UnityEngine.UIElements.VisualElement root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (var child in root.Children())
            {
                var hit = FindElement(child, name);
                if (hit != null) return hit;
            }
            return null;
        }

        private static void Label(float x, float y, float w, float h, string text, GUIStyle style)
        {
            if (style == null)
                style = GUI.skin.label;
            GUI.Label(new Rect(x, y, w, h), text ?? string.Empty, style);
        }
    }
}
