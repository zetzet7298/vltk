// -----------------------------------------------------------------------------
// VLTK Mobile — Vietnamese HUD text overlay
// Uses IMGUI labels only for localized text because UI Toolkit text is disabled
// without a full runtime text theme in this project. Decorative HUD art remains
// sourced from PC SPR assets.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UIElements;

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
        private GUIStyle _skillName;
        private GUIStyle _skillLevel;
        private GUIStyle _skillHint;
        private Texture2D _skillPanelTexture;
        private Texture2D _skillPanelTargetTexture;
        private Texture2D _addPointTexture;
        private readonly System.Collections.Generic.Dictionary<int, Texture2D> _caiBangIconTextures = new();

        private void EnsureStyles()
        {
            if (_topCaption != null && _topValue != null && _chatWarn != null && _menu != null && _minimap != null && _preview != null && _skillName != null && _skillLevel != null && _skillHint != null)
            {
                EnsureSkillTextures();
                return;
            }

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
                fontSize = 11,
                fontStyle = FontStyle.Normal,
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
                normal = { textColor = new Color(1f, 0.96f, 0.30f, 1f) }
            };
            _skillName = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.96f, 0.66f, 1f) }
            };
            _skillLevel = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.82f, 1f, 0.58f, 1f) }
            };
            _skillHint = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = new Color(0.88f, 0.88f, 0.78f, 1f) }
            };

            // Domain reload off can leave nested GUIStyleState references null after script reload.
            _topCaption.normal.textColor = new Color(0.92f, 0.92f, 0.82f, 1f);
            _topValue.normal.textColor = Color.white;
            _topValue.fontStyle = FontStyle.Normal;
            _topValue.fontSize = 11;
            _chatWarn.normal.textColor = new Color(1f, 0.08f, 0.04f, 1f);
            _menu.normal.textColor = new Color(0.88f, 0.92f, 0.82f, 1f);
            _minimap.normal.textColor = new Color(0f, 1f, 0f, 1f);
            _preview.normal.textColor = new Color(1f, 0.96f, 0.30f, 1f);
            _skillName.normal.textColor = new Color(1f, 0.96f, 0.66f, 1f);
            _skillLevel.normal.textColor = new Color(0.82f, 1f, 0.58f, 1f);
            _skillHint.normal.textColor = new Color(0.88f, 0.88f, 0.78f, 1f);
            _skillHint.wordWrap = true;
            EnsureSkillTextures();
        }

        private void EnsureSkillTextures()
        {
            if (_skillPanelTexture == null)
                _skillPanelTexture = LoadTexture(System.IO.Path.Combine(Application.dataPath, "UI/HUD/Art/技能.png"));
            if (_skillPanelTargetTexture == null)
                _skillPanelTargetTexture = LoadTexture(System.IO.Path.Combine(Application.dataPath, "UI/HUD/Art/技能－战斗分页.png"));
            if (_addPointTexture == null)
                _addPointTexture = LoadTexture(System.IO.Path.Combine(Application.dataPath, "UI/HUD/Art/状态加点按钮改_01.png"));
            var caiBangIconIds = new[] { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130,
                                        274, 277, 357, 359, 360, 714, 1073, 1074,
                                        151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166,
                                        3, 4, 6, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21,
                                        43, 45, 47, 48, 50, 51, 54, 55, 57, 58,
                                         77, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93,
                                         23, 24, 26, 29, 30, 31, 32, 33, 34, 35, 36, 37, 40, 41, 42,
                                         60, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76,
                                         95, 97, 99, 100, 101, 102, 103, 105, 108, 109, 111, 113, 114,
                                         131, 132, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150,
                                         167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 181, 182, 183, 184 };
            foreach (var skillId in caiBangIconIds)
            {
                if (_caiBangIconTextures.ContainsKey(skillId) && _caiBangIconTextures[skillId] != null)
                    continue;
                _caiBangIconTextures[skillId] = LoadTexture(System.IO.Path.Combine(Application.dataPath, $"UI/HUD/Art/Generated/cai_bang_skill_{skillId}.png"));
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path))
                return null;
            var bytes = System.IO.File.ReadAllBytes(path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            return tex.LoadImage(bytes) ? tex : null;
        }

        // ── Live-data helpers (lấy từ HudDataBridge / SandboxManager) ──────────

