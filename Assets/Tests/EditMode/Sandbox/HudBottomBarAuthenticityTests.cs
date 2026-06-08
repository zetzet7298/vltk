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
            AssertTextureSize("btn_chat_channel_identity_pc.png", 32, 32);
            AssertTextureSize("chat_bar_top.png", 15, 16);
            AssertTextureSize("chat_bar_bottom.png", 15, 16);
            AssertTextureSize("聊天条阴影按钮.png", 15, 16);
            AssertTextureSize("QQ主界面向上按钮_00.png", 16, 10);
            AssertTextureSize("QQ主界面向下按钮_00.png", 16, 10);
            AssertTextureSize("btn_chat_scroll_thumb_pc.png", 15, 27);
            AssertTextureSize("btn_chat_split_pc.png", 14, 85);
            AssertTextureSize("频道开与关b.png", 20, 20);
            AssertTextureSize("提示信息窗－上_00.png", 15, 14);
            AssertTextureSize("提示信息窗－开关_00.png", 15, 14);
            AssertTextureSize("提示信息窗－下_00.png", 15, 14);
            AssertTextureSize("btn_minimap_flag_pc.png", 16, 16);
            AssertTextureSize("btn_minimap_switch_pc.png", 16, 16);
            AssertTextureSize("btn_minimap_world_full_pc.png", 16, 16);
            AssertTextureSize("btn_minimap_cave_pc.png", 16, 16);
            AssertTextureSize("icon_bar_arena.png", 25, 25);
            AssertTextureSize("icon_bar_activity.png", 23, 23);
            AssertTextureSize("icon_bar_treasure.png", 23, 23);
            AssertTextureSize("icon_bar_shop.png", 23, 23);
            AssertTextureSize("icon_bar_pet.png", 23, 23);
            AssertTextureSize("icon_bar_loginprize.png", 23, 23);
            AssertTextureSize("icon_bar_funcprize.png", 23, 23);
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
            foreach (var name in new[] { "MinimapMarkerBtn", "ToggleMapBtn", "WorldMapBtn", "CaveMapBtn" })
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " must exist as a PC minimap control.");
            foreach (var name in new[] { "OpenChannelBtn", "ChatTabAll", "ChatTabPrivate", "ChatTabRoom", "ChatTabGuild", "ChatTabFaction", "ChatTabOther", "ChatChannelIdentityBtn", "FaceBtn", "SendBtn" })
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " must exist as a PC bottom-chat control.");
            foreach (var name in new[] { "ChatRail", "ChatSizeBtn", "ChatMoveBtn", "ChatShadowBtn", "ChatScrollUpBtn", "ChatScrollThumbBtn", "ChatSplitBtn", "ChatChannelToggleBtn", "ChatScrollDownBtn", "ChatSysUpBtn", "ChatSysOpenBtn", "ChatSysDownBtn" })
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " must exist as a PC chat rail control.");
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
        public void PcHudVisibleControlManifest_CoversCurrentPcEvidence()
        {
            // PC evidence scope: pc-evidence/pc_hud.png plus INIs 8da7027d, dc11ac12, 7e20a7ac/c9c8a750, ec10b91e/f8bf2550.
            // This manifest intentionally separates visual controls from mobile-only placement, so layout can adapt but coverage cannot regress.
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            var controller = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs"));

            string[] topStatusControls =
            {
                "PcConnectionStatusText", "LevelText", "StaminaBarTrack", "HpBarTrack", "MpBarTrack", "ExpBarTrack", "RankText", "SceneName",
            };
            string[] interactivePcButtons =
            {
                "ScenePos", "MinimapMarkerBtn", "ToggleMapBtn", "WorldMapBtn", "CaveMapBtn",
                "ChatSizeBtn", "ChatScrollUpBtn", "ChatScrollThumbBtn", "ChatScrollDownBtn", "ChatSplitBtn",
                "ChatChannelToggleBtn", "ChatShadowBtn", "ChatMoveBtn", "ChatSysUpBtn", "ChatSysDownBtn", "ChatSysOpenBtn",
                "OpenChannelBtn", "ChatTabAll", "ChatTabPrivate", "ChatTabRoom", "ChatTabGuild", "ChatTabFaction", "ChatTabOther",
                "ChatChannelIdentityBtn", "FaceBtn", "SendBtn",
                "PcShortcutToggleBtn", "PcLeftSkillBtn", "PcRightSkillBtn",
                "UtilityToggleBtn", "UtilitySwitchBtn",
                "BtnSit", "BtnRun", "BtnHorse", "BtnExchange", "BtnRec", "BtnPK", "BtnTreasure",
                "BtnStatus", "BtnItems", "BtnItemEx", "BtnSkills", "BtnTask", "BtnFriend",
                "BtnTeam", "BtnFaction", "BtnChatRoom", "BtnOptions",
            };

            foreach (var name in topStatusControls)
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " from PC top status bar must remain present.");
            foreach (var name in interactivePcButtons)
            {
                StringAssert.Contains($"name=\"{name}\"", uxml, name + " from the PC HUD evidence/INI must remain present.");
                if (name == "ToggleMapBtn" || name == "WorldMapBtn")
                    StringAssert.Contains($"RegisterPreviewOpen(root, \"{name}\")", controller, name + " must open the PC map preview.");
                else if (name == "FaceBtn")
                    StringAssert.Contains("OpenFacePicker();", controller, "FaceBtn must open the PC emoji/face picker.");
                else
                    StringAssert.Contains($"RegisterClick(root, \"{name}\"", controller, name + " must have a concrete mobile handler.");
            }
            StringAssert.Contains("name=\"MapPosInput\"", uxml, "PC ec10b91e [MapPosInput] coordinate entry must remain present.");
            StringAssert.Contains("_mapPosInput.RegisterCallback<KeyDownEvent>", controller, "MapPosInput must accept Enter to apply path-find coordinates.");
            StringAssert.Contains("TryParsePcScenePos", controller, "MapPosInput must parse PC x/y coordinate format.");
            for (int i = 0; i < 9; i++)
            {
                StringAssert.Contains($"name=\"PcItemSlot{i}\"", uxml, $"PC quick item slot {i + 1} must remain present.");
                StringAssert.Contains("RegisterClick(root, $\"PcItemSlot{slot}\", () => OnPcItemShortcutClick(slot))", controller);
            }
        }

        [Test]
        public void PcMainHudIniInteractiveControls_HaveExplicitMobileBindings()
        {
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            var controller = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs"));

            Assert.AreEqual(9, HudBottomBarPcSpec.MainHudControlBindings.Count,
                "PC 主界面玩家信息窗口.ini uid e3b06434 interactive controls must be explicitly audited, including Market/OpenChannelBtn/Recorder.");

            foreach (var binding in HudBottomBarPcSpec.MainHudControlBindings)
            {
                StringAssert.Contains($"name=\"{binding.mobileElement}\"", uxml, binding.pcName + " must map to a concrete mobile HUD element.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(binding.sourceNote), binding.pcName + " must keep PC provenance/behavior notes.");

                if (binding.mobileElement == "ChatInput")
                    StringAssert.Contains("_chatInput = root.Q<TextField>(\"ChatInput\")", controller, "PC InputEdit must bind to the runtime chat input field.");
                else if (binding.handlerName == "OpenFacePicker")
                    StringAssert.Contains("OpenFacePicker();", controller, "PC Face button must open the face/emote picker.");
                else
                    StringAssert.Contains($"RegisterClick(root, \"{binding.mobileElement}\", {binding.handlerName})", controller, binding.pcName + " must invoke its PC-equivalent handler.");
            }

            StringAssert.Contains("Market", HudBottomBarPcSpec.MainHudControlBindings[8].pcName);
            StringAssert.Contains("奇珍阁按钮_vn.spr", HudBottomBarPcSpec.MainHudControlBindings[8].sourceNote);
            StringAssert.Contains("MallPanelService.BuildSnapshot", controller, "PC Market/Kỳ Trân Các must route to real mall data, not a placeholder.");
            StringAssert.Contains("Kỳ Trân Các", controller, "PC Market button must expose the Vietnamese Kỳ Trân Các behavior.");
        }

        [Test]
        public void PcMainDateTimeStatus_IsPortedFromMainHudIni()
        {
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            var css = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uss"));
            var controller = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs"));

            StringAssert.Contains("name=\"PcConnectionStatusText\"", uxml);
            StringAssert.Contains("PC 主界面玩家信息窗口.ini [DateTime]", uxml);
            StringAssert.Contains("left: 15px;", css);
            StringAssert.Contains("top: 2px;", css);
            StringAssert.Contains("Hoạt động tốt", controller);
            StringAssert.Contains("Quá đông", controller);
            StringAssert.Contains("Bị giật", controller);
            StringAssert.Contains("UpdatePcConnectionStatus();", controller);
        }

        [Test]
        public void MinimapMapPosInput_ParsesPcCoordinateFormat()
        {
            var method = typeof(GameHudController).GetMethod("TryParsePcScenePos", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.NotNull(method, "PC [MapPosInput] parser must exist.");

            object[] args = { "210/203", Vector2.zero };
            bool ok = (bool)method.Invoke(null, args);
            Assert.IsTrue(ok);
            Assert.AreEqual(new Vector2(1680f, -1624f), (Vector2)args[1]);

            args = new object[] { "210,203", Vector2.zero };
            ok = (bool)method.Invoke(null, args);
            Assert.IsTrue(ok, "Mobile should accept comma as a touch-keyboard-friendly separator too.");
            Assert.AreEqual(new Vector2(1680f, -1624f), (Vector2)args[1]);

            args = new object[] { "bad", Vector2.zero };
            ok = (bool)method.Invoke(null, args);
            Assert.IsFalse(ok);
        }

        [Test]
        public void PcToolbarSourceAudit_DoesNotInventDisabledZhenFaButton()
        {
            // PC source: dc11ac12 工具控制条.ini has Button14=ZhenFa after comment ";û��",
            // but no [ZhenFa] section, Image, Tip, or ClassType. It is not visible in pc-evidence/pc_hud.png.
            Assert.IsTrue(HudBottomBarPcSpec.DisabledDeclaredToolButtons.ContainsKey("ZhenFa"));
            StringAssert.Contains("no [ZhenFa] section/art/ClassType", HudBottomBarPcSpec.DisabledDeclaredToolButtons["ZhenFa"]);
            Assert.IsFalse(HudBottomBarPcSpec.ToolControlBar.ContainsKey("ZhenFa"), "Do not fabricate a ZhenFa HUD button without PC art/handler evidence.");

            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            var icons = GetButtonIconMap();
            Assert.IsFalse(uxml.Contains("BtnZhenFa"), "Mobile HUD must not expose fake ZhenFa art.");
            Assert.IsFalse(icons.ContainsKey("BtnZhenFa"), "ButtonIcons must stay PC-proven only.");
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
            Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, "btn_chat_channel_identity_pc.png")), "PC current-channel identity button must exist in StreamingAssets.");
            StringAssert.Contains("RegisterClick(root, \"OpenChannelBtn\", OnChatChannelToggleClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatChannelIdentityBtn\", OnChatChannelIdentityClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"SendBtn\", OnSendChatClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatScrollUpBtn\", OnChatScrollUpClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatScrollThumbBtn\", OnChatScrollThumbClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatScrollDownBtn\", OnChatScrollDownClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatSplitBtn\", OnChatSplitClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatChannelToggleBtn\", OnChatChannelToggleClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"ChatTabGuild\", () => SelectChatChannel(ChatChannel.Guild))", controller);
            StringAssert.Contains("RegisterClick(root, \"MinimapMarkerBtn\", OnMinimapMarkerClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"CaveMapBtn\", OnCaveMapClick)", controller);
            StringAssert.Contains("RegisterClick(root, \"PcShortcutToggleBtn\", OnPcShortcutToggleClick)", controller);
            StringAssert.Contains("RegisterClick(root, $\"PcItemSlot{slot}\", () => OnPcItemShortcutClick(slot))", controller);
            StringAssert.Contains("RegisterClick(root, \"PcLeftSkillBtn\", () => OnPcSkillShortcutClick(0))", controller);
            StringAssert.Contains("RegisterClick(root, \"PcRightSkillBtn\", () => OnPcSkillShortcutClick(1))", controller);
            foreach (var file in new[] { "btn_minimap_flag_pc.png", "btn_minimap_switch_pc.png", "btn_minimap_world_full_pc.png", "btn_minimap_cave_pc.png" })
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, file)), file + " must exist in StreamingAssets.");
            for (int i = 1; i <= 9; i++)
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, $"btn_quick_item_{i}_pc.png")), $"quick item {i} must exist in StreamingAssets.");
            Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, "btn_pc_left_skill_slot.png")));
            Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, "btn_pc_right_skill_slot.png")));

            Assert.AreEqual(7, HudBottomBarPcSpec.IconBar.Count, "PC Ui3/icon_bar.ini must expose Icon_0..Icon_6.");
            foreach (var pair in PcIconBarButtonIds())
            {
                Assert.IsTrue(icons.ContainsKey(pair.Key), pair.Key + " must have PC SPR icon art.");
                string iconFile = icons[pair.Key] + ".png";
                Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, ArtRoot, iconFile)), iconFile + " must exist in Assets HUD art.");
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, iconFile)), iconFile + " must exist in StreamingAssets.");
                StringAssert.Contains($"RegisterClick(root, \"{pair.Key}\", () => OnIconBarClick({pair.Value}))", controller);
            }
        }

        [Test]
        public void PcIconBar_PortsSevenPcIniIconsWithRuntimeHandlers()
        {
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            var css = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uss"));
            var controller = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs"));

            StringAssert.Contains("PcIconBar", uxml);
            StringAssert.Contains("Ui3/icon_bar.ini", controller);
            StringAssert.Contains("PC Ui3/icon_bar.ini", css);
            StringAssert.Contains("width: 46px;", css, "Icon bar hitboxes must be mobile touch-sized while preserving 23/25px PC icon art.");
            foreach (var pair in PcIconBarButtonIds())
            {
                StringAssert.Contains($"name=\"{pair.Key}\"", uxml);
                StringAssert.Contains($"RegisterClick(root, \"{pair.Key}\", () => OnIconBarClick({pair.Value}))", controller);
            }
            StringAssert.Contains("ArenaService.GetAllArenas()", controller);
            StringAssert.Contains("ActivityService.GetAllActivities()", controller);
            StringAssert.Contains("TreasureHuntService.All", controller);
            StringAssert.Contains("MallService.All", controller);
            StringAssert.Contains("PartnerService.AllActivePets", controller);
            StringAssert.Contains("SignInService.All", controller);
            StringAssert.Contains("EventBonusService.GetAllEvents()", controller);
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
                "主界面按钮-队伍频道选择.png", "主界面按钮-门派频道选择.png", "主界面按钮-好友频道选择.png",
                "chat_bar_top.png", "chat_bar_bottom.png", "聊天条阴影按钮.png",
                "QQ主界面向上按钮_00.png", "QQ主界面向下按钮_00.png", "btn_chat_scroll_thumb_pc.png", "btn_chat_split_pc.png", "频道开与关b.png",
                "提示信息窗－上_00.png", "提示信息窗－开关_00.png", "提示信息窗－下_00.png"
            })
            {
                StringAssert.Contains(file, css, file + " must be used by the HUD CSS, not replaced with generated UI art.");
                Assert.IsTrue(File.Exists(Path.Combine(Application.dataPath, ArtRoot, file)), file + " must exist in Assets HUD art.");
                Assert.IsTrue(File.Exists(Path.Combine(Application.streamingAssetsPath, ArtRoot, file)), file + " must exist in StreamingAssets for mobile.");
            }
        }

        [Test]
        public void ChatRailMainButtons_FollowPcIniTopOrder()
        {
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            int size = uxml.IndexOf("name=\"ChatSizeBtn\"", System.StringComparison.Ordinal);
            int scrollUp = uxml.IndexOf("name=\"ChatScrollUpBtn\"", System.StringComparison.Ordinal);
            int thumb = uxml.IndexOf("name=\"ChatScrollThumbBtn\"", System.StringComparison.Ordinal);
            int scrollDown = uxml.IndexOf("name=\"ChatScrollDownBtn\"", System.StringComparison.Ordinal);
            int split = uxml.IndexOf("name=\"ChatSplitBtn\"", System.StringComparison.Ordinal);
            int channel = uxml.IndexOf("name=\"ChatChannelToggleBtn\"", System.StringComparison.Ordinal);
            int shadow = uxml.IndexOf("name=\"ChatShadowBtn\"", System.StringComparison.Ordinal);
            int move = uxml.IndexOf("name=\"ChatMoveBtn\"", System.StringComparison.Ordinal);
            Assert.IsTrue(size >= 0 && scrollUp > size && thumb > scrollUp && scrollDown > thumb && split > scrollDown && channel > split && shadow > channel && move > shadow,
                "PC chat HUD order is SizeBtn, ChatRoom_Scroll thumb/controls, SplitBtn, channel toggle, ShadowBtn Top=111, MoveImg Top=125; mobile must not omit SplitBtn.");
        }

        [Test]
        public void ChatRailKeepsPcPixelsWithoutDarkMobileOverlay()
        {
            var css = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uss"));
            var panel = ExtractCssBlock(css, ".hud-chat-panel");
            var railButton = ExtractCssBlock(css, ".hud-chat-rail-btn");
            var openChannel = ExtractCssBlock(css, ".hud-open-channel-btn");

            StringAssert.Contains("background-color: rgba(0,0,0,0);", panel, "ChatBar root must stay transparent so it does not wrap the 7-o'clock joystick/chat corner.");
            StringAssert.Contains("border-width: 0px;", panel, "ChatBar root must not draw a mobile wrapper over PC HUD pixels.");
            StringAssert.Contains("background-color: rgba(0,0,0,0);", railButton, "PC chat rail buttons use exact SPR icons only; the widened mobile hitbox must be visually transparent.");
            StringAssert.Contains("border-width: 0px;", railButton, "PC chat rail hitboxes must not draw a dark column over the bottom-left HUD.");
            StringAssert.Contains("width: 60px;", openChannel, "PC [OpenChannelBtn] keeps its 60x60 hit proxy from e3b06434.dat.");
            StringAssert.Contains("height: 60px;", openChannel, "PC [OpenChannelBtn] keeps its 60x60 hit proxy from e3b06434.dat.");
            StringAssert.Contains("background-color: rgba(0,0,0,0);", openChannel, "PC [OpenChannelBtn] has no concrete SPR file; it must not draw a dark 7-o'clock wrapper.");
            StringAssert.Contains("border-width: 0px;", openChannel, "PC [OpenChannelBtn] must be behavioral only, not a visual overlay.");
            StringAssert.Contains("pickingMode = PickingMode.Ignore", File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs")),
                "Decorative HUD containers must not steal joystick touches.");
        }

        [Test]
        public void ChatSystemRailButtons_FollowPcIniTopOrder()
        {
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            int up = uxml.IndexOf("name=\"ChatSysUpBtn\"", System.StringComparison.Ordinal);
            int down = uxml.IndexOf("name=\"ChatSysDownBtn\"", System.StringComparison.Ordinal);
            int open = uxml.IndexOf("name=\"ChatSysOpenBtn\"", System.StringComparison.Ordinal);
            Assert.IsTrue(up >= 0 && down > up && open > down, "PC c9c8a750/7e20a7ac has SysRoom_Up Top=0, SysRoom_Down Top=14, SysRoom_Open Top=28; mobile rail must not swap Down/Open.");
        }

        [Test]
        public void ChatChannelIdentityButton_PortsPcInputPrefixControl()
        {
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            int channel = uxml.IndexOf("name=\"ChatChannelIdentityBtn\"", System.StringComparison.Ordinal);
            int input = uxml.IndexOf("name=\"ChatInput\"", System.StringComparison.Ordinal);
            int face = uxml.IndexOf("name=\"FaceBtn\"", System.StringComparison.Ordinal);
            Assert.IsTrue(channel >= 0 && input > channel && face > input, "PC current-channel identity icon must sit before the chat input, then face/send controls.");

            var controller = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs"));
            StringAssert.Contains("private void OnChatChannelIdentityClick()", controller);
            StringAssert.Contains("SelectChatChannel(next)", controller);
            StringAssert.Contains("PC: ô biểu tượng bên trái dòng nhập", controller);
        }

        [Test]
        public void MinimapButtons_FollowPcIniOrderAndSemantics()
        {
            var uxml = File.ReadAllText(Path.Combine(Application.dataPath, "UI/HUD/GameHud.uxml"));
            int flag = uxml.IndexOf("name=\"MinimapMarkerBtn\"", System.StringComparison.Ordinal);
            int toggle = uxml.IndexOf("name=\"ToggleMapBtn\"", System.StringComparison.Ordinal);
            int world = uxml.IndexOf("name=\"WorldMapBtn\"", System.StringComparison.Ordinal);
            int cave = uxml.IndexOf("name=\"CaveMapBtn\"", System.StringComparison.Ordinal);
            Assert.IsTrue(flag >= 0 && toggle > flag && world > toggle && cave > world, "Mini map buttons must match PC INI order BtnFlag/SwitchBtn/WorldMapBtn/CaveMapBtn.");

            var controller = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/UI/GameHudController.cs"));
            StringAssert.Contains("LoadIcon(markerMap, artPath, \"btn_minimap_flag_pc\")", controller);
            StringAssert.Contains("LoadIcon(toggleMap, artPath, \"btn_minimap_switch_pc\")", controller);
            StringAssert.Contains("LoadIcon(worldMap, artPath, \"btn_minimap_world_full_pc\")", controller);
            StringAssert.Contains("LoadIcon(caveMap, artPath, \"btn_minimap_cave_pc\")", controller);
            StringAssert.Contains("GmTeleportCatalogService.Filter(catalog.GetAllDestinations(), string.Empty, GmTeleportCatalogService.FilterCave)", controller);
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
            var channelIdentity = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_chat_channel_identity_pc.png"));
            var chatThumb = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_chat_scroll_thumb_pc.png"));
            var chatSplit = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_chat_split_pc.png"));
            var minimapFlag = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_minimap_flag_pc.png"));
            var minimapSwitch = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_minimap_switch_pc.png"));
            var minimapWorld = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_minimap_world_full_pc.png"));
            var minimapCave = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_minimap_cave_pc.png"));
            var quick1 = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_quick_item_1_pc.png"));
            var quick9 = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_quick_item_9_pc.png"));
            var leftSkill = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_pc_left_skill_slot.png"));
            var rightSkill = LoadTexture(Path.Combine(Application.dataPath, ArtRoot, "btn_pc_right_skill_slot.png"));
            AssertPixelsEqual(itemEx.GetPixel(14, 14), pc.GetPixel(522 + 14, 559 + 14), "ItemEx crop must stay PC-derived");
            AssertPixelsEqual(task.GetPixel(14, 14), pc.GetPixel(584 + 14, 559 + 14), "Task crop must stay PC-derived");
            AssertPixelsEqual(chatRoom.GetPixel(14, 14), pc.GetPixel(708 + 14, 559 + 14), "ChatRoom crop must stay PC-derived");
            AssertPixelsEqual(rec.GetPixel(15, 15), pc.GetPixel(663 + 15, 502 + 15), "Recorder crop must stay PC-derived");
            AssertPixelsEqual(face.GetPixel(12, 12), pc.GetPixel(282 + 12, 526 + 12), "chat face crop must stay PC-derived");
            AssertPixelsEqual(channelIdentity.GetPixel(16, 16), pc.GetPixel(2 + 16, 526 + 16), "chat current-channel identity crop must stay PC-derived");
            AssertPixelsEqual(chatThumb.GetPixel(7, 13), pc.GetPixel(1 + 7, 337 + 13), "chat scroll thumb crop must stay PC-derived");
            AssertPixelsEqual(chatSplit.GetPixel(7, 42), pc.GetPixel(2 + 7, 365 + 42), "chat split handle crop must stay PC-derived");
            AssertPixelsEqual(minimapFlag.GetPixel(8, 8), pc.GetPixel(742 + 8, 134 + 8), "minimap flag crop must stay PC-derived");
            AssertPixelsEqual(minimapSwitch.GetPixel(8, 8), pc.GetPixel(758 + 8, 134 + 8), "minimap switch crop must stay PC-derived");
            AssertPixelsEqual(minimapWorld.GetPixel(8, 8), pc.GetPixel(774 + 8, 134 + 8), "minimap world-map crop must stay PC-derived");
            AssertPixelsEqual(minimapCave.GetPixel(8, 8), pc.GetPixel(790 + 8, 134 + 8), "minimap cave crop must stay PC-derived");
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
            Object.DestroyImmediate(channelIdentity);
            Object.DestroyImmediate(chatThumb);
            Object.DestroyImmediate(chatSplit);
            Object.DestroyImmediate(minimapFlag);
            Object.DestroyImmediate(minimapSwitch);
            Object.DestroyImmediate(minimapWorld);
            Object.DestroyImmediate(minimapCave);
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
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_chat_channel_identity_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_chat_scroll_thumb_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_chat_split_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_flag_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_switch_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_world_full_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_minimap_cave_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_rec.png");
            for (int i = 1; i <= 9; i++)
                AssertCriticalTextureImport($"Assets/UI/HUD/Art/btn_quick_item_{i}_pc.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_pc_left_skill_slot.png");
            AssertCriticalTextureImport("Assets/UI/HUD/Art/btn_pc_right_skill_slot.png");
            foreach (var file in new[] { "icon_bar_arena.png", "icon_bar_activity.png", "icon_bar_treasure.png", "icon_bar_shop.png", "icon_bar_pet.png", "icon_bar_loginprize.png", "icon_bar_funcprize.png" })
                AssertCriticalTextureImport($"Assets/UI/HUD/Art/{file}");
        }

        private static string ExtractCssBlock(string css, string selector)
        {
            int start = css.IndexOf(selector, System.StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, selector + " must exist in GameHud.uss");
            int open = css.IndexOf('{', start);
            int close = css.IndexOf('}', open + 1);
            Assert.Greater(open, start, selector + " must open a USS block.");
            Assert.Greater(close, open, selector + " must close a USS block.");
            return css.Substring(open + 1, close - open - 1);
        }

        private static Dictionary<string, int> PcIconBarButtonIds()
        {
            return new Dictionary<string, int>
            {
                ["IconBarArenaBtn"] = 0,
                ["IconBarActivityBtn"] = 1,
                ["IconBarTreasureBtn"] = 2,
                ["IconBarShopBtn"] = 3,
                ["IconBarPetBtn"] = 4,
                ["IconBarLoginPrizeBtn"] = 5,
                ["IconBarFuncPrizeBtn"] = 6,
            };
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
