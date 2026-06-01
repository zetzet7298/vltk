// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Screen-space enemy labels matching the PC readable overhead presentation:
    /// elemental Vietnamese name, current/max HP text, then HP bar.
    /// Kept separate from world sprites so dense map art cannot hide text.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BaLangEnemyNameplateOverlay : MonoBehaviour
    {
        public bool visible = true;
        public float maxDrawDistance = 900f;
        public Vector2 screenOffset = new Vector2(0f, -2f);

        private GUIStyle _nameStyle;
        private GUIStyle _hpStyle;
        private Texture2D _barBack;
        private Texture2D _barFill;
        private Texture2D _labelBack;

        private void OnGUI()
        {
            if (!visible) return;
            EnsureStyles();
            var cam = Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
            if (cam == null) return;

            var enemies = FindObjectsOfType<BaLangEnemyAi>();
            foreach (var enemy in enemies)
                DrawEnemy(cam, enemy);
        }

        private void DrawEnemy(Camera cam, BaLangEnemyAi enemy)
        {
            if (enemy == null) return;
            var plate = enemy.GetComponentInChildren<EnemyHealthBar>();
            if (plate == null) return;

            var anchor = enemy.GetComponent<EnemyNameplateAnchor>();
            var worldAnchor = anchor != null ? anchor.ScreenAnchorWorldPosition : enemy.transform.position + new Vector3(0f, 88f, 0f);
            var sp = cam.WorldToScreenPoint(worldAnchor);
            if (sp.z <= 0f) return;
            if (Vector2.Distance(cam.transform.position, enemy.transform.position) > maxDrawDistance) return;

            float x = sp.x + screenOffset.x;
            float y = Screen.height - sp.y + screenOffset.y;
            if (x < -120f || x > Screen.width + 120f || y < -80f || y > Screen.height + 80f) return;

            string name = PcStyleName(plate.DisplayName);
            string hp = $"{plate.CurrentLife}/{plate.MaxLife}";
            float ratio = plate.MaxLife > 0 ? Mathf.Clamp01((float)plate.CurrentLife / plate.MaxLife) : 0f;

            // PC style: compact white outlined name above enemy, thin green life bar directly below.
            DrawOutlinedLabel(new Rect(x - 52f, y - 31f, 104f, 17f), name, _nameStyle, Color.white);
            GUI.DrawTexture(new Rect(x - 31f, y - 15f, 62f, 5f), _barBack);
            GUI.DrawTexture(new Rect(x - 30f, y - 14f, 60f * ratio, 3f), _barFill);
        }

        private static string PcStyleName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "Kẻ địch";
            string[] prefixes = { "Kim hệ ", "Mộc hệ ", "Thủy hệ ", "Hỏa hệ ", "Thổ hệ ", "Vô hệ " };
            foreach (var prefix in prefixes)
                if (displayName.StartsWith(prefix, System.StringComparison.Ordinal))
                    return displayName.Substring(prefix.Length);
            return displayName;
        }

        private void EnsureStyles()
        {
            if (_nameStyle == null)
            {
                _nameStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height / 54f)),
                    fontStyle = FontStyle.Bold,
                };
            }
            if (_hpStyle == null)
            {
                _hpStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = Mathf.Max(12, Mathf.RoundToInt(Screen.height / 48f)),
                    fontStyle = FontStyle.Bold,
                };
            }
            _nameStyle.normal.textColor = Color.white;
            _hpStyle.normal.textColor = Color.white;
            if (_barBack == null) _barBack = MakeTex(new Color(0.02f, 0.16f, 0.03f, 0.95f));
            if (_barFill == null) _barFill = MakeTex(new Color(0.08f, 0.92f, 0.12f, 1f));
            if (_labelBack == null) _labelBack = MakeTex(new Color(0f, 0f, 0f, 0f));
        }

        private static void DrawOutlinedLabel(Rect rect, string text, GUIStyle style, Color color)
        {
            var old = style.normal.textColor;
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x - 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x + 1f, rect.y, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y - 1f, rect.width, rect.height), text, style);
            GUI.Label(new Rect(rect.x, rect.y + 1f, rect.width, rect.height), text, style);
            style.normal.textColor = color;
            GUI.Label(rect, text, style);
            style.normal.textColor = old;
        }

        private static Texture2D MakeTex(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, color);
            tex.Apply();
            return tex;
        }
    }
}
