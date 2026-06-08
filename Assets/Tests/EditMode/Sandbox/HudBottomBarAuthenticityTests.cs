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
            AssertPixelsEqual(itemEx.GetPixel(14, 14), pc.GetPixel(522 + 14, 559 + 14), "ItemEx crop must stay PC-derived");
            AssertPixelsEqual(task.GetPixel(14, 14), pc.GetPixel(584 + 14, 559 + 14), "Task crop must stay PC-derived");
            AssertPixelsEqual(chatRoom.GetPixel(14, 14), pc.GetPixel(708 + 14, 559 + 14), "ChatRoom crop must stay PC-derived");
            AssertPixelsEqual(rec.GetPixel(15, 15), pc.GetPixel(663 + 15, 502 + 15), "Recorder crop must stay PC-derived");

            Object.DestroyImmediate(pc);
            Object.DestroyImmediate(attack);
            Object.DestroyImmediate(empty);
            Object.DestroyImmediate(treasure);
            Object.DestroyImmediate(itemEx);
            Object.DestroyImmediate(task);
            Object.DestroyImmediate(chatRoom);
            Object.DestroyImmediate(rec);
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
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_rec.png");
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