private string GetLevelText()
        {
            var hud = FindObjectOfType<GameHudController>();
            if (hud == null) return "1";
            var label = hud.GetComponent<UIDocument>()
                ?.rootVisualElement?.Q<Label>("LevelText");
            return label?.text ?? "1";
        }

        private string GetHpText()
        {
            var hud = FindObjectOfType<GameHudController>();
            var label = hud?.GetComponent<UnityEngine.UIElements.UIDocument>()
                ?.rootVisualElement?.Q<UnityEngine.UIElements.Label>("HpText");
            return label?.text ?? "100/100";
        }

        private string GetMpText()
        {
            var hud = FindObjectOfType<GameHudController>();
            var label = hud?.GetComponent<UnityEngine.UIElements.UIDocument>()
                ?.rootVisualElement?.Q<UnityEngine.UIElements.Label>("MpText");
            return label?.text ?? "50/50";
        }

        private string GetStaminaText()
        {
            var hud = FindObjectOfType<GameHudController>();
            var label = hud?.GetComponent<UnityEngine.UIElements.UIDocument>()
                ?.rootVisualElement?.Q<UnityEngine.UIElements.Label>("StaminaText");
            return label?.text ?? "100/100";
        }

        private string GetExpText()
        {
            var hud = FindObjectOfType<GameHudController>();
            var label = hud?.GetComponent<UnityEngine.UIElements.UIDocument>()
                ?.rootVisualElement?.Q<UnityEngine.UIElements.Label>("ExpText");
            return label?.text ?? "0%";
        }

        private string GetRankText()
        {
            // PC WorldSort: thứ hạng giang hồ — hiển thị '?' khớp với PC screenshot
            return "?";
        }

        private void OnGUI()
        {
            EnsureStyles();
            float sx = Screen.width / 1280f;
            float sy = Screen.height / 720f;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(sx, sy, 1f));
            int oldDepth = GUI.depth;
            GUI.depth = -10000;

            // Top PC bar captions+values — theo 顶部控制条.ini (Ui800)
            // PC Main.Left=218 → 1280-space: 218×1.6=348. Bar offsets scaled ×1.6.
            // Stamina=87→139, Life=182→291, Mana=277→443, Exp=372→595, Level=53→85, WorldSort=499→798
            const float C = 0f;   // container offset is 0 because bars use absolute 1280-space coords
            const float BW = 166f; // bar width: PC 104 × 1.6

            // "+ Cấp X" — PC format, màu xanh lá
            var lvlStyle = new GUIStyle(_topValue) { alignment = TextAnchor.MiddleLeft, fontSize = 11,
                fontStyle = FontStyle.Bold, normal = { textColor = new Color(55/255f, 231/255f, 63/255f) } };
            Label(348f + 85f, 0f, 80f, 20f, "+ Cấp " + GetLevelText(), lvlStyle);

            // Captions (row 0) - Bỏ theo PC HUD (PC không vẽ tên bar đè lên)
            // Label(348f + 139f, 0f, BW, 14f, "Thể lực",    _topCaption);
            // Label(348f + 291f, 0f, BW, 14f, "Sinh lực",   _topCaption);
            // Label(348f + 443f, 0f, BW, 14f, "Nội lực",    _topCaption);
            // Label(348f + 595f, 0f, BW, 14f, "Kinh nghiệm",_topCaption);

            // Values (below bar tracks, top=19, Left shifted by 8px according to PC INI alignment)
            Label(348f + 131f, 19f, BW, 15f, GetStaminaText(), _topValue);
            Label(348f + 283f, 19f, BW, 15f, GetHpText(),      _topValue);
            Label(348f + 435f, 19f, BW, 15f, GetMpText(),      _topValue);
            Label(348f + 587f, 19f, BW, 15f, GetExpText(),     _topValue);

            // "Hạng N" — WorldSort scaled
            var rankStyle = new GUIStyle(_topValue) { alignment = TextAnchor.MiddleLeft, fontSize = 10,
                fontStyle = FontStyle.Bold, normal = { textColor = new Color(55/255f, 231/255f, 63/255f) } };
            Label(348f + 798f, 0f, 80f, 20f, "Hạng " + GetRankText(), rankStyle);

            // Chat tabs (Tất cả, Mật, Phòng, Bang hội, Môn phái, Khác) giống PC
            var tabStyleNormal = new GUIStyle(_menu) {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                normal = { textColor = new Color(0f, 210/255f, 255/255f) }
            };
            var tabStyleYellow = new GUIStyle(_menu) {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 11,
                normal = { textColor = new Color(255/255f, 220/255f, 0f) }
            };
            float tabX = 155f;
            Label(tabX, 592f, 50f, 18f, "Tất cả", tabStyleNormal);
            Label(tabX + 60f, 592f, 40f, 18f, "Mật", tabStyleNormal);
            Label(tabX + 105f, 592f, 50f, 18f, "Phòng", tabStyleNormal);
            Label(tabX + 155f, 592f, 60f, 18f, "Bang hội", tabStyleNormal);
            Label(tabX + 220f, 592f, 60f, 18f, "Môn phái", tabStyleNormal);
            Label(tabX + 290f, 592f, 50f, 18f, "Khác", tabStyleYellow);

            // Chat/system hint, like Vietnamese PC client.
            Label(155, 642, 430, 20, "!! Hãy sử dụng hồi phục", _chatWarn);

            DrawMinimapCoordinates();

            // Bottom menu labels - shifted left by 76px because right-menu was shifted to accommodate Treasure button.
            string[] labels = { "Nhân", "Túi", "Võ", "Đội", "Bang", "PK" };
            float startX = 899f;
            for (int i = 0; i < labels.Length; i++)
                Label(startX + i * 50f, 706, 46, 12, labels[i], _menu);

            // Bảo Vật button text
            var baovatStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(255/255f, 215/255f, 0f) }
            };
            Label(1200f, 640f, 72f, 72f, "Bảo\nVật", baovatStyle);

            DrawCaiBangSkillPanelText();

            GUI.depth = oldDepth;
            GUI.matrix = Matrix4x4.identity;
        }

        private void DrawCaiBangSkillPanelText()
        {
            var hud = FindObjectOfType<GameHudController>();
            if (hud == null || !hud.IsCaiBangSkillPanelVisible)
                return;

            var snap = hud.CurrentCaiBangSkillSnapshot;
            int points = snap != null ? snap.skillPoints : 200;
            Rect panel = new Rect(338, 110, 205, 376);
            if (_skillPanelTexture != null)
                GUI.DrawTexture(panel, _skillPanelTexture, ScaleMode.StretchToFill, true);
            if (_skillPanelTargetTexture != null)
                GUI.DrawTexture(new Rect(345, 167, 191, 278), _skillPanelTargetTexture, ScaleMode.StretchToFill, true);
            var titleStyle = new GUIStyle(_skillLevel) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
            Label(376, 116, 110, 18, "Kỹ năng võ công", titleStyle);
            var pageOneRect = new Rect(345, 422, 67, 22);
            var pageStyle = new GUIStyle(_skillLevel) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
            Label(345, 422, 67, 22, $"Tất cả ({snap?.rows?.Count ?? 0} skill)", pageStyle);
            if (snap?.rows == null)
                return;

            const float startX = 345f;
            const float startY = 171f;
            const float cellW = 39f;
            const float cellH = 51f;
            const int columns = 5;
            for (int i = 0; i < snap.rows.Count; i++)
            {
                var row = snap.rows[i];
                int col = i % columns;
                int line = i / columns;
                float x = startX + col * cellW;
                float y = startY + line * cellH;
                var oldGuiColor = GUI.color;
                GUI.color = new Color(0.02f, 0.035f, 0.03f, 1f);
                GUI.DrawTexture(new Rect(x, y, 36, 50), Texture2D.whiteTexture);
                GUI.color = oldGuiColor;
                _caiBangIconTextures.TryGetValue(row.skillId, out var icon);
                if (icon != null)
                    GUI.DrawTexture(new Rect(x, y, 36, 36), icon, ScaleMode.StretchToFill, true);
                if (snap.selectedSkillId == row.skillId)
                    GUI.Box(new Rect(x - 1, y - 1, 38, 38), GUIContent.none);
                if (_addPointTexture != null && row.canUpgrade)
                {
                    var addRect = new Rect(x + 22, y + 35, 14, 14);
                    GUI.DrawTexture(addRect, _addPointTexture, ScaleMode.StretchToFill, true);
                    if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && addRect.Contains(Event.current.mousePosition))
                    {
                        hud.TryUpgradeCaiBangSkill(row.skillId);
                        Event.current.Use();
                    }
                }
                if (row.learnedLevel > 0)
                    Label(x, y + 36f, 22, 14, row.learnedLevel.ToString(), _skillLevel);
            }

            if (snap.selectedRow.HasValue)
            {
                var row = snap.selectedRow.Value;
                var detail = $"{row.displayName}\nCấp {row.learnedLevel}/{row.maxLevel} · cần cấp {row.requiredLevel}\n{row.summary}";
                if (!string.IsNullOrEmpty(row.nextLevelSummary))
                    detail += $"\n\nCấp sau\n{row.nextLevelSummary}";
                detail += $"\n\n{row.upgradeStatus}";
                var detailRect = new Rect(panel.xMax + 6, panel.y + 58, 220, 220);
                DrawPcTooltip(detailRect, detail);
            }
        }


        private static void DrawPcTooltip(Rect rect, string text)
        {
            var oldColor = GUI.color;
            GUI.color = new Color(0.02f, 0.02f, 0.02f, 0.92f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = new Color(0.75f, 0.60f, 0.32f, 1f);
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1, rect.width, 1), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 1, rect.y, 1, rect.height), Texture2D.whiteTexture);
            GUI.color = oldColor;
            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.UpperLeft,
                fontSize = 10,
                wordWrap = true,
                normal = { textColor = new Color(0.92f, 0.88f, 0.70f, 1f) }
            };
            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, rect.height - 16), text ?? string.Empty, style);
        }

        private void DrawMinimapCoordinates()
        {
            var player = VLTK.Sandbox.SandboxManager.Instance != null
                ? VLTK.Sandbox.SandboxManager.Instance.PlayerController
                : FindObjectOfType<VLTK.Sandbox.SandboxPlayerController>();
            if (player == null) return;

            var pos = (Vector2)player.transform.position;
            // Display PC MPS coordinates (same format as PC client minimap)
            VLTK.Sandbox.BaLangEnemyDatabase.WorldToMps(pos.x, pos.y, out int mpsX, out int mpsY);
            var coord = $"{mpsX}/{mpsY}";
            var rawMapName = VLTK.Sandbox.SandboxManager.Instance?.MapManager?.ActiveMap?.catalogEntry?.displayNameRaw
                ?? VLTK.Sandbox.SandboxManager.Instance?.MapManager?.ActiveMap?.catalogEntry?.displayNameNormalized
                ?? "Bản đồ";
            var mapName = ToVietnameseMapName(rawMapName);

            // PC small minimap shows scene name + coord on minimap frame matching PC layout.
            Label(1140, 6, 120, 14, mapName, new GUIStyle(_minimap) { alignment = TextAnchor.UpperRight });
            Label(1140, 138, 80, 14, coord + "  Tìm", new GUIStyle(_minimap) { alignment = TextAnchor.MiddleLeft });

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

        private static void LabelShadow(float x, float y, float w, float h, string text, GUIStyle style)
        {
            if (style == null)
                style = GUI.skin.label;
            var old = style.normal.textColor;
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(x + 1, y + 1, w, h), text ?? string.Empty, style);
            style.normal.textColor = old;
            GUI.Label(new Rect(x, y, w, h), text ?? string.Empty, style);
        }

        private static void Label(float x, float y, float w, float h, string text, GUIStyle style)
        {
            if (style == null)
                style = GUI.skin.label;
            GUI.Label(new Rect(x, y, w, h), text ?? string.Empty, style);
        }
    }
}
