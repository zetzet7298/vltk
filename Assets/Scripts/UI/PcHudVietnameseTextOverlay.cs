// -----------------------------------------------------------------------------
// VLTK Mobile — Vietnamese HUD text overlay
// Uses IMGUI labels only for localized text because UI Toolkit text is disabled
// without a full runtime text theme in this project. Decorative HUD art remains
// sourced from PC SPR assets.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.Model;

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
            var artRoot = HudArtPathResolver.ResolveArtRoot("UI/HUD/Art");
            if (_skillPanelTexture == null)
                _skillPanelTexture = LoadTexture(HudArtPathResolver.ResolveUserFacingPngPath(artRoot, "技能"));
            if (_skillPanelTargetTexture == null)
                _skillPanelTargetTexture = LoadTexture(HudArtPathResolver.ResolveUserFacingPngPath(artRoot, "技能－战斗分页"));
            if (_addPointTexture == null)
                _addPointTexture = LoadTexture(HudArtPathResolver.ResolvePngPath(artRoot, "状态加点按钮改_01"));
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
                _caiBangIconTextures[skillId] = LoadTexture(HudArtPathResolver.ResolvePngPath(HudArtPathResolver.ResolveGeneratedArtRoot("UI/HUD/Art"), $"cai_bang_skill_{skillId}"));
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
            return GetHudLabelText("LevelText", "1");
        }

        private string GetHpText()
        {
            return GetHudLabelText("HpText", "100/100");
        }

        private string GetMpText()
        {
            return GetHudLabelText("MpText", "50/50");
        }

        private string GetStaminaText()
        {
            return GetHudLabelText("StaminaText", "100/100");
        }

        private string GetExpText()
        {
            return GetHudLabelText("ExpText", "0%");
        }

        private static string GetHudLabelText(string labelName, string fallback)
        {
            var hud = FindAnyObjectByType<GameHudController>();
            var label = hud != null
                ? hud.GetComponent<UIDocument>()?.rootVisualElement?.Q<Label>(labelName)
                : null;
            return label != null ? label.text : fallback;
        }

        private string GetRankText()
        {
            // GAP DATA: PC Player_WorldSort = SỐ hạng thế giới (vd "9"). Mobile data model
            // (PlayerProgression) chưa có field số hạng per-player — chỉ có title name.
            // Theo jx-pc-port-rule không được bịa số → tạm hiện "?" cho đến khi port
            // field worldRank từ PC (backlog #2).
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

            // Top PC bar captions+values — ground-truth = ffb7d31b.ini / 8da7027d.ini (pak_unpacked/1024)
            // PC Main.Left=120 → 1280-space base=150. MOBILE: HUD_LEFT=75 (margin trái giảm 1/2: 150→75).
            // Level=35→44, Stamina=58→72.5, Life=168→210, Mana=278→347.5, Exp=388→485, WorldSort=522→652.5
            const float HUD_LEFT = 38f; // PC origin 150, mobile shift -112 (margin trái: 150→75→38)
            const float BW = 130f; // bar width: PC 104 × 1.25

            // SỐ CẤP điền vào ô dark-green box bake sẵn. PC 194 → mobile 119 (=HUD_LEFT+44).
            var lvlStyle = new GUIStyle(_topValue) { alignment = TextAnchor.MiddleCenter, fontSize = 10,
                fontStyle = FontStyle.Bold, normal = { textColor = new Color(55/255f, 231/255f, 63/255f) } };
            Label(HUD_LEFT + 44f, 4f, 24f, 13f, GetLevelText(), lvlStyle);

            // Captions (row 0) - Bỏ theo PC HUD (PC không vẽ tên bar đè lên)
            // Label(HUD_LEFT + 72.5f, 0f, BW, 14f, "Thể lực",    _topCaption);
            // Label(HUD_LEFT + 210f, 0f, BW, 14f, "Sinh lực",   _topCaption);
            // Label(HUD_LEFT + 347.5f, 0f, BW, 14f, "Nội lực",    _topCaption);
            // Label(HUD_LEFT + 485f, 0f, BW, 14f, "Kinh nghiệm",_topCaption);

            // Values (dưới thanh fill, căn giữa theo bar width) — thứ tự PC: Stamina→Sinh lực→Nội lực→Kinh nghiệm
            Label(HUD_LEFT + 72.5f, 17f, BW, 15f, GetStaminaText(), _topValue);
            Label(HUD_LEFT + 210f, 17f, BW, 15f, GetHpText(),      _topValue);
            Label(HUD_LEFT + 347.5f, 17f, BW, 15f, GetMpText(),      _topValue);
            Label(HUD_LEFT + 485f, 17f, BW, 15f, GetExpText(),     _topValue);

            // GIÁ TRỊ HẠNG điền vào ô dark-green box bake sẵn. PC 802 → mobile 727 (=HUD_LEFT+652.5).
            var rankStyle = new GUIStyle(_topValue) { alignment = TextAnchor.MiddleCenter, fontSize = 10,
                fontStyle = FontStyle.Bold, normal = { textColor = new Color(55/255f, 231/255f, 63/255f) } };
            Label(HUD_LEFT + 652.5f, 4f, 35f, 13f, GetRankText(), rankStyle);

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
            float tabX = 39f;
            Label(tabX, 592f, 50f, 18f, "Tất cả", tabStyleNormal);
            Label(tabX + 60f, 592f, 40f, 18f, "Mật", tabStyleNormal);
            Label(tabX + 105f, 592f, 50f, 18f, "Phòng", tabStyleNormal);
            Label(tabX + 155f, 592f, 60f, 18f, "Bang hội", tabStyleNormal);
            Label(tabX + 220f, 592f, 60f, 18f, "Môn phái", tabStyleNormal);
            Label(tabX + 290f, 592f, 50f, 18f, "Khác", tabStyleYellow);

            // Chat/system hint, like Vietnamese PC client.
            Label(39, 642, 430, 20, "!! Hãy sử dụng hồi phục", _chatWarn);

            DrawMinimapCoordinates();

            // PC parity: bottom-right menu buttons are icon-only (no caption labels).
            // The UIToolkit labels are hidden via USS; do not redraw them here.

            // Bảo Vật button text
            var baovatStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(255/255f, 215/255f, 0f) }
            };
            Label(1200f, 640f, 72f, 72f, "Bảo\nVật", baovatStyle);

            DrawSkillPanelText();

            GUI.depth = oldDepth;
            GUI.matrix = Matrix4x4.identity;
        }

        private void DrawSkillPanelText()
        {
            var hud = FindAnyObjectByType<GameHudController>();
            if (hud == null || !hud.IsSkillPanelVisible)
                return;

            var snap = hud.CurrentSkillSnapshot;
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
                        hud.TryUpgradeSkill(row.skillId);
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
            // Positioned BELOW the minimap frame (top-right panel) so they do not overlap the
            // top status bar's rank text (PC layout keeps map readout under the minimap).
            Label(1138, 136, 132, 13, mapName, new GUIStyle(_minimap) { alignment = TextAnchor.UpperCenter });
            Label(1138, 149, 132, 13, coord + "  Tìm", new GUIStyle(_minimap) { alignment = TextAnchor.UpperCenter });

            // Large preview window coordinate readout, visible while preview is open.
            var hud = FindAnyObjectByType<GameHudController>();
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
