using System.IO;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Locks the mobile HUD to PC-derived visual pixels while allowing a mobile-first layout.
    /// Sources: /pc-evidence/pc_hud.png crops plus verified PC top-bar SPR art.
    /// </summary>
    [TestFixture]
    public class HudBottomBarAuthenticityTests
    {
        private const string ArtRoot = "UI/HUD/Art";

        [Test]
        public void PcDerivedHudArt_ExistsWithExpectedDimensions()
        {
            AssertTextureSize("top_status_strip.png", 552, 17);
            AssertTextureSize("bar_hp_fill.png", 106, 11);
            AssertTextureSize("bar_mp_fill.png", 106, 11);
            AssertTextureSize("bar_stamina_fill.png", 106, 11);
            AssertTextureSize("bar_exp_fill.png", 106, 11);
            AssertTextureSize("btn_primary_attack.png", 42, 42);
            AssertTextureSize("btn_skill_empty_pc.png", 42, 42);
            AssertTextureSize("btn_treasure.png", 58, 58);

            AssertTextureSize("btn_sit.png", 30, 30);
            AssertTextureSize("btn_run.png", 30, 30);
            AssertTextureSize("btn_horse.png", 30, 30);
            AssertTextureSize("btn_exchange.png", 30, 30);
            AssertTextureSize("btn_rec.png", 31, 31);
            AssertTextureSize("btn_pk.png", 20, 20);
            AssertTextureSize("btn_status.png", 20, 20);
            AssertTextureSize("btn_items.png", 20, 20);
            AssertTextureSize("btn_itemex.png", 28, 28);
            AssertTextureSize("btn_skills.png", 20, 20);
            AssertTextureSize("btn_task.png", 28, 28);
            AssertTextureSize("btn_friend.png", 20, 20);
            AssertTextureSize("btn_team.png", 20, 20);
            AssertTextureSize("btn_faction.png", 20, 20);
            AssertTextureSize("btn_chatroom.png", 28, 28);
            AssertTextureSize("btn_options.png", 20, 20);
            AssertTextureSize("btn_chat_send.png", 20, 20);
            AssertTextureSize("btn_chat_face.png", 24, 24);
            AssertTextureSize("btn_minimap_local_pc.png", 16, 16);
            AssertTextureSize("btn_minimap_search_pc.png", 16, 16);
            AssertTextureSize("btn_minimap_marker_pc.png", 16, 16);
            AssertTextureSize("btn_minimap_world_pc.png", 16, 16);
            for (int i = 1; i <= 9; i++)
                AssertTextureSize($"btn_quick_item_{i}_pc.png", 36, 36);
            AssertTextureSize("btn_pc_left_skill_slot.png", 36, 36);
            AssertTextureSize("btn_pc_right_skill_slot.png", 36, 36);
        }

        [Test]
        public void FullPcUtilitySet_IsDeclaredBehindMinimapSideToggle()
        {
            var uxmlPath = Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml");
            var uxml = File.ReadAllText(uxmlPath);

            StringAssert.Contains("name=\"UtilityToggleBtn\"", uxml);
            StringAssert.Contains("name=\"UtilitySwitchBtn\"", uxml);
            StringAssert.Contains("name=\"MobileUtilityDock\" class=\"hud-mobile-utility-dock hidden\"", uxml);
            StringAssert.Contains("name=\"MobileUtilityActionRow\"", uxml);
            StringAssert.Contains("name=\"MobileUtilityMenuRowA\"", uxml);
            StringAssert.Contains("name=\"MobileUtilityMenuRowB\"", uxml);
            foreach (var name in new[] { "ToggleMapBtn", "MinimapSearchBtn", "MinimapMarkerBtn", "WorldMapBtn" })
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " must exist as a PC minimap control.");
            foreach (var name in new[] { "ChatTabAll", "ChatTabPrivate", "ChatTabRoom", "ChatTabGuild", "ChatTabFaction", "ChatTabOther", "FaceBtn", "SendBtn" })
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " must exist as a PC bottom-chat control.");
            StringAssert.Contains("name=\"PcShortcutToggleBtn\"", uxml);
            StringAssert.Contains("name=\"PcShortcutDock\" class=\"hud-pc-shortcut-dock hidden\"", uxml);
            for (int i = 0; i < 9; i++)
                StringAssert.Contains($"name=\"PcItemSlot{i}\"", uxml, $"PC quick item slot {i + 1} must exist.");
            StringAssert.Contains("name=\"PcLeftSkillBtn\"", uxml);
            StringAssert.Contains("name=\"PcRightSkillBtn\"", uxml);

            foreach (var name in new[]
            {
                "BtnSit", "BtnRun", "BtnHorse", "BtnExchange", "BtnRec", "BtnPK", "BtnTreasure",
                "BtnStatus", "BtnItems", "BtnItemEx", "BtnSkills", "BtnTask", "BtnFriend",
                "BtnTeam", "BtnFaction", "BtnChatRoom", "BtnOptions"
            })
            {
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " must exist in the mobile HUD.");
            }
        }

        [Test]
        public void FullPcUtilitySet_HasPcSpecIconAssetsAndClickHandlers()
        {
            var controllerPath = Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs");
            var controller = File.ReadAllText(controllerPath);
            var icons = GetButtonIconMap();

            Assert.AreEqual(17, HudBottomBarPcSpec.ToolControlBar.Count, "PC 工具控制条 must stay at the full 17-button set.");
            Assert.AreEqual(9, HudBottomBarPcSpec.QuickItemSlots.Count, "PC Item_0..Item_8 quick slots must stay ported.");
            Assert.AreEqual(2, HudBottomBarPcSpec.ImmediateSkillSlots.Count, "PC left/right skill boxes must stay ported.");
            foreach (var pair in PcUtilityButtonIds())
            {
                string pcKey = pair.Key;
                string buttonName = pair.Value.buttonName;
                string handlerName = pair.Value.handlerName;
                Assert.IsTrue(HudBottomBarPcSpec.ToolControlBar.ContainsKey(pcKey), pcKey + " must remain in PC toolbar spec.");
                Assert.IsTrue(icons.ContainsKey(buttonName), buttonName + " must have an icon mapping.");
                string iconFile = icons[buttonName] + ".png";
                Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, ArtRoot, iconFile)), iconFile + " must exist in Assets HUD art.");
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, iconFile)), iconFile + " must exist in StreamingAssets for mobile.");
                StringAssert.Contains($"RegisterClick(root, \"{buttonName}\", {handlerName})", controller, buttonName + " must be wired to its handler.");
            }

            Assert.IsTrue(icons.ContainsKey("UtilitySwitchBtn"), "Mobile bar switch must have a PC-derived icon mapping.");
            Assert.AreEqual("btn_options", icons["UtilitySwitchBtn"], "Switch icon must reuse the PC Options art, not generated art.");
            StringAssert.Contains("RegisterClick(root, \"UtilitySwitchBtn\", OnUtilitySwitchClick)", controller);
            Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, "btn_chat_send.png")), "PC send button must exist in StreamingAssets.");
            Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, "btn_chat_face.png")), "PC face button must exist in StreamingAssets.");
            StringAssert.Contains("RegisterClick(root, \"SendBtn\", OnSendChatClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatTabGuild\", () => SelectChatChannel(ChatChannel.Guild))", controller);
            StringAssert.Contains("RegisterClick(root, \"MinimapSearchBtn\", OnMinimapSearchClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"MinimapMarkerBtn\", OnMinimapMarkerClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"PcShortcutToggleBtn\", OnPcShortcutToggleClick)", controller);
            StringAssert.Contains("RegisterClick(root, $\"PcItemSlot{slot}\", () => OnPcItemShortcutClick(slot))", controller);
            StringAssert.Contains("RegisterClick(root, \"PcLeftSkillBtn\", () => OnPcSkillShortcutClick(0))", controller);
            StringAssert.Contains("RegisterClick(root, \"PcRightSkillBtn\", () => OnPcSkillShortcutClick(1))", controller);
            foreach (var file in new[] { "btn_minimap_local_pc.png", "btn_minimap_search_pc.png", "btn_minimap_marker_pc.png", "btn_minimap_world_pc.png" })
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, file)), file + " must exist in StreamingAssets.");
            for (int i = 1; i <= 9; i++)
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, $"btn_quick_item_{i}_pc.png")), $"quick item {i} must exist in StreamingAssets.");
            Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, "btn_pc_left_skill_slot.png")));
            Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, "btn_pc_right_skill_slot.png")));
        }

        [Test]
        public void StitchUxEvidence_IsDownloadedAndAppliedToPcShortcutDock()
        {
            string evidenceRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../pc-evidence/stitch"));
            string screenshotPath = Path.Combine(evidenceRoot, "wuxia_mobile_arpg_hud_classic_v2.png");
            string screenJsonPath = Path.Combine(evidenceRoot, "wuxia_mobile_arpg_hud_classic_v2.screen.json");
            Assert.IsTrue(File.Exists(screenshotPath), "Stitch HUD screenshot evidence must be checked in.");
            Assert.IsTrue(File.Exists(screenJsonPath), "Stitch screen metadata/code placeholder must be checked in.");
            StringAssert.Contains("34fc0413483548ebab83752d8a1271c3", File.ReadAllText(screenJsonPath));

            var css = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uss"));
            StringAssert.Contains("Stitch \"Jade Path Legacy\" UX", css);
            StringAssert.Contains("right: 148px;", css, "The 1-9/T/P switch must sit in the top-right rail beside the minimap.");
            StringAssert.Contains("top: 68px;", css, "The 1-9/T/P switch must stay out of the bottom thumb/action lane.");
            StringAssert.Contains("right: 204px;", css, "Expanded PC shortcut dock must open left of minimap/rail controls.");
            StringAssert.Contains("top: 74px;", css, "Expanded PC shortcut dock must be a top safe-zone panel.");
            StringAssert.Contains("flex-direction: row;", css, "Expanded PC shortcut dock must be compact, not a tall bottom-center strip.");
            StringAssert.Contains("height: 44px;", css, "Quick-item hitboxes must meet mobile touch target minimum while preserving 36px PC icon art.");
            StringAssert.Contains("/* PC SPR chat channel button visuals */", css);
            foreach (var file in new[]
            {
                "主界面按钮-世界频道选择.png", "主界面按钮-密人频道选择.png", "主界面按钮-城市频道选择.png",
                "主界面按钮-队伍频道选择.png", "主界面按钮-门派频道选择.png", "主界面按钮-好友频道选择.png"
            })
            {
                StringAssert.Contains(file, css, file + " must be used by the HUD CSS, not replaced with generated UI art.");
                Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, ArtRoot, file)), file + " must exist in Assets HUD art.");
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, file)), file + " must exist in StreamingAssets for mobile.");
            }
        }

        [Test]
        public void MobileCombatButtonCrops_PreservePcScreenshotPixels()
        {
            var pc = LoadTexture(Path.GetFullPath(Path.Combine(Application.dataPath, "../pc-evidence/pc_hud.png")));
            var attack = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_primary_attack.png"));
            var empty = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_skill_empty_pc.png"));
            var treasure = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_treasure.png"));

            AssertPixelsEqual(attack.GetPixel(20, 20), pc.GetPixel(374 + 20, 526 + 20), "primary attack crop must stay PC-derived");
            AssertPixelsEqual(empty.GetPixel(20, 20), pc.GetPixel(416 + 20, 526 + 20), "empty skill crop must stay PC-derived");
            AssertPixelsEqual(treasure.GetPixel(29, 29), pc.GetPixel(742 + 29, 502 + 29), "treasure crop must stay PC-derived");

            var itemEx = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_itemex.png"));
            var task = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_task.png"));
            var chatRoom = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_chatroom.png"));
            var rec = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_rec.png"));
            var face = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_chat_face.png"));
            var minimapSearch = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_minimap_search_pc.png"));
            var quick1 = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_quick_item_1_pc.png"));
            var quick9 = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_quick_item_9_pc.png"));
            var leftSkill = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_pc_left_skill_slot.png"));
            var rightSkill = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_pc_right_skill_slot.png"));
            AssertPixelsEqual(itemEx.GetPixel(14, 14), pc.GetPixel(522 + 14, 559 + 14), "ItemEx crop must stay PC-derived");
            AssertPixelsEqual(task.GetPixel(14, 14), pc.GetPixel(584 + 14, 559 + 14), "Task crop must stay PC-derived");
            AssertPixelsEqual(chatRoom.GetPixel(14, 14), pc.GetPixel(708 + 14, 559 + 14), "ChatRoom crop must stay PC-derived");
            AssertPixelsEqual(rec.GetPixel(15, 15), pc.GetPixel(663 + 15, 502 + 15), "Recorder crop must stay PC-derived");
            AssertPixelsEqual(face.GetPixel(12, 12), pc.GetPixel(282 + 12, 526 + 12), "chat face crop must stay PC-derived");
            AssertPixelsEqual(minimapSearch.GetPixel(8, 8), pc.GetPixel(758 + 8, 134 + 8), "minimap search crop must stay PC-derived");
            AssertPixelsEqual(quick1.GetPixel(18, 18), pc.GetPixel(15 + 18, 550 + 18), "quick item 1 crop must stay PC-derived");
            AssertPixelsEqual(quick9.GetPixel(18, 18), pc.GetPixel(320 + 18, 550 + 18), "quick item 9 crop must stay PC-derived");
            AssertPixelsEqual(leftSkill.GetPixel(18, 18), pc.GetPixel(372 + 18, 529 + 18), "left skill crop must stay PC-derived");
            AssertPixelsEqual(rightSkill.GetPixel(18, 18), pc.GetPixel(409 + 18, 529 + 18), "right skill crop must stay PC-derived");

            Object.DestroyImmediate(pc);
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(empty);
            Object.DestroyImmediate(treasure);
            Object.DestroyImmediate(itemEx);
            Object.DestroyImmediate(task);
            Object.DestroyImmediate(chatRoom);
            Object.DestroyImmediate(rec);
            Object.DestroyImmediate(face);
            Object.DestroyImmediate(minimapSearch);
            Object.DestroyImmediate(quick1);
            Object.DestroyImmediate(quick9);
            Object.DestroyImmediate(leftSkill);
            Object.DestroyImmediate(rightSkill);
        }

        [Test]
        public void HudPanelAndTextureImport_KeepResponsivePixelPerfectSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/HUD/HudPanelSettings.asset");
            Assert.NotNull(settings, "HudPanelSettings asset must exist.");
            Assert.AreEqual(PanelScaleMode.ScaleWithScreenSize, settings.scaleMode);
            Assert.AreEqual(new Vector2Int(1280, 720), settings.referenceResolution);
            Assert.AreEqual(PanelScreenMatchMode.Shrink, settings.screenMatchMode);

            AssertCriticalTextureImport("Assets/UI/HUD/Art/top_status_strip.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_primary_attack.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_skill_empty_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_treasure.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_itemex.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_task.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_chatroom.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_chat_send.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_chat_face.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_local_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_search_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_marker_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_world_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_rec.png");
            for (int i = 1; i <= 9; i++)
                AssertCriticalTextureImport($"Assets/UI/HUD/Art/btn_quick_item_{i}_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_pc_left_skill_slot.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_pc_right_skill_slot.png");
        }

        private static Dictionary<string, (string buttonName, string handlerName)> PcUtilityButtonIds()
        {
            return new Dictionary<string, (string buttonName, string handlerName)>
            {
                ["Status"] = ("BtnStatus", "OnStatusClick"),
                ["Items"] = ("BtnItems", "OnItemsClick"),
                ["ItemEx"] = ("BtnItemEx", "OnItemExClick"),
                ["Skills"] = ("BtnSkills", "OnSkillsClick"),
                ["Task"] = ("BtnTask", "OnTaskClick"),
                ["Friend"] = ("BtnFriend", "OnFriendClick"),
                ["Team"] = ("BtnTeam", "OnTeamClick"),
                ["Faction"] = ("BtnFaction", "OnFactionClick"),
                ["ChatRoom"] = ("BtnChatRoom", "OnChatRoomClick"),
                ["Options"] = ("BtnOptions", "OnOptionsClick"),
                ["Sit"] = ("BtnSit", "OnSitClick"),
                ["Run"] = ("BtnRun", "OnRunClick"),
                ["Horse"] = ("BtnHorse", "OnHorseClick"),
                ["Exchange"] = ("BtnExchange", "OnExchangeClick"),
                ["Rec"] = ("BtnRec", "OnRecClick"),
                ["PK"] = ("BtnPK", "OnPKClick"),
                ["Treasure"] = ("BtnTreasure", "OnTreasureClick"),
            };
        }

        private static Dictionary<string, string> GetButtonIconMap()
        {
            var field = typeof(GameHudController).GetField("ButtonIcons", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(field, "GameHudController.ButtonIcons must exist.");
            return (Dictionary<string, string>)field.GetValue(null);
        }

        private static void AssertCriticalTextureImport(string assetPath)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            Assert.NotNull(tex, assetPath + " must import as a Texture2D.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            Assert.NotNull(importer, assetPath + " importer must exist.");
            Assert.AreEqual(TextureImporterNPOTScale.None, importer.npotScale, assetPath + " must preserve NPOT size.");
            Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, assetPath + " must not blur/compress HUD pixels.");
            Assert.AreEqual(FilterMode.Point, importer.filterMode, assetPath + " must remain pixel crisp.");
        }

        private static void AssertTextureSize(string file, int width, int height)
        {
            var path = Path.Combine(Application.dataPath, ArtRoot, file);
            var tex = LoadTexture(path);
            Assert.AreEqual(width, tex.width, file + " width");
            Assert.AreEqual(height, tex.height, file + " height");
            Object.DestroyImmediate(tex);
        }

        private static Texture2D LoadTexture(string path)
        {
            Assert.IsTrue(File.Exists(path), "Missing HUD art: " + path);
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "Invalid PNG: " + path);
            return tex;
        }

        private static void AssertPixelsEqual(Color a, Color b, string message)
        {
            const float epsilon = 1f / 255f;
            Assert.LessOrEqual(Mathf.Abs(a.r - b.r), epsilon, message + " (r)");
            Assert.LessOrEqual(Mathf.Abs(a.g - b.g), epsilon, message + " (g)");
            Assert.LessOrEqual(Mathf.Abs(a.b - b.b), epsilon, message + " (b)");
            Assert.LessOrEqual(Mathf.Abs(a.a - b.a), epsilon, message + " (a)");
        }
    }
}
