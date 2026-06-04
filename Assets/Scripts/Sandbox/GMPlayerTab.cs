using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class GMPlayerTab : MonoBehaviour
    {
        private void Start()
        {
            var placeholder = transform.Find("Placeholder");
            if (placeholder != null)
            {
                var txt = placeholder.GetComponent<Text>();
                if (txt != null)
                {
                    txt.text = "HỆ THỐNG MÔN PHÁI (GM PANEL)\n\nChọn môn phái để chuyển đổi và kiểm tra kĩ năng:";
                }
            }

            // Create buttons container
            var containerGo = new GameObject("ButtonContainer");
            containerGo.transform.SetParent(transform, false);
            var rect = containerGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0, -60);
            rect.sizeDelta = new Vector2(400, 300);

            // Button 1: Cái Bang
            CreateGMButton(containerGo.transform, "Gia nhập Cái Bang (Hỏa)", new Vector2(0, 120), () =>
            {
                SwitchToFaction(CombatFaction.CaiBang);
            });
 
            // Button 2: Võ Đang
            CreateGMButton(containerGo.transform, "Gia nhập Võ Đang (Lôi)", new Vector2(0, 70), () =>
            {
                SwitchToFaction(CombatFaction.WuDang);
            });

            // Button 2.5: Thiếu Lâm
            CreateGMButton(containerGo.transform, "Gia nhập Thiếu Lâm (Kim)", new Vector2(0, 20), () =>
            {
                SwitchToFaction(CombatFaction.Shaolin);
            });

            // Button 2.7: Đường Môn
            CreateGMButton(containerGo.transform, "Gia nhập Đường Môn (Mộc)", new Vector2(0, -30), () =>
            {
                SwitchToFaction(CombatFaction.TangMen);
            });

            // Button 2.9: Nga My
            CreateGMButton(containerGo.transform, "Gia nhập Nga My (Thủy)", new Vector2(0, -80), () =>
            {
                SwitchToFaction(CombatFaction.EMei);
            });
 
            // Button 3: Max Level & Kỹ năng
            CreateGMButton(containerGo.transform, "Tối đa cấp độ & kĩ năng", new Vector2(0, -130), () =>
            {
                MaxAllStats();
            });
        }

        private void CreateGMButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject("GMButton_" + label);
            btnGo.transform.SetParent(parent, false);

            var rect = btnGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(250, 40);

            var img = btnGo.AddComponent<Image>();
            // Use simple dark background color
            img.color = new Color(0.18f, 0.24f, 0.35f, 1f);

            var btn = btnGo.AddComponent<Button>();
            btn.onClick.AddListener(action);

            // Add text child
            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRect = txtGo.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.sizeDelta = Vector2.zero;

            var txt = txtGo.AddComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Font.CreateDynamicFontFromOSFont("Arial", 14);
            txt.fontSize = 14;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }

        private void SwitchToFaction(CombatFaction faction)
        {
            var manager = SandboxManager.Instance;
            if (manager == null)
            {
                SubsystemLog.Warn("GMPlayerTab", "SandboxManager not found.");
                return;
            }

            // Initialize progression faction
            manager.GrantFactionSkillPanelProgression(faction);

            // Re-apply to gameplay actor
            var loop = manager.GameplayLoop;
            if (loop != null && loop.Player != null)
            {
                var player = loop.Player;
                player.combat.faction = faction;
                player.combat.knownSkills.Clear();
                foreach (var id in manager.PlayerProgression.knownSkills)
                    player.combat.knownSkills.Add(id);
                player.combat.skillLevels.Clear();
                foreach (var kv in manager.PlayerProgression.skillLevels)
                    player.combat.skillLevels[kv.Key] = kv.Value > 0 ? kv.Value : 1;
            }

            // Auto-assign default slots in CombatSkillSlotController
            var slotsType = System.Type.GetType("VLTK.UI.CombatSkillSlotController, Assembly-CSharp");
            if (slotsType != null)
            {
                var slotsObj = Object.FindAnyObjectByType(slotsType);
                if (slotsObj != null)
                {
                    var assignMethod = slotsType.GetMethod("AssignSkill");
                    if (assignMethod != null)
                    {
                        if (faction == CombatFaction.CaiBang)
                        {
                            assignMethod.Invoke(slotsObj, new object[] { 0, 357 });
                            assignMethod.Invoke(slotsObj, new object[] { 1, 359 });
                        }
                        else if (faction == CombatFaction.WuDang)
                        {
                            assignMethod.Invoke(slotsObj, new object[] { 0, 153 });
                            assignMethod.Invoke(slotsObj, new object[] { 1, 155 });
                        }
                        else if (faction == CombatFaction.Shaolin)
                        {
                            assignMethod.Invoke(slotsObj, new object[] { 0, 10 });
                            assignMethod.Invoke(slotsObj, new object[] { 1, 11 });
                        }
                        else if (faction == CombatFaction.TangMen)
                        {
                            assignMethod.Invoke(slotsObj, new object[] { 0, 47 });
                            assignMethod.Invoke(slotsObj, new object[] { 1, 58 });
                        }
                        else if (faction == CombatFaction.EMei)
                        {
                            assignMethod.Invoke(slotsObj, new object[] { 0, 80 });
                            assignMethod.Invoke(slotsObj, new object[] { 1, 91 });
                        }
                    }
                }
            }

            // Refresh UI GameHudController
            var hudType = System.Type.GetType("VLTK.UI.GameHudController, Assembly-CSharp");
            if (hudType != null)
            {
                var hudObj = Object.FindAnyObjectByType(hudType);
                if (hudObj != null)
                {
                    var isVisibleProp = hudType.GetProperty("IsCaiBangSkillPanelVisible");
                    bool isVisible = isVisibleProp != null && (bool)isVisibleProp.GetValue(hudObj, null);
                    if (isVisible)
                    {
                        var openMethod = hudType.GetMethod("OpenCaiBangSkillPanel");
                        if (openMethod != null) openMethod.Invoke(hudObj, null);
                    }
                }
            }

            SubsystemLog.Info("GM", $"Chuyển phái thành công: {faction}");
        }

        private void MaxAllStats()
        {
            var manager = SandboxManager.Instance;
            if (manager == null) return;

            var progression = manager.PlayerProgression;
            if (progression != null)
            {
                progression.level = PlayerProgressionState.CaiBangSkillPanelLevel;
                progression.fightSkillPoints = PlayerProgressionState.CaiBangSkillPanelPoints;
                progression.MaxAllSkillLevels(manager.CombatSkillCatalog);

                var loop = manager.GameplayLoop;
                if (loop != null && loop.Player != null)
                {
                    var player = loop.Player;
                    player.combat.level = progression.level;
                    player.combat.knownSkills.Clear();
                    foreach (var id in progression.knownSkills)
                        player.combat.knownSkills.Add(id);
                    player.combat.skillLevels.Clear();
                    foreach (var kv in progression.skillLevels)
                        player.combat.skillLevels[kv.Key] = kv.Value > 0 ? kv.Value : 1;
                }

                // Refresh visual slots
                var slotsType = System.Type.GetType("VLTK.UI.CombatSkillSlotController, Assembly-CSharp");
                if (slotsType != null)
                {
                    var slotsObj = Object.FindAnyObjectByType(slotsType);
                    if (slotsObj != null)
                    {
                        var assignMethod = slotsType.GetMethod("AssignSkill");
                        var leftSlotField = slotsType.GetField("leftSlotSkillId");
                        var rightSlotField = slotsType.GetField("rightSlotSkillId");
                        if (assignMethod != null && leftSlotField != null && rightSlotField != null)
                        {
                            int leftId = (int)leftSlotField.GetValue(slotsObj);
                            int rightId = (int)rightSlotField.GetValue(slotsObj);
                            assignMethod.Invoke(slotsObj, new object[] { 0, leftId });
                            assignMethod.Invoke(slotsObj, new object[] { 1, rightId });
                        }
                    }
                }

                // Refresh UI
                var hudType = System.Type.GetType("VLTK.UI.GameHudController, Assembly-CSharp");
                if (hudType != null)
                {
                    var hudObj = Object.FindAnyObjectByType(hudType);
                    if (hudObj != null)
                    {
                        var isVisibleProp = hudType.GetProperty("IsCaiBangSkillPanelVisible");
                        bool isVisible = isVisibleProp != null && (bool)isVisibleProp.GetValue(hudObj, null);
                        if (isVisible)
                        {
                            var openMethod = hudType.GetMethod("OpenCaiBangSkillPanel");
                            if (openMethod != null) openMethod.Invoke(hudObj, null);
                        }
                    }
                }

                SubsystemLog.Info("GM", "Đã nâng tối đa cấp độ và cấp kỹ năng võ công.");
            }
        }
    }
}
