// -----------------------------------------------------------------------------
// VLTK Mobile — Vietnamese HUD text overlay
// Uses IMGUI labels only for localized text because UI Toolkit text is disabled
// without a full runtime text theme in this project. Decorative HUD art remains
// sourced from PC SPR assets.
// Layout coordinates from PC source: 顶部控制条.ini, 玩家信息主界面.ini, 工具控制条.ini
// PC coordinates (800×600) are scaled ×1.6 for 1280×720 reference space.
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
        private GUIStyle _topValue;
        private GUIStyle _chatWarn;
        private GUIStyle _menu;
        private GUIStyle _minimap;
        private GUIStyle _preview;
        private GUIStyle _skillName;
        private GUIStyle _skillLevel;
        private GUIStyle _skillHint;
        private GUIStyle _chatTab;
        private GUIStyle _chatTabActive;
        private GUIStyle _chatTabAll;
        private GUIStyle _chatTabAllActive;
        private GUIStyle _baovatStyle;
        private GUIStyle _levelStyle;
        private GUIStyle _rankStyle;
        private Texture2D _skillPanelTexture;
        private Texture2D _skillPanelTargetTexture;
        private Texture2D _addPointTexture;
        private readonly System.Collections.Generic.Dictionary<int, Texture2D> _caiBangIconTextures = new();

        private void EnsureStyles()
        {
            if (_topValue != null) { EnsureSkillTextures(); return; }

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
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.08f, 0.04f, 1f) }
            };
            _menu = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 8,
                fontStyle = FontStyle.Normal,
                normal = { textColor = new Color(0.88f, 0.92f, 0.82f, 1f) }
            };
            _minimap = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 9,
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
            _chatTab = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(0f, 210/255f, 255/255f) }
            };
            _chatTabActive = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0f, 210/255f, 255/255f) }
            };
            _chatTabAll = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                normal = { textColor = new Color(255/255f, 230/255f, 174/255f) }
            };
            _chatTabAllActive = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(255/255f, 230/255f, 174/255f) }
            };
            _baovatStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(255/255f, 215/255f, 0f) }
            };
            _levelStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(55/255f, 231/255f, 63/255f) }
            };
            _rankStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(55/255f, 231/255f, 63/255f) }
            };
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

        // ── Live-data helpers ──────────────────────────────────────────

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
            var manager = SandboxManager.Instance;
            var progression = manager != null ? manager.PlayerProgression : null;
            if (progression != null)
            {
                int lvl = progression.level;
                if (lvl >= 200)
                {
                    var r = HudDataService.Instance.GetRankingTitle(10287);
                    if (r != null) return r.name;
                }
                else if (lvl >= 100)
                {
                    int titleId = 10287;
                    switch (progression.faction)
                    {
                        case CombatFaction.Shaolin: titleId = 10277; break;
                        case CombatFaction.TianWang: titleId = 10278; break;
                        case CombatFaction.TangMen: titleId = 10279; break;
                        case CombatFaction.WuDu: titleId = 10280; break;
                        case CombatFaction.EMei: titleId = 10281; break;
                        case CombatFaction.CuiYan: titleId = 10282; break;
                        case CombatFaction.CaiBang: titleId = 10283; break;
                        case CombatFaction.TianRen: titleId = 10284; break;
                        case CombatFaction.WuDang: titleId = 10285; break;
                        case CombatFaction.KunLun: titleId = 10286; break;
                    }
                    var r = HudDataService.Instance.GetRankingTitle(titleId);
                    if (r != null) return r.name;
                }
            }
            return "?";
        }

        private void OnGUI()
        {
            EnsureStyles();
            // Match HudPanelSettings responsive Shrink mode: one uniform scale,
            // with any extra width/height used as centered safe-area padding.
            const float referenceWidth = 1280f;
            const float referenceHeight = 720f;
            float scale = Mathf.Min(Screen.width / referenceWidth, Screen.height / referenceHeight);
            float offsetX = (Screen.width - referenceWidth * scale) * 0.5f;
            float offsetY = (Screen.height - referenceHeight * scale) * 0.5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f), Quaternion.identity, new Vector3(scale, scale, 1f));
            int oldDepth = GUI.depth;
            GUI.depth = -10000;

            // ═══ TOP BAR TEXT — PC 800 顶部控制条.ini (uid 8da7027d) ═══
            // Main.Left=120; Level=35; Stamina=58; Life=168; Mana=278; Exp=388; WorldSort=522.
            const float BW = 104f;

            // Connection status far-left — PC: green text "Hoạt động tốt NN"
            Label(14f, 4f, 100f, 14f, "Hoạt động tốt 97", _levelStyle);

            // Number only: "+ Cấp" is baked into top_status_strip.png.
            Label(155f, 2f, 20f, 12f, GetLevelText(), _levelStyle);

            // Bar values below tracks — PC: Top=12 from bar parent, Text.Left=-5, W=104, H=12
            // Bar parent Top=2 → Text absolute top = 2+12 = 14 → scaled = ~17
            Label(178f, 19f, BW, 12f, GetStaminaText(), _topValue);
            Label(288f, 19f, BW, 12f, GetHpText(), _topValue);
            Label(398f, 19f, BW, 12f, GetMpText(), _topValue);
            Label(508f, 19f, BW, 12f, GetExpText(), _topValue);

            // Number only: "Hạng" is baked into top_status_strip.png.
            Label(642f, 2f, 28f, 12f, GetRankText(), _rankStyle);

            // ═══ CHAT TABS — PC bottom-left ═══
            // PC chat tabs: Tất cả, Mật, Phòng, Bang hội, Môn phái, Khác
            var activeCh = ChatChannel.All;
            if (SandboxManager.Instance != null && SandboxManager.Instance.ChatService != null)
            {
                activeCh = SandboxManager.Instance.ChatService.ActiveChannel;
            }

            float tabX = 8f;
            float tabY = 54f;   // top boundary aligned under top bar
            Label(tabX, tabY, 50f, 16f, "Tất cả", (activeCh == ChatChannel.All) ? _chatTabAllActive : _chatTabAll);
            Label(tabX + 55f, tabY, 35f, 16f, "Mật", (activeCh == ChatChannel.Private) ? _chatTabActive : _chatTab);
            Label(tabX + 95f, tabY, 45f, 16f, "Phòng", (activeCh == ChatChannel.Room) ? _chatTabActive : _chatTab);
            Label(tabX + 145f, tabY, 60f, 16f, "Bang hội", (activeCh == ChatChannel.Guild) ? _chatTabActive : _chatTab);
            Label(tabX + 210f, tabY, 60f, 16f, "Môn phái", (activeCh == ChatChannel.Faction) ? _chatTabActive : _chatTab);
            Label(tabX + 280f, tabY, 45f, 16f, "Khác", (activeCh == ChatChannel.Other) ? _chatTabActive : _chatTab);


            // ═══ MINIMAP TEXT ═══
            DrawMinimapCoordinates();

            // ═══ BOTTOM BAR LABELS — DISABLED ═══
            // Bottom bar now uses authentic PC frame art (bottom_bar_bg.png) which has
            // icons + Vietnamese text ('Bảo vật') + tooltips baked in. Drawing text labels
            // here would double over the PC art. PC bar uses icons, not text labels.
            // (Action/menu buttons remain clickable via UXML containers.)

            // ═══ SKILL PANEL ═══
            DrawSkillPanelText();

            GUI.depth = oldDepth;
            GUI.matrix = Matrix4x4.identity;
        }

        private void DrawSkillPanelText()
        {
            var hud = FindObjectOfType<GameHudController>();
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
            VLTK.Sandbox.BaLangEnemyDatabase.WorldToMps(pos.x, pos.y, out int mpsX, out int mpsY);
            var coord = $"{mpsX}/{mpsY}";
            var mapManager = VLTK.Sandbox.SandboxManager.Instance?.MapManager;
            var rawMapName = mapManager?.ActiveMap?.catalogEntry?.displayNameRaw
                ?? mapManager?.ActiveMap?.catalogEntry?.displayNameNormalized
                ?? "Bản đồ";
            var mapName = VLTK.Sandbox.MapPortManifest.TryGet(mapManager?.ActiveMapId ?? -1, out var portEntry)
                ? portEntry.nameVi
                : ToVietnameseMapName(rawMapName);

            // PC minimap: yellow map name on top, green coords + "Tìm" below frame
            var mapNameStyle = new GUIStyle(_minimap) { alignment = TextAnchor.UpperRight, fontSize = 10,
                normal = { textColor = new Color(1f, 0.96f, 0.30f, 1f) } };
            Label(1138f, 2f, 130f, 14f, mapName, mapNameStyle);

            var coordStyle = new GUIStyle(_minimap) { alignment = TextAnchor.UpperLeft, fontSize = 9 };
            Label(1138f, 138f, 120f, 14f, coord + "  Tìm", coordStyle);

            // Large preview window coordinate readout
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
                "风之骑" => "Phong Kỳ (Vượt ải 120+)",
                "Map_389" => "Phong Kỳ (Vượt ải 120+)",
                "Phong Kỳ (trên 120)" => "Phong Kỳ (Vượt ải 120+)",
                "Phong K?(tr猲 120)" => "Phong Kỳ (Vượt ải 120+)",
                "Phong K� (tr�n 120)" => "Phong Kỳ (Vượt ải 120+)",
                "沙漠山洞1" => "Vượt ải Nhiếp Thí Trần",
                "Map_907" => "Vượt ải Nhiếp Thí Trần",
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
            var t = text ?? string.Empty;
            if (t.Length == 0) return;
            // PC-style black outline: draw text offset in black behind, then white on top
            var prev = style.normal.textColor;
            if (prev.r > 0.6f && prev.g > 0.6f && prev.b > 0.6f)
            {
                style.normal.textColor = new Color(0f, 0f, 0f, 0.9f);
                GUI.Label(new Rect(x - 1, y, w, h), t, style);
                GUI.Label(new Rect(x + 1, y, w, h), t, style);
                GUI.Label(new Rect(x, y - 1, w, h), t, style);
                GUI.Label(new Rect(x, y + 1, w, h), t, style);
                style.normal.textColor = prev;
            }
            GUI.Label(new Rect(x, y, w, h), t, style);
        }
    }
}
